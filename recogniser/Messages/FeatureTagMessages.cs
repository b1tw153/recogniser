// <copyright file="FeatureTagMessages.cs" company="recogniser project contributors">
// Copyright (c) 2025 recogniser project contributors.
// Licensed under the AGPL-3.0-or-later license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Recogniser.Messages
{
    internal static class FeatureTagMessages
    {
        internal static class Josm
        {
            public const string AllTagsMissing =
                "The expected tags (e.g. `{0}`) for this type of feature seem to be missing. Check the tags against the Feature Class in GNIS. (Editing the feature in JOSM will automatically add the default tags for the Feature Class.) ";

            public const string PrimaryTagMissing =
                "The expected primary tag (e.g. `{0}`) for this type of feature seems to be missing. Check the tags against the Feature Class in GNIS. (Editing the feature in JOSM will automatically add the default primary tag for the Feature Class.) ";

            public const string SecondaryTagMissing =
                "The expected secondary tag (e.g. `{0}`) for this type of feature seems to be missing. Check the tags against the Feature Class in GNIS. (Editing the feature in JOSM will automatically add the default secondary tag for the Feature Class.) ";
        }

        internal static class Plain
        {
            public const string AllTagsMissing =
                "The expected tags (e.g. `{0}`) for this type of feature seem to be missing. Check the tags against the Feature Class in GNIS. ";

            public const string PrimaryTagMissing =
                "The expected primary tag (e.g. `{0}`) for this type of feature seems to be missing. Check the tags against the Feature Class in GNIS. ";

            public const string SecondaryTagMissing =
                "The expected secondary tag (e.g. `{0}`) for this type of feature seems to be missing. Check the tags against the Feature Class in GNIS. ";
        }
    }
}
