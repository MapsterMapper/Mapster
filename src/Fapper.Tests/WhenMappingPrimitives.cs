using System;
using Fapper.Adapters;
using NUnit.Framework;

namespace Fapper.Tests
{
    [TestFixture]
    public class WhenMappingPrimitives
    {
        [Test]
        public void Integer_Is_Mapped_To_Byte()
        {
            byte b = PrimitiveAdapter<int, byte>.Adapt(5);

            Assert.IsTrue(b == 5);
        }
    }
}
