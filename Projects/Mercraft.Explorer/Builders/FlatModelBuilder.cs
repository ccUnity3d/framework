﻿using System;
using Mercraft.Core;
using Mercraft.Core.Algorithms;
using Mercraft.Core.MapCss.Domain;
using Mercraft.Core.Scene.Models;
using Mercraft.Core.Unity;
using Mercraft.Explorer.Helpers;
using Mercraft.Infrastructure.Dependencies;
using UnityEngine;

namespace Mercraft.Explorer.Builders
{
    public class FlatModelBuilder : ModelBuilder
    {
        [Dependency]
        public FlatModelBuilder(IGameObjectFactory goFactory)
            : base(goFactory)
        {
        }

        public override IGameObject BuildArea(GeoCoordinate center, Rule rule, Area area)
        {
            base.BuildArea(center, rule, area);
            IGameObject gameObjectWrapper = _goFactory.CreateNew(String.Format("Flat {0}", area));
            var gameObject = gameObjectWrapper.GetComponent<GameObject>();

            var floor = rule.GetZIndex();

            var verticies = PolygonHelper.GetVerticies2D(center, area.Points);

            var mesh = new Mesh();
            mesh.vertices = verticies.GetVerticies(floor);
            mesh.uv = verticies.GetUV();
            mesh.triangles = PolygonHelper.GetTriangles(verticies);

            var meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.mesh.Clear();
            meshFilter.mesh = mesh;
            meshFilter.mesh.RecalculateNormals();

            gameObject.AddComponent<MeshRenderer>();
            gameObject.renderer.material = rule.GetMaterial();
            gameObject.renderer.material.color = rule.GetFillColor();

            return gameObjectWrapper;
        }

        public override IGameObject BuildWay(GeoCoordinate center, Rule rule, Way way)
        {
            base.BuildWay(center, rule, way);
            IGameObject gameObjectWrapper = _goFactory.CreateNew(String.Format("Flat {0}", way));
            var gameObject = gameObjectWrapper.GetComponent<GameObject>();
            var zIndex = rule.GetZIndex();

            var points = PolygonHelper.GetVerticies2D(center, way.Points);

            var lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.material = rule.GetMaterial();
            lineRenderer.material.color = rule.GetFillColor();
            lineRenderer.SetVertexCount(points.Length);


            for (int i = 0; i < points.Length; i++)
            {
                lineRenderer.SetPosition(i, new Vector3(points[i].X, zIndex, points[i].Y));
            }

            var width = rule.GetWidth();
            lineRenderer.SetWidth(width, width);

            return gameObjectWrapper;
        }
    }
}