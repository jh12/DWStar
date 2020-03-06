using System.Collections.Generic;
using System.Linq;

namespace DWStar.Shared.Extensions
{
    public static class IEnumerableExtensions
    {
        public static IEnumerable<T> NotOfType<T, TCondition>(this IEnumerable<T> items)
        {
            return items.Where(i => !(i is TCondition));
        }
    }
}