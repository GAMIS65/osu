﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.Graphics.Sprites;

namespace osu.Game.Graphics.UserInterface
{
    public abstract class ScoreCounter : RollingCounter<double>
    {
        protected override double RollingDuration => 1000;
        protected override Easing RollingEasing => Easing.Out;

        /// <summary>
        /// Whether comma separators should be displayed.
        /// </summary>
        public bool UseCommaSeparator { get; }

        public Bindable<int> RequiredDisplayDigits { get; } = new Bindable<int>();

        /// <summary>
        /// Displays score.
        /// </summary>
        /// <param name="leading">How many leading zeroes the counter will have.</param>
        /// <param name="useCommaSeparator">Whether comma separators should be displayed.</param>
        protected ScoreCounter(int leading = 0, bool useCommaSeparator = false)
        {
            UseCommaSeparator = useCommaSeparator;

            if (useCommaSeparator && leading > 0)
                throw new ArgumentException("Should not mix leading zeroes and comma separators as it doesn't make sense");

            RequiredDisplayDigits.Value = leading;
            RequiredDisplayDigits.BindValueChanged(_ => UpdateDisplay());
        }

        protected override double GetProportionalDuration(double currentValue, double newValue)
        {
            return currentValue > newValue ? currentValue - newValue : newValue - currentValue;
        }

        protected override LocalisableString FormatCount(double count)
        {
            string format = new string('0', RequiredDisplayDigits.Value);
            var output = ((long)count).ToString(format);

            if (UseCommaSeparator)
            {
                for (int i = output.Length - 3; i > 0; i -= 3)
                    output = output.Insert(i, @",");
            }

            return output;
        }

        protected override OsuSpriteText CreateSpriteText()
            => base.CreateSpriteText().With(s => s.Font = s.Font.With(fixedWidth: true));
    }
}
