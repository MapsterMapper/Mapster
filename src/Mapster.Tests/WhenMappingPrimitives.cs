using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenMappingPrimitives
    {
        [TestMethod]
        public void Integer_Is_Mapped_To_Byte()
        {
            byte b = TypeAdapter.Adapt<int, byte>(5);

            Assert.IsTrue(b == 5);
        }

        [TestMethod]
        public void Byte_Array_Is_Mapped_Correctly()
        {
            string testString = "this is a string that will later be converted to a byte array and other text blah blah blah I'm not sure what else to put here...";

            byte[] array = Encoding.ASCII.GetBytes(testString);

            var resultArray = TypeAdapter.Adapt<byte[], byte[]>(array);

            var resultString = Encoding.ASCII.GetString(resultArray);

            testString.ShouldBe(resultString);
        }

        [TestMethod]
        public void Byte_Array_In_Test_Class_Is_Mapped_Correctly()
        {
            string testString = "this is a string that will later be converted to a byte array and other text blah blah blah I'm not sure what else to put here...";

            var testA = new TestA{Bytes = Encoding.ASCII.GetBytes(testString)};

            var testB = TypeAdapter.Adapt<TestA, TestB>(testA);

            var resultString = Encoding.ASCII.GetString(testB.Bytes);

            testString.ShouldBe(resultString);
        }

        [TestMethod]
        public void ValueType_String_Object_Is_Always_Primitive()
        {
            var sourceDto = new PrimitivePoco
            {
                Id = "test",
                Time = TimeSpan.FromHours(7),
                Obj = new object(),
            };
            var targetDto = TypeAdapter.Adapt<PrimitivePoco, PrimitivePoco>(sourceDto);

            targetDto.Id.ShouldBe(sourceDto.Id);
            targetDto.Time.ShouldBe(sourceDto.Time);
            targetDto.Obj.ShouldBeSameAs(sourceDto.Obj);
        }

        [TestMethod]
        public void Immutable_Class_With_No_Mapping_Should_Error()
        {
            try
            {
                TypeAdapterConfig.GlobalSettings.Clear();
                TypeAdapterConfig<ImmutableA, ImmutableB>.NewConfig().Compile();
                Assert.Fail();
            }
            catch (InvalidOperationException exception)
            {
                exception.ToString().Contains("MapWith").ShouldBeTrue();
            }
        }

        [TestMethod]
        public void Able_To_Map_Immutable_Class_With_MapWith()
        {
            TypeAdapterConfig.GlobalSettings.Clear();
            TypeAdapterConfig<ImmutableA, ImmutableB>.NewConfig()
                .MapWith(src => new ImmutableB(src.Name))
                .Compile();
        }

        [TestMethod]
        public void String_Parse()
        {
            var i = "123".Adapt<int>();
            i.ShouldBe(123);
        }

        public class ImmutableA
        {
            public ImmutableA(string name)
            {
                this.Name = name;
            }

            public string Name { get; }
        }

        public class ImmutableB
        {
            public ImmutableB(string name)
            {
                this.NameX = name;
            }

            public string NameX { get; }
        }

        public class TestA
        {
            public Byte[] Bytes { get; set; }
        }

        public class TestB
        {
            public Byte[] Bytes { get; set; }
        }
        
        public class PrimitivePoco
        {
            public string Id { get; set; }
            public TimeSpan Time { get; set; }
            public object Obj { get; set; }
        }

    }
}
