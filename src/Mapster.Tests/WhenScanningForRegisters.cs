using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mapster.Models;
using Mapster.Tests.Classes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenScanningForRegisters
    {
        [TestMethod]
        public void Registers_Are_Found()
        {
            var config = new TypeAdapterConfig();
            IList<IRegister> registers = config.Scan(Assembly.GetExecutingAssembly());
            registers.Count.ShouldBe(2);

            var typeTuples = config.RuleMap.Keys.ToList();

            typeTuples.Any(x => x.Equals(new TypeTuple(typeof (Customer), typeof (CustomerDTO)))).ShouldBeTrue();
            typeTuples.Any(x => x.Equals(new TypeTuple(typeof (Product), typeof (ProductDTO)))).ShouldBeTrue();
            typeTuples.Any(x => x.Equals(new TypeTuple(typeof (Person), typeof (PersonDTO)))).ShouldBeTrue();

            typeTuples.Any(x => x.Equals(new TypeTuple(typeof (PersonDTO), typeof (Person)))).ShouldBeFalse();
        }

        public class TestRegister : IRegister
        {
            public void Register(TypeAdapterConfig config)
            {
                config.NewConfig<Customer, CustomerDTO>();
                config.NewConfig<Product, ProductDTO>();

                config.ForType<Product, ProductDTO>()
                    .Map(dest => dest.Title, src => src.Title + "_AppendSomething!");
            }
        }

        public class TestRegister2 : IRegister
        {
            public void Register(TypeAdapterConfig config)
            {
                config.ForType<Person, PersonDTO>();
            }
        }

    }

}