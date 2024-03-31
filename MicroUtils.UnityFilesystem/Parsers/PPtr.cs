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

    public string GetReferencePath(Func<string, Option<SerializedFile>> getSerializedFile)
    {
        if (this == NullPtr)
        {
            Console.Error.WriteLine("Tried to dereference nullptr");
            return "";
        }

        string path = "Unknown";

        try
        {
            var thisFile = getSerializedFile(this.SerializedFilePath).DefaultWith(() => throw new KeyNotFoundException());

            //SerializedFile referenceFile;

            if (this.FileID == 0)
            {
                path = thisFile.Path;
            }
            else
            {
                // FileID = 0 is "this file", so FileID = 1 is index 0 in the external references list
                path = thisFile.ExternalReferences[this.FileID - 1].Path;

                path = new UnityReferencePath(path).ToFilePath();
            }
        }
        catch
        {

            Console.Error.WriteLine($"{this} Could not get reader for FileID {FileID} = {path}");
        }

        return path;
    }

    public Option<ITypeTreeValue> TryDereference(Func<string, Option<SerializedFile>> getSerializedFile, Func<string, Option<UnityBinaryFileReader>> getReader)
    {
        if (this == NullPtr)
        {
            Console.Error.WriteLine("Tried to dereference nullptr");
            return Option<ITypeTreeValue>.None;
        }

        string path = "Unknown";

        try
        {
            SerializedFile referenceFile;

            if (this.FileID == 0)
            {
                var thisFile = getSerializedFile(this.SerializedFilePath).DefaultWith(() => throw new KeyNotFoundException());

                path = thisFile.Path;
                referenceFile = thisFile;
            }
            else
            {
                path = GetReferencePath(getSerializedFile);

                //// FileID = 0 is "this file", so FileID = 1 is index 0 in the external references list
                //path = thisFile.ExternalReferences[this.FileID - 1].Path;

                ////var match = PPtrParser.PathRegex().Match(path);
                ////var mountPoint = match.Groups["MountPoint"].Value;
                ////var archive = match.Groups["ParentPath"].Value;
                ////var file = match.Groups["ResourcePath"].Value;

                //path = new UnityReferencePath(path).ToFilePath();

                referenceFile = getSerializedFile(path).DefaultWith(() => throw new KeyNotFoundException());
            }

            var reader = getReader(path);

            var pathID = this.PathID;

            return reader.Map(reader => TypeTreeValue.Get(referenceFile, reader, referenceFile.GetObjectByID(pathID)));
        }
        catch (Exception e) when (e is KeyNotFoundException or IndexOutOfRangeException)
        {
            Console.Error.WriteLine($"{this} Could not get reader for FileID {FileID} = {path}");
            return Option<ITypeTreeValue>.None;
        }
    }

    public override string ToString() => this == NullPtr ? $"{{ {nameof(NullPtr)} }}" :
        $"PPtr {{ {nameof(TypeName)} = \"{TypeName}\", {nameof(FileID)} = {FileID}, {nameof(PathID)} = {PathID}, SerializedFile = {SerializedFilePath} }}";
}

partial class PPtrParser : IObjectParser
{
    //[GeneratedRegex(@"^(?'MountPoint'.+?)[\\\/](?:(?'ParentPath'.+?)[\\\/])*(?'ResourcePath'.+)$")]
    //internal static partial Regex PathRegex();

    [GeneratedRegex(@"^PPtr<\$?(\w+)>$")]
    internal static partial Regex PPtrPattern();

    public bool CanParse(TypeTreeNode node) => node.Type.StartsWith("PPtr") && PPtrPattern().IsMatch(node.Type);
    public Type ObjectType(TypeTreeNode _) => typeof(PPtr);
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
