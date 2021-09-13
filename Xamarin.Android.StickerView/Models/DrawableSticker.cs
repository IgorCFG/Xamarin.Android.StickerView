using Android.Graphics;
using Android.Graphics.Drawables;

namespace Xamarin.Android.StickerView.Models
{
    public class DrawableSticker : Sticker
    {
        #region Fields

        private Drawable drawable;
        private readonly Rect _realBounds;

        #endregion

        #region Constructors

        public DrawableSticker(Drawable drawable)
        {
            this.drawable = drawable;
            _realBounds = new Rect(0, 0, GetWidth(), GetHeight());
        }

        #endregion

        #region Sticker Methods

        public override Drawable GetDrawable() => drawable;

        public override Sticker SetDrawable(Drawable drawable)
        {
            this.drawable = drawable;
            return this;
        }

        public override void Draw(Canvas canvas)
        {
            canvas.Save();
            canvas.Concat(GetMatrix());
            drawable.Bounds = _realBounds;
            drawable.Draw(canvas);
            canvas.Restore();
        }

        public override Sticker SetAlpha(int alpha)
        {
            drawable.SetAlpha(alpha);
            return this;
        }

        public override int GetWidth() =>
            drawable.IntrinsicWidth;

        public override int GetHeight() =>
            drawable.IntrinsicHeight;

        public override void Release()
        {
            base.Release();
            if (drawable != null)
                drawable = null;
        }

        #endregion
    }
}