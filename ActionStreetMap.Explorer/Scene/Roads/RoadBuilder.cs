﻿using System;
using System.Collections.Generic;
using System.Linq;
using ActionStreetMap.Core.Elevation;
using ActionStreetMap.Core.Scene.Roads;
using ActionStreetMap.Explorer.Scene.Geometry.Polygons;
using ActionStreetMap.Infrastructure.Dependencies;
using ActionStreetMap.Infrastructure.Diagnostic;
using ActionStreetMap.Infrastructure.Reactive;
using ActionStreetMap.Explorer.Scene.Geometry;
using ActionStreetMap.Explorer.Scene.Geometry.ThickLine;
using ActionStreetMap.Explorer.Scene.Utils;
using UnityEngine;
using ActionStreetMap.Infrastructure.Utilities;

namespace ActionStreetMap.Explorer.Scene.Roads
{
    /// <summary>
    ///     Provides the way to build road using road model.
    /// </summary>
    public interface IRoadBuilder
    {
        /// <summary>
        ///     Builds road.
        /// </summary>
        /// <param name="heightMap">Height map.</param>
        /// <param name="road">Road.</param>
        /// <param name="style">Style.</param>
        void BuildRoad(HeightMap heightMap, Road road, RoadStyle style);

        /// <summary>
        ///     Builds road junction.
        /// </summary>
        /// <param name="heightMap">Height map.</param>
        /// <param name="junction">Road junction.</param>
        /// <param name="style">Style.</param>
        void BuildJunction(HeightMap heightMap, RoadJunction junction, RoadStyle style);
    }

    /// <summary>
    ///     Defaul road builder.
    /// </summary>
    public class RoadBuilder : IRoadBuilder
    {
        private const string OsmTag = "osm.road";

        private readonly IResourceProvider _resourceProvider;
        private readonly IObjectPool _objectPool;
        private readonly HeightMapProcessor _heightMapProcessor;

        /// <summary>
        ///     Creates RoadBuilder.
        /// </summary>
        /// <param name="resourceProvider">Resource provider.</param>
        /// <param name="objectPool">Object pool.</param>
        /// <param name="heightMapProcessor">Height map processor.</param>
        [Dependency]
        public RoadBuilder(IResourceProvider resourceProvider, IObjectPool objectPool, 
            HeightMapProcessor heightMapProcessor)
        {
            _resourceProvider = resourceProvider;
            _objectPool = objectPool;
            _heightMapProcessor = heightMapProcessor;
        }

        /// <inheritdoc />
        public void BuildRoad(HeightMap heightMap, Road road, RoadStyle style)
        {
            var lineElements = road.Elements.Select(e => new LineElement(e.Points, e.Width)).ToList();

            var lineBuilder = new ThickLineBuilder(_objectPool, _heightMapProcessor);
            lineBuilder.Build(heightMap, lineElements, (p, t, u) =>
                Scheduler.MainThread.Schedule(() =>
                {
                    CreateRoadMesh(road, style, p, t, u);
                    lineBuilder.Dispose();
                }));
        }

        /// <inheritdoc />
        public void BuildJunction(HeightMap heightMap, RoadJunction junction, RoadStyle style)
        {
            _heightMapProcessor.AdjustPolygon(heightMap, junction.Polygon, junction.Center.Elevation);

            var buffer = _objectPool.NewList<int>();
            var polygonTriangles = Triangulator.Triangulate(junction.Polygon, buffer);
            _objectPool.StoreList(buffer);

            Scheduler.MainThread.Schedule(() => CreateJunctionMesh(junction, style, polygonTriangles));
        }

        /// <summary>
        ///     Creates unity's mesh for road.
        /// </summary>
        /// <param name="road">Road.</param>
        /// <param name="style">Style.</param>
        /// <param name="points">Points.</param>
        /// <param name="triangles">Triangles.</param>
        /// <param name="uv">UV.</param>
        private void CreateRoadMesh(Road road, RoadStyle style,
            List<Vector3> points, List<int> triangles, List<Vector2> uv)
        {           
            Mesh mesh = new Mesh();
            mesh.vertices = points.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uv.ToArray();
            mesh.RecalculateNormals();

            var gameObject = road.GameObject.GetComponent<GameObject>();
            gameObject.isStatic = true;
            var meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;

            gameObject.AddComponent<MeshCollider>();
            gameObject.AddComponent<RoadBehaviors>().Road = road;
            gameObject.tag = OsmTag;

            gameObject.AddComponent<MeshRenderer>()
                .sharedMaterial = _resourceProvider.GetMatertial(style.Path);
        }

        /// <summary>
        ///     Creates unity's mesh for road junction.
        /// </summary>
        /// <param name="junction">Road junction.</param>
        /// <param name="style">Road style.</param>
        /// <param name="polygonTriangles">Polygon triangles.</param>
        private void CreateJunctionMesh(RoadJunction junction, RoadStyle style, int[] polygonTriangles)
        {
            Mesh mesh = new Mesh();
            mesh.vertices = junction.Polygon.Select(p => new Vector3(p.X, p.Elevation, p.Y)).ToArray();
            mesh.triangles = polygonTriangles;
            // TODO
            mesh.uv = junction.Polygon.Select(p => new Vector2()).ToArray();
            mesh.RecalculateNormals();

            var gameObject = junction.GameObject.GetComponent<GameObject>();
            gameObject.isStatic = true;
            var meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;

            gameObject.AddComponent<MeshCollider>();
            gameObject.AddComponent<JunctionBehavior>().Junction = junction;
            gameObject.tag = OsmTag;

            gameObject.AddComponent<MeshRenderer>()
                .sharedMaterial = _resourceProvider.GetMatertial(style.Path);
        }
    }
}