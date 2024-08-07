﻿using FrooxEngine;
using FrooxEngine.UIX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComponentSelectorAdditions.Events
{
    public sealed class BuildComponentButtonEvent : BuildButtonEvent
    {
        public ComponentResult Component { get; }

        public SelectorPath Path { get; }

        /// <inheritdoc/>
        internal BuildComponentButtonEvent(ComponentSelector selector, UIBuilder ui, SelectorPath path, CategoryNode<Type>? rootCategory, ComponentResult component)
            : base(selector, ui, rootCategory, component.Category, component.Category == rootCategory)
        {
            Path = path;
            Component = component;
        }
    }
}