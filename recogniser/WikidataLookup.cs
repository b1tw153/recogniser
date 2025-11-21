// <copyright file="WikidataLookup.cs" company="recogniser project contributors">
// Copyright (c) 2025 recogniser project contributors.
// Licensed under the AGPL-3.0-or-later license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Recogniser
{
    using System.Collections.Concurrent;
    using System.Text.Json;
    using System.Text.Json.Nodes;

    internal sealed class WikidataLookup
    {
        private const string BaseUrl = @"https://www.wikidata.org/w/rest.php/wikibase/v1";
        private static readonly ConcurrentDictionary<string, string> WikidataCache = new();

        public static string[] GetGnisIds(XOsmFeature osmFeature)
        {
            // get the wikidata id from the feature if it has one
            string? itemId = osmFeature.GetTagCollection()["wikidata"];

            // if the feature does not have a wikidata id
            if (itemId == null)
            {
                // return an empty list of GNIS IDs
                return Array.Empty<string>();
            }

            // if the results of a previous lookup are in our cache
            if (WikidataCache.TryGetValue(itemId, out string? wikidataGnisIds))
            {
                // return the cached GNIS IDs
                return wikidataGnisIds.Split(";");
            }

            // we have a wikidata id and no cached results
            try
            {
                // build the request url
                string url = $"{BaseUrl}/entities/items/{itemId}/statements?property=P590";

                // build the http request
                using (HttpRequestMessage request = new(HttpMethod.Get, url))
                {
                    request.Headers.Add("User-Agent", Program.PrivateData.UserAgent);
                    request.Headers.Add("Authorization", Program.PrivateData.WikidataAuthorization);

                    // send the http request
                    using (HttpResponseMessage response = Program.HttpClient.Send(request))
                    using (StreamReader streamReader = new(response.Content.ReadAsStream()))
                    {
                        string? content = streamReader.ReadToEnd();
                        response.EnsureSuccessStatusCode();

                        // parse the response data
                        JsonNode? wikidataItem = JsonNode.Parse(content);

                        // if we were able to parse the response data
                        if (wikidataItem != null)
                        {
                            List<string> ids = [];

                            // get the GNIS ID statement
                            JsonNode? wikidataGnisIdStatement = wikidataItem["P590"];

                            // if there is a GNIS ID statement
                            if (wikidataGnisIdStatement != null)
                            {
                                // for each instance of the GNIS ID statement
                                foreach (JsonNode? wikidataGnisIdValue in wikidataGnisIdStatement.AsArray())
                                {
                                    // get the GNIS ID value from the statement
                                    string? wikidataGnisId = wikidataGnisIdValue?["value"]?["content"]?.ToString();

                                    // if there is a GNIS ID value for this statement
                                    if (wikidataGnisId != null)
                                    {
                                        Program.Verbose.WriteLine($"Wikidata GNIS ID: {wikidataGnisId}");

                                        // add the GNIS ID to the list
                                        ids.Add(wikidataGnisId);
                                    }
                                }
                            }

                            // cache all the results
                            WikidataCache[itemId] = string.Join(";", ids);

                            // return all the GNIS IDs
                            return [.. ids];
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // it's nice to know if we're getting errors from wikidata but it's not going to stop us
                Console.Error.WriteLine($"Wikidata error: {e.Message}");
            }

            // unable to parse the response
            return [];
        }
    }
}