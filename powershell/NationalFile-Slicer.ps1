<#

.SYNOPSIS
Given the path to a GNIS data file, slice the file to output records that match the parameters.

.DESCRIPTION
NationalFile-Slicer reads a GNIS text file delimited by pipe characters (|) and matches each line against the input parameters. Each line must match one of the values for each of the parameters.

.EXAMPLE
.\NationalFile-Slicer.ps1 -States 'WA','OR','CA' DomesticNames_National.txt > PacificCoast.txt

Extract all domestic names from the Pacific Coast states.

.EXAMPLE
.\NationalFile-Slicer.ps1 -Classes 'Cliff','Pillar','Ridge','Range','Summit' NationalFile_20210825.txt > Mountain_Features.txt

Extract all mountain-related features from the archived national file.

.INPUTS
The path to a GNIS data file.

.OUTPUTS
A file containing matching records from the original GNIS data file.

#>

Param(
	# List of Feature IDs
	[Parameter()]
	[string[]]
	$Ids,

	# List of Feature Names
	[Parameter()]
	[string[]]
	$Names,

	# List of Feature Classes
	[Parameter()]
	[string[]]
	$Classes,

	# List of State Names
	[Parameter()]
	[string[]]
	$States,

	# List of County Names
	[Parameter()]
	[string[]]
	$Counties,

	# List of Map Names
	[Parameter()]
	[string[]]
	$Maps,

	# South side of a bounding box
	[Parameter()]
	[double]
	$South,

	# West side of a bounding box
	[Parameter()]
	[double]
	$West,

	# North side of a bounding box
	[Parameter()]
	[double]
	$North,

	# East side of a bounding box
	[Parameter()]
	[double]
	$East,

	# Path to the GNIS data file.
	[Parameter(Position = 1, Mandatory)]
	[System.IO.DirectoryInfo]
	$gnisFile
)

# fix the path to the input file to correct for funky file system naming
$resolvedPath = Resolve-Path $gnisFile
$splitPath = $resolvedPath.ToString().Split("::")
if ($splitPath.Length -gt 2) {
	$resolvedPath = $splitPath[2]
}

# open a stream reader to read the GNIS file line by line with larger buffer (65KB)
[System.IO.StreamReader]$gnisDataStreamReader = New-Object System.IO.StreamReader($resolvedPath, [System.Text.Encoding]::UTF8, $true, 65536)

# exit if we couldn't open the file
if ($null -eq $gnisDataStreamReader) { return }

# read the first (header) line from the file
$header = $gnisDataStreamReader.ReadLine()

# split the header into column titles
$titles = $header.Split("|")

# exit if the file does not have a pipe separated list of titles in the first line
if ($titles.Count -lt 2) {
	Write-Error "This is not a GNIS data file."
	return
}

# get the indexes of the fields that we need
$FEATURE_ID = [array]::IndexOf($titles, "FEATURE_ID")
$FEATURE_NAME = [array]::IndexOf($titles, "FEATURE_NAME")
$FEATURE_CLASS = [array]::IndexOf($titles, "FEATURE_CLASS")
$FEATURE_STATE = [array]::IndexOf($titles, "STATE_ALPHA")
$FEATURE_COUNTY = [array]::IndexOf($titles, "COUNTY_NAME")
$FEATURE_MAP = [array]::IndexOf($titles, "MAP_NAME")
$SOURCE_LAT_DEC = [array]::IndexOf($titles, "SOURCE_LAT_DEC")
$SOURCE_LONG_DEC = [array]::IndexOf($titles, "SOURCE_LONG_DEC")
$PRIM_LAT_DEC = [array]::IndexOf($titles, "PRIM_LAT_DEC")
$PRIM_LONG_DEC = [array]::IndexOf($titles, "PRIM_LONG_DEC")

# if this might be a new domestic names file
if ($FEATURE_ID -eq -1) {
    $FEATURE_ID = [array]::IndexOf($titles, "feature_id")
    $FEATURE_NAME = [array]::IndexOf($titles, "feature_name")
    $FEATURE_CLASS = [array]::IndexOf($titles, "feature_class")
    $FEATURE_STATE = [array]::IndexOf($titles, "state_name")
    $FEATURE_COUNTY = [array]::IndexOf($titles, "county_name")
    $FEATURE_MAP = [array]::IndexOf($titles, "map_name")
    $SOURCE_LAT_DEC = [array]::IndexOf($titles, "source_lat_dec")
    $SOURCE_LONG_DEC = [array]::IndexOf($titles, "source_long_dec")
    $PRIM_LAT_DEC = [array]::IndexOf($titles, "prim_lat_dec")
    $PRIM_LONG_DEC = [array]::IndexOf($titles, "prim_long_dec")
}

