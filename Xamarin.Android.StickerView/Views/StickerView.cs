using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using Xamarin.Android.StickerView.Abstractions.Enums;
using Xamarin.Android.StickerView.Abstractions.Interfaces;
using Xamarin.Android.StickerView.Events;
using Xamarin.Android.StickerView.Extensions;
using Xamarin.Android.StickerView.Models;
using Xamarin.Android.StickerView.Utils;
using System;
using System.Collections.Generic;
using ActionMode = Xamarin.Android.StickerView.Abstractions.Enums.ActionMode;

namespace Xamarin.Android.StickerView.Views
{
    public class StickerView : FrameLayout
    {
        #region Constants

        private const string TAG = "StickerView";

        private const int DEFAULT_MIN_CLICK_DELAY_TIME = 200;

        #endregion

        #region Fields

        private int touchSlop;
        private bool showIcons;
        private bool showBorder;
        private bool bringToFrontCurrentSticker;

        private readonly List<Sticker> _stickers = new List<Sticker>();
        private readonly List<BitmapStickerIcon> _icons = new List<BitmapStickerIcon>(4);

        private readonly Paint _borderPaint = new Paint();
        private readonly RectF _stickerRect = new RectF();

        private readonly Matrix _sizeMatrix = new Matrix();
        private readonly Matrix _downMatrix = new Matrix();
        private readonly Matrix _moveMatrix = new Matrix();

        private Sticker handlingSticker;
        private BitmapStickerIcon currentIcon;
        private IOnStickerOperationListener onStickerOperationListener;

        private float downX;
        private float downY;
        private float oldDistance = 0f;
        private float oldRotation = 0f;
        private ActionMode currentMode = ActionMode.None;

        private bool locked;
        private bool constrained;

        private long lastClickTime = 0;
        private int minClickDelayTime = DEFAULT_MIN_CLICK_DELAY_TIME;

        #endregion

        #region Storage Fields

        private readonly float[] _bitmapPoints = new float[8];
        private readonly float[] _bounds = new float[8];
        private readonly float[] _point = new float[2];
        private readonly PointF _currentCenterPoint = new PointF();
        private readonly float[] _tmp = new float[2];
        private PointF midPoint = new PointF();

        #endregion

        #region Properties

        public bool Constrained { 
            get => constrained; 
            set => constrained = value;
        }

        public bool Locked
        {
            get => locked;
            set => locked = value;
        }

        public int MinClickDelayTime
        {
            get => minClickDelayTime;
            set => minClickDelayTime = value;
        }

        public IOnStickerOperationListener OnStickerOperationListener
        {
            get => onStickerOperationListener;
            set => onStickerOperationListener = value;
        }

        #endregion

        #region Constructors

        public StickerView(Context context) : base(context, null) { }

        public StickerView(Context context, IAttributeSet attrs)
            : base(context, attrs, 0)
        {
            Initialize(context, attrs);
        }

        public StickerView(Context context, IAttributeSet attrs, int defStyleAttr)
            : base(context, attrs, defStyleAttr)
        {
            Initialize(context, attrs, defStyleAttr);
        }

        #endregion

        #region Overrides

        protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
        {
            base.OnLayout(changed, left, top, right, bottom);
            if (!changed) return;

            _stickerRect.Left = left;
            _stickerRect.Top = top;
            _stickerRect.Right = right;
            _stickerRect.Bottom = bottom;
        }

        protected override void DispatchDraw(Canvas canvas)
        {
            base.DispatchDraw(canvas);
            DrawStickers(canvas);
        }

        public override bool OnInterceptTouchEvent(MotionEvent ev)
        {
            if (locked) return base.OnInterceptTouchEvent(ev);

            switch (ev.Action)
            {
                case MotionEventActions.Down:
                    downX = ev.GetX();
                    downY = ev.GetY();

                    return FindCurrentIconTouched() != null || FindHandlingSticker() != null;
            }

            return base.OnInterceptTouchEvent(ev);
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            if (locked) return base.OnTouchEvent(e);

            var action = e.ActionMasked;

            switch (action)
            {
                case MotionEventActions.Down:
                    if (!OnTouchDown(e)) return false;
                    break;

                case MotionEventActions.PointerDown:
                    oldDistance = CalculateDistance(e);
                    oldRotation = CalculateRotation(e);

                    midPoint = CalculateMidPoint(e);

                    var isInStickerArea = IsInStickerArea(handlingSticker, e.GetX(1), e.GetY(1));
                    if (handlingSticker != null && isInStickerArea && FindCurrentIconTouched() == null)
                        currentMode = ActionMode.ZoomWithTwoFinger;

                    break;

                case MotionEventActions.Move:
                    HandleCurrentMode(e);
                    Invalidate();
                    break;

                case MotionEventActions.Up:
                    OnTouchUp(e);
                    break;

                case MotionEventActions.PointerUp:
                    if (currentMode == ActionMode.ZoomWithTwoFinger && handlingSticker != null)
                    {
                        if (onStickerOperationListener != null)
                        {
                            onStickerOperationListener.OnStickerZoomFinished(handlingSticker);
                        }
                    }

                    currentMode = ActionMode.None;
                    break;
            }

            return true;
        }

        protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
        {
            base.OnSizeChanged(w, h, oldw, oldh);
            for (var i = 0; i < _stickers.Count; i++)
            {
                var sticker = _stickers[i];
                if (sticker == null) continue;

                TransformSticker(sticker);
            }
        }

        #endregion

        #region Public Methods

        /**
         * Swaps sticker at layer [[oldPos]] with the one at layer [[newPos]].
         * Does nothing if either of the specified layers doesn't exist.
         */
        public void SwapLayers(int oldPos, int newPos)
        {
            if (_stickers.Count >= oldPos && _stickers.Count >= newPos)
            {
                _stickers.Swap(oldPos, newPos);
                Invalidate();
            }
        }

        /**
         * Sends sticker from layer [[oldPos]] to layer [[newPos]].
         * Does nothing if either of the specified layers doesn't exist.
         */
        public void SendToLayer(int oldPos, int newPos)
        {
            if (_stickers.Count >= oldPos && _stickers.Count >= newPos)
            {
                Sticker s = _stickers[oldPos];
                _stickers.RemoveAt(oldPos);
                _stickers.AddAt(newPos, s);
                Invalidate();
            }
        }

        public void ZoomAndRotateCurrentSticker(MotionEvent e)
        {
            ZoomAndRotateSticker(handlingSticker, e);
        }

        public void ZoomAndRotateSticker(Sticker sticker, MotionEvent e)
        {
            if (sticker == null) return;

            var newDistance = CalculateDistance(midPoint.X, midPoint.Y, e.GetX(), e.GetY());
            var newRotation = CalculateRotation(midPoint.X, midPoint.Y, e.GetX(), e.GetY());

            _moveMatrix.Set(_downMatrix);
            _moveMatrix.PostScale(newDistance / oldDistance, newDistance / oldDistance, midPoint.X, midPoint.Y);
            _moveMatrix.PostRotate(newRotation - oldRotation, midPoint.X, midPoint.Y);
            handlingSticker.SetMatrix(_moveMatrix);
        }

        public void FlipCurrentSticker(Flip direction) =>
            Flip(handlingSticker, direction);

        public void Flip(Sticker sticker, Flip direction)
        {
            if (sticker == null) return;

            sticker.GetCenterPoint(midPoint);

            if (direction == Abstractions.Enums.Flip.Horizontally)
            {
                sticker.GetMatrix().PreScale(-1, 1, midPoint.X, midPoint.Y);
                sticker.IsFlippedHorizontally = !sticker.IsFlippedHorizontally;
            }
            if (direction == Abstractions.Enums.Flip.Vertically)
            {
                sticker.GetMatrix().PreScale(1, -1, midPoint.X, midPoint.Y);
                sticker.IsFlippedVertically = !sticker.IsFlippedVertically;
            }

            if (onStickerOperationListener != null)
            {
                onStickerOperationListener.OnStickerFlipped(sticker);
            }

            Invalidate();
        }

        public bool Replace(Sticker sticker) =>
            Replace(sticker, true);

        public bool Replace(Sticker sticker, bool needStayState)
        {
            if (!(handlingSticker != null && sticker != null)) return false;

            var width = Width;
            var height = Height;

            if (needStayState)
            {
                sticker.SetMatrix(handlingSticker.GetMatrix());
                sticker.IsFlippedVertically = handlingSticker.IsFlippedVertically;
                sticker.IsFlippedHorizontally = handlingSticker.IsFlippedHorizontally;
            }
            else
            {
                handlingSticker.GetMatrix().Reset();
                // reset scale, angle, and put it in center
                var offsetX = (width - handlingSticker.GetWidth()) / 2f;
                var offsetY = (height - handlingSticker.GetHeight()) / 2f;
                sticker.GetMatrix().PostTranslate(offsetX, offsetY);

                float scaleFactor;
                if (width < height)
                {
                    scaleFactor = width / handlingSticker.GetDrawable().IntrinsicWidth;
                }
                else
                {
                    scaleFactor = height / handlingSticker.GetDrawable().IntrinsicHeight;
                }

                sticker.GetMatrix().PostScale(scaleFactor / 2f, scaleFactor / 2f, width / 2f, height / 2f);
            }

            var index = _stickers.IndexOf(handlingSticker);
            _stickers.AddAt(index, sticker);
            handlingSticker = sticker;

            Invalidate();
            return true;
        }

