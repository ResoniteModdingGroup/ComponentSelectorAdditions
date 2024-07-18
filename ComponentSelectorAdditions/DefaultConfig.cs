﻿using MonkeyLoader.Configuration;
using System;

namespace ComponentSelectorAdditions
{
    /// <summary>
    /// Contains settings for the buttons generated by default.
    /// </summary>
    public sealed class DefaultConfig : ConfigSection
    {
        private static readonly DefiningConfigKey<float> _directButtonHeight = new("DirectButtonHeight", "The height of a button that is targeting the direct child of the current category, in canvas units. The default value is 32.", () => 32)
        {
            new ConfigKeyRange<float>(32, 64)
        };

        private static readonly DefiningConfigKey<float> _indirectButtonHeight = new("IndirectButtonHeight", "The height of a button that is not targeting a direct child of the current category and has to fit a category path as well, in canvas units. The default value is 48.", () => 48)
        {
            new ConfigKeyRange<float>(32, 64)
        };

        /// <summary>
        /// Gets this config's instance.
        /// </summary>
        public static DefaultConfig Instance { get; private set; } = null!;

        /// <inheritdoc/>
        public override string Description => "Contains settings for the buttons generated by default.";

        /// <summary>
        /// The height of a button that is the direct child of the current category.
        /// </summary>
        /// <value>The height in canvas units.</value>
        public float DirectButtonHeight => _directButtonHeight;

        /// <inheritdoc/>
        public override string Id => "Defaults";

        /// <summary>
        /// Gets the height of a button that is not a direct child of the current category and has to fit a category path as well.
        /// </summary>
        /// <value>The height in canvas units.</value>
        public float IndirectButtonHeight => _indirectButtonHeight;

        /// <inheritdoc/>
        public override Version Version { get; } = new Version(1, 0, 0);

        /// <summary>
        /// Creates an instance of this config once.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public DefaultConfig()
        {
            if (Instance is not null)
                throw new InvalidOperationException();

            Instance = this;
        }
    }
}