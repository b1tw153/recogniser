// <copyright file="GnisMatchResult.cs" company="recogniser project contributors">
// Copyright (c) 2025 recogniser project contributors.
// Licensed under the AGPL-3.0-or-later license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Recogniser
{
    using System.Collections.Immutable;
    using System.Text.RegularExpressions;

    internal sealed class GnisMatchResult(XOsmFeature osmFeature)
    {
        public XOsmFeature OsmFeature { get; set; } = osmFeature;

        public GnisFeatureIdMatch FeatureIdMatch { get; set; } = GnisFeatureIdMatch.NOT_PROCESSED;

        public GnisNameMatch NameMatch { get; set; } = GnisNameMatch.NOT_PROCESSED;

        public GnisTagMatch TagMatch { get; set; } = GnisTagMatch.NOT_PROCESSED;

        public GnisGeometryMatch GeometryMatch { get; set; } = GnisGeometryMatch.NOT_PROCESSED;

        public GnisConflictingTagMatch ConflictingTagMatch { get; set; } = GnisConflictingTagMatch.NOT_PROCESSED;

        public GnisMatchSpecialCondition SpecialCondition { get; set; } = GnisMatchSpecialCondition.NO_SPECIAL_CONDITION;

        public string FeatureIdKey { get; set; } = string.Empty;

        public string NameKey { get; set; } = string.Empty;

        public string PrimaryTagKey { get; set; } = string.Empty;

        public string SecondaryTagKey { get; set; } = string.Empty;

        public string ConflictingTag { get; set; } = string.Empty;

        public bool FeatureIdMatched =>
            FeatureIdMatch != GnisFeatureIdMatch.NO_MATCH &&
            FeatureIdMatch != GnisFeatureIdMatch.NOT_PROCESSED;

        public bool NameMatched =>
            NameMatch != GnisNameMatch.NO_MATCH &&
            NameMatch != GnisNameMatch.NOT_PROCESSED;

        public bool ExactNameMatched =>
            NameMatch == GnisNameMatch.FEATURE_NAME_EXACT_MATCH ||
            NameMatch == GnisNameMatch.FEATURE_NAME_EXACT_MATCH_1 ||
            NameMatch == GnisNameMatch.FEATURE_NAME_EXACT_MATCH_2 ||
            NameMatch == GnisNameMatch.FEATURE_NAME_EXACT_MATCH_ALT ||
            NameMatch == GnisNameMatch.FEATURE_NAME_EXACT_MATCH_LOC ||
            NameMatch == GnisNameMatch.FEATURE_NAME_EXACT_MATCH_OFFICIAL ||
            NameMatch == GnisNameMatch.FEATURE_NAME_EXACT_MATCH_OLD;

        public bool TagMatched =>
            TagMatch != GnisTagMatch.NO_MATCH &&
            TagMatch != GnisTagMatch.NOT_PROCESSED;

        public bool PrimaryTagMatched =>
            TagMatch == GnisTagMatch.FEATURE_CLASS_ALL_TAGS_MATCH ||
            TagMatch == GnisTagMatch.FEATURE_CLASS_PRIMARY_TAG_MATCH;

        public bool ConflictingTagMatched =>
            ConflictingTagMatch != GnisConflictingTagMatch.NO_MATCH &&
            ConflictingTagMatch != GnisConflictingTagMatch.NOT_PROCESSED;

        public bool GeometryMatched =>
            GeometryMatch != GnisGeometryMatch.NO_MATCH &&
            GeometryMatch != GnisGeometryMatch.NO_MATCH_REVERSE &&
            GeometryMatch != GnisGeometryMatch.NOT_PROCESSED;

        public bool GeometryReversed =>
            GeometryMatch == GnisGeometryMatch.FEATURE_COORDINATE_EXTENT_REVERSE_EXACT_MATCH ||
            GeometryMatch == GnisGeometryMatch.FEATURE_COORDINATE_EXTENT_REVERSE_CLOSE_MATCH ||
            GeometryMatch == GnisGeometryMatch.FEATURE_COORDINATE_START_PRIMARY_CLOSE_MATCH ||
            GeometryMatch == GnisGeometryMatch.FEATURE_COORDINATE_START_PRIMARY_EXACT_MATCH ||
            GeometryMatch == GnisGeometryMatch.FEATURE_COORDINATE_END_SOURCE_CLOSE_MATCH ||
            GeometryMatch == GnisGeometryMatch.FEATURE_COORDINATE_END_SOURCE_EXACT_MATCH ||
            GeometryMatch == GnisGeometryMatch.NO_MATCH_REVERSE;

        public GnisMatchType MatchType
        {
            get
            {
                if (!ConflictingTagMatched)
                {
                    if (FeatureIdMatched)
                    {
                        return GnisMatchType.ExactMatch;
                    }
                    else if (ExactNameMatched && PrimaryTagMatched)
                    {
                        return GnisMatchType.ExactMatch;
                    }
                    else if (NameMatched && TagMatched)
                    {
                        return GnisMatchType.CloseMatch;
                    }
                    else if (NameMatched && GeometryMatched)
                    {
                        return GnisMatchType.CloseMatch;
                    }
                    else if (PrimaryTagMatched && GeometryMatched)
                    {
                        return GnisMatchType.CloseMatch;
                    }
                    else
                    {
                        return GnisMatchType.NoMatch;
                    }
                }
                else
                {
                    if (FeatureIdMatched)
                    {
                        return GnisMatchType.ConflictingMatch;
                    }
                    else if (ExactNameMatched && PrimaryTagMatched)
                    {
                        return GnisMatchType.ConflictingMatch;
                    }
                    else if (NameMatched && TagMatched)
                    {
                        return GnisMatchType.ConflictingMatch;
                    }
                    else if (NameMatched && GeometryMatched)
                    {
                        return GnisMatchType.NoMatch;
                    }
                    else if (PrimaryTagMatched && GeometryMatched)
                    {
                        return GnisMatchType.NoMatch;
                    }
                    else
                    {
                        return GnisMatchType.NoMatch;
                    }
                }
            }
        }

        public override string ToString()
        {
            List<string> result = [];

            if (FeatureIdMatched)
            {
                result.Add(FeatureIdMatch.ToString());
            }

            if (NameMatched)
            {
                result.Add(NameMatch.ToString());
            }

            if (TagMatched)
            {
                result.Add(TagMatch.ToString());
            }

            if (ConflictingTagMatched)
            {
                result.Add(ConflictingTagMatch.ToString());
            }

            if (GeometryMatched)
            {
                result.Add(GeometryMatch.ToString());
            }

            return string.Join(";", result);
        }
    }
}