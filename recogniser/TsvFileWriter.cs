namespace recogniser
{
    public class TsvFileWriter : IDisposable
    {
        private readonly TextWriter outputStreamWriter;
        private bool disposedValue;

        private class OutputRecord
        {
            public string FEATURE_ID = string.Empty;
            public string FEATURE_NAME = string.Empty;
            public string FEATURE_CLASS = string.Empty;
            public string PRIM_LAT_DEC = string.Empty;
            public string PRIM_LONG_DEC = string.Empty;
            public string SOURCE_LAT_DEC = string.Empty;
            public string SOURCE_LONG_DEC = string.Empty;
            public string OSM_TYPE = string.Empty;
            public string OSM_ID = string.Empty;
            public string OSM_NAME = string.Empty;
            public string OVERPASS_QUERY = string.Empty;
            public string AREA_SOUTH = string.Empty;
            public string AREA_EAST = string.Empty;
            public string AREA_NORTH = string.Empty;
            public string AREA_WEST = string.Empty;
            public string MATCH_TYPES = string.Empty;
            public string VALIDATION_RESULTS = string.Empty;
            public string GNIS_LINK = string.Empty;
            public string OSM_LINK = string.Empty;
            public string ID_LINK = string.Empty;
            public string JOSM_AREA_LINK = string.Empty;
            public string JOSM_OBJECT_LINK = string.Empty;
        }

        public TsvFileWriter(string? outputFileName)
        {
            if (string.IsNullOrEmpty(outputFileName))
                outputStreamWriter = new StreamWriter(Stream.Null);
            else
                outputStreamWriter = new StreamWriter(outputFileName);
            WriteOutputHeader();
        }

        private void WriteOutputHeader()
        {
            lock (outputStreamWriter)
            {
                bool first = true;
                foreach (var field in typeof(OutputRecord).GetFields())
                {
                    object name = field.Name;
                    if (first)
                        first = false;
                    else
                        outputStreamWriter.Write("\t");
                    outputStreamWriter.Write($"{name}");
                }
                outputStreamWriter.WriteLine();
            }
        }

        private void WriteOutputRecord(OutputRecord record)
        {
            lock (outputStreamWriter)
            {
                bool first = true;
                foreach (var field in typeof(OutputRecord).GetFields())
                {
                    object value = field.GetValue(record) ?? string.Empty;

                    if (first)
                        first = false;
                    else
                        outputStreamWriter.Write("\t");

                    outputStreamWriter.Write($"{value}");
                }
                outputStreamWriter.WriteLine();
                outputStreamWriter.Flush();
            }
        }

        public void WriteOutputRecord(GnisRecord gnisRecord, string? overpassQuery = null, GnisMatchResult? matchResult = null, GnisValidationResult? validationResult = null)
        {
            OutputRecord record = new();

            // make a two kilometer box with the feature at the center (i.e. 1 km in each direction)
            double[] twoKilometerBox = OverpassQueryBuilder.MakeBoundingBox(gnisRecord.Primary.Latitude, gnisRecord.Primary.Longitude, 2000);

            record.FEATURE_ID = gnisRecord.FeatureId;
            record.FEATURE_NAME = gnisRecord.FeatureName;
            record.FEATURE_CLASS = gnisRecord.FeatureClass;
            record.PRIM_LAT_DEC = gnisRecord.PrimaryLat;
            record.PRIM_LONG_DEC = gnisRecord.PrimaryLon;
            record.SOURCE_LAT_DEC = gnisRecord.SourceLat;
            record.SOURCE_LONG_DEC = gnisRecord.SourceLon;
            if (matchResult != null)
            {
                record.OSM_TYPE = matchResult.osmFeature.GetOsmType().ToString();
                record.OSM_ID = matchResult.osmFeature.Id.ToString();
                record.OSM_NAME = matchResult.osmFeature.GetName();
            }
            if (overpassQuery != null)
                record.OVERPASS_QUERY = overpassQuery;
            record.AREA_SOUTH = twoKilometerBox[0].ToString();
            record.AREA_WEST = twoKilometerBox[1].ToString();
            record.AREA_NORTH = twoKilometerBox[2].ToString();
            record.AREA_EAST = twoKilometerBox[3].ToString();
            record.MATCH_TYPES = matchResult != null ? matchResult.ToString() : string.Empty;
            record.VALIDATION_RESULTS = validationResult != null ? validationResult.ToString() : string.Empty;
            record.GNIS_LINK = $"=HYPERLINK(\"https://edits.nationalmap.gov/apps/gaz-domestic/public/summary/{record.FEATURE_ID}\",{record.FEATURE_ID})";
            record.OSM_LINK = $"=HYPERLINK(\"https://www.openstreetmap.org/#map=18/{record.PRIM_LAT_DEC}/{record.PRIM_LONG_DEC}\",\"{record.PRIM_LAT_DEC}/{record.PRIM_LONG_DEC}\")";
            if (matchResult != null)
            {
                record.ID_LINK = $"=HYPERLINK(\"https://www.openstreetmap.org/{matchResult.osmFeature.GetOsmType()}/{matchResult.osmFeature.Id}\",\"{matchResult.osmFeature.GetOsmType()}/{matchResult.osmFeature.Id}\")";
            }
            record.JOSM_AREA_LINK = $"=HYPERLINK(\"http://127.0.0.1:8111/load_and_zoom?left={record.AREA_WEST}&right={record.AREA_EAST}&top={record.AREA_NORTH}&bottom={record.AREA_SOUTH}\",\"{record.PRIM_LAT_DEC}/{record.PRIM_LONG_DEC}\")";
            if (matchResult != null)
            {
                record.JOSM_OBJECT_LINK = $"=HYPERLINK(\"http://127.0.0.1:8111/load_object?newlayer=false&objects={matchResult.osmFeature.GetOsmType().ToString().ToCharArray()[0]}{matchResult.osmFeature.Id}\",\"{matchResult.osmFeature.GetOsmType().ToString().ToCharArray()[0]}{matchResult.osmFeature.Id}\")";
            }

            WriteOutputRecord(record);
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

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~TsvOutputWriter()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}