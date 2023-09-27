using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System;
using System.Collections.Generic;

namespace Mapster.Tests
{
    /// <summary>
    /// Tests for https://github.com/MapsterMapper/Mapster/issues/537
    /// </summary>
    [TestClass]

    public class WhenMappingRecordRegression
    {
        [TestMethod]
        public void AdaptRecordToRecord()
        {
            var _source = new TestRecord() { X = 700 };

            var _destination = new TestRecord() { X = 500 };

            var _result = _source.Adapt(_destination);



            _result.X.ShouldBe(700);

            object.ReferenceEquals(_result, _destination).ShouldBeFalse();
        }

        [TestMethod]
        public void AdaptPositionalRecordToPositionalRecord()
        {
            var _sourcePositional = new TestRecordPositional(600);

            var _destinationPositional = new TestRecordPositional(900);

            var _positionalResult = _sourcePositional.Adapt(_destinationPositional);



            _positionalResult.X.ShouldBe(600);

            object.ReferenceEquals(_destinationPositional, _positionalResult).ShouldBeFalse();

        }

        [TestMethod]
        public void AdaptRecordStructToRecordStruct()
        {
            var _sourceStruct = new TestRecordStruct() { X = 1000 };

            var _destinationStruct = new TestRecordStruct() { X = 800 };

            var _structResult = _sourceStruct.Adapt(_destinationStruct);



            _structResult.X.ShouldBe(1000);

            object.ReferenceEquals(_destinationStruct, _structResult).ShouldBeFalse();


        }


        [TestMethod]
        public void AdaptRecordToClass()
        {
            var _sourсe = new TestRecordPositional(200);

            var _destination = new TestClassProtectedCtr(400);

            var _result = _sourсe.Adapt(_destination);




            _destination.ShouldBeOfType<TestClassProtectedCtr>();

            _destination.X.ShouldBe(200);


            object.ReferenceEquals(_destination, _result).ShouldBeTrue();
        }

        [TestMethod]
        public void AdaptClassToRecord()
        {
            var _sourсe = new TestClassProtectedCtr(200);

            var _destination = new TestRecordPositional(400);

            var _result = _sourсe.Adapt(_destination);




            _destination.ShouldBeOfType<TestRecordPositional>();

            _result.X.ShouldBe(200);


            object.ReferenceEquals(_destination, _result).ShouldBeFalse();
        }

        [TestMethod]
        public void AdaptClassToClassPublicCtrIsNotInstanse()
        {
            var _source = new TestClassPublicCtr(200);

            var _destination = new TestClassPublicCtr(400);

            var _result = _source.Adapt(_destination);




            _destination.ShouldBeOfType<TestClassPublicCtr>();

            _destination.X.ShouldBe(200);


            object.ReferenceEquals(_destination, _result).ShouldBeTrue();
        }


        [TestMethod]
        public void AdaptClassToClassProtectdCtrIsNotInstanse()
        {
            var _source = new TestClassPublicCtr(200);

            var _destination = new TestClassProtectedCtr(400);

            var _result = _source.Adapt(_destination);




            _destination.ShouldBeOfType<TestClassProtectedCtr>();

            _destination.X.ShouldBe(200);


            object.ReferenceEquals(_destination, _result).ShouldBeTrue();
        }


        /// <summary>
        /// https://github.com/MapsterMapper/Mapster/issues/615
        /// </summary>
        [TestMethod]
        public void AdaptClassIncludeStruct()
        {
            TypeAdapterConfig<SourceWithClass, DestinationWithStruct>
                .ForType()
                .Map(x => x.TestStruct, x => x.SourceWithStruct.TestStruct);

            var source = new SourceWithClass
            {
                SourceWithStruct = new SourceWithStruct
                {
                    TestStruct = new TestStruct("A")
                }
            };

            var destination = source.Adapt<DestinationWithStruct>();

            destination.TestStruct.Property.ShouldBe("A");
        }

        /// <summary>
        /// https://github.com/MapsterMapper/Mapster/issues/482
        /// </summary>
        [TestMethod]
        public void AdaptClassToClassFromPrivatePropertyIsNotInstanse()
        {
            var _source = new TestClassPublicCtr(200);

            var _destination = new TestClassProtectedCtrPrivateProperty(400, "Me");

            var _result = _source.Adapt(_destination);




            _destination.ShouldBeOfType<TestClassProtectedCtrPrivateProperty>();

            _destination.X.ShouldBe(200);

            _destination.Name.ShouldBe("Me");


            object.ReferenceEquals(_destination, _result).ShouldBeTrue();
        }


