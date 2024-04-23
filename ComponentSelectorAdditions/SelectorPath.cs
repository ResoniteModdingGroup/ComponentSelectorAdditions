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
        public string? Group { get; }

        [MemberNotNullWhen(true, nameof(Group))]
        public bool HasGroup => Group is not null;

        [MemberNotNullWhen(true, nameof(Search))]
        public bool HasSearch => Search is not null;

        public bool IsRootCategory => PathSegments.Length == 0;
        public bool IsSelectorRoot { get; }
        public string OpenParentCategoryPath => $"/{PathSegments.Take(PathSegments.Length - 1).Join(delimiter: "/")}{(HasGroup ? $"?{Group}" : "")}";

        public string Path { get; }
        public string[] PathSegments { get; }
        public string? Search { get; }

        internal SelectorPath(string? rawPath, string? group, bool isSelectorRoot)
        {
            Group = group;
            IsSelectorRoot = isSelectorRoot;

            var pathSplit = rawPath?.Split(_pathSeparators, StringSplitOptions.RemoveEmptyEntries);
            PathSegments = pathSplit?.TakeWhile(segment => !SearchSegment.Equals(segment, StringComparison.OrdinalIgnoreCase)).ToArray() ?? Array.Empty<string>();

            Path = $"/{PathSegments.Join(delimiter: "/")}";

            // PathSegments ends before search, so +2 must be the search string
            if (pathSplit is not null && pathSplit.Length > PathSegments.Length + 1)
                Search = pathSplit[pathSplit.Length + 1];
        }
    }
}