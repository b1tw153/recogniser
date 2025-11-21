// <copyright file="GnisValidationResult.cs" company="recogniser project contributors">
// Copyright (c) 2025 recogniser project contributors.
// Licensed under the AGPL-3.0-or-later license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Recogniser
{
    internal sealed class GnisValidationResult(XOsmFeature osmFeature)
    {
        public XOsmFeature OsmFeature { get; set; } = osmFeature;

        public GnisFeatureIdValidation FeatureIdValidation { get; set; } = GnisFeatureIdValidation.NOT_PROCESSED;

        public GnisNameValidation NameValidation { get; set; } = GnisNameValidation.NOT_PROCESSED;

        public GnisTagValidation TagValidation { get; set; } = GnisTagValidation.NOT_PROCESSED;

        public GnisConflictingTagValidation ConflictingTagValidation { get; set; } = GnisConflictingTagValidation.NOT_PROCESSED;

        public GnisGeometryValidation GeometryValidation { get; set; } = GnisGeometryValidation.NOT_PROCESSED;

        public bool AllOk =>
            (FeatureIdValidation == GnisFeatureIdValidation.OK || FeatureIdValidation == GnisFeatureIdValidation.NOT_PROCESSED) &&
            (NameValidation == GnisNameValidation.OK || NameValidation == GnisNameValidation.NOT_PROCESSED) &&
            (TagValidation == GnisTagValidation.OK || TagValidation == GnisTagValidation.NOT_PROCESSED) &&
            (ConflictingTagValidation == GnisConflictingTagValidation.OK || ConflictingTagValidation == GnisConflictingTagValidation.NOT_PROCESSED) &&
            (GeometryValidation == GnisGeometryValidation.OK || GeometryValidation == GnisGeometryValidation.NOT_PROCESSED);

        public override string ToString()
        {
            List<string> result = [];

            /*
            if (featureIdValidation == GnisFeatureIdValidation.NOT_PROCESSED
                || nameValidation == GnisNameValidation.NOT_PROCESSED
                || tagValidation == GnisTagValidation.NOT_PROCESSED
                || conflictingTagValidation == GnisConflictingTagValidation.NOT_PROCESSED
                || geometryValidation == GnisGeometryValidation.NOT_PROCESSED)
                throw new Exception("All validation should have been processed.");
            */

            if (FeatureIdValidation != GnisFeatureIdValidation.OK && FeatureIdValidation != GnisFeatureIdValidation.NOT_PROCESSED)
            {
                result.Add(FeatureIdValidation.ToString());
            }

            if (NameValidation != GnisNameValidation.OK && NameValidation != GnisNameValidation.NOT_PROCESSED)
            {
                result.Add(NameValidation.ToString());
            }

            if (TagValidation != GnisTagValidation.OK && TagValidation != GnisTagValidation.NOT_PROCESSED)
            {
                result.Add(TagValidation.ToString());
            }

            if (ConflictingTagValidation != GnisConflictingTagValidation.OK && ConflictingTagValidation != GnisConflictingTagValidation.NOT_PROCESSED)
            {
                result.Add(ConflictingTagValidation.ToString());
            }

            if (GeometryValidation != GnisGeometryValidation.OK && GeometryValidation != GnisGeometryValidation.NOT_PROCESSED)
            {
                result.Add(GeometryValidation.ToString());
            }

            return string.Join(";", result);
        }
    }
}