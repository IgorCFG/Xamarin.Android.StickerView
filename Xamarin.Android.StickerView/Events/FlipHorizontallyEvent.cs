using Xamarin.Android.StickerView.Abstractions;
using Xamarin.Android.StickerView.Abstractions.Enums;

namespace Xamarin.Android.StickerView.Events
{
    public class FlipHorizontallyEvent : FlipEvent
    {
        protected override Flip GetFlipDirection() =>
            Flip.Horizontally;
    }
}