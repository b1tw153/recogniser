using System.Text.Json;

namespace recogniser
{
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
        private static readonly int threadsDefault = 8;

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
        static void Main(string[] args)
        {
            Dictionary<string, string> parsedArgs = new();

            if (!TryParseArgs(args, parsedArgs))
                return;

            try
            {
                PerformanceTimer initializationTimer = new("Initialization");
                initializationTimer.Start(0);

                // initialize output writers that may go to the Console
                if (parsedArgs.ContainsKey("--performance"))
                    _performance = Console.Out;
                if (parsedArgs.ContainsKey("--progress"))
                    _progress = Console.Out;
                if (parsedArgs.ContainsKey("--verbose"))
                    _verbose = Console.Out;

                // set the flag to always match geometry
                if (parsedArgs.ContainsKey("--alwaysMatchGeometry"))
                    _alwaysMatchGeometry = true;

                // set the flag to skip matched GNIS records
                if (parsedArgs.ContainsKey("--skipMatches"))
                    _skipMatches = true;

                // path to the directory containing executable file
                string exeDirPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ".";

                // path to the private data file
                string privateDataPath = parsedArgs.TryGetValue("--privateData", out string? argValue) ? argValue : Path.Combine(exeDirPath, privateDataPathDefault);

                // read the private data file
                _privateData = JsonSerializer.Deserialize<JPrivateData>(File.ReadAllText(privateDataPath)) ?? new JPrivateData();

                // set default user agent string
                if (String.IsNullOrEmpty(_privateData.UserAgent) && !String.IsNullOrEmpty(_privateData.OperatorEmail))
                    // note that Wikidata expects the user agent string to be "program-name/0.0 (user@emailhost)"
                    _privateData.UserAgent = $"{userAgentBaseString} ({_privateData.OperatorEmail})";
 
                // path to the GNIS glass attributes
                string gnisClassDataPath = parsedArgs.TryGetValue("--gnisClassData", out argValue) ? argValue : Path.Combine(exeDirPath, gnisClassDataPathDefault);

                // read the GNIS class attributes
                GnisClassData gnisClassData = new(gnisClassDataPath);

                // URL for the Overpass API endpoint
                string overpassUrl = parsedArgs.TryGetValue("--overpassUrl", out argValue) ? argValue : overpassUrlDefault;

                // configure the query builder
                OverpassQueryBuilder overpassQueryBuilder = new(gnisClassData, overpassUrl);

                // configure the gnis matcher
                _gnisMatcher = new(gnisClassData);

                // configure the gnis validator
                _gnisValidator = new(gnisClassData);

                // path to the errata file
                string errataPath = parsedArgs.TryGetValue("--errata", out argValue) ? argValue : Path.Combine(exeDirPath, errataPathDefault);

                // read the errata file
                JErrata errataObject = JsonSerializer.Deserialize<JErrata>(File.ReadAllText(errataPath)) ?? new JErrata();
                Dictionary<string, Erratum> errata = new();
                foreach (Erratum erratum in errataObject.Errata)
                    errata.Add(erratum.Id, erratum);

                // type of MapRoulette output to generate
                string mapRouletteOutputType = parsedArgs.TryGetValue("--mapRouletteType", out argValue) ? argValue : mapRouletteOutputTypeDefault;

                // path to the file containing GNIS records (required)
                if (!parsedArgs.TryGetValue("--gnisFile", out string? gnisFilePath))
                {
                    PrintHelp();
                    return;
                }

                // path to the output table file (optional)
                parsedArgs.TryGetValue("--outputFile", out string? tsvFilePath);

                // path to the MapRoulette output file (optional)
                parsedArgs.TryGetValue("--mapRouletteFile", out string? mapRouletteOutputPath);

                // path to the OsmChange output file (optional)
                parsedArgs.TryGetValue("--osmChangeFile", out string? osmChangeOutputPath);

                // type of MapRoulette output to generate
                int threads = parsedArgs.TryGetValue("--threads", out argValue) ? int.Parse(argValue) : threadsDefault;

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
                Parallel.ForEach(gnisFileReader, new ParallelOptions { MaxDegreeOfParallelism = threads }, ( fileRecord, parallelLoopState, iteration ) =>
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
                    if ((!gnisClassAttributes.Current && !parsedArgs.ContainsKey("--archived")) || erratum.Skip || gnisRecord.HasZeroPrimary() || censusDivision)
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
                            overpassQuery = overpassQueryBuilder.BuildObjectQuery(erratum.Use.Type, erratum.Use.Ref);

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
                        GnisMatchResult? bestResult = _gnisMatcher.FindBestMatch(matchResults);
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

        private static bool TryParseArgs(string[] args, Dictionary<string, string> parsedArgs)
        {
            string[] possibleOptions = { "gnisFile", "outputFile", "mapRouletteFile", "mapRouletteType", "osmChangeFile", "privateData", "gnisClassData", "errata", "overpassUrl", "threads" };
            string[] possibleSwitches = { "performance", "progress", "verbose", "archived", "skipMatches", "alwaysMatchGeometry" };

            try
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if ("--help".Equals(args[i]) || "/?".Equals(args[i]) || "-h".Equals(args[i]))
                    {
                        PrintHelp();
                        return false;
                    }

                    foreach (var poss in possibleOptions)
                    {
                        if ($"--{poss}".Equals(args[i]))
                        {
                            parsedArgs.Add(args[i], args[i+1]);
                            i++;
                        }
                    }

                    foreach (var poss in possibleSwitches)
                    {
                        if ($"--{poss}".Equals(args[i]))
                        {
                            parsedArgs.Add(args[i], "true");
                        }
                    }
                }
            }
            catch (Exception)
            {
                PrintHelp();
                return false;
            }

            return true;
        }

        private static readonly string usage =
@"Usage: recogniser [OPTION]...
Search for GNIS features in OSM and output MapRoulette tasks or OSC XML to update them.

  --gnisFile FILE           file containing GNIS records with pipe-separated fields (REQUIRED)
  --outputFile FILE         output TSV file containing search results
  --mapRouletteFile FILE    output GeoJson file containing MapRoulette challenge data
  --mapRouletteType TYPE    type of MapRoulette tasks to create, values are:
        collaborative       collaborative tasks containing OsmChange XML (DEFAULT)
        tagfix              collaborative tasks containing Tag Fix data
        plain               ordinary MapRoulette tasks without automated changes
  --osmChangeFile FILE      output a single OsmChange XML file containing all changes
  --privateData FILE        JSON file containing private configuration data (DEFAULT: conf/private_data.json)
  --gnisClassData FILE      CSV file containing GNIS class data (DEFAULT: conf/gnis_class_data.csv)
  --errata FILE             JSON file containing errata records (DEFAULT: conf/errata.json)
  --overpassUrl URL         URL for the Overpass interpreter (DEFAULT: http://127.0.0.1/api/interpreter)
  --performance             write a performance summary to stdout at the end of the run
  --progress                write periodic progress updates to stdout
  --verbose                 write huge amounts of progress data to stdout
  --archived                process archived GNIS classes (excluded by default)
  --alwaysMatchGeometry     process geometry matches for every OSM feature (may produce false positives)
  --skipMatches             output results only for GNIS records that did not match OSM features
  --threads N               number of parallel threads to use for processing (default is 8)
  --help, -h, /?            display this help and exit";

        private static void PrintHelp()
        {
            Console.WriteLine(usage);
        }
    }
}
