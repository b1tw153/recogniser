namespace Recogniser
{
    using System.Xml;
    using System.Xml.Serialization;

    [Serializable]
    [XmlType(AnonymousType = true)]
    [XmlRoot(Namespace = "", ElementName = "osmChange", IsNullable = false)]
    internal class XOsmChange
    {
        [XmlAttribute("version")]
        public double Version { get; set; } = 0.6;

        [XmlAttribute("generator")]
        public string Generator { get; set; } = Program.PrivateData.UserAgent;

        [XmlElement("create")]
        public ChangeSection? CreateSection { get; set; } = null;

        [XmlElement("modify")]
        public ChangeSection? ModifySection { get; set; } = null;

        internal class ChangeSection
        {
            [XmlElement("node")]
            public List<OsmNode> Nodes { get; set; } = [];

            [XmlElement("way")]
            public List<OsmWay> Ways { get; set; } = [];

            [XmlElement("relation")]
            public List<OsmRelation> Relations { get; set; } = [];
        }

        public bool IsEmpty()
        {
            return CreateSection == null && ModifySection == null;
        }

        public void Create(XOsmFeature osmFeature)
        {
            lock (this)
            {
                CreateSection ??= new ChangeSection();

                // if it's not already in the create list
                if (osmFeature is OsmNode node && !CreateSection.Nodes.Contains(node))
                {
                    CreateSection.Nodes.Add(node);

                    // if we already had it in the modify list, remove it
                    if (ModifySection?.Nodes.Contains(node) ?? false)
                    {
                        ModifySection.Nodes.Remove(node);
                    }
                }

                // if it's not already in the create list
                if (osmFeature is OsmWay way && !CreateSection.Ways.Contains(way))
                {
                    CreateSection.Ways.Add(way);

                    // if we already had it in the modify list, remove it
                    if (ModifySection?.Ways.Contains(way) ?? false)
                    {
                        ModifySection.Ways.Remove(way);
                    }
                }

                // if it's not already in the create list
                if (osmFeature is OsmRelation relation && !CreateSection.Relations.Contains(relation))
                {
                    CreateSection.Relations.Add(relation);

                    // if we already had it in the modify list, remove it
                    if (ModifySection?.Relations.Contains(relation) ?? false)
                    {
                        ModifySection.Relations.Remove(relation);
                    }
                }
            }
        }

        public void Modify(XOsmFeature osmFeature)
        {
            lock (this)
            {
                ModifySection ??= new ChangeSection();

                // if it's not already in the modify or create list
                if (osmFeature is OsmNode node
                    && !ModifySection.Nodes.Contains(node)
                    && !(CreateSection?.Nodes.Contains(node) ?? false))
                {
                    ModifySection.Nodes.Add(node);
                }

                // if it's not already in the modify or create list
                if (osmFeature is OsmWay way
                    && !ModifySection.Ways.Contains(way)
                    && !(CreateSection?.Ways.Contains(way) ?? false))
                {
                    ModifySection.Ways.Add(way);
                }

                // if it's not already in the modify or create list
                if (osmFeature is OsmRelation relation
                    && !ModifySection.Relations.Contains(relation)
                    && !(CreateSection?.Relations.Contains(relation) ?? false))
                {
                    ModifySection.Relations.Add(relation);
                }
            }
        }

        public string Serialize()
        {
            StringWriter result = new();

            // omit the xml declaration
            using XmlWriter xmlWriter = XmlWriter.Create(result, new XmlWriterSettings { OmitXmlDeclaration = true, Indent = true });

            // add a blank namespace to avoid the xmlns:xsi and xmlns:xsd attributes
            XmlSerializerNamespaces nameSpaces = new([XmlQualifiedName.Empty]);

            XmlSerializer osmChangeSerializer = new(typeof(XOsmChange));

            osmChangeSerializer.Serialize(xmlWriter, this, nameSpaces);

            return result.ToString();
        }
    }
}