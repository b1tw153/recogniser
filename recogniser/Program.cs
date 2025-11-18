using System.CommandLine;
using System.Text.Json;

namespace recogniser
{
    /// <summary>
    /// Configuration class to hold all command line arguments
    /// </summary>
    public class CommandLineOptions
    {
        public string? GnisFile { get; set; }
        public string? OutputFile { get; set; }
        public string? MapRouletteFile { get; set; }
        public string MapRouletteType { get; set; } = "collaborative";
        public string? OsmChangeFile { get; set; }
        public string? PrivateData { get; set; }
        public string? GnisClassData { get; set; }
        public string? Errata { get; set; }
        public string? OverpassUrl { get; set; }
        public bool Performance { get; set; }
        public bool Progress { get; set; }
        public bool Verbose { get; set; }
        public bool Archived { get; set; }
        public bool SkipMatches { get; set; }
        public bool AlwaysMatchGeometry { get; set; }
    }

    public class Program
    {
        public static readonly string userAgentBaseString = "recogniser-bot/0.1";

        public static readonly HashSet<string> extraGnisTags = new()
        {
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
        };

        private static TextWriter _verbose = TextWriter.Synchronized(new StreamWriter(Stream.Null));
        private static TextWriter _progress = TextWriter.Synchronized(new StreamWriter(Stream.Null));
        private static TextWriter _performance = TextWriter.Synchronized(new StreamWriter(Stream.Null));

        public static TextWriter Verbose { get { return _verbose; } }
        public static TextWriter Progress { get { return _progress; } }
        public static TextWriter Performance { get { return _performance; } }

        private static readonly string gnisClassDataPathDefault = @"conf/gnis_class_data.csv";
        private static readonly string errataPathDefault = @"conf/errata.json";
        private static readonly string overpassUrlDefault = @"http://127.0.0.1/api/interpreter";
        private static readonly string mapRouletteOutputTypeDefault = "collaborative";
        private static readonly string privateDataPathDefault = @"conf/private_data.json";

        private static bool _alwaysMatchGeometry = false;
        private static bool _skipMatches = false;

        public static bool AlwaysMatchGeometry { get { return _alwaysMatchGeometry; } }

        private static GnisMatcher? _gnisMatcher;

        public static GnisMatcher GnisMatcher
        {
            get
            {
                if (_gnisMatcher == null)
                    throw new Exception("GnisMatcher is not initialized");
                return _gnisMatcher;
            }
        }

        private static JPrivateData? _privateData;

        public static JPrivateData PrivateData
        {
            get
            {
                if (_privateData == null)
                    throw new Exception("PrivateData is not initialized");
                return _privateData;
            }
        }

        private static GnisValidator? _gnisValidator;

        public static GnisValidator GnisValidator
        {
            get
            {
                if (_gnisValidator == null)
                    throw new Exception("GnisValidator is not initialized");
                return _gnisValidator;
            }
        }

        private readonly static HttpClient _httpClient = new();

        public static HttpClient HttpClient { get { return _httpClient; } }

        /// <summary>
        /// Main body of the command-line app
        /// </summary>
        /// <param name="args">Command-line arguments</param>
        static async Task<int> Main(string[] args)
        {
            var rootCommand = CreateRootCommand();
            return await rootCommand.InvokeAsync(args);
        }

        /// <summary>
        /// Creates and configures the root command with all options
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
                getDefaultValue: () => mapRouletteOutputTypeDefault,
                description: "Type of MapRoulette tasks (collaborative/tagfix/plain)");

            var osmChangeFileOption = new Option<string?>(
                name: "--osmChangeFile",
                description: "Output OsmChange XML file containing all changes");

            // Configuration file options with defaults
            var privateDataOption = new Option<string?>(
                name: "--privateData",
                getDefaultValue: () => null,
                description: $"JSON file containing private configuration data (DEFAULT: {privateDataPathDefault})");

            var gnisClassDataOption = new Option<string?>(
                name: "--gnisClassData",
                getDefaultValue: () => null,
                description: $"CSV file containing GNIS class data (DEFAULT: {gnisClassDataPathDefault})");

            var errataOption = new Option<string?>(
                name: "--errata",
                getDefaultValue: () => null,
                description: $"JSON file containing errata records (DEFAULT: {errataPathDefault})");

            var overpassUrlOption = new Option<string?>(
                name: "--overpassUrl",
                getDefaultValue: () => null,
                description: $"URL for the Overpass interpreter (DEFAULT: {overpassUrlDefault})");

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
            rootCommand.SetHandler(
                RunApplication,
                gnisFileOption,
                outputFileOption,
                mapRouletteFileOption,
                mapRouletteTypeOption,
                osmChangeFileOption,
                privateDataOption,
                gnisClassDataOption,
                errataOption,
                overpassUrlOption,
                performanceOption,
                progressOption,
                verboseOption,
                archivedOption,
                alwaysMatchGeometryOption,
                skipMatchesOption);

