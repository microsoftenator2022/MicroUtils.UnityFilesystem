namespace MicroUtils.UnityFilesystem.Parsers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MicroUtils.Functional;
using MicroUtils.Types;

using UnityDataTools.FileSystem;

public readonly record struct Sprite(
    SpriteSettings SpriteSettings,
    Optional<PPtr> TexturePtr,
    Optional<PPtr> AtlasPtr,
    Optional<(Guid, long)> RenderDataKey,
    Optional<Rectf> Rect,
    Optional<Rectf> TextureRect,
    TypeTreeObject TypeTreeObject) : ITypeTreeObject
{
    public ITypeTreeValue this[string key] => ((ITypeTreeObject)TypeTreeObject)[key];
    public TypeTreeNode Node => TypeTreeObject.Node;
    public MicroStack<TypeTreeNode> Ancestors => TypeTreeObject.Ancestors;
    public long StartOffset => TypeTreeObject.StartOffset;
    public long EndOffset => TypeTreeObject.EndOffset;
    public IDictionary<string, ITypeTreeValue> ToDictionary() => TypeTreeObject.ToDictionary();
}

class SpriteParser : IObjectParser
{
    public bool CanParse(TypeTreeNode node) => node.Type == "Sprite";
    public Type ObjectType(TypeTreeNode node) => typeof(Sprite);
    public Optional<ITypeTreeValue> TryParse(ITypeTreeValue obj, SerializedFile sf)
    {
        if (obj is TypeTreeObject tto)
        { 
            var dict = tto.ToDictionary();

            if (!dict.TryGetValue("m_RD", out var rd) || rd is not ITypeTreeObject renderData)
            {
                return default;
            }

            var settingsRaw = renderData.TryGetField<uint>("settingsRaw").Map(get => get());

            var texturePtr = renderData.TryGetField<PPtr>("texture").Map(get => get());

            var textureRect = renderData.TryGetField<Rectf>("textureRect").Map(get => get());

            var atlas = tto.TryGetField<PPtr>("m_SpriteAtlas").Map(get => get());

            Optional<(Guid, long)> rdKey = default;

            if (dict.TryGetValue("m_RenderDataKey", out var k))
            {
                rdKey = k.TryGetValue<(ITypeTreeValue, ITypeTreeValue)>()
                    .Bind(get =>
                    {
                        var (guid, fileid) = get();

                        return Optional.Some((Func<Guid> g, Func<long> fid) => (g(), fid()))
                            .Apply(guid.TryGetValue<Guid>(), fileid.TryGetValue<long>());
                    });
            }

            var rect = Optional<Rectf>.None;

            if (dict.TryGetValue("m_Rect", out var r))
            {
                rect = r.TryGetValue<Rectf>().Map(get => get());
            }

            return settingsRaw.Map(sr => new Sprite(new(sr), texturePtr, atlas, rdKey, rect, textureRect, tto) as ITypeTreeValue);
        }
        return default;
    }
}

public enum SpritePackingRotation
{
    None = 0,
    FlipHorizontal = 1,
    FlipVertical = 2,
    Rotate180 = 3,
    Rotate90 = 4
};

public enum SpritePackingMode
{
    Tight = 0,
    Rectangle
};

public enum SpriteMeshType
{
    FullRect,
    Tight
};

#pragma warning disable IDE1006 // Naming Styles
public readonly record struct SpriteSettings(uint settingsRaw)
{
    public readonly bool packed = (settingsRaw & 1) != 0; //1
    public readonly SpritePackingMode packingMode = (SpritePackingMode)(settingsRaw >> 1 & 1); //1
    public readonly SpritePackingRotation packingRotation = (SpritePackingRotation)(settingsRaw >> 2 & 0xf); //4
    public readonly SpriteMeshType meshType = (SpriteMeshType)(settingsRaw >> 6 & 1); //1
}
#pragma warning restore IDE1006 // Naming Styles
