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
        length0 = img.GetLength(0);
        length1 = img.GetLength(1);
        this.img = Normalizator.NormalizeImage(img);
        distanceCache = new DistanceCache(Math.Max(length0, length1));
    }

    public double[,] GetTransformedImage(int referenceValue) 
    {
        CheckType<T>();

        double[,] outimg = new double[length0, length1];
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
                    var absoluteCoords = distanceCache.GetAbsoluteCoordinatesToSpecificDistanceLevel(i, j, distanceLevel, length0, length1);
                    double sumOverThisDistanceLevel = 0;
                    for (int k = 0; k < absoluteCoords.Count; k++)
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


    internal DistanceCache(int maxDimension)
    {
        SortedDictionary<double, List<Coordinate>> distanceCache = new SortedDictionary<double, List<Coordinate>>();
        for (int i = 0; i < maxDimension; ++i)
        {
            for (int j = 0; j <= i; ++j)
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
            }
        }
        Distances = distanceCache.Keys.ToArray();
        CoordinateLists = distanceCache.Values.ToArray();
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

    internal List<Coordinate> GetAbsoluteCoordinatesToSpecificDistanceLevel(int i, int j, int distanceLevel, int length0, int length1)
    {
        var relativeCoords = CoordinateLists[distanceLevel];
        int count = relativeCoords.Count;
        var absoluteCoords = new List<Coordinate>(count);
        for (int k = 0; k < count; k++)
        {
            Coordinate coord = new(i + relativeCoords[k].i, j + relativeCoords[k].j);
            if (coord.i >= 0
                && coord.i < length0
                && coord.j >= 0
                && coord.j < length1)
            {
                absoluteCoords.Add(coord);
            }
        }
        return absoluteCoords;
    }
}