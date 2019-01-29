using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenCompilingConfig
    {
        [TestInitialize]
        public void Setup()
        {
            TypeAdapterConfig.GlobalSettings.Clear();
        }

        [TestMethod]
        public void Compile_Success_When_Contain_Collection()
        {
            TypeAdapterConfig<SrcItem, DestItem>.ForType();

            //Validate globally
            TypeAdapterConfig<MainSrc, MainDest>.ForType()
                .Map(d => d.DestItems, s => s.SrcItems);

            TypeAdapterConfig.GlobalSettings.Compile();
        }

        public class MainSrc
        {
            public int SrcId { get; set; }
            public List<SrcItem> SrcItems { get; set; }
        }
        public class MainDest
        {
            public int DestId { get; set; }
            public List<DestItem> DestItems { get; set; }
        }
        public class SrcItem
        {
            public int ItemId { get; set; }
            public string StringData { get; set; }
        }
        public class DestItem
        {
            public int ItemId { get; set; }
            public string StringData { get; set; }
        }
    }
}
