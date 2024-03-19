namespace MicroUtils.UnityFilesystem.Converters;

using System;
using System.Runtime.CompilerServices;

using MicroUtils.Functional;
using MicroUtils.UnityFilesystem.Parsers;

using Texture2DDecoder;

public static class Texture2DConverter
{
    private readonly ref struct TextureData(Texture2D texture, ReadOnlySpan<byte> rawData)
    {
        public readonly Texture2D Texture = texture;
        public readonly ReadOnlySpan<byte> RawData = rawData;

        public int Size => this.Texture.Width * this.Texture.Height;
        public int Width => this.Texture.Width;
        public int Height => this.Texture.Height;
    }

    public static bool DecodeTexture2D(this Texture2D texture, Span<byte> buffer, Func<string, Option<UnityBinaryFileReader>> getReader)
    {
        if (texture.Width < 1 || texture.Height < 1)
            return false;

        var rawData = texture.GetRawData(getReader);

        if (rawData.Length == 0)
        {
            Console.Error.WriteLine($"Could not get data for {texture.ToDictionary()["m_Name"].GetValue<string>()}");

            //if (texture.ToDictionary().Keys.Any(k => k == "image data"))
            //{
            //    var node = texture.ToDictionary()["image data"];

            //    Console.WriteLine($"image data node type? {node.GetType()}");
            //    Console.WriteLine($"image data is array? {node.IsArray()}");
            //}

            return false;
        }

        var td = new TextureData(texture, rawData);

        switch (texture.Format)
        {
            case TextureFormat.Alpha8: //test pass
                return DecodeAlpha8(td, buffer);
            case TextureFormat.ARGB4444: //test pass
                //SwapBytesForXbox(buff);
                return DecodeARGB4444(td, buffer);
            case TextureFormat.RGB24: //test pass
                return DecodeRGB24(td, buffer);
            case TextureFormat.RGBA32: //test pass
                return DecodeRGBA32(td, buffer);
            case TextureFormat.ARGB32: //test pass
                return DecodeARGB32(td, buffer);
            case TextureFormat.RGB565: //test pass
                //SwapBytesForXbox(buff);
                return DecodeRGB565(td, buffer);
            case TextureFormat.R16: //test pass
                return DecodeR16(td, buffer);
            case TextureFormat.DXT1: //test pass
                //SwapBytesForXbox(buff);
                return DecodeDXT1(td, buffer);
            case TextureFormat.DXT3:
                break;
            case TextureFormat.DXT5: //test pass
                //SwapBytesForXbox(buff);
                return DecodeDXT5(td, buffer);
            case TextureFormat.RGBA4444: //test pass
                return DecodeRGBA4444(td, buffer);
            case TextureFormat.BGRA32: //test pass
                return DecodeBGRA32(td, buffer);
            case TextureFormat.RHalf:
                return DecodeRHalf(td, buffer);
            case TextureFormat.RGHalf:
                return DecodeRGHalf(td, buffer);
            case TextureFormat.RGBAHalf: //test pass
                return DecodeRGBAHalf(td, buffer);
            case TextureFormat.RFloat:
                return DecodeRFloat(td, buffer);
            case TextureFormat.RGFloat:
                return DecodeRGFloat(td, buffer);
            case TextureFormat.RGBAFloat:
                return DecodeRGBAFloat(td, buffer);
            case TextureFormat.YUY2: //test pass
                return DecodeYUY2(td, buffer);
            case TextureFormat.RGB9e5Float: //test pass
                return DecodeRGB9e5Float(td, buffer);
            case TextureFormat.BC6H: //test pass
                return DecodeBC6H(td, buffer);
            case TextureFormat.BC7: //test pass
                return DecodeBC7(td, buffer);
            case TextureFormat.BC4: //test pass
                return DecodeBC4(td, buffer);
            case TextureFormat.BC5: //test pass
                return DecodeBC5(td, buffer);
            case TextureFormat.DXT1Crunched: //test pass
                return DecodeDXT1Crunched(td, buffer);
            case TextureFormat.DXT5Crunched: //test pass
                return DecodeDXT5Crunched(td, buffer);
            case TextureFormat.PVRTC_RGB2: //test pass
            case TextureFormat.PVRTC_RGBA2: //test pass
                return DecodePVRTC(td, buffer, true);
            case TextureFormat.PVRTC_RGB4: //test pass
            case TextureFormat.PVRTC_RGBA4: //test pass
                return DecodePVRTC(td, buffer, false);
            case TextureFormat.ETC_RGB4: //test pass
            case TextureFormat.ETC_RGB4_3DS:
                return DecodeETC1(td, buffer);
            case TextureFormat.ATC_RGB4: //test pass
                return DecodeATCRGB4(td, buffer);
            case TextureFormat.ATC_RGBA8: //test pass
                return DecodeATCRGBA8(td, buffer);
            case TextureFormat.EAC_R: //test pass
                return DecodeEACR(td, buffer);
            case TextureFormat.EAC_R_SIGNED:
                return DecodeEACRSigned(td, buffer);
            case TextureFormat.EAC_RG: //test pass
                return DecodeEACRG(td, buffer);
            case TextureFormat.EAC_RG_SIGNED:
                return DecodeEACRGSigned(td, buffer);
            case TextureFormat.ETC2_RGB: //test pass
                return DecodeETC2(td, buffer);
            case TextureFormat.ETC2_RGBA1: //test pass
                return DecodeETC2A1(td, buffer);
            case TextureFormat.ETC2_RGBA8: //test pass
            case TextureFormat.ETC_RGBA8_3DS:
                return DecodeETC2A8(td, buffer);
            case TextureFormat.ASTC_RGB_4x4: //test pass
            case TextureFormat.ASTC_RGBA_4x4: //test pass
            case TextureFormat.ASTC_HDR_4x4: //test pass
                return DecodeASTC(td, buffer, 4);
            case TextureFormat.ASTC_RGB_5x5: //test pass
            case TextureFormat.ASTC_RGBA_5x5: //test pass
            case TextureFormat.ASTC_HDR_5x5: //test pass
                return DecodeASTC(td, buffer, 5);
            case TextureFormat.ASTC_RGB_6x6: //test pass
            case TextureFormat.ASTC_RGBA_6x6: //test pass
            case TextureFormat.ASTC_HDR_6x6: //test pass
                return DecodeASTC(td, buffer, 6);
            case TextureFormat.ASTC_RGB_8x8: //test pass
            case TextureFormat.ASTC_RGBA_8x8: //test pass
            case TextureFormat.ASTC_HDR_8x8: //test pass
                return DecodeASTC(td, buffer, 8);
            case TextureFormat.ASTC_RGB_10x10: //test pass
            case TextureFormat.ASTC_RGBA_10x10: //test pass
            case TextureFormat.ASTC_HDR_10x10: //test pass
                return DecodeASTC(td, buffer, 10);
            case TextureFormat.ASTC_RGB_12x12: //test pass
            case TextureFormat.ASTC_RGBA_12x12: //test pass
            case TextureFormat.ASTC_HDR_12x12: //test pass
                return DecodeASTC(td, buffer, 12);
            case TextureFormat.RG16: //test pass
                return DecodeRG16(td, buffer);
            case TextureFormat.R8: //test pass
                return DecodeR8(td, buffer);
            case TextureFormat.ETC_RGB4Crunched: //test pass
                return DecodeETC1Crunched(td, buffer);
            case TextureFormat.ETC2_RGBA8Crunched: //test pass
                return DecodeETC2A8Crunched(td, buffer);
            case TextureFormat.RG32: //test pass
                return DecodeRG32(td, buffer);
            case TextureFormat.RGB48: //test pass
                return DecodeRGB48(td, buffer);
            case TextureFormat.RGBA64: //test pass
                return DecodeRGBA64(td, buffer);
        }

        return false;
    }

