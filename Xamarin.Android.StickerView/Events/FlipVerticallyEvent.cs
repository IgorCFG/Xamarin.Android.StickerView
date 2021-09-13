using Xamarin.Android.StickerView.Abstractions;
using Xamarin.Android.StickerView.Abstractions.Enums;

namespace Xamarin.Android.StickerView.Events
{
    public class FlipVerticallyEvent : FlipEvent
    {
        protected override Flip GetFlipDirection() =>
            Flip.Vertically;
    }
}