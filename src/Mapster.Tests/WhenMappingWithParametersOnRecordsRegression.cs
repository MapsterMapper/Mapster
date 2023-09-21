using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System;

namespace Mapster.Tests
{
    /// <summary>
    /// This turned out not to be a bug, but I left the regression test here anyway
    /// </summary>
    [TestClass]
    public class WhenMappingWithParametersOnRecordsRegression
    {
        record Class1(string Title);
        record Class2(int Id, string Title);

        [TestMethod]
        public void TestMapRecordsWithParameters()
        {
            TypeAdapterConfig<Class1, Class2>.NewConfig()
                                        .Map(dest => dest.Id,
                                                src => MapContext.Current.Parameters["Id"]);

            var c1 = new Class1("title1");
            var c2 = c1.BuildAdapter()
                .AddParameters("Id", 1)
                .AdaptToType<Class2>();

            c2.Id.ShouldBe(1);
            c2.Title.ShouldBe("title1");
        }
    }
}