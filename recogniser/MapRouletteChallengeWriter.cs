using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace recogniser
{
    public class MapRouletteChallengeWriter : IDisposable
    {
        private readonly GnisClassData _gnisClassData;

        private readonly OsmChangeBuilder _osmChangeBuilder;

        private readonly TagFixBuilder _tagFixBuilder;

        private readonly MapRouletteTaskBuilder _mapRouletteTaskBuilder;

        private readonly TextWriter _outputStreamWriter;

        private readonly string _outputType;

        private bool _disposedValue;

        public MapRouletteChallengeWriter(GnisClassData gnisClassData, string? outputFileName, string outputType)
        {
            _gnisClassData = gnisClassData;

            // configure the OsmChange builder
            _osmChangeBuilder = new(gnisClassData);

            // configure the Tag Fix builder
            _tagFixBuilder = new(gnisClassData);

            // configure the MapRoulette task builder
            _mapRouletteTaskBuilder = new(gnisClassData);

            // open the output file
            if (string.IsNullOrEmpty(outputFileName))
                _outputStreamWriter = TextWriter.Synchronized(new StreamWriter(Stream.Null));
            else
                _outputStreamWriter = TextWriter.Synchronized(new StreamWriter(outputFileName));

            // set the output type
            this._outputType = outputType;
        }

        /// <summary>
        /// Write a MapRoulette task for a collection of OSM features that match the GNIS record.
        /// </summary>
        /// <param name="gnisRecord"></param>
        /// <param name="matchResults"></param>
        /// <param name="validationResults"></param>
        public void WriteTask(GnisRecord gnisRecord, List<GnisMatchResult> matchResults, List<GnisValidationResult> validationResults)
        {
            bool allOk = true;

            foreach (GnisValidationResult validationResult in validationResults)
            {
                allOk &= validationResult.AllOk;
            }

            // if all the match results are fine
            if (allOk)
                // don't write out a task
                return;

            // build a plain task with all the results
            string mapRouletteTask = _mapRouletteTaskBuilder.BuildPlainMapRouletteTask(gnisRecord, matchResults, validationResults);

            Program.Verbose.WriteLine(mapRouletteTask);

            lock (_outputStreamWriter)
            {
                _outputStreamWriter.WriteLine($"\u001e{mapRouletteTask}");
            }
        }

        /// <summary>
        /// Write a MapRoulette task for a single OSM feature that matched a GNIS record
        /// </summary>
        /// <param name="gnisRecord"></param>
        /// <param name="matchResult"></param>
        /// <param name="validationResult"></param>
        public void WriteTask(GnisRecord gnisRecord, GnisMatchResult matchResult, GnisValidationResult validationResult)
        {
            string outputType = _outputType;

            // if the match result is a consolidated new relation
            // and we're building tag fix tasks
            if (matchResult.specialCondition == GnisMatchSpecialCondition.NEW_RELATION && !"collaborative".Equals(outputType))
                // force this to be a plain task
                outputType = "plain";

            // if we're building collaborative tasks
            if ("collaborative".Equals(outputType))
            {
                // build an OsmChange XML file
                string? osmChange = _osmChangeBuilder.BuildOsmChange(gnisRecord, matchResult, validationResult);

                // don't output this task if there are no changes
                if (osmChange != null || matchResult.MatchType == GnisMatchType.conflictingMatch)
                {
                    Program.Verbose.WriteLine(osmChange);
                    string mapRouletteTask = _mapRouletteTaskBuilder.BuildCollaborativeMapRouletteTask(gnisRecord, matchResult, validationResult, osmChange);
                    Program.Verbose.WriteLine(mapRouletteTask);

                    lock (_outputStreamWriter)
                    {
                        _outputStreamWriter.WriteLine($"\u001e{mapRouletteTask}");
                    }
                }
            }
            // if we're building tag fix tasks
            else if ("tagfix".Equals(outputType))
            {
                // build a Tag Fix object
                List<TagFixOperation>? operations = _tagFixBuilder.BuildTagFix(gnisRecord, matchResult, validationResult);

                // don't output this task if there are no changes
                if (operations != null)
                {
                    Program.Verbose.WriteLine(JsonSerializer.Serialize(operations));
                    string mapRouletteTask = _mapRouletteTaskBuilder.BuildTagFixMapRouletteTask(gnisRecord, matchResult, validationResult, operations);
                    Program.Verbose.WriteLine(mapRouletteTask);

                    lock (_outputStreamWriter)
                    {
                        _outputStreamWriter.WriteLine($"\u001e{mapRouletteTask}");
                    }
                }
            }
            else if ("plain".Equals(outputType))
            {
                if (!validationResult.AllOk)
                {
                    string mapRouletteTask = _mapRouletteTaskBuilder.BuildPlainMapRouletteTask(gnisRecord, matchResult, validationResult);
                    Program.Verbose.WriteLine(mapRouletteTask);

                    lock (_outputStreamWriter)
                    {
                        _outputStreamWriter.WriteLine($"\u001e{mapRouletteTask}");
                    }
                }
            }
            else
            {
                throw new Exception($"Unknown output type: {outputType}");
            }
        }

        /// <summary>
        /// Write a MapRoulette task where there was no OSM feature match for the GNIS record
        /// </summary>
        /// <param name="gnisRecord"></param>
        public void WriteTask(GnisRecord gnisRecord)
        {
            if ("collaborative".Equals(_outputType))
            {
                string? osmChange = _osmChangeBuilder.BuildOsmChange(gnisRecord);
                Program.Verbose.WriteLine(osmChange);
                string mapRouletteTask = _mapRouletteTaskBuilder.BuildCollaborativeMapRouletteTask(gnisRecord, osmChange);
                Program.Verbose.WriteLine(mapRouletteTask);

                lock (_outputStreamWriter)
                {
                    _outputStreamWriter.WriteLine($"\u001e{mapRouletteTask}");
                }
            }
            else if ("tagfix".Equals(_outputType))
            {
                // can't output a tagfix task unless there's a match
            }
            else if ("plain".Equals(_outputType))
            {
                string mapRouletteTask = _mapRouletteTaskBuilder.BuildPlainMapRouletteTask(gnisRecord);
                Program.Verbose.WriteLine(mapRouletteTask);

                lock (_outputStreamWriter)
                {
                    _outputStreamWriter.WriteLine($"\u001e{mapRouletteTask}");
                }
            }
            else
            {
                throw new Exception($"Unknown output type: {_outputType}");
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
        // ~MapRouletteChallengeWriter()
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