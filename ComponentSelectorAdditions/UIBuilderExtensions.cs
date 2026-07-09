using Elements.Core;
using FrooxEngine.UIX;
using FrooxEngine;

namespace ComponentSelectorAdditions
{
    internal static class UIBuilderExtensions
    {
        public static UIBuilder SetupStyle(this UIBuilder builder)
        {
            RadiantUI_Constants.SetupEditorStyle(builder, extraPadding: true);

            builder.Style.TextAlignment = Alignment.MiddleLeft;
            builder.Style.ButtonTextAlignment = Alignment.MiddleLeft;
            builder.Style.MinHeight = 32;

            return builder;
        }
    }
}