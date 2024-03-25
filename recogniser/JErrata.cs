using System.Text.Json.Serialization;

namespace recogniser
{
	public class JErrata
	{
		[JsonPropertyName("errata")]
		public Erratum[] Errata { get; set; } = Array.Empty<Erratum>();
	}

	public class Erratum
	{
		public static readonly Erratum Empty = new();

		[JsonPropertyName("id")]
		public string Id { get; set; } = string.Empty;

		[JsonPropertyName("skip")]
		public bool Skip { get; set; } = false;

		[JsonPropertyName("substitute")]
		public string Substitute { get; set; } = string.Empty;

		[JsonPropertyName("use")]
		public OsmFeatureRef? Use { get; set; } = null;

		[JsonPropertyName("reason")]
		public string Reason { get; set; } = string.Empty;
	}

	public class OsmFeatureRef
	{
		[JsonPropertyName("type")]
		public string Type { get; set; } = string.Empty;

		[JsonPropertyName("ref")]
		public long Ref { get; set; }
	}
}
