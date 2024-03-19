using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MicroUtils.Functional;

using UnityDataTools.FileSystem;

namespace MicroUtils.UnityFilesystem.Parsers
{
    public readonly record struct AssetInfo(
        int PreloadIndex,
        int PreloadSize,
        PPtr Asset)
    {
        public IEnumerable<PPtr> GetAllAssetPPtrs(PPtr[] preloadTable) => preloadTable.Skip(PreloadIndex).Take(PreloadSize);
    }

    public record class AssetBundle(
        ITypeTreeObject TypeTreeObject,
        PPtr[] PreloadTable,
        Dictionary<string, AssetInfo[]> ContainerMap);

    internal class AssetBundleParser : IObjectParser
    {
        public bool CanParse(TypeTreeNode node) => node.Type == "AssetBundle";
        public Type ObjectType(TypeTreeNode node) => typeof(AssetBundle);
        public Option<ITypeTreeValue> TryParse(ITypeTreeValue obj, SerializedFile sf)
        {
            if (obj is not ITypeTreeObject o)
                return Option<ITypeTreeValue>.None;

            var dict = o.ToDictionary();

            var preloadTable = Option<PPtr[]>.None;

            if (dict.TryGetValue("m_PreloadTable", out var pt))
            {
                preloadTable = pt.TryGetArray<PPtr>();
            }
            
            var containerInfo =
                o.TryGetField<Map>("m_Container")
                    .Bind(get => get().TryGetEntries())
                    .Map(entries =>
                        entries.Choose(entry =>
                        {
                            var (k, v) = entry;

                            var key = k.TryGetValue<string>().Map(f => f());

                            if (v.NodeType() != "AssetInfo")
                            {
                                return Option<(string, AssetInfo)>.None;
                            }

                            var value =
                                v.TryGetObject()
                                    .Bind(o =>
                                    {
                                        var preloadIndex = o.TryGetField<int>("preloadIndex").Map(f => f());
                                        var preloadSize = o.TryGetField<int>("preloadSize").Map(f => f());
                                        var asset = o.TryGetField<PPtr>("asset").Map(f => f());

                                        return Option.Some((int preloadIndex) => (int preloadSize) => (PPtr asset) =>
                                                new AssetInfo(preloadIndex, preloadSize, asset))
                                            .Apply(preloadIndex)
                                            .Apply(preloadSize)
                                            .Apply(asset);
                                    });

                            //Console.WriteLine(value);

                            return Option.Some((string key) => (AssetInfo value) => (key, value))
                                .Apply(key)
                                .Apply(value);
                        })
                        .GroupBy(((string key, AssetInfo value) entry) => entry.key)
                        .Select(g =>
                        {


                            return (
                                g.Key,
                                g.Select(entry =>
                                {
                                    var (_, value) = entry;
                                    return value;
                                }).ToArray()
                            );
                        }).ToDictionary()
                    );

            var ab =
                Option.Some((PPtr[] preloadTable) => (Dictionary<string, AssetInfo[]> containerInfo) => new AssetBundle(o, preloadTable, containerInfo))
                    .Apply(preloadTable)
                    .Apply(containerInfo);

            return ab.Map(ab => obj.WithValue(ab) as ITypeTreeValue);

            //return containerInfo.Map(ci => obj.WithValue(new AssetBundle(o, ci)) as ITypeTreeValue);
        }
    }
}
