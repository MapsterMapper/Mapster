using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenUsingNonDefaultConstructor
    {

        [TestMethod]
        public void Dest_Calls_Calls_Non_Default_Constructor_With_ConstructUsing()
        {
            TypeAdapterConfig<SimplePoco, SimpleDtoWithDefaultConstructor>.NewConfig()
                .IgnoreNullValues(true)
                .ConstructUsing(src => new SimpleDtoWithDefaultConstructor("unmapped"))
                .Compile();

            var simplePoco = new SimplePoco {Id = Guid.NewGuid(), Name = "TestName"};

            var dto = TypeAdapter.Adapt<SimpleDtoWithDefaultConstructor>(simplePoco);

            dto.Id.ShouldBe(simplePoco.Id);
            dto.Name.ShouldBe(simplePoco.Name);
            dto.Unmapped.ShouldBe("unmapped");
        }

        [TestMethod]
        public void Dest_Calls_Calls_Factory_Method_With_ConstructUsing()
        {
            TypeAdapterConfig<SimplePoco, SimpleDtoWithDefaultConstructor>.NewConfig()
                .IgnoreNullValues(true)
                .ConstructUsing(src => new SimpleDtoWithDefaultConstructor{Unmapped = "unmapped"})
                .Compile();

            var simplePoco = new SimplePoco { Id = Guid.NewGuid(), Name = "TestName" };

            var dto = TypeAdapter.Adapt<SimpleDtoWithDefaultConstructor>(simplePoco);

            dto.Id.ShouldBe(simplePoco.Id);
            dto.Name.ShouldBe(simplePoco.Name);
            dto.Unmapped.ShouldBe("unmapped");
        }

        [TestMethod]
        public void Construct_From_Interface()
        {
            TypeAdapterConfig<SimplePoco, ISimpleDtoWithDefaultConstructor>.NewConfig()
                .IgnoreNullValues(true)
                .ConstructUsing(src => new SimpleDtoWithDefaultConstructor {Unmapped = "unmapped"})
                .Compile();

            var simplePoco = new SimplePoco { Id = Guid.NewGuid(), Name = "TestName" };

            var dto = TypeAdapter.Adapt<ISimpleDtoWithDefaultConstructor>(simplePoco);

            dto.Id.ShouldBe(simplePoco.Id);
            dto.Name.ShouldBe(simplePoco.Name);
            dto.Unmapped.ShouldBe("unmapped");
        }


        #region TestClasses

        public class SimplePoco
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
        }

        public interface ISimpleDtoWithDefaultConstructor
        {
            Guid Id { get; set; }
            string Name { get; set; }
            string Unmapped { get; set; }
        }

        public class SimpleDtoWithDefaultConstructor : ISimpleDtoWithDefaultConstructor
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