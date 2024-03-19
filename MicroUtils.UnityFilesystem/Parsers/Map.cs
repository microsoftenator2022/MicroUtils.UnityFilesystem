using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MicroUtils.Functional;
//using MicroUtils.Linq;
using UnityDataTools.FileSystem;

namespace MicroUtils.UnityFilesystem.Parsers
{
    public readonly record struct Map(ITypeTreeObject TypeTreeObject)
    {
        //public Option<IDictionary<TKey, TValue>> TryGetDictionary<TKey, TValue>() where TKey : notnull
        //{
        //    var dict = this.TypeTreeObject["Array"].TryGetArray<(TypeTreeValue<TKey, TValue)>().Map(v => v.ToDictionary() as IDictionary<TKey, TValue>);

        //    if (dict.IsNone)
        //        dict = this.TypeTreeObject["Array"].TryGetArray()
        //            .Map(arr =>
        //            {
        //                Console.WriteLine($"WARNING: elements not {typeof((TKey, TValue))}");
        //                return arr.Choose(x => x.TryGetValue<(TKey, TValue)>().Map(get => get())).ToDictionary() as IDictionary<TKey, TValue>;
        //            });

        //    return dict;
        //}

        public Option<(ITypeTreeValue, ITypeTreeValue)[]> TryGetEntries() => this.TypeTreeObject["Array"].TryGetArray<(ITypeTreeValue,ITypeTreeValue)>();
    }

    class MapParser : IObjectParser
    {
        public bool CanParse(TypeTreeNode node) => node.Type == "map";
        public Type ObjectType(TypeTreeNode node) => typeof(TypeTreeValue<Map>);
        public Option<ITypeTreeValue> TryParse(ITypeTreeValue obj, SerializedFile sf)
        {
            if (obj is not ITypeTreeObject o)
                return Option<ITypeTreeValue>.None;
            
            return Option.Some(obj.WithValue(new Map(o)) as ITypeTreeValue);
        }
    }
}
