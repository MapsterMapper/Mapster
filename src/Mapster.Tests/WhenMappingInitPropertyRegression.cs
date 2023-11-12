using MapsterMapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System.Reflection;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenMappingInitPropertyRegression
    {
        /// <summary>
        /// https://github.com/MapsterMapper/Mapster/issues/656
        /// </summary>
        [TestMethod]
        public void InitPropertyWorked()
        {
            MapsterTest656 mapster = new MapsterTest656();

            var from = new MyClass656 { MyProperty = 1 };
            var to = mapster.Mapper.Map<DtoClass656>(from);

            to.MyPropertyDto.ShouldBe(1);
        }


    }

    #region TestClasses
    public class MapsterTest656
    {
        public Mapper Mapper { get; }

        TypeAdapterConfig config;

        public MapsterTest656()
        {
            config = new TypeAdapterConfig();
            config.Default.Settings.PreserveReference = true;
            config.Scan(Assembly.GetAssembly(typeof(MapsterTest656)));
            config.Compile();
            Mapper = new Mapper(config);
        }
    }

    internal class MyClass656
    {
        public int MyProperty { get; set; }
    }

    public class DtoClass656
    {
        public int MyPropertyDto { get; init; }
    }
    internal class Register656 : IRegister
    {
        void IRegister.Register(TypeAdapterConfig config)
        {
            config.NewConfig<MyClass656, DtoClass656>()
                .Map(x => x.MyPropertyDto, x => x.MyProperty);
        }
    }

    #endregion TestClasses
}
