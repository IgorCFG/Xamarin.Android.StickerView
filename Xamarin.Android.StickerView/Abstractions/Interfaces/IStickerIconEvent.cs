using Android.Views;
using StckerView = Xamarin.Android.StickerView.Views.StickerView;

namespace Xamarin.Android.StickerView.Abstractions.Interfaces
{
    public interface IStickerIconEvent
    {
        void OnActionDown(StckerView stickerView, MotionEvent motionEvent);
        void OnActionMove(StckerView stickerView, MotionEvent motionEvent);
        void OnActionUp(StckerView stickerView, MotionEvent motionEvent);
    }
}