        public bool RemoveCurrentSticker() =>
            Remove(handlingSticker);

        public bool Remove(Sticker sticker)
        {
            if (!_stickers.Contains(sticker))
            {
                Log.WriteLine(
                    LogPriority.Debug,
                    TAG,
                    "remove: the sticker is not in this StickerView");

                return false;
            }

            _stickers.Remove(sticker);

            if (onStickerOperationListener != null)
                onStickerOperationListener.OnStickerDeleted(sticker);

            if (handlingSticker == sticker)
                handlingSticker = null;

            Invalidate();

            return true;
        }

        public void RemoveAllStickers()
        {
            _stickers.Clear();
            if (handlingSticker != null)
            {
                handlingSticker.Release();
                handlingSticker = null;
            }

            Invalidate();
        }

        public StickerView AddSticker(Sticker sticker) =>
            AddSticker(sticker, Position.Center);

        public StickerView AddSticker(Sticker sticker, Position position)
        {
            if (IsLaidOut)
            {
                AddStickerImmediately(sticker, position);
            } else
            {
                Post(() => AddStickerImmediately(sticker, position));
            }

            return this;
        }

        public float[] GetStickerPoints(Sticker sticker)
        {
            float[] points = new float[8];
            GetStickerPoints(sticker, points);
            return points;
        }

        public void GetStickerPoints(Sticker sticker, float[] dst)
        {
            if (sticker == null)
            {
                Array.Fill(dst, 0);
                return;
            }

            sticker.GetBoundPoints(_bounds);
            sticker.GetMappedPoints(dst, _bounds);
        }

        public Bitmap CreateBitmap()
        {
            handlingSticker = null;
            Bitmap bitmap = Bitmap.CreateBitmap(Width, Height, Bitmap.Config.Argb8888);
            Canvas canvas = new Canvas(bitmap);
            Draw(canvas);
            return bitmap;
        }

        public StickerView UpdateLocked(bool locked)
        {
            Locked = locked;
            Invalidate();
            return this;
        }

        public StickerView UpdateConstrained(bool constrained)
        {
            Constrained = constrained;
            PostInvalidate();
            return this;
        }

        public void UpdateIcons(List<BitmapStickerIcon> icons)
        {
            _icons.Clear();
            _icons.AddRange(icons);
            Invalidate();
        }

        public int GetStickerCount() => _stickers.Count;

        public bool HasStickers() => _stickers.Count > 0;

        public Sticker GetCurrentSticker() => handlingSticker;

        public List<BitmapStickerIcon> GetIcons() => _icons;

        #endregion

        #region Protected Methods

        protected void DrawStickers(Canvas canvas)
        {
            for (var i = 0; i < _stickers.Count; i++)
            {
                var sticker = _stickers[i];
                if (sticker == null) continue;

                sticker.Draw(canvas);
            }

            if (handlingSticker != null && !locked && (showBorder || showIcons))
            {
                GetStickerPoints(handlingSticker, _bitmapPoints);

                var x1 = _bitmapPoints[0];
                var y1 = _bitmapPoints[1];
                var x2 = _bitmapPoints[2];
                var y2 = _bitmapPoints[3];
                var x3 = _bitmapPoints[4];
                var y3 = _bitmapPoints[5];
                var x4 = _bitmapPoints[6];
                var y4 = _bitmapPoints[7];

                if (showBorder)
                {
                    canvas.DrawLine(x1, y1, x2, y2, _borderPaint);
                    canvas.DrawLine(x1, y1, x3, y3, _borderPaint);
                    canvas.DrawLine(x2, y2, x4, y4, _borderPaint);
                    canvas.DrawLine(x4, y4, x3, y3, _borderPaint);
                }

                //draw icons
                if (showIcons)
                {
                    float rotation = CalculateRotation(x4, y4, x3, y3);
                    for (int i = 0; i < _icons.Count; i++)
                    {
                        BitmapStickerIcon icon = _icons[i];
                        switch (icon.GetPosition())
                        {
                            case Abstractions.Enums.Gravity.LeftTop:
                                ConfigIconMatrix(icon, x1, y1, rotation);
                                break;

                            case Abstractions.Enums.Gravity.RightTop:
                                ConfigIconMatrix(icon, x2, y2, rotation);
                                break;

                            case Abstractions.Enums.Gravity.LeftBottom:
                                ConfigIconMatrix(icon, x3, y3, rotation);
                                break;

                            case Abstractions.Enums.Gravity.RightBottom:
                                ConfigIconMatrix(icon, x4, y4, rotation);
                                break;
                        }

                        icon.Draw(canvas, _borderPaint);
                    }
                }
            }
        }

