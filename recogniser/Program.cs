// <copyright file="Program.cs" company="recogniser project contributors">
// Copyright (c) 2025 recogniser project contributors.
// Licensed under the AGPL-3.0-or-later license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Recogniser
{
    using System.CommandLine;
    using System.Text.Json;

    internal class Program
    {
        public const string UserAgentBaseString = "recogniser-bot/0.1";

        public static readonly HashSet<string> ExtraGnisTags =
        [
            "gnis:ftype",
            "gnis:created",
            "gnis:county_id",
            "gnis:state_id",
            "gnis:created",
            "gnis:County",
            "gnis:ST_num",
            "gnis:County_num",
            "gnis:county_name",
            "gnis:feature_type",
            "gnis:Class",
            "gnis:import_uuid",
            "gnis:reviewed",
            "gnis:edited",
            "gnis:ST_alpha",
            "gnis:state",
            "gnis:county",
            "GNIS_Name",
            "gnis:feature",
            "GNID_NAME",
            "gnis:class",
            "gnis:review",
            "gnis:date_created",
            "gnis:date_edited",
            "gnis_name",
            "gnis:cre",
            "gnis:created_1",
            "gnis:state_alpha",
            "gnis:import_id",
            "gnis:fcode",
            "tiger:CLASSFP",
            "tiger:CPI",
            "tiger:FUNCSTAT",
            "tiger:LSAD",
            "tiger:MTFCC",
            "tiger:NAME",
            "tiger:NAMELSAD",
            "tiger:PCICBSA",
            "tiger:PCINECTA",
            "tiger:PLACEFP",
            "tiger:PLCIDFP",
            "tiger:STATEFP"
        ];

        private const string GnisClassDataPathDefault = @"conf/gnis_class_data.csv";
        private const string ErrataPathDefault = @"conf/errata.json";
        private const string OverpassUrlDefault = @"http://127.0.0.1/api/interpreter";
        private const string MapRouletteOutputTypeDefault = "collaborative";
        private const string PrivateDataPathDefault = @"conf/private_data.json";

        private static GnisMatcher? gnisMatcher;
        private static JPrivateData? privateData;
        private static GnisValidator? gnisValidator;

        public static TextWriter Verbose { get; private set; } = TextWriter.Synchronized(new StreamWriter(Stream.Null));

        public static TextWriter Progress { get; private set; } = TextWriter.Synchronized(new StreamWriter(Stream.Null));

        public static TextWriter Performance { get; private set; } = TextWriter.Synchronized(new StreamWriter(Stream.Null));

        public static bool AlwaysMatchGeometry { get; private set; }

        public static bool SkipMatches { get; private set; }

        public static GnisMatcher GnisMatcher
        {
            get
            {
                if (gnisMatcher == null)
                {
                    throw new InvalidOperationException("GnisMatcher is not initialized");
                }

                return gnisMatcher;
            }

            private set => gnisMatcher = value;
        }

        public static JPrivateData PrivateData
        {
            get
            {
                if (privateData == null)
                {
                    throw new InvalidOperationException("PrivateData is not initialized");
                }

                return privateData;
            }

            private set => privateData = value;
        }

        public static GnisValidator GnisValidator
        {
            get
            {
                if (gnisValidator == null)
                {
                    throw new InvalidOperationException("GnisValidator is not initialized");
                }

                return gnisValidator;
            }

            private set => gnisValidator = value;
        }

        public static HttpClient HttpClient { get; } = new();

        /// <summary>
        /// Main body of the command-line app.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        private static async Task<int> Main(string[] args)
        {
            var rootCommand = CreateRootCommand();
            return await rootCommand.InvokeAsync(args).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates and configures the root command with all options.
        /// </summary>
        private static RootCommand CreateRootCommand()
        {
            var rootCommand = new RootCommand("Search for GNIS features in OSM and output MapRoulette tasks or OSC XML to update them.");

            // Required options
            var gnisFileOption = new Option<string>(
                name: "--gnisFile",
                description: "File containing GNIS records with pipe-separated fields (REQUIRED)")
            { IsRequired = true };

            // Output file options
            var outputFileOption = new Option<string?>(
                name: "--outputFile",
                description: "Output TSV file containing search results");

            var mapRouletteFileOption = new Option<string?>(
                name: "--mapRouletteFile",
                description: "Output GeoJson file containing MapRoulette challenge data");

            var mapRouletteTypeOption = new Option<string>(
                name: "--mapRouletteType",
                getDefaultValue: () => MapRouletteOutputTypeDefault,
                description: "Type of MapRoulette tasks (collaborative/tagfix/plain)");

            var osmChangeFileOption = new Option<string?>(
                name: "--osmChangeFile",
                description: "Output OsmChange XML file containing all changes");

            // Configuration file options with defaults
            var privateDataOption = new Option<string?>(
                name: "--privateData",
                getDefaultValue: () => null,
                description: $"JSON file containing private configuration data (DEFAULT: {PrivateDataPathDefault})");

            var gnisClassDataOption = new Option<string?>(
                name: "--gnisClassData",
                getDefaultValue: () => null,
                description: $"CSV file containing GNIS class data (DEFAULT: {GnisClassDataPathDefault})");

            var errataOption = new Option<string?>(
                name: "--errata",
                getDefaultValue: () => null,
                description: $"JSON file containing errata records (DEFAULT: {ErrataPathDefault})");

            var overpassUrlOption = new Option<string?>(
                name: "--overpassUrl",
                getDefaultValue: () => null,
                description: $"URL for the Overpass interpreter (DEFAULT: {OverpassUrlDefault})");

            // Boolean switches
            var performanceOption = new Option<bool>(
                name: "--performance",
                description: "Write performance summary to stdout at the end");

            var progressOption = new Option<bool>(
                name: "--progress",
                description: "Write periodic progress updates to stdout");

            var verboseOption = new Option<bool>(
                name: "--verbose",
                description: "Write detailed progress data to stdout");

            var archivedOption = new Option<bool>(
                name: "--archived",
                description: "Process archived GNIS classes (excluded by default)");

            var alwaysMatchGeometryOption = new Option<bool>(
                name: "--alwaysMatchGeometry",
                description: "Process geometry matches for every OSM feature");

            var skipMatchesOption = new Option<bool>(
                name: "--skipMatches",
                description: "Output results only for GNIS records that didn't match");

            // Add all options to root command
            rootCommand.AddOption(gnisFileOption);
            rootCommand.AddOption(outputFileOption);
            rootCommand.AddOption(mapRouletteFileOption);
            rootCommand.AddOption(mapRouletteTypeOption);
            rootCommand.AddOption(osmChangeFileOption);
            rootCommand.AddOption(privateDataOption);
            rootCommand.AddOption(gnisClassDataOption);
            rootCommand.AddOption(errataOption);
            rootCommand.AddOption(overpassUrlOption);
            rootCommand.AddOption(performanceOption);
            rootCommand.AddOption(progressOption);
            rootCommand.AddOption(verboseOption);
            rootCommand.AddOption(archivedOption);
            rootCommand.AddOption(alwaysMatchGeometryOption);
            rootCommand.AddOption(skipMatchesOption);

            // Set the handler
            rootCommand.SetHandler((context) =>
            {
                RunApplication(
                    context.ParseResult.GetValueForOption(gnisFileOption) !,
                    context.ParseResult.GetValueForOption(outputFileOption),
                    context.ParseResult.GetValueForOption(mapRouletteFileOption),
                    context.ParseResult.GetValueForOption(mapRouletteTypeOption) !,
                    context.ParseResult.GetValueForOption(osmChangeFileOption),
                    context.ParseResult.GetValueForOption(privateDataOption),
                    context.ParseResult.GetValueForOption(gnisClassDataOption),
                    context.ParseResult.GetValueForOption(errataOption),
                    context.ParseResult.GetValueForOption(overpassUrlOption),
                    context.ParseResult.GetValueForOption(performanceOption),
                    context.ParseResult.GetValueForOption(progressOption),
                    context.ParseResult.GetValueForOption(verboseOption),
                    context.ParseResult.GetValueForOption(archivedOption),
                    context.ParseResult.GetValueForOption(alwaysMatchGeometryOption),
                    context.ParseResult.GetValueForOption(skipMatchesOption));
            });

            return rootCommand;
        }

        /// <summary>
        /// Main application logic.
        /// </summary>
        private static void RunApplication(
            string gnisFilePath,
            string? tsvFilePath,
            string? mapRouletteOutputPath,
            string mapRouletteOutputType,
            string? osmChangeOutputPath,
            string? privateDataPath,
            string? gnisClassDataPath,
            string? errataPath,
            string? overpassUrl,
            bool performance,
            bool progress,
            bool verbose,
            bool archived,
            bool alwaysMatchGeometry,
            bool skipMatches)
        {
            PerformanceTimer initializationTimer = new("Initialization");
            initializationTimer.Start(0);

            // initialize output writers that may go to the Console
            if (performance)
            {
                Performance = Console.Out;
            }

            if (progress)
            {
                Progress = Console.Out;
            }

            if (verbose)
            {
                Verbose = Console.Out;
            }

            // set the flag to always match geometry
            if (alwaysMatchGeometry)
            {
                AlwaysMatchGeometry = true;
            }

            // set the flag to skip matched GNIS records
            if (skipMatches)
            {
                SkipMatches = true;
            }

            // path to the directory containing executable file
            string exeDirPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ".";

            // Apply defaults for configuration file paths
            privateDataPath ??= Path.Combine(exeDirPath, PrivateDataPathDefault);
            gnisClassDataPath ??= Path.Combine(exeDirPath, GnisClassDataPathDefault);
            errataPath ??= Path.Combine(exeDirPath, ErrataPathDefault);
            overpassUrl ??= OverpassUrlDefault;

            // read the private data file
            PrivateData = JsonSerializer.Deserialize<JPrivateData>(File.ReadAllText(privateDataPath)) ?? new JPrivateData();

            // set default user agent string
            if (string.IsNullOrEmpty(PrivateData.UserAgent) && !string.IsNullOrEmpty(PrivateData.OperatorEmail))
            {
                // note that Wikidata expects the user agent string to be "program-name/0.0 (user@emailhost)"
                PrivateData.UserAgent = $"{UserAgentBaseString} ({PrivateData.OperatorEmail})";
            }

            // read the GNIS class attributes
            GnisClassData gnisClassData = new(gnisClassDataPath);

            // configure the query builder
            OverpassQueryBuilder overpassQueryBuilder = new(gnisClassData, overpassUrl);

            // configure the gnis matcher
            GnisMatcher = new(gnisClassData);

            // configure the gnis validator
            GnisValidator = new(gnisClassData);

            // read the errata file
            JErrata errataObject = JsonSerializer.Deserialize<JErrata>(File.ReadAllText(errataPath)) ?? new JErrata();
            Dictionary<string, Erratum> errata = [];
            foreach (Erratum erratum in errataObject.Errata)
            {
                errata.Add(erratum.Id, erratum);
            }

            Verbose.WriteLine(Directory.GetCurrentDirectory());

            // set up the output file writers
            using GnisFileReader gnisFileReader = new(gnisFilePath);
            using TsvFileWriter tsvFileWriter = new(tsvFilePath);
            using MapRouletteChallengeWriter mapRouletteChallengeWriter = new(gnisClassData, mapRouletteOutputPath, mapRouletteOutputType);
            using OsmChangeWriter osmChangeWriter = new(gnisClassData, osmChangeOutputPath);

            PerformanceTimer runTimer = new("Entire Run");
            PerformanceTimer gnisRecordTimer = new("Entire Record");
            PerformanceTimer proximityQueryTimer = new("Proximity Query");
            PerformanceTimer proximityMatchTimer = new("Proximity Match");
            PerformanceTimer secondQueryTimer = new("Second Query");
            PerformanceTimer secondMatchTimer = new("Second Match");
            PerformanceTimer validationTimer = new("Validation");
            PerformanceTimer outputTimer = new("Output");

            Verbose.WriteLine("Initialized");
            initializationTimer.Stop(0);
            runTimer.Start(0);

            // process each GNIS record in parallel
            // this proves to be a more robust threading model than breaking the processing down into smaller tasks
            Parallel.ForEach(gnisFileReader, new ParallelOptions { MaxDegreeOfParallelism = 8 }, (fileRecord, parallelLoopState, iteration) =>
            {
                gnisRecordTimer.Start(iteration);

                GnisRecord gnisRecord = fileRecord;
                List<GnisMatchResult> matchResults;
                string overpassQuery;
                XOsmData? osmData;
                Erratum erratum = errata.TryGetValue(gnisRecord.FeatureId, out Erratum? value) ? value : Erratum.Empty;

                Progress.WriteLine($"{gnisRecord.FeatureId} {gnisRecord.FeatureName} ({gnisRecord.FeatureClass})");

                // if the errata contains a replacement for this record
                if (!string.IsNullOrEmpty(erratum.Substitute))
                {
                    gnisRecord = gnisFileReader.ParseRecord(erratum.Substitute);
                    Progress.WriteLine($"{gnisRecord.FeatureId} {gnisRecord.FeatureName} ({gnisRecord.FeatureClass}) -- Substitute");
                }

                GnisClassAttributes gnisClassAttributes = gnisClassData.GetGnisClassAttributes(gnisRecord.FeatureClass);

                bool censusDivision = (gnisRecord.FeatureClass.Equals("Census", StringComparison.Ordinal) || gnisRecord.FeatureClass.Equals("Civil", StringComparison.Ordinal)) && gnisRecord.FeatureName.EndsWith("Division", StringComparison.Ordinal);

                // skip all the classes that are not current because we can't link to the gnis records
                // and skip all records flagged to be skipped in the errata
                // and skip all records with 0,0 as primary coordinates
                // and skip all records for census divisions because we don't need to map them
                if ((!gnisClassAttributes.Current && !archived) || erratum.Skip || gnisRecord.HasZeroPrimary() || censusDivision)
                {
                    Progress.WriteLine("...");

                    gnisRecordTimer.Stop(iteration);

                    // continue with the next GNIS record
                    return;
                }
                else
                {
                    proximityQueryTimer.Start(iteration);

                    // first pass query for nodes, ways, and relations near the feature's primary coordinates

                    // if the errata does not specify an OSM object to be used
                    if (erratum.Use == null)
                    {
                        // build the proximity query
                        overpassQuery = overpassQueryBuilder.BuildProximityQuery(gnisRecord);
                    }
                    else
                    {
                        // build a query for the specific OSM feature
                        overpassQuery = OverpassQueryBuilder.BuildObjectQuery(erratum.Use.Type, erratum.Use.Ref);
                    }

                    Verbose.WriteLine(overpassQuery);

                    osmData = overpassQueryBuilder.SendQuery(overpassQuery);

                    proximityQueryTimer.Stop(iteration);
                    proximityMatchTimer.Start(iteration);

                    matchResults = GnisMatcher.GetMatchResults(gnisRecord, osmData);

                    proximityMatchTimer.Stop(iteration);
                }

                // if there are no match results or if this is a waterway
                // waterways get special treatment because we need to find all the component ways even if they're not part of a relation
                if (matchResults.Count == 0 || gnisClassAttributes.IsWaterwayClass())
                {
                    // second pass query for features with matching name and primary tag over a larger area

                    secondQueryTimer.Start(iteration);

                    overpassQuery = overpassQueryBuilder.BuildSecondQuery(gnisRecord);

                    Verbose.WriteLine(overpassQuery);

                    osmData = XOsmData.Merge(osmData, overpassQueryBuilder.SendQuery(overpassQuery));

                    secondQueryTimer.Stop(iteration);
                    secondMatchTimer.Start(iteration);

                    List<GnisMatchResult> secondMatchResults = GnisMatcher.GetMatchResults(gnisRecord, osmData);

                    // remove duplicate matches
                    foreach (GnisMatchResult firstResult in matchResults)
                    {
                        for (int i = 0; i < secondMatchResults.Count; i++)
                        {
                            if (firstResult.OsmFeature.Id == secondMatchResults[i].OsmFeature.Id)
                            {
                                secondMatchResults.RemoveAt(i);
                                i--;
                            }
                        }
                    }

                    matchResults.AddRange(secondMatchResults);

                    secondMatchTimer.Stop(iteration);
                }

                validationTimer.Start(iteration);

                // see if there's an exact match in the results
                bool exactMatch = false;
                foreach (GnisMatchResult matchResult in matchResults)
                {
                    if (matchResult.MatchType == GnisMatchType.ExactMatch)
                    {
                        exactMatch = true;
                        break;
                    }
                }

                // if there is an exact match in the results
                if (exactMatch)
                {
                    // remove all the close matches
                    for (int i = 0; i < matchResults.Count; i++)
                    {
                        if (matchResults[i].MatchType != GnisMatchType.ExactMatch)
                        {
                            matchResults.Remove(matchResults[i]);
                            i--;
                        }
                    }
                }

                List<GnisValidationResult> validationResults = [];

                // validate each match and output the TSV data
                foreach (GnisMatchResult matchResult in matchResults)
                {
                    GnisValidationResult validationResult = GnisValidator.ValidateOsmFeature(gnisRecord, matchResult);
                    validationResults.Add(validationResult);

                    // write output with match details
                    if (!SkipMatches)
                    {
                        tsvFileWriter.WriteOutputRecord(gnisRecord, overpassQuery, matchResult, validationResult);
                    }
                }

                validationTimer.Stop(iteration);
                outputTimer.Start(iteration);

                // if there was still no match for the record
                if (matchResults.Count == 0)
                {
                    // don't write output for historical features with no match
                    if (!gnisRecord.IsHistorical())
                    {
                        // the feature is not historical

                        // write output without any match or validation data
                        tsvFileWriter.WriteOutputRecord(gnisRecord);

                        // write MapRoulette challenge without any match or validation data
                        mapRouletteChallengeWriter.WriteTask(gnisRecord);

                        // add to OsmChange without any match or validation data
                        osmChangeWriter.AddToOsmChange(gnisRecord);
                    }

                    // if the feature is historical and we found a match
                    // we wrote the record out earlier so that it can be validated and updated as needed
                }
                else if (matchResults.Count == 1 && !SkipMatches)
                {
                    // write MapRoulette task with match details
                    mapRouletteChallengeWriter.WriteTask(gnisRecord, matchResults[0], validationResults[0]);

                    // add to OsmChange with match details
                    osmChangeWriter.AddToOsmChange(gnisRecord, matchResults[0], validationResults[0]);
                }
                else if (!SkipMatches)
                {
                    GnisMatchResult? bestResult = GnisMatcher.FindBestMatch(matchResults);
                    if (bestResult != null)
                    {
                        int index = matchResults.IndexOf(bestResult);

                        // write MapRoulette task with match details
                        mapRouletteChallengeWriter.WriteTask(gnisRecord, matchResults[index], validationResults[index]);

                        // add to OsmChange with match details
                        osmChangeWriter.AddToOsmChange(gnisRecord, matchResults[index], validationResults[index]);
                    }
                    else
                    {
                        GnisMatchResult? newMatchResult = null;
                        GnisValidationResult? newValidationResult = null;
                        GnisMatcher.ConsolidateMatches(gnisRecord, matchResults, validationResults, newMatchResult, newValidationResult);

                        if (newMatchResult != null && newValidationResult != null)
                        {
                            // write MapRoulette task with new match details
                            mapRouletteChallengeWriter.WriteTask(gnisRecord, newMatchResult, newValidationResult);

                            // add to OsmChange with new match details
                            osmChangeWriter.AddToOsmChange(gnisRecord, newMatchResult, newValidationResult);
                        }
                        else
                        {
                            // output a simple MapRoulette task for the full collection of results
                            mapRouletteChallengeWriter.WriteTask(gnisRecord, matchResults, validationResults);

                            // skip the OsmChange because this task needs human intervention
                            // osmChangeWriter.AddToOsmChange(gnisRecord, matchResults, validationResults);
                        }
                    }
                }

                outputTimer.Stop(iteration);
                gnisRecordTimer.Stop(iteration);
            });

            // write the OsmChange XML to the output file
            osmChangeWriter.WriteOsmChange();

            runTimer.Stop(0);

            Performance.WriteLine(initializationTimer.GetSummary());
            Performance.WriteLine(proximityQueryTimer.GetSummary());
            Performance.WriteLine(proximityMatchTimer.GetSummary());
            Performance.WriteLine(secondQueryTimer.GetSummary());
            Performance.WriteLine(secondMatchTimer.GetSummary());
            Performance.WriteLine(validationTimer.GetSummary());
            Performance.WriteLine(outputTimer.GetSummary());
            Performance.WriteLine(gnisRecordTimer.GetSummary());
            Performance.WriteLine(runTimer.GetSummary());
        }
    }
}
