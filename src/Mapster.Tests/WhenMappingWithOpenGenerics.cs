using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenMappingWithOpenGenerics
    {
        [TestMethod]
        public void Map_With_Open_Generics()
        {
            TypeAdapterConfig.GlobalSettings.ForType(typeof(GenericPoco<>), typeof(GenericDto<>))
                .Map("value", "Value");

            var poco = new GenericPoco<int> { Value = 123 };
            var dto = poco.Adapt<GenericDto<int>>();
            dto.value.ShouldBe(poco.Value);
        }

        public class GenericPoco<T>
        {
            public T Value { get; set; }
        }

        public class GenericDto<T>
        {
            public T value { get; set; }
        }
            
    }
}
