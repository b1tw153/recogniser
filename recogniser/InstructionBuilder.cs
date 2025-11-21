// <copyright file="InstructionBuilder.cs" company="recogniser project contributors">
// Copyright (c) 2025 recogniser project contributors.
// Licensed under the AGPL-3.0-or-later license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Recogniser
{
    using System.Globalization;
    using System.Text;
    using System.Text.RegularExpressions;
    using Recogniser.Messages;

    internal sealed partial class InstructionBuilder
    {
        private static readonly string[] Numbers =
        {
            "some",
            "one",
            "two",
            "three",
            "four",
            "five",
            "six",
            "seven",
            "eight",
            "nine",
            "ten",
            "eleven",
            "twelve",
        };

        private readonly GnisClassData gnisClassData;

        private readonly Regex historical = HistoricalRegex();

        public InstructionBuilder(GnisClassData gnisClassData)
        {
            this.gnisClassData = gnisClassData;
        }

        public string BuildMultiMatchInstructions(GnisRecord gnisRecord, List<GnisMatchResult> matchResults, List<GnisValidationResult> validationResults)
        {
            StringBuilder result = new();

            int count = matchResults.Count;
            string number = count < Numbers.Length ? $"{Numbers[count]}" : $"{count}";
            string features = count == 1 ? "feature" : "features";

            List<string> josmObjectRefs = [];

            foreach (GnisMatchResult matchResult in matchResults)
            {
                if (matchResult.OsmFeature is OsmNode)
                {
                    josmObjectRefs.Add($"n{matchResult.OsmFeature.Id}");
                }

                if (matchResult.OsmFeature is OsmWay)
                {
                    josmObjectRefs.Add($"w{matchResult.OsmFeature.Id}");
                }

                if (matchResult.OsmFeature is OsmRelation)
                {
                    josmObjectRefs.Add($"r{matchResult.OsmFeature.Id}");
                }
            }

            result.Append(CultureInfo.InvariantCulture, $"The GNIS record may be mapped as {number} {features} near this location. [Click here to load the {features} in JOSM](http://localhost:8111/load_object?objects={string.Join(",", josmObjectRefs)}). Use the information in the GNIS record to find the appropriate {features} and update them to match the GNIS record. ");

            if (gnisRecord.HasSource())
            {
                result.Append("\n\nGNIS provides start and end points for this feature (shown in the preview). Use the start and end coordinates from GNIS to add or update the feature. ");
            }

            return result.ToString();
        }

        public string BuildPlainInstructions(GnisRecord gnisRecord, GnisMatchResult matchResult, GnisValidationResult validationResult)
        {
            StringBuilder result = new();

            if (matchResult.ConflictingTagMatched)
            {
                result.Append(CultureInfo.InvariantCulture, $"This feature seems like a match for GNIS record {gnisRecord.FeatureId} {gnisRecord.FeatureName} ({gnisRecord.FeatureClass}) but the feature has a `{matchResult.ConflictingTag}` tag that conflicts with the GNIS record. This might be the wrong feature or the tags might be wrong.\n\n");
            }
            else
            {
                result.Append(CultureInfo.InvariantCulture, $"This feature looks like a match for GNIS record {gnisRecord.FeatureId} {gnisRecord.FeatureName} ({gnisRecord.FeatureClass}).\n\n");
            }

            result.Append(BuildVariantMatchInstructions(gnisRecord, matchResult, validationResult, useJosmMessages: false));

            return result.ToString();
        }

        public string BuildSingleMatchInstructions(GnisRecord gnisRecord, GnisMatchResult matchResult, GnisValidationResult validationResult)
        {
            StringBuilder result = new();

            result.Append(CultureInfo.InvariantCulture, $"This feature looks like a match for GNIS record {gnisRecord.FeatureId} {gnisRecord.FeatureName} ({gnisRecord.FeatureClass}). \n\n");

            result.Append(BuildVariantMatchInstructions(gnisRecord, matchResult, validationResult, useJosmMessages: true));

            return result.ToString();
        }

        public string BuildNoMatchInstructions(GnisRecord gnisRecord)
        {
            StringBuilder result = new();

            result.Append("We didn't find an existing feature in OSM that matched the GNIS record. Sometimes the feature is mapped but doesn't have the right tags but more often the feature is not in OSM. ");
            if ("Civil".Equals(gnisRecord.FeatureClass, StringComparison.Ordinal))
            {
                if (gnisRecord.FeatureName.EndsWith("Reservation", StringComparison.Ordinal))
                {
                    result.Append("\n\nThis feature should be mapped as an [aboriginal lands boundary](https://wiki.openstreetmap.org/wiki/Tag:boundary%3Daboriginal_lands). If the feature is not already mapped, download the latest boundary data from the [US Census American Indian Geography Data Set](https://www.census.gov/cgi-bin/geo/shapefiles/index.php?year=2022&layergroup=American+Indian+Area+Geography) and import the boundary polygon.");
                }
                else
                {
                    result.Append("\n\nThis feature should be mapped as an [administrative boundary](https://wiki.openstreetmap.org/wiki/United_States/Boundaries). If the feature is not already mapped, download the latest boundary data from the [US Census Urban Areas Data Set](https://www.census.gov/cgi-bin/geo/shapefiles/index.php?year=2022&layergroup=Urban+Areas) and import the boundary polygon.");
                }
            }
            else if ("Census".Equals(gnisRecord.FeatureClass, StringComparison.Ordinal))
            {
                result.Append("\n\nThis feature should be mapped as a [census boundary](https://wiki.openstreetmap.org/wiki/Tag:boundary%3Dcensus). If the feature is not already mapped, download the latest boundary data from the [US Census Designated Places Data Set](https://www.census.gov/cgi-bin/geo/shapefiles/index.php?year=2022&layergroup=Places) and import the boundary polygon.");
            }
            else if ("Military".Equals(gnisRecord.FeatureClass, StringComparison.Ordinal))
            {
                result.Append("\n\nThis feature should be mapped as a [military use area](https://wiki.openstreetmap.org/wiki/Tag:landuse%3Dmilitary). If the feature is not already mapped, download the latest boundary data from the [TIGER/Line Shapefiles](https://www.census.gov/cgi-bin/geo/shapefiles/index.php?year=2022&layergroup=Military+Installations) and import the boundary polygon.");
            }
            else
            {
                if (!gnisRecord.HasSource())
                {
                    result.Append("(Editing the feature in JOSM will automatically add the feature at the GNIS coordinates.) ");
                }
                else
                {
                    result.Append("\n\nUse the start and end coordinates in GNIS to verify the extent of the feature. (Editing the feature in JOSM will automatically add the feature as a way between the start and end coordinates in GNIS. You will need to add nodes to the way to align the feature with USGS Topo maps.) ");
                }
            }

            return result.ToString();
        }

        internal string BuildTagFixInstructions(GnisRecord gnisRecord, GnisMatchResult matchResult, GnisValidationResult validationResult)
        {
            return BuildPlainInstructions(gnisRecord, matchResult, validationResult);
        }

        internal string BuildPlainNoMatchInstructions(GnisRecord gnisRecord)
        {
            StringBuilder result = new();

            result.Append("We didn't find an existing feature in OSM that matched the GNIS record. Sometimes the feature is mapped but doesn't have the right tags but more often the feature is not in OSM. ");
            if ("Civil".Equals(gnisRecord.FeatureClass, StringComparison.Ordinal))
            {
                if (gnisRecord.FeatureName.EndsWith("Reservation", StringComparison.Ordinal))
                {
                    result.Append("\n\nThis feature should be mapped as an [aboriginal lands boundary](https://wiki.openstreetmap.org/wiki/Tag:boundary%3Daboriginal_lands). If the feature is not already mapped, download the latest boundary data from the [US Census American Indian Geography Data Set](https://www.census.gov/cgi-bin/geo/shapefiles/index.php?year=2022&layergroup=American+Indian+Area+Geography) and import the boundary polygon.");
                }
                else
                {
                    result.Append("\n\nThis feature should be mapped as an [administrative boundary](https://wiki.openstreetmap.org/wiki/United_States/Boundaries). If the feature is not already mapped, download the latest boundary data from the [US Census Urban Areas Data Set](https://www.census.gov/cgi-bin/geo/shapefiles/index.php?year=2022&layergroup=Urban+Areas) and import the boundary polygon.");
                }
            }
            else if ("Census".Equals(gnisRecord.FeatureClass, StringComparison.Ordinal))
            {
                result.Append("\n\nThis feature should be mapped as a [census boundary](https://wiki.openstreetmap.org/wiki/Tag:boundary%3Dcensus). If the feature is not already mapped, download the latest boundary data from the [US Census Designated Places Data Set](https://www.census.gov/cgi-bin/geo/shapefiles/index.php?year=2022&layergroup=Places) and import the boundary polygon.");
            }
            else if ("Military".Equals(gnisRecord.FeatureClass, StringComparison.Ordinal))
            {
                result.Append("\n\nThis feature should be mapped as a [military use area](https://wiki.openstreetmap.org/wiki/Tag:landuse%3Dmilitary). If the feature is not already mapped, download the latest boundary data from the [TIGER/Line Shapefiles](https://www.census.gov/cgi-bin/geo/shapefiles/index.php?year=2022&layergroup=Military+Installations) and import the boundary polygon.");
            }
            else
            {
                if (!gnisRecord.HasSource())
                {
                    result.Append("\n\nUse the GNIS record to verify the location of the feature. ");
                }
                else
                {
                    result.Append("\n\nUse the start and end coordinates in GNIS to verify the extent of the feature. ");
                }
            }

            return result.ToString();
        }

        internal string BuildNewRelationInstructions(GnisRecord gnisRecord, GnisMatchResult matchResult, GnisValidationResult validationResult)
        {
            StringBuilder result = new();

            int count = (matchResult.OsmFeature as OsmRelation)?.Members.Count ?? 0;
            string number = count < Numbers.Length ? $"{Numbers[count]}" : $"{count}";
            string features = count == 1 ? "feature" : "features";

            result.Append(CultureInfo.InvariantCulture, $"The GNIS record is mapped as {number} {features} near this location. Use the information in the GNIS record to find the appropriate {features} and add a parent relation to match the GNIS record. (Editing in JOSM will automatically add a parent relation for the {features}.)\n\n");

            result.Append(BuildVariantMatchInstructions(gnisRecord, matchResult, validationResult, useJosmMessages: true));

            return result.ToString();
        }

        [GeneratedRegex(@".*\(historical\)$")]
        private static partial Regex HistoricalRegex();

        private string BuildVariantMatchInstructions(GnisRecord gnisRecord, GnisMatchResult matchResult, GnisValidationResult validationResult, bool useJosmMessages)
        {
            StringBuilder result = new();

            if (historical.IsMatch(gnisRecord.FeatureName))
            {
                result.Append("This GNIS record refers to an historical feature that may no longer exist. Check aerial and/or street-level imagery to determine if this feature still exists. If not, please remove it from OSM. ");
                result.Append("\n\n");
            }

            switch (validationResult.FeatureIdValidation)
            {
                case GnisFeatureIdValidation.OK:
                    break;
                case GnisFeatureIdValidation.FEATURE_ID_MISSING:
                    result.Append(useJosmMessages ? FeatureIdMessages.Josm.Missing : FeatureIdMessages.Plain.Missing);
                    result.Append("\n\n");
                    break;
                case GnisFeatureIdValidation.FEATURE_ID_MISMATCH:
                    result.Append(string.Format(CultureInfo.InvariantCulture, useJosmMessages ? FeatureIdMessages.Josm.Mismatch : FeatureIdMessages.Plain.Mismatch, matchResult.FeatureIdKey));
                    result.Append("\n\n");
                    break;
                case GnisFeatureIdValidation.FEATURE_ID_MALFORMED_VALUE:
                    result.Append(useJosmMessages ? FeatureIdMessages.Josm.MalformedValue : FeatureIdMessages.Plain.MalformedValue);
                    result.Append("\n\n");
                    break;
                case GnisFeatureIdValidation.FEATURE_ID_EXTRANEOUS_CHARACTERS:
                    result.Append(string.Format(CultureInfo.InvariantCulture, useJosmMessages ? FeatureIdMessages.Josm.ExtraneousCharacters : FeatureIdMessages.Plain.ExtraneousCharacters, matchResult.FeatureIdKey));
                    result.Append("\n\n");
                    break;
                case GnisFeatureIdValidation.FEATURE_ID_MULTIPLE_VALUES:
                    result.Append(string.Format(CultureInfo.InvariantCulture, useJosmMessages ? FeatureIdMessages.Josm.MultipleValues : FeatureIdMessages.Plain.MultipleValues, matchResult.FeatureIdKey));
                    result.Append("\n\n");
                    break;
                case GnisFeatureIdValidation.FEATURE_ID_MULTIPLE_VALUES_EXTRANEOUS_CHARACTERS:
                    result.Append(string.Format(CultureInfo.InvariantCulture, useJosmMessages ? FeatureIdMessages.Josm.MultipleValues : FeatureIdMessages.Plain.MultipleValues, matchResult.FeatureIdKey));
                    result.Append(string.Format(CultureInfo.InvariantCulture, useJosmMessages ? FeatureIdMessages.Josm.ExtraneousCharacters : FeatureIdMessages.Plain.ExtraneousCharacters, matchResult.FeatureIdKey));
                    result.Append("\n\n");
                    break;
                case GnisFeatureIdValidation.FEATURE_ID_WRONG_KEY:
                    result.Append(string.Format(CultureInfo.InvariantCulture, useJosmMessages ? FeatureIdMessages.Josm.WrongKey : FeatureIdMessages.Plain.WrongKey, matchResult.FeatureIdKey));
                    result.Append("\n\n");
                    break;
                case GnisFeatureIdValidation.FEATURE_ID_WRONG_KEY_EXTRANEOUS_CHARACTERS:
                    result.Append(string.Format(CultureInfo.InvariantCulture, useJosmMessages ? FeatureIdMessages.Josm.WrongKey : FeatureIdMessages.Plain.WrongKey, matchResult.FeatureIdKey));
                    result.Append(string.Format(CultureInfo.InvariantCulture, useJosmMessages ? FeatureIdMessages.Josm.ExtraneousCharacters : FeatureIdMessages.Plain.ExtraneousCharacters, matchResult.FeatureIdKey));
                    result.Append("\n\n");
                    break;
                case GnisFeatureIdValidation.NOT_PROCESSED:
                    break;
                default:
                    throw new NotImplementedException();
            }

            switch (validationResult.NameValidation)
            {
                case GnisNameValidation.OK:
                    break;
                case GnisNameValidation.FEATURE_NAME_MISSING:
                    result.Append(useJosmMessages ? FeatureNameMessages.Josm.Missing : FeatureNameMessages.Plain.Missing);
                    result.Append("\n\n");
                    break;
                case GnisNameValidation.FEATURE_NAME_MISMATCH:
                    result.Append(useJosmMessages ? FeatureNameMessages.Josm.Mismatch : FeatureNameMessages.Plain.Mismatch);
                    result.Append("\n\n");
                    break;
                case GnisNameValidation.FEATURE_NAME_DEPRECATED_KEY:
                    result.Append(string.Format(CultureInfo.InvariantCulture, useJosmMessages ? FeatureNameMessages.Josm.DeprecatedKey : FeatureNameMessages.Plain.DeprecatedKey, matchResult.NameKey));
                    result.Append("\n\n");
                    break;
                case GnisNameValidation.FEATURE_NAME_DIFFERENT:
                    result.Append(useJosmMessages ? FeatureNameMessages.Josm.Different : FeatureNameMessages.Plain.Different);
                    result.Append("\n\n");
                    break;
                case GnisNameValidation.FEATURE_NAME_DIFFERENT_DEPRECATED_KEY:
                    result.Append(useJosmMessages ? FeatureNameMessages.Josm.Different : FeatureNameMessages.Plain.Different);
                    result.Append(string.Format(CultureInfo.InvariantCulture, useJosmMessages ? FeatureNameMessages.Josm.DeprecatedKey : FeatureNameMessages.Plain.DeprecatedKey, matchResult.NameKey));
                    result.Append("\n\n");
                    break;
                case GnisNameValidation.NOT_PROCESSED:
                    break;
                default:
                    throw new NotImplementedException();
            }

            string defaultPrimaryTag = gnisClassData.GetGnisClassAttributes(gnisRecord.FeatureClass).DefaultPrimaryTag;
            string defaultSecondaryTag = gnisClassData.GetGnisClassAttributes(gnisRecord.FeatureClass).DefaultSecondaryTag;
            string allDefaultTags = string.IsNullOrEmpty(defaultSecondaryTag) ? defaultPrimaryTag : $"{defaultPrimaryTag} and {defaultSecondaryTag}";

            switch (validationResult.TagValidation)
            {
                case GnisTagValidation.OK:
                    break;
                case GnisTagValidation.FEATURE_CLASS_TAGS_MISSING:
                    result.Append(string.Format(CultureInfo.InvariantCulture, useJosmMessages ? FeatureTagMessages.Josm.AllTagsMissing : FeatureTagMessages.Plain.AllTagsMissing, allDefaultTags));
                    result.Append("\n\n");
                    break;
                case GnisTagValidation.FEATURE_CLASS_SECONDARY_TAG_MISSING:
                    result.Append(string.Format(CultureInfo.InvariantCulture, useJosmMessages ? FeatureTagMessages.Josm.SecondaryTagMissing : FeatureTagMessages.Plain.SecondaryTagMissing, defaultSecondaryTag));
                    result.Append("\n\n");
                    break;
                case GnisTagValidation.FEATURE_CLASS_PRIMARY_TAG_MISSING:
                    result.Append(string.Format(CultureInfo.InvariantCulture, useJosmMessages ? FeatureTagMessages.Josm.PrimaryTagMissing : FeatureTagMessages.Plain.PrimaryTagMissing, defaultPrimaryTag));
                    result.Append("\n\n");
                    break;
                case GnisTagValidation.NOT_PROCESSED:
                    break;
                default:
                    throw new NotImplementedException();
            }

            switch (validationResult.GeometryValidation)
            {
                case GnisGeometryValidation.OK:
                case GnisGeometryValidation.OK_REVERSED:
                    break;
                case GnisGeometryValidation.FEATURE_COORDINATE_EXTENT_OFF:
                case GnisGeometryValidation.FEATURE_COORDINATE_EXTENT_OFF_REVERSED:
                    if (!gnisRecord.HasSource())
                    {
                        if (matchResult.OsmFeature is OsmNode)
                        {
                            result.Append(useJosmMessages ? FeatureGeometryMessages.Josm.ExtentOffPoint : FeatureGeometryMessages.Plain.ExtentOffPoint);
                            result.Append("\n\n");
                        }
                    }
                    else
                    {
                        result.Append(useJosmMessages ? FeatureGeometryMessages.Josm.ExtentOffLine : FeatureGeometryMessages.Plain.ExtentOffLine);
                        result.Append("\n\n");
                    }

                    break;
                case GnisGeometryValidation.FEATURE_COORDINATE_START_SOURCE_OFF:
                case GnisGeometryValidation.FEATURE_COORDINATE_START_PRIMARY_OFF:
                    if (gnisRecord.HasSource())
                    {
                        result.Append(useJosmMessages ? FeatureGeometryMessages.Josm.StartLocationOff : FeatureGeometryMessages.Plain.StartLocationOff);
                        result.Append("\n\n");
                    }

                    break;
                case GnisGeometryValidation.FEATURE_COORDINATE_END_PRIMARY_OFF:
                case GnisGeometryValidation.FEATURE_COORDINATE_END_SOURCE_OFF:
                    if (gnisRecord.HasSource())
                    {
                        result.Append(useJosmMessages ? FeatureGeometryMessages.Josm.EndLocationOff : FeatureGeometryMessages.Plain.EndLocationOff);
                        result.Append("\n\n");
                    }

                    break;
                case GnisGeometryValidation.NOT_PROCESSED:
                    break;
                default:
                    throw new NotImplementedException();
            }

            OsmTagCollection tags = matchResult.OsmFeature.GetTagCollection();
            if (tags.ContainsKey("ele"))
            {
                double osmElevation = double.TryParse(tags["ele"], out double value) ? value : -100;
                double gnisElevation = double.TryParse(gnisRecord.Elevation, out value) ? value : -200;

                if (Math.Abs(osmElevation - gnisElevation) < 2)
                {
                    result.Append(CultureInfo.InvariantCulture, $"The elevation value ele={osmElevation} ({Math.Round(osmElevation * 3.28084, 0)} ft) might be incorrect. Check the value against the elevation on USGS Topo maps.");
                    result.Append("\n\n");
                }
            }

            if (tags.ContainsKey("waterway") && !tags.ContainsKey("intermittent"))
            {
                result.Append("Check the USGS Topo map to determine if this waterway is intermittent (i.e. mapped with a dashed/dotted line) and add the `intermittent=yes` tag if it is. ");
                result.Append("\n\n");
            }

            if (matchResult.OsmFeature is OsmRelation)
            {
                result.Append("Remember to check the relation members for tags that might conflict with the GNIS record (e.g. different Feature IDs or names). ");
                result.Append("\n\n");
            }

            return result.ToString();
        }
    }
}