        protected void ConfigIconMatrix(BitmapStickerIcon icon, float x, float y, float rotation)
        {
            icon.SetX(x);
            icon.SetY(y);
            icon.GetMatrix().Reset();

            icon.GetMatrix().PostRotate(rotation, icon.GetWidth() / 2, icon.GetHeight() / 2);
            icon.GetMatrix().PostTranslate(x - icon.GetWidth() / 2, y - icon.GetHeight() / 2);
        }

        /**
         * @param event MotionEvent received from {@link #onTouchEvent)
         * @return true if has touch something
         */
        protected bool OnTouchDown(MotionEvent e)
        {
            currentMode = ActionMode.Drag;

            downX = e.GetX();
            downY = e.GetY();

            midPoint = CalculateMidPoint();
            oldDistance = CalculateDistance(midPoint.X, midPoint.Y, downX, downY);
            oldRotation = CalculateRotation(midPoint.X, midPoint.Y, downX, downY);

            currentIcon = FindCurrentIconTouched();
            if (currentIcon != null) {
                currentMode = ActionMode.Icon;
                currentIcon.OnActionDown(this, e);
            } else {
                handlingSticker = FindHandlingSticker();
            }

            if (handlingSticker != null) {
                _downMatrix.Set(handlingSticker.GetMatrix());
                if (bringToFrontCurrentSticker) {
                    _stickers.Remove(handlingSticker);
                    _stickers.Add(handlingSticker);
                }

                OnStickerClicked();
            }

            if (currentIcon == null && handlingSticker == null)
                return false;

            Invalidate();

            return true;
        }

        protected void OnTouchUp(MotionEvent e)
        {
            var currentTime = SystemClock.UptimeMillis();
            if (currentMode == ActionMode.Icon && currentIcon != null && handlingSticker != null)
                currentIcon.OnActionUp(this, e);

            if (currentMode == ActionMode.Drag 
                && Math.Abs(e.GetX() - downX) < touchSlop
                && Math.Abs(e.GetY() - downY) < touchSlop
                && handlingSticker != null)
            {
                currentMode = ActionMode.Click;
                //OnStickerClicked();

                if (currentTime - lastClickTime < minClickDelayTime)
                {
                    if (onStickerOperationListener != null)
                    {
                        onStickerOperationListener.OnStickerDragFinished(handlingSticker);
                    }
                }
            }

            currentMode = ActionMode.None;
            lastClickTime = currentTime;
        }

        protected void HandleCurrentMode(MotionEvent e)
        {
            switch (currentMode)
            {
                case ActionMode.None:
                case ActionMode.Click:
                    break;

                case ActionMode.Drag:
                    if (handlingSticker != null)
                    {
                        _moveMatrix.Set(_downMatrix);
                        _moveMatrix.PostTranslate(e.GetX() - downX, e.GetY() - downY);
                        handlingSticker.SetMatrix(_moveMatrix);
                        if (constrained)
                            ConstrainSticker(handlingSticker);
                    }
                    break;

                case ActionMode.ZoomWithTwoFinger:
                    if (handlingSticker != null)
                    {
                        var newDistance = CalculateDistance(e);
                        var newRotation = CalculateRotation(e);

                        _moveMatrix.Set(_downMatrix);
                        _moveMatrix.PostScale(newDistance / oldDistance, newDistance / oldDistance, midPoint.X, midPoint.Y);
                        _moveMatrix.PostRotate(newRotation - oldRotation, midPoint.X, midPoint.Y);
                        handlingSticker.SetMatrix(_moveMatrix);
                    }
                    break;

                case ActionMode.Icon:
                    if (handlingSticker != null && currentIcon != null)
                        currentIcon.OnActionMove(this, e);
                    break;
            }
        }

