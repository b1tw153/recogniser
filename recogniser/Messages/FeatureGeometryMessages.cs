// <copyright file="FeatureGeometryMessages.cs" company="recogniser project contributors">
// Copyright (c) 2025 recogniser project contributors.
// Licensed under the AGPL-3.0-or-later license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Recogniser.Messages
{
    internal static class FeatureGeometryMessages
    {
        internal static class Josm
        {
            public const string ExtentOffPoint =
                "The location of this feature doesn't match the coordinates in GNIS. Check the GNIS coordinates using aerial imagery and USGS Topo maps. (Editing the feature in JOSM will automatically move it to the location specified in GNIS.) ";

            public const string ExtentOffLine =
                "The extent of this feature doesn't match the coordinates in GNIS. Check the start and end points of the feature using the GNIS coordinates. (Editing the feature in JOSM will automatically add start and end nodes at the locations specified in GNIS.) ";

            public const string StartLocationOff =
                "The start of this feature doesn't match the coordinates in GNIS. Check the start point using the GNIS coordinates. (Editing the feature in JOSM will automatically add a start node at the location specified in GNIS.) ";

            public const string EndLocationOff =
                "The end of this feature doesn't match the coordinates in GNIS. Check the end point using the GNIS coordinates. (Editing the feature in JOSM will automatically add an end node at the location specified in GNIS.) ";
        }

        internal static class Plain
        {
            public const string ExtentOffPoint =
                "The location of this feature doesn't match the coordinates in GNIS. Check the GNIS coordinates using aerial imagery and USGS Topo maps. ";

            public const string ExtentOffLine =
                "The extent of this feature doesn't match the coordinates in GNIS. Check the start and end points of the feature using the GNIS coordinates. ";

            public const string StartLocationOff =
                "The start of this feature doesn't match the coordinates in GNIS. Check the start point using the GNIS coordinates. ";

            public const string EndLocationOff =
                "The end of this feature doesn't match the coordinates in GNIS. Check the end point using the GNIS coordinates. ";
        }
    }
}