# Convert filter arrays to hashtables for O(1) lookup instead of O(n)
$IdHash = @{}
$ClassHash = @{}
$StateHash = @{}
$CountyHash = @{}
$MapHash = @{}

foreach ($id in $Ids) { $IdHash[$id] = $true }
foreach ($class in $Classes) { $ClassHash[$class] = $true }
foreach ($state in $States) { $StateHash[$state] = $true }
foreach ($county in $Counties) { $CountyHash[$county] = $true }
foreach ($map in $Maps) { $MapHash[$map] = $true }

# Pre-compile Name patterns if provided
$NamePatterns = @()
if ($Names.Count -gt 0) {
    $NamePatterns = $Names | ForEach-Object { [regex]::new($_) }
}

# output the file header
Write-Output $header

# Batch records for more efficient pipeline output
$batch = @()
$batchSize = 5000

# while there are more lines in the file
while (-not $gnisDataStreamReader.EndOfStream) {

	# read the next record
	$record = $gnisDataStreamReader.ReadLine()

	# split the record into fields
	$fields = $record.Split("|")

	# skip bad records
	if (($null -eq $fields[$FEATURE_ID]) -or ("" -eq $fields[$FEATURE_ID])) {
		continue
	}

    # Check ID filter (skip if Ids provided and this doesn't match)
    if ($Ids.Count -gt 0 -and -not $IdHash.ContainsKey($fields[$FEATURE_ID])) { continue }

    # Check Name filter (skip if Names provided and no pattern matches)
    if ($NamePatterns.Count -gt 0) {
        $nameMatched = $false
        foreach ($pattern in $NamePatterns) {
            if ($pattern.IsMatch($fields[$FEATURE_NAME])) {
                $nameMatched = $true
                break
            }
        }
        if (-not $nameMatched) { continue }
    }

    # Check Class filter
    if ($Classes.Count -gt 0 -and -not $ClassHash.ContainsKey($fields[$FEATURE_CLASS])) { continue }

    # Check State filter
    if ($States.Count -gt 0 -and -not $StateHash.ContainsKey($fields[$FEATURE_STATE])) { continue }

    # Check County filter
    if ($Counties.Count -gt 0 -and -not $CountyHash.ContainsKey($fields[$FEATURE_COUNTY])) { continue }

    # Check Map filter
    if ($Maps.Count -gt 0 -and -not $MapHash.ContainsKey($fields[$FEATURE_MAP])) { continue }

    # Check bounding box filters
    if ($South -ne 0 -and $North -ne 0) {
        $latInBounds = $false

        $primaryLat = [double]::NaN
        if ([double]::TryParse($fields[$PRIM_LAT_DEC], [ref]$primaryLat) -and $primaryLat -gt $South -and $primaryLat -lt $North) {
            $latInBounds = $true
        }

        if (-not $latInBounds) {
            $sourceLat = [double]::NaN
            if ([double]::TryParse($fields[$SOURCE_LAT_DEC], [ref]$sourceLat) -and $sourceLat -gt $South -and $sourceLat -lt $North) {
                $latInBounds = $true
            }
        }

        if (-not $latInBounds) { continue }
    }

    if ($West -ne 0 -and $East -ne 0) {
        $lonInBounds = $false

        $primaryLon = [double]::NaN
        if ([double]::TryParse($fields[$PRIM_LONG_DEC], [ref]$primaryLon) -and $primaryLon -gt $West -and $primaryLon -lt $East) {
            $lonInBounds = $true
        }

        if (-not $lonInBounds) {
            $sourceLon = [double]::NaN
            if ([double]::TryParse($fields[$SOURCE_LONG_DEC], [ref]$sourceLon) -and $sourceLon -gt $West -and $sourceLon -lt $East) {
                $lonInBounds = $true
            }
        }

        if (-not $lonInBounds) { continue }
    }

	# add record to batch
	$batch += $record

	# output batch when it reaches batch size
	if ($batch.Count -ge $batchSize) {
		Write-Output $batch
		$batch = @()
	}
}

# output any remaining records in batch
if ($batch.Count -gt 0) {
	Write-Output $batch
}

# close the input data stream
$gnisDataStreamReader.Close()
$gnisDataStreamReader.Dispose()
