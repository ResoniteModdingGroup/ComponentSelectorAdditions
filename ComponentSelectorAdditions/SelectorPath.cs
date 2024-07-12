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

        private static readonly char[] _pathSeparators = { '/', '\\' };

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