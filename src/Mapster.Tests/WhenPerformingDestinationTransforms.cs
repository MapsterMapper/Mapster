using System;
using System.Collections.Generic;
using NUnit.Framework;
using Should;

namespace Mapster.Tests
{
    [TestFixture]
    public class WhenPerformingDestinationTransforms
    {
        [TearDown]
        public void TearDown()
        {
            TypeAdapterConfig.GlobalSettings.DestinationTransforms.Clear();
        }

        [Test]
        public void Transform_Doesnt_Occur_If_None_Present()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig();
            var source = new SimplePoco { Id = new Guid(), Name = "Test    " };

            var destination = TypeAdapter.Adapt<SimpleDto>(source);

            destination.Name.ShouldEqual(source.Name);
        }

        [Test]
        public void Global_Destination_Transform_Is_Applied_To_Class()
        {
            TypeAdapterConfig.GlobalSettings.DestinationTransforms.Upsert<string>(x => x.Trim());

            var source = new SimplePoco {Id = new Guid(), Name = "Test    "};
            var destination = TypeAdapter.Adapt<SimpleDto>(source);

            destination.Name.ShouldEqual("Test");
        }

        [Test]
        public void Global_Destination_Transform_Is_Applied_To_Primitive()
        {
            TypeAdapterConfig.GlobalSettings.DestinationTransforms.Upsert<string>(x => x.Trim());

            var source ="Test    " ;
            var destination = TypeAdapter.Adapt<string>(source);

            destination.ShouldEqual("Test");
        }

        [Test]
        public void Adapter_Destination_Transform_Is_Applied_To_Class()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .DestinationTransforms.Upsert<string>(x => x.Trim());

            var source = new SimplePoco { Id = new Guid(), Name = "Test    " };
            var destination = TypeAdapter.Adapt<SimpleDto>(source);

            destination.Name.ShouldEqual("Test");
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
            public string Name { get; protected set; }
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

            public IReadOnlyList<ChildDto> Children { get; protected set; }
        }

        #endregion

    }
}