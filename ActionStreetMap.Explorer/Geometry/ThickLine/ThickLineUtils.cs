﻿using System.Collections.Generic;
using System.Linq;
using ActionStreetMap.Core;
using ActionStreetMap.Infrastructure.Utilities;

namespace ActionStreetMap.Explorer.Geometry.ThickLine
{
    /// <summary> Defines thick line util methods. </summary>
    internal class ThickLineUtils
    {
        #region Line elements in tile

        /// <summary> Returns line elements which only consist of points in tile. </summary>
        public static void GetLineElementsInTile(MapPoint leftBottomCorner, MapPoint rightUpperCorner,
            List<LineElement> elements, List<LineElement> resultElements, IObjectPool objectPool)
        {
            // NOTE Current implementation can filter long lines accidentally. Actually, if line which connects two points 
            // crosses more than 1 tile border we will ignore it as we don't check segment-border intersections

            var pointBuffer = objectPool.NewList<MapPoint>(64);

            MapPoint? lastOutOfTilePoint = null;
            int startPointIndex = 0;
            for (int z = 0; z < elements.Count; z++)  
            {
                var lineElement = elements[z];
                var el = lineElement;
                for (int i = startPointIndex; i < el.Points.Count; i++)
                {
                    var point = el.Points[i];
                    // found point which is not in tile
                    if (!IsPointInTile(point, leftBottomCorner, rightUpperCorner))
                    {
                        // we're already out of tile or it's first
                        if (lastOutOfTilePoint != null || i == 0)
                        {
                            lastOutOfTilePoint = point;
                            continue;
                        }

                        var points = pointBuffer.ToList(); // make copy
                        points.Add(GetIntersectionPoint(el.Points[i - 1], point, leftBottomCorner, rightUpperCorner));

                        resultElements.Add(el);
                        var newEl = new LineElement(el.Points.Skip(i).ToList(), el.Width);
                        el.Points = points;
                        lastOutOfTilePoint = point;
                        pointBuffer.Clear();
                        i = 0;
                        el = newEl;
                        continue;
                    }
                    // return back from outside
                    if (lastOutOfTilePoint != null)
                    {
                        pointBuffer.Add(GetIntersectionPoint(point, lastOutOfTilePoint.Value, leftBottomCorner, rightUpperCorner));
                        lastOutOfTilePoint = null;
                    }
                    pointBuffer.Add(point);
                }
                // if we find any points then we should keep this line element
                if (pointBuffer.Any())
                {
                    // we want to connect two nearby elements which share the same point
                    if (z != elements.Count - 1 &&
                        lineElement.Points[lineElement.Points.Count - 1].Equals(elements[z + 1].Points[0]))
                    {
                        startPointIndex = 1;
                        continue;
                    }
 
                    el.Points = pointBuffer.ToList(); // assume that we create a copy of this array
                    resultElements.Add(el);
                }

                // reuse buffer
                pointBuffer.Clear();
                startPointIndex = 0;
            }

            objectPool.StoreList(pointBuffer);
        }

        private static bool IsPointInTile(MapPoint point, MapPoint minPoint, MapPoint maxPoint)
        {
            return point.X >= minPoint.X && point.X <= maxPoint.X &&
                   point.Y >= minPoint.Y && point.Y <= maxPoint.Y;
        }

        /// <summary> Find intesection point of segment with tile borders </summary>
        private static MapPoint GetIntersectionPoint(MapPoint tilePoint, MapPoint nonTilePoint, MapPoint minPoint,
            MapPoint maxPoint)
        {
            // detect the side of tile which intersects with line between points and find its projection on this side,
            // and tangens of side 
            MapPoint sideProjectionPoint;
            MapPoint axisProjectionPoint;

            bool isVertical = false;
            // right side
            if (nonTilePoint.X > minPoint.X && nonTilePoint.X > maxPoint.X)
                sideProjectionPoint = new MapPoint(maxPoint.X, tilePoint.Y);
            
            // left side
            else if (nonTilePoint.X < minPoint.X && nonTilePoint.X < maxPoint.X)
                sideProjectionPoint = new MapPoint(minPoint.X, tilePoint.Y);
             
           // top side
            else if (nonTilePoint.Y > minPoint.Y && nonTilePoint.Y > maxPoint.Y)
            {
                sideProjectionPoint = new MapPoint(tilePoint.X, maxPoint.Y);
                isVertical = true;
            }
            // bottom side
            else
            {
                sideProjectionPoint = new MapPoint(tilePoint.X, minPoint.Y);
                isVertical = true;
            }

            axisProjectionPoint = new MapPoint(
                isVertical ? tilePoint.X : nonTilePoint.X,
                isVertical ? nonTilePoint.Y : tilePoint.Y);

            // calculate tangents
            float tanAlpha = axisProjectionPoint.DistanceTo(nonTilePoint)/axisProjectionPoint.DistanceTo(tilePoint);

            // calculate distance from side projection point to intersection point
            float distance = tanAlpha*sideProjectionPoint.DistanceTo(tilePoint);

            // should detect sign of offset 
            if (isVertical && tilePoint.X > nonTilePoint.X)
                distance = (-distance);

            if (!isVertical && tilePoint.Y > nonTilePoint.Y)
                distance = (-distance);

            return new MapPoint(
                isVertical ? sideProjectionPoint.X + distance : sideProjectionPoint.X,
                isVertical ? sideProjectionPoint.Y : sideProjectionPoint.Y + distance,
                // NOTE Elevation is set only for flat mode. 
                // However, is it possible that this logic is ignored completely for flat mode?
                tilePoint.Elevation);
        }

        #endregion
    }
}
