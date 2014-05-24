﻿
using Mercraft.Core;

namespace Mercraft.Maps.UnitTests
{
    internal static class TestHelper
    {
        public static readonly GeoCoordinate SmallPbfFileCenter = new GeoCoordinate(52.529814, 13.388015);
        public static readonly GeoCoordinate BerlinGeoCenter = new GeoCoordinate(52.529814, 13.388015);

        public const string ConfigAppRootFile = @"..\..\..\..\..\Config\app.config";
        public const string ConfigTestRootFile = "test.config";

        public const string TestPbfFilePath = @"..\..\..\..\Tests\TestAssets\Osm\kempen.osm.pbf";

        //52.53057 13.38687 52.52940 13.39022
        public const string TestXmlFilePath = @"..\..\..\..\Tests\TestAssets\Osm\berlin_house.osm.xml";

        public const string TestBigPbfFilePath = @"..\..\..\..\Tests\TestAssets\Osm\berlin-latest.osm.pbf";

        public const string TestBaseMapcssFile = @"..\..\..\..\Tests\TestAssets\Mapcss\base.mapcss";
        public const string DefaultMapcssFile = @"..\..\..\..\..\Config\mapcss\default.mapcss";
    }
}
