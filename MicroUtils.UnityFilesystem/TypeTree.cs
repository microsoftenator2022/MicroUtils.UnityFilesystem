namespace MicroUtils.UnityFilesystem;

using System.Diagnostics;
using System.Text;

using MicroUtils;
using MicroUtils.Linq;
using MicroUtils.Functional;
using MicroUtils.UnityFilesystem.Parsers;

using UnityDataTools.FileSystem;
using System.Reflection;

public interface ITypeTreeValue
{
    TypeTreeNode Node { get; }
    MicroStack<TypeTreeNode> Ancestors { get; }
    long StartOffset { get; }
    long EndOffset { get; }
    ITypeTreeValue? this[string key] { get; }
}

public interface ITypeTreeObject : ITypeTreeValue
{
    new ITypeTreeValue this[string key] { get; }
    IDictionary<string, ITypeTreeValue> ToDictionary();
}

public record class TypeTreeValue<T>(
    TypeTreeNode Node,
    MicroStack<TypeTreeNode> Ancestors,
    long StartOffset,
    long EndOffset,
    T Value) : ITypeTreeValue
{
    public ITypeTreeValue? this[string key] => this.TryGetObject().Map(o => o[key]).MaybeValue;
    
    static IEnumerable<(int, string)> GetToStringLines(ITypeTreeValue obj, int indentLevel = 0)
    {
        yield return (indentLevel, $"{obj.Node.Name} : {obj.Node.Type} ({obj.Node.CSharpType})");

        if (obj is ITypeTreeObject tto)
        {
            yield return (indentLevel, "{");
            foreach (var n in tto.ToDictionary().Values)
                foreach (var (il, line) in GetToStringLines(n, indentLevel + 1))
                    yield return (il, $"{line}");
            yield return (indentLevel, "}");
            yield break;
        }

        var type = obj.GetType();

        if (obj.IsArray())
        {
            var arr = (obj as TypeTreeValue<System.Array>)?.Value;
            //yield return $"{arr?.Length ?? 0} items";

            if (arr?.Length is 0)
            {
                yield return (indentLevel, "[]");
                yield break;
            }

            yield return (indentLevel, "[");
         
            if (obj is TypeTreeValue<ITypeTreeValue[]> objArr)
            {
                foreach (var n in objArr.Value)
                    foreach (var (il, line) in GetToStringLines(n, indentLevel + 1))
                        yield return (il, $"{line},");
            }
            else if (arr is not null)
            {
                yield return (indentLevel + 1, $"{string.Join(", ", arr.Cast<object>().Select(o => o.ToString()))}");
            }

            yield return (indentLevel, "]");
            yield break;
        }

        if (type.IsConstructedGenericType)
        {
            var v = type.GetProperty(nameof(Value))?.GetValue(obj);

            if (v is ITypeTreeValue o)
            {
                foreach (var line in GetToStringLines(o, indentLevel + 1))
                    yield return (indentLevel, $"{line}");
            }
            else
                yield return (indentLevel + 1, $"= {v?.ToString() ?? "NULL"}");
        }
    }

    static string GetIndent(int level) => String.Concat(Enumerable.Repeat("  ", level));

    public override string ToString()
    {
        var sb = new StringBuilder();
        foreach (var (indentLevel, s) in GetToStringLines(this))
        {
            var indent = GetIndent(indentLevel);

            var lines = s.ReplaceLineEndings("\n").Split("\n");

            foreach (var line in lines)
            {
                sb.AppendLine($"{indent}{line}");
            }
        }

        return sb.ToString();
    }
}

public record class TypeTreeObject : TypeTreeValue<Dictionary<string, ITypeTreeValue>>, ITypeTreeObject
{
    readonly TypeTreeValue<Dictionary<string, ITypeTreeValue>> TypeTreeValue;

    ITypeTreeValue ITypeTreeObject.this[string key] => TypeTreeValue.Value[key];

