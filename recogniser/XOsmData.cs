// NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
using GeoCoordinatePortable;
using System.ComponentModel;
using System.Xml.Serialization;

namespace recogniser
{
    /// <remarks/>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    [XmlRoot(Namespace = "", ElementName = "osm", IsNullable = false)]
    public class XOsmData
    {
        [NonSerialized]
        private List<OsmFeature>? _features = null;

        [NonSerialized]
        private readonly Dictionary<long, OsmNode> _nodeCollection = new();

        [NonSerialized]
        private readonly Dictionary<long, OsmWay> _wayCollection = new();

        [NonSerialized]
        private readonly Dictionary<long, OsmRelation> _relationCollection = new();

        /// <remarks/>
        [XmlAttribute("version")]
        public string Version { get; set; } = "0.6";

        /// <remarks/>
        [XmlAttribute("generator")]
        public string Generator { get; set; } = "gnis-matcher";

        /// <remarks/>
        [XmlElement("note")]
        public string Note { get; set; } = string.Empty;

        /// <remarks/>
        [XmlElement("meta")]
        public OsmMeta Meta { get; set; } = new OsmMeta();

        /// <remarks/>
        [XmlElement("node")]
        public List<OsmNode> Nodes { get; set; } = new();

        /// <remarks/>
        [XmlElement("way")]
        public List<OsmWay> Ways { get; set; } = new();

        /// <remarks/>
        [XmlElement("relation")]
        public List<OsmRelation> Relations { get; set; } = new();

        public List<OsmFeature> GetFeatures()
        {
            if (_features == null)
            {
                _features = new();
                _features.AddRange(Nodes);
                _features.AddRange(Ways);
                _features.AddRange(Relations);
            }
            return _features;
        }

        public void CollectNodesAndWays()
        {
            foreach (OsmNode node in Nodes)
            {
                node.SetParent(this);
                //_nodeCollection.Add(node.Id, node);
            }

            foreach (OsmWay way in Ways)
            {
                way.SetParent(this);
                //_wayCollection.Add(way.Id, way);
            }

            foreach (OsmRelation relation in Relations)
            {
                relation.SetParent(this);
                //_relationCollection.Add(relation.Id, relation);
            }
        }

        
        public Dictionary<long, OsmNode> GetNodeCollection()
        {
            return _nodeCollection;
        }

        public Dictionary<long, OsmWay> GetWayCollection()
        {
            return _wayCollection;
        }

        public Dictionary<long, OsmRelation> GetRelationCollection()
        {
            return _relationCollection;
        }

        public static XOsmData? Merge(XOsmData? firstOsmDataSet, XOsmData? secondOsmDataSet)
        {
            if (firstOsmDataSet == null)
                return secondOsmDataSet;
            if (secondOsmDataSet == null)
                return null;

            firstOsmDataSet._features = null;

            foreach (OsmNode node in secondOsmDataSet.Nodes)
            {
                if (!firstOsmDataSet._nodeCollection.ContainsKey(node.Id))
                {
                    firstOsmDataSet.Nodes.Add(node);
                    node.SetParent(firstOsmDataSet);
                }
            }

            foreach (OsmWay way in secondOsmDataSet.Ways)
            {
                if (!firstOsmDataSet._wayCollection.ContainsKey(way.Id))
                {
                    firstOsmDataSet.Ways.Add(way);
                    way.SetParent(firstOsmDataSet);
                }
            }

            foreach (OsmRelation relation in secondOsmDataSet.Relations)
            {
                if (!firstOsmDataSet._relationCollection.ContainsKey(relation.Id))
                {
                    firstOsmDataSet.Relations.Add(relation);
                    relation.SetParent(firstOsmDataSet);
                }
            }

            return firstOsmDataSet;
        }
    }

    /// <remarks/>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class OsmMeta
    {
        /// <remarks/>
        [XmlAttribute("osm_base")]
        public string OsmBase { get; set; } = string.Empty;
    }

