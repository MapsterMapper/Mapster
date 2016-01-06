using System;
using NUnit.Framework;
using Should;

namespace Mapster.Tests
{
    [TestFixture]
    public class WhenAddingPrimitiveTypes
    {
        public WhenAddingPrimitiveTypes()
        {
            //this is to prevent type initialization exception
            TypeAdapterConfig.GlobalSettings.PrimitiveTypes.Add(typeof (Uri));
            TypeAdapter<Uri, Uri>.Recompile();
        }

        [TearDown]
        public void TearDown()
        {
            TypeAdapterConfig.GlobalSettings.PrimitiveTypes.Clear();
        }

        [Test]
        public void No_Primitive_Uri_Should_Throw()
        {
            try
            {
                TypeAdapterConfig.GlobalSettings.PrimitiveTypes.Clear();
                TypeAdapter<Uri, Uri>.Recompile();

                var sourceUri = new Uri("http://example.com");
                TypeAdapter.Adapt<Uri, Uri>(sourceUri);
                Assert.Fail("Should go to catch");
            }
            catch (ArgumentException) { }
        }

        [Test]
        public void Set_Primitive_Uri_Success()
        {
            TypeAdapterConfig.GlobalSettings.PrimitiveTypes.Add(typeof(Uri));
            TypeAdapter<Uri, Uri>.Recompile();

            var sourceUri = new Uri("http://example.com");
            var targetUri = TypeAdapter.Adapt<Uri, Uri>(sourceUri);

            targetUri.ShouldEqual(sourceUri);
        }

        [Test]
        public void No_Primitive_Uri_Property_Should_Throw()
        {
            try {
                TypeAdapterConfig.GlobalSettings.PrimitiveTypes.Clear();
                TypeAdapter<Uri, Uri>.Recompile();

                var sourceDto = new SimplePoco
                {
                    Id = 1,
                    Website = new Uri("http://example.com"),
                };

                TypeAdapter.Adapt<SimplePoco, SimplePoco>(sourceDto);
                Assert.Fail("Should go to catch");
            }
            catch (ArgumentException) { }
        }

        [Test]
        public void Set_Primitive_Uri_Property_Success()
        {
            TypeAdapterConfig.GlobalSettings.PrimitiveTypes.Add(typeof(Uri));
            TypeAdapter<Uri, Uri>.Recompile();

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
