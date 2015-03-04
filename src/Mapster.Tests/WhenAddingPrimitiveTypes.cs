using System;
using NUnit.Framework;
using Should;

namespace Mapster.Tests
{
    [TestFixture]
    public class WhenAddingPrimitiveTypes
    {
        [TearDown]
        public void TearDown()
        {
            TypeAdapterConfig.GlobalSettings.PrimitiveTypes.Clear();
        }

        [Test]
        public void No_Primitive_Uri_Should_Throw()
        {
            TypeAdapter.ClearCache();
            var sourceUri = new Uri("http://example.com");

            var exception = Assert.Throws<ArgumentNullException>(() => TypeAdapter.Adapt<Uri, Uri>(sourceUri));
            Console.WriteLine(exception.Message);

            exception.ParamName.ShouldEqual("con");
        }

        [Test]
        public void Set_Primitive_Uri_Success()
        {
            TypeAdapter.ClearCache();
            TypeAdapterConfig.GlobalSettings.PrimitiveTypes.Add(typeof(Uri));

            var sourceUri = new Uri("http://example.com");
            var targetUri = TypeAdapter.Adapt<Uri, Uri>(sourceUri);

            targetUri.ShouldEqual(sourceUri);
        }

        [Test]
        public void No_Primitive_Uri_Property_Should_Throw()
        {
            TypeAdapterConfig<SimplePoco, SimplePoco>.Clear();
            var sourceDto = new SimplePoco
            {
                Id = 1,
                Website = new Uri("http://example.com"),
            };

            var exception = Assert.Throws<InvalidOperationException>(() => TypeAdapter.Adapt<SimplePoco, SimplePoco>(sourceDto));
            Console.WriteLine(exception.Message);

            var innerException = exception.InnerException;
            innerException.ShouldBeType<ArgumentNullException>();
            ((ArgumentNullException)innerException).ParamName.ShouldEqual("con");
        }

        [Test]
        public void Set_Primitive_Uri_Property_Success()
        {
            TypeAdapterConfig<SimplePoco, SimplePoco>.Clear();
            TypeAdapterConfig.GlobalSettings.PrimitiveTypes.Add(typeof(Uri));

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
