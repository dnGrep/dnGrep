using System;
using System.Linq;
using System.Collections.Generic;

namespace NetDiff
{
    internal static class Extension
    {
        public static IEnumerable<Tuple<TSource, TSource>> MakePairsWithNext<TSource>(this IEnumerable<TSource> source)
        {
            using (var enumerator = source.GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    var previous = enumerator.Current;
                    while (enumerator.MoveNext())
                    {
                        var current = enumerator.Current;

                        yield return new Tuple<TSource, TSource>(previous, current);

                        previous = current;
                    }
                }
            }
        }

        public static TSource FindMin<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
        {
            return source.FirstOrDefault(c => selector(c).Equals(source.Min(selector)));
        }

        public static TSource FindMax<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
        {
            return source.FirstOrDefault(c => selector(c).Equals(source.Max(selector)));
        }

        public static IEnumerable<TSource> FindMins<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
        {
            return source.ToLookup(selector).FindMin(n => n.Key);
        }

        public static IEnumerable<TSource> FindMaxs<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
        {
            return source.ToLookup(selector).FindMax(n => n.Key);
        }
    }
}
