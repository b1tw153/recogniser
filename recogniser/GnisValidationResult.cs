// <copyright file="GnisValidationResult.cs" company="recogniser project contributors">
// Copyright (c) 2025 recogniser project contributors.
// Licensed under the AGPL-3.0-or-later license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Recogniser
{
    internal class GnisValidationResult
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
            List<string> result = [];

            /*
            if (featureIdValidation == GnisFeatureIdValidation.NOT_PROCESSED
                || nameValidation == GnisNameValidation.NOT_PROCESSED
                || tagValidation == GnisTagValidation.NOT_PROCESSED
                || conflictingTagValidation == GnisConflictingTagValidation.NOT_PROCESSED
                || geometryValidation == GnisGeometryValidation.NOT_PROCESSED)
                throw new Exception("All validation should have been processed.");
            */

            if (featureIdValidation != GnisFeatureIdValidation.OK && featureIdValidation != GnisFeatureIdValidation.NOT_PROCESSED)
            {
                result.Add(featureIdValidation.ToString());
            }

            if (nameValidation != GnisNameValidation.OK && nameValidation != GnisNameValidation.NOT_PROCESSED)
            {
                result.Add(nameValidation.ToString());
            }

            if (tagValidation != GnisTagValidation.OK && tagValidation != GnisTagValidation.NOT_PROCESSED)
            {
                result.Add(tagValidation.ToString());
            }

            if (conflictingTagValidation != GnisConflictingTagValidation.OK && conflictingTagValidation != GnisConflictingTagValidation.NOT_PROCESSED)
            {
                result.Add(conflictingTagValidation.ToString());
            }

            if (geometryValidation != GnisGeometryValidation.OK && geometryValidation != GnisGeometryValidation.NOT_PROCESSED)
            {
                result.Add(geometryValidation.ToString());
            }

            return string.Join(";", result);
        }
    }
}