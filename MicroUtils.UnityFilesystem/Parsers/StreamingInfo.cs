namespace MicroUtils.UnityFilesystem.Parsers;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using MicroUtils.Functional;
using MicroUtils.UnityFilesystem;

using UnityDataTools.FileSystem;

public readonly record struct StreamingInfo(ulong Offset, ulong Size, string RawPath)
{
    public string GetReferencePath() => new UnityReferencePath(RawPath).ToFilePath();

    public Option<byte[]> TryGetData()
    {
        var size = this.Size;
        UnityBinaryFileReader? reader = null;
        Option<UnityBinaryFileReader> getReader(string path) => Option.Some(new UnityBinaryFileReader(path, (int)size));
        
        var result = TryGetData(getReader);

        reader?.Dispose();

        return result;
    }

    public Option<byte[]> TryGetData(Func<string, Option<UnityBinaryFileReader>> getReader)
    {

        var path = this.GetReferencePath();
        #if DEBUG
        Console.WriteLine($"Get stream from file: {path}, offset = {this.Offset}, size = {this.Size}");
        #endif

        try
        {
            var reader = getReader(path).DefaultWith(() => throw new KeyNotFoundException());

            var data = new byte[this.Size];

            reader.ReadArray((long)this.Offset, (int)this.Size, data);

            return Option.Some(data);
        }
        catch (Exception e)
        {
            if (e is KeyNotFoundException)
                Console.Error.WriteLine($"Could not get {path} reader for {this}");
            else
                Console.Error.WriteLine(e.ToString());

            Console.WriteLine("Falling back to UnityFileReader");

            using var reader = new UnityFileReader(path, (int)this.Size);
            
            var data = new byte[this.Size];

            reader.ReadArray((long)this.Offset, (int)this.Size, data);

            return Option.Some(data);
        }
    }
}

partial class StreamingInfoParser : IObjectParser
{
    public bool CanParse(TypeTreeNode node) => node.Type == "StreamingInfo" || node.Type == "StreamedResource";
    public Type ObjectType(TypeTreeNode _) => typeof(TypeTreeValue<StreamingInfo>);
    public Option<ITypeTreeValue> TryParse(ITypeTreeValue obj, SerializedFile sf)
    {
        if (!CanParse(obj.Node))
            return Option<ITypeTreeValue>.None;

        var si = obj.TryGetObject();

        var (path, offset, size) = obj.Node.Type switch
        {
            "StreamingInfo" => 
                (si
                  .Bind(si => si.TryGetField<string?>("path"))
                    .Bind(path => 
                    {
                        var s = path();
                        if (string.IsNullOrEmpty(s))
                            return Option<string>.None;

                        return Option.Some(s);
                    }),
                    si.Bind(si => si.TryGetField<ulong>("offset")).Map(offset => offset()),
                    si.Bind(si => si.TryGetField<uint>("size")).Map(size => (ulong)(size()))),
            "StreamedResource" =>
                (si
                  .Bind(si => si.TryGetField<string?>("m_Source"))
                    .Bind(path =>
                    {
                        var s = path();
                        if (string.IsNullOrEmpty(s))
                            return Option<string>.None;

                        return Option.Some(s);
                    }),
                    si.Bind(si => si.TryGetField<ulong>("m_Offset")).Map(offset => offset()),
                    si.Bind(si => si.TryGetField<ulong>("m_Size")).Map(size => size())),
            _ => (Option<string>.None, Option<ulong>.None, Option<ulong>.None)
        };

        if (path.IsNone || offset.IsNone || size.IsNone)
            return Option<ITypeTreeValue>.None;

        var node = obj.Node;

        return Option.Some<ITypeTreeValue>(new TypeTreeValue<StreamingInfo>(
            node,
            obj.Ancestors,
            obj.StartOffset,
            obj.EndOffset,
            new(offset.Value, size.Value, path.Value)));
    }
}
