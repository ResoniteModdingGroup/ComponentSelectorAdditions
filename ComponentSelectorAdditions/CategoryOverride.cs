using ComponentSelectorAdditions.Events;
using FrooxEngine;
using System;
using System.Collections.Generic;
using System.Text;

namespace ComponentSelectorAdditions
{
    // Make this have source and target to actually remove the types from the source?
    // Removal would be optional, of course
    internal sealed class CategoryOverride
    {
        private readonly Func<EnumerateComponentsEvent, IEnumerable<ComponentResult>> _getAdditionalComponents;
        public CategoryNode<Type> TargetCategory { get; }

        public CategoryOverride(CategoryNode<Type> targetCategory, Func<EnumerateComponentsEvent, IEnumerable<ComponentResult>> getAdditionalComponents)
        {
            TargetCategory = targetCategory;
            _getAdditionalComponents = getAdditionalComponents;
        }

        public IEnumerable<ComponentResult> GetAdditionalComponents(EnumerateComponentsEvent eventData)
            => _getAdditionalComponents(eventData);
    }
}