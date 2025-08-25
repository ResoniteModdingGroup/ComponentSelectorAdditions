using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComponentSelectorAdditions
{
    /// <summary>
    /// Represents the currently opened path in a <see cref="FrooxEngine.ComponentSelector"/>.
    /// </summary>
    public sealed class SelectorPath
    {
        /// <summary>
        /// The path segment after which the search string begins.
        /// </summary>
        public const string SearchSegment = "Search";

        private static readonly char _genericParamEnd = '>';
        private static readonly char _genericParamStart = '<';

        private static readonly char[] _pathSeparators = { '/', '\\' };

        private static readonly char[] _searchSplits = new[] { ' ', '.', ',', ';', '?', '!', '+', '|', '&', '`', '´', '"', '(', ')', '/', '\\', '\n', '\r', '\t' };

        /// <summary>
        /// Gets whether this path targets a generic type.
        /// </summary>
        public bool GenericType { get; }

        /// <summary>
        /// Gets the group targeted by the path, if present.
        /// </summary>
        public string? Group { get; }

        /// <summary>
        /// Gets whether this path targets a group.
        /// </summary>
        [MemberNotNullWhen(true, nameof(Group))]
        public bool HasGroup => !string.IsNullOrWhiteSpace(Group);

        /// <summary>
        /// Gets whether this path has a search string.
        /// </summary>
        [MemberNotNullWhen(true, nameof(Search))]
        public bool HasSearch => !string.IsNullOrWhiteSpace(Search);

        /// <summary>
        /// Gets whether this path has a <see cref="SearchGeneric">generic argument</see> for the search.
        /// </summary>
        [MemberNotNullWhen(true, nameof(SearchGeneric))]
        public bool HasSearchGeneric => !string.IsNullOrWhiteSpace(SearchGeneric);

        /// <summary>
        /// Gets whether this path targets the root category.
        /// </summary>
        public bool IsRootCategory => PathSegments.Length == 0;

        /// <summary>
        /// Gets whether this path targets whatever the root category of its <see cref="FrooxEngine.ComponentSelector"/> is.
        /// </summary>
        public bool IsSelectorRoot { get; }

        /// <summary>
        /// Gets the path to open the current path's target's parent.
        /// </summary>
        public string OpenParentCategoryPath => $"/{PathSegments.Take(PathSegments.Length - (GenericType || !HasGroup ? 1 : 0)).Join(delimiter: "/")}{(GenericType && HasGroup ? $"?{Group}" : "")}";

        /// <summary>
        /// Gets this path as a string.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets this path's individual segments.
        /// </summary>
        public string[] PathSegments { get; }

        /// <summary>
        /// Gets this path's search string, if present.
        /// </summary>
        public string? Search { get; }

        /// <summary>
        /// Gets this path's search fragments (before the <see cref="SearchGeneric">generic argument</see>).
        /// </summary>
        public string[] SearchFragments { get; } = Array.Empty<string>();

        /// <summary>
        /// Gets this path's generic argument for the search.
        /// </summary>
        public string? SearchGeneric { get; }

        internal SelectorPath(string? rawPath, string? search, bool genericType, string? group, bool isSelectorRoot)
        {
            Search = search;

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search!.Replace('[', _genericParamStart).Replace(']', _genericParamEnd);

                var genericParamStartIndex = search.IndexOf(_genericParamStart);

                if (genericParamStartIndex > 0)
                {
                    var generic = search[(genericParamStartIndex + 1)..].Trim();
                    var starts = generic.Count(static c => c == _genericParamStart);
                    var ends = generic.Count(static c => c == _genericParamEnd);

                    if (starts > ends) // Automatically add any missing >
                        generic += new string(_genericParamEnd, starts - ends);
                    else if (ends > starts) // Probably not gonna happen often, but if someone adds too many closing > ...
                        generic = generic[..^(ends - starts)];

                    SearchGeneric = generic;
                    search = search[..genericParamStartIndex];
                }

                SearchFragments = search.Split(_searchSplits, StringSplitOptions.RemoveEmptyEntries);
            }

            GenericType = genericType;
            Group = group;
            IsSelectorRoot = isSelectorRoot;

            PathSegments = rawPath?.Split(_pathSeparators, StringSplitOptions.RemoveEmptyEntries).ToArray() ?? Array.Empty<string>();
            Path = $"/{PathSegments.Join(delimiter: "/")}";
        }
    }
}