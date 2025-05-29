using Mapster.Tests.Classes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System;

namespace Mapster.Tests;

[TestClass]
public class WhenNullPropagationRegression
{
    [TestMethod]
    public void WhenCustomDefaultWorked()
    {
        string customdefault = "42";

        TypeAdapterConfig<SimplePocoToNullPropagation, SimpleDtoToNullPropagation>.NewConfig()
                .Map(dest => dest.AnotherName, src => src.Name).SourceDefaultValue(customdefault)
                .Map(dest => dest.LastModified, src => DateTime.Now)
                .Compile();

        var poco = new SimplePocoToNullPropagation { Id = Guid.NewGuid(), Name = null};

        var dto = poco.Adapt<SimpleDtoToNullPropagation>();

        dto.Id.ShouldBe(poco.Id);
        dto.Name.ShouldBe(null);
        dto.AnotherName.ShouldBe(customdefault);
    }

    [TestMethod]
    public void WhenCustomDefaultMapPathWorked()
    {
        string customdefault = "Default Location";

        TypeAdapterConfig<PocoToPathNullPropagation, DtoToPathNullPropagation>.NewConfig()
                .Map(dest => dest.Location, src => src.Child.Address.Location).SourceDefaultValue(customdefault);

        var poco = new PocoToPathNullPropagation
        {
            Id = Guid.NewGuid(),
            Child = new()
        };

        var dto = poco.Adapt<DtoToPathNullPropagation>();
        dto.Id.ShouldBe(poco.Id);
        dto.Location.ShouldBe(customdefault);
    }

    [TestMethod]
    public void WhenThrowWhenNullWorked()
    {
        TypeAdapterConfig<PocoToPathNullPropagation, DtoToPathNullPropagation>.NewConfig()
                .Map(dest => dest.Location, src => src.Child.Address.Location).ThrowWhenNull();

        var poco = new PocoToPathNullPropagation
        {
            Id = Guid.NewGuid(),
            Child = new()
        };

        Should.Throw<NullReferenceException>(() =>
        {
            var dto = poco.Adapt<DtoToPathNullPropagation>();
        });
    }

    #region Classes

    public class SimplePocoToNullPropagation
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
    }

    public class SimpleDtoToNullPropagation
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public string AnotherName { get; set; }
        public DateTime LastModified { get; set; }
       
    }

    public class PocoToPathNullPropagation
    {
        public Guid Id { get; set; }
        public ChildPocoToNullPropagation? Child { get; set; }
    }

    public class DtoToPathNullPropagation
    {
        public Guid Id { get; set; }
        public Address Address { get; set; }
        public string Location { get; set; }
    }

    public class ChildPocoToNullPropagation
    {
        public AddressNullPropagation? Address { get; set; }
    }
    public class AddressNullPropagation
    {
        public string? Number { get; set; }
        public string? Location { get; set; }
    }

    #endregion Classes
}
