using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MicroUtils.UnityFilesystem.Parsers
{
    public readonly partial record struct UnityReferencePath(string RawPath)
    {
        [GeneratedRegex(@"^(?'MountPoint'.+?)[\\\/](?:(?'ParentPath'.+?)[\\\/])*(?'ResourcePath'.+)$")]
        public static partial Regex PathRegex();

        private readonly Match match = PathRegex().Match(RawPath);

        public string MountPoint => match.Groups["MountPoint"].Value;
        public string ParentPath => match.Groups["ParentPath"].Value;
        public string ResourcePath => match.Groups["ResourcePath"].Value;
        public string ToFilePath() => $"{this.MountPoint}/{this.ResourcePath}";
    }
}
