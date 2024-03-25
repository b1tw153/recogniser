namespace recogniser
{
    internal class OsmChangeWriter : IDisposable
    {
        private readonly TextWriter _outputStreamWriter;
        private readonly XOsmChange _osmChange = new();
        private readonly OsmChangeBuilder _osmChangeBuilder;
        private bool _disposedValue;

        public OsmChangeWriter(GnisClassData gnisClassData, string? osmChangeOutputPath)
        {
            _osmChangeBuilder = new(gnisClassData);

            // open the output file
            if (string.IsNullOrEmpty(osmChangeOutputPath))
                _outputStreamWriter = TextWriter.Synchronized(new StreamWriter(Stream.Null));
            else
                _outputStreamWriter = TextWriter.Synchronized(new StreamWriter(osmChangeOutputPath));
        }

        public void AddToOsmChange(GnisRecord gnisRecord, GnisMatchResult matchResult, GnisValidationResult validationResult)
        {
            _osmChangeBuilder.AddToOsmChange(_osmChange, gnisRecord, matchResult, validationResult);
        }

        public void AddToOsmChange(GnisRecord gnisRecord)
        {
            _osmChangeBuilder.AddToOsmChange(_osmChange, gnisRecord);
        }

        public void AddToOsmChange(GnisRecord gnisRecord, List<GnisMatchResult> matchResults, List<GnisValidationResult> validationResults)
        {
            // this is too complex for an automated change
            // it needs human intervention
            // don't add this to the OsmChange
        }

        public void WriteOsmChange()
        {
            lock (_outputStreamWriter)
            {
                _outputStreamWriter.Write(_osmChange.Serialize());
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                    _outputStreamWriter.Dispose();
                }

                // free unmanaged resources (unmanaged objects) and override finalizer
                // set large fields to null
                _disposedValue = true;
            }
        }

        // // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~OsmChangeWriter()
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