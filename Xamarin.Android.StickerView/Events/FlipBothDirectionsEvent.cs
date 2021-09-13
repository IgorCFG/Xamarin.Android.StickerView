using Xamarin.Android.StickerView.Abstractions;
using Xamarin.Android.StickerView.Abstractions.Enums;

namespace Xamarin.Android.StickerView.Events
{
    public class FlipBothDirectionsEvent : FlipEvent
    {
        protected override Flip GetFlipDirection() =>
            Flip.Vertically | Flip.Horizontally;
    }
}