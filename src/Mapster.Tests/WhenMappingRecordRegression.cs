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
            _destinationStruct.X.Equals(_structResult.X).ShouldBeFalse();
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
        public void AdaptToSealtedRecord()
        {
            var _sourceRecord = new TestRecord() { X = 2000 };
            var _destinationSealtedRecord = new TestSealedRecord() { X = 3000 };
            var _RecordResult = _sourceRecord.Adapt(_destinationSealtedRecord);

            _RecordResult.X.ShouldBe(2000);
            object.ReferenceEquals(_destinationSealtedRecord, _RecordResult).ShouldBeFalse();
        }

        [TestMethod]
        public void AdaptToSealtedPositionalRecord()
        {
            var _sourceRecord = new TestRecord() { X = 2000 };
            var _destinationSealtedPositionalRecord = new TestSealedRecordPositional(4000);
            var _RecordResult = _sourceRecord.Adapt(_destinationSealtedPositionalRecord);

            _RecordResult.X.ShouldBe(2000);
            object.ReferenceEquals(_destinationSealtedPositionalRecord, _RecordResult).ShouldBeFalse();
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

            var _sourceEmailUpdate = new UserAccount("123", "123@gmail.com", new DateTime(2023, 9, 24));
            var _updateEmail = new UpdateUser
            {
                Email = "245@gmail.com",
            };

            var config = new TypeAdapterConfig();
            config.ForType<UpdateUser, UserAccount>()
                .IgnoreNullValues(true);

            var _resultEmail = _updateEmail.Adapt(_sourceEmailUpdate, config);

            _source.Id.ShouldBe("123");
            _source.Created.ShouldBe(new DateTime(2023, 9, 24));
            _source.Modified.ShouldBe(new DateTime(2025, 9, 24));
            _source.Email.ShouldBe("123@gmail.com");
            _sourceEmailUpdate.Id.ShouldBe("123");
            _sourceEmailUpdate.Created.ShouldBe(new DateTime(2023, 9, 24));
            _sourceEmailUpdate.Modified.ShouldBe(null);
            _sourceEmailUpdate.Email.ShouldBe("245@gmail.com");

        }

        /// <summary>
        /// https://github.com/MapsterMapper/Mapster/issues/569
        /// </summary>
        [TestMethod]
        public void ImplicitOperatorCurrentWorkFromClass()
        {
            var guid = Guid.NewGuid();
            var pocoWithGuid1 = new PocoWithGuid { Id = guid };
            var pocoWithId2 = new PocoWithId { Id = new Id(guid) };

            var pocoWithId1 = pocoWithGuid1.Adapt<PocoWithId>();
            var pocoWithGuid2 = pocoWithId2.Adapt<PocoWithGuid>();

            pocoWithId1.Id.ToString().Equals(guid.ToString()).ShouldBeTrue();
            pocoWithGuid2.Id.Equals(guid).ShouldBeTrue();

            var _result = pocoWithId1.Adapt(pocoWithGuid2);

            _result.Id.ToString().Equals(guid.ToString()).ShouldBeTrue(); // Guid value transmitted
            object.ReferenceEquals(_result, pocoWithGuid2).ShouldBeTrue(); // Not created new instanse from class pocoWithGuid2
            _result.ShouldBeOfType<PocoWithGuid>();

        }

        [TestMethod]
        public void DetectFakeRecord()
        {
            var _source = new TestClassPublicCtr(200);
            var _destination = new FakeRecord { X = 300 };
            var _result = _source.Adapt(_destination);
            _destination.X.ShouldBe(200);
            object.ReferenceEquals(_destination, _result).ShouldBeTrue();
        }
                
        [TestMethod]
        public void OnlyInlineRecordWorked()
        {
            var _sourcePoco = new InlinePoco501() { MyInt = 1 , MyString = "Hello" };
            var _sourceOnlyInitRecord = new OnlyInitRecord501 { MyInt = 2, MyString = "Hello World" };

            var _resultOnlyinitRecord = _sourcePoco.Adapt<OnlyInitRecord501>();
            var _updateResult = _sourceOnlyInitRecord.Adapt(_resultOnlyinitRecord);

            _resultOnlyinitRecord.MyInt.ShouldBe(1);
            _resultOnlyinitRecord.MyString.ShouldBe("Hello");
            _updateResult.MyInt.ShouldBe(2);
            _updateResult.MyString.ShouldBe("Hello World");
        }

        [TestMethod]
        public void MultyCtorRecordWorked()
        {
            var _sourcePoco = new InlinePoco501() { MyInt = 1, MyString = "Hello" };
            var _sourceMultyCtorRecord = new MultiCtorRecord (2, "Hello World");

            var _resultMultyCtorRecord = _sourcePoco.Adapt<MultiCtorRecord>();
            var _updateResult = _sourceMultyCtorRecord.Adapt(_resultMultyCtorRecord);

            _resultMultyCtorRecord.MyInt.ShouldBe(1);
            _resultMultyCtorRecord.MyString.ShouldBe("Hello");
            _updateResult.MyInt.ShouldBe(2);
            _updateResult.MyString.ShouldBe("Hello World");
        }

        [TestMethod]
        public void MultiCtorAndInlineRecordWorked()
        {
            var _sourcePoco = new MultiCtorAndInlinePoco() { MyInt = 1, MyString = "Hello", MyEmail = "123@gmail.com", InitData="Test"};
            var _sourceMultiCtorAndInline = new MultiCtorAndInlineRecord(2, "Hello World") { InitData = "Worked", MyEmail = "243@gmail.com" };

            var _resultMultiCtorAndInline = _sourcePoco.Adapt<MultiCtorAndInlineRecord>();
            var _updateResult = _sourceMultiCtorAndInline.Adapt(_resultMultiCtorAndInline);

            _resultMultiCtorAndInline.MyInt.ShouldBe(1);
            _resultMultiCtorAndInline.MyString.ShouldBe("Hello");
            _resultMultiCtorAndInline.MyEmail.ShouldBe("123@gmail.com");
            _resultMultiCtorAndInline.InitData.ShouldBe("Test");
            _updateResult.MyInt.ShouldBe(2);
            _updateResult.MyString.ShouldBe("Hello World");
            _updateResult.MyEmail.ShouldBe("243@gmail.com");
            _updateResult.InitData.ShouldBe("Worked");
        }


        [TestMethod]
        public void MappingInterfaceToInterface()
        {
            TypeAdapterConfig<IActivityData, IActivityDataExtentions>
                .ForType()
                .Map(dest => dest.TempLength, src => src.Temp.Length);


            var sourceBase = new SampleInterfaceClsBase
            {
                ActivityData = new SampleActivityData
                {
                    Data = new SampleActivityParsedData
                    {
                        Steps = new List<string> { "A", "B", "C" }
                    },
                    Temp = "Temp data"

                }

            };
            var sourceDerived = new SampleInterfaceClsDerived
            {
                ActivityData = new SampleActivityData
                {
                    Data = new SampleActivityParsedData
                    {
                        Steps = new List<string> { "X", "Y", "Z" }
                    },
                    Temp = "Update Temp data"

                }

            };

            var sourceExt = new SampleInterfaceClsExtentions
            {
                ActivityData = new SampleActivityDataExtentions
                {
                    Data = new SampleActivityParsedData
                    {
                        Steps = new List<string> { "o", "o", "o" }
                    },
                    Temp = "Extentions data",
                    TempLength = "Extentions data".Length

                }

            };

            var TargetBase = sourceBase.Adapt<SampleInterfaceClsBase>();
            var targetDerived = sourceDerived.Adapt<SampleInterfaceClsDerived>();
            var update = targetDerived.Adapt(TargetBase);

            var targetExtention = sourceExt.Adapt<SampleInterfaceClsExtentions>();


            var updExt = targetDerived.Adapt(targetExtention);

            targetDerived.ShouldNotBeNull();
            targetDerived.ShouldSatisfyAllConditions(
                () => targetDerived.ActivityData.ShouldBe(sourceDerived.ActivityData),
                () => update.ActivityData.ShouldBe(targetDerived.ActivityData),

                ()=> updExt.ActivityData.ShouldBe(targetExtention.ActivityData), 
                () => ((SampleActivityDataExtentions)updExt.ActivityData).Temp.ShouldBe(sourceDerived.ActivityData.Temp),
                () => ((SampleActivityDataExtentions)updExt.ActivityData).TempLength.ShouldBe(sourceDerived.ActivityData.Temp.Length),
                // IActivityData interface and all its derivatives  do not provide access to the Data property for all implementations of the SampleActivityData class,
                // so this property will not be changed by mapping 
                () => ((SampleActivityDataExtentions)updExt.ActivityData).Data.ShouldBe(((SampleActivityDataExtentions)targetExtention.ActivityData).Data)

            );

        }



        #region NowNotWorking

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

        #endregion NowNotWorking

    }


    #region TestClasses

    public interface IActivityDataExtentions : IActivityData
    {
        public int TempLength { get; set; }
    }

    public interface IActivityData : IActivityDataBase
    {
        public string Temp { get; set; }
    }

    public interface IActivityDataBase
    {

    }


    public class SampleInterfaceClsExtentions
    {
        public IActivityDataExtentions? ActivityData { get; set; }

        public SampleInterfaceClsExtentions()
        {

        }

        public SampleInterfaceClsExtentions(IActivityDataExtentions data)
        {
            SetActivityData(data);
        }

        public void SetActivityData(IActivityDataExtentions data)
        {
            ActivityData = data;
        }
    }



    public class SampleInterfaceClsBase
    {
        public IActivityDataBase? ActivityData { get; set; }

        public SampleInterfaceClsBase()
        {

        }

        public SampleInterfaceClsBase(IActivityDataBase data)
        {
            SetActivityData(data);
        }

        public void SetActivityData(IActivityDataBase data)
        {
            ActivityData = data;
        }
    }

    public class SampleInterfaceClsDerived
    {
        public IActivityData? ActivityData { get; set; }

        public SampleInterfaceClsDerived()
        {

        }

        public SampleInterfaceClsDerived(IActivityData data)
        {
            SetActivityData(data);
        }

        public void SetActivityData(IActivityData data)
        {
            ActivityData = data;
        }
    }

    public class SampleActivityDataExtentions : IActivityDataExtentions
    {
        public SampleActivityParsedData Data { get; set; }
        public string Temp { get; set; }
        public int TempLength { get; set; }
    }

    public class SampleActivityData : IActivityData
    {
        public SampleActivityParsedData Data { get; set; }
        public string Temp { get; set; }
    }

    public class SampleActivityParsedData
    {
        public List<string> Steps { get; set; } = new List<string>();
    }



    class MultiCtorAndInlinePoco
    {
        public int MyInt { get; set; }
        public string MyString { get; set; }
        public string MyEmail { get; set; }
        public string InitData { get; set; }
    }

    record MultiCtorAndInlineRecord
    {
        public MultiCtorAndInlineRecord(int myInt)
        {
            MyInt = myInt;
        }

        public MultiCtorAndInlineRecord(int myInt, string myString) : this(myInt)
        {
            MyString = myString;
        }

        
        public int MyInt { get; private set; }
        public string MyString { get; private set; }
        public string MyEmail { get; set; }
        public string InitData { get; init; }
    }

    record MultiCtorRecord
    {
        public MultiCtorRecord(int myInt)
        {
            MyInt = myInt;
        }

        public MultiCtorRecord(int myInt, string myString) : this(myInt)
        {
            MyString = myString;
        }

        public int MyInt { get; private set; }
        public string MyString { get; private set; }
    }

    class InlinePoco501
    {
        public int MyInt { get; set; }
        public string MyString { get; set; }
    }

    record OnlyInitRecord501
    {
        public int MyInt { get; init; }
        public string MyString { get; init; }
    }

    class PocoWithGuid
    {
        public Guid Id { get; init; }
    }

    class PocoWithId
    {
        public Id Id { get; init; }
    }

    class Id
    {
        private readonly Guid _guid;
        public Id(Guid id) => _guid = id;

        public static implicit operator Id(Guid value) => new(value);
        public static implicit operator Guid(Id value) => value._guid;

        public override string ToString() => _guid.ToString();
    }

    public class FakeRecord
    {
        protected FakeRecord(FakeRecord fake) { }
        public FakeRecord() { }

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
        public TestClassPublicCtr() { }

        public TestClassPublicCtr(int x)
        {
            X = x;
        }

        public int X { get; set; }
    }

    class TestClassProtectedCtr
    {
        protected TestClassProtectedCtr() { }

        public TestClassProtectedCtr(int x)
        {
            X = x;
        }

        public int X { get; set; }
    }

    class TestClassProtectedCtrPrivateProperty
    {
        protected TestClassProtectedCtrPrivateProperty() { }

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

    /// <summary>
    /// Different Checked Constructor Attribute From Spec
    /// https://learn.microsoft.com/ru-ru/dotnet/csharp/language-reference/proposals/csharp-9.0/records#copy-and-clone-members
    /// </summary>
    sealed record TestSealedRecord()
    {
        public int X { get; set; }
    }

    sealed record TestSealedRecordPositional(int X);










    #endregion TestClasses
}
