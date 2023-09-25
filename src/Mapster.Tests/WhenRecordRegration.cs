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

    public class WhenRecordRegress
    {


        [TestMethod]
        public void AdaptRecordToRecord()
        {
            var _sourse = new TestREcord() { X = 700 };

            var _destination = new TestREcord() { X = 500 };

            var _result = _sourse.Adapt(_destination);



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
            var _sourse = new TestRecordPositional(200);

            var _destination = new TestClassProtectedCtr(400);

            var _result = _sourse.Adapt(_destination);




            _destination.ShouldBeOfType<TestClassProtectedCtr>();

            _destination.X.ShouldBe(200);


            object.ReferenceEquals(_destination, _result).ShouldBeTrue();
        }

        [TestMethod]
        public void AdaptClassToRecord()
        {
            var _sourse = new TestClassProtectedCtr(200);

            var _destination = new TestRecordPositional(400);

            var _result = _sourse.Adapt(_destination);




            _destination.ShouldBeOfType<TestRecordPositional>();

            _result.X.ShouldBe(200);


            object.ReferenceEquals(_destination, _result).ShouldBeFalse();
        }





        [TestMethod]
        public void AdaptClassToClassPublicCtr_IsNotInstanse()
        {
            var _sourse = new TestClassPublicCtr(200);

            var _destination = new TestClassPublicCtr(400);

            var _result = _sourse.Adapt(_destination);




            _destination.ShouldBeOfType<TestClassPublicCtr>();

            _destination.X.ShouldBe(200);


            object.ReferenceEquals(_destination, _result).ShouldBeTrue();
        }


        [TestMethod]
        public void AdaptClassToClassProtectdCtr_IsNotInstanse()
        {
            var _sourse = new TestClassPublicCtr(200);

            var _destination = new TestClassProtectedCtr(400);

            var _result = _sourse.Adapt(_destination);




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
        public void AdaptClassToClassFromPrivateProperty_IsNotInstanse()
        {
            var _sourse = new TestClassPublicCtr(200);

            var _destination = new TestClassProtectedCtrPrivateProperty(400, "Me");

            var _result = _sourse.Adapt(_destination);




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
            var _sourse = new UserAccount("123", "123@gmail.com", new DateTime(2023, 9, 24));

            var _update = new UpdateUser
            {
                Id = "123",

            };

            var configDate = new TypeAdapterConfig();
            configDate.ForType<UpdateUser, UserAccount>()
                .Map(dest => dest.Modified, src => new DateTime(2025, 9, 24))
                .IgnoreNullValues(true);


            _update.Adapt(_sourse, configDate);



            var _sourseEmailUpdate = new UserAccount("123", "123@gmail.com", new DateTime(2023, 9, 24));

            var _updateEmail = new UpdateUser
            {
                Email = "245@gmail.com",
            };


            var config = new TypeAdapterConfig();
            config.ForType<UpdateUser, UserAccount>()

                .IgnoreNullValues(true);


            var _resultEmail = _updateEmail.Adapt(_sourseEmailUpdate, config);


            _sourse.Id.ShouldBe("123");
            _sourse.Created.ShouldBe(new DateTime(2023, 9, 24));
            _sourse.Modified.ShouldBe(new DateTime(2025, 9, 24));
            _sourse.Email.ShouldBe("123@gmail.com");


            _sourseEmailUpdate.Id.ShouldBe("123");
            _sourseEmailUpdate.Created.ShouldBe(new DateTime(2023, 9, 24));
            _sourseEmailUpdate.Modified.ShouldBe(null);
            _sourseEmailUpdate.Email.ShouldBe("245@gmail.com");



        }




        #region NowNotWorking

    //    [TestMethod]
    //public void DetectFakeRecord()
    //{
    //    var _sourse = new TestClassPublicCtr(200);

    //    var _destination = new FakeRecord { X = 300 };

    //    var _result = _sourse.Adapt(_destination);
    //}


    /// <summary>
    /// https://github.com/MapsterMapper/Mapster/issues/430
    /// </summary>
    //    [TestMethod]
    //    public void CollectionUpdate()
    //    {
    //        List<TestClassPublicCtr> sources = new()
    //        {
    //            new(541),
    //            new(234)

    //        };


    //        var destination = new List<TestClassPublicCtr>();


    //        var _result = sources.Adapt(destination);

    //        destination.Count.ShouldBe(_result.Count);

    //    }

    //    /// <summary>
    //    /// https://github.com/MapsterMapper/Mapster/issues/524
    //    /// </summary>
    //    [TestMethod]
    //    public void TSousreIsObjectUpdate()
    //    {
    //        var source = new TestClassPublicCtr { X = 123 };

    //        var _result = Somemap(source);

    //        _result.X.ShouldBe(123);

    //    }

    //     TestClassPublicCtr Somemap(object source)
    //    {
    //        var dest = new TestClassPublicCtr { X = 321 };
    //        var dest1 = source.Adapt(dest);

    //        return dest;
    //    }

 #endregion NowNotWorking


 }





   





    #region TestClases

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


    record TestREcord()
    {
        

        public int X { set; get; }
    }

    record TestRecordPositional(int X);

    record struct TestRecordStruct
    {
        public int X { set; get; }
    }


    #endregion TestClases
}
