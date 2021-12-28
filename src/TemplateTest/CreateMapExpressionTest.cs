using ExpressionDebugger;
using Mapster;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace TemplateTest
{
    [TestClass]
    public class CreateMapExpressionTest
    {
        [TestMethod]
        public void TestCreateMapExpression()
        {
            TypeAdapterConfig.GlobalSettings.SelfContainedCodeGeneration = true;
            var foo = default(Customer);
            var def = new ExpressionDefinitions
            {
                IsStatic = true,
                MethodName = "Map",
                Namespace = "Benchmark",
                TypeName = "CustomerMapper"
            };
            var code = foo.BuildAdapter()
                .CreateMapExpression<CustomerDTO>()
                .ToScript(def);

            Assert.IsNotNull(code);
        }

        [TestMethod]
        public void TestCreateMapToTargetExpression()
        {
            TypeAdapterConfig.GlobalSettings.SelfContainedCodeGeneration = true;
            var foo = default(Customer);
            var def = new ExpressionDefinitions
            {
                IsStatic = true,
                MethodName = "Map",
                Namespace = "Benchmark",
                TypeName = "CustomerMapper"
            };
            var code = foo.BuildAdapter()
                .CreateMapToTargetExpression<CustomerDTO>()
                .ToScript(def);

            Assert.IsNotNull(code);
        }

        [TestMethod]
        public void TestCreateProjectionExpression()
        {
            TypeAdapterConfig.GlobalSettings.SelfContainedCodeGeneration = true;
            var foo = default(Customer);
            var def = new ExpressionDefinitions
            {
                IsStatic = true,
                MethodName = "Map",
                Namespace = "Benchmark",
                TypeName = "CustomerMapper"
            };
            var code = foo.BuildAdapter()
                .CreateProjectionExpression<CustomerDTO>()
                .ToScript(def);

            Assert.IsNotNull(code);
        }
    }

    public class Address
    {
        public int Id { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
    }

    public class AddressDTO
    {
        public int Id { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
    }

    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal? Credit { get; set; }
        public Address Address { get; set; }
        public Address HomeAddress { get; set; }
        public Address[] Addresses { get; set; }
        public ICollection<Address> WorkAddresses { get; set; }
    }

    public class CustomerDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public AddressDTO Address { get; set; }
        public AddressDTO HomeAddress { get; set; }
        public AddressDTO[] Addresses { get; set; }
        public List<AddressDTO> WorkAddresses { get; set; }
        public string AddressCity { get; set; }
    }
}