using System.ComponentModel.DataAnnotations;
using System.ComponentModel.Design;
using System.Text.Json;

namespace recogniser
{

    public class MapRouletteTaskBuilder
    {
        private readonly InstructionBuilder instructionBuilder;

        public MapRouletteTaskBuilder(GnisClassData gnisClassData)
        {
            this.instructionBuilder = new(gnisClassData);
        }

        /// <summary>
        /// Build a MapRoulette task for a collection of OSM features that match the GNIS record.
        /// </summary>
        /// <param name="gnisRecord"></param>
        /// <param name="matchResults"></param>
        /// <param name="validationResults"></param>
        /// <returns></returns>
        public string BuildPlainMapRouletteTask(GnisRecord gnisRecord, List<GnisMatchResult> matchResults, List<GnisValidationResult> validationResults)
        {
            JMapRouletteGeoJson task = new();

            // seems like MR is only happy if there is one root feature in the FeatureCollection
            /*
            foreach (GnisMatchResult matchResult in matchResults)
            {
                GeoJsonFeature osmFeature = ConvertToGeoJsonFeature(matchResult.osmFeature, gnisRecord);
                task.Features.Add(osmFeature);
            }
            */

            // use the GNIS record to create a GeoJson feature for MapRoulette
            GeoJsonFeature gnisFeature = ConvertToGeoJsonFeature(gnisRecord);
            task.Features.Add(gnisFeature);

            string instructions = instructionBuilder.BuildMultiMatchInstructions(gnisRecord, matchResults, validationResults);
            gnisFeature.Properties.Add("instructions", instructions);

            return JsonSerializer.Serialize(task);
        }

        /// <summary>
        /// Build a plain MapRoulette task for a single OSM feature that matched the GNIS record.
        /// </summary>
        /// <param name="gnisRecord"></param>
        /// <param name="matchResults"></param>
        /// <param name="validationResults"></param>
        /// <returns></returns>
        public string BuildPlainMapRouletteTask(GnisRecord gnisRecord, GnisMatchResult matchResult, GnisValidationResult validationResult)
        {
            JMapRouletteGeoJson task = new();

            GeoJsonFeature osmFeature = ConvertToGeoJsonFeature(matchResult.osmFeature, gnisRecord);
            task.Features.Add(osmFeature);

            string instructions = instructionBuilder.BuildPlainInstructions(gnisRecord, matchResult, validationResult);
            osmFeature.Properties.Add("instructions", instructions);

            return JsonSerializer.Serialize(task);
        }

        /// <summary>
        /// Build a plain MapRoulette where there was no OSM feature that matched the GNIS record
        /// </summary>
        /// <param name="gnisRecord"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        internal string BuildPlainMapRouletteTask(GnisRecord gnisRecord)
        {
            JMapRouletteGeoJson task = new();

            GeoJsonFeature osmFeature = ConvertToGeoJsonFeature(gnisRecord);
            task.Features.Add(osmFeature);

            string instructions = instructionBuilder.BuildPlainNoMatchInstructions(gnisRecord);
            osmFeature.Properties.Add("instructions", instructions);

            return JsonSerializer.Serialize(task);
        }

        /// <summary>
        /// Build a collaborative MapRoulette task for a single OSM feature that matched a GNIS record.
        /// </summary>
        /// <param name="gnisRecord"></param>
        /// <param name="matchResult"></param>
        /// <param name="validationResult"></param>
        /// <param name="osmChangeXml"></param>
        /// <returns></returns>
        public string BuildCollaborativeMapRouletteTask(GnisRecord gnisRecord, GnisMatchResult matchResult, GnisValidationResult validationResult, string? osmChangeXml)
        {
            // build an OSC task to modify an existing OSM feature
            JMapRouletteGeoJson task = new();

            GeoJsonFeature osmFeature = ConvertToGeoJsonFeature(matchResult.osmFeature, gnisRecord);
            task.Features.Add(osmFeature);

            if (osmChangeXml != null)
            {
                task.CooperativeWork = new()
                {
                    File = new()
                    {
                        Content = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(osmChangeXml))
                    }
                };
            }

            string instructions;
            if (matchResult.MatchType != GnisMatchType.conflictingMatch)
            {
                if (matchResult.specialCondition == GnisMatchSpecialCondition.NEW_RELATION)
                    instructions = instructionBuilder.BuildNewRelationInstructions(gnisRecord, matchResult, validationResult);
                else
                    instructions = instructionBuilder.BuildSingleMatchInstructions(gnisRecord, matchResult, validationResult);
            }
            else
                instructions = instructionBuilder.BuildPlainInstructions(gnisRecord, matchResult, validationResult);
            osmFeature.Properties.Add("instructions", instructions);

            return JsonSerializer.Serialize(task);
        }

        /// <summary>
        /// Build a collaborative MapRoulette task where there was no OSM feature that matched the GNIS record.
        /// </summary>
        /// <param name="gnisRecord"></param>
        /// <param name="osmChangeXml"></param>
        /// <returns></returns>
        internal string BuildCollaborativeMapRouletteTask(GnisRecord gnisRecord, string? osmChangeXml)
        {
            // build an OSC task to add a feature that didn't exist before
            JMapRouletteGeoJson task = new();

            GeoJsonFeature gnisFeature = ConvertToGeoJsonFeature(gnisRecord);
            task.Features.Add(gnisFeature);

            if (osmChangeXml != null)
            {
                task.CooperativeWork = new()
                {
                    File = new()
                    {
                        Content = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(osmChangeXml))
                    }
                };
            }

            string instructions = instructionBuilder.BuildNoMatchInstructions(gnisRecord);
            gnisFeature.Properties.Add("instructions", instructions);

