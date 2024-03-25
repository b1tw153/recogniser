namespace recogniser
{
    public class OsmChangeBuilder
    {
        private readonly GnisClassData gnisClassData;

        public OsmChangeBuilder(GnisClassData gnisClassData)
        {
            this.gnisClassData = gnisClassData;
        }

        /// <summary>
        /// Build an OsmChange XML string to create a new feature based on a GNIS record
        /// </summary>
        /// <param name="gnisRecord"></param>
        /// <returns></returns>
        internal string? BuildOsmChange(GnisRecord gnisRecord)
        {
            XOsmChange osmChange = new();

            AddToOsmChange(osmChange, gnisRecord);

            return osmChange.IsEmpty() ? null : osmChange.Serialize();
        }

        /// <summary>
        /// Build an OsmChange XML string for a single matched OSM feature
        /// </summary>
        /// <param name="gnisRecord"></param>
        /// <param name="matchResult"></param>
        /// <param name="validationResult"></param>
        /// <returns></returns>
        public string? BuildOsmChange(GnisRecord gnisRecord, GnisMatchResult matchResult, GnisValidationResult validationResult)
        {
            XOsmChange osmChange = new();

            AddToOsmChange(osmChange, gnisRecord, matchResult, validationResult);

            return osmChange.IsEmpty() ? null : osmChange.Serialize();
        }

        public void AddToOsmChange(XOsmChange osmChange, GnisRecord gnisRecord)
        {
            List<OsmFeature> newOsmFeatures = new();

            // don't build OsmChange XML for unmatched Civil or Census classes because they can't be mapped as points and we don't have the boundary polygons
            //if ("Civil".Equals(gnisRecord.FeatureClass) || "Census".Equals(gnisRecord.FeatureClass))
            //    return;

            XOsmData osmData = new();

            if (!gnisRecord.HasSource())
            {
                OsmNode newNode = new(gnisRecord.Primary);
                newNode.SetParent(osmData);
                newNode.AddTag(new OsmTag("name", gnisRecord.FeatureName));
                newNode.AddTag(new OsmTag("gnis:feature_id", gnisRecord.FeatureId));
                AddPrimaryTag(gnisRecord, newNode);
                AddSecondaryTag(gnisRecord, newNode);
                newOsmFeatures.Add(newNode);
            }
            else
            {
                OsmWay newWay = new();
                newWay.SetParent(osmData);
                newWay.AddTag(new OsmTag("name", gnisRecord.FeatureName));
                newWay.AddTag(new OsmTag("gnis:feature_id", gnisRecord.FeatureId));
                AddPrimaryTag(gnisRecord, newWay);
                AddSecondaryTag(gnisRecord, newWay);

                OsmNode startNode = new(gnisRecord.Source);
                startNode.SetParent(osmData);
                OsmNode endNode = new(gnisRecord.Primary);
                endNode.SetParent(osmData);
                AddStartNode(startNode, newWay, newOsmFeatures, newOsmFeatures);
                AddEndNode(endNode, newWay, newOsmFeatures, newOsmFeatures);
            }

            foreach (OsmFeature newFeature in newOsmFeatures)
                osmChange.Create(newFeature);
        }

        public void AddToOsmChange(XOsmChange osmChange, GnisRecord gnisRecord, GnisMatchResult matchResult, GnisValidationResult validationResult)
        {
            bool modified = false;
            bool created = false;
            List<OsmFeature> newOsmFeatures = new();
            List<OsmFeature> modifiedOsmFeatures = new();

            // don't modify the feature if there was a conflicting match
            if (matchResult.MatchType == GnisMatchType.conflictingMatch)
                return;

            // if the matchResult is for a newly created feature
            if (matchResult.osmFeature.Id < 0)
                created = true;

            modified |= ModifyFeatureId(gnisRecord, matchResult, validationResult);

            modified |= ModifyName(gnisRecord, matchResult, validationResult);

            modified |= ModifyTags(gnisRecord, matchResult, validationResult);

            modified |= ModifyGeometry(gnisRecord, matchResult, validationResult, newOsmFeatures, modifiedOsmFeatures);

            if (modified || created)
            {
                DeleteExtraGnisTags(matchResult.osmFeature);

                if (created)
                    osmChange.Create(matchResult.osmFeature);
                else
                    osmChange.Modify(matchResult.osmFeature);

                foreach (OsmFeature newFeature in newOsmFeatures)
                    osmChange.Create(newFeature);

                // this is ok even if the modified feature is the same as the original feature
                // because the OsmChange method enforces uniqueness
                foreach (OsmFeature modifiedFeature in modifiedOsmFeatures)
                    osmChange.Modify(modifiedFeature);
            }
        }

        private void DeleteExtraGnisTags(OsmFeature osmFeature)
        {
            OsmTagCollection tags = osmFeature.GetTagCollection();

            foreach (OsmTag tag in tags)
            {
                if (Program.extraGnisTags.Contains(tag.Key))
                    osmFeature.RemoveTag(new OsmTagProto(tag.Key, tag.Value));
            }
        }

        private static bool ModifyGeometry(GnisRecord gnisRecord, GnisMatchResult matchResult, GnisValidationResult validationResult, List<OsmFeature> newOsmFeatures, List<OsmFeature> modifiedOsmFeatures)
        {
            bool modified = false;

            OsmNode? startNode;
            OsmNode? endNode;

            if (!matchResult.GeometryReversed)
            {
                startNode = gnisRecord.HasSource() ? new(gnisRecord.Source) : null;
                startNode?.SetParent(matchResult.osmFeature.GetParent());
                endNode = new(gnisRecord.Primary);
                endNode.SetParent(matchResult.osmFeature.GetParent());
            }
            else
            {
                startNode = new(gnisRecord.Primary);
                startNode.SetParent(matchResult.osmFeature.GetParent());
                endNode = gnisRecord.HasSource() ? new(gnisRecord.Source) : null;
                endNode?.SetParent(matchResult.osmFeature.GetParent());
            }

            // note that this method can add duplicate features to modifiedOsmFeatures
            // this is ok because the OsmChange methods enforce uniqueness

            switch (validationResult.geometryValidation)
            {
                case GnisGeometryValidation.OK:
                case GnisGeometryValidation.OK_REVERSED:
                    return false;

                case GnisGeometryValidation.FEATURE_COORDINATE_EXTENT_OFF:
                case GnisGeometryValidation.FEATURE_COORDINATE_EXTENT_OFF_REVERSED:

                    /*
                    // if the feature is reversed
                    if (validationResult.geometryValidation == GnisGeometryValidation.FEATURE_COORDINATE_EXTENT_OFF_REVERSED)
                    {
                        modified |= Reverse(gnisRecord, matchResult.osmFeature, newOsmFeatures, modifiedOsmFeatures);
                    }
                    */

                    if (gnisRecord.HasSource())
                    {
                        if (matchResult.osmFeature is not OsmNode osmNode)
                        {
                            // propose new start node
                            modified |= AddStartNode(startNode, matchResult.osmFeature, newOsmFeatures, modifiedOsmFeatures);

                            // propose new end node
                            modified |= AddEndNode(endNode, matchResult.osmFeature, newOsmFeatures, modifiedOsmFeatures);

                            return modified;
                        }
                        else
                        {
                            // convert the feature to a way
                            OsmWay newWay = new();
                            newWay.SetParent(matchResult.osmFeature.GetParent());
                            newWay.AddEndNode(osmNode);

                            // move the tags to the way
                            newWay.Tags = osmNode.Tags;
                            osmNode.Tags = new();

                            // add new start and end nodes
                            AddStartNode(startNode, newWay, newOsmFeatures, modifiedOsmFeatures);
                            AddEndNode(endNode, newWay, newOsmFeatures, modifiedOsmFeatures);

                            return true;
                        }
                    }
                    else
                    {
                        if (matchResult.osmFeature is OsmNode osmNode)
                        {
                            // propose new coordinates
                            osmNode.SetCoordinate(gnisRecord.Primary);

                            modifiedOsmFeatures.Add(osmNode);

                            return true;
                        }
                        // otherwise make no modifications
                        return false;
                    }

                case GnisGeometryValidation.FEATURE_COORDINATE_END_SOURCE_OFF:
                case GnisGeometryValidation.FEATURE_COORDINATE_END_PRIMARY_OFF:

                    /*
                    // if the feature is reversed
                    if (validationResult.geometryValidation == GnisGeometryValidation.FEATURE_COORDINATE_START_PRIMARY_OFF)
                    {
                        modified |= Reverse(gnisRecord, matchResult.osmFeature, newOsmFeatures, modifiedOsmFeatures);
                    }
                    */

                    // propose new end coordinates
                    if (gnisRecord.HasSource())
                    {
                        if (matchResult.osmFeature is not OsmNode osmNode)
                        {
                            return modified | AddEndNode(endNode, matchResult.osmFeature, newOsmFeatures, modifiedOsmFeatures);
                        }
                        else
                        {
                            // convert the feature to a way
                            OsmWay newWay = new();
                            newWay.SetParent(matchResult.osmFeature.GetParent());
                            newWay.AddStartNode(osmNode);

                            // move the tags to the way
                            newWay.Tags = osmNode.Tags;
                            osmNode.Tags = new();

                            // add new end node
                            AddEndNode(endNode, newWay, newOsmFeatures, modifiedOsmFeatures);

                            return true;
                        }
                    }
                    else
                        return modified;

                case GnisGeometryValidation.FEATURE_COORDINATE_START_PRIMARY_OFF:
                case GnisGeometryValidation.FEATURE_COORDINATE_START_SOURCE_OFF:

                    /*
                    // if the feature is reversed
                    if (validationResult.geometryValidation == GnisGeometryValidation.FEATURE_COORDINATE_START_PRIMARY_OFF)
                    {
                        modified |= Reverse(gnisRecord, matchResult.osmFeature, newOsmFeatures, modifiedOsmFeatures);
                    }
                    */

                    // propose new start coordinates
                    if (gnisRecord.HasSource())
                    {
                        if (matchResult.osmFeature is not OsmNode osmNode)
                        {
                            return modified | AddStartNode(startNode, matchResult.osmFeature, newOsmFeatures, modifiedOsmFeatures);
                        }
                        else
                        {
                            // convert the feature to a way
                            OsmWay newWay = new();
                            newWay.SetParent(matchResult.osmFeature.GetParent());
                            newWay.AddEndNode(osmNode);

                            // move the tags to the way
                            newWay.Tags = osmNode.Tags;
                            osmNode.Tags = new();

                            // add new start node
                            AddStartNode(startNode, newWay, newOsmFeatures, modifiedOsmFeatures);

                            return true;
                        }
                    }
                    else
                        return modified;

                case GnisGeometryValidation.NOT_PROCESSED:
                    return false;

                default:
                    throw new NotImplementedException();
            }
        }

        private static bool AddStartNode(OsmNode? startNode, OsmFeature osmFeature, List<OsmFeature> newOsmFeatures, List<OsmFeature> modifiedOsmFeatures)
        {
            if (startNode != null && osmFeature is not OsmNode)
            {
                newOsmFeatures.Add(startNode);

                OsmFeature? modifiedWay = osmFeature.AddStartNode(startNode);
                if (modifiedWay != null)
                    modifiedOsmFeatures.Add(modifiedWay);

                return true;
            }
            return false;
        }

        private static bool AddEndNode(OsmNode? endNode, OsmFeature osmFeature, List<OsmFeature> newOsmFeatures, List<OsmFeature> modifiedOsmFeatures)
        {
            if (endNode != null && osmFeature is not OsmNode)
            {
                newOsmFeatures.Add(endNode);

                OsmFeature? modifiedWay = osmFeature.AddEndNode(endNode);
                if (modifiedWay != null)
                    modifiedOsmFeatures.Add(modifiedWay);

                return true;
            }
            return false;
        }

        /*
        private static bool Reverse(GnisRecord gnisRecord, OsmFeature osmFeature, List<OsmFeature> newOsmFeatures, List<OsmFeature> modifiedOsmFeatures)
        {
            // reverse the feature
            List<OsmFeature>? reversedFeatures = osmFeature.Reverse();

            if (reversedFeatures != null)
            {
                modifiedOsmFeatures.AddRange(reversedFeatures);
                return true;
            }
            return false;
        }
        */

        private bool ModifyTags(GnisRecord gnisRecord, GnisMatchResult matchResult, GnisValidationResult validationResult)
        {
            switch (validationResult.tagValidation)
            {
                case GnisTagValidation.OK:
                    return false;
                case GnisTagValidation.FEATURE_CLASS_PRIMARY_TAG_MISSING:
                    return AddPrimaryTag(gnisRecord, matchResult.osmFeature);
                case GnisTagValidation.FEATURE_CLASS_SECONDARY_TAG_MISSING:
                    return AddSecondaryTag(gnisRecord, matchResult.osmFeature);
                case GnisTagValidation.FEATURE_CLASS_TAGS_MISSING:
                    bool modified = false;
                    modified |= AddPrimaryTag(gnisRecord, matchResult.osmFeature);
                    modified |= AddSecondaryTag(gnisRecord, matchResult.osmFeature);
                    return modified;
                case GnisTagValidation.NOT_PROCESSED:
                    return false;
                default:
                    throw new NotImplementedException();
            }
        }

        private bool AddPrimaryTag(GnisRecord gnisRecord, OsmFeature osmFeature)
        {
            GnisClassAttributes gnisClassAttributes = gnisClassData.GetGnisClassAttributes(gnisRecord.FeatureClass);

            OsmTagProto primaryTag = gnisClassAttributes.GetPrimaryTags()[0];

            osmFeature.AddTag(new OsmTag(primaryTag.Name, primaryTag.Value));

            return true;
        }

        private bool AddSecondaryTag(GnisRecord gnisRecord, OsmFeature osmFeature)
        {
            GnisClassAttributes gnisClassAttributes = gnisClassData.GetGnisClassAttributes(gnisRecord.FeatureClass);

            OsmTagProto[] secondaryTags = gnisClassAttributes.GetSecondaryTags();

            if (secondaryTags.Length > 0)
            {
                OsmTagProto secondaryTag = secondaryTags[0];

                osmFeature.AddTag(new OsmTag(secondaryTag.Name, secondaryTag.Value));

                return true;
            }

            return false;
        }

        private static bool ModifyName(GnisRecord gnisRecord, GnisMatchResult matchResult, GnisValidationResult validationResult)
        {
            OsmTagCollection tags = matchResult.osmFeature.GetTagCollection();

            switch (validationResult.nameValidation)
            {
                case GnisNameValidation.OK:
                    return false;

                case GnisNameValidation.FEATURE_NAME_MISSING:

                    // add the feature name
                    matchResult.osmFeature.AddTag(new OsmTag("name", gnisRecord.FeatureName));

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
                        string name = matchResult.osmFeature.GetTagCollection()[matchResult.nameKey] ?? string.Empty;
                        matchResult.osmFeature.AddTag(new OsmTag("name", name));
                        matchResult.osmFeature.RemoveTag(new OsmTagProto(matchResult.nameKey, "*"));
                        return true;
                    }
                    return false;

                case GnisNameValidation.NOT_PROCESSED:
                    return false;

                default:
                    throw new NotImplementedException();
            }
        }

        private static bool ModifyFeatureId(GnisRecord gnisRecord, GnisMatchResult matchResult, GnisValidationResult validationResult)
        {
            OsmTagCollection tags = matchResult.osmFeature.GetTagCollection();
            string[] oldValues;
            long[] newValues;
            string newValue;

            switch (validationResult.featureIdValidation)
            {
                case GnisFeatureIdValidation.OK:
                    return false;

                case GnisFeatureIdValidation.FEATURE_ID_MISSING:

                    // add gnis:feature_id tag
                    matchResult.osmFeature.AddTag(new OsmTag("gnis:feature_id", gnisRecord.FeatureId));

                    return true;

                case GnisFeatureIdValidation.FEATURE_ID_MISMATCH:
                    return false;

                case GnisFeatureIdValidation.FEATURE_ID_MALFORMED_VALUE:
                case GnisFeatureIdValidation.FEATURE_ID_EXTRANEOUS_CHARACTERS:

                    // remove the old gnis:feature_id tag
                    matchResult.osmFeature.RemoveTag(new OsmTagProto("gnis:feature_id", "*"));

                    // add gnis:feature_id tag
                    matchResult.osmFeature.AddTag(new OsmTag("gnis:feature_id", gnisRecord.FeatureId));

                    return true;

                case GnisFeatureIdValidation.FEATURE_ID_MULTIPLE_VALUES:
                    return false;

                case GnisFeatureIdValidation.FEATURE_ID_MULTIPLE_VALUES_EXTRANEOUS_CHARACTERS:

                    // build a clean tag value
                    string? multiValue = matchResult.osmFeature.GetTagCollection()["gnis:feature_id"];
                    oldValues = multiValue != null ? multiValue.Split(";") : Array.Empty<string>();
                    newValues = new long[oldValues.Length];

                    for (int i = 0; i < oldValues.Length; i++)
                    {
                        // dangerous, but this should not throw an exception
                        newValues[i] = long.Parse(oldValues[i]);
                    }

                    newValue = string.Join(";", newValues);

                    // remove the old gnis:feature_id tag
                    matchResult.osmFeature.RemoveTag(new OsmTagProto("gnis:feature_id", "*"));

                    // add gnis:feature_id tag
                    matchResult.osmFeature.AddTag(new OsmTag("gnis:feature_id", newValue));

                    return true;

                case GnisFeatureIdValidation.FEATURE_ID_WRONG_KEY:
                case GnisFeatureIdValidation.FEATURE_ID_WRONG_KEY_EXTRANEOUS_CHARACTERS:

                    // add gnis:feature_id tag
                    matchResult.osmFeature.AddTag(new OsmTag("gnis:feature_id", gnisRecord.FeatureId));

                    // delete the synonymous/unexpected key
                    matchResult.osmFeature.RemoveTag(new OsmTagProto(matchResult.featureIdKey, "*"));

                    return true;

                case GnisFeatureIdValidation.FEATURE_ID_WRONG_KEY_MULTIPLE_VALUES:
                case GnisFeatureIdValidation.FEATURE_ID_WRONG_KEY_MULTIPLE_VALUES_EXTRANEOUS_CHARACTERS:

                    // collect the old values
                    oldValues = tags[matchResult.featureIdKey]?.Split(";") ?? Array.Empty<string>();

                    // build a clean value
                    newValues = new long[oldValues.Length];

                    for (int i = 0; i < oldValues.Length; i++)
                    {
                        // dangerous, but this should not throw an exception
                        newValues[i] = long.Parse(oldValues[i]);
                    }

                    newValue = string.Join(";", newValues);

                    // remove the old wrong tag
                    matchResult.osmFeature.RemoveTag(new OsmTagProto(matchResult.featureIdKey, "*"));

                    // add gnis:feature_id tag
                    matchResult.osmFeature.AddTag(new OsmTag("gnis:feature_id", newValue));

                    return true;

                case GnisFeatureIdValidation.NOT_PROCESSED:
                    return false;

                default:
                    throw new NotImplementedException();
            }
        }

    }
}