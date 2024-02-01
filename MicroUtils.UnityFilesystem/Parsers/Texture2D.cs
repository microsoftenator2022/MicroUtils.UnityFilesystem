﻿namespace MicroUtils.UnityFilesystem.Parsers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MicroUtils.Functional;

using UnityDataTools.FileSystem;

public record class Texture2D(int Width, int Height, TextureFormat Format, TypeTreeObject TypeTreeObject) : ITypeTreeObject
{
    public ITypeTreeValue this[string key] => ((ITypeTreeObject)TypeTreeObject)[key];

    public TypeTreeNode Node => TypeTreeObject.Node;

    public MicroStack<TypeTreeNode> Ancestors => TypeTreeObject.Ancestors;

    public long StartOffset => TypeTreeObject.StartOffset;

    public long EndOffset => TypeTreeObject.EndOffset;

    public byte[] GetRawData(Func<string, Option<UnityBinaryFileReader>> getReader)
    {
        var imageData = this["image data"].TryGetArray<byte>().DefaultValue([]);

        if (imageData.Length > 0)
            return imageData;

        return this["m_StreamData"].TryGetValue<StreamingInfo>().Bind(get => get().TryGetData(getReader)).DefaultValue([]);
    }

    public IDictionary<string, ITypeTreeValue> ToDictionary() => TypeTreeObject.ToDictionary();
}

partial class Texture2DParser : IObjectParser
{
    public bool CanParse(TypeTreeNode node) => node.Type == "Texture2D";
    public Type ObjectType(TypeTreeNode node) => typeof(Texture2D);

    Option<Texture2D> TryCreate(TypeTreeObject tto)
    {
        var width = tto.TryGetField<int>("m_Width").Map(get => get());
        var height = tto.TryGetField<int>("m_Height").Map(get => get());
        var format = tto.TryGetField<int>("m_TextureFormat").Map(get => (TextureFormat)get());

        var create = Option.Some(new Unit()).Map<Unit, Func<int, int, Func<TextureFormat, Texture2D>>>(_ =>
            (width, height) =>
            format => new Texture2D(width, height, format, tto));

        return create
            .Apply2(width, height)
            .Apply(format);
    }

    public Option<ITypeTreeValue> TryParse(ITypeTreeValue obj, SerializedFile sf)
    {
        return obj.TryGetObject()
            .Bind(o => (o as TypeTreeObject).ToOption())
            .Bind(TryCreate)
            .Map<Texture2D, ITypeTreeValue>(t2d => t2d);
    }
}

public enum TextureFormat
{
    Alpha8 = 1,
    ARGB4444,
    RGB24,
    RGBA32,
    ARGB32,
    ARGBFloat,
    RGB565,
    BGR24,
    R16,
    DXT1,
    DXT3,
    DXT5,
    RGBA4444,
    BGRA32,
    RHalf,
    RGHalf,
    RGBAHalf,
    RFloat,
    RGFloat,
    RGBAFloat,
    YUY2,
    RGB9e5Float,
    RGBFloat,
    BC6H,
    BC7,
    BC4,
    BC5,
    DXT1Crunched,
    DXT5Crunched,
    PVRTC_RGB2,
    PVRTC_RGBA2,
    PVRTC_RGB4,
    PVRTC_RGBA4,
    ETC_RGB4,
    ATC_RGB4,
    ATC_RGBA8,
    EAC_R = 41,
    EAC_R_SIGNED,
    EAC_RG,
    EAC_RG_SIGNED,
    ETC2_RGB,
    ETC2_RGBA1,
    ETC2_RGBA8,
    ASTC_RGB_4x4,
    ASTC_RGB_5x5,
    ASTC_RGB_6x6,
    ASTC_RGB_8x8,
    ASTC_RGB_10x10,
    ASTC_RGB_12x12,
    ASTC_RGBA_4x4,
    ASTC_RGBA_5x5,
    ASTC_RGBA_6x6,
    ASTC_RGBA_8x8,
    ASTC_RGBA_10x10,
    ASTC_RGBA_12x12,
    ETC_RGB4_3DS,
    ETC_RGBA8_3DS,
    RG16,
    R8,
    ETC_RGB4Crunched,
    ETC2_RGBA8Crunched,
    ASTC_HDR_4x4,
    ASTC_HDR_5x5,
    ASTC_HDR_6x6,
    ASTC_HDR_8x8,
    ASTC_HDR_10x10,
    ASTC_HDR_12x12,
    RG32,
    RGB48,
    RGBA64
}
