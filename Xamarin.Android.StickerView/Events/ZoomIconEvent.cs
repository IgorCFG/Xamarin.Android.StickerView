using Android.Views;
using Xamarin.Android.StickerView.Abstractions.Interfaces;
using StckerView = Xamarin.Android.StickerView.Views.StickerView;

namespace Xamarin.Android.StickerView.Events
{
    public class ZoomIconEvent : IStickerIconEvent
    {
        public void OnActionDown(StckerView stickerView, MotionEvent motionEvent) { }

        public void OnActionMove(StckerView stickerView, MotionEvent motionEvent) =>
            stickerView.ZoomAndRotateCurrentSticker(motionEvent);

        public void OnActionUp(StckerView stickerView, MotionEvent motionEvent)
        {
            var listener = stickerView.OnStickerOperationListener;
            if (listener == null) return;

            listener.OnStickerZoomFinished(stickerView.GetCurrentSticker());
        }
    }
}