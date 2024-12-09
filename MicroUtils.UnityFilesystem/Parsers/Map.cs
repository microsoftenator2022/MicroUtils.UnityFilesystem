using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MicroUtils;
using MicroUtils.Types;

//using MicroUtils.Linq;

using UnityDataTools.FileSystem;

namespace MicroUtils.UnityFilesystem.Parsers
{
    public readonly record struct Map(ITypeTreeObject TypeTreeObject)
    {
        public Optional<(ITypeTreeValue, ITypeTreeValue)[]> TryGetEntries() => this.TypeTreeObject["Array"].TryGetArray<(ITypeTreeValue,ITypeTreeValue)>();
    }

    class MapParser : IObjectParser
    {
        public bool CanParse(TypeTreeNode node) => node.Type == "map";
        public Type ObjectType(TypeTreeNode node) => typeof(TypeTreeValue<Map>);
        public Optional<ITypeTreeValue> TryParse(ITypeTreeValue obj, SerializedFile sf)
        {
            if (obj is not ITypeTreeObject o)
                return default;

            return Optional.Some<ITypeTreeValue>(obj.WithValue(new Map(o)));
        }
    }
}
