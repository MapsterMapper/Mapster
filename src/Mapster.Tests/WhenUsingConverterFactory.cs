using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenUsingConverterFactory
    {

        [TestMethod]
        public void Custom_Mapping_From_String_To_Char_Array()
        {
            TypeAdapterConfig<string, char[]>.NewConfig()
                .MapWith(str => str.ToCharArray());

            var chars = TypeAdapter.Adapt<char[]>("Hello");

            chars.Length.ShouldBe(5);
            chars[0].ShouldBe('H');
            chars[1].ShouldBe('e');
            chars[2].ShouldBe('l');
            chars[3].ShouldBe('l');
            chars[4].ShouldBe('o');
        }
    }
}