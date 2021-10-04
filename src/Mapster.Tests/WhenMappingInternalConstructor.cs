using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Mapster.Tests.InternalsVisibleAssembly;
using System;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenMappingInternalConstructor
    {
       
        [TestMethod]
        public void MapToConstructor_InternalVisible()
        {             
            var poco = new Poco { Id = Guid.NewGuid(), Name = "Test", Prop = "Prop", OtherProp = "OtherProp" };
            var dto = TypeAdapter.Adapt<DtoInternal>(poco);

            dto.Id.ShouldBe(poco.Id);
            dto.Name.ShouldBe(poco.Name);
            dto.Age.ShouldBe(-1);
            dto.Prop.ShouldBe(poco.Prop);
        }
      

        [TestMethod]
        public void MapToConstructor_PrivateVisible_ShouldThrow()
        {            
            var poco = new Poco { Id = Guid.NewGuid(), Name = "Test", Prop = "Prop", OtherProp = "OtherProp" };
            var ex = Assert.ThrowsException<CompileException>(() => poco.Adapt<DtoPrivate>());
            Assert.IsInstanceOfType(ex.InnerException, typeof(InvalidOperationException));
            Assert.IsTrue(ex.InnerException.Message.Contains("No default constructor for type"));
        }

        public class Poco
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public string Prop { get; set; }
            public string OtherProp { get; set; }
        }       
    }
}