    public TypeTreeObject(TypeTreeValue<Dictionary<string, ITypeTreeValue>> typeTreeValue) : base(
        Node: typeTreeValue.Node,
        Ancestors: typeTreeValue.Ancestors,
        StartOffset: typeTreeValue.StartOffset,
        EndOffset: typeTreeValue.EndOffset,
        Value: typeTreeValue.Value)
    {
        this.TypeTreeValue = typeTreeValue;
    }

    public IDictionary<string, ITypeTreeValue> ToDictionary() => TypeTreeValue.Value;

    public override string ToString() => base.ToString();

    public static TypeTreeObject FromValue(TypeTreeValue<Dictionary<string, ITypeTreeValue>> value) => value as TypeTreeObject ?? new(value);
}

public readonly record struct TypeTreeIgnored(
    TypeTreeNode Node,
    MicroStack<TypeTreeNode> Ancestors,
    long StartOffset,
    long EndOffset) : ITypeTreeValue
{
    public ITypeTreeValue? this[string key] => null;
}

public static class TypeTreeExtensions
{
    public static long SizeSafe(this TypeTreeNode node) => node.Size < 0 ? 0 : node.Size;

    public static string Name(this ITypeTreeValue obj) => obj.Node.Name;
    public static string NodeType(this ITypeTreeValue obj) => obj.Node.Type;
    public static long NodeSize(this ITypeTreeValue obj) => obj.Node.SizeSafe();
    public static long NextNodeOffset(this ITypeTreeValue obj) => TypeTreeUtil.AlignOffset(obj.EndOffset, obj.Node);
}

public static class TypeTreeUtil
{
    public static long AlignOffset(long offset, TypeTreeNode node)
    {
        //if (node.MetaFlags.HasFlag(TypeTreeMetaFlags.AlignBytes) ||
        //    node.MetaFlags.HasFlag(TypeTreeMetaFlags.AnyChildUsesAlignBytes))

        var alignBytes = node.MetaFlags & (TypeTreeMetaFlags.AlignBytes | TypeTreeMetaFlags.AnyChildUsesAlignBytes);

        if (alignBytes != 0)
        {
            return (offset + 3L) & (~(3L));
        }

        return offset;
    }

    public static Option<ITypeTreeValue> TryIntegralValue(
        UnityBinaryFileReader reader,
        MicroStack<TypeTreeNode> ancestors,
        long offset,
        TypeTreeNode node)
    {
        ITypeTreeValue value<T>(T value) =>
            new TypeTreeValue<T>(node, ancestors, offset, offset + node.SizeSafe(), value);

        if (!node.IsBasicType)
            return Option<ITypeTreeValue>.None;

        return node.CSharpType switch
        {
            var t when t == typeof(int) => Option.Some(value(reader.ReadInt32(offset))),
            var t when t == typeof(uint) => Option.Some(value(reader.ReadUInt32(offset))),
            var t when t == typeof(float) => Option.Some(value(reader.ReadFloat(offset))),
            var t when t == typeof(double) => Option.Some(value(reader.ReadDouble(offset))),
            var t when t == typeof(short) => Option.Some(value(reader.ReadInt16(offset))),
            var t when t == typeof(ushort) => Option.Some(value(reader.ReadUInt16(offset))),
            var t when t == typeof(long) => Option.Some(value(reader.ReadInt64(offset))),
            var t when t == typeof(ulong) => Option.Some(value(reader.ReadUInt64(offset))),
            var t when t == typeof(sbyte) => Option.Some(value(reader.ReadInt8(offset))),
            var t when t == typeof(byte) => Option.Some(value(reader.ReadUInt8(offset))),
            var t when t == typeof(bool) => Option.Some(value(reader.ReadUInt8(offset) != 0)),
            _ => Option<ITypeTreeValue>.None
        };
    }

    public static Option<ITypeTreeValue> TryString(
        UnityBinaryFileReader reader,
        MicroStack<TypeTreeNode> ancestors,
        long startOffset,
        TypeTreeNode node)
    {
        ITypeTreeValue value(long endOffset, string value) =>
            new TypeTreeValue<string>(node, ancestors, startOffset, endOffset, value);

        if (node.Type != "string")
            return Option<ITypeTreeValue>.None;

        var length = reader.ReadInt32(startOffset);
        var offset = startOffset + 4L;

        (length, var s) =
            length > 0 ?
            (length, reader.ReadString(offset, length)) :
            (0, "");

        return Option.Some(value(offset + length, s));
    }