    // xbox360 only? :owlcat_suspecting:
    //private static void SwapBytesForXbox(byte[] image_data)
    //{
    //    throw new NotImplementedException();

    //    if (platform == BuildTarget.XBOX360)
    //    {
    //        for (var i = 0; i < (int)reader.BaseStream.Length / 2; i++)
    //        {
    //            var b = image_data[i * 2];
    //            image_data[i * 2] = image_data[i * 2 + 1];
    //            image_data[i * 2 + 1] = b;
    //        }
    //    }
    //}

    private static bool DecodeAlpha8(TextureData texture, Span<byte> buff)
    {
        buff.Fill(0xff);
        for (var i = 0; i < texture.Size; i++)
        {
            buff[i * 4 + 3] = texture.RawData[i];
        }
        return true;
    }

    private static bool DecodeARGB4444(TextureData texture, Span<byte> buff)
    {
        for (var i = 0; i < texture.Size; i++)
        {
            var pixelNew = buff.Slice(i * 4, 4);

            var pixelOldShort = BitConverter.ToUInt16(texture.RawData[(i * 2)..]);
            pixelNew[0] = (byte)(pixelOldShort & 0x000f);
            pixelNew[1] = (byte)((pixelOldShort & 0x00f0) >> 4);
            pixelNew[2] = (byte)((pixelOldShort & 0x0f00) >> 8);
            pixelNew[3] = (byte)((pixelOldShort & 0xf000) >> 12);

            for (var j = 0; j < 4; j++)
                pixelNew[j] = (byte)(pixelNew[j] << 4 | pixelNew[j]);

        }
        return true;
    }

