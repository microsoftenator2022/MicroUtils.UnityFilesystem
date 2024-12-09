namespace MicroUtils.UnityFilesystem.Parsers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MicroUtils.Functional;
using MicroUtils.Types;

using MicroUtils.UnityFilesystem.Converters;

using UnityDataTools.FileSystem;

class GUIDParser : IObjectParser
{
    public bool CanParse(TypeTreeNode node) => node.Type == "GUID";
    public Type ObjectType(TypeTreeNode node) => typeof(TypeTreeValue<Guid>);
    public Optional<ITypeTreeValue> TryParse(ITypeTreeValue obj, SerializedFile _)
    {
        if (obj is ITypeTreeObject o && o.TryAsArray<uint>().Value is TypeTreeValue<uint[]> arr)
        {
            var buf = new byte[16];
            Buffer.BlockCopy(arr.Value, 0, buf, 0, 16);
            var guid = new Guid(buf);
            return Optional.Some(new TypeTreeValue<Guid>(obj.Node, obj.Ancestors, obj.StartOffset, obj.EndOffset, guid) as ITypeTreeValue);
        }

        return default;
    }
}