        protected void ConstrainSticker(Sticker sticker)
        {
            var moveX = 0f;
            var moveY = 0f;
            var width = Width;
            var height = Height;

            sticker.GetMappedCenterPoint(_currentCenterPoint, _point, _tmp);
            if (_currentCenterPoint.X < 0)
                moveX = -_currentCenterPoint.X;

            if (_currentCenterPoint.X > width)
                moveX = width - _currentCenterPoint.X;

            if (_currentCenterPoint.Y < 0)
                moveY = -_currentCenterPoint.Y;

            if (_currentCenterPoint.Y > height)
                moveY = height - _currentCenterPoint.Y;

            sticker.GetMatrix().PostTranslate(moveX, moveY);
        }

        protected BitmapStickerIcon FindCurrentIconTouched()
        {
            foreach (var icon in _icons)
            {
                var x = icon.GetX() - downX;
                var y = icon.GetY() - downY;
                var distancePow2 = x * x + y * y;
                if (distancePow2 <= Math.Pow(icon.GetIconRadius() + icon.GetIconRadius(), 2))
                    return icon;
            }

            return null;
        }

        /**
         * find the touched Sticker
         **/
        protected Sticker FindHandlingSticker()
        {
            for (int i = _stickers.Count - 1; i >= 0; i--)
            {
                if (IsInStickerArea(_stickers[i], downX, downY))
                    return _stickers[i];
            }

            return null;
        }

        /**
         * check if sticker is on touched area
         **/
        protected bool IsInStickerArea(Sticker sticker, float downX, float downY)
        {
            _tmp[0] = downX;
            _tmp[1] = downY;
            return sticker.Contains(_tmp);
        }

        protected PointF CalculateMidPoint()
        {
            if (handlingSticker == null)
            {
                midPoint.Set(0, 0);
                return midPoint;
            }

            handlingSticker.GetMappedCenterPoint(midPoint, _point, _tmp);
            return midPoint;
        }

        protected PointF CalculateMidPoint(MotionEvent e)
        {
            if (e == null || e.PointerCount < 2)
            {
                midPoint.Set(0, 0);
                return midPoint;
            }

            var x = (e.GetX(0) + e.GetX(1)) / 2;
            var y = (e.GetY(0) + e.GetY(1)) / 2;
            midPoint.Set(x, y);
            return midPoint;
        }

        /**
         * calculate rotation in line with two fingers and x-axis
         **/
        protected float CalculateRotation(MotionEvent e)
        {
            if (e == null || e.PointerCount < 2)
                return 0f;

            return CalculateRotation(e.GetX(0), e.GetY(0), e.GetX(1), e.GetY(1));
        }

        protected float CalculateRotation(float x1, float y1, float x2, float y2)
        {
            double x = x1 - x2;
            double y = y1 - y2;
            double radians = Math.Atan2(y, x);

            return (float) StickerUtils.RadianToDegrees(radians);
        }

        /**
         * calculate Distance in two fingers
         **/
        protected float CalculateDistance(MotionEvent e)
        {
            if (e == null || e.PointerCount < 2) 
                return 0f;

            return CalculateDistance(e.GetX(0), e.GetY(0), e.GetX(1), e.GetY(1));
        }

        protected float CalculateDistance(float x1, float y1, float x2, float y2)
        {
            var x = x1 - x2;
            var y = y1 - y2;

            return (float)Math.Sqrt(x * x + y * y);
        }

        /**
         * Sticker's drawable will be too bigger or smaller
         * This method is to transform it to fit
         * step 1：let the center of the sticker image is coincident with the center of the View.
         * step 2：Calculate the zoom and zoom
         **/
        protected void TransformSticker(Sticker sticker)
        {
            if (sticker == null)
            {
                Log.WriteLine(
                    LogPriority.Debug, 
                    TAG, 
                    "transformSticker: the bitmapSticker is null or the bitmapSticker bitmap is null");

                return;
            }

            _sizeMatrix.Reset();

            var width = Width;
            var height = Height;
            var stickerWidth = sticker.GetWidth();
            var stickerHeight = sticker.GetHeight();
            //step 1
            var offsetX = (width - stickerWidth) / 2;
            var offsetY = (height - stickerHeight) / 2;

            _sizeMatrix.PostTranslate(offsetX, offsetY);

            //step 2
            float scaleFactor;
            if (width < height)
            {
                scaleFactor = width / stickerWidth;
            }
            else
            {
                scaleFactor = height / stickerHeight;
            }

            _sizeMatrix.PostScale(scaleFactor / 2f, scaleFactor / 2f, width / 2f, height / 2f);

            sticker.GetMatrix().Reset();
            sticker.SetMatrix(_sizeMatrix);

            Invalidate();
        }

