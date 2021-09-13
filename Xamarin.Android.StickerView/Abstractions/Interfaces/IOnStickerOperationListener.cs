using Xamarin.Android.StickerView.Models;

namespace Xamarin.Android.StickerView.Abstractions.Interfaces
{
    public interface IOnStickerOperationListener
    {
        void OnStickerAdded(Sticker sticker);

        void OnStickerClicked(Sticker sticker);

        void OnStickerDeleted(Sticker sticker);

        void OnStickerDragFinished(Sticker sticker);

        void OnStickerZoomFinished(Sticker sticker);

        void OnStickerFlipped(Sticker sticker);

        void OnStickerDoubleTapped(Sticker sticker);
    }
}