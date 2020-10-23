using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenUsingDestinationValue
    {
        [TestMethod]
        public void MapUsingDestinationValue()
        {
            TypeAdapterConfig.GlobalSettings.Compiler = exp => exp.CompileWithDebugInfo();
            TypeAdapterConfig<Invoice, InvoiceDto>.NewConfig().TwoWays();

            var dto = new InvoiceDto
            {
                Id = 1,
                DocumentNumber = "AA001",
                SupplierCompany = "COM01",
                SupplierName = "Apple",
                Numbers = Enumerable.Range(1, 5).ToList(),
            };
            var poco = dto.Adapt<Invoice>();
            poco.Id.ShouldBe(dto.Id);
            poco.DocumentNumber.ShouldBe("FOO");
            poco.Supplier.Name.ShouldBe(dto.SupplierName);
            poco.Supplier.Company.ShouldBe(dto.SupplierCompany);
            poco.Numbers.ShouldBe(Enumerable.Range(1, 5));
        }

        public class ContractingParty
        {
            public string Name { get; set; }
            public string Company { get; set; }
        }

        public class Invoice
        {
            public long Id { get; set; }

            [UseDestinationValue] 
            public string DocumentNumber { get; set; } = "FOO";

            [UseDestinationValue]
            public ContractingParty Supplier { get; } = new ContractingParty();

            [UseDestinationValue]
            public ICollection<int> Numbers { get; } = new List<int>();
        }

        public class InvoiceDto
        {
            public long Id { get; set; }
            public string DocumentNumber { get; set; }
            public string SupplierName { get; set; }
            public string SupplierCompany { get; set; }
            public IEnumerable<int> Numbers { get; set; }
        }
    }
}