            return JsonSerializer.Serialize(task);
        }

        /// <summary>
        /// Build a Tag Fix MapRoulette task for a single OSM feature that matched a GNIS record.
        /// </summary>
        /// <param name="gnisRecord"></param>
        /// <param name="matchResult"></param>
        /// <param name="validationResult"></param>
        /// <param name="operations"></param>
        /// <returns></returns>
        internal string BuildTagFixMapRouletteTask(GnisRecord gnisRecord, GnisMatchResult matchResult, GnisValidationResult validationResult, List<TagFixOperation> operations)
        {
            // build a Tag Fix task to modify an existing OSM feature
            JMapRouletteGeoJson task = new();

            GeoJsonFeature gnisFeature = ConvertToGeoJsonFeature(matchResult.osmFeature, gnisRecord);
            task.Features.Add(gnisFeature);

            task.CooperativeWork = new()
            {
                Operations = operations
            };

            string instructions = instructionBuilder.BuildTagFixInstructions(gnisRecord, matchResult, validationResult);
            gnisFeature.Properties.Add("instructions", instructions);

            return JsonSerializer.Serialize(task);
        }

        private GeoJsonFeature ConvertToGeoJsonFeature(GnisRecord gnisRecord)
        {
            GeoJsonFeature feature = new();

            feature.Properties["name"] = gnisRecord.FeatureName;
            feature.Properties["gnis:feature_name"] = gnisRecord.FeatureName;
            feature.Properties["gnis:feature_id"] = gnisRecord.FeatureId;
            feature.Properties["gnis:feature_class"] = gnisRecord.FeatureClass;

            if (!gnisRecord.HasSource())
            {
                feature.Geometry = new GeoJsonPointGeometry()
                {
                    Coordinates = new double[] { gnisRecord.Primary.Longitude, gnisRecord.Primary.Latitude }
                };
            }
            else
            {
                feature.Geometry = new GeoJsonLineStringGeometry()
                {
                    Coordinates = new double[][]{
                        new double[] { gnisRecord.Source.Longitude, gnisRecord.Source.Latitude },
                        new double[] { gnisRecord.Primary.Longitude, gnisRecord.Primary.Latitude }
                    }
                };
            }

            feature.Id = -long.Parse(gnisRecord.FeatureId);

            return feature;
        }

        private GeoJsonFeature ConvertToGeoJsonFeature(OsmFeature osmFeature, GnisRecord gnisRecord)
        {
            GeoJsonFeature feature = new();

            foreach (OsmTag tag in osmFeature.Tags)
            {
                feature.Properties[tag.Key] = tag.Value;
            }

            feature.Properties["gnis:feature_name"] = gnisRecord.FeatureName;
            feature.Properties["gnis:feature_id"] = gnisRecord.FeatureId;
            feature.Properties["gnis:feature_class"] = gnisRecord.FeatureClass;

            if (osmFeature is OsmNode node)
            {
                feature.Geometry = new GeoJsonPointGeometry()
                {
                    Coordinates = new double[] { node.Lon, node.Lat }
                };
            }

            if (osmFeature is OsmWay way)
            {
                GeoJsonLineStringGeometry geometry = new();
                feature.Geometry = geometry;

                List<double[]> coordinates = new();

                foreach (OsmWayNode wayNodeRef in way.Nodes)
                {
                    if (way.GetNode(wayNodeRef) is OsmNode wayNode)
                        coordinates.Add(new double[] { wayNode.Lon, wayNode.Lat });
                }

                geometry.Coordinates = coordinates.ToArray();
            }

            if (osmFeature is OsmRelation relation)
            {
                GeoJsonMultiLineStringGeometry geometry = new();
                feature.Geometry = geometry;

                List<double[][]> coordinates = new();
                double[]? labelCoordinates = null;
                double[]? adminCoordinates = null;

                foreach (OsmRelationMember relationMember in relation.Members)
                {
                    OsmFeature? member = relation.GetMember(relationMember);

                    if (member is OsmNode memberNode)
                    {
                        // if this is a label for a boundary relation
                        if ("label".Equals(relationMember.Role) && memberNode != null)
                        {
                            labelCoordinates = new double[] { memberNode.Lon, memberNode.Lat };
                        }
                        else if ("admin_centre".Equals(relationMember.Role) && memberNode != null)
                        {
                            adminCoordinates = new double[] { memberNode.Lon, memberNode.Lat };
                        }
                        else
                            continue;
                    }

                    if (member is OsmWay memberWay)
                    {
                        List<double[]> wayCoordinates = new();

                        foreach (OsmWayNode wayNodeRef in memberWay.Nodes)
                        {
                            if (memberWay.GetNode(wayNodeRef) is OsmNode wayNode)
                                wayCoordinates.Add(new double[] { wayNode.Lon, wayNode.Lat });
                        }

                        coordinates.Add(wayCoordinates.ToArray());
                    }
                }

                // if the relation members were downloaded
                if (coordinates.Count > 0)
                {
                    geometry.Coordinates = coordinates.ToArray();
                }
                // if the relation members weren't downloaded but we got a label node
                else if (labelCoordinates != null)
                {
                    feature.Geometry = new GeoJsonPointGeometry()
                    {
                        Coordinates = labelCoordinates
                    };
                }
                // or if we got an admin_centre node
                else if (adminCoordinates != null)
                {
                    feature.Geometry = new GeoJsonPointGeometry()
                    {
                        Coordinates = adminCoordinates
                    };
                }
                // otherwise just drop a pin at the primary location from the GNIS record
                else
                {
                    feature.Geometry = new GeoJsonPointGeometry()
                    {
                        Coordinates = new double[] { gnisRecord.Primary.Longitude, gnisRecord.Primary.Latitude }
                    };
                }
            }

            feature.Id = osmFeature.Id;

            return feature;
        }
    }
}