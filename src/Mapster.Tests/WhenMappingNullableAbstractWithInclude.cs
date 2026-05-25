using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenMappingNullableAbstractWithInclude
    {
        [TestMethod]
        public void Null_Abstract_Property_With_Include_Maps_To_Null()
        {
            var config = new TypeAdapterConfig();
            config.Default.MapToConstructor(true);
            config
                .NewConfig<DtoBase, DomainBase>()
                .Include<DtoDerived, DomainDerived>();

            var dto = new DtoFoo { Field = null };

            var domain = dto.Adapt<DomainFoo>(config);

            domain.Field.ShouldBeNull();
        }

        [TestMethod]
        public void Derived_Abstract_Property_With_Include_Maps_To_Derived_Domain()
        {
            var config = new TypeAdapterConfig();
            config.Default.MapToConstructor(true);
            config
                .NewConfig<DtoBase, DomainBase>()
                .Include<DtoDerived, DomainDerived>();

            var dto = new DtoFoo
            {
                Field = new DtoDerived { Id = "id" },
            };

            var domain = dto.Adapt<DomainFoo>(config);

            domain.Field.ShouldNotBeNull();
            domain.Field.ShouldBeOfType<DomainDerived>();
            ((DomainDerived)domain.Field).Id.ShouldBe("id");
        }

        #region test classes

        public abstract class DtoBase
        {
        }

        public class DtoDerived : DtoBase
        {
            public string Id { get; set; } = null!;
        }

        public class DtoFoo
        {
            public DtoBase? Field { get; set; }
        }

        public abstract class DomainBase
        {
        }

        public class DomainDerived : DomainBase
        {
            public string Id { get; set; } = null!;
        }

        public class DomainFoo
        {
            public DomainBase? Field { get; set; }
        }

        #endregion
    }
}
