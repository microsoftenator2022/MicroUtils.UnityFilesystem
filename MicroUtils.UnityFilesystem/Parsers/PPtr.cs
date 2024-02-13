namespace MicroUtils.UnityFilesystem.Parsers;

using System.IO;
using System.Text.RegularExpressions;

using MicroUtils;
using MicroUtils.Functional;
using MicroUtils.UnityFilesystem;

using UnityDataTools.FileSystem;

public readonly record struct PPtr(string TypeName, int FileID, long PathID, string SerializedFilePath)
{
    public static readonly PPtr NullPtr = new();

    public Option<ITypeTreeValue> TryDereference(Func<string, Option<SerializedFile>> getSerializedFile, Func<string, Option<UnityBinaryFileReader>> getReader)
    {
        if (this == NullPtr)
        {
            Console.WriteLine("Tried to dereference nullptr");
            return Option<ITypeTreeValue>.None;
        }

        string path = "Unknown";

        try
        {
            var thisFile = getSerializedFile(this.SerializedFilePath).DefaultWith(() => throw new KeyNotFoundException());
            
            SerializedFile referenceFile;

            if (this.FileID == 0)
            {
                path = thisFile.Path;
                referenceFile = thisFile;
            }
            else
            {
                // FileID = 0 is "this file", so FileID = 1 is index 0 in the external references list
                path = thisFile.ExternalReferences[this.FileID - 1].Path;
                referenceFile = getSerializedFile(path).DefaultWith(() => throw new KeyNotFoundException());
            }

            var reader = getReader(path);

            var pathID = this.PathID;

            return reader.Map(reader => TypeTreeValue.Get(referenceFile, reader, referenceFile.GetObjectByID(pathID)));
        }
        catch (Exception e) when (e is KeyNotFoundException or IndexOutOfRangeException)
        {
            Console.WriteLine($"Could not get reader for FileID {FileID} = {path}");
            return Option<ITypeTreeValue>.None;
        }
    }

    public override string ToString() => this == NullPtr ? $"{{ {nameof(NullPtr)} }}" :
        $"PPtr {{ {nameof(TypeName)} = \"{TypeName}\", {nameof(FileID)} = {FileID}, {nameof(PathID)} = {PathID}, SerializedFile = {SerializedFilePath} }}";
}

partial class PPtrParser : IObjectParser
{
    [GeneratedRegex(@"^PPtr<(\w+)>$")]
    internal static partial Regex PPtrPattern();

    public bool CanParse(TypeTreeNode node) => node.Type.StartsWith("PPtr") && PPtrPattern().IsMatch(node.Type);
    public Type ObjectType(TypeTreeNode _) => typeof(TypeTreeValue<PPtr>);
    public Option<ITypeTreeValue> TryParse(ITypeTreeValue obj, SerializedFile sf)
    {
        var match = PPtrPattern().Match(obj.NodeType());

        if (!match.Success)
            return Option<ITypeTreeValue>.None;

        var typeName = match.Groups[1].Value;

        var p = obj.TryGetObject();
        var fid = p.Bind(p => p.TryGetField<int>("m_FileID")).Map(f => f());
        var pid = p.Bind(p => p.TryGetField<long>("m_PathID")).Map(f => f());

        if (!fid.IsSome || !pid.IsSome)
            return Option<ITypeTreeValue>.None;

        var ptr = PPtr.NullPtr;

        if (pid.Value != 0)
            ptr = new PPtr(typeName, fid.Value, pid.Value, sf.Path);

        return Option.Some<ITypeTreeValue>(new TypeTreeValue<PPtr>(
            obj.Node,
            obj.Ancestors,
            obj.StartOffset,
            obj.EndOffset,
            ptr));
    }
}
