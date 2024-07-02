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
    public sealed class ComponentResult
    {
        public CategoryNode<Type> Category { get; }
        public string FullName => Type.FullName;
        public string? Group { get; }

        public string? GroupName { get; }

        [MemberNotNullWhen(true, nameof(Group), nameof(GroupName))]
        public bool HasGroup => Group is not null;

        public bool IsGeneric => Type.IsGenericTypeDefinition;

        public string NiceName => Type.GetNiceName();
        public Type Type { get; }

        public ComponentResult(CategoryNode<Type> category, Type type)
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