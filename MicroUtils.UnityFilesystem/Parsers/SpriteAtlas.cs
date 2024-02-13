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
    public readonly record struct SpriteAtlasData(
        PPtr Texture,
        PPtr AlphaTexture,
        Rectf TextureRect,
        //Vector2f TextureRectOffset,
        //Vector2f AtlasRectOffset,
        SpriteSettings SpriteSettings,
        ITypeTreeObject TypeTreeObject) : ITypeTreeObject
    {
        public ITypeTreeValue this[string key] => TypeTreeObject[key];
        public TypeTreeNode Node => TypeTreeObject.Node;
        public MicroStack<TypeTreeNode> Ancestors => TypeTreeObject.Ancestors;
        public long StartOffset => TypeTreeObject.StartOffset;
        public long EndOffset => TypeTreeObject.EndOffset;
        public IDictionary<string, ITypeTreeValue> ToDictionary() => TypeTreeObject.ToDictionary();
    }

    class SpriteAtlasDataParser : IObjectParser
    {
        public bool CanParse(TypeTreeNode node) => node.Type == "SpriteAtlasData"; //node.Type == "SpriteAtlasData";
        public Type ObjectType(TypeTreeNode node) => typeof(SpriteAtlasData);
        public Option<ITypeTreeValue> TryParse(ITypeTreeValue obj, SerializedFile sf)
        {
            if (obj is not ITypeTreeObject o)
                return Option<ITypeTreeValue>.None;

            var texture = o.TryGetField<PPtr>("texture").Map(get => get());
            var alpha = o.TryGetField<PPtr>("alphaTexture").Map(get => get());
            var textureRect = o.TryGetField<Rectf>("textureRect").Map(get => get());
            //var textureRectOffset = o.TryGetField<Vector2f>("textureRectOffset").Map(get => get());
            //var atlasRectOffset = o.TryGetField<Vector2f>("atlasRectOffset").Map(get => get());
            var spriteSettings = o.TryGetField<uint>("settingsRaw").Map(get => new SpriteSettings(get()));

            return Option.Some(
                (PPtr texture) =>
                (PPtr alpha) =>
                (Rectf textureRect) =>
                (SpriteSettings spriteSettings) =>
                    new SpriteAtlasData(texture, alpha, textureRect, spriteSettings, o) as ITypeTreeValue)
                .Apply(texture)
                .Apply(alpha)
                .Apply(textureRect)
                .Apply(spriteSettings);
        }
    }

    public readonly record struct SpriteAtlas(
        string Name,
        (string, PPtr)[] PackedSprites,
        Dictionary<(Guid, long), SpriteAtlasData> RenderDataMap,
        TypeTreeObject TypeTreeObject) : ITypeTreeObject
    {
        public ITypeTreeValue this[string key] => ((ITypeTreeObject)TypeTreeObject)[key];
        public TypeTreeNode Node => TypeTreeObject.Node;
        public MicroStack<TypeTreeNode> Ancestors => TypeTreeObject.Ancestors;
        public long StartOffset => TypeTreeObject.StartOffset;
        public long EndOffset => TypeTreeObject.EndOffset;
        public IDictionary<string, ITypeTreeValue> ToDictionary() => TypeTreeObject.ToDictionary();
    }

    class SpriteAtlasParser : IObjectParser
    {
        public bool CanParse(TypeTreeNode node) => node.Type == "SpriteAtlas";
        public Type ObjectType(TypeTreeNode _) => typeof(SpriteAtlas);
        public Option<ITypeTreeValue> TryParse(ITypeTreeValue obj, SerializedFile sf)
        {
            if (obj is not TypeTreeObject o)
                return Option<ITypeTreeValue>.None;

            var name = o.TryGetField<string>("m_Name").Map(get => get());

            var dict = o.ToDictionary();

            if (!dict.TryGetValue("m_PackedSprites", out var pss) ||
                !dict.TryGetValue("m_PackedSpriteNamesToIndex", out var sns))
                return Option<ITypeTreeValue>.None;

            var packedSprites = pss.TryGetArray<TypeTreeValue<PPtr>>().Map(arr => arr.Select(value => value.Value).ToArray());
            var spriteNames = sns.TryGetArray<TypeTreeValue<string>>().Map(arr => arr.Select(s => s.Value).ToArray());

            var sprites =
                Option.Some((string[] names) => (PPtr[] values) =>
                {
                    if (names.Length != values.Length)
                        return Option<(string, PPtr)[]>.None;

                    return Option.Some<(string, PPtr)[]>(names.Zip(values).ToArray());
                })
                .Apply(spriteNames)
                .Apply(packedSprites)
                .Bind(F.Identity);

            var renderDataMap =
                o.TryGetField<Map>("m_RenderDataMap").Bind(get => get().TryGetEntries())
                    .Map(entries => 
                    {
                        return entries.Choose(entry =>
                        {
                            var (k, v) = entry;

                            var key = k.TryGetValue<(ITypeTreeValue, ITypeTreeValue)>()
                                .Bind(get =>
                                {
                                    var (guid, fileid) = get();

                                    return Option.Some((Func<Guid> g, Func<long> fid) => (g(), fid()))
                                        .Apply2(guid.TryGetValue<Guid>(), fileid.TryGetValue<long>());
                                });

                            return Option.Some(((Guid, long) k, SpriteAtlasData v) => (k, v))
                                .Apply2(key, v is SpriteAtlasData sad ? Option.Some(sad) : Option<SpriteAtlasData>.None);
                        })
                        .ToDictionary();
                    });

            return Option.Some(
                (string name) =>
                ((string, PPtr)[] sprites) =>
                (Dictionary<(Guid, long), SpriteAtlasData> renderDataMap) =>
                    new SpriteAtlas(name, sprites, renderDataMap, o) as ITypeTreeValue)
                .Apply(name)
                .Apply(sprites)
                .Apply(renderDataMap);
        }
    }
}
