﻿using System.Collections.Generic;
using ActionStreetMap.Core;
using ActionStreetMap.Unity.Wrappers;

namespace ActionStreetMap.Explorer.Terrain.Flat
{
    /// <summary> Represents area which should be filled with gradient. </summary>
    public class GradientArea
    {
        /// <summary> Gets or sets gradient. </summary>
        public GradientWrapper Gradient { get; set; }

        /// <summary> Gets or sets outer points. </summary>
        public List<MapPoint> Points { get; set; }
    }
}