        /// <summary>
        /// https://github.com/MapsterMapper/Mapster/issues/427
        /// </summary>
        [TestMethod]
        public void UpdateNullable()
        {
            var _source = new UserAccount("123", "123@gmail.com", new DateTime(2023, 9, 24));

            var _update = new UpdateUser
            {
                Id = "123",

            };

            var configDate = new TypeAdapterConfig();
            configDate.ForType<UpdateUser, UserAccount>()
                .Map(dest => dest.Modified, src => new DateTime(2025, 9, 24))
                .IgnoreNullValues(true);


            _update.Adapt(_source, configDate);



            var _sourseEmailUpdate = new UserAccount("123", "123@gmail.com", new DateTime(2023, 9, 24));

            var _updateEmail = new UpdateUser
            {
                Email = "245@gmail.com",
            };


            var config = new TypeAdapterConfig();
            config.ForType<UpdateUser, UserAccount>()

                .IgnoreNullValues(true);


            var _resultEmail = _updateEmail.Adapt(_sourseEmailUpdate, config);


            _source.Id.ShouldBe("123");
            _source.Created.ShouldBe(new DateTime(2023, 9, 24));
            _source.Modified.ShouldBe(new DateTime(2025, 9, 24));
            _source.Email.ShouldBe("123@gmail.com");


            _sourseEmailUpdate.Id.ShouldBe("123");
            _sourseEmailUpdate.Created.ShouldBe(new DateTime(2023, 9, 24));
            _sourseEmailUpdate.Modified.ShouldBe(null);
            _sourseEmailUpdate.Email.ShouldBe("245@gmail.com");



        }


        /// <summary>
        /// https://github.com/MapsterMapper/Mapster/issues/524
        /// </summary>
        [TestMethod]
        public void TSousreIsObjectUpdateUseDynamicCast()
        {
            var source = new TestClassPublicCtr { X = 123 };

            var _result = SomemapWithDynamic(source);

            _result.X.ShouldBe(123);

        }

        TestClassPublicCtr SomemapWithDynamic(object source)
        {
            var dest = new TestClassPublicCtr { X = 321 };
            var dest1 = source.Adapt(dest,source.GetType(),dest.GetType());

            return dest;
        }





        #region NowNotWorking

        [TestMethod]
        [Ignore]
        public void DetectFakeRecord()
        {
            var _source = new TestClassPublicCtr(200);

            var _destination = new FakeRecord { X = 300 };

            var _result = _source.Adapt(_destination);


            _destination.X.ShouldBe(200);

            object.ReferenceEquals(_destination, _result).ShouldBeTrue();


        }


        /// <summary>
        /// https://github.com/MapsterMapper/Mapster/issues/430
        /// </summary>
        [Ignore]
        [TestMethod]
        public void CollectionUpdate()
        {
            List<TestClassPublicCtr> sources = new()
           {
                new(541),
                new(234)

            };


            var destination = new List<TestClassPublicCtr>();


            var _result = sources.Adapt(destination);

            destination.Count.ShouldBe(_result.Count);

        }

        /// <summary>
        /// https://github.com/MapsterMapper/Mapster/issues/524 
        /// Not work. Already has a special overload:  
        /// .Adapt(this object source, object destination, Type sourceType, Type destinationType)
        /// </summary>
        [Ignore]
        [TestMethod]
        public void TSousreIsObjectUpdate()
        {
            var source = new TestClassPublicCtr { X = 123 };

            var _result = Somemap(source);

            _result.X.ShouldBe(123);

        }

        TestClassPublicCtr Somemap(object source)
        {
            var dest = new TestClassPublicCtr { X = 321 };
            var dest1 = source.Adapt(dest); // typeof(TSource) always return Type as Object. Need use dynamic or Cast to Runtime Type before Adapt

            return dest;
        }

        #endregion NowNotWorking


    }


    #region TestClasses

    public class FakeRecord
    {
        protected FakeRecord(FakeRecord fake)
        {

        }

        public FakeRecord()
        {

        }

        public int X { get; set; }
    }


    class UserAccount
    {
        public UserAccount(string id, string email, DateTime created)
        {
            Id = id;
            Email = email;
            Created = created;

        }

        protected UserAccount() { }

        public string Id { get; set; }
        public string? Email { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Modified { get; set; }
    }

    class UpdateUser
    {
        public string? Id { get; set; }
        public string? Email { get; set; }

        public DateTime? Created { get; set; }
        public DateTime? Modified { get; set; }

    }


    class DestinationWithStruct
    {
        public TestStruct TestStruct { get; set; }
    }

    class SourceWithClass
    {
        public SourceWithStruct SourceWithStruct { get; set; }
    }

    class SourceWithStruct
    {
        public TestStruct TestStruct { get; set; }
    }

    struct TestStruct
    {
        public string Property { get; }

        public TestStruct(string property) : this()
        {
            Property = property;
        }
    }


    class TestClassPublicCtr
    {
        public TestClassPublicCtr()
        {

        }

        public TestClassPublicCtr(int x)
        {
            X = x;
        }

        public int X { get; set; }
    }


    class TestClassProtectedCtr
    {
        protected TestClassProtectedCtr()
        {

        }

        public TestClassProtectedCtr(int x)
        {
            X = x;
        }

        public int X { get; set; }
    }


    class TestClassProtectedCtrPrivateProperty
    {
        protected TestClassProtectedCtrPrivateProperty()
        {

        }

        public TestClassProtectedCtrPrivateProperty(int x, string name)
        {
            X = x;
            Name = name;
        }

        public int X { get; private set; }

        public string Name { get; private set; }
    }


    record TestRecord()
    {


        public int X { set; get; }
    }

    record TestRecordPositional(int X);

    record struct TestRecordStruct
    {
        public int X { set; get; }
    }

    #endregion TestClasses
}
