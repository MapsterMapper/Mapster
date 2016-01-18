using System;
using NUnit.Framework;
using Should;

namespace Mapster.Tests
{
    [TestFixture]
    public class WhenUsingRuleBasedSetting
    {

        [Test]
        public void Rule_Base_Testing()
        {
            TypeAdapterConfig.GlobalSettings.When((srcType, destType, mapType) => srcType == destType)
                .Ignore("Id");

            var simplePoco = new SimplePoco {Id = Guid.NewGuid(), Name = "TestName"};

            var dto = TypeAdapter.Adapt<SimplePoco>(simplePoco);

            dto.Id.ShouldEqual(Guid.Empty);
            dto.Name.ShouldEqual(simplePoco.Name);
        }

        #region TestClasses

        public class SimplePoco
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
        }

        #endregion


    }
}