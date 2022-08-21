using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views.InputMethods;
using System;
using Xamarin.Essentials;

namespace Xamarin.Android.StickerView.Utils
{
    public static class StickerUtils
    {
        public static RectF TrapToRect(float[] array)
        {
            RectF r = new RectF();
            TrapToRect(r, array);
            return r;
        }

        public static void TrapToRect(RectF r, float[] array)
        {
            r.Set(float.PositiveInfinity, float.PositiveInfinity, float.NegativeInfinity, float.NegativeInfinity);

            for (int i = 1; i < array.Length; i += 2)
            {
                float x = (float)Math.Round(array[i - 1] * 10) / 10;
                float y = (float)Math.Round(array[i] * 10) / 10;

                r.Left = (x < r.Left) ? x : r.Left;
                r.Top = (y < r.Top) ? y : r.Top;
                r.Right = (x > r.Right) ? x : r.Right;
                r.Bottom = (y > r.Bottom) ? y : r.Bottom;
            }

            r.Sort();
        }

        public static double RadianToDegrees(double angle) =>
            angle * (180.0 / Math.PI);

        /**
         * @return the number of pixels which scaledPixels corresponds to on the device.
         */
        public static float ConvertSpToPx(float scaledPixels)
        {
            var context = Platform.AppContext;
            if (context == null) return 0f;

            return scaledPixels * context.Resources.DisplayMetrics.ScaledDensity;
        }

        public static float ConvertPxToDp(float pixels)
        {
            var context = Platform.AppContext;
            if (context == null) return 0f;

            return TypedValue.ApplyDimension(
                ComplexUnitType.Sp,
                pixels,
                context.Resources.DisplayMetrics);
        }
    }
}