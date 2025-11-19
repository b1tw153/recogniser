// <copyright file="GnisFileReader.cs" company="recogniser project contributors">
// Copyright (c) 2025 recogniser project contributors.
// Licensed under the AGPL-3.0-or-later license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Recogniser
{
    using System.Collections;

    /// <summary>
    /// This class reads pipe-delimited text files containing GNIS data.
    /// </summary>
    internal class GnisFileReader : IDisposable, IEnumerable<GnisRecord>
    {
        /// <summary>
        /// TextReader for the file containing GNIS data.
        /// </summary>
        private readonly TextReader gnisFileStreamReader;

        /// <summary>
        /// Lock object for thread-safe access to gnisFileStreamReader.
        /// </summary>
        private readonly Lock readerLock = new();

        /// <summary>
        /// List of field names from the header of the GNIS file.
        /// </summary>
        private string[]? gnisFileFieldNames;

        /// <summary>
        /// Flag to indicate whether the gnisFileStreamReader has been disposed.
        /// </summary>
        private bool disposedValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="GnisFileReader"/> class.
        /// </summary>
        /// <param name="gnisFilePath">The absolute or relative path to the file containing pipe-delimited GNIS data.</param>
        public GnisFileReader(string gnisFilePath)
        {
            // if the path is not valid
            if (!File.Exists(gnisFilePath))
            {
                throw new FileNotFoundException("Path to GNIS file is not valid: " + gnisFilePath);
            }

            // open a StreamReader to the file
            gnisFileStreamReader = new StreamReader(gnisFilePath);

            // if we were unable to open the StreamReader (but didn't get an exception?)
            if (gnisFileStreamReader == null)
            {
                throw new IOException("Unable to open GNIS file: " + gnisFilePath);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GnisFileReader"/> class.
        /// </summary>
        /// <param name="stdin">The TextReader to read GNIS data from.</param>
        public GnisFileReader(TextReader stdin)
        {
            gnisFileStreamReader = stdin;
        }

        /// <summary>
        /// Reads a record from the GNIS data file and returns a GnisRecord object containing the field names and values.
        /// </summary>
        /// <returns>A GnisRecord containing the field names and values, or null if at EOF.</returns>
        public GnisRecord? ReadRecord()
        {
            // if we haven't read the header yet
            if (gnisFileFieldNames == null)
            {
                // read and split the header
                ReadHeader();
            }

            lock (readerLock)
            {
                // read the next line from the file
                string? line = gnisFileStreamReader.ReadLine();

                // if we couldn't read a line from the file (at eof)
                if (line == null)
                {
                    // return null to indicate eof
                    return null;
                }

                // return the dictionary object with field names and values
                return ParseRecord(line);
            }
        }

        /// <summary>
        /// Parses a line of pipe-delimited text into a GnisRecord.
        /// </summary>
        /// <param name="line">The line of text to parse.</param>
        /// <returns>A GnisRecord containing the parsed field names and values.</returns>
        public GnisRecord ParseRecord(string line)
        {
            // split the line using pipe delimiters
            string[] fields = line.Split("|");

            // if the line doesn't have the same number of fields as the header
            if (gnisFileFieldNames == null || fields.Length != gnisFileFieldNames.Length)
            {
                throw new InvalidDataException("Line does not have the same number of fields as the header: " + line);
            }

            // create a result object
            GnisRecord result = [];

            // for each field in the header
            for (int i = 0; i < gnisFileFieldNames.Length; i++)
            {
                // clean up "\N" null values from mysql
                if ("\\N".Equals(fields[i], StringComparison.Ordinal))
                {
                    fields[i] = string.Empty;
                }

                // add the field name and value to the result
                result.Add(gnisFileFieldNames[i], fields[i]);
            }

            return result;
        }

        /// <summary>
        /// Releases all resources used by the GnisFileReader.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        public IEnumerator<GnisRecord> GetEnumerator()
        {
            GnisRecord? gnisRecord = ReadRecord();
            while (gnisRecord != null)
            {
                yield return gnisRecord;
                gnisRecord = ReadRecord();
            }
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Releases the unmanaged resources used by the GnisFileReader and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    gnisFileStreamReader.Dispose();
                }

                disposedValue = true;
            }
        }

        /// <summary>
        /// Reads and parses the header line from the GNIS data file.
        /// </summary>
        private void ReadHeader()
        {
            lock (readerLock)
            {
                // Read the first line of the file
                string header = gnisFileStreamReader.ReadLine()
                    ?? throw new InvalidDataException("Unable to read header from GNIS file.");

                // Convert new GNIS file header to upper case to match old file header
                header = header.ToUpperInvariant();

                // split the header line with pipe delimiters
                gnisFileFieldNames = header.Split("|");
            }
        }
    }
}