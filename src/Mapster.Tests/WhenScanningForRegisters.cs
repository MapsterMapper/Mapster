using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mapster.Models;
using Mapster.Tests.Classes;
using NUnit.Framework;
using Should;

namespace Mapster.Tests
{
	[TestFixture]
	public class WhenScanningForRegisters
	{
		[SetUp]
		public void Setup()
		{
			TypeAdapterConfig.GlobalSettings.Clear();
		}

		[Test]
		public void Registers_Are_Found()
		{
			IList<IRegister> registers = TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());
			registers.Count.ShouldEqual(2);

			var typeTuples = TypeAdapterConfig.GlobalSettings.Dict.Keys.ToList();

			typeTuples.Any(x => x.Equals(new TypeTuple(typeof(Customer), typeof(CustomerDTO)))).ShouldBeTrue();
			typeTuples.Any(x => x.Equals(new TypeTuple(typeof(Product), typeof(ProductDTO)))).ShouldBeTrue();
			typeTuples.Any(x => x.Equals(new TypeTuple(typeof(Person), typeof(PersonDTO)))).ShouldBeTrue();

			typeTuples.Any(x => x.Equals(new TypeTuple(typeof(PersonDTO), typeof(Person)))).ShouldBeFalse();
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