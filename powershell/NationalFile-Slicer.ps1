<#

.SYNOPSIS
Given the path to a GNIS data file, slice the file to output records that match the parameters.

.DESCRIPTION
NationalFile-Slicer reads a GNIS text file delimited by pipe characters (|) and matches each line against the input parameters. Each line must match one of the values for each of the parameters.

.EXAMPLE
.\GnisTo-GeoJson.ps1 NationalFile_20210825.txt

.INPUTS
The path to a GNIS data file.

.OUTPUTS
A GeoJson file containing an item for each record in the original GNIS data file.

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

# open a stream reader to read the GNIS file line by line
[System.IO.StreamReader]$gnisDataStreamReader = [System.IO.File]::OpenText($resolvedPath)

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

# output the file header
Write-Output($header)

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

    $match = $True

    foreach ($id in $Ids)
    {
        $match = $False
        if ($id -eq $fields[$FEATURE_ID])
        {
            $match = $True
            break
        }
    }

    if (-not $match) { continue; }

    foreach ($name in $Names)
    {
        $match = $False
        if ($fields[$FEATURE_NAME] -match $name)
        {
            $match = $True
            break
        }
    }

    if (-not $match) { continue; }

    foreach ($class in $Classes)
    {
        $match = $False
        if ($class -eq $fields[$FEATURE_CLASS])
        {
            $match = $True
            break
        }
    }

    if (-not $match) { continue; }

    foreach ($state in $States)
    {
        $match = $False
        if ($state -eq $fields[$FEATURE_STATE])
        {
            $match = $True
            break
        }
    }

    if (-not $match) { continue; }

    foreach ($county in $Counties)
    {
        $match = $False
        if ($county -eq $fields[$FEATURE_COUNTY])
        {
            $match = $True
            break
        }
    }

    if (-not $match) { continue }

    foreach ($map in $Maps)
    {
        $match = $False
        if ($map -eq $fields[$FEATURE_MAP])
        {
            $match = $True
            break
        }
    }

    if (-not $match) { continue }

    if ($South -ne 0 -and $North -ne 0)
    {
        $latInBounds = $false

        $primaryLat = [double]::NaN
        $parsed = [double]::TryParse($fields[$PRIM_LAT_DEC],[ref]$primaryLat)

        if ($parsed -and $primaryLat -gt $South -and $primaryLat -lt $North) { $latInBounds = $True }

        $sourceLat = [double]::NaN
        $parsed = [double]::TryParse($fields[$SOURCE_LAT_DEC],[ref]$sourceLat)

        if ($parsed -and $sourceLat -gt $South -and $sourceLat -lt $North) { $latInBounds = $True }

        if (!$latInBounds) { continue }
    }

    if ($West -ne 0 -and $East -ne 0)
    {
        $lonInBounds = $False

        $primaryLon = [double]::NaN
        $parsed = [double]::TryParse($fields[$PRIM_LONG_DEC],[ref]$primaryLon)

        if ($parsed -and $primaryLon -gt $West -and $primaryLon -lt $East) { $lonInBounds = $True }

        $sourceLon = [double]::NaN
        $parsed = [double]::TryParse($fields[$SOURCE_LONG_DEC],[ref]$sourceLon)

        if ($parsed -and $sourceLon -gt $West -and $sourceLon -lt $East) { $lonInBounds = $True }

        if (!$lonInBounds) { continue }
    }

	# output the GeoJson item
	Write-Output($record)
}

# close the input data stream
$gnisDataStreamReader.Close()
