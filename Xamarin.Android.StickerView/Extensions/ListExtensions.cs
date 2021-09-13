using System.Collections.Generic;

namespace Xamarin.Android.StickerView.Extensions
{
    public static class ListExtensions
    {
        public static void AddAt<T>(this List<T> list, int index, T value)
        {
            list[index] = value;
        }

        public static void Swap<T>(this List<T> list, int fromPos, int toPos)
        {
            var tmp = list[fromPos];

            list[fromPos] = list[toPos];
            list[toPos] = tmp;
        }
    }
}