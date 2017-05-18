using System;
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
            TypeAdapterConfig.GlobalSettings.EnableDebugging();

            var config = TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig();
            config.AddDestinationTransform((string x) => x.Trim());
            config.Compile();

            var source = new SimplePoco { Id = new Guid(), Name = "TestMethod    " };
            var destination = TypeAdapter.Adapt<SimplePoco, SimpleDto>(source);

            destination.Name.ShouldBe("TestMethod");
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
        }

        public class CollectionDto
        {
            public Guid Id { get; set; }
            public string Name { get; set; }

            public IReadOnlyList<ChildDto> Children { get; internal set; }
        }

        #endregion

    }
}