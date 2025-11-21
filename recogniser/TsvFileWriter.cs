// <copyright file="TsvFileWriter.cs" company="recogniser project contributors">
// Copyright (c) 2025 recogniser project contributors.
// Licensed under the AGPL-3.0-or-later license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Recogniser
{
    using System.Globalization;

    internal class TsvFileWriter : IDisposable
    {
        private static readonly string[] ColumnHeaders =
        [
            "FEATURE_ID",
            "FEATURE_NAME",
            "FEATURE_CLASS",
            "PRIM_LAT_DEC",
            "PRIM_LONG_DEC",
            "SOURCE_LAT_DEC",
            "SOURCE_LONG_DEC",
            "OSM_TYPE",
            "OSM_ID",
            "OSM_NAME",
            "OVERPASS_QUERY",
            "AREA_SOUTH",
            "AREA_EAST",
            "AREA_NORTH",
            "AREA_WEST",
            "MATCH_TYPES",
            "VALIDATION_RESULTS",
            "GNIS_LINK",
            "OSM_LINK",
            "ID_LINK",
            "JOSM_AREA_LINK",
            "JOSM_OBJECT_LINK",
        ];

        private readonly TextWriter outputStreamWriter;
        private readonly Lock outputStreamWriterLock = new();
        private bool disposedValue;

        public TsvFileWriter(string? outputFileName)
        {
            if (string.IsNullOrEmpty(outputFileName))
            {
                outputStreamWriter = new StreamWriter(Stream.Null);
            }
            else
            {
                outputStreamWriter = new StreamWriter(outputFileName);
            }

            WriteOutputHeader();
        }

        public void WriteOutputRecord(GnisRecord gnisRecord, string? overpassQuery = null, GnisMatchResult? matchResult = null, GnisValidationResult? validationResult = null)
        {
            // make a two kilometer box with the feature at the center (i.e. 1 km in each direction)
            double[] twoKilometerBox = OverpassQueryBuilder.MakeBoundingBox(gnisRecord.Primary.Latitude, gnisRecord.Primary.Longitude, 2000);

            string featureId = gnisRecord.FeatureId;
            string featureName = gnisRecord.FeatureName;
            string featureClass = gnisRecord.FeatureClass;
            string primLatDec = gnisRecord.PrimaryLat;
            string primLongDec = gnisRecord.PrimaryLon;
            string sourceLatDec = gnisRecord.SourceLat;
            string sourceLongDec = gnisRecord.SourceLon;

            string osmType = matchResult != null ? matchResult.OsmFeature.GetOsmType().ToString() : string.Empty;
            string osmId = matchResult != null ? matchResult.OsmFeature.Id.ToString(CultureInfo.InvariantCulture) : string.Empty;
            string osmName = matchResult != null ? matchResult.OsmFeature.GetName() : string.Empty;

            string overpassQueryValue = overpassQuery ?? string.Empty;

            string areaSouth = twoKilometerBox[0].ToString(CultureInfo.InvariantCulture);
            string areaWest = twoKilometerBox[1].ToString(CultureInfo.InvariantCulture);
            string areaNorth = twoKilometerBox[2].ToString(CultureInfo.InvariantCulture);
            string areaEast = twoKilometerBox[3].ToString(CultureInfo.InvariantCulture);

            string matchTypes = matchResult != null ? matchResult.ToString() : string.Empty;
            string validationResults = validationResult != null ? validationResult.ToString() : string.Empty;

            string gnisLink = $"=HYPERLINK(\"https://edits.nationalmap.gov/apps/gaz-domestic/public/summary/{featureId}\",{featureId})";
            string osmLink = $"=HYPERLINK(\"https://www.openstreetmap.org/#map=18/{primLatDec}/{primLongDec}\",\"{primLatDec}/{primLongDec}\")";

            string idLink = matchResult != null
                ? $"=HYPERLINK(\"https://www.openstreetmap.org/{matchResult.OsmFeature.GetOsmType()}/{matchResult.OsmFeature.Id}\",\"{matchResult.OsmFeature.GetOsmType()}/{matchResult.OsmFeature.Id}\")"
                : string.Empty;

            string josmAreaLink = $"=HYPERLINK(\"http://127.0.0.1:8111/load_and_zoom?left={areaWest}&right={areaEast}&top={areaNorth}&bottom={areaSouth}\",\"{primLatDec}/{primLongDec}\")";

            string josmObjectLink = matchResult != null
                ? $"=HYPERLINK(\"http://127.0.0.1:8111/load_object?newlayer=false&objects={matchResult.OsmFeature.GetOsmType().ToString().ToCharArray()[0]}{matchResult.OsmFeature.Id}\",\"{matchResult.OsmFeature.GetOsmType().ToString().ToCharArray()[0]}{matchResult.OsmFeature.Id}\")"
                : string.Empty;

            string[] values =
            [
                featureId,
                featureName,
                featureClass,
                primLatDec,
                primLongDec,
                sourceLatDec,
                sourceLongDec,
                osmType,
                osmId,
                osmName,
                overpassQueryValue,
                areaSouth,
                areaEast,
                areaNorth,
                areaWest,
                matchTypes,
                validationResults,
                gnisLink,
                osmLink,
                idLink,
                josmAreaLink,
                josmObjectLink,
            ];

            WriteOutputRecord(values);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    outputStreamWriter.Dispose();
                }

                disposedValue = true;
            }
        }

        private void WriteOutputHeader()
        {
            lock (outputStreamWriterLock)
            {
                outputStreamWriter.WriteLine(string.Join("\t", ColumnHeaders));
            }
        }

        private void WriteOutputRecord(string[] values)
        {
            lock (outputStreamWriterLock)
            {
                outputStreamWriter.WriteLine(string.Join("\t", values));
                outputStreamWriter.Flush();
            }
        }
    }
}