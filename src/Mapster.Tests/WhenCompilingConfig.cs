using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Mapster.Tests
{
    [TestFixture]
    public class WhenCompilingConfig
    {
        [TearDown]
        public void TearDown()
        {
            TypeAdapterConfig.GlobalSettings.Clear();
        }

        [Test]
        public void Compile_Success_When_Contain_Collection()
        {
            TypeAdapterConfig<SrcItem, DestItem>.ForType();

            //Validate globally
            TypeAdapterConfig<MainSrc, MainDest>.ForType()
                .Map(d => d.DestItems, s => s.SrcItems);

            TypeAdapterConfig.GlobalSettings.Compile();
        }

        class MainSrc
        {
            public int SrcId { get; set; }
            public List<SrcItem> SrcItems { get; set; }
        }
        class MainDest
        {
            public int DestId { get; set; }
            public List<DestItem> DestItems { get; set; }
        }
        class SrcItem
        {
            public int ItemId { get; set; }
            public string StringData { get; set; }
        }
        class DestItem
        {
            public int ItemId { get; set; }
            public string StringData { get; set; }
        }
    }
}
