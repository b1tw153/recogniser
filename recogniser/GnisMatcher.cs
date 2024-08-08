using GeoCoordinatePortable;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace recogniser
{
    public enum GnisMatchType
    {
        noMatch,
        conflictingMatch,
        closeMatch,
        exactMatch
    }

    public enum GnisFeatureIdMatch
    {
        NOT_PROCESSED,
        NO_MATCH,
        FEATURE_ID_MALFORMED_VALUE,
        FEATURE_ID_EXACT_MATCH,
        FEATURE_ID_EXACT_NUMERIC_MATCH,
        FEATURE_ID_PARTIAL_MATCH,
        FEATURE_ID_PARTIAL_NUMERIC_MATCH,
        FEATURE_ID_EXACT_MATCH_SYNONYMOUS_KEY,
        FEATURE_ID_EXACT_MATCH_UNEXPECTED_KEY,
        FEATURE_ID_EXACT_NUMERIC_MATCH_SYNONYMOUS_KEY,
        FEATURE_ID_EXACT_NUMERIC_MATCH_UNEXPECTED_KEY,
        FEATURE_ID_WIKIDATA_MATCH,
        FEATURE_ID_PARTIAL_MATCH_SYNONYMOUS_KEY,
        FEATURE_ID_PARTIAL_MATCH_UNEXPECTED_KEY,
        FEATURE_ID_PARTIAL_NUMERIC_MATCH_SYNONYMOUS_KEY,
        FEATURE_ID_PARTIAL_NUMERIC_MATCH_UNEXPECTED_KEY
    }

    public enum GnisNameMatch
    {
        NOT_PROCESSED,
        NO_MATCH,
        FEATURE_NAME_EXACT_MATCH,
        FEATURE_NAME_EXACT_MATCH_OFFICIAL,
        FEATURE_NAME_EXACT_MATCH_ALT,
        FEATURE_NAME_EXACT_MATCH_OLD,
        FEATURE_NAME_EXACT_MATCH_LOC,
        FEATURE_NAME_EXACT_MATCH_1,
        FEATURE_NAME_EXACT_MATCH_2,
        FEATURE_NAME_CLOSE_MATCH,
        FEATURE_NAME_CLOSE_MATCH_OFFICIAL,
        FEATURE_NAME_CLOSE_MATCH_ALT,
        FEATURE_NAME_CLOSE_MATCH_OLD,
        FEATURE_NAME_CLOSE_MATCH_LOC,
        FEATURE_NAME_CLOSE_MATCH_1,
        FEATURE_NAME_CLOSE_MATCH_2
    }

    public enum GnisTagMatch
    {
        NOT_PROCESSED,
        NO_MATCH,
        FEATURE_CLASS_ALL_TAGS_MATCH,
        FEATURE_CLASS_PRIMARY_TAG_MATCH,
        FEATURE_CLASS_SECONDARY_TAG_MATCH
    }

    public enum GnisGeometryMatch
    {
        NOT_PROCESSED,
        NO_MATCH,
        FEATURE_COORDINATE_EXACT_MATCH,
        FEATURE_COORDINATE_CLOSE_MATCH,
        FEATURE_COORDINATE_EXTENT_EXACT_MATCH,
        FEATURE_COORDINATE_EXTENT_CLOSE_MATCH,
        FEATURE_COORDINATE_START_SOURCE_EXACT_MATCH,
        FEATURE_COORDINATE_START_SOURCE_CLOSE_MATCH,
        FEATURE_COORDINATE_END_PRIMARY_EXACT_MATCH,
        FEATURE_COORDINATE_END_PRIMARY_CLOSE_MATCH,
        FEATURE_COORDINATE_EXTENT_REVERSE_EXACT_MATCH,
        FEATURE_COORDINATE_EXTENT_REVERSE_CLOSE_MATCH,
        FEATURE_COORDINATE_START_PRIMARY_EXACT_MATCH,
        FEATURE_COORDINATE_START_PRIMARY_CLOSE_MATCH,
        FEATURE_COORDINATE_END_SOURCE_EXACT_MATCH,
        FEATURE_COORDINATE_END_SOURCE_CLOSE_MATCH,
        NO_MATCH_REVERSE
    }

    public enum GnisConflictingTagMatch
    {
        NOT_PROCESSED,
        NO_MATCH,
        FEATURE_CLASS_CONFLICTING_TAG_MATCH
    }

    public enum GnisMatchSpecialCondition
    {
        NO_SPECIAL_CONDITION,
        NEW_RELATION,
        MODIFIED_RELATION
    }

    public class GnisMatchResult
    {
        public OsmFeature osmFeature;
        //public GnisMatchType matchType = GnisMatchType.noMatch;
        public GnisFeatureIdMatch featureIdMatch = GnisFeatureIdMatch.NOT_PROCESSED;
        public GnisNameMatch nameMatch = GnisNameMatch.NOT_PROCESSED;
        public GnisTagMatch tagMatch = GnisTagMatch.NOT_PROCESSED;
        public GnisGeometryMatch geometryMatch = GnisGeometryMatch.NOT_PROCESSED;
        public GnisConflictingTagMatch conflictingTagMatch = GnisConflictingTagMatch.NOT_PROCESSED;
        public GnisMatchSpecialCondition specialCondition = GnisMatchSpecialCondition.NO_SPECIAL_CONDITION;
        public string featureIdKey = string.Empty;
        public string nameKey = string.Empty;
        public string primaryTagKey = string.Empty;
        public string secondaryTagKey = string.Empty;
        public string conflictingTag = string.Empty;

        public bool FeatureIdMatched =>
            featureIdMatch != GnisFeatureIdMatch.NO_MATCH &&
            featureIdMatch != GnisFeatureIdMatch.NOT_PROCESSED;
        public bool NameMatched =>
            nameMatch != GnisNameMatch.NO_MATCH &&
            nameMatch != GnisNameMatch.NOT_PROCESSED;
        public bool ExactNameMatched =>
            nameMatch == GnisNameMatch.FEATURE_NAME_EXACT_MATCH ||
            nameMatch == GnisNameMatch.FEATURE_NAME_EXACT_MATCH_1 ||
            nameMatch == GnisNameMatch.FEATURE_NAME_EXACT_MATCH_2 ||
            nameMatch == GnisNameMatch.FEATURE_NAME_EXACT_MATCH_ALT ||
            nameMatch == GnisNameMatch.FEATURE_NAME_EXACT_MATCH_LOC ||
            nameMatch == GnisNameMatch.FEATURE_NAME_EXACT_MATCH_OFFICIAL ||
            nameMatch == GnisNameMatch.FEATURE_NAME_EXACT_MATCH_OLD;

        public bool TagMatched =>
            tagMatch != GnisTagMatch.NO_MATCH &&
            tagMatch != GnisTagMatch.NOT_PROCESSED;
        public bool PrimaryTagMatched =>
            tagMatch == GnisTagMatch.FEATURE_CLASS_ALL_TAGS_MATCH ||
            tagMatch == GnisTagMatch.FEATURE_CLASS_PRIMARY_TAG_MATCH;
        public bool ConflictingTagMatched =>
            conflictingTagMatch != GnisConflictingTagMatch.NO_MATCH &&
            conflictingTagMatch != GnisConflictingTagMatch.NOT_PROCESSED;
        public bool GeometryMatched =>
            geometryMatch != GnisGeometryMatch.NO_MATCH &&
            geometryMatch != GnisGeometryMatch.NO_MATCH_REVERSE &&
            geometryMatch != GnisGeometryMatch.NOT_PROCESSED;

        public bool GeometryReversed =>
            geometryMatch == GnisGeometryMatch.FEATURE_COORDINATE_EXTENT_REVERSE_EXACT_MATCH ||
            geometryMatch == GnisGeometryMatch.FEATURE_COORDINATE_EXTENT_REVERSE_CLOSE_MATCH ||
            geometryMatch == GnisGeometryMatch.FEATURE_COORDINATE_START_PRIMARY_CLOSE_MATCH ||
            geometryMatch == GnisGeometryMatch.FEATURE_COORDINATE_START_PRIMARY_EXACT_MATCH ||
            geometryMatch == GnisGeometryMatch.FEATURE_COORDINATE_END_SOURCE_CLOSE_MATCH ||
            geometryMatch == GnisGeometryMatch.FEATURE_COORDINATE_END_SOURCE_EXACT_MATCH ||
            geometryMatch == GnisGeometryMatch.NO_MATCH_REVERSE;

        public GnisMatchType MatchType
        {
            get
            {
                if (!ConflictingTagMatched)
                {
                    if (FeatureIdMatched)
                        return GnisMatchType.exactMatch;
                    else if (ExactNameMatched && PrimaryTagMatched)
                        return GnisMatchType.exactMatch;
                    else if (NameMatched && TagMatched)
                        return GnisMatchType.closeMatch;
                    else if (NameMatched && GeometryMatched)
                        return GnisMatchType.closeMatch;
                    else if (PrimaryTagMatched && GeometryMatched)
                        return GnisMatchType.closeMatch;
                    else
                        return GnisMatchType.noMatch;
                }
                else
                {
                    if (FeatureIdMatched)
                        return GnisMatchType.conflictingMatch;
                    else if (ExactNameMatched && PrimaryTagMatched)
                        return GnisMatchType.conflictingMatch;
                    else if (NameMatched && TagMatched)
                        return GnisMatchType.conflictingMatch;
                    else if (NameMatched && GeometryMatched)
                        return GnisMatchType.noMatch;
                    else if (PrimaryTagMatched && GeometryMatched)
                        return GnisMatchType.noMatch;
                    else
                        return GnisMatchType.noMatch;
                }
            }
        }

        public GnisMatchResult(OsmFeature osmFeature)
        {
            this.osmFeature = osmFeature;
        }

        public override string ToString()
        {
            List<string> result = new();

            if (FeatureIdMatched)
                result.Add(featureIdMatch.ToString());
            if (NameMatched)
                result.Add(nameMatch.ToString());
            if (TagMatched)
                result.Add(tagMatch.ToString());
            if (ConflictingTagMatched)
                result.Add(conflictingTagMatch.ToString());
            if (GeometryMatched)
                result.Add(geometryMatch.ToString());

            return String.Join(";", result);
        }
    }

    public partial class GnisMatcher
    {
        private readonly GnisClassData _gnisClassData;

        public GnisMatcher(GnisClassData gnisClassData)
        {
            _gnisClassData = gnisClassData;
        }

        public List<GnisMatchResult> GetMatchResults(GnisRecord gnisRecord, XOsmData? osmData)
        {
            List<GnisMatchResult> results = new();

            if (osmData == null)
                return results;

            osmData.CollectNodesAndWays();

            foreach (OsmFeature osmFeature in osmData.GetFeatures())
            {
                GnisMatchResult matchResults = MatchOsmFeature(gnisRecord, osmFeature);

                if (matchResults.MatchType != GnisMatchType.noMatch)
                    results.Add(matchResults);
            }

            return results;
        }

        public GnisMatchResult MatchOsmFeature(GnisRecord gnisRecord, OsmFeature osmFeature)
        {
            GnisMatchResult result = new(osmFeature);

            result.featureIdMatch = MatchGnisFeatureId(gnisRecord, osmFeature, out result.featureIdKey);

            if (result.FeatureIdMatched)
            {
                Program.Verbose.WriteLine($"{osmFeature.GetOsmType()} {osmFeature.Id} {result.featureIdMatch}");
            }

            result.nameMatch = MatchGnisFeatureName(gnisRecord, osmFeature, out result.nameKey);

            if (result.NameMatched)
            {
                Program.Verbose.WriteLine($"{osmFeature.GetOsmType()} {osmFeature.Id} {result.nameMatch}");
            }

            result.tagMatch = MatchGnisFeatureClass(gnisRecord, osmFeature, out result.primaryTagKey, out result.secondaryTagKey);

            if (result.TagMatched)
            {
                Program.Verbose.WriteLine($"{osmFeature.GetOsmType()} {osmFeature.Id} {result.tagMatch}");
            }

            result.conflictingTagMatch = CheckForConflictingTags(gnisRecord, osmFeature, out result.conflictingTag);

            if (result.ConflictingTagMatched)
            {
                Program.Verbose.WriteLine($"{osmFeature.GetOsmType()} {osmFeature.Id} {result.conflictingTagMatch}");
            }

            result.geometryMatch = GnisGeometryMatch.NOT_PROCESSED;

            // special case for the Locale, Building, School, Church, and Hospital classes in GNIS
            // don't do the geometry match unless the osmFeature also has name match
            // because there are lots of buildings in urban areas
            // and a generic tag and geometry match is not enough to identify a matching feature
            bool noGeometryAndTagMatchForFeatureClass = (
                "Building".Equals(gnisRecord.FeatureClass) ||
                "Church".Equals(gnisRecord.FeatureClass) ||
                "Hospital".Equals(gnisRecord.FeatureClass) ||
                "Locale".Equals(gnisRecord.FeatureClass) ||
                "School".Equals(gnisRecord.FeatureClass)
                );

            // geometry match is intensive; don't do it unless there's a reason to
            // bool doGeometryMatch = !result.isMatch && (result.nameMatch != null || result.tagMatch != null);
            // bool doGeometryMatch = (result.matchType == MatchType.noMatch && result.conflictingTagMatch == null);
            // we want to do a geometry match if we already know this is a matching feature so that we can use the results for validation
            // if this is not a matching feature, the only way a geometry match would make a difference is if we already have a name match or primary tag match
            bool doGeometryMatch = /*!result.ConflictingTagMatched &&*/ (
                result.MatchType != GnisMatchType.noMatch ||
                result.NameMatched ||
                (result.PrimaryTagMatched && !noGeometryAndTagMatchForFeatureClass)
                );

            if (doGeometryMatch || Program.AlwaysMatchGeometry)
            {
                result.geometryMatch = MatchGnisFeatureCoordinates(gnisRecord, osmFeature);

                if (result.GeometryMatched)
                {
                    Program.Verbose.WriteLine($"{osmFeature.GetOsmType()} {osmFeature.Id} {result.geometryMatch}");
                }
            }

            return result;
        }

        public static GnisMatchResult? FindBestMatch(List<GnisMatchResult> matchResults)
        {
            // find all the relations in the match result
            List<GnisMatchResult> relationMatches = matchResults.FindAll(match => match.osmFeature is OsmRelation);

            // if only one result is a relation
            if (relationMatches.Count == 1)
            {
                // find out if all the other match results are members of the relation
                OsmRelation relation = (OsmRelation)relationMatches[0].osmFeature;
                bool containsAllMembers = true;

                // for each of the non-relation match results
                foreach (GnisMatchResult match in matchResults.FindAll(m => m.osmFeature is not OsmRelation))
                {
                    // if the ID of the match result is not in the list of members of the relation
                    if (relation.Members.Find(member => member.Ref == match.osmFeature.Id) == null)
                    {
                        containsAllMembers = false;
                        break;
                    }
                }

                if (containsAllMembers)
                    return relationMatches[0];
            }

            // if only one result has a feature id match
            List<GnisMatchResult> featureIdMatches = matchResults.FindAll(match => match.FeatureIdMatched);
            if (featureIdMatches.Count == 1)
                return featureIdMatches[0];

            // if only one result has an exact name match
            List<GnisMatchResult> exactNameMatches = matchResults.FindAll(match => match.ExactNameMatched);
            if (exactNameMatches.Count == 1)
                return exactNameMatches[0];

            // if only one result has a close name match
            List<GnisMatchResult> closeNameMatches = matchResults.FindAll(match => match.NameMatched);
            if (closeNameMatches.Count == 1)
                return closeNameMatches[0];

            return null;
        }

        [GeneratedRegex("^\\w+ of (.*)$")]
        private static partial Regex CivilOfRegex();


        [GeneratedRegex("^(.*) Census Designated Place$")]
        private static partial Regex CensusDesignatedPlaceRegex();

        private static GnisNameMatch MatchGnisFeatureName(GnisRecord gnisRecord, OsmFeature osmFeature, out string nameKey)
        {
            OsmTagCollection tags = osmFeature.GetTagCollection();
            GnisNameMatch result /*= GnisNameMatch.NOT_PROCESSED*/;

            // no tags
            if (tags == null)
            {
                nameKey = string.Empty;
                return GnisNameMatch.NO_MATCH;
            }

            /*
            // no name tag
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
            {
                nameKey = string.Empty;
                return GnisNameMatch.NO_NAME;
            }
            */

            // try to match the name in the record against possible name tags in the feature
            result = MatchNameTags(gnisRecord.FeatureName, tags, out nameKey);

            // if there was a match, return the result
            if (result != GnisNameMatch.NO_MATCH)
                return result;

            // otherwise try removing common prefixes and suffixes

            // for feature classes with names of settlements
            if (gnisRecord.FeatureClass.Equals("Census") || gnisRecord.FeatureClass.Equals("Civil") || gnisRecord.FeatureClass.Equals("Populated Place"))
            {
                // Township/Town/City/Village/* of ...
                Regex commonPrefixes = CivilOfRegex();

                // see if the name has one of the common prefixes
                MatchCollection matches = commonPrefixes.Matches(gnisRecord.FeatureName);

                // one match if there's a prefix, zero matches if not
                if (matches.Count == 1)
                {
                    // try to match the name without the prefix
                    result = MatchNameTags(matches[0].Groups[1].Value, tags, out nameKey);

                    // if there was a match, return the result
                    if (result != GnisNameMatch.NO_MATCH)
                        return result;
                }
            }

            // for census designated places
            if (gnisRecord.FeatureClass.Equals("Census"))
            {
                // ... Census Designated Place
                Regex commonSuffixes = CensusDesignatedPlaceRegex();

                // see if the name has one of the common suffixes
                MatchCollection matches = commonSuffixes.Matches(gnisRecord.FeatureName);

                // one match if there's a suffix, zero matches if not
                if (matches.Count == 1)
                {
                    // try to match the name without the suffix
                    result = MatchNameTags(matches[0].Groups[1].Value, tags, out nameKey);

                    // if there was a match, return the result
                    if (result != GnisNameMatch.NO_MATCH)
                        return result;
                }
            }

            // no match
            nameKey = string.Empty;
            return GnisNameMatch.NO_MATCH;
        }

        private static GnisNameMatch MatchNameTags(String featureName, OsmTagCollection tags, out string nameKey)
        {
            const double levRatio = 0.22;

            // exact name match
            if (featureName.Equals(tags["name"], StringComparison.InvariantCultureIgnoreCase))
            {
                nameKey = "name";
                return GnisNameMatch.FEATURE_NAME_EXACT_MATCH;
            }

            // official name match
            if (featureName.Equals(tags["official_name"], StringComparison.InvariantCultureIgnoreCase))
            {
                nameKey = "official_name";
                return GnisNameMatch.FEATURE_NAME_EXACT_MATCH_OFFICIAL;
            }

            // alt name match
            if (featureName.Equals(tags["alt_name"], StringComparison.InvariantCultureIgnoreCase))
            {
                nameKey = "alt_name";
                return GnisNameMatch.FEATURE_NAME_EXACT_MATCH_ALT;
            }

            // old name match
            if (featureName.Equals(tags["old_name"], StringComparison.InvariantCultureIgnoreCase))
            {
                nameKey = "old_name";
                return GnisNameMatch.FEATURE_NAME_EXACT_MATCH_OLD;
            }

            // local name match
            if (featureName.Equals(tags["old_name"], StringComparison.InvariantCultureIgnoreCase))
            {
                nameKey = "loc_name";
                return GnisNameMatch.FEATURE_NAME_EXACT_MATCH_LOC;
            }

            // name_1 match
            if (featureName.Equals(tags["name_1"], StringComparison.InvariantCultureIgnoreCase))
            {
                nameKey = "name_1";
                return GnisNameMatch.FEATURE_NAME_EXACT_MATCH_1;
            }

            // name_2 match
            if (featureName.Equals(tags["name_2"], StringComparison.InvariantCultureIgnoreCase))
            {
                nameKey = "name_2";
                return GnisNameMatch.FEATURE_NAME_EXACT_MATCH_2;
            }

            // name near match
            if ((LevenshteinDistance(featureName, tags["name"]) / (double)featureName.Length) < levRatio)
            {
                nameKey = "name";
                return GnisNameMatch.FEATURE_NAME_CLOSE_MATCH;
            }

            // official name near match
            if ((LevenshteinDistance(featureName, tags["official_name"]) / (double)featureName.Length) < levRatio)
            {
                nameKey = "official_name";
                return GnisNameMatch.FEATURE_NAME_CLOSE_MATCH_OFFICIAL;
            }

            // alt name near match
            if ((LevenshteinDistance(featureName, tags["alt_name"]) / (double)featureName.Length) < levRatio)
            {
                nameKey = "alt_name";
                return GnisNameMatch.FEATURE_NAME_CLOSE_MATCH_ALT;
            }

            // old name near match
            if ((LevenshteinDistance(featureName, tags["old_name"]) / (double)featureName.Length) < levRatio)
            {
                nameKey = "old_name";
                return GnisNameMatch.FEATURE_NAME_CLOSE_MATCH_OLD;
            }

            // local name near match
            if ((LevenshteinDistance(featureName, tags["old_name"]) / (double)featureName.Length) < levRatio)
            {
                nameKey = "loc_name";
                return GnisNameMatch.FEATURE_NAME_CLOSE_MATCH_LOC;
            }

            // name_1 near match
            if ((LevenshteinDistance(featureName, tags["name_1"]) / (double)featureName.Length) < levRatio)
            {
                nameKey = "name_1";
                return GnisNameMatch.FEATURE_NAME_CLOSE_MATCH_1;
            }

            // name_2 near match
            if ((LevenshteinDistance(featureName, tags["name_2"]) / (double)featureName.Length) < levRatio)
            if ((LevenshteinDistance(featureName, tags["name_2"]) / (double)featureName.Length) < levRatio)
            {
                nameKey = "name_2";
                return GnisNameMatch.FEATURE_NAME_CLOSE_MATCH_2;
            }

            // no match
            nameKey = string.Empty;
            return GnisNameMatch.NO_MATCH;
        }

        private static int LevenshteinDistance(String? a, String? b)
        {
            // source is an array of characters 0..m-1
            char[] source = a != null ? a.ToUpperInvariant().ToCharArray() : Array.Empty<char>();
            int m = source.Length;

            // target is an array of characters 0..n-1
            char[] target = b != null ? b.ToUpperInvariant().ToCharArray() : Array.Empty<char>();
            int n = target.Length;

            // two work vectors representing the current and previous rows of an m by n matrix
            // the indexes of the current/previous vectors swap on each iteration
            int[,] v = new int[2, n + 1];

            // index of the previous row in the matrix
            int k = 0;

            // index of the current row in the matrix
            int l = 1;

            // initialize the first row of the matrix
            for (int j = 0; j <= n; j++)
            {
                // values represent changes to transform the empty prefix of the source array into the target array by inserting characters
                v[k, j] = j;
            }

            // for each row in the matrix
            for (int i = 0; i < m; i++)
            {
                // the first element in the row is the number of changes to transform the source prefix to the empty target prefix by deleting characters
                v[l, 0] = i + 1;

                // calculate edit distances for the rest of the row
                for (int j = 0; j < n; j++)
                {
                    // calculating distance for A[i+1][j+1]

                    // deletion cost is prior row, current column plus one
                    int deletionCost = v[k, j + 1] + 1;

                    // insertion cost is current row, prior column plus one
                    int insertionCost = v[l, j] + 1;

                    // if the source and target characters match
                    // substitution cost is the same as the prior row, prior column
                    int substitutionCost = v[k, j];

                    // if the source and target characters don't match
                    if (source[i] != target[j])
                        // substitution cost is the prior row, prior column plus one
                        substitutionCost++;

                    // distance for A[i+i][j+1] is the minium of the three costs
                    v[l, j + 1] = Math.Min(Math.Min(deletionCost, insertionCost), substitutionCost);
                }

                // swap the current and prior rows
                k = (k + 1) % 2;
                l = (l + 1) % 2;
            }

            // after the last swap, the result is in the "prior" row
            return v[k, n];
        }

        private GnisTagMatch MatchGnisFeatureClass(GnisRecord gnisRecord, OsmFeature osmFeature, out string primaryTagKey, out string secondaryTagKey)
        {
            GnisClassAttributes gnisClassAttributes = _gnisClassData.GetGnisClassAttributes(gnisRecord.FeatureClass);
            bool primaryTagMatch = false;
            bool secondaryTagMatch = false;
            bool secondaryTagsPresent = gnisClassAttributes.SecondaryTags.Length > 0;
            OsmTagCollection tags = osmFeature.GetTagCollection();

            primaryTagKey = string.Empty;
            secondaryTagKey = string.Empty;

            // if the OSM feature doesn't have any tags
            if (tags == null)
                return GnisTagMatch.NO_MATCH;

            // for each of the OSM feature's tags
            foreach (OsmTag tag in tags)
            {
                // if the tag matches one of the primary tags associated with this FEATURE_CLASS
                if (!primaryTagMatch && gnisClassAttributes.MatchesPrimaryTag(tag.Key, tag.Value))
                {
                    primaryTagKey = tag.Key;
                    primaryTagMatch = true;
                }

                // if the tag matches one of the secondary tags associated with this FEATURE_CLASS
                if (secondaryTagsPresent && !secondaryTagMatch && gnisClassAttributes.MatchesSecondaryTag(tag.Key, tag.Value))
                {
                    secondaryTagKey = tag.Key;
                    secondaryTagMatch = true;
                }

                // if we have both matches
                if (primaryTagMatch && secondaryTagMatch)
                    // no need to continue
                    break;
            }

            // all tags present
            if (primaryTagMatch && (secondaryTagMatch || !secondaryTagsPresent))
                return GnisTagMatch.FEATURE_CLASS_ALL_TAGS_MATCH;

            // primary tag present, secondary tag absent
            if (primaryTagMatch && !secondaryTagMatch && secondaryTagsPresent)
                return GnisTagMatch.FEATURE_CLASS_PRIMARY_TAG_MATCH;

            // primary tag absent, secondary tag absent
            if (!primaryTagMatch && secondaryTagMatch && secondaryTagsPresent)
                return GnisTagMatch.FEATURE_CLASS_SECONDARY_TAG_MATCH;

            // no match
            return GnisTagMatch.NO_MATCH;
        }

        private GnisConflictingTagMatch CheckForConflictingTags(GnisRecord gnisRecord, OsmFeature osmFeature, out string conflictingTag)
        {
            GnisClassAttributes gnisClassAttributes = _gnisClassData.GetGnisClassAttributes(gnisRecord.FeatureClass);

            OsmTagCollection tags = osmFeature.GetTagCollection();

            conflictingTag = string.Empty;

            // if the GNIS class doesn't have any conflicting tags
            if (gnisClassAttributes.ConflictingTags.Length == 0)
                return GnisConflictingTagMatch.NO_MATCH;

            // if the OSM feature doesn't have any tags
            if (tags == null)
                return GnisConflictingTagMatch.NO_MATCH;

            // for each of the OSM feature's tags
            foreach (OsmTag tag in tags)
            {
                // if the tag matches one of the secondary tags associated with this FEATURE_CLASS
                if (gnisClassAttributes.MatchesConflictingTag(tag.Key, tag.Value))
                {
                    conflictingTag = $"{tag.Key}={tag.Value}";
                    return GnisConflictingTagMatch.FEATURE_CLASS_CONFLICTING_TAG_MATCH;
                }
            }

            // no match
            return GnisConflictingTagMatch.NO_MATCH;
        }

        private GnisGeometryMatch MatchGnisFeatureCoordinates(GnisRecord gnisRecord, OsmFeature osmFeature)
        {
            GnisClassAttributes gnisClassAttributes = _gnisClassData.GetGnisClassAttributes(gnisRecord.FeatureClass);
            double startDistance;
            double endDistance;
            double reverseStartDistance;
            double reverseEndDistance;

            if (osmFeature is OsmNode osmNode)
            {
                // if the GNIS class is not represented as a point feature
                if (!gnisClassAttributes.HasGeometry("point"))
                    return GnisGeometryMatch.NO_MATCH;

                // calculate distance between GNIS feature location and OSM feature location
                endDistance = gnisRecord.Primary.GetDistanceTo(osmNode.GetCoordinate());

                if (!gnisRecord.HasSource())
                {
                    // if the distance is less than 1 meter
                    if (endDistance < 1)
                        return GnisGeometryMatch.FEATURE_COORDINATE_EXACT_MATCH;

                    // if the distance is less than 100 meters
                    if (endDistance < 100)
                        return GnisGeometryMatch.FEATURE_COORDINATE_CLOSE_MATCH;
                }
                else
                {
                    startDistance = gnisRecord.HasSource() ? gnisRecord.Source.GetDistanceTo(osmNode.GetCoordinate()) : double.MaxValue;

                    if (startDistance < 1 && endDistance < 1)
                        return GnisGeometryMatch.FEATURE_COORDINATE_EXTENT_EXACT_MATCH;
                    if (startDistance < 100 && endDistance < 100)
                        return GnisGeometryMatch.FEATURE_COORDINATE_EXTENT_CLOSE_MATCH;
                    if (startDistance < 1) // && endDistance > 100
                        return GnisGeometryMatch.FEATURE_COORDINATE_START_SOURCE_EXACT_MATCH;
                    if (startDistance < 100) // && endDistance > 100
                        return GnisGeometryMatch.FEATURE_COORDINATE_START_SOURCE_CLOSE_MATCH;
                    if (endDistance < 1) // && startDistance > 100
                        return GnisGeometryMatch.FEATURE_COORDINATE_END_PRIMARY_EXACT_MATCH;
                    if (endDistance < 100) // && startDistance > 100
                        return GnisGeometryMatch.FEATURE_COORDINATE_END_PRIMARY_CLOSE_MATCH;
                }
            }
            else
            {
                OsmLinearExtent? linearExtent = osmFeature.GetLinearExtent();

                if (linearExtent == null)
                    return GnisGeometryMatch.NO_MATCH;

                // calculate distances to start and end of feature in forward direction
                startDistance = gnisRecord.HasSource() ? gnisRecord.Source.GetDistanceTo(linearExtent.Start) : double.MaxValue;
                endDistance = gnisRecord.Primary.GetDistanceTo(linearExtent.End);

                if (startDistance < 1 && endDistance < 1)
                    return GnisGeometryMatch.FEATURE_COORDINATE_EXTENT_EXACT_MATCH;
                if (startDistance < 100 && endDistance < 100)
                    return GnisGeometryMatch.FEATURE_COORDINATE_EXTENT_CLOSE_MATCH;
                if (startDistance < 1) // && endDistance > 100
                    return GnisGeometryMatch.FEATURE_COORDINATE_START_SOURCE_EXACT_MATCH;
                if (startDistance < 100) // && endDistance > 100
                    return GnisGeometryMatch.FEATURE_COORDINATE_START_SOURCE_CLOSE_MATCH;
                if (endDistance < 1) // && startDistance > 100
                    return GnisGeometryMatch.FEATURE_COORDINATE_END_PRIMARY_EXACT_MATCH;
                if (endDistance < 100) // && startDistance > 100
                    return GnisGeometryMatch.FEATURE_COORDINATE_END_PRIMARY_CLOSE_MATCH;

                // calculate distances to start and end of feature in reverse direction
                reverseStartDistance = gnisRecord.Primary.GetDistanceTo(linearExtent.Start);
                reverseEndDistance = gnisRecord.HasSource() ? gnisRecord.Source.GetDistanceTo(linearExtent.End) : double.MaxValue;

                if (reverseStartDistance < 1 && reverseEndDistance < 1)
                    return GnisGeometryMatch.FEATURE_COORDINATE_EXTENT_REVERSE_EXACT_MATCH;
                if (reverseStartDistance < 100 && reverseEndDistance < 100)
                    return GnisGeometryMatch.FEATURE_COORDINATE_EXTENT_REVERSE_CLOSE_MATCH;
                if (reverseStartDistance < 1) // && reverseEndDistance > 100
                    return GnisGeometryMatch.FEATURE_COORDINATE_START_PRIMARY_EXACT_MATCH;
                if (reverseStartDistance < 100) // && reverseEndDistance > 100
                    return GnisGeometryMatch.FEATURE_COORDINATE_START_PRIMARY_CLOSE_MATCH;
                if (reverseEndDistance < 1) // && reverseStartDistance > 100
                    return GnisGeometryMatch.FEATURE_COORDINATE_END_SOURCE_EXACT_MATCH;
                if (reverseEndDistance < 100) // && reverseStartDistance > 100
                    return GnisGeometryMatch.FEATURE_COORDINATE_END_SOURCE_CLOSE_MATCH;
                if (reverseStartDistance < startDistance && reverseEndDistance < endDistance)
                    return GnisGeometryMatch.NO_MATCH_REVERSE;
            }

            return GnisGeometryMatch.NO_MATCH;
        }

//      private static readonly ConcurrentDictionary<string, string> wikidataCache = new();
        private static readonly string primaryKey = "gnis:feature_id";
        private static readonly ImmutableHashSet<string> synonymousKeys = ImmutableHashSet.Create( new string[] 
        {
            "gnis:id",
            "tiger:PLACENS",
            "NHD:GNIS_ID",
            "ref:gnis"
        });

        // private static readonly Regex valuePattern = new(@"\d+(;\d+)*");
        [GeneratedRegex(@"\d+(;\d+)*")]
        private static partial Regex ValuePattern();

        //private static readonly Regex numberPattern = new();
        [GeneratedRegex(@"^\d+$")]
        private static partial Regex NumberPattern();

        private static GnisFeatureIdMatch MatchGnisFeatureId(GnisRecord gnisRecord, OsmFeature osmFeature, out string featureIdKey)
        {
            OsmTagCollection tags = osmFeature.GetTagCollection();

            // no tags
            if (tags == null)
            {
                featureIdKey = string.Empty;
                return GnisFeatureIdMatch.NO_MATCH;
            }

            long parsedValue;
            if (tags.ContainsKey(primaryKey))
            {
                string primaryValue = tags[primaryKey] ?? string.Empty;

                // malformed gnis:feature_id tag
                if (!ValuePattern().IsMatch(primaryValue))
                {
                    featureIdKey = primaryKey;
                    return GnisFeatureIdMatch.FEATURE_ID_MALFORMED_VALUE;
                }

                // exact match for gnis:feature_id tag
                if (gnisRecord.FeatureId.Equals(tags[primaryKey]))
                {
                    featureIdKey = primaryKey;
                    return GnisFeatureIdMatch.FEATURE_ID_EXACT_MATCH;
                }

                // exact match for gnis:feature_id tag after numeric conversion
                if (long.Parse(gnisRecord.FeatureId) == (long.TryParse(tags[primaryKey], out parsedValue) ? parsedValue : -1))
                {
                    featureIdKey = primaryKey;
                    return GnisFeatureIdMatch.FEATURE_ID_EXACT_NUMERIC_MATCH;
                }

                foreach (string value in primaryValue.Split(";"))
                {
                    // partial match for gnis:feature_id tag
                    if (gnisRecord.FeatureId.Equals(value))
                    {
                        featureIdKey = primaryKey;
                        return GnisFeatureIdMatch.FEATURE_ID_PARTIAL_MATCH;
                    }

                    // partial match for gnis:feature_id after numeric conversion
                    if (long.Parse(gnisRecord.FeatureId) == (long.TryParse(value, out parsedValue) ? parsedValue : -1))
                    {
                        featureIdKey = primaryKey;
                        return GnisFeatureIdMatch.FEATURE_ID_PARTIAL_NUMERIC_MATCH;
                    }
                }
            }

            foreach (OsmTag tag in tags)
            {
                // is this a synonmous key?
                bool synonymous = synonymousKeys.Contains(tag.Key);

                // exact match for other tag
                if (gnisRecord.FeatureId.Equals(tag.Value))
                {
                    featureIdKey = tag.Key;

                    // exact match for synonymous tag
                    if (synonymous)
                        return GnisFeatureIdMatch.FEATURE_ID_EXACT_MATCH_SYNONYMOUS_KEY;

                    // exact match for unexpected key
                    else
                        return GnisFeatureIdMatch.FEATURE_ID_EXACT_MATCH_UNEXPECTED_KEY;
                }

                if (NumberPattern().IsMatch(tag.Value))
                {
                    // exact match for other tag after numeric conversion
                    if (long.Parse(gnisRecord.FeatureId) == (long.TryParse(tag.Value, out parsedValue) ? parsedValue : -1))
                    {
                        featureIdKey = tag.Key;

                        // exact match for synonymous key
                        if (synonymous)
                            return GnisFeatureIdMatch.FEATURE_ID_EXACT_NUMERIC_MATCH_SYNONYMOUS_KEY;

                        // exact match for unexpected key
                        else
                            return GnisFeatureIdMatch.FEATURE_ID_EXACT_NUMERIC_MATCH_UNEXPECTED_KEY;
                    }
                }

                // partial match for other tag
                foreach (string value in tag.Value.Split(";"))
                {
                    // partial match for synonymous tag
                    if (gnisRecord.FeatureId.Equals(value))
                    {
                        featureIdKey = tag.Key;

                        // exact match for synonymous key
                        if (synonymous)
                            return GnisFeatureIdMatch.FEATURE_ID_PARTIAL_MATCH_SYNONYMOUS_KEY;

                        // partial match for unexpected key
                        else
                            return GnisFeatureIdMatch.FEATURE_ID_PARTIAL_MATCH_UNEXPECTED_KEY;
                    }

                    if (NumberPattern().IsMatch(value))
                    {
                        // partial match for synonymous tag after numeric conversions
                        if (long.Parse(gnisRecord.FeatureId) == (long.TryParse(value, out parsedValue) ? parsedValue : -1))
                        {
                            featureIdKey = tag.Key;

                            // exact match for synonymous key
                            if (synonymous)
                                return GnisFeatureIdMatch.FEATURE_ID_PARTIAL_NUMERIC_MATCH_SYNONYMOUS_KEY;

                            // partial match for unexpected key
                            else
                                return GnisFeatureIdMatch.FEATURE_ID_PARTIAL_NUMERIC_MATCH_UNEXPECTED_KEY;
                        }
                    }
                }
            }

            /*
            if (WikidataFeatureIdMatch(gnisRecord, osmFeature) == GnisFeatureIdMatch.FEATURE_ID_WIKIDATA_MATCH)
            {
                featureIdKey = string.Empty;
                return GnisFeatureIdMatch.FEATURE_ID_WIKIDATA_MATCH;
            }
            */

            string[] wikidataGnisIds = WikidataLookup.GetGnisIds(osmFeature);
            foreach (string wikidataGnisId in wikidataGnisIds)
            {
                if (long.TryParse(wikidataGnisId, out long gnisId) && gnisId == osmFeature.Id)
                {
                    featureIdKey = string.Empty;
                    return GnisFeatureIdMatch.FEATURE_ID_WIKIDATA_MATCH;
                }
            }

            // no match
            featureIdKey = string.Empty;
            return GnisFeatureIdMatch.NO_MATCH;
        }

        /*
        private static GnisFeatureIdMatch WikidataFeatureIdMatch(GnisRecord gnisRecord, OsmFeature osmFeature)
        {
            // if the feature in OSM has a Wikidata ID, try to use that to get the GNIS feature ID
            if (osmFeature.GetTagCollection().ContainsKey("wikidata"))
            {
                string baseUrl = @"https://wikidata.org/w/rest.php/wikibase/v0";
                string? itemId = osmFeature.GetTagCollection()["wikidata"];

                // check the wikidata cache first
                if (itemId != null && wikidataCache.TryGetValue(itemId, out string? wikidataGnisIds))
                {
                    foreach (string wikidataGnisId in wikidataGnisIds.Split(";"))
                    {
                        if (gnisRecord.FeatureId.Equals(wikidataGnisId))
                        {
                            return GnisFeatureIdMatch.FEATURE_ID_WIKIDATA_MATCH;
                        }
                    }
                }
                else if (itemId != null)
                {
                    wikidataCache[itemId] = string.Empty;

                    try
                    {
                        // todo: change this to request only the GNIS ID statement
                        // see https://doc.wikimedia.org/Wikibase/master/js/rest-api/
                        string url = $"{baseUrl}/entities/items/{itemId}/statements";
                        HttpRequestMessage request = new(HttpMethod.Get, url);
                        request.Headers.Add("User-Agent", Program.PrivateData.UserAgent);
                        request.Headers.Add("Authorization", Program.PrivateData.WikidataAuthorization);
                        HttpResponseMessage response = Program.HttpClient.Send(request);
                        string? content = new StreamReader(response.Content.ReadAsStream()).ReadToEnd();
                        response.EnsureSuccessStatusCode();
                        JsonNode? wikidataItem = JsonNode.Parse(content);

                        if (wikidataItem != null)
                        {
                            List<string> ids = new();
                            bool match = false;

                            JsonNode? wikidataGnisIdStatement = wikidataItem["P590"];
                            if (wikidataGnisIdStatement != null)
                            {
                                foreach (JsonNode? wikidataGnisIdValue in wikidataGnisIdStatement.AsArray())
                                {
                                    string? wikidataGnisId = wikidataGnisIdValue?["value"]?["content"]?.ToString();
                                    if (wikidataGnisId != null)
                                    {
                                        Program.Verbose.WriteLine($"Wikidata GNIS ID: {wikidataGnisId}");
                                        ids.Add(wikidataGnisId);
                                        if (gnisRecord.FeatureId.Equals(wikidataGnisId))
                                        {
                                            match = true;
                                        }
                                    }
                                }
                            }

                            wikidataCache[itemId] = String.Join(";", ids);

                            if (match)
                            {
                                return GnisFeatureIdMatch.FEATURE_ID_WIKIDATA_MATCH;
                            }
                        }

                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
            }
            return GnisFeatureIdMatch.NO_MATCH;
        }
        */

        internal void ConsolidateMatches(GnisRecord gnisRecord, List<GnisMatchResult> matchResults, List<GnisValidationResult> validationResults, GnisMatchResult? newMatchResult, GnisValidationResult? newValidationResult)
        {
            bool allOk = true;

            foreach (GnisValidationResult validationResult in validationResults)
            {
                allOk &= validationResult.AllOk;
            }

            // if all the match results are fine
            if (allOk)
                // don't create a new match
                return;

            // this method creates a new relation for a consistent set of waterway matches
            // another case might be a boundary relation with a label node that is not yet a member
            // or a reservoir area and separate reservoir node that need to be combined

            // if the GNIS record is for a waterway
            if (_gnisClassData.GetGnisClassAttributes(gnisRecord.FeatureClass).IsWaterwayClass())
            {
                // find all the relations in the match result
                List<GnisMatchResult> relationMatches = matchResults.FindAll(match => match.osmFeature is OsmRelation);

                // if there are no relations in the match results
                if (relationMatches.Count == 0)
                {
                    // try to build a relation with the match results as members
                    OsmRelation? newRelation = BuildNewWaterwayRelation(gnisRecord, matchResults);

                    if (newRelation != null)
                    {
                        // run match and validation on the new relation
                        newMatchResult = Program.GnisMatcher.MatchOsmFeature(gnisRecord, newRelation);
                        newMatchResult.specialCondition = GnisMatchSpecialCondition.NEW_RELATION;
                        newValidationResult = Program.GnisValidator.ValidateOsmFeature(gnisRecord, newMatchResult);

                        // drop the new relation if the tags conflict with the GNIS feature class
                        if (newMatchResult.MatchType != GnisMatchType.conflictingMatch)
                        {
                            newMatchResult = null;
                            newValidationResult = null;
                            return;
                        }
                    }
                }
                // if there is one relation in the match results
                else if (relationMatches.Count == 1)
                {
                    // try to add any non-members in the match results to the relation
                    // rerun the match and validation on the relation because geometry has changed
                    // build a collaborative task to modify the relation
                }
            }
        }

        private OsmRelation? BuildNewWaterwayRelation(GnisRecord gnisRecord, List<GnisMatchResult> matchResults)
        {
            GnisClassAttributes gnisClassAttributes = _gnisClassData.GetGnisClassAttributes(gnisRecord.FeatureClass);

            // get the parent for the OSM data set
            XOsmData parent = matchResults[0].osmFeature.GetParent();

            // create a new relation
            OsmRelation newRelation = new();
            newRelation.SetParent(parent);

            // use the default relation type for the feature class
            newRelation.AddTag(new OsmTag("type", gnisClassAttributes.DefaultRelationType));

            // collect other tags from the match results
            Dictionary<string, string> newRelationTags = new();
            foreach (GnisMatchResult matchResult in matchResults)
            {
                foreach (OsmTag osmFeatureTag in matchResult.osmFeature.GetTagCollection())
                {
                    if (newRelationTags.TryGetValue(osmFeatureTag.Key, out string? relationTagValue))
                    {
                        // if the value differs from other objects
                        if (!relationTagValue.Equals(osmFeatureTag.Value))
                        {
                            // use an empty value to indicate that this tag will not be added to the relation
                            newRelationTags[osmFeatureTag.Key] = string.Empty;
                        }
                    }
                    else
                    {
                        newRelationTags[osmFeatureTag.Key] = osmFeatureTag.Value;
                    }
                }
            }

            // add the common tags to the relation
            foreach (string key in newRelationTags.Keys)
            {
                // skip tags that don't get promoted to the relation
                if ("layer".Equals(key) || "tunnel".Equals(key))
                    continue;

                // if the tag has an actual value
                if (!string.IsNullOrEmpty(newRelationTags[key]))
                    newRelation.AddTag(new OsmTag(key, newRelationTags[key]));
            }

            // note that if there were defects in the tagging of the results
            // e.g. using gnis:id or name_1 tags
            // these will be carried over to the tags on the relation
            // but this new relation will be rematched, revalidated, and processed for tag changes

            // add all the matched features to the relation
            foreach (GnisMatchResult matchResult in matchResults)
            {
                newRelation.AddMember(matchResult.osmFeature);
            }

            // verify that the relation is fully connected
            OsmLinearExtent? linearExtent = newRelation.GetLinearExtent();

            // if the relation is not fully connected
            if (linearExtent == null)
                return null;

            return newRelation;
        }
    }
}