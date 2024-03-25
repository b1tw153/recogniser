using GeoCoordinatePortable;
using System.ComponentModel;
using System.Xml.Serialization;

namespace recogniser
{
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]

    public abstract class OsmFeature
    {
        public enum FeatureType
        {
            node,
            way,
            relation
        }

        private OsmTagCollection? _tagCollection = null;

        /// <remarks/>
        [XmlElement("tag")]
        public List<OsmTag> Tags { get; set; } = new List<OsmTag>();

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

        public OsmFeature()
        {
            Timestamp = DateTime.Now;
        }

        public abstract FeatureType GetOsmType();

        public OsmTagCollection GetTagCollection()
        {
            _tagCollection ??= new OsmTagCollection(Tags);
            return _tagCollection;
        }

        // public OsmBounds? Bounds { get; }

        // public long Id { get; }

        public abstract OsmLinearExtent? GetLinearExtent();

        public string GetName()
        {
            OsmTag? nameTag = Tags.Find(tag => "name".Equals(tag.Key));
            string name = nameTag != null ? nameTag.Value : string.Empty;
            return name;
        }

        public void AddTag(OsmTag tag)
        {
            OsmTag? existingTag = Tags.Find(t => t.Key.Equals(tag.Key));
            if (existingTag != null)
                existingTag.Value = tag.Value;
            else
                Tags.Add(tag);
        }

        public void RemoveTag(OsmTagProto tag)
        {
            for (int i = 0; i < Tags.Count; i++)
            {
                if (tag.Matches(Tags[i]))
                {
                    Tags.RemoveAt(i);
                    i--;
                }
            }
        }

        public abstract OsmFeature? AddStartNode(OsmNode startNode);

        public abstract OsmFeature? AddEndNode(OsmNode endNode);

        public abstract List<OsmFeature>? Reverse();

        public abstract void SetParent(XOsmData osmData);

        public abstract XOsmData GetParent();

    }

    public class OsmLinearExtent
    {
        private readonly GeoCoordinate _start;
        private readonly GeoCoordinate _end;

        public OsmLinearExtent(GeoCoordinate start, GeoCoordinate end)
        {
            _start = start;
            _end = end;
        }

        public OsmLinearExtent(double startLat, double startLon, double endLat, double endLon)
        {
            _start = new GeoCoordinate(startLat, startLon);
            _end = new GeoCoordinate(endLat, endLon);
        }

        public GeoCoordinate Start { get { return _start; } }
        public GeoCoordinate End { get { return _end; } }
    }
}