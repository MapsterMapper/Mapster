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

        [TestMethod]
        public void Setting_From_OpenGeneric_Has_No_SideEffect()
        {
            var config = new TypeAdapterConfig();
            config
                .NewConfig(typeof(A<>), typeof(B<>))
                .Map("BProperty", "AProperty");

            var a = new A<C> { AProperty = "A" };
            var c = new C { BProperty = "C" };
            var b = a.Adapt<B<C>>(config); // successful mapping
            var cCopy = c.Adapt<C>(config);
        }

        public class GenericPoco<T>
        {
            public T Value { get; set; }
        }

        public class GenericDto<T>
        {
            public T value { get; set; }
        }
         
        class A<T> { public string AProperty { get; set; } }

        class B<T> { public string BProperty { get; set; } }

        class C { public string BProperty { get; set; } }
    }
}
