using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenMappingWithGetMethod
    {
        [TestMethod]
        public void Should_Copy_Value_From_Get_Method()
        {
            var poco = new Poco {FirstName = "Foo", LastName = "Bar"};
            var dto = poco.Adapt<Dto>();
            dto.FullName.ShouldBe("Foo Bar");
        }

        [TestMethod]
        public void Should_Ignore_GetType()
        {
            var poco = new Poco { FirstName = "Foo", LastName = "Bar" };
            var dto = poco.Adapt<Dto2>();
            dto.Type.ShouldBeNull();
        }

        [TestMethod]
        public void Allow_GetType_If_Property_Is_Type()
        {
            var poco = new Poco { FirstName = "Foo", LastName = "Bar" };
            var dto = poco.Adapt<Dto3>();
            dto.Type.ShouldBe(typeof(Poco));
        }

        #region TestClasses

        public class Poco
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string GetFullName() => FirstName + " " + LastName;
        }

        public class Dto
        {
            public string FullName { get; set; }
        }

        public class Dto2
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Type { get; set; }
        }

        public class Dto3
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public Type Type { get; set; }
        }

        #endregion
    }
}
