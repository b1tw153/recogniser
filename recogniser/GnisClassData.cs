// <copyright file="GnisClassData.cs" company="recogniser project contributors">
// Copyright (c) 2025 recogniser project contributors.
// Licensed under the AGPL-3.0-or-later license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Recogniser
{
    internal class GnisClassData
    {
        private readonly Dictionary<string, GnisClassAttributes> data = [];

        public GnisClassData(string gnisClassDataPath)
        {
            using StreamReader gnisClassDataFile = new(gnisClassDataPath);

            string? header = gnisClassDataFile.ReadLine()
                ?? throw new InvalidDataException("Unable to read GNIS attributes file header.");

            string[] fieldNames = header.Split(",");

            string? line = gnisClassDataFile.ReadLine();

            while (line != null)
            {
                string[] fieldValues = line.Split(",");

                if (fieldValues.Length != fieldNames.Length)
                {
                    throw new InvalidDataException("Improperly formatted line: " + line);
                }

                string gnisClass;
                if ("FEATURE_CLASS".Equals(fieldNames[0], StringComparison.Ordinal))
                {
                    gnisClass = fieldValues[0];
                }
                else
                {
                    throw new InvalidDataException("FEATURE_CLASS is not the first field in the file.");
                }

                GnisClassAttributes gnisClassAttributes = new();

                for (int i = 0; i < fieldNames.Length; i++)
                {
                    gnisClassAttributes.Set(fieldNames[i], fieldValues[i]);
                }

                data.Add(gnisClass, gnisClassAttributes);

                line = gnisClassDataFile.ReadLine();
            }
        }

        public GnisClassAttributes GetGnisClassAttributes(string featureClass)
        {
            return data[featureClass];
        }
    }
}