    private static bool DecodeRGB24(TextureData texture, Span<byte> buff)
    {
        for (var i = 0; i < texture.Size; i++)
        {
            buff[i * 4] = texture.RawData[i * 3 + 2];
            buff[i * 4 + 1] = texture.RawData[i * 3 + 1];
            buff[i * 4 + 2] = texture.RawData[i * 3 + 0];
            buff[i * 4 + 3] = 255;
        }
        return true;
    }

    private static bool DecodeRGBA32(TextureData texture, Span<byte> buff)
    {
        for (var i = 0; i < texture.Size * 4; i += 4)
        {
            buff[i] = texture.RawData[i + 2];
            buff[i + 1] = texture.RawData[i + 1];
            buff[i + 2] = texture.RawData[i + 0];
            buff[i + 3] = texture.RawData[i + 3];
        }
        return true;
    }

    private static bool DecodeARGB32(TextureData texture, Span<byte> buff)
    {
        for (var i = 0; i < texture.Size * 4; i += 4)
        {
            buff[i] = texture.RawData[i + 3];
            buff[i + 1] = texture.RawData[i + 2];
            buff[i + 2] = texture.RawData[i + 1];
            buff[i + 3] = texture.RawData[i + 0];
        }
        return true;
    }

    private static bool DecodeRGB565(TextureData texture, Span<byte> buff)
    {
        for (var i = 0; i < texture.Size; i++)
        {
            var p = BitConverter.ToUInt16(texture.RawData[(i * 2)..]);
            buff[i * 4] = (byte)(p << 3 | p >> 2 & 7);
            buff[i * 4 + 1] = (byte)(p >> 3 & 0xfc | p >> 9 & 3);
            buff[i * 4 + 2] = (byte)(p >> 8 & 0xf8 | p >> 13);
            buff[i * 4 + 3] = 255;
        }
        return true;
    }

    private static bool DecodeR16(TextureData texture, Span<byte> buff)
    {
        for (var i = 0; i < texture.Size; i++)
        {
            buff[i * 4] = 0; //b
            buff[i * 4 + 1] = 0; //g
            buff[i * 4 + 2] = DownScaleFrom16BitTo8Bit(BitConverter.ToUInt16(texture.RawData[(i * 2)..])); //r
            buff[i * 4 + 3] = 255; //a
        }
        return true;
    }

    private static bool DecodeDXT1(TextureData texture, Span<byte> buff) =>
        TextureDecoder.DecodeDXT1(texture.RawData, texture.Width, texture.Height, buff);

    private static bool DecodeDXT5(TextureData texture, Span<byte> buff) =>
        TextureDecoder.DecodeDXT5(texture.RawData, texture.Width, texture.Height, buff);

    private static bool DecodeRGBA4444(TextureData texture, Span<byte> buff)
    {
        for (var i = 0; i < texture.Size; i++)
        {
            var pixelNew = buff.Slice(i * 4, 4);

            var pixelOldShort = BitConverter.ToUInt16(texture.RawData[(i * 2)..]);
            pixelNew[0] = (byte)((pixelOldShort & 0x00f0) >> 4);
            pixelNew[1] = (byte)((pixelOldShort & 0x0f00) >> 8);
            pixelNew[2] = (byte)((pixelOldShort & 0xf000) >> 12);
            pixelNew[3] = (byte)(pixelOldShort & 0x000f);
            for (var j = 0; j < 4; j++)
                pixelNew[j] = (byte)(pixelNew[j] << 4 | pixelNew[j]);
        }
        return true;
    }

    private static bool DecodeBGRA32(TextureData texture, Span<byte> buff)
    {
        for (var i = 0; i < texture.Size * 4; i += 4)
        {
            buff[i] = texture.RawData[i];
            buff[i + 1] = texture.RawData[i + 1];
            buff[i + 2] = texture.RawData[i + 2];
            buff[i + 3] = texture.RawData[i + 3];
        }
        return true;
    }

