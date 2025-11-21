// <copyright file="JErrata.cs" company="recogniser project contributors">
// Copyright (c) 2025 recogniser project contributors.
// Licensed under the AGPL-3.0-or-later license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Recogniser
{
    using System.Text.Json.Serialization;

    internal sealed class JErrata
    {
        [JsonPropertyName("errata")]
        public Erratum[] Errata { get; set; } = [];
    }

    internal sealed class Erratum
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

    internal class OsmFeatureRef
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("ref")]
        public long Ref { get; set; }
    }
}
