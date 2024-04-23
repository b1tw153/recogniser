using System.Xml;
using System.Xml.Serialization;

namespace recogniser
{

    [Serializable()]
    [XmlType(AnonymousType = true)]
    [XmlRoot(Namespace = "", ElementName = "osmChange", IsNullable = false)]
    public class XOsmChange
    {
        [XmlAttribute("version")]
        public double Version { get; set; } = 0.6;

        [XmlAttribute("generator")]
        public string Generator { get; set; } = Program.PrivateData.UserAgent;

        [XmlElement("create")]
        public ChangeSection? CreateSection { get; set; } = null;

        [XmlElement("modify")]
        public ChangeSection? ModifySection { get; set; } = null;

        public class ChangeSection
        {
            [XmlElement("node")]
            public List<OsmNode> Nodes { get; set; } = new List<OsmNode>();

            [XmlElement("way")]
            public List<OsmWay> Ways { get; set; } = new List<OsmWay>();

            [XmlElement("relation")]
            public List<OsmRelation> Relations { get; set; } = new List<OsmRelation>();
        }

        public bool IsEmpty()
        {
            return CreateSection == null && ModifySection == null;
        }

        public void Create(OsmFeature osmFeature)
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
                        ModifySection.Nodes.Remove(node);
                }

                // if it's not already in the create list
                if (osmFeature is OsmWay way && !CreateSection.Ways.Contains(way))
                {
                    CreateSection.Ways.Add(way);

                    // if we already had it in the modify list, remove it
                    if (ModifySection?.Ways.Contains(way) ?? false)
                        ModifySection.Ways.Remove(way);
                }

                // if it's not already in the create list
                if (osmFeature is OsmRelation relation && !CreateSection.Relations.Contains(relation))
                {
                    CreateSection.Relations.Add(relation);

                    // if we already had it in the modify list, remove it
                    if (ModifySection?.Relations.Contains(relation) ?? false)
                        ModifySection.Relations.Remove(relation);
                }
            }
        }

        public void Modify(OsmFeature osmFeature)
        {
            lock (this)
            {
                ModifySection ??= new ChangeSection();

                // if it's not already in the modify or create list
                if (osmFeature is OsmNode node
                    && !ModifySection.Nodes.Contains(node)
                    && !(CreateSection?.Nodes.Contains(node) ?? false))
                    ModifySection.Nodes.Add(node);

                // if it's not already in the modify or create list
                if (osmFeature is OsmWay way
                    && !ModifySection.Ways.Contains(way)
                    && !(CreateSection?.Ways.Contains(way) ?? false))
                    ModifySection.Ways.Add(way);

                // if it's not already in the modify or create list
                if (osmFeature is OsmRelation relation
                    && !ModifySection.Relations.Contains(relation)
                    && !(CreateSection?.Relations.Contains(relation) ?? false))
                    ModifySection.Relations.Add(relation);
            }
        }

        public string Serialize()
        {
            StringWriter result = new();

            // omit the xml declaration
            XmlWriter xmlWriter = XmlWriter.Create(result, new XmlWriterSettings { OmitXmlDeclaration = true, Indent = true });

            // add a blank namespace to avoid the xmlns:xsi and xmlns:xsd attributes
            XmlSerializerNamespaces nameSpaces = new(new[] { XmlQualifiedName.Empty });

            XmlSerializer osmChangeSerializer = new(typeof(XOsmChange));

            osmChangeSerializer.Serialize(xmlWriter, this, nameSpaces);

            return result.ToString();
        }
    }
}