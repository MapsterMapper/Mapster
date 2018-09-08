using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Mapster.Tests
{

    [TestClass]
    public class WhenMappingErrorThrown
    {
        [TestInitialize]
        public void Setup()
        {
            TypeAdapterConfig<SimplePocoThatThrowsOnGet, SimpleDto>.Clear();
            TypeAdapterConfig<SimplePoco, SimpleDtoThatThrowsOnSet>.Clear();
            TypeAdapterConfig<SimplePoco, SimpleDto>.Clear();
        }

        [TestMethod]
        public void When_Getter_Throws_Exception_Bubbles_Up()
        {
            TypeAdapterConfig<SimplePocoThatThrowsOnGet, SimpleDto>.NewConfig().Compile();

            var poco = new SimplePocoThatThrowsOnGet
            {
                Id = new Guid(),
                Name = "TestName"
            };

            Should.Throw<InvalidOperationException>(() => TypeAdapter.Adapt<SimplePocoThatThrowsOnGet, SimpleDto>(poco));
        }

        [TestMethod]
        public void When_Setter_Throws_Exception_Bubbles_Up()
        {
            TypeAdapterConfig<SimplePoco, SimpleDtoThatThrowsOnSet>.NewConfig().Compile();

            var poco = new SimplePoco
            {
                Id = new Guid(),
                Name = "TestName"
            };

            Should.Throw<InvalidOperationException>(() => TypeAdapter.Adapt<SimplePoco, SimpleDtoThatThrowsOnSet>(poco));
        }

        [TestMethod]
        public void When_Source_Expression_Throws_Exception_Bubbles_Up()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .Map(dest => dest.Amount, src => src.Amount/src.Count).Compile();

            var poco = new SimplePoco
            {
                Id = new Guid(),
                Name = "TestName",
                Amount = 100,
                Count = 0
            };

            var exception = Should.Throw<DivideByZeroException>(() => TypeAdapter.Adapt<SimplePoco, SimpleDto>(poco));
        }

        [TestMethod]
        public void When_Condition_Throws_Exception_Bubbles_Up()
        {
            TypeAdapterConfig<SimplePoco, SimplePoco>.NewConfig()
                .Map(dest => dest.Amount, src => src.Amount, cond => cond.Amount/cond.Count > 0).Compile();

            var poco = new SimplePoco
            {
                Id = new Guid(),
                Name = "TestName",
                Amount = 100,
                Count = 0
            };

            var exception = Should.Throw<DivideByZeroException>(() => TypeAdapter.Adapt<SimplePoco, SimplePoco>(poco));
        }

        #region TestMethod Classes

        public class SimplePoco
        {
            public Guid Id { get; set; }
            
            public string Name { get; set; }

            public int Amount { get; set; }

            public int Count { get; set; }

        }

        public class SimplePocoThatThrowsOnGet
        {
            private string _name;
            public Guid Id { get; set; }

            public string Name
            {
                get
                {
                    throw new InvalidOperationException("Something bad happened!!!");
                }
                set { _name = value; }
            }

            public int Amount { get; set; }

            public int Count { get; set; }
        }

        public class SimpleDto
        {
            public Guid Id { get; set; }
            public string Name { get; set; }

            public int Amount { get; set; }

            public int Count { get; set; }
        }

        public class SimpleDtoThatThrowsOnSet
        {
            private string _name;
            public Guid Id { get; set; }

            public string Name
            {
                get { return _name; }
                set
                {
                    _name = value;
                    throw new InvalidOperationException("Something bad happened!!!");
                }
            }

            public int Amount { get; set; }

            public int Count { get; set; }
        }


        #endregion
    }
}