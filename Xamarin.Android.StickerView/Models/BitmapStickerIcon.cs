using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views;
using Xamarin.Android.StickerView.Abstractions.Interfaces;
using Xamarin.Android.StickerView.Utils;
using StckerView = Xamarin.Android.StickerView.Views.StickerView;
using Gravity = Xamarin.Android.StickerView.Abstractions.Enums.Gravity;

namespace Xamarin.Android.StickerView.Models
{
    public class BitmapStickerIcon : DrawableSticker, IStickerIconEvent
    {
        #region Fields

        private float iconRadius;
        private float iconExtraRadius;
        private float x;
        private float y;

        private Gravity position = Gravity.LeftTop;

        private IStickerIconEvent iconEvent;

        #endregion

        #region Getters/Setters

        public float GetX()
        {
            return x;
        }

        public void SetX(float x)
        {
            this.x = x;
        }

        public float GetY()
        {
            return y;
        }

        public void SetY(float y)
        {
            this.y = y;
        }

        public float GetIconRadius()
        {
            return iconRadius;
        }

        public void SetIconRadius(float iconRadius)
        {
            this.iconRadius = iconRadius;
        }

        public float GetIconExtraRadius()
        {
            return iconExtraRadius;
        }

        public void SetIconExtraRadius(float iconExtraRadius)
        {
            this.iconExtraRadius = iconExtraRadius;
        }

        public IStickerIconEvent GetIconEvent()
        {
            return iconEvent;
        }

        public void SetIconEvent(IStickerIconEvent iconEvent)
        {
            this.iconEvent = iconEvent;
        }

        public Gravity GetPosition()
        {
            return position;
        }

        public void SetPosition(Gravity position)
        {
            this.position = position;
        }

        #endregion

        #region Constructors

        public BitmapStickerIcon(Drawable drawable, Gravity gravity) : base(drawable)
        {
            position = gravity;
            iconRadius = StickerUtils.ConvertSpToPx(25f);
            iconExtraRadius = StickerUtils.ConvertSpToPx(10f);
        }

        #endregion

        #region Public Methods

        public void Draw(Canvas canvas, Paint paint)
        {
            canvas.DrawCircle(x, y, iconRadius, paint);
            base.Draw(canvas);
        }

        #endregion

        #region IStickerIconEvent

        public void OnActionDown(StckerView stickerView, MotionEvent motionEvent)
        {
            if (iconEvent == null) return;
            iconEvent.OnActionDown(stickerView, motionEvent);
        }

        public void OnActionMove(StckerView stickerView, MotionEvent motionEvent)
        {
            if (iconEvent == null) return;
            iconEvent.OnActionMove(stickerView, motionEvent);
        }

        public void OnActionUp(StckerView stickerView, MotionEvent motionEvent)
        {
            if (iconEvent == null) return;
            iconEvent.OnActionUp(stickerView, motionEvent);
        }

        #endregion
    }
}