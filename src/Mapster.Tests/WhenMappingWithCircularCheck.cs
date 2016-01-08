using System.Collections.Generic;
using NUnit.Framework;
using Should;

namespace Mapster.Tests
{
    [Explicit]
    public class WhenMappingWithCircularCheck
    {
        [Test]
        public void Circular_Object_Should_Not_Take_Forever()
        {
            Initialize();

            TypeAdapterConfig<MaxDepthSource, MaxDepthDestination>.NewConfig().CircularReferenceCheck(true);

            var dest = TypeAdapter.Adapt<MaxDepthSource, MaxDepthDestination>(_source);

            dest.ShouldNotBeNull();
            dest.Parent.ShouldBeNull();
            dest.Level.ShouldEqual(1);
            dest.Children[0].Parent.ShouldBeSameAs(dest);
        }

        #region Data

        private MaxDepthSource _source;

        public void Initialize()
        {
            var top = new MaxDepthSource(1);

            top.AddChild(new MaxDepthSource(2));
            top.Children[0].AddChild(new MaxDepthSource(3));
            top.Children[0].AddChild(new MaxDepthSource(3));
            top.Children[0].Children[1].AddChild(new MaxDepthSource(4));
            top.Children[0].Children[1].AddChild(new MaxDepthSource(4));
            top.Children[0].Children[1].AddChild(new MaxDepthSource(4)); 
            top.Children[0].Children[1].AddChild(new MaxDepthSource(4));

            top.AddChild(new MaxDepthSource(2));
            top.Children[1].AddChild(new MaxDepthSource(3));

            top.AddChild(new MaxDepthSource(2));
            top.Children[2].AddChild(new MaxDepthSource(3));

            top.AddChild(new MaxDepthSource(2));
            top.Children[3].AddChild(new MaxDepthSource(3));

            _source = top;
        }


        public class MaxDepthSource
        {
            public int Level { get; set; }

            public IList<MaxDepthSource> Children { get; set; }

            public MaxDepthSource Parent { get; set; }

            public MaxDepthSource(int level)
            {
                Children = new List<MaxDepthSource>();
                Level = level;
            }

            public void AddChild(MaxDepthSource child)
            {
                Children.Add(child);
                child.Parent = this;
            }
        }

        public class MaxDepthDestination
        {
            public int Level { get; set; }

            public IList<MaxDepthDestination> Children { get; set; }

            public MaxDepthDestination Parent { get; set; }
        }


        #endregion
    }
}