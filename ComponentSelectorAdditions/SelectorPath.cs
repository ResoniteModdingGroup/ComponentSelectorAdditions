using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComponentSelectorAdditions
{
    public sealed class SelectorPath
    {
        public const string SearchSegment = "Search";
        private static readonly char[] _groupSeparators = { '?', ':' };
        private static readonly char[] _pathSeparators = { '/', '\\' };
        public bool GenericType { get; }
        public string? Group { get; }

        [MemberNotNullWhen(true, nameof(Group))]
        public bool HasGroup => Group is not null;

        [MemberNotNullWhen(true, nameof(Search))]
        public bool HasSearch => Search is not null;

        public bool IsRootCategory => PathSegments.Length == 0;
        public bool IsSelectorRoot { get; }
        public string OpenParentCategoryPath => $"/{PathSegments.Take(PathSegments.Length - (GenericType || !HasGroup ? 1 : 0)).Join(delimiter: "/")}{(GenericType && HasGroup ? $"?{Group}" : "")}";

        public string Path { get; }
        public string[] PathSegments { get; }
        public string? Search { get; }

        internal SelectorPath(string? rawPath, string? search, bool genericType, string? group, bool isSelectorRoot)
        {
            Search = search;
            GenericType = genericType;
            Group = group;
            IsSelectorRoot = isSelectorRoot;

            PathSegments = rawPath?.Split(_pathSeparators, StringSplitOptions.RemoveEmptyEntries).ToArray() ?? Array.Empty<string>();
            Path = $"/{PathSegments.Join(delimiter: "/")}";
        }
    }
}