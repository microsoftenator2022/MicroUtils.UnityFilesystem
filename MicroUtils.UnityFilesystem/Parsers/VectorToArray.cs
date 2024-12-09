using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MicroUtils.Functional;
using MicroUtils.Types;

using UnityDataTools.FileSystem;

namespace MicroUtils.UnityFilesystem.Parsers
{
    class VectorToArrayParser : IObjectParser
    {
        public bool CanParse(TypeTreeNode node) => node.Type == "vector";
        public Type ObjectType(TypeTreeNode node) => typeof(TypeTreeValue<System.Array>);
        public Optional<ITypeTreeValue> TryParse(ITypeTreeValue obj, SerializedFile _)
        {
            if (obj is not ITypeTreeObject o)
                return default;

            if (!o.ToDictionary().TryGetValue("Array", out var arr))
                return default;

            return arr.TryGetValue<System.Array>().Map(get => obj.WithValue(get()) as ITypeTreeValue);
        }
    }
}
