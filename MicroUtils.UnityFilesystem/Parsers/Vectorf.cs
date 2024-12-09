using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MicroUtils.Functional;
using MicroUtils.Types;

using UnityDataTools.FileSystem;

namespace MicroUtils.UnityFilesystem.Parsers
{
#pragma warning disable IDE1006 // Naming Styles
    public readonly record struct Vector2f(float x, float y);
    public readonly record struct Vector3f(float x, float y, float z);
    public readonly record struct Vector4f(float x, float y, float z, float w);
#pragma warning restore IDE1006 // Naming Styles

    class VectorfParser : IObjectParser
    {
        public bool CanParse(TypeTreeNode node) => node.Type is nameof(Vector2f) or nameof(Vector3f) or nameof(Vector4f);
        public Type ObjectType(TypeTreeNode node) => node.Type switch
        {
            nameof(Vector2f) => typeof(TypeTreeValue<Vector2f>),
            nameof(Vector3f) => typeof(TypeTreeValue<Vector3f>),
            nameof(Vector4f) => typeof(TypeTreeValue<Vector4f>),
            _ => throw new NotSupportedException()
        };

        public Optional<ITypeTreeValue> TryParse(ITypeTreeValue obj, SerializedFile _)
        {
            static Optional<float> tryGetFloat(ITypeTreeObject obj, string name)
            {
                return obj.TryGetField<float>(name).Map(get => get());
            }

            var x = obj.TryGetObject().Bind(o => tryGetFloat(o, "x"));
            var y = obj.TryGetObject().Bind(o => tryGetFloat(o, "y"));
            var z = obj.TryGetObject().Bind(o => tryGetFloat(o, "z"));
            var w = obj.TryGetObject().Bind(o => tryGetFloat(o, "w"));

            return obj.TryGetObject()
                .Bind(o => o.Node.Type switch
                {
                    nameof(Vector2f) =>
                        Optional.Some<Func<float, Func<float, ITypeTreeValue>>>(
                            x => y => obj.WithValue(new Vector2f(x, y)))
                            .Apply(x)
                            .Apply(y),

                    nameof(Vector3f) =>
                        Optional.Some<Func<float, Func<float, Func<float, ITypeTreeValue>>>>(
                            x => y => z => obj.WithValue(new Vector3f(x, y, z)))
                            .Apply(x)
                            .Apply(y)
                            .Apply(z),

                    nameof(Vector4f) =>
                        Optional.Some<Func<float, Func<float, Func<float, Func<float, ITypeTreeValue>>>>>(
                            x => y => z => w => obj.WithValue(new Vector4f(x, y, z, w)))
                            .Apply(x)
                            .Apply(y)
                            .Apply(z)
                            .Apply(w),

                    _ => default
                });
        }
    }
}
