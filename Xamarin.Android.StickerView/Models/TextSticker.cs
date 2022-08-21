using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Text;
using Xamarin.Android.StickerView.Utils;
using System;

namespace Xamarin.Android.StickerView.Models
{
    public class TextSticker : Sticker
    {
        #region Constants

        private const int DEFAULT_MIN_TEXT_SIZE = 14;
        private const int DEFAULT_MAX_TEXT_SIZE = 120;

        #endregion

        #region Fields

        private static readonly string ellipsis = "\u2026";

        private readonly float _screenWidth, _screenHeight;
        private readonly Rect _realBounds;
        private readonly Rect _textRect;
        private readonly TextPaint _textPaint;

        private Drawable drawable;
        private StaticLayout staticLayout;
        private Layout.Alignment alignment;
        private string text;

        #endregion

        #region Properties

        /**
         * Upper bounds for text size.
         * This acts as a starting point for resizing.
         */
        private float MaxTextSizePixels { get; set; }

        /**
         * Lower bounds for text size.
         */
        private float MinTextSizePixels { get; set; }

        /**
         * Line spacing multiplier.
         */
        private float LineSpacingMultiplier { get; set; } = 1.0f;

        /**
         * Additional line spacing.
         */
        private float LineSpacingExtra { get; set; } = 0.0f;

        public Layout.Alignment Alignment {
            get => alignment;
            set => alignment = value;
        }

        public string Text
        {
            get => text;
            set {
                text = value;
                OnTextChanged?.Invoke();
            }
        }

        public Action OnTextChanged { get; set; }

        #endregion

        #region Constructors

        public TextSticker(Context context): this(context, null) { }

        public TextSticker(Context context, Drawable drawable, bool useScreenSize = false)
        {
            this.drawable = drawable;

            if (drawable == null)
                this.drawable = context
                    .GetDrawable(Resource.Drawable.transparent_background);

            MinTextSizePixels = StickerUtils.ConvertSpToPx(DEFAULT_MIN_TEXT_SIZE);
            MaxTextSizePixels = StickerUtils.ConvertSpToPx(DEFAULT_MAX_TEXT_SIZE);

            _screenWidth = useScreenSize 
                ? context.Resources.DisplayMetrics.WidthPixels
                : drawable.IntrinsicWidth;

            _screenHeight = useScreenSize
                ? context.Resources.DisplayMetrics.HeightPixels
                : drawable.IntrinsicHeight;

            _textPaint = new TextPaint(PaintFlags.AntiAlias);

            _realBounds = new Rect(
                0,
                0,
                GetWidth(), 
                GetHeight());

            _textRect = new Rect(
                0,
                0,
                GetWidth(),
                GetHeight());

            _textPaint.TextSize = MaxTextSizePixels;

            alignment = Layout.Alignment.AlignNormal;
        }

        #endregion

        #region Overrides

        public override void Draw(Canvas canvas)
        {
            var matrix = GetMatrix();
            canvas.Save();
            canvas.Concat(matrix);

            if (drawable != null)
            {
                drawable.Bounds = _realBounds;
                drawable.Draw(canvas);
            }

            canvas.Restore();
            canvas.Save();
            canvas.Concat(matrix);

            if (_textRect.Width() == GetWidth())
            {
                canvas.Translate(0, 0);
            }
            else
            {
                var dx = _textRect.Left;
                var dy = _textRect.Top + _textRect.Height() / 2 - staticLayout?.Height ?? 0 / 2;
                canvas.Translate(dx, dy);
            }

            staticLayout?.Draw(canvas);
            canvas.Restore();
        }

        public override int GetHeight() => (int)_screenHeight;

        public override int GetWidth() => (int)_screenWidth;

        public override Sticker SetAlpha(int alpha)
        {
            _textPaint.Alpha = alpha;
            return this;
        }

        public override Drawable GetDrawable() => drawable;

        public override Sticker SetDrawable(Drawable drawable)
        {
            this.drawable = drawable;
            _realBounds.Set(0, 0, GetWidth(), GetHeight());
            _textRect.Set(0, 0, GetWidth(), GetHeight());
            return this;
        }

        #endregion

        #region Public Methods

