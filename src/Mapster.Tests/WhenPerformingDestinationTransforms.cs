using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenPerformingDestinationTransforms
    {
        [TestCleanup]
        public void TestCleanup()
        {
            TypeAdapterConfig.GlobalSettings.Default.Settings.DestinationTransforms.Clear();
        }

        [TestMethod]
        public void Transform_Doesnt_Occur_If_None_Present()
        {
            TypeAdapterConfig<string, string>.Clear();
            TypeAdapterConfig<SimplePoco, SimpleDto>.Clear();

            var source = new SimplePoco { Id = new Guid(), Name = "TestMethod    " };

            var destination = TypeAdapter.Adapt<SimpleDto>(source);

            destination.Name.ShouldBe(source.Name);
        }

        [TestMethod]
        public void Global_Destination_Transform_Is_Applied_To_Class()
        {
            TypeAdapterConfig.GlobalSettings.Default.AddDestinationTransform((string x) => x.Trim());
            TypeAdapterConfig<string, string>.Clear();

            var source = new SimplePoco {Id = new Guid(), Name = "TestMethod"};
            var destination = TypeAdapter.Adapt<SimpleDto>(source);

            destination.Name.ShouldBe("TestMethod");
        }

        [TestMethod]
        public void Adapter_Destination_Transform_Is_Applied_To_Class()
        {
            var config = TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig();
            config.AddDestinationTransform((string x) => x.Trim());
            config.Compile();

            var source = new SimplePoco { Id = new Guid(), Name = "TestMethod    " };
            var destination = TypeAdapter.Adapt<SimplePoco, SimpleDto>(source);

            destination.Name.ShouldBe("TestMethod");
        }

        [TestMethod]
        public void Adapter_Destination_Transform_Collection()
        {
            var config = new TypeAdapterConfig();
            config.Default.AddDestinationTransform((IReadOnlyList<ChildDto> list) => list ?? new List<ChildDto>());

            var source = new CollectionPoco();
            var destination = source.Adapt<CollectionDto>(config);

            destination.Children.ShouldNotBeNull();
        }

        [TestMethod]
        public void Adapter_Destination_Transform_Collection_Generic()
        {
            var config = new TypeAdapterConfig();
            config.Default.AddDestinationTransform(DestinationTransform.EmptyCollectionIfNull);

            var source = new CollectionPoco();
            var destination = source.Adapt<CollectionDto>(config);

            destination.Children.Count.ShouldBe(0);
            destination.Array.Length.ShouldBe(0);
            destination.MultiDimentionalArray.Length.ShouldBe(0);
            destination.ChildDict.Count.ShouldBe(0);
            destination.Set.Count.ShouldBe(0);
        }

        [TestMethod]
        public void Adapter_Destination_Transform_CreateNewIfNull()
        {
            var config = new TypeAdapterConfig();
            config.Default.AddDestinationTransform(DestinationTransform.CreateNewIfNull);

            var source = new CollectionPoco();
            var destination = source.Adapt<CollectionPoco>(config);

            destination.Children.Count.ShouldBe(0);
            destination.ChildDict.Count.ShouldBe(0);
            destination.Set.Count.ShouldBe(0);
        }

        #region TestClasses

        public class SimplePoco
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
        }

        public class SimpleDto
        {
            public Guid Id { get; set; }
            public string Name { get; internal set; }
        }

        public class ChildPoco
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
        }

        public class ChildDto
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
        }

        public class CollectionPoco
        {
            public Guid Id { get; set; }
            public string Name { get; set; }

            public List<ChildPoco> Children { get; set; }
            public int[] Array { get; set; }
            public double[,] MultiDimentionalArray { get; set; }
            public Dictionary<string, ChildPoco> ChildDict { get; set; }
            public HashSet<string> Set { get; set; }
        }

        public class CollectionDto
        {
            public Guid Id { get; set; }
            public string Name { get; set; }

            public IReadOnlyList<ChildDto> Children { get; internal set; }
            public int[] Array { get; set; }
            public double[,] MultiDimentionalArray { get; set; }
            public IReadOnlyDictionary<string, ChildDto> ChildDict { get; set; }
            public ISet<string> Set { get; set; }
        }

        #endregion

    }
}