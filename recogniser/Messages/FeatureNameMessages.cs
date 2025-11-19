// <copyright file="FeatureNameMessages.cs" company="recogniser project contributors">
// Copyright (c) 2025 recogniser project contributors.
// Licensed under the AGPL-3.0-or-later license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Recogniser.Messages
{
    internal static class FeatureNameMessages
    {
        internal static class Josm
        {
            public const string Missing =
                "The feature doesn't seem to have a name, so the name from GNIS should be added. (Editing the feature in JOSM will automatically add the name.) ";

            public const string Mismatch =
                "The feature's name is different from the name in GNIS. This can happen if the name is entered differently or if this is the wrong feature. Check the name in the GNIS record to confirm whether the name is correct. ";

            public const string DeprecatedKey =
                "The feature's name is in the `{0}` tag and should be moved to the `name` tag. (Editing the feature in JOSM may make this change if there is no conflict with an exiting `name` tag.) ";

            public const string Different =
                "The feature's name differs from the name in GNIS but is reasonably similar. This can happen if the name is entered differently or if this is the wrong feature. Check the name in the GNIS record to confirm whether the name is correct. ";
        }

        internal static class Plain
        {
            public const string Missing =
                "The feature doesn't seem to have a name, so the name from GNIS should be added. ";

            public const string Mismatch =
                "The feature's name is different from the name in GNIS. This can happen if the name is entered differently or if this is the wrong feature. Check the name in the GNIS record to confirm whether the name is correct. ";

            public const string DeprecatedKey =
                "The feature's name is in the `{0}` tag and should be moved to the `name` tag. ";

            public const string Different =
                "The feature's name differs from the name in GNIS but is reasonably similar. This can happen if the name is entered differently or if this is the wrong feature. Check the name in the GNIS record to confirm whether the name is correct. ";
        }
    }
}
