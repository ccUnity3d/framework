﻿using System.Collections.Generic;
using ActionStreetMap.Maps.Osm.Entities;
using ActionStreetMap.Maps.Osm.Visitors;

namespace ActionStreetMap.Maps.UnitTests.Osm.Stubs
{
    public class CountableElementVisitor: IElementVisitor
    {
        private readonly List<Element> _elements = new List<Element>();

        public void VisitNode(Node node)
        {
            _elements.Add(node);
        }

        public void VisitRelation(Relation relation)
        {
            _elements.Add(relation);
        }

        public void VisitWay(Way way)
        {
            _elements.Add(way);
        }

        public IList<Element> Elements
        {
            get
            {
                return _elements;
            }
        }
    }
}