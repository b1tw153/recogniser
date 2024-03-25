using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.RegularExpressions;

namespace recogniser
{

    public class InstructionBuilder
    {
        private readonly GnisClassData gnisClassData;

        private class FeatureIdMessages
        {
            public string missing =
                "The `gnis:feature_id` tag is missing and should be added. (Editing the feature in JOSM will automatically add this tag.) ";
            public string mismatch =
                "The `{0}` tag for this feature doesn't match the GNIS record. This might be the wrong feature or the tag value might be wrong. Check the feature against the GNIS record. ";
            public string malformedValue =
                "The `gnis:feature_id` tag has a malformed value and should be replaced. (Editing the feature in JOSM will automatically replace this tag.) ";
            public string extraneousCharacters =
                "The `{0}` tag contains extra characters (i.e. zeros or spaces) which should be removed. (Editing the feature in JOSM will automatically correct the tag.) ";
            public string multipleValues =
                "The `{0}` tag has multiple values, one of which might match the GNIS record. This often happens when two GNIS records are merged into a single OSM feature. Check the other Feature IDs and decide whether they should be deleted or moved to separate features. ";
            public string wrongKey =
                "The GNIS Feature ID is present in the `{0}` tag and should be moved to the `gnis:feature_id` tag. (Editing the feature in JOSM will automatically move the value to the correct tag.) ";
        }

        private class FeatureIdMessagesPlain : FeatureIdMessages
        {
            public FeatureIdMessagesPlain()
            {
                missing =
                    "The `gnis:feature_id` tag is missing. This might be the wrong feature in OSM. Check the feature against the GNIS record. ";
                malformedValue =
                    "The `gnis:feature_id` tag has a malformed value. The `gnis:feature_id` tag should either be corrected or deleted. ";
                extraneousCharacters =
                    "The `{0}` tag contains extra characters (i.e. zeros or spaces) which should be removed. ";
                multipleValues =
                    "The `{0}` tag has multiple values, one of which might match the GNIS record. This often happens when two GNIS records are merged into a single OSM feature. Check the GNIS record and decide whether this feature should be split into two separate features. ";
                wrongKey =
                    "The GNIS Feature ID is present in the `{0}` tag and should be moved to the `gnis:feature_id tag`. ";
            }
        }

        private class FeatureNameMessages
        {
            public string missing =
                "The feature doesn't seem to have a name, so the name from GNIS should be added. (Editing the feature in JOSM will automatically add the name.) ";
            public string mismatch =
                "The feature's name is different from the name in GNIS. This can happen if the name is entered differently or if this is the wrong feature. Check the name in the GNIS record to confirm whether the name is correct. ";
            public string deprecatedKey =
                "The feature's name is in the `{0}` tag and should be moved to the `name` tag. (Editing the feature in JOSM may make this change if there is no conflict with an exiting `name` tag.) ";
            public string different =
                "The feature's name differs from the name in GNIS but is reasonably similar. This can happen if the name is entered differently or if this is the wrong feature. Check the name in the GNIS record to confirm whether the name is correct. ";
        }

        private class FeatureNameMessagesPlain : FeatureNameMessages
        {
            public FeatureNameMessagesPlain()
            {
                missing =
                    "The feature doesn't seem to have a name, so the name from GNIS should be added. ";
                deprecatedKey =
                    "The feature's name is in the `{0}` tag and should be moved to the `name` tag. ";
            }
        }

