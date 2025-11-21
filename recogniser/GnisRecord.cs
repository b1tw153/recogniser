// <copyright file="GnisRecord.cs" company="recogniser project contributors">
// Copyright (c) 2025 recogniser project contributors.
// Licensed under the AGPL-3.0-or-later license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Recogniser
{
    using GeoCoordinatePortable;

    internal sealed class GnisRecord : Dictionary<string, string>
    {
        private GeoCoordinate? primary;
        private GeoCoordinate? source;
        private bool? hasSource;

        public string FeatureId => this["FEATURE_ID"];

        public string FeatureName => this["FEATURE_NAME"];

        public string FeatureClass => this["FEATURE_CLASS"];

        public string PrimaryLat => this["PRIM_LAT_DEC"];

        public string PrimaryLon => this["PRIM_LONG_DEC"];

        public string SourceLat => this["SOURCE_LAT_DEC"];

        public string SourceLon => this["SOURCE_LONG_DEC"];

        public string Elevation
        {
            get
            {
                if (TryGetValue("ELEV_IN_M", out string? ele))
                {
                    return ele;
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        public GeoCoordinate Primary
        {
            get
            {
                if (primary == null)
                {
                    primary = new(double.TryParse(PrimaryLat, out double primaryLat) ? primaryLat : 0, double.TryParse(PrimaryLon, out double primaryLon) ? primaryLon : 0);
                }

                return primary;
            }
        }

        public GeoCoordinate Source
        {
            get
            {
                if (source == null)
                {
                    source = new(double.TryParse(SourceLat, out double sourceLat) ? sourceLat : 0, double.TryParse(SourceLon, out double sourceLon) ? sourceLon : 0);
                }

                return source;
            }
        }

        public bool HasSource()
        {
            hasSource ??= Source.Latitude != 0 && Source.Longitude != 0;

            return hasSource ?? false;
        }

        public bool HasZeroPrimary()
        {
            return Primary.Latitude == 0 && Primary.Longitude == 0;
        }

        public bool IsHistorical()
        {
            return FeatureName.EndsWith("(historical)", StringComparison.Ordinal);
        }
    }
}