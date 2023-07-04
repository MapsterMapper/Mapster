using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenPerformingBeforeMapping
    {
        [TestCleanup]
        public void TestCleanup()
        {
            TypeAdapterConfig.GlobalSettings.Clear();
        }

        [TestMethod]
        public void MapToType_Support_CorrectInheritanceOrder()
        {
            TypeAdapterConfig<SimplePocoBase, SimpleDto>.NewConfig()
                .BeforeMapping((src, result) => result.Type = $"{src.Name}!!!");
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .BeforeMapping((src, result) => result.Type += "xxx");

            var poco = new SimplePoco
            {
                Id = Guid.NewGuid(),
                Name = "test",
            };
            var result = TypeAdapter.Adapt<SimpleDto>(poco);
            result.Id.ShouldBe(poco.Id);
            result.Name.ShouldBe(poco.Name);
            result.Type.ShouldBe($"{poco.Name}!!!xxx");
        }

        [TestMethod]
        public void MapToType_Support_Destination_Parameter()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .BeforeMapping((src, result, destination) => result.Name += $"{destination.Name}xxx");

            var poco = new SimplePoco
            {
                Id = Guid.NewGuid(),
                Name = "test",
            };

            // check expression is successfully compiled
            Assert.ThrowsException<NullReferenceException>(() => poco.Adapt<SimpleDto>());
        }

        [TestMethod]
        public void MapToTarget_Support_Destination_Parameter()
        {
            TypeAdapterConfig<IEnumerable<int>, IEnumerable<int>>.NewConfig()
                .BeforeMapping((src, result, destination) =>
                {
                    if (!ReferenceEquals(result, destination) && destination != null && result is ICollection<int> resultCollection)
                    {
                        foreach (var item in destination)
                        {
                            resultCollection.Add(item);
                        }
                    }
                });

            IEnumerable<int> source = new List<int> { 1, 2, 3, };
            IEnumerable<int> destination = new List<int> { 0, };

            var result = source.Adapt(destination);

            destination.ShouldBe(new List<int> { 0, });
            source.ShouldBe(new List<int> { 1, 2, 3, });
            result.ShouldBe(new List<int> { 0, 1, 2, 3, });
        }

        public class SimplePocoBase
        {
            public string Name { get; set; }
        }

        public class SimplePoco : SimplePocoBase
        {
            public Guid Id { get; set; }
        }

        public class SimpleDto
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public string Type { get; set; }
        }
    }
}
