using System.Collections;

namespace recogniser
{
    /// <summary>
    /// <c>GnisFileReader</c>
    /// <para>This class reads pipe-delimited text files containing GNIS data</para>
    /// </summary>
    public class GnisFileReader : IDisposable, IEnumerable<GnisRecord>
    {
        /// <summary>
        /// TextReader for the file containing GNIS data.
        /// </summary>
        readonly TextReader gnisFileStreamReader;

        /// <summary>
        /// List of field names from the header of the GNIS file.
        /// </summary>
        string[]? gnisFileFieldNames = null;

        /// <summary>
        /// Flag to indicate whether the gnisFileStreamReader has been disposed.
        /// </summary>
        private bool disposedValue;

        /// <summary>
        /// <c>GnisFileReader(string)</c>
        /// <para>Create a new GnisFileReader with a path to the file to be read.</para>
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
                throw new Exception("Unable to open GNIS file: " + gnisFilePath);
            }
        }

        public GnisFileReader(TextReader stdin)
        {
            gnisFileStreamReader = stdin;
        }

        /// <summary>
        /// <c>ReadHeader</c>
        /// <para>Read and parse the header line in the GNIS data file.</para>
        /// </summary>
        /// <exception cref="Exception"></exception>
        private void ReadHeader()
        {
            lock (gnisFileStreamReader)
            {
                // Read the first line of the file
                string header = gnisFileStreamReader.ReadLine()
                    ?? throw new Exception("Unable to read header from GNIS file.");

                // Convert new GNIS file header to upper case to match old file header
                header = header.ToUpperInvariant();

                // split the header line with pipe delimiters
                gnisFileFieldNames = header.Split("|");
            }
        }

        /// <summary>
        /// <c>ReadRecord</c>
        /// <para>Read a record from the GNIS data file and return a Dictionary object containing the record field names and values.</para>
        /// </summary>
        /// <returns>A Dictionary object containing the record field names and values, or null if at EOF.</returns>
        /// <exception cref="Exception">If the record could not be read</exception>
        public GnisRecord? ReadRecord()
        {
            // if we haven't read the header yet
            if (gnisFileFieldNames == null)
            {
                // read and split the header
                ReadHeader();
            }

            lock (gnisFileStreamReader)
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

        public GnisRecord ParseRecord(string line)
        {
            // split the line using pipe delimiters
            string[] fields = line.Split("|");

            // if the line doesn't have the same number of fields as the header
            if (gnisFileFieldNames == null || fields.Length != gnisFileFieldNames.Length)
            {
                throw new Exception("Line does not have the same number of fields as the header: " + line);
            }

            // create a result object
            GnisRecord result = new();  

            // for each field in the header
            for (int i = 0; i < gnisFileFieldNames.Length; i++)
            {
                // clean up "\N" null values from mysql
                if ("\\N".Equals(fields[i]))
                    fields[i] = string.Empty;

                // add the field name and value to the result
                result.Add(gnisFileFieldNames[i], fields[i]);
            }

            return result;
        }

        /// <summary>
        /// <c>Dispose(bool)</c>
        /// <para>Dispose of the GnisFileReader</para>
        /// </summary>
        /// <param name="disposing">False if this method is called by the runtime finalizer.</param>
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
        /// <c>Dispose</c>
        /// <para>Implementation of the required IDispose method.</para>
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public IEnumerator<GnisRecord> GetEnumerator()
        {
            GnisRecord? gnisRecord = ReadRecord();
            while (gnisRecord != null)
            {
                yield return gnisRecord;
                gnisRecord = ReadRecord();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}