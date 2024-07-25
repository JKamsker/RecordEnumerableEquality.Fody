using System.Collections.Generic;
using System.Linq;

namespace RecordEnumerableEquality;

public class EnumerableValueComparer<T> : IEqualityComparer<IEnumerable<T>>
{
    public static EnumerableValueComparer<T> Default { get; } = new EnumerableValueComparer<T>();

    public bool Equals(IEnumerable<T>? x, IEnumerable<T>? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (x is null)
        {
            return !y.Any();
        }

        if (y is null)
        {
            return !x.Any();
        }

        return x.SequenceEqual(y);
    }

    public int GetHashCode(IEnumerable<T>? obj)
    {
        var hashCode = 397;
        if (obj == null)
            return hashCode;
        foreach (var item in obj)
            hashCode = (hashCode * 397) ^ (item != null ? item.GetHashCode() : 0);
        return hashCode;
    }
}