        private class FeatureGeometryMessages
        {
            public string extentOffPoint =
                "The location of this feature doesn't match the coordinates in GNIS. Check the GNIS coordinates using aerial imagery and USGS Topo maps. (Editing the feature in JOSM will automatically move it to the location specified in GNIS.) ";
            public string extentOffLine =
                "The extent of this feature doesn't match the coordinates in GNIS. Check the start and end points of the feature using the GNIS coordinates. (Editing the feature in JOSM will automatically add start and end nodes at the locations specified in GNIS.) ";
            public string startLocationOff =
                "The start of this feature doesn't match the coordinates in GNIS. Check the start point using the GNIS coordinates. (Editing the feature in JOSM will automatically add a start node at the location specified in GNIS.) ";
            public string endLocationOff =
                "The end of this feature doesn't match the coordinates in GNIS. Check the end point using the GNIS coordinates. (Editing the feature in JOSM will automatically add an end node at the location specified in GNIS.) ";
            //public readonly string reversed =
            //    "The feature is mapped in the wrong direction and should be reversed. (Editing the feature in JOSM will automatically reverse the direction of the feature.) ";
        }

        private class FeatureGeometryMessagesPlain : FeatureGeometryMessages
        {
            public FeatureGeometryMessagesPlain()
            {
                extentOffPoint =
                    "The location of this feature doesn't match the coordinates in GNIS. Check the GNIS coordinates using aerial imagery and USGS Topo maps. ";
                extentOffLine =
                    "The extent of this feature doesn't match the coordinates in GNIS. Check the start and end points of the feature using the GNIS coordinates. ";
                startLocationOff =
                    "The start of this feature doesn't match the coordinates in GNIS. Check the start point using the GNIS coordinates. ";
                endLocationOff =
                    "The end of this feature doesn't match the coordinates in GNIS. Check the end point using the GNIS coordinates. ";
                //reversed =
                    //"The feature is mapped in the wrong direction. If this is the correct feature for the GNIS record, it should be reversed. ";
            }
        }

        private class FeatureTagMessages
        {
            public string allTagsMissing =
                "The expected tags (e.g. `{0}`) for this type of feature seem to be missing. Check the tags against the Feature Class in GNIS. (Editing the feature in JOSM will automatically add the default tags for the Feature Class.) ";
            public string primaryTagMissing =
                "The expected primary tag (e.g. `{0}`) for this type of feature seems to be missing. Check the tags against the Feature Class in GNIS. (Editing the feature in JOSM will automatically add the default primary tag for the Feature Class.) ";
            public string secondaryTagMissing =
                "The expected secondary tag (e.g. `{0}`) for this type of feature seems to be missing. Check the tags against the Feature Class in GNIS. (Editing the feature in JOSM will automatically add the default secondary tag for the Feature Class.) ";
        }

        private class FeatureTagMessagesPlain : FeatureTagMessages
        {
            public FeatureTagMessagesPlain()
            {
                allTagsMissing =
                    "The expected tags (e.g. `{0}`) for this type of feature seem to be missing. Check the tags against the Feature Class in GNIS. ";
                primaryTagMissing =
                    "The expected primary tag (e.g. `{0}`) for this type of feature seems to be missing. Check the tags against the Feature Class in GNIS. ";
                secondaryTagMissing =
                    "The expected secondary tag (e.g. `{0}`) for this type of feature seems to be missing. Check the tags against the Feature Class in GNIS. ";
            }
        }

        private static readonly string[] numbers =
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
            "twelve"
        };

        public InstructionBuilder(GnisClassData gnisClassData)
        {
            this.gnisClassData = gnisClassData;
        }

        public string BuildMultiMatchInstructions(GnisRecord gnisRecord, List<GnisMatchResult> matchResults, List<GnisValidationResult> validationResults)
        {
            StringBuilder result = new();

            int count = matchResults.Count;
            string number = count < numbers.Length? $"{numbers[count]}" : $"{count}";
            string features = count == 1 ? "feature" : "features";

            List<string> josmObjectRefs = new();

            foreach (GnisMatchResult matchResult in matchResults)
            {
                if (matchResult.osmFeature is OsmNode)
                    josmObjectRefs.Add($"n{matchResult.osmFeature.Id}");

                if (matchResult.osmFeature is OsmWay)
                    josmObjectRefs.Add($"w{matchResult.osmFeature.Id}");

                if (matchResult.osmFeature is OsmRelation)
                    josmObjectRefs.Add($"r{matchResult.osmFeature.Id}");
            }

            result.Append($"The GNIS record may be mapped as {number} {features} near this location. [Click here to load the {features} in JOSM](http://localhost:8111/load_object?objects={string.Join(",",josmObjectRefs)}). Use the information in the GNIS record to find the appropriate {features} and update them to match the GNIS record. ");

            if (gnisRecord.HasSource())
                result.Append("\n\nGNIS provides start and end points for this feature (shown in the preview). Use the start and end coordinates from GNIS to add or update the feature. ");

            return result.ToString();
        }