    public static Option<ITypeTreeValue> TryArray(
        UnityBinaryFileReader reader,
        MicroStack<TypeTreeNode> ancestors,
        long startOffset,
        TypeTreeNode node,
        SerializedFile sf)
    {
        ITypeTreeValue value(long endOffset, System.Array value) =>
            new TypeTreeValue<System.Array>(node, ancestors, startOffset, endOffset, value);

        if (!node.IsArray)
            return Option<ITypeTreeValue>.None;

        var sizeNode = node.Children[0];
        if (!sizeNode.IsLeaf || sizeNode.Size != 4)
            throw new Exception("Unexpected array size node");

        var length = reader.ReadInt32(startOffset);
        var offset = startOffset + 4L;

        length = length > 0 ? length : 0;

        var dataNode = node.Children[1];

        if (dataNode.IsBasicType)
        {
            var array = System.Array.CreateInstance(dataNode.CSharpType, length);

            if (length > 0)
                reader.ReadArray(offset, length, array);

            offset += (dataNode.SizeSafe() * (long)length);

            return Option.Some(value(offset, array));
        }

        var elements = new ITypeTreeValue[length];

        if (length > 0)
        {
            for (var i = 0; i < length; i++)
            {
                elements[i] = TypeTreeValue.Get(reader, (node, ancestors), offset, dataNode, sf);

                offset = elements[i].NextNodeOffset();
            }
        }

        return Option.Some(value(offset, elements));
    }

    public static ITypeTreeObject GetObject(
        UnityBinaryFileReader reader,
        MicroStack<TypeTreeNode> ancestors,
        long startOffset,
        TypeTreeNode node,
        SerializedFile sf)
    {
        TypeTreeValue<Dictionary<string, ITypeTreeValue>> value(
            long endOffset,
            Dictionary<string, ITypeTreeValue> value) =>
            new(node, ancestors, startOffset, endOffset, value);

        var offset = startOffset;

        var children = new ITypeTreeValue[node.Children.Count];

        for (var i = 0; i < children.Length; i++)
        {
            var childNode = node.Children[i];
            var childOffset = offset;

            var child = TypeTreeValue.Get(reader, (node, ancestors), childOffset, childNode, sf);
            children[i] = child;
            offset = child.NextNodeOffset();
        }

        var properties = new Dictionary<string, ITypeTreeValue>();

        foreach (var c in children)
        {
            properties.Add(c.Name(), c);
        }

        return new TypeTreeObject(value(offset, properties));
    }
}

public static class TypeTreeValue
{
    internal static ITypeTreeValue Get(
        UnityBinaryFileReader reader,
        MicroStack<TypeTreeNode> ancestors,
        long offset,
        TypeTreeNode node,
        SerializedFile sf)
    {
        try
        {
            var result = TypeTreeUtil.TryIntegralValue(reader, ancestors, offset, node)
                .OrElseWith(() => TypeTreeUtil.TryString(reader, ancestors, offset, node))
                .OrElseWith(() => TypeTreeUtil.TryArray(reader, ancestors, offset, node, sf))
                .OrElseWith(() =>
                {
                    if (node.IsManagedReferenceRegistry)
                    {
                        if (!node.IsLeaf && !ancestors.IsEmpty)
                        {
                            return Option.Some(new TypeTreeIgnored(node, ancestors, offset, node.SizeSafe()) as ITypeTreeValue);
                        }

                        throw new Exception($"{node.Type} not implemented");
                    }

                    return Option<ITypeTreeValue>.None;
                })
                .DefaultWith(() => TypeTreeUtil.GetObject(reader, ancestors, offset, node, sf));

            result =
                ObjectParsers.Parsers.Value.TryFind(p => p.CanParse(result.Node))
                    .Bind(p => p.TryParse(result, sf))
                    .DefaultValue(result);

            return result;
        }
        catch (Exception ex)
        {
            Debugger.Break();

            throw new Exception(
                $"Exception in node {node.Type} \"{node.Name}\" at offset {offset}:\n  {ex.Message}", ex);
        }
    }

