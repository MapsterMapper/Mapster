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
    public class WhenConvertingFromObjects
    {

        #region TestClasses

        public class SimplePoco
        {
            public int Int32 { get; set; }
            public long Int64 { get; set; }
        }

        #endregion

        [TestMethod]
        public void Int32_In_Object_Is_Converted_To_Int64()
        {
            var dictionaryData = new Dictionary<string, object>
                {
                    { "Int32", 32 },
                    { "Int64", 64 }
                };

            var poco = dictionaryData.Adapt<SimplePoco>();
            poco.Int32.ShouldBe(32);
            poco.Int64.ShouldBe(64L);
        }
    }
}
