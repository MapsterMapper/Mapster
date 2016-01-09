using System;
using NUnit.Framework;
using Should;

namespace Mapster.Tests
{
    [TestFixture]
    public class WhenAddingPrimitiveTypes
    {
        [Test]
        public void Uri_Success()
        {
            var sourceUri = new Uri("http://example.com");
            var targetUri = TypeAdapter.Adapt<Uri, Uri>(sourceUri);

            targetUri.ShouldEqual(sourceUri);
        }

        [Test]
        public void Uri_Property_Success()
        {
            var sourceDto = new SimplePoco
            {
                Id = 1,
                Website = new Uri("http://example.com"),
            };
            var targetDto = TypeAdapter.Adapt<SimplePoco, SimplePoco>(sourceDto);

            targetDto.Website.ShouldEqual(sourceDto.Website);
        }

        #region TestClasses

        public class SimplePoco
        {
            public int Id { get; set; }
            public Uri Website { get; set; }
        }

        #endregion
    }
}
