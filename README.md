# recoGNISer

Automated matching between [GNIS](https://www.usgs.gov/us-board-on-geographic-names/download-gnis-data) records and [OpenStreetMap](https://osm.org/) features.

## Building recoGNISer

The recoGNISer project is built using Visual Studio 2022 with .NetCore and the GeoCoordinate package. It can be built for and run on any system that supports .Net 7.0.

## Using recoGNISer

The recoGNISer is a command-line executable. It takes a portion of a GNIS text data file as input and optionally produces several types of outputs.

**NOTE:** You must configure the private_data.json file before using the recoGNISer. See below for details.

**CAUTION:** The recoGNISer uses Overpass queries to find features in OSM and can generate large volumes of Overpass traffic very quickly. **Be kind!** Don't overload public Overpass servers. [Consider running a local Overpass server instance](https://www.openstreetmap.org/user/Kai%20Johnson/diary/401263) to support the recoGNISer.

### Usage
```
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
  --help, -h, /?            display this help and exit";
```

### Options

*gnisFile* - This is a portion of either the archived GNIS text data file or current GNIS text data file for Domestic Names. Typically, you'll want this file to contain a few hundred records. The format of the archived and current GNIS text files differs slightly, and recoGNISer will accept both formats. The first line of the file MUST contain the column headings as they appear in the source GNIS data files as recoGNISer uses this to idenitify the file format.

It is not practical to give recoGNISer the entire GNIS Domestic Names text file as input. Processing the entire file would take days and the output would be impractically large. Instead, you'll want to process a small portion of the source data file. The NationalFile-Slicer.ps1 script in the `powershell` directory will extract records from the source Domestic Names files using filters to match data fields, or selecting features based on a bounding box.

*outputFile* - This file containing tab-separated values is a dump of the processing results as recoGNISer matches GNIS records against OSM features. This is primarily of interest for debugging but can also be used to manually verify results.

*mapRouletteFile* - Produces a GeoJson file suitable as source data for a MapRoulette challenge.

The recoGNISer includes fields in each GeoJson feature that can be used to dynamically create instructions within MapRoulette. To use these fields, set the detailed instructions for mappers to something like this:

```
Add or update [GNIS Feature {{gnis:feature_id}} - {{gnis:feature_name}} ({{gnis:feature_class}})](https://edits.nationalmap.gov/apps/gaz-domestic/public/summary/{{gnis:feature_id}})

{{instructions}}

Please use USGS Topo maps, aerial imagery, and the [GNIS Record](https://edits.nationalmap.gov/apps/gaz-domestic/public/summary/{{gnis:feature_id}}) to add or update the feature.

How did we do?

Matching: [select " " name="Matching" values="Found the right item,Found the wrong item,Didn't find the right item,Some right items and some wrong items,Some right items but missed other items"]

Instructions: [select " " name="Instructions" values="Instructions were clear and helpful,Instructions were hard to understand,Instructions were wrong"]

Editing: [select " " name="Editing" values="Editing in JOSM worked fine,Editing in iD worked fine,Problems editing in JOSM,Problems editing in iD"]

Automated Updates: [select " " name="Automated Updates" values="Automatic updates in JOSM were helpful,Automatic updates in JOSM were wrong,Automatic updates in JOSM didn't work"]

GNIS Data: [select " " name="GNIS Data" values="GNIS data was good,GNIS data was wrong,GNIS data was wrong but the GNIS web site is correct"]

If there's something that can be better, leave us a comment!
```

You might use the `#gnis` and `#recogniser` tags in the changeset description in MapRoulette and set the changeset source to `recogniser-bot/0.1`.

Sometimes there are interesting interactions between MapRoulette and JOSM for collaborative tasks. When JOSM loads a MapRoulette task, it likes to pull the history for every relation, way, and node. That can take a while and is typically not necessary. You can cancel that operation and simply ask JOSM to update the current data set to make sure you have all the current information from OSM. Also, JOSM may make API requests for unacceptably large areas while loading a MapRoulette task and then fail to load the data. You can typically avoid this by setting the Minimum Zoom Level in MapRoulette to 13.

*osmChangeFile* - Produces an OsmChange XML file containing all the changes suggested by recoGNISer. This can be useful for editing features in bulk. However, **you must either get approval for an [automated edit](https://wiki.openstreetmap.org/wiki/Automated_edits) or manually review each change** before uploading these changes to OSM.

If you're manually reviewing the changes, this workflow seems to work well:

1. Load the .osc file in JOSM and load any relevant background layers (USGS Topo and 3DEP Contours are often helpful)
2. Select all the elements with `gnis:feature_id` tags and add them to the task list using the Todo plugin
3. Select and zoom to the first item on the task list
4. Download nearyby data from OSM
5. Verify the changes to the selected element and make corrections as needed
6. Mark the item in the task list as done and repeat from Step 3.
7. When all the elements in the task list have been reviewed, upload the changeset.

If you decide to work with OsmChange XML files, remember to keep the bounding boxes and number of elements in your changesets relatively small.

*privateDataFile* - This data file contains your e-mail address and Wikidata API key. The recoGNISer uses Wikidata as a secondary data source to match OSM features to GNIS records, so you MUST configure this data file before using the recoGNISer. To configure the data file, copy the `private_data_example.json` file and rename it to `private_data.json`. [Register for REST API access to Wikidata and obtain a bearer token](https://www.wikidata.org/wiki/Wikidata:REST_API/Authentication). Edit the `private_data.json` file to put your e-mail address and Wikidata API bearer token in the file.

*gnisClassDataFile* - This file contains reference data that the recoGNISer uses to match GNIS records to OSM features and suggest changes to OSM features. The default path uses the file included in this repository.

*errata* - Sometimes GNIS records are wrong. If you're reprocessing a GNIS data set and want to ignore certain records or supply alternate data to the recoGNISer, you can edit the JSON data in this file to specify corrections to the GNIS source data. Here's an example of an errata.json file:

```
{"errata" : [
	{
		"id" : "375819",
		"substitute" : "375819|West Fork Newe Waippe Naokwaide|Stream|ID|16|Owyhee|073|431856N|1165303W|43.3181607|-116.8856478|431625N|1165305W|43.2682678|-116.8912453|1414|4639|Piute Butte|06/21/1979|",
		"use" : 
		{
			"type" : "way",
			"ref" : 1097370221
		},
		"reason" : "Correction to name and coordinates; direct OSM object query"
	},
	{
		"id" : "1664789",
		"skip" : true,
		"reason" : "USGS confirmed that this feature does not exist"
	}
]}
```

Note that the substitute text MUST be in the same format as the source GNIS data file and this format varies between the archived and current data sets.

*overpassUrl* - By default, the recoGNISer looks for the Overpass server on localhost. Running the recoGNISer on the same host as the Overpass server is not ideal because they will be competing for CPU resources. Specify this URL to point to a separate local or public Overpass server.

*performance* - Outputs a summary of the time spent on individual tasks within the recoGNISer.

*progress* - Outputs one line for each GNIS record that is processed so that you can track progress.

*verbose* - Outputs a lot of data from internal processing within the recoGNISer. This output can be useful for debugging if the source file only has one data record.

*archived* - In August of 2021, [USGS removed records for many man-made features from the current GNIS data and retained them in the archived data set](https://wiki.openstreetmap.org/wiki/USGS_GNIS#The_Archived_Data_Set). By default, the recoGNISer will skip records in the archived feature classes. Specifying this option allows the recoGNISer to process these records.

**Use caution.** The quality of data in the archived GNIS records varies greatly. In many cases, the GNIS records in the archived data set are wrong or out of date. 

*alwaysMatchGeometry* - The recoGNISer generally aims for more false negative matches than false positives and in some cases this means ignoring possible matches where features appear to be roughly in the right place. Specifying this option tells the recoGNISer to always consider geometry matches even if this produces additional false positive results.

*skipMatches* - The recoGNISer will normally suggest both updates to existing features and adding missing features. With this option, the recoGNISer will only suggest adding missing features. This is useful for quickly adding missing features in bulk, rather than spending time editing existing features.

### Example

```
.\powershell\NationalFile-Slicer.ps1 -States California -Counties Mono -gnisFile ~\Documents\GNIS\DomesticNames_National.txt > ~\Documents\GNIS\California_Mono.txt

.\recogniser\bin\Release\net7.0\recogniser.exe --overpassUrl 'http://192.168.0.13/api/interpreter' --gnisFile ~\Documents\GNIS\California_Mono.txt --mapRouletteFile ~\Documents\GNIS\California_Mono.geojson --progress --performance
```
