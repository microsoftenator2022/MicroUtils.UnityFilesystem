using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MicroUtils.Functional;

using UnityDataTools.FileSystem;

namespace MicroUtils.UnityFilesystem.Parsers
{
    class PairParser : IObjectParser
    {
        public bool CanParse(TypeTreeNode node) => node.Type == "pair";
        public Type ObjectType(TypeTreeNode node) => typeof((ITypeTreeValue, ITypeTreeValue));
        public Option<ITypeTreeValue> TryParse(ITypeTreeValue obj, SerializedFile sf)
        {
            if (obj is not TypeTreeObject o)
                return Option<ITypeTreeValue>.None;

            var dict = o.ToDictionary();

            if (!dict.TryGetValue("first", out var first) || !dict.TryGetValue("second", out var second))
            {
                return Option<ITypeTreeValue>.None;
            }

            return Option<ITypeTreeValue>.Some(obj.WithValue((first, second)));
        }
    }
}