        public string BuildPlainInstructions(GnisRecord gnisRecord, GnisMatchResult matchResult, GnisValidationResult validationResult)
        {
            StringBuilder result = new();

            if (matchResult.ConflictingTagMatched)
                result.Append($"This feature seems like a match for GNIS record {gnisRecord.FeatureId} {gnisRecord.FeatureName} ({gnisRecord.FeatureClass}) but the feature has a `{matchResult.conflictingTag}` tag that conflicts with the GNIS record. This might be the wrong feature or the tags might be wrong.\n\n");
            else
                result.Append($"This feature looks like a match for GNIS record {gnisRecord.FeatureId} {gnisRecord.FeatureName} ({gnisRecord.FeatureClass}).\n\n");

            result.Append(BuildVariantMatchInstructions(gnisRecord, matchResult, validationResult, new FeatureIdMessagesPlain(), new FeatureNameMessagesPlain(), new FeatureTagMessagesPlain(), new FeatureGeometryMessagesPlain()));

            return result.ToString();
        }

        public string BuildSingleMatchInstructions(GnisRecord gnisRecord, GnisMatchResult matchResult, GnisValidationResult validationResult)
        {
            StringBuilder result = new();

            result.Append($"This feature looks like a match for GNIS record {gnisRecord.FeatureId} {gnisRecord.FeatureName} ({gnisRecord.FeatureClass}). \n\n");

            result.Append(BuildVariantMatchInstructions(gnisRecord, matchResult, validationResult, new FeatureIdMessages(), new FeatureNameMessages(), new FeatureTagMessages(), new FeatureGeometryMessages()));

            return result.ToString();
        }

        private readonly Regex historical = new(@".*\(historical\)$");