    private static bool DecodeRHalf(TextureData texture, Span<byte> buff)
    {
        for (var i = 0; i < texture.Size * 4; i += 4)
        {
            buff[i] = 0;
            buff[i + 1] = 0;
            buff[i + 2] = (byte)Math.Round((float)BitConverter.ToHalf(texture.RawData[(i / 2)..]) * 255.0f);
            buff[i + 3] = 255;
        }
        return true;
    }

    private static bool DecodeRGHalf(TextureData texture, Span<byte> buff)
    {
        for (var i = 0; i < texture.Size * 4; i += 4)
        {
            buff[i] = 0;
            buff[i + 1] = (byte)Math.Round((float)BitConverter.ToHalf(texture.RawData[(i + 2)..]) * 255f);
            buff[i + 2] = (byte)Math.Round((float)BitConverter.ToHalf(texture.RawData[i..]) * 255f);
            buff[i + 3] = 255;
        }
        return true;
    }

    private static bool DecodeRGBAHalf(TextureData texture, Span<byte> buff)
    {
        for (var i = 0; i < texture.Size * 4; i += 4)
        {
            buff[i] = (byte)Math.Round((float)BitConverter.ToHalf(texture.RawData[(i * 2 + 4)..]) * 255f);
            buff[i + 1] = (byte)Math.Round((float)BitConverter.ToHalf(texture.RawData[(i * 2 + 2)..]) * 255f);
            buff[i + 2] = (byte)Math.Round((float)BitConverter.ToHalf(texture.RawData[(i * 2)..]) * 255f);
            buff[i + 3] = (byte)Math.Round((float)BitConverter.ToHalf(texture.RawData[(i * 2 + 6)..]) * 255f);
        }
        return true;
    }

    private static bool DecodeRFloat(TextureData texture, Span<byte> buff)
    {
        for (var i = 0; i < texture.Size * 4; i += 4)
        {
            buff[i] = 0;
            buff[i + 1] = 0;
            buff[i + 2] = (byte)Math.Round(BitConverter.ToSingle(texture.RawData[i..]) * 255f);
            buff[i + 3] = 255;
        }
        return true;
    }

    private static bool DecodeRGFloat(TextureData texture, Span<byte> buff)
    {
        for (var i = 0; i < texture.Size * 4; i += 4)
        {
            buff[i] = 0;
            buff[i + 1] = (byte)Math.Round(BitConverter.ToSingle(texture.RawData[(i * 2 + 4)..]) * 255f);
            buff[i + 2] = (byte)Math.Round(BitConverter.ToSingle(texture.RawData[(i * 2)..]) * 255f);
            buff[i + 3] = 255;
        }
        return true;
    }

