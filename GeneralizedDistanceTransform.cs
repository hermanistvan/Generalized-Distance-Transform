using System.ComponentModel;
using System.Numerics;


namespace GeneralizedDistanceTransform;

public class GeneralizedDistanceTransform<T> 
    where T : struct, IMinMaxValue<T>, IComparisonOperators<T, T, bool>, ISubtractionOperators<T, T, T>, IDivisionOperators<T, T, T>, IMultiplyOperators<T, T, T>
{
    private int length0;
    private int length1;
    private DistanceCache distanceCache;
    private T[,] img;

    public GeneralizedDistanceTransform(T[,] img) 
    {
        CheckType<T>();
        length0 = img.GetLength(0);
        length1 = img.GetLength(1);
        this.img = Normalizator.NormalizeImage(img);
        distanceCache = new DistanceCache(length0, length1);
    }

    public double[,] GetTransformedImage(int referenceValue) 
    {
        double[,] outimg = new double[length0, length1];
        Coordinate[] absoluteCoords = new Coordinate[distanceCache.maxCoordinateListCount];
        for (int i = 0; i < length0; i++)
        {
            for (int j = 0; j < length1; j++)
            {
                double sumOverThisPixel = 0;

                for (int distanceLevel = 0; 
                        sumOverThisPixel < referenceValue 
                        && distanceLevel < distanceCache.Distances.Length; 
                    distanceLevel++)
                {
                    //reuse the same array for the list of the required coordinates to avoid memory and garbage collector overhead
                    byte absoluteCoordsCount = distanceCache.GetAbsoluteCoordinatesToSpecificDistanceLevel(i, j, distanceLevel, ref absoluteCoords);
                    double sumOverThisDistanceLevel = 0;
                    for (int k = 0; k < absoluteCoordsCount; k++)
                    {
                        double value = Convert.ToDouble(img[absoluteCoords[k].i, absoluteCoords[k].j]); 
                        sumOverThisDistanceLevel += value;
                    }
                    var previousSumOverThisPixel = sumOverThisPixel;
                    sumOverThisPixel += sumOverThisDistanceLevel;
                    if (sumOverThisPixel == referenceValue)
                    {
                        //outimg[i, j] = distanceLevel;
                        outimg[i, j] = distanceCache.Distances[distanceLevel];
                    }
                    else if (sumOverThisPixel > referenceValue)
                    {
                        double fraction_remainder = 
                                 ((double)(referenceValue - previousSumOverThisPixel))
                                / ((double)sumOverThisDistanceLevel);
                        //outimg[i, j] = distanceLevel + fraction_remainder;
                        double distanceBase = distanceLevel > 0 ?
                                                distanceCache.Distances[distanceLevel - 1]:
                                                0;
                        double distancefractionRemainder = (distanceCache.Distances[distanceLevel]
                                                           - distanceBase)
                                                           * fraction_remainder;
                        outimg[i, j] = distanceBase + distancefractionRemainder;
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

internal record Coordinate(int i, int j);

class DistanceCache
{
    internal double[] Distances { get; }
    internal IReadOnlyList<Coordinate>[] CoordinateLists { get; }
    internal int maxCoordinateListCount { get; }

    private int length0;
    private int length1;

    internal DistanceCache(int length0, int length1)
    {
        this.length0 = length0;
        this.length1 = length1;
        int maxDimension = Math.Max(length0, length1);
        int mindimension = Math.Min(length0, length1);
        int maxListLength = 0;
        SortedDictionary<double, List<Coordinate>> distanceCache = new SortedDictionary<double, List<Coordinate>>();
        for (int i = 0; i < maxDimension; ++i)
        {
            for (int j = 0; j <= i && j < mindimension; ++j)
            {
                double id = i;
                double jd = j;
                double distance = Math.Sqrt(id * id + jd * jd);

                if (!distanceCache.TryGetValue(distance, out var currentSet))
                {
                    currentSet = new List<Coordinate>();
                    distanceCache.Add(distance, currentSet);
                }
                GenerateAndAddCoordinateVariants(i, j, currentSet);
                int count = currentSet.Count;
                if (currentSet.Count > maxListLength) maxListLength = count;
            }
        }
        Distances = distanceCache.Keys.ToArray();
        CoordinateLists = distanceCache.Values.ToArray();
        maxCoordinateListCount = maxListLength; ;
    }

    void GenerateAndAddCoordinateVariants(int i, int j, List<Coordinate> currentSet)
    {
        bool equal = i == j;
        bool hasAnyZero = i == 0 || j == 0;
        if (!equal && !hasAnyZero)
        {
            currentSet.Add(new Coordinate(i, j));
            currentSet.Add(new Coordinate(-i, j));
            currentSet.Add(new Coordinate(i, -j));
            currentSet.Add(new Coordinate(-i, -j));

            currentSet.Add(new Coordinate(j, i));
            currentSet.Add(new Coordinate(-j, i));
            currentSet.Add(new Coordinate(j, -i));
            currentSet.Add(new Coordinate(-j, -i));
        }
        else if (equal && !hasAnyZero)
        {
            currentSet.Add(new Coordinate(i, j));
            currentSet.Add(new Coordinate(-i, j));
            currentSet.Add(new Coordinate(i, -j));
            currentSet.Add(new Coordinate(-i, -j));
        }
        else if (!equal && hasAnyZero)
        {
            int value = i + j; //only either of them is 0 so the sum will yield the non-zero element
            currentSet.Add(new Coordinate(0, value));
            currentSet.Add(new Coordinate(0, -value));
            currentSet.Add(new Coordinate(value, 0));
            currentSet.Add(new Coordinate(-value, 0));
        }
        else //if (equal && hasAnyZero) //both zero
        {
            currentSet.Add(new Coordinate(i, j)); //[0, 0]
        }
    }

    internal byte GetAbsoluteCoordinatesToSpecificDistanceLevel(int i, int j, int distanceLevel, ref Coordinate[] absoluteCoordinates)
    {
        var relativeCoords = CoordinateLists[distanceLevel];
        int relativeCoordsCount = relativeCoords.Count;
        byte absoluteCoordsCount = 0;
        for (int k = 0; k < relativeCoordsCount; k++)
        {
            Coordinate coord = new(i + relativeCoords[k].i, j + relativeCoords[k].j);
            if (coord.i >= 0
                && coord.i < length0
                && coord.j >= 0
                && coord.j < length1)
            {
                //reuse the same array for the list of the required coordinates to avoid memory and garbage collector overhead
                absoluteCoordinates[absoluteCoordsCount] = coord;
                absoluteCoordsCount++;
            }
        }
        return absoluteCoordsCount;
    }
}