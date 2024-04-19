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
    internal readonly struct ComponentResult
    {
        public CategoryNode<Type> Category { get; }
        public string? Group { get; }

        [MemberNotNullWhen(true, nameof(Group))]
        public bool HasGroup => Group is not null;

        public Type Type { get; }

        public ComponentResult(CategoryNode<Type> category, Type type)
        {
            Type = type;
            Category = category;
            Group = type.GetCustomAttribute<GroupingAttribute>()?.GroupName;
        }
    }
}