    private static bool DecodeRGBAFloat(TextureData texture, Span<byte> buff)
    {
        for (var i = 0; i < texture.Size * 4; i += 4)
        {
            buff[i] = (byte)Math.Round(BitConverter.ToSingle(texture.RawData[(i * 4 + 8)..]) * 255f);
            buff[i + 1] = (byte)Math.Round(BitConverter.ToSingle(texture.RawData[(i * 4 + 4)..]) * 255f);
            buff[i + 2] = (byte)Math.Round(BitConverter.ToSingle(texture.RawData[(i * 4)..]) * 255f);
            buff[i + 3] = (byte)Math.Round(BitConverter.ToSingle(texture.RawData[(i * 4 + 12)..]) * 255f);
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte ClampByte(int x) =>
        (byte)(byte.MaxValue < x ? byte.MaxValue : x > byte.MinValue ? x : byte.MinValue);

    private static bool DecodeYUY2(TextureData texture, Span<byte> buff)
    {
        int p = 0;
        int o = 0;
        int halfWidth = texture.Width / 2;
        for (int j = 0; j < texture.Height; j++)
        {
            for (int i = 0; i < halfWidth; ++i)
            {
                int y0 = texture.RawData[p++];
                int u0 = texture.RawData[p++];
                int y1 = texture.RawData[p++];
                int v0 = texture.RawData[p++];
                int c = y0 - 16;
                int d = u0 - 128;
                int e = v0 - 128;
                buff[o++] = ClampByte(298 * c + 516 * d + 128 >> 8);            // b
                buff[o++] = ClampByte(298 * c - 100 * d - 208 * e + 128 >> 8);  // g
                buff[o++] = ClampByte(298 * c + 409 * e + 128 >> 8);            // r
                buff[o++] = 255;
                c = y1 - 16;
                buff[o++] = ClampByte(298 * c + 516 * d + 128 >> 8);            // b
                buff[o++] = ClampByte(298 * c - 100 * d - 208 * e + 128 >> 8);  // g
                buff[o++] = ClampByte(298 * c + 409 * e + 128 >> 8);            // r
                buff[o++] = 255;
            }
        }
        return true;
    }

    private static bool DecodeRGB9e5Float(TextureData texture, Span<byte> buff)
    {
        for (var i = 0; i < texture.Size * 4; i += 4)
        {
            var n = BitConverter.ToInt32(texture.RawData[i..]);
            var scale = n >> 27 & 0x1f;
            var scalef = Math.Pow(2, scale - 24);
            var b = n >> 18 & 0x1ff;
            var g = n >> 9 & 0x1ff;
            var r = n & 0x1ff;
            buff[i] = (byte)Math.Round(b * scalef * 255f);
            buff[i + 1] = (byte)Math.Round(g * scalef * 255f);
            buff[i + 2] = (byte)Math.Round(r * scalef * 255f);
            buff[i + 3] = 255;
        }
        return true;
    }

    private static bool DecodeBC4(TextureData texture, Span<byte> buff) =>
        TextureDecoder.DecodeBC4(texture.RawData, texture.Width, texture.Height, buff);

    private static bool DecodeBC5(TextureData texture, Span<byte> buff) =>
        TextureDecoder.DecodeBC5(texture.RawData, texture.Width, texture.Height, buff);

    private static bool DecodeBC6H(TextureData texture, Span<byte> buff) =>
        TextureDecoder.DecodeBC6(texture.RawData, texture.Width, texture.Height, buff);

    private static bool DecodeBC7(TextureData texture, Span<byte> buff) =>
        TextureDecoder.DecodeBC7(texture.RawData, texture.Width, texture.Height, buff);

    private static bool DecodeDXT1Crunched(TextureData texture, Span<byte> buff)
    {
        if (UnpackCrunch(texture.RawData.ToArray(), out var result))
        {
            if (DecodeDXT1(new(texture.Texture, result), buff))
            {
                return true;
            }
        }
        return false;
    }

    private static bool DecodeDXT5Crunched(TextureData texture, Span<byte> buff)
    {
        if (UnpackCrunch(texture.RawData.ToArray(), out var result))
        {
            if (DecodeDXT5(new(texture.Texture, result), buff))
            {
                return true;
            }
        }
        return false;
    }

    private static bool DecodePVRTC(TextureData texture, Span<byte> buff, bool is2bpp) =>
        TextureDecoder.DecodePVRTC(texture.RawData, texture.Width, texture.Height, buff, is2bpp);

    private static bool DecodeETC1(TextureData texture, Span<byte> buff) =>
        TextureDecoder.DecodeETC1(texture.RawData, texture.Width, texture.Height, buff);

    private static bool DecodeATCRGB4(TextureData texture, Span<byte> buff) =>
        TextureDecoder.DecodeATCRGB4(texture.RawData, texture.Width, texture.Height, buff);

    private static bool DecodeATCRGBA8(TextureData texture, Span<byte> buff) =>
        TextureDecoder.DecodeATCRGBA8(texture.RawData, texture.Width, texture.Height, buff);

    private static bool DecodeEACR(TextureData texture, Span<byte> buff) =>
        TextureDecoder.DecodeEACR(texture.RawData, texture.Width, texture.Height, buff);

    private static bool DecodeEACRSigned(TextureData texture, Span<byte> buff) =>
        TextureDecoder.DecodeEACRSigned(texture.RawData, texture.Width, texture.Height, buff);

    private static bool DecodeEACRG(TextureData texture, Span<byte> buff) =>
        TextureDecoder.DecodeEACRG(texture.RawData, texture.Width, texture.Height, buff);

    private static bool DecodeEACRGSigned(TextureData texture, Span<byte> buff) =>
        TextureDecoder.DecodeEACRGSigned(texture.RawData, texture.Width, texture.Height, buff);

    private static bool DecodeETC2(TextureData texture, Span<byte> buff) =>
        TextureDecoder.DecodeETC2(texture.RawData, texture.Width, texture.Height, buff);

    private static bool DecodeETC2A1(TextureData texture, Span<byte> buff) =>
        TextureDecoder.DecodeETC2A1(texture.RawData, texture.Width, texture.Height, buff);

    private static bool DecodeETC2A8(TextureData texture, Span<byte> buff) =>
        TextureDecoder.DecodeETC2A8(texture.RawData, texture.Width, texture.Height, buff);

    private static bool DecodeASTC(TextureData texture, Span<byte> buff, int blocksize) =>
        TextureDecoder.DecodeASTC(texture.RawData, texture.Width, texture.Height, blocksize, blocksize, buff);

    private static bool DecodeRG16(TextureData texture, Span<byte> buff)
    {
        for (var i = 0; i < texture.Size; i++)
        {
            buff[i * 4] = 0; //B
            buff[i * 4 + 1] = texture.RawData[i * 2 + 1]; //G
            buff[i * 4 + 2] = texture.RawData[i * 2]; //R
            buff[i * 4 + 3] = 255; //A
        }
        return true;
    }

    private static bool DecodeR8(TextureData texture, Span<byte> buff)
    {
        for (var i = 0; i < texture.Size; i++)
        {
            buff[i * 4] = 0; //B
            buff[i * 4 + 1] = 0; //G
            buff[i * 4 + 2] = texture.RawData[i]; //R
            buff[i * 4 + 3] = 255; //A
        }
        return true;
    }

    private static bool DecodeETC1Crunched(TextureData texture, Span<byte> buff)
    {
        if (UnpackCrunch(texture.RawData.ToArray(), out var result))
        {
            if (DecodeETC1(new(texture.Texture, result), buff))
            {
                return true;
            }
        }
        return false;
    }

    private static bool DecodeETC2A8Crunched(TextureData texture, Span<byte> buff)
    {
        if (UnpackCrunch(texture.RawData.ToArray(), out var result))
        {
            if (DecodeETC2A8(new(texture.Texture, result), buff))
            {
                return true;
            }
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte DownScaleFrom16BitTo8Bit(ushort component) =>
        (byte)(component * 255 + 32895 >> 16);

    private static bool DecodeRG32(TextureData texture, Span<byte> buff)
    {
        for (var i = 0; i < texture.Size * 4; i += 4)
        {
            buff[i] = 0;                                                                                //b
            buff[i + 1] = DownScaleFrom16BitTo8Bit(BitConverter.ToUInt16(texture.RawData[(i + 2)..]));  //g
            buff[i + 2] = DownScaleFrom16BitTo8Bit(BitConverter.ToUInt16(texture.RawData[i..]));        //r
            buff[i + 3] = byte.MaxValue;                                                                //a
        }
        return true;
    }

    private static bool DecodeRGB48(TextureData texture, Span<byte> buff)
    {
        for (var i = 0; i < texture.Size; i++)
        {
            buff[i * 4] = DownScaleFrom16BitTo8Bit(BitConverter.ToUInt16(texture.RawData[(i * 6 + 4)..]));     //b
            buff[i * 4 + 1] = DownScaleFrom16BitTo8Bit(BitConverter.ToUInt16(texture.RawData[(i * 6 + 2)..])); //g
            buff[i * 4 + 2] = DownScaleFrom16BitTo8Bit(BitConverter.ToUInt16(texture.RawData[(i * 6)..]));     //r
            buff[i * 4 + 3] = byte.MaxValue;                                                                   //a
        }
        return true;
    }

    private static bool DecodeRGBA64(TextureData texture, Span<byte> buff)
    {
        for (var i = 0; i < texture.Size * 4; i += 4)
        {
            buff[i] = DownScaleFrom16BitTo8Bit(BitConverter.ToUInt16(texture.RawData[(i * 2 + 4)..]));     //b
            buff[i + 1] = DownScaleFrom16BitTo8Bit(BitConverter.ToUInt16(texture.RawData[(i * 2 + 2)..])); //g
            buff[i + 2] = DownScaleFrom16BitTo8Bit(BitConverter.ToUInt16(texture.RawData[(i * 2)..]));     //r
            buff[i + 3] = DownScaleFrom16BitTo8Bit(BitConverter.ToUInt16(texture.RawData[(i * 2 + 6)..])); //a
        }
        return true;
    }

    private static bool UnpackCrunch(byte[] image_data, out byte[] result)
    {
        //if (version.Major > 2017 || version.Major == 2017 && version.Minor >= 3 //2017.3 and up
        //    || m_TextureFormat == TextureFormat.ETC_RGB4Crunched
        //    || m_TextureFormat == TextureFormat.ETC2_RGBA8Crunched)
        //{
        result = TextureDecoder.UnpackUnityCrunch(image_data);
        //}
        //else
        //{
        //    result = TextureDecoder.UnpackCrunch(image_data);
        //}
        if (result != null)
        {
            return true;
        }
        return false;
    }
}
