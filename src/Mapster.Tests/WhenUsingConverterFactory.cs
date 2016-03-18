using System;
using NUnit.Framework;
using Shouldly;

namespace Mapster.Tests
{
    [TestFixture]
    public class WhenUsingConverterFactory
    {

        [Test]
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