    /// <remarks/>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class OsmTag
    {
        /// <remarks/>
        [XmlAttribute("k")]
        public string Key { get; set; } = string.Empty;

        /// <remarks/>
        [XmlAttribute("v")]
        public string Value { get; set; } = string.Empty;

        public OsmTag() { }

        public OsmTag(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public OsmTag(string keyValue)
        {
            string[] parts = Key.Split('=');
            if (parts.Length != 2)
                throw new Exception($"Malformed key/value string: {keyValue}");
            Key = parts[0];
            Value = parts[1];
        }

        public override string ToString()
        {
            return $"{Key}={Value}";
        }
    }

    /// <remarks/>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class OsmBounds
    {
        /// <remarks/>
        [XmlAttribute("minlat")]
        public double MinLat { get; set; }

        /// <remarks/>
        [XmlAttribute("minlon")]
        public double MinLon { get; set; }

        /// <remarks/>
        [XmlAttribute("maxlat")]
        public double MaxLat { get; set; }

        /// <remarks/>
        [XmlAttribute("maxlon")]
        public double MaxLon { get; set; }
    }

    /// <remarks/>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class OsmNode : OsmFeature
    {
        private static long _temporaryId = -1;

        private XOsmData? _parent = null;

        /*
        /// <remarks/>
        [XmlElement("tag")]
        public List<OsmTag> Tags { get; set; } = new();

        /// <remarks/>
        [XmlAttribute("id")]
        public long Id { get; set; }
        */

        /// <remarks/>
        [XmlAttribute("lat")]
        public double Lat { get; set; }

        /// <remarks/>
        [XmlAttribute("lon")]
        public double Lon { get; set; }

        /*
        /// <remarks/>
        [XmlAttribute("version")]
        public long Version { get; set; }

        /// <remarks/>
        [XmlAttribute("timestamp")]
        public DateTime Timestamp { get; set; }

        /// <remarks/>
        [XmlAttribute("changeset")]
        public long Changeset { get; set; }

        /// <remarks/>
        [XmlAttribute("uid")]
        public long Uid { get; set; }

        /// <remarks/>
        [XmlAttribute("user")]
        public string? User { get; set; }

        //[XmlAttribute("bounds")]
        //public OsmBounds? Bounds => null;
        */

        public OsmNode()
        {
            Id = Interlocked.Decrement(ref _temporaryId);
        }

        /*
        public OsmNode(string lat, string lon)
        {
            Id = _temporaryId--;
            Lat = double.Parse(lat);
            Lon = double.Parse(lon);
        }
        */
        
        public OsmNode(GeoCoordinate coordinate)
        {
            Id = Interlocked.Decrement(ref _temporaryId);
            Lat = coordinate.Latitude;
            Lon = coordinate.Longitude;
        }
        
        public override FeatureType GetOsmType() => FeatureType.node;

        public GeoCoordinate GetCoordinate() => new(Lat, Lon);

        public void SetCoordinate(GeoCoordinate coordinate)
        {
            Lat = coordinate.Latitude;
            Lon = coordinate.Longitude;
        }

        public override OsmLinearExtent? GetLinearExtent() => null;

        public override OsmFeature? AddStartNode(OsmNode startNode)
        {
            return null;
        }

        public override OsmFeature? AddEndNode(OsmNode endNode)
        {
            return null;
        }

        public override List<OsmFeature>? Reverse()
        {
            return null;
        }

        public override void SetParent(XOsmData parent)
        {
            _parent = parent;
            _parent.GetNodeCollection().TryAdd(Id,this);
        }

        public override XOsmData GetParent()
        {
            if (_parent == null)
                throw new Exception("Parent reference must not be null.");
            return _parent;
        }
    }

    /// <remarks/>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class OsmWay : OsmFeature
    {
        private static long _temporaryId = -1;

        private XOsmData? _parent = null;

        /// <remarks/>
        //[XmlElement("bounds")]
        //public OsmBounds? Bounds { get; set; }

        /// <remarks/>
        [XmlElement("nd")]
        public List<OsmWayNode> Nodes { get; set; } = new();

        /*
        /// <remarks/>
        [XmlElement("tag")]
        public List<OsmTag> Tags { get; set; } = new();

        /// <remarks/>
        [XmlAttribute("id")]
        public long Id { get; set; }

        /// <remarks/>
        [XmlAttribute("version")]
        public long Version { get; set; }

        /// <remarks/>
        [XmlAttribute("timestamp")]
        public DateTime Timestamp { get; set; }

        /// <remarks/>
        [XmlAttribute("changeset")]
        public long Changeset { get; set; }

        /// <remarks/>
        [XmlAttribute("uid")]
        public long Uid { get; set; }

        /// <remarks/>
        [XmlAttribute("user")]
        public string? User { get; set; }
        */

        public OsmWay()
        {
            Id = Interlocked.Decrement(ref _temporaryId);
        }

        public override void SetParent(XOsmData parent)
        {
            _parent = parent;
            _parent.GetWayCollection().TryAdd(Id, this);
        }

        public override XOsmData GetParent()
        {
            if (_parent == null)
                throw new Exception("Parent must not be null.");
            return _parent;
        }

        public override FeatureType GetOsmType() => FeatureType.way;

        public override OsmLinearExtent? GetLinearExtent()
        {
            if (_parent == null)
                throw new Exception("Parent data set reference must not be null.");
            if (Nodes.Count == 0)
                return null;
            OsmNode first = _parent.GetNodeCollection()[Nodes[0].Ref];
            OsmNode last = _parent.GetNodeCollection()[Nodes[^1].Ref];
            return new OsmLinearExtent(first.Lat, first.Lon, last.Lat, last.Lon);
        }

        public override OsmFeature? AddStartNode(OsmNode startNode)
        {
            if (_parent == null)
                throw new Exception("Parent data set reference must not be null.");

            // this node must already be part a data set
            // if not, the caller messed up
            if (startNode.GetParent().GetNodeCollection()[startNode.Id] == null)
                throw new Exception("Node is not part of an OSM data set.");

            // add a new node to this way that refers to the start node
            OsmWayNode newNode = new() { Ref = startNode.Id };
            Nodes.Insert(0, newNode);

            return this;
        }

        public override OsmFeature? AddEndNode(OsmNode endNode)
        {
            if (_parent == null)
                throw new Exception("Parent data set reference must not be null.");

            // this node must already be part a data set
            // if not, the caller messed up
            if (endNode.GetParent().GetNodeCollection()[endNode.Id] == null)
                throw new Exception("Node is not part of an OSM data set.");

            // add a new node to this way that refers to the start node
            OsmWayNode newNode = new() { Ref = endNode.Id };
            Nodes.Add(newNode);

            return this;
        }

        public override List<OsmFeature>? Reverse()
        {
            Nodes.Reverse();
            return new() { this };
        }

        public OsmNode? GetNode(OsmWayNode wayNode)
        {
            return _parent?.GetNodeCollection()[wayNode.Ref];
        }
    }

    /// <remarks/>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class OsmWayNode
    {
        /// <remarks/>
        [XmlAttribute("ref")]
        public long Ref { get; set; }
    }

    /// <remarks/>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    [XmlRoot(Namespace = "", ElementName = "relation", IsNullable = false)]
    public class OsmRelation : OsmFeature
    {
        private static long _temporaryId = -1;
        private XOsmData? _parent = null;

        private List<OsmRelationMember>? _connectedMembers = null;

        /// <remarks/>
        //public OsmBounds Bounds { get; set; }

        /// <remarks/>
        [XmlElement("member")]
        public List<OsmRelationMember> Members { get; set; } = new();

        /*
        /// <remarks/>
        [XmlElement("tag")]
        public List<OsmTag> Tags { get; set; } = new();

        /// <remarks/>
        [XmlAttribute("id")]
        public long Id { get; set; }

        /// <remarks/>
        [XmlAttribute("version")]
        public long Version { get; set; }

        /// <remarks/>
        [XmlAttribute("timestamp")]
        public DateTime Timestamp { get; set; }

        /// <remarks/>
        [XmlAttribute("changeset")]
        public long Changeset { get; set; }

        /// <remarks/>
        [XmlAttribute("uid")]
        public long Uid { get; set; }

        /// <remarks/>
        [XmlAttribute("user")]
        public string? User { get; set; }
        */

        public OsmRelation()
        {
            Id = Interlocked.Decrement(ref _temporaryId);
        }

        public override void SetParent(XOsmData parent)
        {
            _parent = parent;
            _parent.GetRelationCollection().TryAdd(Id, this);

            foreach (OsmRelationMember member in Members)
            {
                member.SetParent(parent);
            }
        }

        public override XOsmData GetParent()
        {
            if (_parent == null)
                throw new Exception("Parent must not be null");
            return _parent;
        }

        public override FeatureType GetOsmType() => FeatureType.relation;

        // public long Id => _id;

        public override OsmFeature? AddStartNode(OsmNode startNode)
        {
            if (_parent == null)
                throw new Exception("Parent data set reference must not be null.");
            if (_connectedMembers != null)
            {
                OsmFeature? firstWay = _connectedMembers.First().GetOsmFeature();
                return firstWay?.AddStartNode(startNode);
            }
            return null;
        }

        public override OsmFeature? AddEndNode(OsmNode endNode)
        {
            if (_parent == null)
                throw new Exception("Parent data set reference must not be null.");
            if (_connectedMembers != null)
            {
                OsmWay lastWay = _parent.GetWayCollection()[_connectedMembers.Last().Ref];
                return lastWay.AddEndNode(endNode);
            }
            return null;
        }

        public override List<OsmFeature>? Reverse()
        {
            if (_parent == null)
                throw new Exception("Parent data set reference must not be null.");

            List<OsmFeature> modifiedOsmFeatures = new();

            foreach (OsmRelationMember member in Members)
            {
                if ("way".Equals(member.Type))
                {
                    OsmWay memberWay = _parent.GetWayCollection()[member.Ref];
                    memberWay.Nodes.Reverse();
                    modifiedOsmFeatures.Add(memberWay);
                }
            }

            Members.Reverse();

            _connectedMembers?.Reverse();

            modifiedOsmFeatures.Add(this);

            return modifiedOsmFeatures;
        }

        public override OsmLinearExtent? GetLinearExtent()
        {
            // if there are no members
            if (Members.Count == 0)
                return null;

            /*
            // if there are too many members, don't bother
            if (_members.Length > 50)
            {
                Program.Verbose.WriteLine($"Not processing: Relation {this._id} with {_members.Length} members");
                return null;
            }
            */

            // start with a list of all the (presumably) unordered members
            List<OsmRelationMember> disjointMembers = new();
            disjointMembers.AddRange(Members);

            // build a list of ordered members where the end of one member matches the start of the next member
            List<OsmRelationMember> connectedMembers = new();

            // start the ordered list with the first member
            connectedMembers.Add(Members[0]);
            disjointMembers.Remove(Members[0]);

            // track the full extent of the list of ordered members
            OsmLinearExtent? connectedLinearExtent = Members[0].GetLinearExtent();

            // true if we made a connection in this iteration
            bool connectionMade;

            do
            {
                connectionMade = false;

                // for each of the disjoint members
                foreach (OsmRelationMember member in disjointMembers)
                {
                    // if this is a side stream in a waterway relation
                    if ("side_stream".Equals(member.Role))
                    {
                        // it isn't part of the linear extent
                        disjointMembers.Remove(member);
                        break;
                    }

                    // get the extent of the member
                    OsmLinearExtent? memberExtent = member.GetLinearExtent();

                    // if the member has no nodes (which is unlikely)
                    if (memberExtent == null)
                        continue;

                    // if the first member didn't have any nodes (also unlikely)
                    if (connectedLinearExtent == null)
                    {
                        // add the member to the end of the connected list
                        connectedMembers.Add(member);

                        // and remove it from the list of disjoint members
                        disjointMembers.Remove(member);

                        // use the extent of this member as the extent of the connected list
                        connectedLinearExtent = memberExtent;

                        // we made a connection in this iteration
                        connectionMade = true;

                        // restart the loop since we modified the list
                        break;
                    }
                    // if the end of the last ordered member matches the start of this member
                    else if (connectedLinearExtent.End.GetDistanceTo(memberExtent.Start) < 1.0)
                    {
                        // add the member to the end of the connected list
                        connectedMembers.Add(member);

                        // and remove it from the list of disjoint members
                        disjointMembers.Remove(member);

                        // update the end of the extent of the connected list
                        connectedLinearExtent = new OsmLinearExtent(connectedLinearExtent.Start, memberExtent.End);

                        // we made a connection this iteration
                        connectionMade = true;

                        // restart the loop since we modified the list
                        break;
                    }
                    // if the end of the member matches the start of the first ordered member
                    else if (memberExtent.End.GetDistanceTo(connectedLinearExtent.Start) < 1.0)
                    {
                        // add the member to the start of the connected list
                        connectedMembers.Insert(0, member);

                        // and remove it from the list of disjoint members
                        disjointMembers.Remove(member);

                        // update the start of the extent of the connected list
                        connectedLinearExtent = new OsmLinearExtent(memberExtent.Start, connectedLinearExtent.End);

                        // we made a connection this iteration
                        connectionMade = true;

                        // restart the loop since we modified the list
                        break;
                    }
                }
            }
            // stop if we couldn't add any members to the connected list or if there are no more disjoint members
            while (connectionMade && disjointMembers.Count > 0);

            // the connected list contains all the members (and if the connected list is not a closed area)
            if (disjointMembers.Count == 0 && connectedLinearExtent?.Start.GetDistanceTo(connectedLinearExtent.End) > 1.0)
            {
                _connectedMembers = connectedMembers;
                return connectedLinearExtent;
            }
            else
                return null;
        }

        internal OsmFeature? GetMember(OsmRelationMember member)
        {
            if (_parent == null)
                throw new Exception("Parent data set reference must not be null.");
            if ("node".Equals(member.Type) && _parent.GetNodeCollection().TryGetValue(member.Ref, out OsmNode? node))
                return node;
            else if ("way".Equals(member.Type) && _parent.GetWayCollection().TryGetValue(member.Ref, out OsmWay? way))
                return way;
            else if ("relation".Equals(member.Type) && _parent.GetRelationCollection().TryGetValue(member.Ref, out OsmRelation? relation))
                return relation;
            else
                return null;
        }

        internal void AddMember(OsmFeature osmFeature)
        {
            if (_parent == null)
                throw new Exception("Parent data set reference must not be null.");

            /*
            // this member must already be part of the same data set as the relation
            // if not, the caller messed up
            if (osmFeature is OsmNode node)
            {
                if (!_parent.GetNodeCollection().ContainsKey(node.Id) || _parent != node.GetParent())
                    throw new Exception("Node is not part of the same collection.");
            } else if (osmFeature is OsmWay way)
            {
                if (!_parent.GetWayCollection().ContainsKey(way.Id) || _parent != way.GetParent())
                    throw new Exception("Way is not part of the same collection.");
            } else if (osmFeature is OsmRelation relation)
            {
                if (!_parent.GetRelationCollection().ContainsKey(relation.Id) || _parent != relation.GetParent())
                    throw new Exception("Relation is not part of the same collection.");
            }
            */

            // this member must be a parent of an OSM data set
            if (osmFeature.GetParent() == null)
                throw new Exception("Member must be part of an OSM data set.");

            OsmRelationMember relationMember = new()
            {
                Type = osmFeature.GetOsmType().ToString(),
                Ref = osmFeature.Id
            };
            relationMember.SetParent(osmFeature.GetParent());

            Members.Add(relationMember);
        }
    }

    /// <remarks/>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class OsmRelationMember
    {
        private XOsmData? _parent = null;

        /// <remarks/>
        [XmlAttribute("type")]
        public string Type { get; set; } = string.Empty;

        /// <remarks/>
        [XmlAttribute("ref")]
        public long Ref { get; set; }

        /// <remarks/>
        [XmlAttribute("role")]
        public string Role { get; set; } = string.Empty;

        public void SetParent(XOsmData? parent)
        {
            _parent = parent;
        }

        public OsmLinearExtent? GetLinearExtent()
        {
            if ("node".Equals(Type))
                return null;

            if (_parent?.GetWayCollection().TryGetValue(Ref, out OsmWay? way) ?? false)
                return way.GetLinearExtent();
            else
                return null;
        }

        internal OsmFeature? GetOsmFeature()
        {
            if ("node".Equals(Type))
                return _parent?.GetNodeCollection()[Ref];
            if ("way".Equals(Type))
                return _parent?.GetWayCollection()[Ref];
            if ("relation".Equals(Type))
                return _parent?.GetRelationCollection()[Ref];
            return null;
        }
    }
}