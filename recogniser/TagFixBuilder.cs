// <copyright file="TagFixBuilder.cs" company="recogniser project contributors">
// Copyright (c) 2025 recogniser project contributors.
// Licensed under the AGPL-3.0-or-later license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Recogniser
{
    using System.Globalization;

    internal sealed class TagFixBuilder
    {
        private readonly GnisClassData gnisClassData;

        public TagFixBuilder(GnisClassData gnisClassData)
        {
            this.gnisClassData = gnisClassData;
        }

        public List<TagFixOperation>? BuildTagFix(GnisRecord gnisRecord, GnisMatchResult matchResult, GnisValidationResult validationResult)
        {
            bool modified = false;

            TagFixOperation tagFix = new();
            TagFixIndependentOperation tagFixIndependentOperation = new()
            {
                Id = $"{matchResult.OsmFeature.GetOsmType()}/{matchResult.OsmFeature.Id}",
            };

            tagFix.Data = [tagFixIndependentOperation];

            modified |= ModifyFeatureId(gnisRecord, matchResult, validationResult, tagFixIndependentOperation.Operations);

            modified |= ModifyName(gnisRecord, matchResult, validationResult, tagFixIndependentOperation.Operations);

            modified |= ModifyTags(gnisRecord, matchResult, validationResult, tagFixIndependentOperation.Operations);

            if (modified)
            {
                DeleteExtraGnisTags(matchResult.OsmFeature, tagFixIndependentOperation.Operations);

                return [tagFix];
            }
            else
            {
                return null;
            }
        }

        private static bool ModifyName(GnisRecord gnisRecord, GnisMatchResult matchResult, GnisValidationResult validationResult, List<TagFixDependentOperation> operations)
        {
            TagFixDependentOperation operation;
            OsmTagCollection tags = matchResult.OsmFeature.GetTagCollection();

            switch (validationResult.NameValidation)
            {
                case GnisNameValidation.OK:
                    return false;

                case GnisNameValidation.FEATURE_NAME_MISSING:

                    // add the feature name
                    matchResult.OsmFeature.AddTag(new OsmTag("name", gnisRecord.FeatureName));
                    operation = new()
                    {
                        Operation = "setTags",
                        Data = new Dictionary<string, string>()
                        {
                            { "name", gnisRecord.FeatureName },
                        },
                    };

                    operations.Add(operation);

                    return true;

                case GnisNameValidation.FEATURE_NAME_MISMATCH:
                case GnisNameValidation.FEATURE_NAME_DIFFERENT:
                    return false;

                case GnisNameValidation.FEATURE_NAME_DEPRECATED_KEY:
                case GnisNameValidation.FEATURE_NAME_DIFFERENT_DEPRECATED_KEY:
                    // if the "name" key is unused
                    if (!tags.ContainsKey("name"))
                    {
                        // move the name to the "name" key
                        string name = matchResult.OsmFeature.GetTagCollection()[matchResult.NameKey] ?? string.Empty;

                        // delete the deprecated key
                        operation = new()
                        {
                            Operation = "unsetTags",
                            Data = new string[]
                            {
                                matchResult.NameKey,
                            },
                        };

                        operations.Add(operation);

                        // add the feature name
                        matchResult.OsmFeature.AddTag(new OsmTag("name", gnisRecord.FeatureName));
                        operation = new()
                        {
                            Operation = "setTags",
                            Data = new Dictionary<string, string>()
                            {
                                { "name", name },
                            },
                        };

                        operations.Add(operation);
                        return true;
                    }

                    return false;

                case GnisNameValidation.NOT_PROCESSED:
                    return false;

                default:
                    throw new NotImplementedException();
            }
        }

        private static void DeleteExtraGnisTags(XOsmFeature osmFeature, List<TagFixDependentOperation> operations)
        {
            List<string> tagsToDelete = [];

            OsmTagCollection tags = osmFeature.GetTagCollection();

            foreach (OsmTag tag in tags)
            {
                if (Program.ExtraGnisTags.Contains(tag.Key))
                {
                    tagsToDelete.Add(tag.Key);
                }
            }

            if (tagsToDelete.Count > 0)
            {
                TagFixDependentOperation operation = new()
                {
                    Operation = "unsetTags",
                    Data = tagsToDelete.ToArray(),
                };

                operations.Add(operation);
            }
        }

        private static bool ModifyFeatureId(GnisRecord gnisRecord, GnisMatchResult matchResult, GnisValidationResult validationResult, List<TagFixDependentOperation> operations)
        {
            TagFixDependentOperation operation;
            OsmTagCollection tags = matchResult.OsmFeature.GetTagCollection();
            string[] oldValues;
            long[] newValues;
            string newValue;

            switch (validationResult.FeatureIdValidation)
            {
                case GnisFeatureIdValidation.OK:
                    return false;

                case GnisFeatureIdValidation.FEATURE_ID_MISSING:

                    // add gnis:feature_id tag
                    operation = new()
                    {
                        Operation = "setTags",
                        Data = new Dictionary<string, string>()
                        {
                            { "gnis:feature_id", gnisRecord.FeatureId },
                        },
                    };

                    operations.Add(operation);

                    return true;

                case GnisFeatureIdValidation.FEATURE_ID_MISMATCH:
                    return false;

                case GnisFeatureIdValidation.FEATURE_ID_MALFORMED_VALUE:
                case GnisFeatureIdValidation.FEATURE_ID_EXTRANEOUS_CHARACTERS:

                    // replace gnis:feature_id tag
                    operation = new()
                    {
                        Operation = "setTags",
                        Data = new Dictionary<string, string>()
                        {
                            { "gnis:feature_id", gnisRecord.FeatureId },
                        },
                    };

                    operations.Add(operation);

                    return true;

                case GnisFeatureIdValidation.FEATURE_ID_MULTIPLE_VALUES:
                    return false;

                case GnisFeatureIdValidation.FEATURE_ID_MULTIPLE_VALUES_EXTRANEOUS_CHARACTERS:

                    // build a clean tag value
                    string? multiValue = matchResult.OsmFeature.GetTagCollection()["gnis:feature_id"];
                    oldValues = multiValue != null ? multiValue.Split(";") : Array.Empty<string>();
                    newValues = new long[oldValues.Length];

                    for (int i = 0; i < oldValues.Length; i++)
                    {
                        // dangerous, but this should not throw an exception
                        newValues[i] = long.Parse(oldValues[i], CultureInfo.InvariantCulture);
                    }

                    newValue = string.Join(";", newValues);

                    // replace gnis:feature_id tag
                    operation = new()
                    {
                        Operation = "setTags",
                        Data = new Dictionary<string, string>()
                        {
                            { "gnis:feature_id", newValue },
                        },
                    };

                    operations.Add(operation);

                    return true;

                case GnisFeatureIdValidation.FEATURE_ID_WRONG_KEY:
                case GnisFeatureIdValidation.FEATURE_ID_WRONG_KEY_EXTRANEOUS_CHARACTERS:

                    // add gnis:feature_id tag
                    operation = new()
                    {
                        Operation = "setTags",
                        Data = new Dictionary<string, string>()
                        {
                            { "gnis:feature_id", gnisRecord.FeatureId },
                        },
                    };

                    operations.Add(operation);

                    // delete the synonymous/unexpected key
                    operation = new()
                    {
                        Operation = "unsetTags",
                        Data = new string[]
                        {
                            matchResult.FeatureIdKey,
                        },
                    };

                    operations.Add(operation);

                    return true;

                case GnisFeatureIdValidation.FEATURE_ID_WRONG_KEY_MULTIPLE_VALUES:
                case GnisFeatureIdValidation.FEATURE_ID_WRONG_KEY_MULTIPLE_VALUES_EXTRANEOUS_CHARACTERS:

                    // collect the old values
                    oldValues = tags[matchResult.FeatureIdKey]?.Split(";") ?? Array.Empty<string>();

                    // build a clean value
                    newValues = new long[oldValues.Length];

                    for (int i = 0; i < oldValues.Length; i++)
                    {
                        // dangerous, but this should not throw an exception
                        newValues[i] = long.Parse(oldValues[i], CultureInfo.InvariantCulture);
                    }

                    newValue = string.Join(";", newValues);

                    // remove the old wrong tag
                    operation = new()
                    {
                        Operation = "unsetTags",
                        Data = new string[]
                        {
                            matchResult.FeatureIdKey,
                        },
                    };

                    operations.Add(operation);

                    // add gnis:feature_id tag
                    operation = new()
                    {
                        Operation = "setTags",
                        Data = new Dictionary<string, string>()
                        {
                            { "gnis:feature_id", newValue },
                        },
                    };

                    operations.Add(operation);

                    return true;

                case GnisFeatureIdValidation.NOT_PROCESSED:
                    return false;

                default:
                    throw new NotImplementedException();
            }
        }

        private bool ModifyTags(GnisRecord gnisRecord, GnisMatchResult matchResult, GnisValidationResult validationResult, List<TagFixDependentOperation> operations)
        {
            switch (validationResult.TagValidation)
            {
                case GnisTagValidation.OK:
                    return false;
                case GnisTagValidation.FEATURE_CLASS_PRIMARY_TAG_MISSING:
                    return AddPrimaryTag(gnisRecord, matchResult.OsmFeature, operations);
                case GnisTagValidation.FEATURE_CLASS_SECONDARY_TAG_MISSING:
                    return AddSecondaryTag(gnisRecord, matchResult.OsmFeature, operations);
                case GnisTagValidation.FEATURE_CLASS_TAGS_MISSING:
                    bool modified = false;
                    modified |= AddPrimaryTag(gnisRecord, matchResult.OsmFeature, operations);
                    modified |= AddSecondaryTag(gnisRecord, matchResult.OsmFeature, operations);
                    return modified;
                case GnisTagValidation.NOT_PROCESSED:
                    return false;
                default:
                    throw new NotImplementedException();
            }
        }

        private bool AddSecondaryTag(GnisRecord gnisRecord, XOsmFeature osmFeature, List<TagFixDependentOperation> operations)
        {
            GnisClassAttributes gnisClassAttributes = gnisClassData.GetGnisClassAttributes(gnisRecord.FeatureClass);

            OsmTagProto[] secondaryTags = gnisClassAttributes.GetSecondaryTags();

            if (secondaryTags.Length > 0)
            {
                OsmTagProto secondaryTag = secondaryTags[0];

                TagFixDependentOperation operation = new()
                {
                    Operation = "setTags",
                    Data = new Dictionary<string, string>()
                    {
                        { secondaryTag.Name, secondaryTag.Value },
                    },
                };

                operations.Add(operation);

                return true;
            }

            return false;
        }

        private bool AddPrimaryTag(GnisRecord gnisRecord, XOsmFeature osmFeature, List<TagFixDependentOperation> operations)
        {
            GnisClassAttributes gnisClassAttributes = gnisClassData.GetGnisClassAttributes(gnisRecord.FeatureClass);

            OsmTagProto primaryTag = gnisClassAttributes.GetPrimaryTags()[0];

            TagFixDependentOperation operation = new()
            {
                Operation = "setTags",
                Data = new Dictionary<string, string>()
                {
                    { primaryTag.Name, primaryTag.Value },
                },
            };

            operations.Add(operation);

            return true;
        }
    }
}