using GeoCoordinatePortable;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace recogniser
{
    public class OverpassQueryBuilder
    {
        private readonly GnisClassData gnisClassData;
        private readonly XmlSerializer overpassSerializer = new(typeof(XOsmData));
        private readonly string overpassUrl;

        public OverpassQueryBuilder(GnisClassData gnisClassData, string overpassUrl)
        {
            this.gnisClassData = gnisClassData;
            this.overpassUrl = overpassUrl;
        }

        public string BuildProximityQuery(GnisRecord gnisRecord)
        {
            // builder for the query string
            StringBuilder query = new();

            // get the attributes for the feature class of the GNIS record
            GnisClassAttributes gnisClassAttributes = gnisClassData.GetGnisClassAttributes(gnisRecord.FeatureClass);

            // make a two kilometer box with the feature at the center (i.e. 1 km in each direction)
            double[] twoKilometerBox = MakeBoundingBox(gnisRecord.Primary.Latitude, gnisRecord.Primary.Longitude, 2000);

            // use the two kilometer box as the area filter for the query statements
            string areaFilter = string.Join(",", twoKilometerBox);

            // if this feature class can be mapped as a node in OSM
            if (gnisClassAttributes.HasGeometry("point"))
            {
                // find nodes within the box (but only nodes with tags)
                query.Append($"node({areaFilter})(if: count_tags() > 0) -> .points; ");

                // if there are conflicting tags that this feature can't have
                if (!string.IsNullOrEmpty(gnisClassAttributes.ConflictingTags))
                {
                    StringBuilder conflictingTagBuilder = new();

                    OsmTagProto[] conflictingTags = gnisClassAttributes.GetConflictingTags();

                    // filter out nodes that have conflicting tags
                    foreach (OsmTagProto conflictingTag in conflictingTags)
                    {
                        if ("*".Equals(conflictingTag.Value))
                            conflictingTagBuilder.Append($"(.points; - node[\"{conflictingTag.Name}\"].points;) -> .points; ");
                        else
                            conflictingTagBuilder.Append($"(.points; - node[\"{conflictingTag.Name}\"=\"{conflictingTag.Value}\"].points;) -> .points; ");
                    }

                    query.Append(conflictingTagBuilder);
                }
            }

            // if this feature class can be mapped as a way or relation in OSM
            if (gnisClassAttributes.HasGeometry("line") || gnisClassAttributes.HasGeometry("area"))
            {
                // find ways within the box
                query.Append($"way({areaFilter}) -> .lines; ");

                // if we want to include relations in the results
                if (!string.IsNullOrEmpty(gnisClassAttributes.RelationTypes))
                {
                    // recurse up to relations that have the ways as members
                    query.Append("(.lines; .lines <;) -> .lines; ");
                }

                // if the feature can be mapped using any type (or an unspecified type) of relation
                if ("*".Equals(gnisClassAttributes.RelationTypes))
                {
                    // drop big admin boundaries
                    query.Append($"(.lines; - rel(if: abs(t[\"admin_level\"]) < 6).lines;) -> .lines; ");
                }
                // if the feature can be mapped using more than one type of relation
                else if (gnisClassAttributes.RelationTypes.Contains('|'))
                {
                    // drop relations that don't have the required tags
                    query.Append($"(.lines; - rel[type!~\"{gnisClassAttributes.RelationTypes}\"].lines;) -> .lines; ");
                }
                // if the feature can be mapped as only one type of relation
                // i.e. RelationTypes is not wildcard or empty string and isn't pipe delimited
                else if (!string.IsNullOrEmpty(gnisClassAttributes.RelationTypes))
                {
                    // drop relations that don't have the exact tag
                    query.Append($"(.lines; - rel[type!=\"{gnisClassAttributes.RelationTypes}\"].lines;) -> .lines; ");
                }

                // filter out big boundaries if they might be in the results
                if (gnisClassAttributes.RelationTypes.Contains("boundary"))
                {
                    // filter out irrelevant boundaries and boundaries at county level or larger
                    query.Append("(.lines; - rel[boundary~\"region|timezone|fire_district|collection\"].lines;) -> .lines; (.lines; - rel(if: abs(t[\"admin_level\"]) < 7).lines;) -> .lines; ");
                }

                // if there are conflicting tags that this feature can't have
                if (!string.IsNullOrEmpty(gnisClassAttributes.ConflictingTags))
                {
                    StringBuilder conflictingTagBuilder = new();

                    OsmTagProto[] conflictingTags = gnisClassAttributes.GetConflictingTags();

                    // filter out ways and relations that have conflicting tags
                    foreach (OsmTagProto conflictingTag in conflictingTags)
                    {
                        if ("*".Equals(conflictingTag.Value))
                            conflictingTagBuilder.Append($"(.lines; - wr[\"{conflictingTag.Name}\"].lines;) -> .lines; ");
                        else
                            conflictingTagBuilder.Append($"(.lines; - wr[\"{conflictingTag.Name}\"=\"{conflictingTag.Value}\"].lines;) -> .lines; ");
                    }

                    query.Append(conflictingTagBuilder);
                }

                // recurse down to collect ways within relations and nodes within ways
                query.Append("(.lines; .lines >;) -> .lines; ");
            }

            // combine results
            query.Append("(.points; .lines;); ");

            // query.Append("); convert item ::=::,::geom=geom(),_osm_type=type(),::id=id(); out geom; ");
            query.Append("out meta;");

            return query.ToString();
        }

        public string BuildFeatureIdQuery(GnisRecord gnisRecord)
        {
            // use a 20 km bounding box
            string areaFilter = string.Join(",", MakeBoundingBox(gnisRecord.Primary.Latitude, gnisRecord.Primary.Longitude, 20000));

            // find features that exactly match the feature id
            return $"nwr[\"gnis:feature_id\"=\"{gnisRecord.FeatureId}\"]({areaFilter}); (._; >;); out meta;";
        }

        public string BuildNameAndTagQuery(GnisRecord gnisRecord)
        {
            StringBuilder query = new();

            // get the attributes for the feature class of the GNIS record
            GnisClassAttributes gnisClassAttributes = gnisClassData.GetGnisClassAttributes(gnisRecord.FeatureClass);

            string areaFilter;
            if (!gnisRecord.HasSource())
            {
                // use a 20 km bounding box
                areaFilter = string.Join(",", MakeBoundingBox(gnisRecord.Primary.Latitude, gnisRecord.Primary.Longitude, 20000));
            }
            else
            {
                // make a square bounding box that encloses the feature
                /*
                double centerLat = (gnisRecord.Source.Latitude + gnisRecord.Primary.Latitude) / 2;
                double centerLon = (gnisRecord.Source.Longitude + gnisRecord.Primary.Longitude) / 2;

                double distance = gnisRecord.Primary.GetDistanceTo(gnisRecord.Source);

                areaFilter = string.Join(",", MakeBoundingBox(centerLat, centerLon, distance));
                */
                areaFilter = string.Join(",", MakeEnclosingBox(gnisRecord.Primary.Latitude, gnisRecord.Primary.Longitude, gnisRecord.Source.Latitude, gnisRecord.Source.Longitude));

            }

            query.Append("( ");

            // escape quotes in the feature name
            string escapedFeatureName = gnisRecord.FeatureName.Contains('"')
                ? Regex.Replace(gnisRecord.FeatureName, "\"", "\\\"")
                : gnisRecord.FeatureName;

            // for each of the primary tags
            foreach (OsmTagProto tag in gnisClassAttributes.GetPrimaryTags())
            {
                // if the tag has a wildcard value
                if ("*".Equals(tag.Value))
                    // find all features with the name and tag key
                    query.Append($"nwr[\"{tag.Name}\"][\"name\"=\"{escapedFeatureName}\"]({areaFilter}); ");
                else
                    // find all features where the name and specific tag value
                    query.Append($"nwr[\"{tag.Name}\"=\"{tag.Value}\"][\"name\"=\"{escapedFeatureName}\"]({areaFilter}); ");
            }

            query.Append(" ); (._; >;); out meta;");

            return query.ToString();
        }

        public string BuildEnclosureQuery(GnisRecord gnisRecord)
        {
            return $"is_in({gnisRecord.PrimaryLat},{gnisRecord.PrimaryLon})->.a; wr(pivot.a); (._; - rel(if: abs(t[\"admin_level\"]) < 6)._;); (._; node(w);); out meta;";
        }

        public string BuildSecondQuery(GnisRecord gnisRecord)
        {
            StringBuilder query = new();

            // get the attributes for the feature class of the GNIS record
            GnisClassAttributes gnisClassAttributes = gnisClassData.GetGnisClassAttributes(gnisRecord.FeatureClass);

            string areaFilter;
            if (!gnisRecord.HasSource())
            {
                // use a 20 km bounding box
                areaFilter = string.Join(",", OverpassQueryBuilder.MakeBoundingBox(gnisRecord.Primary.Latitude, gnisRecord.Primary.Longitude, 20000));
            }
            else
            {
                // use the extent of the feature as the bounding box
                areaFilter = string.Join(",", OverpassQueryBuilder.MakeEnclosingBox(gnisRecord.Primary.Latitude, gnisRecord.Primary.Longitude, gnisRecord.Source.Latitude, gnisRecord.Source.Longitude));
            }

            string osmTypes;

            if (gnisClassAttributes.HasGeometry("point"))
            {
                if (gnisClassAttributes.HasGeometry("line") || gnisClassAttributes.HasGeometry("area"))
                {
                    osmTypes = string.IsNullOrEmpty(gnisClassAttributes.RelationTypes) ? "nw" : "nwr";
                }
                else
                {
                    osmTypes = string.IsNullOrEmpty(gnisClassAttributes.RelationTypes) ? "node" : "nr";
                }
            }
            else
            {
                // this would exclude nodes, but often GNIS features are imported as nodes and not ever edited
                //osmTypes = string.IsNullOrEmpty(gnisClassAttributes.RelationTypes) ? "way" : "wr";
                osmTypes = string.IsNullOrEmpty(gnisClassAttributes.RelationTypes) ? "nw" : "nwr";
            }

            // get everything in the area
            query.Append($"[bbox:{areaFilter}];");

            // look for anything with the correct Feature ID
            query.Append("( ");
            query.Append($"{osmTypes}[\"gnis:feature_id\"][\"gnis:feature_id\"~\".*{gnisRecord.FeatureId}.*\"]; ");
            /*
            // no longer needed after August 2023
            query.Append($"{osmTypes}[\"gnis:id\"][\"gnis:id\"~\".*{gnisRecord.FeatureId}.*\"]; ");
            query.Append($"{osmTypes}[\"tiger:PLACENS\"][\"tiger:PLACENS\"~\".*{gnisRecord.FeatureId}.*\"]; ");
            query.Append($"{osmTypes}[\"NHD:GNIS_ID\"][\"NHD:GNIS_ID\"~\".*{gnisRecord.FeatureId}.*\"]; ");
            query.Append($"{osmTypes}[\"ref:gnis\"][\"ref:gnis\"~\".*{gnisRecord.FeatureId}.*\"]; ");
            */

            // escape quotes in the feature name
            string escapedFeatureName = gnisRecord.FeatureName.Contains('"')
                ? Regex.Replace(gnisRecord.FeatureName, "\"", "\\\"")
                : gnisRecord.FeatureName;

            // for each of the primary tags
            foreach (OsmTagProto tag in gnisClassAttributes.GetPrimaryTags())
            {
                // if the tag has a wildcard value
                if ("*".Equals(tag.Value))
                    // find all features with the name and tag key
                    query.Append($"{osmTypes}[\"{tag.Name}\"][\"name\"=\"{escapedFeatureName}\"]; ");
                else
                    // find all features where the name and specific tag value
                    query.Append($"{osmTypes}[\"{tag.Name}\"=\"{tag.Value}\"][\"name\"=\"{escapedFeatureName}\"]; ");
            }

            query.Append(") -> .features; ");

            // if the feature can be an area
            if (gnisClassAttributes.HasGeometry("area"))
            {
                // if the feature can be a relation
                if (!string.IsNullOrEmpty(gnisClassAttributes.RelationTypes))
                    query.Append($"is_in({gnisRecord.PrimaryLat},{gnisRecord.PrimaryLon})->.a; wr(pivot.a) -> .areas; (.areas; - rel(if: abs(t[\"admin_level\"]) < 6).areas;) -> .areas; ");
                else
                    query.Append($"is_in({gnisRecord.PrimaryLat},{gnisRecord.PrimaryLon})->.a; way(pivot.a) -> .areas; ");
            }

            query.Append("(.features; .features >; .areas; node(w.areas);); out meta; ");

            return query.ToString();
        }

        public static double[] MakeBoundingBox(double lat, double lon, double width)
        {
            // offset from the GNIS coordinates in degrees
            double degreeOffset = 0.01F;

            // difference in meters between the length of a side of the box and the target length (2 km)
            double error;

            // coordinates of the sides of the box
            GeoCoordinate east;
            GeoCoordinate west;
            GeoCoordinate north;
            GeoCoordinate south;

            // use newton's method to find the right width of the box in degrees longitude
            do
            {
                // set west and east sides of the box
                east = new GeoCoordinate(lat, lon + degreeOffset);
                west = new GeoCoordinate(lat, lon - degreeOffset);

                // calculate the width of the box
                double distance = east.GetDistanceTo(west);

                // calculate difference from target width (2 km)
                error = distance - width;

                // correct offset using newton's method
                degreeOffset -= error * (degreeOffset / distance);

                // stop when the error is less than 1 m (usually on the second iteration)
            } while (Math.Abs(error) > 1.0);

            // reset the offset
            degreeOffset = 0.01F;

            // use newton's method to find the right height of the box in degrees latitude
            do
            {
                // set the north and south sides of the box
                north = new GeoCoordinate(lat + degreeOffset, lon);
                south = new GeoCoordinate(lat - degreeOffset, lon);

                // calculate the height of the box
                double distance = north.GetDistanceTo(south);

                // calculate difference from target height (2 km)
                error = distance - width;

                // correct offset using newton's method
                degreeOffset -= error * (degreeOffset / distance);

                // stop when the error is less than 1 m (usually on the second iteration)
            } while (Math.Abs(error) > 1.0);

            // return the box with coordinates in the order for an Overpass bounding box
            double[] result = { Math.Round(south.Latitude, 7), Math.Round(west.Longitude, 7), Math.Round(north.Latitude, 7), Math.Round(east.Longitude, 7) };
            return result;
        }

        public static double[] MakeEnclosingBox(double primaryLat, double primaryLon, double sourceLat, double sourceLon)
        {
            double centerLat = (primaryLat + sourceLat) / 2;
            double centerLon = (primaryLon + sourceLon) / 2;

            GeoCoordinate west = new(centerLat, Math.Min(primaryLon, sourceLon));
            GeoCoordinate east = new(centerLat, Math.Max(primaryLon, sourceLon));
            GeoCoordinate north = new(Math.Max(primaryLat, sourceLat), centerLon);
            GeoCoordinate south = new(Math.Min(primaryLat, sourceLat), centerLon);

            double width = Math.Max(west.GetDistanceTo(east), south.GetDistanceTo(north));

            return MakeBoundingBox(centerLat, centerLon, width + 100);
        }

        public XOsmData? SendQuery(string overpassQuery)
        {
            MemoryStream memoryStream = new MemoryStream();

            // sometimes Overpass balks under heavy load
            // use an exponential fallback and retry
            for (int i = 0; i < 4; i++)
            {
                try
                {
                    // set up the request
                    HttpRequestMessage request = new(HttpMethod.Post, overpassUrl);
                    request.Headers.Add("User-Agent", Program.PrivateData.UserAgent);
                    request.Content = new StringContent(overpassQuery);

                    // send the query
                    HttpResponseMessage response = Program.HttpClient.Send(request);

                    // make sure the response is not an error
                    response.EnsureSuccessStatusCode();

                    // copy to a memory stream so that we can re-read the response if needed
                    response.Content.ReadAsStream().CopyTo(memoryStream);
                    memoryStream.Position = 0;

                    // return the deserialized XML
                    return overpassSerializer.Deserialize(memoryStream) as XOsmData;
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e);
                    memoryStream.Position = 0;
                    Console.Error.WriteLine($"Response: {new StreamReader(memoryStream).ReadToEnd()}");
                    Thread.Sleep((int)Math.Pow(2,i));
                    Console.Error.WriteLine($"Retry {i+1}: {overpassQuery}");
                }
            }

            throw new Exception("Unable to get a response from Overpass.");
        }

        public async Task<XOsmData?> SendQueryAsync(string overpassQuery)
        {
            // set up the request
            HttpRequestMessage request = new(HttpMethod.Post, overpassUrl);
            request.Headers.Add("User-Agent", Program.PrivateData.UserAgent);
            request.Content = new StringContent(overpassQuery);

            // send the query
            Task<HttpResponseMessage> responseTask = Program.HttpClient.SendAsync(request);

            HttpResponseMessage response = await responseTask;

            response.EnsureSuccessStatusCode();

            return overpassSerializer.Deserialize(response.Content.ReadAsStream()) as XOsmData;
        }

        internal string BuildObjectQuery(string type, long id)
        {
            if ("node".Equals(type))
                return $"node({id}); out meta;";

            if ("way".Equals(type))
                return $"(way({id}); >;); out meta;";

            if ("relation".Equals(type))
                return $"(rel({id}); >;); out meta;";

            throw new ArgumentException($"Unknown type: {type}");
        }
    }
}