using Elements.Core;
using FrooxEngine;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ComponentSelectorAdditions
{
    /// <summary>
    /// Represents a component or node returned by the <see cref="Events.EnumerateComponentsEvent"/>.
    /// </summary>
    public sealed class ComponentResult
    {
        /// <summary>
        /// Gets the category that should be shown as the <see cref="Type">Type's</see> parent.
        /// </summary>
        public CategoryNode<Type>? Category { get; }

        /// <summary>
        /// Gets the <see cref="Type">Type's</see> group identifier, if present.
        /// </summary>
        public string? Group { get; }

        /// <summary>
        /// Gets the <see cref="Type">Type's</see> group name, if present.
        /// </summary>
        public string? GroupName { get; }

        /// <summary>
        /// Gets whether this <see cref="Type">Type</see> has a category.
        /// </summary>
        [MemberNotNullWhen(true, nameof(Category))]
        public bool HasCategory => Category is not null;

        /// <summary>
        /// Gets whether the <see cref="Type">Type</see> is part of a group.
        /// </summary>
        [MemberNotNullWhen(true, nameof(Group), nameof(GroupName))]
        public bool HasGroup => Group is not null;

        /// <summary>
        /// Gets whether this <see cref="Type">Type</see> is a generic type definition.
        /// </summary>
        public bool IsGeneric => Type.IsGenericTypeDefinition;

        /// <summary>
        /// Gets the <see cref="Type">Type's</see> nice name.
        /// </summary>
        public string NiceName => Type.GetNiceName();

        /// <summary>
        /// Gets the component / node <see cref="System.Type"/> result.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// Creates a new component / node <see cref="System.Type"/> result with the given category.
        /// </summary>
        /// <param name="category">The category that should be shown as the <see cref="Type">Type's</see> parent.</param>
        /// <param name="type">The component / node <see cref="System.Type"/>.</param>
        public ComponentResult(CategoryNode<Type>? category, Type type)
        {
            Type = type;
            Category = category;
            Group = type.GetCustomAttribute<GroupingAttribute>()?.GroupName;
            GroupName = Group?.Split('.').Last();
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
            => obj is ComponentResult otherResult && otherResult.Type == Type;

        /// <inheritdoc/>
        public override int GetHashCode() => Type.GetHashCode();
    }
}