namespace recogniser
{
    public class GnisClassData
    {
        private readonly Dictionary<string, GnisClassAttributes> _data = new();

        public GnisClassData(string gnisClassDataPath)
        {
            using StreamReader gnisClassDataFile = new(gnisClassDataPath);

            string? header = gnisClassDataFile.ReadLine()
                ?? throw new Exception("Unable to read GNIS attributes file header.");

            string[] fieldNames = header.Split(",");

            string? line = gnisClassDataFile.ReadLine();

            while (line != null)
            {
                string[] fieldValues = line.Split(",");

                if (fieldValues.Length != fieldNames.Length)
                {
                    throw new Exception("Improperly formatted line: " + line);
                }

                string gnisClass;
                if ("FEATURE_CLASS".Equals(fieldNames[0]))
                {
                    gnisClass = fieldValues[0];
                }
                else
                {
                    throw new Exception("FEATURE_CLASS is not the first field in the file.");
                }

                GnisClassAttributes gnisClassAttributes = new();

                for (int i = 0; i < fieldNames.Length; i++)
                {
                    gnisClassAttributes.Set(fieldNames[i], fieldValues[i]);
                }

                _data.Add(gnisClass, gnisClassAttributes);

                line = gnisClassDataFile.ReadLine();
            }
        }

        public GnisClassAttributes GetGnisClassAttributes(string featureClass)
        {
            return _data[featureClass];
        }
    }
}