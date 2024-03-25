<#

.SYNOPSIS
Given the path to a GNIS data file, convert the file to GeoJson format with an item for each record in the file.

.DESCRIPTION
GnisTo-GeoJson reads a GNIS text file delimited by pipe characters (|), converts each record to GeoJson item and outputs all the items as a Overpass file containing GeoJson items.

.EXAMPLE
.\GnisTo-GeoJson.ps1 NationalFile_20210825.txt

.INPUTS
The path to a GNIS data file.

.OUTPUTS
A GeoJson file containing an item for each record in the original GNIS data file.

#>

Param(
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
$ELEV_IN_M = [array]::IndexOf($titles, "ELEV_IN_M")
$SOURCE_LAT_DEC = [array]::IndexOf($titles, "SOURCE_LAT_DEC")
$SOURCE_LONG_DEC = [array]::IndexOf($titles, "SOURCE_LONG_DEC")
$PRIM_LAT_DEC = [array]::IndexOf($titles, "PRIM_LAT_DEC")
$PRIM_LONG_DEC = [array]::IndexOf($titles, "PRIM_LONG_DEC")

# output the Overpass file header
Write-Output('{"version": 0.6,"generator": "GnisTo-GeoJson","elements": [')

# counter to generate unique ids in the output
$idCounter = -1;

# flag to indicate if this is the first line of data (second line in the file)
$firstRecord = $true

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

	# create a new object for the GeoJson item
	$geoJsonItem = New-Object -TypeName psobject

	# populate members of the GeoJson item
	$geoJsonItem | Add-Member -MemberType NoteProperty -Name 'type' -Value 'item'
	$geoJsonItem | Add-Member -MemberType NoteProperty -Name 'id' -Value ($idCounter--)
	$geoJsonItem | Add-Member -MemberType NoteProperty -Name 'geometry' -Value (New-Object -TypeName psobject)
	$geoJsonItem | Add-Member -MemberType NoteProperty -Name 'tags' -Value (New-Object -TypeName psobject)
	
	# add tags to the GeoJson item

	$geoJsonItem.tags | Add-Member -MemberType NoteProperty -Name 'gnis:feature_id' -Value $fields[$FEATURE_ID]
	$geoJsonItem.tags | Add-Member -MemberType NoteProperty -Name 'gnis:feature_name' -Value $fields[$FEATURE_NAME]
	$geoJsonItem.tags | Add-Member -MemberType NoteProperty -Name 'gnis:feature_class' -Value $fields[$FEATURE_CLASS]
	$geoJsonItem.tags | Add-Member -MemberType NoteProperty -Name 'gnis:elev_in_m' -Value $fields[$ELEV_IN_M]

	
	# if the source coordinates are empty
	if (("" -eq $fields[$SOURCE_LAT_DEC]) -or ("" -eq $fields[$SOURCE_LONG_DEC])) {
		# populate geometry with Point data
		$geoJsonItem.geometry | Add-Member -MemberType NoteProperty -Name 'type' -Value 'Point'

		$geoJsonItem.geometry | Add-Member -MemberType NoteProperty -Name 'coordinates' -Value @(($fields[$PRIM_LONG_DEC] -as [double]), ($fields[$PRIM_LAT_DEC] -as [double]))

		
		# tag this as an OSM node
		$geoJsonItem.tags | Add-Member -MemberType NoteProperty -Name '_osm_type' -Value 'node'
	}
	else {
		# populate geometry with LineString data
		$geoJsonItem.geometry | Add-Member -MemberType NoteProperty -Name 'type' -Value 'LineString'
		$geoJsonItem.geometry | Add-Member -MemberType NoteProperty -Name 'coordinates' -Value @((($fields[$SOURCE_LONG_DEC] -as [double]), ($fields[$SOURCE_LAT_DEC] -as [double])), (($fields[$PRIM_LONG_DEC] -as [double]), ($fields[$PRIM_LAT_DEC] -as [double])) )


		# tag this as an OSM way
		$geoJsonItem.tags | Add-Member -MemberType NoteProperty -Name '_osm_type' -Value 'way'
	}

	# output a separating comma if this is not the first record
	if ($firstRecord) {
		$firstRecord = $false
	}
	else {
		Write-Output(",")
	}

	# output the GeoJson item
	Write-Output((ConvertTo-Json -Depth 3 -Compress $geoJsonItem))
}

# close the input data stream
$gnisDataStreamReader.Close()

# write the Overpass footer
Write-Output("]}")
