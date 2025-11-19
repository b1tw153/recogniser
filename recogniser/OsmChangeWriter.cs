// <copyright file="OsmChangeWriter.cs" company="recogniser project contributors">
// Copyright (c) 2025 recogniser project contributors.
// Licensed under the AGPL-3.0-or-later license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Recogniser
{
    internal class OsmChangeWriter : IDisposable
    {
        private readonly TextWriter outputStreamWriter;
        private readonly XOsmChange osmChange = new();
        private readonly OsmChangeBuilder osmChangeBuilder;
        private readonly StreamWriter? innerStreamWriter;
        private readonly Lock outputStreamWriterLock = new();
        private bool disposedValue;

        public OsmChangeWriter(GnisClassData gnisClassData, string? osmChangeOutputPath)
        {
            osmChangeBuilder = new(gnisClassData);

            // open the output file
            if (string.IsNullOrEmpty(osmChangeOutputPath))
            {
                innerStreamWriter = new StreamWriter(Stream.Null);
            }
            else
            {
                innerStreamWriter = new StreamWriter(osmChangeOutputPath);
            }

            outputStreamWriter = TextWriter.Synchronized(innerStreamWriter);
        }

        public static void AddToOsmChange(GnisRecord gnisRecord, List<GnisMatchResult> matchResults, List<GnisValidationResult> validationResults)
        {
            // this is too complex for an automated change
            // it needs human intervention
            // don't add this to the OsmChange
        }

        public void AddToOsmChange(GnisRecord gnisRecord, GnisMatchResult matchResult, GnisValidationResult validationResult)
        {
            osmChangeBuilder.AddToOsmChange(osmChange, gnisRecord, matchResult, validationResult);
        }

        public void AddToOsmChange(GnisRecord gnisRecord)
        {
            osmChangeBuilder.AddToOsmChange(osmChange, gnisRecord);
        }

        public void WriteOsmChange()
        {
            lock (outputStreamWriterLock)
            {
                outputStreamWriter.Write(osmChange.Serialize());
            }
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
                    // dispose managed state (managed objects)
                    outputStreamWriter.Dispose();
                    innerStreamWriter?.Dispose();
                }

                // free unmanaged resources (unmanaged objects) and override finalizer
                // set large fields to null
                disposedValue = true;
            }
        }

        // // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~OsmChangeWriter()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }
    }
}