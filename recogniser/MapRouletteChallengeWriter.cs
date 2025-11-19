// <copyright file="MapRouletteChallengeWriter.cs" company="recogniser project contributors">
// Copyright (c) 2025 recogniser project contributors.
// Licensed under the AGPL-3.0-or-later license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Recogniser
{
    using System.Text.Json;

    internal class MapRouletteChallengeWriter : IDisposable
    {
        private readonly GnisClassData gnisClassData;

        private readonly OsmChangeBuilder osmChangeBuilder;

        private readonly TagFixBuilder tagFixBuilder;

        private readonly MapRouletteTaskBuilder mapRouletteTaskBuilder;

        private readonly StreamWriter? innerStreamWriter;

        private readonly TextWriter outputStreamWriter;

        private readonly string outputType;

        private readonly Lock outputStreamWriterLock = new();

        private bool disposedValue;

        public MapRouletteChallengeWriter(GnisClassData gnisClassData, string? outputFileName, string outputType)
        {
            this.gnisClassData = gnisClassData;

            // configure the OsmChange builder
            osmChangeBuilder = new(gnisClassData);

            // configure the Tag Fix builder
            tagFixBuilder = new(gnisClassData);

            // configure the MapRoulette task builder
            mapRouletteTaskBuilder = new(gnisClassData);

            // open the output file
            if (string.IsNullOrEmpty(outputFileName))
            {
                innerStreamWriter = new StreamWriter(Stream.Null);
            }
            else
            {
                innerStreamWriter = new StreamWriter(outputFileName);
            }

            outputStreamWriter = TextWriter.Synchronized(innerStreamWriter);

            // set the output type
            this.outputType = outputType;
        }

        /// <summary>
        /// Write a MapRoulette task for a collection of OSM features that match the GNIS record.
        /// </summary>
        /// <param name="gnisRecord">The GNIS record.</param>
        /// <param name="matchResults">The matching results.</param>
        /// <param name="validationResults">The validation results.</param>
        public void WriteTask(GnisRecord gnisRecord, List<GnisMatchResult> matchResults, List<GnisValidationResult> validationResults)
        {
            bool allOk = true;

            foreach (GnisValidationResult validationResult in validationResults)
            {
                allOk &= validationResult.AllOk;
            }

            // if all the match results are fine
            if (allOk)
            {
                // don't write out a task
                return;
            }

            // build a plain task with all the results
            string mapRouletteTask = mapRouletteTaskBuilder.BuildPlainMapRouletteTask(gnisRecord, matchResults, validationResults);

            Program.Verbose.WriteLine(mapRouletteTask);

            lock (outputStreamWriterLock)
            {
                outputStreamWriter.WriteLine($"\u001e{mapRouletteTask}");
            }
        }

        /// <summary>
        /// Write a MapRoulette task for a single OSM feature that matched a GNIS record.
        /// </summary>
        /// <param name="gnisRecord">The GNIS record.</param>
        /// <param name="matchResult">The matching result.</param>
        /// <param name="validationResult">The validation result.</param>
        public void WriteTask(GnisRecord gnisRecord, GnisMatchResult matchResult, GnisValidationResult validationResult)
        {
            string outputType = this.outputType;

            // if the match result is a consolidated new relation
            // and we're building tag fix tasks
            if (matchResult.SpecialCondition == GnisMatchSpecialCondition.NEW_RELATION && !"collaborative".Equals(outputType, StringComparison.Ordinal))
            {
                // force this to be a plain task
                outputType = "plain";
            }

            // if we're building collaborative tasks
            if ("collaborative".Equals(outputType, StringComparison.Ordinal))
            {
                // build an OsmChange XML file
                string? osmChange = osmChangeBuilder.BuildOsmChange(gnisRecord, matchResult, validationResult);

                // don't output this task if there are no changes
                if (osmChange != null || matchResult.MatchType == GnisMatchType.ConflictingMatch)
                {
                    Program.Verbose.WriteLine(osmChange);
                    string mapRouletteTask = mapRouletteTaskBuilder.BuildCollaborativeMapRouletteTask(gnisRecord, matchResult, validationResult, osmChange);
                    Program.Verbose.WriteLine(mapRouletteTask);

                    lock (outputStreamWriterLock)
                    {
                        outputStreamWriter.WriteLine($"\u001e{mapRouletteTask}");
                    }
                }
            }

            // if we're building tag fix tasks
            else if ("tagfix".Equals(outputType, StringComparison.Ordinal))
            {
                // build a Tag Fix object
                List<TagFixOperation>? operations = tagFixBuilder.BuildTagFix(gnisRecord, matchResult, validationResult);

                // don't output this task if there are no changes
                if (operations != null)
                {
                    Program.Verbose.WriteLine(JsonSerializer.Serialize(operations));
                    string mapRouletteTask = mapRouletteTaskBuilder.BuildTagFixMapRouletteTask(gnisRecord, matchResult, validationResult, operations);
                    Program.Verbose.WriteLine(mapRouletteTask);

                    lock (outputStreamWriterLock)
                    {
                        outputStreamWriter.WriteLine($"\u001e{mapRouletteTask}");
                    }
                }
            }
            else if ("plain".Equals(outputType, StringComparison.Ordinal))
            {
                if (!validationResult.AllOk)
                {
                    string mapRouletteTask = mapRouletteTaskBuilder.BuildPlainMapRouletteTask(gnisRecord, matchResult, validationResult);
                    Program.Verbose.WriteLine(mapRouletteTask);

                    lock (outputStreamWriterLock)
                    {
                        outputStreamWriter.WriteLine($"\u001e{mapRouletteTask}");
                    }
                }
            }
            else
            {
                throw new ArgumentException($"Unknown output type: {outputType}");
            }
        }

        /// <summary>
        /// Write a MapRoulette task where there was no OSM feature match for the GNIS record.
        /// </summary>
        /// <param name="gnisRecord">The GNIS record.</param>
        public void WriteTask(GnisRecord gnisRecord)
        {
            if ("collaborative".Equals(outputType, StringComparison.Ordinal))
            {
                string? osmChange = osmChangeBuilder.BuildOsmChange(gnisRecord);
                Program.Verbose.WriteLine(osmChange);
                string mapRouletteTask = mapRouletteTaskBuilder.BuildCollaborativeMapRouletteTask(gnisRecord, osmChange);
                Program.Verbose.WriteLine(mapRouletteTask);

                lock (outputStreamWriterLock)
                {
                    outputStreamWriter.WriteLine($"\u001e{mapRouletteTask}");
                }
            }
            else if ("tagfix".Equals(outputType, StringComparison.Ordinal))
            {
                // can't output a tagfix task unless there's a match
            }
            else if ("plain".Equals(outputType, StringComparison.Ordinal))
            {
                string mapRouletteTask = mapRouletteTaskBuilder.BuildPlainMapRouletteTask(gnisRecord);
                Program.Verbose.WriteLine(mapRouletteTask);

                lock (outputStreamWriterLock)
                {
                    outputStreamWriter.WriteLine($"\u001e{mapRouletteTask}");
                }
            }
            else
            {
                throw new ArgumentException($"Unknown output type: {outputType}");
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
        // ~MapRouletteChallengeWriter()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }
    }
}