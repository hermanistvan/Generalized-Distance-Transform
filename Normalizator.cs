using System.Numerics;


namespace GeneralizedDistanceTransform
{
    public static class Normalizator
    {
        public static T[,] NormalizeImage<T>(T[,] img)
            where T : struct, IMinMaxValue<T>, IComparisonOperators<T, T, bool>, ISubtractionOperators<T, T, T>, IDivisionOperators<T, T, T>, IMultiplyOperators<T, T, T>
        {
            if (img is null) throw new ArgumentNullException(nameof(img));

            int length0 = img.GetLength(0);
            int length1 = img.GetLength(1);

            if (length0 == 0 || length1 == 0) throw new ArgumentException("Input array must have non-zero dimensions.", nameof(img));

            T min = T.MaxValue;
            T max = T.MinValue;

            T upperValue = (T)(((IConvertible)255).ToType(typeof(T), null));
            if (min != default(T) && max != upperValue)
            {
                for (int y = 0; y < length0; y++)
                {
                    for (int x = 0; x < length1; x++)
                    {
                        T v = img[y, x];
                        if (v < min) min = v;
                        if (v > max) max = v;
                    }
                }

                T[,] normalizedImg = new T[length0, length1];

                if (!EqualityComparer<T>.Default.Equals(min, max))
                {
                    T range = max - min;
                    for (int i = 0; i < length0; i++)
                    {
                        for (int j = 0; j < length1; j++)
                        {
                            T value = img[i, j];
                            normalizedImg[i, j] = (upperValue * (value - min)) / range;
                        }
                    }
                }
                //else
                //{
                // All values equal: leave normalizedImg filled with default(T) (zeros).
                //}

                return normalizedImg;
            }
            else
            {
                return img;
            }
        }
    }
}
