using GeoCoordinatePortable;
using System;

namespace recogniser
{
	public class GnisRecord : Dictionary<string, string>
	{
		GeoCoordinate? _primary = null;
		GeoCoordinate? _source = null;
		bool? _hasSource = null;

		public string FeatureId { get { return this["FEATURE_ID"]; } }

		public string FeatureName { get { return this["FEATURE_NAME"]; } }

		public string FeatureClass { get { return this["FEATURE_CLASS"]; } }

		public string PrimaryLat { get { return this["PRIM_LAT_DEC"]; } }

        public string PrimaryLon { get { return this["PRIM_LONG_DEC"]; } }

        public string SourceLat { get { return this["SOURCE_LAT_DEC"]; } }

        public string SourceLon { get { return this["SOURCE_LONG_DEC"]; } }

        public string Elevation
		{
			get
			{
				if (TryGetValue("ELEV_IN_M", out string? ele))
					return ele;
				else
					return String.Empty;
			}
		}

        public GeoCoordinate Primary
		{
			get
			{
				if (_primary == null)
					_primary = new(double.TryParse(PrimaryLat, out double primaryLat) ? primaryLat : 0, double.TryParse(PrimaryLon, out double primaryLon) ? primaryLon : 0);
				return _primary;
			}
		}

		public GeoCoordinate Source
		{
			get
			{
				if (_source == null)
					_source = new(double.TryParse(SourceLat, out double sourceLat) ? sourceLat : 0, double.TryParse(SourceLon, out double sourceLon) ? sourceLon : 0);
                return _source;
			}
		}

		public bool HasSource()
		{
			if (_hasSource == null)
                _hasSource = Source.Latitude != 0 && Source.Longitude != 0;
			return _hasSource ?? false;
		}

		public bool HasZeroPrimary()
		{
			return Primary.Latitude == 0 && Primary.Longitude == 0;
		}

		public bool IsHistorical()
		{
			return FeatureName.EndsWith("(historical)");
		}
    }
}