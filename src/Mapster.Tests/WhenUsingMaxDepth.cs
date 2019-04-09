using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenUsingMaxDepth
    {
        [TestInitialize]
        public void Setup()
        {
            TypeAdapterConfig.GlobalSettings.Clear();
        }

        [TestMethod]
        public void TestMaxDepth()
        {
            TypeAdapterConfig<Poco, Dto>.NewConfig()
                .MaxDepth(3);

            var poco = new Poco {Id = Guid.NewGuid()};
            poco.Parent = poco;
            poco.Children = new List<Poco> {poco};

            var result = poco.Adapt<Dto>();
            AssertModel(result, poco, 1, 3);
        }

        [TestMethod]
        public void TestMaxDepth_Projection()
        {
            TypeAdapterConfig<Poco, Dto>.NewConfig()
                .MaxDepth(3);

            var poco = new Poco { Id = Guid.NewGuid() };
            poco.Parent = poco;
            poco.Children = new List<Poco> { poco };

            var result = new[] {poco}.AsQueryable().ProjectToType<Dto>();
            AssertModel(result.First(), poco, 1, 3);
        }

        private static void AssertModel(Dto actual, Poco based, int depth, int maxDepth)
        {
            if (depth > maxDepth)
                return;

            actual.Id.ShouldBe(based.Id);
            if (depth == maxDepth)
            {
                actual.Parent.ShouldBeNull();
                actual.Children.ShouldBeNull();
            }
            else
            {
                actual.Parent.Id.ShouldBe(based.Id);
                actual.Children[0].Id.ShouldBe(based.Id);
                AssertModel(actual.Parent, based, depth + 1, maxDepth);
                AssertModel(actual.Children[0], based, depth + 1, maxDepth);
            }
        }

        public class Poco
        {
            public Guid Id { get; set; }
            public Poco Parent { get; set; }
            public List<Poco> Children { get; set; }
        }
        public class Dto
        {
            public Guid Id { get; set; }
            public Dto Parent { get; set; }
            public List<Dto> Children { get; set; }
        }
    }
}
