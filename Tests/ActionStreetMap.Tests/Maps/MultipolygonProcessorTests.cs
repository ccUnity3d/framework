﻿using System.Collections.Generic;
using System.Linq;
using ActionStreetMap.Core;
using ActionStreetMap.Core.Geometry;
using ActionStreetMap.Core.Tiling.Models;
using ActionStreetMap.Maps.Helpers;
using NUnit.Framework;
using Way = ActionStreetMap.Maps.Entities.Way;
using Relation = ActionStreetMap.Maps.Entities.Relation;
using RelationMember = ActionStreetMap.Maps.Entities.RelationMember;

namespace ActionStreetMap.Tests.Maps
{
    /// <summary>
    ///     Tests relation processing. See http://wiki.openstreetmap.org/wiki/Talk:Relation:multipolygon
    /// </summary>
    [TestFixture]
    public class MultipolygonProcessorTests
    {
        [Test]
        public void CanProcessOneOuterOneInnerAllClosed()
        {
            // ARRANGE
            var relation = new Relation()
            {
                Tags = new Dictionary<string, string>()
                {
                    {"type", "multipolygon"},
                    {"tag","tags"}
                }.ToTags(),
                Members = new List<RelationMember>()
                {
                    CreateRelationWayMember("outer", new Vector2d(0, 0), new Vector2d(3, 5),
                        new Vector2d(7, 3), new Vector2d(8, -1), new Vector2d(3, -4), new Vector2d(0, 0)), 
                    CreateRelationWayMember("inner", new Vector2d(2, 0), new Vector2d(3, 2), 
                        new Vector2d(5, 1), new Vector2d(4, -1), new Vector2d(2, 0))
                }
            };
            var areas = new List<Area>();

            // ACT
            MultipolygonProcessor.FillAreas(relation, areas);

            // ASSERT
            Assert.AreEqual(1, areas.Count);
            var area = areas.First();
            Assert.AreEqual(6, area.Points.Count);
            Assert.AreEqual(1, area.Holes.Count);
            Assert.AreEqual(5, area.Holes.First().Count);
        }

        [Test]
        public void CanProcessOneOuterTwoInnerAllClosed()
        {
            // ARRANGE
            var relation = new Relation()
            {
                Tags = new Dictionary<string, string>()
                {
                    {"type", "multipolygon"},
                    {"tag","tags"}
                }.ToTags(),
                Members = new List<RelationMember>()
                {
                    CreateRelationWayMember("outer", new Vector2d(0, 0), new Vector2d(3, 5),
                        new Vector2d(7, 3), new Vector2d(8, -1), new Vector2d(3, -4), new Vector2d(0, 0)), 

                    CreateRelationWayMember("inner", new Vector2d(2, 1), new Vector2d(3, 3), 
                        new Vector2d(5, 2), new Vector2d(4, 0), new Vector2d(2, 1)),

                    CreateRelationWayMember("inner", new Vector2d(3, -1), new Vector2d(5, -1), 
                        new Vector2d(3, -3), new Vector2d(2, -2), new Vector2d(3, -1))
                }
            };
            var areas = new List<Area>();

            // ACT
            MultipolygonProcessor.FillAreas(relation, areas);

            // ASSERT
            Assert.AreEqual(1, areas.Count);
            var area = areas.First();
            Assert.AreEqual(6, area.Points.Count);
            Assert.AreEqual(2, area.Holes.Count);
            Assert.AreEqual(5, area.Holes.First().Count);
            Assert.AreEqual(5, area.Holes.Last().Count);
            Assert.AreNotEqual(area.Holes.First(), area.Holes.Last());
        }

        [Test]
        public void CanProcessOneOuterNonClosed()
        {
            // ARRANGE
            var relation = new Relation()
            {
                Tags = new Dictionary<string, string>()
                {
                    {"type", "multipolygon"},
                    {"tag", "tags"}
                }.ToTags(),
                Members = new List<RelationMember>()
                {
                    CreateRelationWayMember("outer", new Vector2d(0, 0), new Vector2d(3, 5),
                        new Vector2d(7, 3)),
                    CreateRelationWayMember("outer", new Vector2d(7, 3), new Vector2d(8, -1),
                        new Vector2d(3, -4), new Vector2d(0, 0)),

                }
            };
            var areas = new List<Area>();

            // ACT
            MultipolygonProcessor.FillAreas(relation, areas);

            // ASSERT
            Assert.AreEqual(1, areas.Count);
            var area = areas.First();
            Assert.AreEqual(6, area.Points.Count);
            Assert.AreEqual(0, area.Holes.Count);
        }

        [Test(Description = "Checks whether we can process two separate outer areas")]
        public void CanProcessTwoOuterClosed()
        {
            // ARRANGE
            var relation = new Relation()
            {
                Tags = new Dictionary<string, string>()
                {
                    {"type", "multipolygon"},
                    {"tag","tags"}
                }.ToTags(),
                Members = new List<RelationMember>()
                {
                   CreateRelationWayMember("outer", new Vector2d(0, 0), new Vector2d(3, 5),
                        new Vector2d(7, 3), new Vector2d(8, -1), new Vector2d(3, -4), new Vector2d(0, 0)), 
                   CreateRelationWayMember("outer", new Vector2d(10,-3), new Vector2d(14,-3), new Vector2d(14,-6), 
                   new Vector2d(10, -6), new Vector2d(10,-3)), 
                }
            };
            var areas = new List<Area>();

            // ACT
            MultipolygonProcessor.FillAreas(relation, areas);

            // ASSERT
            Assert.AreEqual(2, areas.Count);
            Assert.AreNotEqual(areas.First(), areas.Last());
            Assert.Greater(areas.First().Points.Count, 0);
            Assert.AreEqual(0, areas.First().Holes.Count);
            Assert.AreEqual(0, areas.Last().Holes.Count);
            Assert.Greater(areas.Last().Points.Count, 0);
        }

