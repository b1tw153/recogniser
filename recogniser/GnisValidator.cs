namespace recogniser
{
    public enum GnisFeatureIdValidation
    {
        OK,
        FEATURE_ID_MISSING,
        FEATURE_ID_MISMATCH,
        FEATURE_ID_MALFORMED_VALUE,
        FEATURE_ID_EXTRANEOUS_CHARACTERS,
        FEATURE_ID_MULTIPLE_VALUES,
        FEATURE_ID_MULTIPLE_VALUES_EXTRANEOUS_CHARACTERS,
        FEATURE_ID_WRONG_KEY,
        FEATURE_ID_WRONG_KEY_EXTRANEOUS_CHARACTERS,
        FEATURE_ID_WRONG_KEY_MULTIPLE_VALUES,
        FEATURE_ID_WRONG_KEY_MULTIPLE_VALUES_EXTRANEOUS_CHARACTERS,
        NOT_PROCESSED
    }

    public enum GnisNameValidation
    {
        OK,
        FEATURE_NAME_MISSING,
        FEATURE_NAME_MISMATCH,
        FEATURE_NAME_DEPRECATED_KEY,
        FEATURE_NAME_DIFFERENT,
        FEATURE_NAME_DIFFERENT_DEPRECATED_KEY,
        NOT_PROCESSED
    }
    public enum GnisTagValidation
    {
        OK,
        FEATURE_CLASS_TAGS_MISSING,
        FEATURE_CLASS_SECONDARY_TAG_MISSING,
        FEATURE_CLASS_PRIMARY_TAG_MISSING,
        NOT_PROCESSED
    }
    public enum GnisConflictingTagValidation
    {
        OK,
        FEATURE_CLASS_CONFLICTING_TAG,
        NOT_PROCESSED
    }
    public enum GnisGeometryValidation
    {
        OK,
        FEATURE_COORDINATE_EXTENT_OFF,
        FEATURE_COORDINATE_START_SOURCE_OFF,
        FEATURE_COORDINATE_END_PRIMARY_OFF,
        NOT_PROCESSED,
        OK_REVERSED,
        FEATURE_COORDINATE_START_PRIMARY_OFF,
        FEATURE_COORDINATE_END_SOURCE_OFF,
        FEATURE_COORDINATE_EXTENT_OFF_REVERSED
    }
    public class GnisValidationResult
    {
        public OsmFeature osmFeature;
        public GnisFeatureIdValidation featureIdValidation = GnisFeatureIdValidation.NOT_PROCESSED;
        public GnisNameValidation nameValidation = GnisNameValidation.NOT_PROCESSED;
        public GnisTagValidation tagValidation = GnisTagValidation.NOT_PROCESSED;
        public GnisConflictingTagValidation conflictingTagValidation = GnisConflictingTagValidation.NOT_PROCESSED;
        public GnisGeometryValidation geometryValidation = GnisGeometryValidation.NOT_PROCESSED;

        public GnisValidationResult(OsmFeature osmFeature)
        {
            this.osmFeature = osmFeature;
        }

        public bool AllOk =>
            (featureIdValidation == GnisFeatureIdValidation.OK || featureIdValidation == GnisFeatureIdValidation.NOT_PROCESSED) &&
            (nameValidation == GnisNameValidation.OK || nameValidation == GnisNameValidation.NOT_PROCESSED) &&
            (tagValidation == GnisTagValidation.OK || tagValidation == GnisTagValidation.NOT_PROCESSED) &&
            (conflictingTagValidation == GnisConflictingTagValidation.OK || conflictingTagValidation == GnisConflictingTagValidation.NOT_PROCESSED) &&
            (geometryValidation == GnisGeometryValidation.OK || geometryValidation == GnisGeometryValidation.NOT_PROCESSED);


        public override string ToString()
        {
            List<string> result = new();

            /*
            if (featureIdValidation == GnisFeatureIdValidation.NOT_PROCESSED
                || nameValidation == GnisNameValidation.NOT_PROCESSED
                || tagValidation == GnisTagValidation.NOT_PROCESSED
                || conflictingTagValidation == GnisConflictingTagValidation.NOT_PROCESSED
                || geometryValidation == GnisGeometryValidation.NOT_PROCESSED)
                throw new Exception("All validation should have been processed.");
            */

            if (featureIdValidation != GnisFeatureIdValidation.OK && featureIdValidation != GnisFeatureIdValidation.NOT_PROCESSED)
                result.Add(featureIdValidation.ToString());
            if (nameValidation != GnisNameValidation.OK && nameValidation != GnisNameValidation.NOT_PROCESSED)
                result.Add(nameValidation.ToString());
            if (tagValidation != GnisTagValidation.OK && tagValidation != GnisTagValidation.NOT_PROCESSED)
                result.Add(tagValidation.ToString());
            if (conflictingTagValidation != GnisConflictingTagValidation.OK && conflictingTagValidation != GnisConflictingTagValidation.NOT_PROCESSED)
                result.Add(conflictingTagValidation.ToString());
            if (geometryValidation != GnisGeometryValidation.OK && geometryValidation != GnisGeometryValidation.NOT_PROCESSED)
                result.Add(geometryValidation.ToString());

            return String.Join(";", result);
        }
    }

    public class GnisValidator
    {
        private readonly GnisClassData gnisClassData;

        public GnisValidator(GnisClassData gnisClassData)
        {
            this.gnisClassData = gnisClassData;
        }

        public GnisValidationResult ValidateOsmFeature(GnisRecord gnisRecord, GnisMatchResult matchResult)
        {
            GnisValidationResult validationResult = new(matchResult.osmFeature)
            {
                featureIdValidation = ValidateFeatureId(matchResult),

                nameValidation = ValidateName(matchResult),

                tagValidation = ValidateTag(matchResult),

                conflictingTagValidation = ValidateConflictingTag(matchResult),

                geometryValidation = ValidateGeometry(matchResult)
            };

            return validationResult;
        }

        private GnisGeometryValidation ValidateGeometry(GnisMatchResult matchResult)
        {
            switch (matchResult.geometryMatch)
            {
                case GnisGeometryMatch.NOT_PROCESSED:
                    return GnisGeometryValidation.NOT_PROCESSED;
                case GnisGeometryMatch.NO_MATCH:
                    if (matchResult.osmFeature is OsmNode || matchResult.osmFeature.GetLinearExtent() != null)
                        return GnisGeometryValidation.FEATURE_COORDINATE_EXTENT_OFF;
                    else
                        // if we didn't download all of this feature
                        // or if it's a closed area
                        return GnisGeometryValidation.NOT_PROCESSED;
                case GnisGeometryMatch.FEATURE_COORDINATE_EXACT_MATCH:
                case GnisGeometryMatch.FEATURE_COORDINATE_CLOSE_MATCH:
                case GnisGeometryMatch.FEATURE_COORDINATE_EXTENT_EXACT_MATCH:
                case GnisGeometryMatch.FEATURE_COORDINATE_EXTENT_CLOSE_MATCH:
                    return GnisGeometryValidation.OK;
                case GnisGeometryMatch.FEATURE_COORDINATE_START_SOURCE_EXACT_MATCH:
                case GnisGeometryMatch.FEATURE_COORDINATE_START_SOURCE_CLOSE_MATCH:
                    return GnisGeometryValidation.FEATURE_COORDINATE_END_PRIMARY_OFF;
                case GnisGeometryMatch.FEATURE_COORDINATE_END_PRIMARY_EXACT_MATCH:
                case GnisGeometryMatch.FEATURE_COORDINATE_END_PRIMARY_CLOSE_MATCH:
                    return GnisGeometryValidation.FEATURE_COORDINATE_START_SOURCE_OFF;

                case GnisGeometryMatch.NO_MATCH_REVERSE:
                    return GnisGeometryValidation.FEATURE_COORDINATE_EXTENT_OFF_REVERSED;
                case GnisGeometryMatch.FEATURE_COORDINATE_EXTENT_REVERSE_EXACT_MATCH:
                case GnisGeometryMatch.FEATURE_COORDINATE_EXTENT_REVERSE_CLOSE_MATCH:
                    return GnisGeometryValidation.OK_REVERSED;
                case GnisGeometryMatch.FEATURE_COORDINATE_START_PRIMARY_EXACT_MATCH:
                case GnisGeometryMatch.FEATURE_COORDINATE_START_PRIMARY_CLOSE_MATCH:
                    return GnisGeometryValidation.FEATURE_COORDINATE_END_SOURCE_OFF;
                case GnisGeometryMatch.FEATURE_COORDINATE_END_SOURCE_EXACT_MATCH:
                case GnisGeometryMatch.FEATURE_COORDINATE_END_SOURCE_CLOSE_MATCH:
                    return GnisGeometryValidation.FEATURE_COORDINATE_START_PRIMARY_OFF;

                default:
                    throw new NotImplementedException();
            }
        }

        private GnisConflictingTagValidation ValidateConflictingTag(GnisMatchResult matchResult)
        {
            switch (matchResult.conflictingTagMatch)
            {
                case GnisConflictingTagMatch.NOT_PROCESSED:
                    throw new Exception("Conflicting tag match should have been processed");
                //return GnisConflictingTagValidation.NOT_PROCESSED;
                case GnisConflictingTagMatch.NO_MATCH:
                    return GnisConflictingTagValidation.OK;
                case GnisConflictingTagMatch.FEATURE_CLASS_CONFLICTING_TAG_MATCH:
                    return GnisConflictingTagValidation.FEATURE_CLASS_CONFLICTING_TAG;
                default:
                    throw new NotImplementedException();
            }
        }

        private GnisTagValidation ValidateTag(GnisMatchResult matchResult)
        {
            switch (matchResult.tagMatch)
            {
                case GnisTagMatch.NOT_PROCESSED:
                    throw new Exception("Tag match should have been processed");
                //return GnisTagValidation.NOT_PROCESSED;
                case GnisTagMatch.NO_MATCH:
                    return GnisTagValidation.FEATURE_CLASS_TAGS_MISSING;
                case GnisTagMatch.FEATURE_CLASS_ALL_TAGS_MATCH:
                    return GnisTagValidation.OK;
                case GnisTagMatch.FEATURE_CLASS_PRIMARY_TAG_MATCH:
                    return GnisTagValidation.FEATURE_CLASS_SECONDARY_TAG_MISSING;
                case GnisTagMatch.FEATURE_CLASS_SECONDARY_TAG_MATCH:
                    return GnisTagValidation.FEATURE_CLASS_PRIMARY_TAG_MISSING;
                default:
                    throw new NotImplementedException($"Missing case for GnisTagValidation.{matchResult.tagMatch}");
            }
        }

        private static readonly string[] nameTags =
        {
            "name",
            "official_name",
            "alt_name",
            "old_name",
            "loc_name",
            "name_1",
            "name_2"
        };

        private GnisNameValidation ValidateName(GnisMatchResult matchResult)
        {
            switch (matchResult.nameMatch)
            {
                case GnisNameMatch.NOT_PROCESSED:
                    throw new Exception("Name match should have been processed");
                //return GnisNameValidation.NOT_PROCESSED;
                case GnisNameMatch.NO_MATCH:
                    OsmTagCollection tags = matchResult.osmFeature.GetTagCollection();
                    bool hasName = false;
                    foreach (string nameTag in nameTags)
                    {
                        if (tags.ContainsKey(nameTag))
                        {
                            hasName = true;
                            break;
                        }
                    }
                    if (!hasName)
                        return GnisNameValidation.FEATURE_NAME_MISSING;
                    else
                        return GnisNameValidation.FEATURE_NAME_MISMATCH;
                case GnisNameMatch.FEATURE_NAME_EXACT_MATCH:
                case GnisNameMatch.FEATURE_NAME_EXACT_MATCH_OFFICIAL:
                case GnisNameMatch.FEATURE_NAME_EXACT_MATCH_ALT:
                case GnisNameMatch.FEATURE_NAME_EXACT_MATCH_OLD:
                    return GnisNameValidation.OK;
                case GnisNameMatch.FEATURE_NAME_EXACT_MATCH_LOC: // kinda cheating to leave this here but the results would be what we want
                case GnisNameMatch.FEATURE_NAME_EXACT_MATCH_1:
                case GnisNameMatch.FEATURE_NAME_EXACT_MATCH_2:
                    return GnisNameValidation.FEATURE_NAME_DEPRECATED_KEY;
                case GnisNameMatch.FEATURE_NAME_CLOSE_MATCH:
                case GnisNameMatch.FEATURE_NAME_CLOSE_MATCH_OFFICIAL:
                case GnisNameMatch.FEATURE_NAME_CLOSE_MATCH_ALT:
                case GnisNameMatch.FEATURE_NAME_CLOSE_MATCH_OLD:
                    return GnisNameValidation.FEATURE_NAME_DIFFERENT;
                case GnisNameMatch.FEATURE_NAME_CLOSE_MATCH_LOC: // kinda cheating to leave this here but the results would be what we want
                case GnisNameMatch.FEATURE_NAME_CLOSE_MATCH_1:
                case GnisNameMatch.FEATURE_NAME_CLOSE_MATCH_2:
                    return GnisNameValidation.FEATURE_NAME_DIFFERENT_DEPRECATED_KEY;
                default:
                    throw new NotImplementedException($"Missing case for GnisNameValidation.{matchResult.nameMatch}");
            }
        }

        private GnisFeatureIdValidation ValidateFeatureId(GnisMatchResult matchResult)
        {
            switch (matchResult.featureIdMatch)
            {
                case GnisFeatureIdMatch.NOT_PROCESSED:
                    throw new Exception("Feature ID match should have been processed");
                //return GnisFeatureIdValidation.NOT_PROCESSED;
                case GnisFeatureIdMatch.NO_MATCH:
                    if (!matchResult.osmFeature.GetTagCollection().ContainsKey("gnis:feature_id"))
                        return GnisFeatureIdValidation.FEATURE_ID_MISSING;
                    else
                        return GnisFeatureIdValidation.FEATURE_ID_MISMATCH;
                case GnisFeatureIdMatch.FEATURE_ID_MALFORMED_VALUE:
                    return GnisFeatureIdValidation.FEATURE_ID_MALFORMED_VALUE;
                case GnisFeatureIdMatch.FEATURE_ID_EXACT_MATCH:
                    return GnisFeatureIdValidation.OK;
                case GnisFeatureIdMatch.FEATURE_ID_EXACT_NUMERIC_MATCH:
                    return GnisFeatureIdValidation.FEATURE_ID_EXTRANEOUS_CHARACTERS;
                case GnisFeatureIdMatch.FEATURE_ID_EXACT_MATCH_SYNONYMOUS_KEY:
                case GnisFeatureIdMatch.FEATURE_ID_EXACT_MATCH_UNEXPECTED_KEY:
                    return GnisFeatureIdValidation.FEATURE_ID_WRONG_KEY;
                case GnisFeatureIdMatch.FEATURE_ID_EXACT_NUMERIC_MATCH_SYNONYMOUS_KEY:
                case GnisFeatureIdMatch.FEATURE_ID_EXACT_NUMERIC_MATCH_UNEXPECTED_KEY:
                    return GnisFeatureIdValidation.FEATURE_ID_WRONG_KEY_EXTRANEOUS_CHARACTERS;
                case GnisFeatureIdMatch.FEATURE_ID_PARTIAL_MATCH:
                    return GnisFeatureIdValidation.FEATURE_ID_MULTIPLE_VALUES;
                case GnisFeatureIdMatch.FEATURE_ID_PARTIAL_NUMERIC_MATCH:
                    return GnisFeatureIdValidation.FEATURE_ID_MULTIPLE_VALUES_EXTRANEOUS_CHARACTERS;
                case GnisFeatureIdMatch.FEATURE_ID_PARTIAL_MATCH_SYNONYMOUS_KEY:
                case GnisFeatureIdMatch.FEATURE_ID_PARTIAL_MATCH_UNEXPECTED_KEY:
                    return GnisFeatureIdValidation.FEATURE_ID_WRONG_KEY_MULTIPLE_VALUES;
                case GnisFeatureIdMatch.FEATURE_ID_PARTIAL_NUMERIC_MATCH_SYNONYMOUS_KEY:
                case GnisFeatureIdMatch.FEATURE_ID_PARTIAL_NUMERIC_MATCH_UNEXPECTED_KEY:
                    return GnisFeatureIdValidation.FEATURE_ID_WRONG_KEY_MULTIPLE_VALUES_EXTRANEOUS_CHARACTERS;
                case GnisFeatureIdMatch.FEATURE_ID_WIKIDATA_MATCH:
                    return GnisFeatureIdValidation.FEATURE_ID_MISSING;
                default:
                    throw new NotImplementedException($"Missing case for GnisFeatureIdMatch.{matchResult.featureIdMatch}");
            }
        }
    }
}