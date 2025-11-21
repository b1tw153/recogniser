// <copyright file="XOsmFeature.cs" company="recogniser project contributors">
// Copyright (c) 2025 recogniser project contributors.
// Licensed under the AGPL-3.0-or-later license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Recogniser
{
    using System.ComponentModel;
    using System.Xml.Serialization;
    using GeoCoordinatePortable;

    [Serializable]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    internal abstract class XOsmFeature
    {
        private OsmTagCollection? _tagCollection;

        public XOsmFeature()
        {
            Timestamp = DateTime.Now;
        }

        internal enum FeatureType
        {
            node,
            way,
            relation,
        }

        /// <remarks/>
        [XmlElement("tag")]
        public List<OsmTag> Tags { get; set; } = [];

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
            OsmTag? nameTag = Tags.Find(tag => "name".Equals(tag.Key, StringComparison.Ordinal));
            string name = nameTag != null ? nameTag.Value : string.Empty;
            return name;
        }

        public void AddTag(OsmTag tag)
        {
            OsmTag? existingTag = Tags.Find(t => t.Key.Equals(tag.Key, StringComparison.Ordinal));
            if (existingTag != null)
            {
                existingTag.Value = tag.Value;
            }
            else
            {
                Tags.Add(tag);
            }
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

        public abstract XOsmFeature? AddStartNode(OsmNode startNode);

        public abstract XOsmFeature? AddEndNode(OsmNode endNode);

        public abstract List<XOsmFeature>? Reverse();

        public abstract void SetParent(XOsmData osmData);

        public abstract XOsmData GetParent();
    }

    internal class OsmLinearExtent
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