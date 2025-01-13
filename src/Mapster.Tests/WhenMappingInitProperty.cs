using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Mapster.Tests;

[TestClass]
public class WhenMappingInitProperty
{

    #region Tests
    /// <summary>
    /// From Issue #672 
    /// https://github.com/MapsterMapper/Mapster/issues/672
    /// </summary>
    [TestMethod]
    public void WhenMappingToHiddenandNewInitFieldDestination()
    {
        var source = new Source672() { Id = 156};
        var c =  source.Adapt<BDestination>(); 
        var s = source.Adapt(new BDestination());

        ((ADestination)c).Id.ShouldBe(156);
        s.Id.ShouldBe(156);
    }

    [TestMethod]
    public void WhenMappingToHiddenandNewInitFieldWithConstructUsing()
    {
        TypeAdapterConfig<Source672, BDestination>.NewConfig().ConstructUsing(_ => new BDestination());


        var source = new Source672() { Id = 256 };
        var c = source.Adapt<BDestination>();
        var s = source.Adapt(new BDestination());

        ((ADestination)c).Id.ShouldBe(256);
        s.Id.ShouldBe(256);
    }


    #endregion Tests


    #region TestClasses

    class Source672
    {
        public long Id { get; init; }
    }

    class ADestination
    {
        public int Id { get; init; }
    }

    class BDestination : ADestination
    {
        public new long Id { get; init; }
    }


    #endregion TestClasses
}
