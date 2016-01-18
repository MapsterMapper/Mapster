using System;
using NUnit.Framework;
using Should;

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

            chars.Length.ShouldEqual(5);
            chars[0].ShouldEqual('H');
            chars[1].ShouldEqual('e');
            chars[2].ShouldEqual('l');
            chars[3].ShouldEqual('l');
            chars[4].ShouldEqual('o');
        }
    }
}