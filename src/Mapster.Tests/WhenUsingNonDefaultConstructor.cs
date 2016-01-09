using System;
using NUnit.Framework;
using Should;

namespace Mapster.Tests
{
    [TestFixture]
    public class WhenUsingNonDefaultConstructor
    {

        [Test]
        public void Dest_Calls_Calls_Non_Default_Constructor_With_ConstructUsing()
        {
            TypeAdapterConfig<SimplePoco, SimpleDtoWithDefaultConstructor>.NewConfig()
                .IgnoreNullValues(true)
                .ConstructUsing(() => new SimpleDtoWithDefaultConstructor("unmapped"))
                .Compile();

            var simplePoco = new SimplePoco {Id = Guid.NewGuid(), Name = "TestName"};

            var dto = TypeAdapter.Adapt<SimpleDtoWithDefaultConstructor>(simplePoco);

            dto.Id.ShouldEqual(simplePoco.Id);
            dto.Name.ShouldEqual(simplePoco.Name);
            dto.Unmapped.ShouldEqual("unmapped");
        }

        [Test]
        public void Dest_Calls_Calls_Factory_Method_With_ConstructUsing()
        {
            TypeAdapterConfig<SimplePoco, SimpleDtoWithDefaultConstructor>.NewConfig()
                .IgnoreNullValues(true)
                .ConstructUsing(() => new SimpleDtoWithDefaultConstructor{Unmapped = "unmapped"})
                .Compile();

            var simplePoco = new SimplePoco { Id = Guid.NewGuid(), Name = "TestName" };

            var dto = TypeAdapter.Adapt<SimpleDtoWithDefaultConstructor>(simplePoco);

            dto.Id.ShouldEqual(simplePoco.Id);
            dto.Name.ShouldEqual(simplePoco.Name);
            dto.Unmapped.ShouldEqual("unmapped");
        }


        #region TestClasses

        public class SimplePoco
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
        }

        public class SimpleDtoWithDefaultConstructor
        {
            public SimpleDtoWithDefaultConstructor()
            {
            }

            public SimpleDtoWithDefaultConstructor(string unmapped)
            {
                Unmapped = unmapped;
            }

            public Guid Id { get; set; }
            public string Name { get; set; }

            public string Unmapped { get; set; }
        }

        #endregion


    }
}