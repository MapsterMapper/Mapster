using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenMappingNullableEnumRegression
    {
        /// <summary>
        /// https://github.com/MapsterMapper/Mapster/issues/640
        /// </summary>
        [TestMethod]
        public void NullEnumToNullClass()
        {
            TypeAdapterConfig<Enum?, KeyValueData?>
                .NewConfig()
                .MapWith(s => s == null ? null : new KeyValueData(s.ToString(), Enums.Manager));

            MyClass myClass = new() { TypeEmployer = MyEnum.User };

            MyClass myClassNull = new() { TypeEmployer = null};


            var _result = myClass?.Adapt<MyDestination?>(); // Work

            var _resultNull = myClassNull.Adapt<MyDestination>(); // Null Not Error When (object)s if (MyEnum)s - NullReferenceException

            _result.TypeEmployer.Key.ShouldBe(MyEnum.User.ToString());

            _resultNull.TypeEmployer.ShouldBeNull();
        }

        /// <summary>
        /// https://github.com/MapsterMapper/Mapster/issues/640
        /// </summary>
        [Ignore] // Will work after RecordType fix 
        [TestMethod]
        public void UpdateNullEnumToClass()
        {
            TypeAdapterConfig<Enum?, KeyValueData?>
                .NewConfig()
                .MapWith(s => s == null ? null : new KeyValueData(s.ToString(), Enums.Manager));
                      
            MyClass myClass = new() { TypeEmployer = MyEnum.User };

            var mDest2 = new MyDestination() { TypeEmployer = new KeyValueData("Admin", null) };
           
            var _MyDestination = myClass?.Adapt<MyDestination?>(); // Work
            var _result = _MyDestination.Adapt(mDest2);

            _result.TypeEmployer.Key.ShouldBe(MyEnum.User.ToString());
        }
    }

    #region TestClasses

    class MyDestination
    {
        public KeyValueData? TypeEmployer { get; set; }
    }

    class MyClass
    {
        public MyEnum? TypeEmployer { get; set; }
    }

    enum MyEnum
    {
        Anonymous = 0,
        User = 2,
    }

    class FakeResourceManager
    {

    }

    class Enums
    {
        protected Enums(string data) {}
        public static FakeResourceManager Manager { get; set; }
    }

    record KeyValueData
    {
        private readonly string? keyHolder; 
        private string? description;

        public KeyValueData(string key, FakeResourceManager manager)
        {
            this.keyHolder = key?.ToString();
            Description = manager?.ToString();
        }

        public string Key
        {
            get => keyHolder!;
            set { } 
        }

        public string? Description
        {
            get => description;
            set => description ??= value;
        }
    }

    #endregion TestClasses
}
