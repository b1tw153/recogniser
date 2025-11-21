// <copyright file="CommandLineOptions.cs" company="recogniser project contributors">
// Copyright (c) 2025 recogniser project contributors.
// Licensed under the AGPL-3.0-or-later license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Recogniser
{
    /// <summary>
    /// Configuration class to hold all command line arguments.
    /// </summary>
    internal sealed class CommandLineOptions
    {
        public string? GnisFile { get; set; }

        public string? OutputFile { get; set; }

        public string? MapRouletteFile { get; set; }

        public string MapRouletteType { get; set; } = "collaborative";

        public string? OsmChangeFile { get; set; }

        public string? PrivateData { get; set; }

        public string? GnisClassData { get; set; }

        public string? Errata { get; set; }

        public string? OverpassUrl { get; set; }

        public bool Performance { get; set; }

        public bool Progress { get; set; }

        public bool Verbose { get; set; }

        public bool Archived { get; set; }

        public bool SkipMatches { get; set; }

        public bool AlwaysMatchGeometry { get; set; }
    }
}
