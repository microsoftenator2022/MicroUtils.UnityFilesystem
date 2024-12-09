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
    public readonly record struct Rectf(float x, float y, float width, float height);
#pragma warning restore IDE1006 // Naming Styles
    class RectfParser : IObjectParser
    {
        public bool CanParse(TypeTreeNode node) => node.Type == "Rectf";
        public Type ObjectType(TypeTreeNode node) => typeof(TypeTreeValue<Rectf>);
        public Optional<ITypeTreeValue> TryParse(ITypeTreeValue obj, SerializedFile sf)
        {
            if (obj is not ITypeTreeObject o)
                return default;

            var x = o.TryGetField<float>("x").Map(get => get());
            var y = o.TryGetField<float>("y").Map(get => get());
            var width = o.TryGetField<float>("width").Map(get => get());
            var height = o.TryGetField<float>("height").Map(get => get());

            return Optional.Some((float x) => (float y) => (float width) => (float height) => new Rectf(x, y, width, height))
                .Apply(x)
                .Apply(y)
                .Apply(width)
                .Apply(height)
                .Map<Rectf, ITypeTreeValue>(rect => obj.WithValue(rect));
        }
    }
}
