using Android.Views;
using Xamarin.Android.StickerView.Abstractions.Enums;
using Xamarin.Android.StickerView.Abstractions.Interfaces;
using StckerView = Xamarin.Android.StickerView.Views.StickerView;

namespace Xamarin.Android.StickerView.Abstractions
{
    public abstract class FlipEvent : IStickerIconEvent
    {
        public void OnActionDown(StckerView stickerView, MotionEvent motionEvent) { }

        public void OnActionMove(StckerView stickerView, MotionEvent motionEvent) { }

        public void OnActionUp(StckerView stickerView, MotionEvent motionEvent) {
            stickerView.FlipCurrentSticker(GetFlipDirection());
        }

        protected abstract Flip GetFlipDirection();
    }
}