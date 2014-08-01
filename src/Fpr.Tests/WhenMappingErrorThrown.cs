using System;
using NUnit.Framework;
using Should;

namespace Fpr.Tests
{

    [TestFixture]
    public class WhenMappingErrorThrown
    {
        public void Setup()
        {
            TypeAdapterConfig<SimplePocoThatThrowsOnGet, SimpleDto>.Clear();
            TypeAdapterConfig<SimplePoco, SimpleDtoThatThrowsOnSet>.Clear();
            TypeAdapterConfig<SimplePoco, SimpleDto>.Clear();
        }

        [Test]
        public void When_Getter_Throws_Exception_Bubbles_Up()
        {
            TypeAdapterConfig<SimplePocoThatThrowsOnGet, SimpleDto>.NewConfig();

            var poco = new SimplePocoThatThrowsOnGet
            {
                Id = new Guid(),
                Name = "TestName"
            };

            Assert.Throws<InvalidOperationException>(() => TypeAdapter.Adapt<SimpleDto>(poco));
        }

        [Test]
        public void When_Setter_Throws_Exception_Bubbles_Up()
        {
            TypeAdapterConfig<SimplePoco, SimpleDtoThatThrowsOnSet>.NewConfig();

            var poco = new SimplePoco
            {
                Id = new Guid(),
                Name = "TestName"
            };

            Assert.Throws<InvalidOperationException>(() => TypeAdapter.Adapt<SimpleDtoThatThrowsOnSet>(poco));
        }

        [Test]
        public void When_Source_Expression_Throws_Exception_Bubbles_Up()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .Map(dest => dest.Amount, src => src.Amount/src.Count);

            var poco = new SimplePoco
            {
                Id = new Guid(),
                Name = "TestName",
                Amount = 100,
                Count = 0
            };

            var exception = Assert.Throws<InvalidOperationException>(() => TypeAdapter.Adapt<SimpleDto>(poco));

            exception.InnerException.ShouldBeType<DivideByZeroException>();
        }

        [Test]
        public void When_Condition_Throws_Exception_Bubbles_Up()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .Map(dest => dest.Amount, src => src.Amount, cond => cond.Amount/cond.Count > 0);

            var poco = new SimplePoco
            {
                Id = new Guid(),
                Name = "TestName",
                Amount = 100,
                Count = 0
            };

            var exception = Assert.Throws<InvalidOperationException>(() => TypeAdapter.Adapt<SimpleDto>(poco));

            exception.InnerException.ShouldBeType<DivideByZeroException>();
        }

        #region Test Classes

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
                    throw new InvalidOperationException("Something bad happened!!!");
                }
            }

            public int Amount { get; set; }

            public int Count { get; set; }
        }


        #endregion
    }
}