using System;
using System.Linq;
using System.Collections.Generic;

namespace HelloWold;

public static class Program
{
    public static void Main()
    {
        int[,] img = new int[,]
        {
            {0, 0, 0, 1, 0, 6, 0, 0},
            {0, 1, 0, 0, 0, 8, 0, 0},
            {0, 0, 0, 0, 2, 5, 0, 0},
            {0, 0, 0, 1, 4, 2, 1, 1},
            {0, 0, 0, 1, 5, 0, 0, 0},
            {7, 8, 7, 6, 2, 0, 0, 0},
            {2, 1, 0, 1, 0, 0, 0, 0},
            {0, 0, 0, 1, 0, 0, 1, 0},
        };
        int referenceValue = 7;

        var length0 = img.GetLength(0);
        var length1 = img.GetLength(1);

        InitDistanceCache(Math.Max(length0, length1));
        var indexerHelper = new IndexerHelper<int>(img, length0, length1);

        double[,] outimg = new double[length0, length1];
        for (int i = 0; i < length0; i++)
        {
            for (int j = 0; j < length1; j++)
            {
                int sumOverThisPixel = 0;

                for (int distanceLevel = 0; sumOverThisPixel < referenceValue && distanceLevel < distanceCache.Keys.Count; distanceLevel++)
                {
                    var relativeCoords = getCoordinatesToSpecificDistanceLevel(distanceLevel);
                    var absoluteCoords = CombineCoords(i, j, relativeCoords);
                    int sumOverThisDistanceLevel = 0;
                    foreach (var coord in absoluteCoords)
                    {
                        var value = indexerHelper.Get(coord);
                        sumOverThisDistanceLevel += value;
                    }
                    var tempsum = sumOverThisPixel + sumOverThisDistanceLevel;
                    if (tempsum < referenceValue)
                    {
                        sumOverThisPixel = tempsum;
                    }
                    else if (tempsum == referenceValue)
                    {
                        outimg[i, j] = distanceLevel;
                    }
                    else if (tempsum > referenceValue)
                    {
                        double fraction_remainder = 
                                 ((double)(referenceValue - sumOverThisPixel))
                                / ((double)sumOverThisDistanceLevel);
                        outimg[i, j] = distanceLevel + fraction_remainder;
                    }
                }
            }
        }
        for (int i = 0; i < outimg.GetLength(0); i++)
        {
            for (int j = 0; j < outimg.GetLength(1); j++)
            {
                 Console.Write(Math.Round(outimg[i,j]) + ", ");
            } 
            Console.WriteLine();
        } 
    }

    static Dictionary<double, SortedSet<(int, int)>> distanceCache;

    static void InitDistanceCache(int n)
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

    static IEnumerable<(int, int)> getCoordinatesToSpecificDistanceLevel(int distanceLevel)
    {
        double distance = distanceCache.Keys.OrderBy(x=>x).ElementAt(distanceLevel);
        var coreCoords = distanceCache[distance];
        foreach (var coreCoord in coreCoords)
        {
            yield return coreCoord;
            yield return (-coreCoord.Item1, coreCoord.Item2);
            yield return (coreCoord.Item1, -coreCoord.Item2);
            yield return (-coreCoord.Item1, -coreCoord.Item2);

            if (coreCoord.Item1 != coreCoord.Item2)
            {
                yield return (coreCoord.Item2, coreCoord.Item1);
                yield return (-coreCoord.Item2, coreCoord.Item1);
                yield return (coreCoord.Item2, -coreCoord.Item1);
                yield return (-coreCoord.Item2, -coreCoord.Item1);
            }

        }
    }

    static IEnumerable<(int, int)> CombineCoords(int i, int j, IEnumerable<(int, int)> diffs)
    {
        foreach (var diff in diffs)
        {
            yield return (i + diff.Item1, j + diff.Item2);
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

class TriangularArray<T>
{
    T[] innerArray;
    int size;

    public TriangularArray(int n)
    {
        size = n;
        int m = translateIndexBase(n);
        innerArray = new T[m];
    }

    private int translateIndexBase(int @base)
    {
        return @base * (@base + 1) / 2;
    }

    private int translateCoordinates(int i, int j)
    {
        int @base = i > j ? i : j;
        int offset = i < j ? i : j;
        if (@base >= size || offset < 0) throw new IndexOutOfRangeException();
        int innerIndex = translateIndexBase(@base) + offset;
        return innerIndex;
    }

    public T this[int i, int j]
    {
        get
        {
            int innerIndex = translateCoordinates(i, j);
            return innerArray[innerIndex];
        }
        set
        {
            int innerIndex = translateCoordinates(i, j);
            innerArray[innerIndex] = value;
        }
    }
}
