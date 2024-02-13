namespace MicroUtils.UnityFilesystem.Parsers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MicroUtils.Functional;

using UnityDataTools.FileSystem;

public readonly record struct Sprite(SpriteSettings SpriteSettings, TypeTreeObject TypeTreeObject) : ITypeTreeObject
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
    public Option<ITypeTreeValue> TryParse(ITypeTreeValue obj, SerializedFile sf)
    {
        if (obj is TypeTreeObject tto)
        { 
            var settingsRaw =
                obj.TryGetObject()
                    .Bind(sprite => sprite["m_RD"].TryGetObject())
                    .Bind(rd => rd["settingsRaw"].TryGetValue<uint>());

            return settingsRaw.Map(sr => new Sprite(new(sr()), tto) as ITypeTreeValue);
        }
        return Option<ITypeTreeValue>.None;
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
