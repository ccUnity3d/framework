﻿using System.Collections.Generic;
using Mercraft.Core.Algorithms;
using Mercraft.Infrastructure.Dependencies;
using Mercraft.Core.Scene;
using UnityEngine;

namespace Mercraft.Core.Tiles
{
    public class TileProvider
    {
        private readonly ISceneBuilder _sceneBuilder;
        private readonly TileSettings _settings;
        private readonly List<Tile> _tiles;

        [Dependency]
        public TileProvider(ISceneBuilder sceneBuilder, TileSettings settings)
        {
            _sceneBuilder = sceneBuilder;
            _settings = settings;
            _tiles = new List<Tile>();
        }

        public Tile GetTile(Vector2 mapPoint, GeoCoordinate relativeNullPoint)
        {
            Tile tile = GetTile(mapPoint);
            if (tile != null)
                return tile;
            
            // need to create tile

            var coordinate = GeoProjection.ToGeoCoordinate(relativeNullPoint, mapPoint);
            
            // TODO invesigate tile size/bbox radius optimal ratio
            var radius = _settings.Size / 2;

            var bbox = BoundingBox.CreateBoundingBox(coordinate, radius);

            var scene = _sceneBuilder.Build(coordinate, bbox);

            tile = new Tile(scene, coordinate, mapPoint, _settings.Size);
            _tiles.Add(tile);

            return tile;
        }

        private Tile GetTile(Vector2 mapPoint)
        {
            // TODO check whether position locates in tile

            // TODO key should be the center of tile!
            //if (_tiles.ContainsKey(mapPoint))
            //    return _tiles[mapPoint];

            return null;
        }
    }
}