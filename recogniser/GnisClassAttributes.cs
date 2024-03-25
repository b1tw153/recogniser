namespace recogniser
{
    public class GnisClassAttributes
    {
        private string featureClass = string.Empty;
        private bool current;
        private readonly HashSet<string> geometry = new();
        private readonly List<OsmTagProto> primaryTags = new();
        private readonly List<OsmTagProto> secondaryTags = new();
        private readonly List<string> relationTypes = new();
        private readonly List<OsmTagProto> conflictingTags = new();

        public void Set(string name, string value)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(value))
                return;

            switch (name)
            {
                case "FEATURE_CLASS":
                    featureClass = value;
                    break;
                case "FEATURE_CLASS_CURRENT":
                    if ("TRUE".Equals(value))
                    {
                        current = true;
                    }
                    else if ("FALSE".Equals(value))
                    {
                        current = false;
                    }
                    break;
                case "OSM_GEOMETRY":
                    foreach (string geometryType in value.Split("|"))
                    {
                        geometry.Add(geometryType);
                    }
                    break;
                case "OSM_PRIMARY_TAGS":
                    foreach (string tag in value.Split("|"))
                    {
                        primaryTags.Add(new OsmTagProto(tag));
                    }
                    break;
                case "OSM_SECONDARY_TAGS":
                    foreach (string tag in value.Split("|"))
                    {
                        secondaryTags.Add(new OsmTagProto(tag));
                    }
                    break;
                case "OSM_RELATION_TYPES":
                    foreach (string relationType in value.Split("|"))
                    {
                        relationTypes.Add(relationType);
                    }
                    break;
                case "OSM_CONFLICTING_TAGS":
                    foreach (string tag in value.Split("|"))
                    {
                        conflictingTags.Add(new OsmTagProto(tag));
                    }
                    break;
            }
        }

        public string FeatureClass
        {
            get { return featureClass; }
        }

        public bool Current
        {
            get { return current; }
        }

        public string Geometry
        {
            get { return string.Join("|", geometry); }
        }

        public string PrimaryTags
        {
            get { return string.Join("|", primaryTags); }
        }

        public string SecondaryTags
        {
            get { return string.Join("|", secondaryTags); }
        }

        public string ConflictingTags
        {
            get { return string.Join("|", conflictingTags); }
        }

        public string RelationTypes
        {
            get { return string.Join("|", relationTypes); }
        }

        public bool HasGeometry(string geometryType)
        {
            return geometry.Contains(geometryType);
        }

        public OsmTagProto[] GetPrimaryTags()
        {
            return primaryTags.ToArray();
        }
        public OsmTagProto[] GetSecondaryTags()
        {
            return secondaryTags.ToArray();
        }
        public OsmTagProto[] GetConflictingTags()
        {
            return conflictingTags.ToArray();
        }

        public bool IsWaterwayClass()
        {
            return "Stream".Equals(featureClass) || "Arroyo".Equals(featureClass) || "Canal".Equals(featureClass);
        }

        public string DefaultPrimaryTag
        {
            get
            {
                if (primaryTags.Count == 0)
                    return string.Empty;

                OsmTagProto primaryTag = primaryTags[0];
                return primaryTag.ToString();
            }
        }

        public string DefaultSecondaryTag
        {
            get
            {
                if (secondaryTags.Count == 0)
                    return string.Empty;

                OsmTagProto secondaryTag = secondaryTags[0];
                return secondaryTag.ToString();
            }
        }

        public string DefaultRelationType
        {
            get
            {
                if (relationTypes.Count == 0)
                    return string.Empty;

                return relationTypes[0];
            }
        }

public bool MatchesPrimaryTag(string key, string value)
        {
            foreach (OsmTagProto tag in primaryTags)
                if (tag.Matches(key, value))
                    return true;

            return false;
        }

        public bool MatchesSecondaryTag(string key, string value)
        {
            foreach (OsmTagProto tag in secondaryTags)
                if (tag.Matches(key, value))
                    return true;

            return false;
        }

        public bool MatchesConflictingTag(string key, string value)
        {
            foreach (OsmTagProto tag in conflictingTags)
                if (tag.Matches(key, value))
                    return true;

            return false;
        }
    }
}