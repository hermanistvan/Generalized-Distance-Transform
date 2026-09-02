using System.ComponentModel;


namespace GeneralizedDistanceTransform;

public static class GeneralizedDistanceTransform
{
    public static double[,] GetTransformedImage<T>(T[,] img, int referenceValue) where T: struct
    {
        CheckType<T>();

        var length0 = img.GetLength(0);
        var length1 = img.GetLength(1);

        DistanceCache distanceCache = new DistanceCache(Math.Max(length0, length1));
        var indexerHelper = new IndexerHelper<T>(img, length0, length1);

        double[,] outimg = new double[length0, length1];
        for (int i = 0; i < length0; i++)
        {
            for (int j = 0; j < length1; j++)
            {
                int sumOverThisPixel = 0;

                for (int distanceLevel = 0; 
                        sumOverThisPixel < referenceValue 
                        && distanceLevel < distanceCache.NumberOfDistanceLevels; 
                    distanceLevel++)
                {
                    var absoluteCoords = distanceCache.GetAbsoluteCoordinatesToSpecificDistanceLevel(i, j, distanceLevel);
                    int sumOverThisDistanceLevel = 0;
                    foreach (var coord in absoluteCoords)
                    {
                        int value = Convert.ToInt32(indexerHelper.Get(coord));
                        sumOverThisDistanceLevel += value;
                    }
                    var previousSumOverThisPixel = sumOverThisPixel;
                    sumOverThisPixel += sumOverThisDistanceLevel;
                    if (sumOverThisPixel == referenceValue)
                    {
                        outimg[i, j] = distanceLevel;
                    }
                    else if (sumOverThisPixel > referenceValue)
                    {
                        double fraction_remainder = 
                                 ((double)(referenceValue - previousSumOverThisPixel))
                                / ((double)sumOverThisDistanceLevel);
                        outimg[i, j] = distanceLevel + fraction_remainder;
                    }
                }
            }
        }
        return outimg;
    }

    static void CheckType<T>() where T : struct
    {
        Type sourceType = typeof(T);
        Type targetType = typeof(int);

        // Get the converter for the source type
        TypeConverter converter = TypeDescriptor.GetConverter(sourceType);

        // Check if it can convert to the target type
        bool canConvert = converter.CanConvertTo(targetType);

        if (!canConvert)
        {
            // Include the improper type name in the exception message
            throw new ArgumentException($"Type '{sourceType.FullName}' must be a numeric value type.");
        }
    }

}

class IndexerHelper<T> where T : struct
{
    T[,] img;
    int length0;
    int length1;

    public IndexerHelper(T[,] img, int length0, int length1)
    {
        this.img = img;
        this.length0 = length0;
        this.length1 = length1;
    }

    public T Get((int, int) coords)
    {
        int i = coords.Item1;
        int j = coords.Item2;

        if (i < 0
         || i >= length0
         || j < 0
         || j >= length1)
        {
            return default(T);
        }
        else
        {
            return img[i, j];
        }
    }
}

class DistanceCache
{
    Dictionary<double, SortedSet<(int, int)>> distanceCache;

    internal DistanceCache(int n)
    {
        distanceCache = new Dictionary<double, SortedSet<(int, int)>>();
        for (int i = 0; i < n; ++i)
        {
            for (int j = 0; j <= i; ++j)
            {
                double id = i;
                double jd = j;
                double distance = Math.Sqrt(id * id + jd * jd);
                if (!distanceCache.ContainsKey(distance))
                {
                    SortedSet<(int, int)> coords = new SortedSet<(int, int)>();
                    coords.Add((i, j));
                    distanceCache.Add(distance, coords);

                }
                else
                {
                    distanceCache[distance].Add((i, j));
                }
            }
        }
    }

    internal int NumberOfDistanceLevels
    {   get
        {
            return distanceCache.Keys.Count;
        }
    }

    IEnumerable<(int, int)> getRelativeCoordinatesToSpecificDistanceLevel(int distanceLevel)
    {
        double distance = distanceCache.Keys.OrderBy(x => x).ElementAt(distanceLevel);
        var coreCoords = distanceCache[distance];
        foreach (var coreCoord in coreCoords)
        {
            yield return coreCoord;
            if (coreCoord.Item1 != 0)
            {
                yield return (-coreCoord.Item1, coreCoord.Item2);
            }
            if (coreCoord.Item2 != 0)
            {
                yield return (coreCoord.Item1, -coreCoord.Item2);
            }
            if (coreCoord.Item1 != 0 && coreCoord.Item2 != 0)
            {
                yield return (-coreCoord.Item1, -coreCoord.Item2);
            }
            if (coreCoord.Item1 != coreCoord.Item2)
            {
                yield return (coreCoord.Item2, coreCoord.Item1);
                if (coreCoord.Item2 != 0)
                {
                    yield return (-coreCoord.Item2, coreCoord.Item1);
                }
                if (coreCoord.Item1 != 0)
                {
                    yield return (coreCoord.Item2, -coreCoord.Item1);
                }
                if (coreCoord.Item1 != 0 && coreCoord.Item2 != 0)
                {
                    yield return (-coreCoord.Item2, -coreCoord.Item1);
                }
            }

        }
    }

    IEnumerable<(int, int)> combineCoordinates(int i, int j, IEnumerable<(int, int)> diffs)
    {
        foreach (var diff in diffs)
        {
            yield return (i + diff.Item1, j + diff.Item2);
        }
    }

    internal IEnumerable<(int, int)>  GetAbsoluteCoordinatesToSpecificDistanceLevel(int i, int j, int distanceLevel)
    {
        var relativeCoords = getRelativeCoordinatesToSpecificDistanceLevel(distanceLevel);
        return combineCoordinates(i, j, relativeCoords);
    }
}