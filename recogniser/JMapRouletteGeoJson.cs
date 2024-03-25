using System.Text.Json.Serialization;

namespace recogniser
{
    public class JMapRouletteGeoJson
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "FeatureCollection";

        [JsonPropertyName("generator")]
        public string Generator { get; set; } = Program.PrivateData.UserAgent;

        [JsonPropertyName("features")]
        public List<GeoJsonFeature> Features { get; set; } = new();

        [JsonPropertyName("cooperativeWork")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Cooperativework? CooperativeWork { get; set; } = null;
    }

    public class Cooperativework
    {
        [JsonPropertyName("meta")]
        public CooperativeworkMeta Meta { get; set; } = new();

        [JsonPropertyName("file")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public CooperativeworkFile? File { get; set; } = null;

        [JsonPropertyName("operations")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<TagFixOperation>? Operations { get; set; } = null;
    }

    public class CooperativeworkMeta
    {
        [JsonPropertyName("version")]
        public int Version { get; set; } = 2;

        [JsonPropertyName("type")]
        public int Type { get; set; } = 2;
    }

    public class CooperativeworkFile
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "xml";

        [JsonPropertyName("format")]
        public string Format { get; set; } = "osc";

        [JsonPropertyName("encoding")]
        public string Encoding { get; set; } = "base64";

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    public class TagFixOperation
    {
        [JsonPropertyName("operationType")]
        public string OperationType { get; set; } = "modifyElement";

        [JsonPropertyName("data")]
        public List<TagFixIndependentOperation> Data { get; set; } = new();

    }

    public class TagFixIndependentOperation
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("operations")]
        public List<TagFixDependentOperation> Operations { get; set; } = new();
    }

    public class TagFixDependentOperation
    {

        [JsonPropertyName("operation")]
        public string Operation { get; set; } = string.Empty;

        // this is either a Dictionary<string,string> or an Array<string>
        [JsonPropertyName("data")]
        public object? Data { get; set; } = null;
    }

    public class GeoJsonFeature
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "Feature";

        [JsonPropertyName("properties")]
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

        [JsonPropertyName("geometry")]
        public object Geometry { get; set; } = new GeoJsonPointGeometry();

        [JsonPropertyName("id")]
        public long Id { get; set; }
    }

    public class GeoJsonPointGeometry
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "Point";

        [JsonPropertyName("coordinates")]
        public double[] Coordinates { get; set; } = Array.Empty<double>();
    }

    public class GeoJsonMultiPointGeometry
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "MultiPoint";

        [JsonPropertyName("coordinates")]
        public double[][] Coordinates { get; set; } = Array.Empty<double[]>();
    }

    public class GeoJsonLineStringGeometry
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "LineString";

        [JsonPropertyName("coordinates")]
        public double[][] Coordinates { get; set; } = Array.Empty<double[]>();
    }

    public class GeoJsonMultiLineStringGeometry
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "MultiLineString";

        [JsonPropertyName("coordinates")]
        public double[][][] Coordinates { get; set; } = Array.Empty<double[][]>();
    }
}