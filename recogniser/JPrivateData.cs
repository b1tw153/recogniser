using System.Text.Json.Serialization;

namespace recogniser
{
    public class JPrivateData
    {
        [JsonPropertyName("user_agent")]
        public string UserAgent { get; set; } = string.Empty;

        [JsonPropertyName("operator_email")]
        public string OperatorEmail { get; set; } = string.Empty;

        [JsonPropertyName("wikidata_authorization")]
        public string WikidataAuthorization { get; set; } = string.Empty;
    }
}