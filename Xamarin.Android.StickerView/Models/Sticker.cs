using Android.Graphics;
using Android.Graphics.Drawables;
using Xamarin.Android.StickerView.Utils;
using System;

namespace Xamarin.Android.StickerView.Models
{
    public abstract class Sticker
    {
        #region Fields

        private readonly float[] _matrixValues = new float[9];
        private readonly float[] _unrotatedWrapperCorner = new float[8];
        private readonly float[] _unrotatedPoint = new float[2];
        private readonly float[] _boundPoints = new float[8];
        private readonly float[] _mappedBounds = new float[8];
        private readonly RectF _trappedRect = new RectF();
        private readonly Matrix _matrix = new Matrix();

        #endregion

        #region Properties

        public bool IsFlippedHorizontally { get; set; }

        public bool IsFlippedVertically { get; set; }

        public Matrix GetMatrix() => _matrix;

        public Sticker SetMatrix(Matrix matrix)
        {
            _matrix.Set(matrix);
            return this;
        }

        #endregion

        #region Abstract Methods

        public abstract void Draw(Canvas canvas);

        public abstract int GetWidth();

        public abstract int GetHeight();

        public abstract Sticker SetDrawable(Drawable drawable);

        public abstract Drawable GetDrawable();

        public abstract Sticker SetAlpha(int alpha);

        #endregion

        #region Virtual Methods

        public virtual void Release()
        {
        }

        #endregion

        #region Public Methods

        public float[] GetBoundPoints()
        {
            float[] points = new float[8];
            GetBoundPoints(points);
            return points;
        }

        public void GetBoundPoints(float[] points)
        {
            if (!IsFlippedHorizontally)
            {
                if (!IsFlippedVertically)
                {
                    points[0] = 0f;
                    points[1] = 0f;
                    points[2] = GetWidth();
                    points[3] = 0f;
                    points[4] = 0f;
                    points[5] = GetHeight();
                    points[6] = GetWidth();
                    points[7] = GetHeight();
                }
                else
                {
                    points[0] = 0f;
                    points[1] = GetHeight();
                    points[2] = GetWidth();
                    points[3] = GetHeight();
                    points[4] = 0f;
                    points[5] = 0f;
                    points[6] = GetWidth();
                    points[7] = 0f;
                }
            }
            else
            {
                if (!IsFlippedVertically)
                {
                    points[0] = GetWidth();
                    points[1] = 0f;
                    points[2] = 0f;
                    points[3] = 0f;
                    points[4] = GetWidth();
                    points[5] = GetHeight();
                    points[6] = 0f;
                    points[7] = GetHeight();
                }
                else
                {
                    points[0] = GetWidth();
                    points[1] = GetHeight();
                    points[2] = 0f;
                    points[3] = GetHeight();
                    points[4] = GetWidth();
                    points[5] = 0f;
                    points[6] = 0f;
                    points[7] = 0f;
                }
            }
        }

        public float[] GetMappedBoundPoints()
        {
            float[] dst = new float[8];
            GetMappedPoints(dst, GetBoundPoints());
            return dst;
        }

        public float[] GetMappedPoints(float[] src)
        {
            float[] dst = new float[src.Length];
            _matrix.MapPoints(dst, src);
            return dst;
        }

        public void GetMappedPoints(float[] dst, float[] src)
        {
            _matrix.MapPoints(dst, src);
        }

        public RectF GetBound()
        {
            RectF bound = new RectF();
            GetBound(bound);
            return bound;
        }

        public void GetBound(RectF dst)
        {
            dst.Set(0, 0, GetWidth(), GetHeight());
        }

        public RectF getMappedBound()
        {
            RectF dst = new RectF();
            GetMappedBound(dst, GetBound());
            return dst;
        }

        public void GetMappedBound(RectF dst, RectF bound)
        {
            _matrix.MapRect(dst, bound);
        }

        public PointF GetCenterPoint()
        {
            PointF center = new PointF();
            GetCenterPoint(center);
            return center;
        }

        public void GetCenterPoint(PointF dst)
        {
            dst.Set(GetWidth() * 1f / 2, GetHeight() * 1f / 2);
        }

        public PointF GetMappedCenterPoint()
        {
            PointF pointF = GetCenterPoint();
            GetMappedCenterPoint(pointF, new float[2], new float[2]);
            return pointF;
        }

        public void GetMappedCenterPoint(PointF dst, float[] mappedPoints, float[] src)
        {
            GetCenterPoint(dst);
            src[0] = dst.X;
            src[1] = dst.Y;
            GetMappedPoints(mappedPoints, src);
            dst.Set(mappedPoints[0], mappedPoints[1]);
        }

        public float GetCurrentScale() =>
            GetMatrixScale(_matrix);

        public float GetCurrentHeight() =>
            GetMatrixScale(_matrix) * GetHeight();

        public float GetCurrentWidth() =>
            GetMatrixScale(_matrix) * GetWidth();

        /**
         * This method calculates scale value for given Matrix object.
         */
        public float GetMatrixScale(Matrix matrix) =>
            (float)Math.Sqrt(Math.Pow(GetMatrixValue(matrix, Matrix.MscaleX), 2) + Math.Pow(GetMatrixValue(matrix, Matrix.MskewY), 2));

        /**
         * @return - current image rotation angle.
         */
        public float GetCurrentAngle() =>
            GetMatrixAngle(_matrix);

        /**
         * This method calculates rotation angle for given Matrix object.
         */
        public float GetMatrixAngle(Matrix matrix) =>
            (float)StickerUtils.RadianToDegrees(-Math.Atan2(GetMatrixValue(matrix, Matrix.MskewX), GetMatrixValue(matrix, Matrix.MscaleX)));

        public float GetMatrixValue(Matrix matrix, int valueIndex)
        {
            matrix.GetValues(_matrixValues);
            return _matrixValues[valueIndex];
        }

        public bool Contains(float x, float y)
        {
            return Contains(new float[] { x, y });
        }

        public bool Contains(float[] point)
        {
            Matrix tempMatrix = new Matrix();
            tempMatrix.SetRotate(-GetCurrentAngle());
            GetBoundPoints(_boundPoints);
            GetMappedPoints(_mappedBounds, _boundPoints);
            tempMatrix.MapPoints(_unrotatedWrapperCorner, _mappedBounds);
            tempMatrix.MapPoints(_unrotatedPoint, point);

            StickerUtils.TrapToRect(_trappedRect, _unrotatedWrapperCorner);
            return _trappedRect.Contains(_unrotatedPoint[0], _unrotatedPoint[1]);
        }

        #endregion
    }
}