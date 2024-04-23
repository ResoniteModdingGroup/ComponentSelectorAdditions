﻿using FrooxEngine;
using FrooxEngine.UIX;
using MonkeyLoader.Events;
using MonkeyLoader.Resonite.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComponentSelectorAdditions.Events
{
    public sealed class BuildGroupButtonEvent : BuildButtonEvent
    {
        public string Group { get; }
        public string GroupName { get; }

        /// <inheritdoc/>
        internal BuildGroupButtonEvent(ComponentSelector selector, UIBuilder ui, CategoryNode<Type> rootCategory, ComponentResult groupComponent)
            : base(selector, ui, rootCategory, groupComponent.Category)
        {
            Group = groupComponent.Group!;
            GroupName = groupComponent.GroupName!;
        }
    }
}