        private string BuildVariantMatchInstructions(GnisRecord gnisRecord, GnisMatchResult matchResult, GnisValidationResult validationResult, FeatureIdMessages idMessages, FeatureNameMessages nameMessages, FeatureTagMessages tagMessages, FeatureGeometryMessages geometryMessages)
        {
            StringBuilder result = new();

            if (historical.IsMatch(gnisRecord.FeatureName))
            {
                result.Append("This GNIS record refers to an historical feature that may no longer exist. Check aerial and/or street-level imagery to determine if this feature still exists. If not, please remove it from OSM. ");
                result.Append("\n\n");
            }

            switch (validationResult.featureIdValidation)
            {
                case GnisFeatureIdValidation.OK:
                    break;
                case GnisFeatureIdValidation.FEATURE_ID_MISSING:
                    result.Append(idMessages.missing);
                    result.Append("\n\n");
                    break;
                case GnisFeatureIdValidation.FEATURE_ID_MISMATCH:
                    result.Append(string.Format(idMessages.mismatch, matchResult.featureIdKey));
                    result.Append("\n\n");
                    break;
                case GnisFeatureIdValidation.FEATURE_ID_MALFORMED_VALUE:
                    result.Append(idMessages.malformedValue);
                    result.Append("\n\n");
                    break;
                case GnisFeatureIdValidation.FEATURE_ID_EXTRANEOUS_CHARACTERS:
                    result.Append(string.Format(idMessages.extraneousCharacters, matchResult.featureIdKey));
                    result.Append("\n\n");
                    break;
                case GnisFeatureIdValidation.FEATURE_ID_MULTIPLE_VALUES:
                    result.Append(string.Format(idMessages.multipleValues, matchResult.featureIdKey));
                    result.Append("\n\n");
                    break;
                case GnisFeatureIdValidation.FEATURE_ID_MULTIPLE_VALUES_EXTRANEOUS_CHARACTERS:
                    result.Append(string.Format(idMessages.multipleValues, matchResult.featureIdKey));
                    result.Append(string.Format(idMessages.extraneousCharacters, matchResult.featureIdKey));
                    result.Append("\n\n");
                    break;
                case GnisFeatureIdValidation.FEATURE_ID_WRONG_KEY:
                    result.Append(string.Format(idMessages.wrongKey, matchResult.featureIdKey));
                    result.Append("\n\n");
                    break;
                case GnisFeatureIdValidation.FEATURE_ID_WRONG_KEY_EXTRANEOUS_CHARACTERS:
                    result.Append(string.Format(idMessages.wrongKey, matchResult.featureIdKey));
                    result.Append(string.Format(idMessages.extraneousCharacters, matchResult.featureIdKey));
                    result.Append("\n\n");
                    break;
                case GnisFeatureIdValidation.NOT_PROCESSED:
                    break;
                default:
                    throw new NotImplementedException();
            }

            switch (validationResult.nameValidation)
            {
                case GnisNameValidation.OK:
                    break;
                case GnisNameValidation.FEATURE_NAME_MISSING:
                    result.Append(nameMessages.missing);
                    result.Append("\n\n");
                    break;
                case GnisNameValidation.FEATURE_NAME_MISMATCH:
                    result.Append(nameMessages.mismatch);
                    result.Append("\n\n");
                    break;
                case GnisNameValidation.FEATURE_NAME_DEPRECATED_KEY:
                    result.Append(string.Format(nameMessages.deprecatedKey, matchResult.nameKey));
                    result.Append("\n\n");
                    break;
                case GnisNameValidation.FEATURE_NAME_DIFFERENT:
                    result.Append(nameMessages.different);
                    result.Append("\n\n");
                    break;
                case GnisNameValidation.FEATURE_NAME_DIFFERENT_DEPRECATED_KEY:
                    result.Append(nameMessages.different);
                    result.Append(string.Format(nameMessages.deprecatedKey, matchResult.nameKey));
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

            switch (validationResult.tagValidation)
            {
                case GnisTagValidation.OK:
                    break;
                case GnisTagValidation.FEATURE_CLASS_TAGS_MISSING:
                    result.Append(string.Format(tagMessages.allTagsMissing, allDefaultTags));
                    result.Append("\n\n");
                    break;
                case GnisTagValidation.FEATURE_CLASS_SECONDARY_TAG_MISSING:
                    result.Append(string.Format(tagMessages.secondaryTagMissing, defaultSecondaryTag));
                    result.Append("\n\n");
                    break;
                case GnisTagValidation.FEATURE_CLASS_PRIMARY_TAG_MISSING:
                    result.Append(string.Format(tagMessages.primaryTagMissing, defaultPrimaryTag));
                    result.Append("\n\n");
                    break;
                case GnisTagValidation.NOT_PROCESSED:
                    break;
                default:
                    throw new NotImplementedException();
            }

            switch (validationResult.geometryValidation)
            {
                case GnisGeometryValidation.OK:
                case GnisGeometryValidation.OK_REVERSED:
                    break;
                case GnisGeometryValidation.FEATURE_COORDINATE_EXTENT_OFF:
                case GnisGeometryValidation.FEATURE_COORDINATE_EXTENT_OFF_REVERSED:
                    if (!gnisRecord.HasSource())
                    {
                        if (matchResult.osmFeature is OsmNode)
                        {
                            result.Append(geometryMessages.extentOffPoint);
                            result.Append("\n\n");
                        }
                    }
                    else
                    {
                        result.Append(geometryMessages.extentOffLine);
                        result.Append("\n\n");
                    }
                    break;
                case GnisGeometryValidation.FEATURE_COORDINATE_START_SOURCE_OFF:
                case GnisGeometryValidation.FEATURE_COORDINATE_START_PRIMARY_OFF:
                    if (gnisRecord.HasSource())
                    {
                        result.Append(geometryMessages.startLocationOff);
                        result.Append("\n\n");
                    }
                    break;
                case GnisGeometryValidation.FEATURE_COORDINATE_END_PRIMARY_OFF:
                case GnisGeometryValidation.FEATURE_COORDINATE_END_SOURCE_OFF:
                    if (gnisRecord.HasSource())
                    {
                        result.Append(geometryMessages.endLocationOff);
                        result.Append("\n\n");
                    }
                    break;
                case GnisGeometryValidation.NOT_PROCESSED:
                    break;
                default:
                    throw new NotImplementedException();
            }

            OsmTagCollection tags = matchResult.osmFeature.GetTagCollection();
            if (tags.ContainsKey("ele"))
            {
                double osmElevation = double.TryParse(tags["ele"], out double value) ? value : -100;
                double gnisElevation = double.TryParse(gnisRecord.Elevation, out value) ? value : -200;

                if (Math.Abs(osmElevation - gnisElevation) < 2)
                {
                    result.Append($"The elevation value ele={osmElevation} ({Math.Round(osmElevation*3.28084, 0)} ft) might be incorrect. Check the value against the elevation on USGS Topo maps.");
                    result.Append("\n\n");
                }
            }

            if (tags.ContainsKey("waterway") && !tags.ContainsKey("intermittent"))
            {
                result.Append("Check the USGS Topo map to determine if this waterway is intermittent (i.e. mapped with a dashed/dotted line) and add the `intermittent=yes` tag if it is. ");
                result.Append("\n\n");
            }

            if (matchResult.osmFeature is OsmRelation)
            {
                result.Append("Remember to check the relation members for tags that might conflict with the GNIS record (e.g. different Feature IDs or names). ");
                result.Append("\n\n");
            }

            return result.ToString();
        }
        public string BuildNoMatchInstructions(GnisRecord gnisRecord)
        {
            StringBuilder result = new();

            result.Append("We didn't find an existing feature in OSM that matched the GNIS record. Sometimes the feature is mapped but doesn't have the right tags but more often the feature is not in OSM. ");
            if ("Civil".Equals(gnisRecord.FeatureClass))
            {
                if (gnisRecord.FeatureName.EndsWith("Reservation"))
                    result.Append("\n\nThis feature should be mapped as an [aboriginal lands boundary](https://wiki.openstreetmap.org/wiki/Tag:boundary%3Daboriginal_lands). If the feature is not already mapped, download the latest boundary data from the [US Census American Indian Geography Data Set](https://www.census.gov/cgi-bin/geo/shapefiles/index.php?year=2022&layergroup=American+Indian+Area+Geography) and import the boundary polygon.");
                else
                    result.Append("\n\nThis feature should be mapped as an [administrative boundary](https://wiki.openstreetmap.org/wiki/United_States/Boundaries). If the feature is not already mapped, download the latest boundary data from the [US Census Urban Areas Data Set](https://www.census.gov/cgi-bin/geo/shapefiles/index.php?year=2022&layergroup=Urban+Areas) and import the boundary polygon.");
            }
            else if ("Census".Equals(gnisRecord.FeatureClass))
            {
                result.Append("\n\nThis feature should be mapped as a [census boundary](https://wiki.openstreetmap.org/wiki/Tag:boundary%3Dcensus). If the feature is not already mapped, download the latest boundary data from the [US Census Designated Places Data Set](https://www.census.gov/cgi-bin/geo/shapefiles/index.php?year=2022&layergroup=Places) and import the boundary polygon.");
            }
            else if ("Military".Equals(gnisRecord.FeatureClass))
            {
                result.Append("\n\nThis feature should be mapped as a [military use area](https://wiki.openstreetmap.org/wiki/Tag:landuse%3Dmilitary). If the feature is not already mapped, download the latest boundary data from the [TIGER/Line Shapefiles](https://www.census.gov/cgi-bin/geo/shapefiles/index.php?year=2022&layergroup=Military+Installations) and import the boundary polygon.");
            }
            else
            {
                if (!gnisRecord.HasSource())
                    result.Append("(Editing the feature in JOSM will automatically add the feature at the GNIS coordinates.) ");
                else
                    result.Append("\n\nUse the start and end coordinates in GNIS to verify the extent of the feature. (Editing the feature in JOSM will automatically add the feature as a way between the start and end coordinates in GNIS. You will need to add nodes to the way to align the feature with USGS Topo maps.) ");
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
            if ("Civil".Equals(gnisRecord.FeatureClass))
            {
                if (gnisRecord.FeatureName.EndsWith("Reservation"))
                    result.Append("\n\nThis feature should be mapped as an [aboriginal lands boundary](https://wiki.openstreetmap.org/wiki/Tag:boundary%3Daboriginal_lands). If the feature is not already mapped, download the latest boundary data from the [US Census American Indian Geography Data Set](https://www.census.gov/cgi-bin/geo/shapefiles/index.php?year=2022&layergroup=American+Indian+Area+Geography) and import the boundary polygon.");
                else
                    result.Append("\n\nThis feature should be mapped as an [administrative boundary](https://wiki.openstreetmap.org/wiki/United_States/Boundaries). If the feature is not already mapped, download the latest boundary data from the [US Census Urban Areas Data Set](https://www.census.gov/cgi-bin/geo/shapefiles/index.php?year=2022&layergroup=Urban+Areas) and import the boundary polygon.");
            }
            else if ("Census".Equals(gnisRecord.FeatureClass))
            {
                result.Append("\n\nThis feature should be mapped as a [census boundary](https://wiki.openstreetmap.org/wiki/Tag:boundary%3Dcensus). If the feature is not already mapped, download the latest boundary data from the [US Census Designated Places Data Set](https://www.census.gov/cgi-bin/geo/shapefiles/index.php?year=2022&layergroup=Places) and import the boundary polygon.");
            }
            else if ("Military".Equals(gnisRecord.FeatureClass))
            {
                result.Append("\n\nThis feature should be mapped as a [military use area](https://wiki.openstreetmap.org/wiki/Tag:landuse%3Dmilitary). If the feature is not already mapped, download the latest boundary data from the [TIGER/Line Shapefiles](https://www.census.gov/cgi-bin/geo/shapefiles/index.php?year=2022&layergroup=Military+Installations) and import the boundary polygon.");
            }
            else
            {
                if (!gnisRecord.HasSource())
                    result.Append("\n\nUse the GNIS record to verify the location of the feature. ");
                else
                    result.Append("\n\nUse the start and end coordinates in GNIS to verify the extent of the feature. ");
            }

            return result.ToString();
        }

        internal string BuildNewRelationInstructions(GnisRecord gnisRecord, GnisMatchResult matchResult, GnisValidationResult validationResult)
        {
            StringBuilder result = new();

            int count = (matchResult.osmFeature as OsmRelation)?.Members.Count ?? 0;
            string number = count < numbers.Length ? $"{numbers[count]}" : $"{count}";
            string features = count == 1 ? "feature" : "features";

            result.Append($"The GNIS record is mapped as {number} {features} near this location. Use the information in the GNIS record to find the appropriate {features} and add a parent relation to match the GNIS record. (Editing in JOSM will automatically add a parent relation for the {features}.)\n\n");

            result.Append(BuildVariantMatchInstructions(gnisRecord, matchResult, validationResult, new FeatureIdMessages(), new FeatureNameMessages(), new FeatureTagMessages(), new FeatureGeometryMessages()));

            return result.ToString();
        }
    }
}