        [Test]
        public void CanProcessOneOuterNonClosedAndTwoInnerClosed()
        {
            // ARRANGE
            var relation = new Relation()
            {
                Tags = new Dictionary<string, string>()
                {
                    {"type", "multipolygon"},
                    {"tag","tags"}
                }.ToTags(),
                Members = new List<RelationMember>()
                {
                    CreateRelationWayMember("outer", new Vector2d(0, 0), new Vector2d(3, 5),
                        new Vector2d(7, 3)), 
                    CreateRelationWayMember("outer", new Vector2d(7, 3), new Vector2d(8, -1), 
                      new Vector2d(3, -4), new Vector2d(0, 0)),

                    CreateRelationWayMember("inner", new Vector2d(2, 1), new Vector2d(3, 3), 
                        new Vector2d(5, 2), new Vector2d(4, 0), new Vector2d(2, 1)),
                    CreateRelationWayMember("inner", new Vector2d(3, -1), new Vector2d(5, -1), 
                        new Vector2d(3, -3), new Vector2d(2, -2), new Vector2d(3, -1))
                  
                }
            };
            var areas = new List<Area>();

            // ACT
            MultipolygonProcessor.FillAreas(relation, areas);

            // ASSERT
            Assert.AreEqual(1, areas.Count);
            var area = areas.First();
            Assert.AreEqual(6, area.Points.Count);
            Assert.AreEqual(2, area.Holes.Count);
            Assert.AreEqual(5, area.Holes.First().Count);
            Assert.AreEqual(5, area.Holes.Last().Count);
            Assert.AreNotEqual(area.Holes.First(), area.Holes.Last());
        }

        [Test]
        public void CanProcessMultiplyOuterAndMultiplyInnerArbitaryTypes()
        {
            // see fig.6 http://wiki.openstreetmap.org/wiki/Talk:Relation:multipolygon
            // accessed 22.11.2014

            // ARRANGE
            var relation = new Relation()
            {
                Tags = new Dictionary<string, string>()
                {
                    {"type", "multipolygon"},
                    {"tag","tags"}
                }.ToTags(),
                Members = new List<RelationMember>()
                {
                    CreateRelationWayMember("outer", new Vector2d(1, 5), new Vector2d(8, 4)), 
                    CreateRelationWayMember("outer", new Vector2d(8, 4), new Vector2d(9, -1)), 
                    CreateRelationWayMember("outer", new Vector2d(9, -1), new Vector2d(8, -6), new Vector2d(2, -5)), 
                    CreateRelationWayMember("outer", new Vector2d(2, -5), new Vector2d(0, -3), new Vector2d(1, 5)), 

                    CreateRelationWayMember("inner", new Vector2d(2,1), new Vector2d(3,3), new Vector2d(6,3)),
                    CreateRelationWayMember("inner", new Vector2d(6,3), new Vector2d(4,0), new Vector2d(2,1)),
                    CreateRelationWayMember("inner", new Vector2d(1,-2), new Vector2d(3,-1)),
                    CreateRelationWayMember("inner", new Vector2d(3,-1), new Vector2d(4,-4)),
                    CreateRelationWayMember("inner", new Vector2d(4,-4), new Vector2d(1,-3)),
                    CreateRelationWayMember("inner", new Vector2d(1,-3), new Vector2d(1,-2)),
                    CreateRelationWayMember("inner", new Vector2d(6,-3), new Vector2d(7,-1), 
                        new Vector2d(8, -4), new Vector2d(6,-3)),
                  
                    CreateRelationWayMember("outer", new Vector2d(10, 5), new Vector2d(14,5)), 
                    CreateRelationWayMember("outer", new Vector2d(14,5), new Vector2d(14,-1)), 
                    CreateRelationWayMember("outer", new Vector2d(14,-1), new Vector2d(10,-1)), 
                    CreateRelationWayMember("outer", new Vector2d(10,-1), new Vector2d(10,5)), 

                    CreateRelationWayMember("inner", new Vector2d(11,4), new Vector2d(13,4)), 
                    CreateRelationWayMember("inner", new Vector2d(13,4), new Vector2d(13,0), new Vector2d(11,0)), 
                    CreateRelationWayMember("inner", new Vector2d(11,0), new Vector2d(12,2)), 
                    CreateRelationWayMember("inner", new Vector2d(12,2), new Vector2d(11,4)), 

                    CreateRelationWayMember("outer", new Vector2d(10,-3), new Vector2d(14,-3), new Vector2d(14,-6), 
                        new Vector2d(10, -6), new Vector2d(10,-3)), 
                }
            };
            var areas = new List<Area>();

            // ACT
            MultipolygonProcessor.FillAreas(relation, areas);

            // ASSERT
            Assert.AreEqual(3, areas.Count);
            Assert.AreEqual(3, areas.Single(a => a.Points.Count == 7).Holes.Count);
            Assert.AreEqual(1, areas.Single(a => a.Points.Count == 5 && a.Points[0].Longitude > -2).Holes.Count); // select higher rectangle
            Assert.AreEqual(0, areas.Single(a => a.Points.Count == 5 && a.Points[0].Longitude < -2).Holes.Count); // select lower rectangle
        }

        private RelationMember CreateRelationWayMember(string role, params Vector2d[] points)
        {
            return new RelationMember()
            {
                Member = new Way()
                {
                    Coordinates = points.Select(p => new GeoCoordinate(p.X, p.Y)).ToList()
                },
                Role = role,
            };
        }
    }
}