        /**
           * Resize this view's text size with respect to its width and height
           * (minus padding). You should always call this method after the initialization.
           */
        public TextSticker ResizeText()
        {
            var availableHeightPixels = _textRect.Height();
            var availableWidthPixels = _textRect.Width();

            var text = Text;

            // Safety check
            // (Do not resize if the view does not have dimensions or if there is no text)
            if (text == null
                || availableHeightPixels <= 0
                || availableWidthPixels <= 0
                || MaxTextSizePixels <= 0)
            {
                return this;
            }

            var targetTextSizePixels = MaxTextSizePixels;
            var targetTextHeightPixels =
                GetTextHeightPixels(text, availableWidthPixels, targetTextSizePixels);

            // Until we either fit within our TextView
            // or we have reached our minimum text size,
            // incrementally try smaller sizes
            while (targetTextHeightPixels > availableHeightPixels
                && targetTextSizePixels > MinTextSizePixels)
            {
                targetTextSizePixels = Math.Max(targetTextSizePixels - 2, MinTextSizePixels);

                targetTextHeightPixels =
                    GetTextHeightPixels(text, availableWidthPixels, targetTextSizePixels);
            }

            // If we have reached our minimum text size and the text still doesn't fit,
            // append an ellipsis
            // (NOTE: Auto-ellipsize doesn't work hence why we have to do it here)
            if (targetTextSizePixels == MinTextSizePixels
                && targetTextHeightPixels > availableHeightPixels)
            {
                // Make a copy of the original TextPaint object for measuring
                var textPaintCopy = new TextPaint(_textPaint);
                textPaintCopy.TextSize = targetTextSizePixels;

                // Measure using a StaticLayout instance
                staticLayout =
                    new StaticLayout(
                        text, 
                        textPaintCopy, 
                        availableWidthPixels, 
                        Layout.Alignment.AlignNormal,
                        LineSpacingMultiplier, 
                        LineSpacingExtra, 
                        false);

                // Check that we have a least one line of rendered text
                if (staticLayout.LineCount > 0)
                {
                    // Since the line at the specific vertical position would be cut off,
                    // we must trim up to the previous line and add an ellipsis
                    var lastLine = staticLayout.GetLineForVertical(availableHeightPixels) - 1;

                    if (lastLine >= 0)
                    {
                        var startOffset = staticLayout.GetLineStart(lastLine);
                        var endOffset = staticLayout.GetLineEnd(lastLine);
                        var lineWidthPixels = staticLayout.GetLineWidth(lastLine);
                        var ellipseWidth = textPaintCopy.MeasureText(ellipsis);

                        // Trim characters off until we have enough room to draw the ellipsis
                        while (availableWidthPixels < lineWidthPixels + ellipseWidth)
                        {
                            endOffset--;
                            lineWidthPixels = textPaintCopy
                                .MeasureText(text.Substring(startOffset, endOffset + 1).ToString());
                        }

                        Text = text.Substring(0, endOffset) + ellipsis;
                    }
                }
            }
            
            _textPaint.TextSize = targetTextSizePixels;

            staticLayout =
                new StaticLayout(
                    Text, 
                    _textPaint, 
                    _textRect.Width(), 
                    Alignment, 
                    LineSpacingMultiplier, 
                    LineSpacingExtra, 
                    true);

            return this;
        }

        public TextSticker SetDrawable(Drawable drawable, Rect region)
        {
            this.drawable = drawable;

            _realBounds.Set(0, 0, GetWidth(), GetHeight());

            if (region == null)
            {
                _textRect.Set(0, 0, GetWidth(), GetHeight());
            }
            else
            {
                _textRect.Set(region.Left, region.Top, region.Right, region.Bottom);
            }

            return this;
        }

        public TextSticker SetTypeface(Typeface typeface)
        {
            _textPaint.SetTypeface(typeface);
            return this;
        }

        public TextSticker SetTextColor(Color color)
        {
            _textPaint.Color = color;
            return this;
        }

        public TextSticker SetTextAlign(Layout.Alignment alignment)
        {
            Alignment = alignment;
            return this;
        }

        public TextSticker SetMaxTextSize(float size)
        {
            _textPaint.TextSize = StickerUtils.ConvertSpToPx(size);
            MaxTextSizePixels = _textPaint.TextSize;
            return this;
        }

        /**
         * Sets the lower text size limit
         *
         * @param minTextSizeScaledPixels the minimum size to use for text in this view,
         * in scaled pixels.
         */
        public TextSticker SetMinTextSize(float minTextSizeScaledPixels)
        {
            MinTextSizePixels = StickerUtils.ConvertSpToPx(minTextSizeScaledPixels);
            return this;
        }

        public TextSticker SetLineSpacing(float add, float multiplier)
        {
            LineSpacingMultiplier = multiplier;
            LineSpacingExtra = add;
            return this;
        }

        #endregion

        #region Protected Methods

        /**
         * Sets the text size of a clone of the view's {@link TextPaint} object
         * and uses a {@link StaticLayout} instance to measure the height of the text.
         *
         * @return the height of the text when placed in a view
         * with the specified width
         * and when the text has the specified size.
         */
        protected int GetTextHeightPixels(string source, int availableWidthPixels, float textSizePixels)
        {
            _textPaint.TextSize = textSizePixels;
            // It's not efficient to create a StaticLayout instance
            // every time when measuring, we can use StaticLayout.Builder
            // since api 23.
            StaticLayout staticLayout =
                new StaticLayout(
                    source,
                    _textPaint, 
                    availableWidthPixels, 
                    Layout.Alignment.AlignNormal,
                    LineSpacingMultiplier, 
                    LineSpacingExtra, 
                    true);

            return staticLayout.Height;
        }

        #endregion
    }
}