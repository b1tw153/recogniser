// <copyright file="JPrivateData.cs" company="recogniser project contributors">
// Copyright (c) 2025 recogniser project contributors.
// Licensed under the AGPL-3.0-or-later license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Recogniser
{
    using System.Text.Json.Serialization;

    internal sealed class JPrivateData
    {
        [JsonPropertyName("user_agent")]
        public string UserAgent { get; set; } = string.Empty;

        [JsonPropertyName("operator_email")]
        public string OperatorEmail { get; set; } = string.Empty;

        [JsonPropertyName("wikidata_authorization")]
        public string WikidataAuthorization { get; set; } = string.Empty;
    }
}