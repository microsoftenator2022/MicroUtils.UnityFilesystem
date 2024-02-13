using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using MicroUtils.Functional;

namespace MicroUtils.UnityFilesystem.Converters
{
    internal static partial class ArrayObjectConverter
    {
        [GeneratedRegex(@"^data\[\d+\]$")]
        private static partial Regex DataIndexRegex();

        public static Option<TypeTreeValue<T[]>> TryAsArray<T>(this ITypeTreeObject obj)
        {
            var dict = obj.ToDictionary();

            if (dict.Keys.Any(key => !DataIndexRegex().IsMatch(key)))
                return Option<TypeTreeValue<T[]>>.None;

            var values = dict.Values.Choose(value => value.TryGetValue<T>()).Select(get => get()!).ToArray();

            if (values.Length != dict.Count)
                return Option<TypeTreeValue<T[]>>.None;

            return Option.Some(new TypeTreeValue<T[]>(obj.Node, obj.Ancestors, obj.StartOffset, obj.EndOffset, values));
        }
    }
}