        protected void AddStickerImmediately(Sticker sticker, Position position)
        {
            SetStickerPosition(sticker, position);

            float scaleFactor, widthScaleFactor, heightScaleFactor;

            widthScaleFactor = (float)Width / sticker.GetDrawable().IntrinsicWidth;
            heightScaleFactor = (float)Height / sticker.GetDrawable().IntrinsicHeight;
            scaleFactor = widthScaleFactor > heightScaleFactor ? heightScaleFactor : widthScaleFactor;

            sticker.GetMatrix()
                .PostScale(scaleFactor / 2, scaleFactor / 2, Width / 2, Height / 2);

            handlingSticker = sticker;
            _stickers.Add(sticker);

            if (onStickerOperationListener != null)
                onStickerOperationListener.OnStickerAdded(sticker);

            Invalidate();
        }

        protected void SetStickerPosition(Sticker sticker, Position position)
        {
            float width = Width;
            float height = Height;
            var offsetX = width - sticker.GetWidth();
            var offsetY = height - sticker.GetHeight();

            if (position == Position.Top)
            {
                offsetY /= 4f;
            }
            else if (position == Position.Bottom)
            {
                offsetY *= 3f / 4f;
            }
            else
            {
                offsetY /= 2f;
            }

            if (position == Position.Left)
            {
                offsetX /= 4f;
            }
            else if (position == Position.Right)
            {
                offsetX *= 3f / 4f;
            }
            else
            {
                offsetX /= 2f;
            }

            sticker.GetMatrix().PostTranslate(offsetX, offsetY);
        }

        #endregion

        #region Private Methods

        private void Initialize(Context context, IAttributeSet attrs, int defStyleAttr = 0)
        {
            touchSlop = ViewConfiguration.Get(context).ScaledTouchSlop;
            TypedArray a = null;

            try
            {
                a = context.ObtainStyledAttributes(attrs, Resource.Styleable.StickerView);

                showIcons = a.GetBoolean(Resource.Styleable.StickerView_showIcons, false);
                showBorder = a.GetBoolean(Resource.Styleable.StickerView_showBorder, false);
                bringToFrontCurrentSticker = a.GetBoolean(Resource.Styleable.StickerView_bringToFrontCurrentSticker, false);

                _borderPaint.AntiAlias = true;
                _borderPaint.Color = a.GetColor(Resource.Styleable.StickerView_borderColor, Color.Black);
                _borderPaint.Alpha = a.GetInteger(Resource.Styleable.StickerView_borderAlpha, 128);

                ConfigDefaultIcons();
            }
            finally
            {
                if (a != null)
                {
                    a.Recycle();
                }
            }
        }

        private void ConfigDefaultIcons()
        {
            _icons.Clear();

            ConfigDeleteIcon();
            ConfigZoomIcon();
            ConfigFlipIcon();
        }

        private void ConfigDeleteIcon()
        {
            var deleteDrawable = Context.GetDrawable(
                Resource.Drawable.ic_sticker_delete);

            var deleteIcon = new BitmapStickerIcon(
                deleteDrawable, Abstractions.Enums.Gravity.LeftTop);

            deleteIcon.SetIconEvent(new DeleteIconEvent());

            _icons.Add(deleteIcon);
        }

        private void ConfigZoomIcon()
        {
            var zoomDrawable = Context.GetDrawable(
                Resource.Drawable.ic_sticker_zoom);

            var zoomIcon = new BitmapStickerIcon(
                zoomDrawable, Abstractions.Enums.Gravity.RightBottom);

            zoomIcon.SetIconEvent(new ZoomIconEvent());

            _icons.Add(zoomIcon);
        }

        private void ConfigFlipIcon()
        {
            var flipDrawable = Context
                .GetDrawable(Resource.Drawable.ic_sticker_flip);

            var flipIcon = new BitmapStickerIcon(
                flipDrawable, Abstractions.Enums.Gravity.RightTop);

            flipIcon.SetIconEvent(new FlipHorizontallyEvent());

            _icons.Add(flipIcon);
        }

        private void OnStickerClicked()
        {
            if (onStickerOperationListener == null) return;

            onStickerOperationListener.OnStickerClicked(handlingSticker);
        }

        #endregion
    }
}