using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using MicroUtils.Functional;
using MicroUtils.Types;

namespace MicroUtils.UnityFilesystem.Converters
{
    internal static partial class ArrayObjectConverter
    {
        [GeneratedRegex(@"^data\[\d+\]$")]
        private static partial Regex DataIndexRegex();

        public static Optional<TypeTreeValue<T[]>> TryAsArray<T>(this ITypeTreeObject obj)
        {
            var dict = obj.ToDictionary();

            if (dict.Keys.Any(key => !DataIndexRegex().IsMatch(key)))
                return default;

            var values = dict.Values.Choose(value => value.TryGetValue<T>()).Select(get => get()!).ToArray();

            if (values.Length != dict.Count)
                return default;

            return Optional.Some(new TypeTreeValue<T[]>(obj.Node, obj.Ancestors, obj.StartOffset, obj.EndOffset, values));
        }
    }
}