    public static ITypeTreeValue Get(
        SerializedFile sf,
        UnityBinaryFileReader reader,
        ObjectInfo objectInfo) =>
        Get(reader, MicroStack<TypeTreeNode>.Empty, objectInfo.Offset, sf.GetTypeTreeRoot(objectInfo.Id), sf);

    public static IEnumerable<ITypeTreeValue> Find(this ITypeTreeValue obj, Func<ITypeTreeValue, bool> predicate)
    {
        if (predicate(obj))
            yield return obj;

        if (obj is ITypeTreeObject tto)
        {
            foreach (var c in tto.ToDictionary().Values.SelectMany(c => c.Find(predicate)))
                yield return c;
        }
        else if (obj is TypeTreeValue<ITypeTreeValue[]> arr)
        {
            foreach (var c in arr.Value.SelectMany(c => c.Find(predicate)))
                yield return c;
        }
    }

    public static Option<Func<T>> TryGetValue<T>(this ITypeTreeValue tto)
    {
        if (tto is TypeTreeValue<T> obj)
            return Option.Some(() => obj.Value);

        var type = tto.GetType();

        var p = type.GetProperty("Value");

        if (p is not null && typeof(T).IsAssignableFrom(p.PropertyType))
        {
            return Option.Some(() => (T)p.GetValue(tto)!);
        }

        return Option<Func<T>>.None;
    }

    public static Option<T[]> TryGetArray<T>(this ITypeTreeValue tto) =>
        TryGetValue<System.Array?>(tto).Bind(get =>
        {
            var value = get();

            if (value is null)
                return Option<T[]>.None;

            if (value is T[] arr)
                return Option.Some(arr);

            if (value.Length == 0)
                return Option<T[]>.Some([]);

            if (value is ITypeTreeValue[] ttArray)
            {
                return Option.Some(
                    ttArray.Select(x =>
                    {
                        if (x is T t)
                            return t;

                        if (x is TypeTreeValue<T> tt)
                            return tt.Value;

                        else
                        {
                            throw new InvalidCastException();
                        }
                    }).ToArray());
            }

            if (value.OfType<T>().Count() != value.Length)
                return Option<T[]>.None;

            return value.ToOption().Map(arr => arr.Cast<T>().ToArray());
        });

    public static Option<ITypeTreeValue[]> TryGetArray(this ITypeTreeValue tto) => TryGetArray<ITypeTreeValue>(tto);

    public static Option<ITypeTreeObject> TryGetObject(this ITypeTreeValue tto)
    {
        if (tto is ITypeTreeObject obj)
            return Option.Some(obj);
        
        if (tto is TypeTreeValue<Dictionary<string, ITypeTreeValue>> objectValue)
            return Option<ITypeTreeObject>.Some(TypeTreeObject.FromValue(objectValue));
        
        return Option<ITypeTreeObject>.None;
    }

    public static Option<Func<T>> TryGetField<T>(this ITypeTreeObject tto, string fieldName)
    {
        if (tto.ToDictionary().TryGetValue(fieldName, out var value))
            return value.TryGetValue<T>();

        return Option<Func<T>>.None;
    }

    public static T GetValue<T>(this ITypeTreeValue tto) => tto.TryGetValue<T>().Value();

    public static bool IsObject(this ITypeTreeValue tto) => tto is ITypeTreeObject;
    public static bool IsArray(this ITypeTreeValue tto) => tto is TypeTreeValue<System.Array>;

    public static TypeTreeValue<T> WithValue<T>(this ITypeTreeValue ttValue, T value) =>
        new(ttValue.Node, ttValue.Ancestors, ttValue.StartOffset, ttValue.EndOffset, value);

    //public static T? TryGetFieldValue<T>(this TypeTreeValue<Dictionary<string, ITypeTreeObject>> tto, string fieldName) =>
    //    tto.TryGetField<T>(fieldName).DefaultValue(() => default!)();
}
