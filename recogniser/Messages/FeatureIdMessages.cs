// <copyright file="FeatureIdMessages.cs" company="recogniser project contributors">
// Copyright (c) 2025 recogniser project contributors.
// Licensed under the AGPL-3.0-or-later license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Recogniser.Messages
{
    internal static class FeatureIdMessages
    {
        internal static class Josm
        {
            public const string Missing =
                "The `gnis:feature_id` tag is missing and should be added. (Editing the feature in JOSM will automatically add this tag.) ";

            public const string Mismatch =
                "The `{0}` tag for this feature doesn't match the GNIS record. This might be the wrong feature or the tag value might be wrong. Check the feature against the GNIS record. ";

            public const string MalformedValue =
                "The `gnis:feature_id` tag has a malformed value and should be replaced. (Editing the feature in JOSM will automatically replace this tag.) ";

            public const string ExtraneousCharacters =
                "The `{0}` tag contains extra characters (i.e. zeros or spaces) which should be removed. (Editing the feature in JOSM will automatically correct the tag.) ";

            public const string MultipleValues =
                "The `{0}` tag has multiple values, one of which might match the GNIS record. This often happens when two GNIS records are merged into a single OSM feature. Check the other Feature IDs and decide whether they should be deleted or moved to separate features. ";

            public const string WrongKey =
                "The GNIS Feature ID is present in the `{0}` tag and should be moved to the `gnis:feature_id` tag. (Editing the feature in JOSM will automatically move the value to the correct tag.) ";
        }

        internal static class Plain
        {
            public const string Missing =
                "The `gnis:feature_id` tag is missing. This might be the wrong feature in OSM. Check the feature against the GNIS record. ";

            public const string Mismatch =
                "The `{0}` tag for this feature doesn't match the GNIS record. This might be the wrong feature or the tag value might be wrong. Check the feature against the GNIS record. ";

            public const string MalformedValue =
                "The `gnis:feature_id` tag has a malformed value. The `gnis:feature_id` tag should either be corrected or deleted. ";

            public const string ExtraneousCharacters =
                "The `{0}` tag contains extra characters (i.e. zeros or spaces) which should be removed. ";

            public const string MultipleValues =
                "The `{0}` tag has multiple values, one of which might match the GNIS record. This often happens when two GNIS records are merged into a single OSM feature. Check the GNIS record and decide whether this feature should be split into two separate features. ";

            public const string WrongKey =
                "The GNIS Feature ID is present in the `{0}` tag and should be moved to the `gnis:feature_id tag`. ";
        }
    }
}