            return rootCommand;
        }

        /// <summary>
        /// Main application logic
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
            try
            {
                PerformanceTimer initializationTimer = new("Initialization");
                initializationTimer.Start(0);

                // initialize output writers that may go to the Console
                if (performance)
                    _performance = Console.Out;
                if (progress)
                    _progress = Console.Out;
                if (verbose)
                    _verbose = Console.Out;

                // set the flag to always match geometry
                if (alwaysMatchGeometry)
                    _alwaysMatchGeometry = true;

                // set the flag to skip matched GNIS records
                if (skipMatches)
                    _skipMatches = true;

                // path to the directory containing executable file
                string exeDirPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ".";

                // Apply defaults for configuration file paths
                privateDataPath ??= Path.Combine(exeDirPath, privateDataPathDefault);
                gnisClassDataPath ??= Path.Combine(exeDirPath, gnisClassDataPathDefault);
                errataPath ??= Path.Combine(exeDirPath, errataPathDefault);
                overpassUrl ??= overpassUrlDefault;

                // read the private data file
                _privateData = JsonSerializer.Deserialize<JPrivateData>(File.ReadAllText(privateDataPath)) ?? new JPrivateData();

                // set default user agent string
                if (String.IsNullOrEmpty(_privateData.UserAgent) && !String.IsNullOrEmpty(_privateData.OperatorEmail))
                    // note that Wikidata expects the user agent string to be "program-name/0.0 (user@emailhost)"
                    _privateData.UserAgent = $"{userAgentBaseString} ({_privateData.OperatorEmail})";

                // read the GNIS class attributes
                GnisClassData gnisClassData = new(gnisClassDataPath);

                // configure the query builder
                OverpassQueryBuilder overpassQueryBuilder = new(gnisClassData, overpassUrl);

                // configure the gnis matcher
                _gnisMatcher = new(gnisClassData);

                // configure the gnis validator
                _gnisValidator = new(gnisClassData);

                // read the errata file
                JErrata errataObject = JsonSerializer.Deserialize<JErrata>(File.ReadAllText(errataPath)) ?? new JErrata();
                Dictionary<string, Erratum> errata = new();
                foreach (Erratum erratum in errataObject.Errata)
                    errata.Add(erratum.Id, erratum);

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
                Parallel.ForEach(gnisFileReader, new ParallelOptions { MaxDegreeOfParallelism = 8 }, ( fileRecord, parallelLoopState, iteration ) =>
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

                    bool censusDivision = (gnisRecord.FeatureClass.Equals("Census") || gnisRecord.FeatureClass.Equals("Civil")) && gnisRecord.FeatureName.EndsWith("Division");

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
                            // build the proximity query
                            overpassQuery = overpassQueryBuilder.BuildProximityQuery(gnisRecord);
                        else
                            // build a query for the specific OSM feature
                            overpassQuery = OverpassQueryBuilder.BuildObjectQuery(erratum.Use.Type, erratum.Use.Ref);

                        Verbose.WriteLine(overpassQuery);

                        osmData = overpassQueryBuilder.SendQuery(overpassQuery);

                        proximityQueryTimer.Stop(iteration);
                        proximityMatchTimer.Start(iteration);

                        matchResults = _gnisMatcher.GetMatchResults(gnisRecord, osmData);

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

                        osmData = XOsmData.Merge(osmData,overpassQueryBuilder.SendQuery(overpassQuery));

                        secondQueryTimer.Stop(iteration);
                        secondMatchTimer.Start(iteration);

                        List<GnisMatchResult> secondMatchResults = _gnisMatcher.GetMatchResults(gnisRecord, osmData);

                        // remove duplicate matches
                        foreach (GnisMatchResult firstResult in matchResults)
                        {
                            for (int i = 0; i < secondMatchResults.Count; i++)
                            {
                                if (firstResult.osmFeature.Id == secondMatchResults[i].osmFeature.Id)
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
                        if (matchResult.MatchType == GnisMatchType.exactMatch)
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
                            if (matchResults[i].MatchType != GnisMatchType.exactMatch)
                            {
                                matchResults.Remove(matchResults[i]);
                                i--;
                            }
                        }
                    }

                    List<GnisValidationResult> validationResults = new();

                    // validate each match and output the TSV data
                    foreach (GnisMatchResult matchResult in matchResults)
                    {
                        GnisValidationResult validationResult = _gnisValidator.ValidateOsmFeature(gnisRecord, matchResult);
                        validationResults.Add(validationResult);

                        // write output with match details
                        if (!_skipMatches)
                            tsvFileWriter.WriteOutputRecord(gnisRecord, overpassQuery, matchResult, validationResult);
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
                    else if (matchResults.Count == 1 && !_skipMatches)
                    {
                        // write MapRoulette task with match details
                        mapRouletteChallengeWriter.WriteTask(gnisRecord, matchResults[0], validationResults[0]);

                        // add to OsmChange with match details
                        osmChangeWriter.AddToOsmChange(gnisRecord, matchResults[0], validationResults[0]);
                    }
                    else if (!_skipMatches)
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
                            _gnisMatcher.ConsolidateMatches(gnisRecord, matchResults, validationResults, newMatchResult, newValidationResult);

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
            catch (Exception e)
            {
                Console.Error.WriteLine(e.ToString());
            }
        }
    }
}
