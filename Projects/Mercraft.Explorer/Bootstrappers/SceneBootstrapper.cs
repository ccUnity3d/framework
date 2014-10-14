﻿using Mercraft.Core.MapCss;
using Mercraft.Core.Scene;
using Mercraft.Core.World;
using Mercraft.Explorer.Scene;
using Mercraft.Explorer.Scene.Builders;
using Mercraft.Explorer.Themes;
using Mercraft.Infrastructure.Bootstrap;
using Mercraft.Infrastructure.Config;
using Mercraft.Infrastructure.Dependencies;
using Mercraft.Models.Buildings;
using Mercraft.Models.Buildings.Facades;
using Mercraft.Models.Buildings.Roofs;
using Mercraft.Models.Roads;
using Mercraft.Models.Terrain;
using Mercraft.Models.Utils;

namespace Mercraft.Explorer.Bootstrappers
{
    public class SceneBootstrapper: BootstrapperPlugin
    {
        private const string ThemeKey = "theme";

        public override string Name { get { return "scene"; } }

        public override bool Run()
        {
            Container.Register(Component.For<WorldManager>().Use<WorldManager>().Singleton());

            Container.Register(Component.For<IResourceProvider>().Use<UnityResourceProvider>().Singleton());

            Container.Register(Component.For<IModelVisitor>().Use<TileModelLoader>().Singleton());

            var themeConfigPath = GlobalConfigSection.GetString(ThemeKey);
            var themeConfig = new ConfigSection(themeConfigPath, FileSystemService);
            // register theme provider
            Container.Register(Component
                .For<IThemeProvider>()
                .Use<ThemeProvider>()
                .Singleton()
                .SetConfig(themeConfig));

            // register stylesheet provider
            Container.Register(Component
                .For<IStylesheetProvider>()
                .Use<StylesheetProvider>()
                .Singleton()
                .SetConfig(GlobalConfigSection));

            // register model builders
            Container.Register(Component.For<IModelBuilder>().Use<BuildingModelBuilder>().Named("building").Singleton());
            Container.Register(Component.For<IModelBuilder>().Use<SphereModelBuilder>().Named("sphere").Singleton());
            Container.Register(Component.For<IModelBuilder>().Use<CylinderModelBuilder>().Named("cylinder").Singleton());
            Container.Register(Component.For<IModelBuilder>().Use<WaterModelBuilder>().Named("water").Singleton());
            Container.Register(Component.For<IModelBuilder>().Use<DetailModelBuilder>().Named("detail").Singleton());
            Container.Register(Component.For<IModelBuilder>().Use<BarrierModelBuilder>().Named("barrier").Singleton());
            Container.Register(Component.For<IModelBuilder>().Use<InfoModelBuilder>().Named("info").Singleton());
            
            // register core behaviours
            // NOTE no standard behaviours so far

            // buildings
            Container.Register(Component.For<IBuildingStyleProvider>().Use<BuildingStyleProvider>().Singleton());
            // facades
            Container.Register(Component.For<IBuildingBuilder>().Use<BuildingBuilder>().Singleton());
            Container.Register(Component.For<IFacadeBuilder>().Use<FlatFacadeBuilder>().Named("flat").Singleton());
            // roofs
            Container.Register(Component.For<IRoofBuilder>().Use<FlatRoofBuilder>().Named("flat").Singleton());
            Container.Register(Component.For<IRoofBuilder>().Use<MansardRoofBuilder>().Named("mansard").Singleton());

            // terrain
            Container.Register(Component.For<ITerrainBuilder>().Use<TerrainBuilder>().Singleton());
            
            // roads
            Container.Register(Component.For<IRoadStyleProvider>().Use<RoadStyleProvider>().Singleton());
            Container.Register(Component.For<IRoadBuilder>().Use<RoadBuilder>().Singleton());

            return true;
        }
    }
}
