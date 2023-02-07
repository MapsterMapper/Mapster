using System;
using System.Reflection;
using Mapster.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Mapster.Tests;

[TestClass]
public class WhenUsingIMapFromWrong
{
    [TestMethod]
    public void TestIMapFrom_WhenMethodImplementedWrong_ShouldRaiseException()
    {
        Should.Throw<Exception>(() =>
        {
            TypeAdapterConfig.GlobalSettings.ScanInheritedTypes(Assembly.GetExecutingAssembly());
        });
    }
}

public class WrongInheritedDestinationModel : IMapFrom<SourceModel>
{
    public string Type { get; set; }
    public int Value { get; set; }

    public void ConfigureMapping()
    {
        TypeAdapterConfig.GlobalSettings
            .NewConfig<SourceModel, InheritedDestinationModel>()
            .Map(dest => dest.Value, _ => DesireValues.Number);
    }
}