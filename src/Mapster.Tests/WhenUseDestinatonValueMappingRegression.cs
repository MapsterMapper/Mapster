using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System.Collections.Generic;
using System.Linq;

namespace Mapster.Tests;

[TestClass]
public class WhenUseDestinatonValueMappingRegression
{
    [TestClass]
    public class WhenUseDestinatonMappingRegression
    {
        [TestMethod]
        public void UseDestinatonValueUsingMapWithasParam()
        {
            TypeAdapterConfig<ThumbnailDetailsSource, ICollection<ThumbnailDestination>>
                .NewConfig()
                .MapWith(src => MapThumbnailDetailsData(src).ToList());

            var channelSrc = new ChannelSource
            {
                ChannelId = "123",
                Thumbnails = new ThumbnailDetailsSource
                {
                    Default = new ThumbnailSource
                    {
                        Url = "https://www.youtube.com/default.jpg"
                    },
                    Medium = new ThumbnailSource
                    {
                        Url = "https://www.youtube.com/medium.jpg"
                    },
                    High = new ThumbnailSource
                    {
                        Url = "https://www.youtube.com/high.jpg"
                    }
                },

                TempThumbnails = new List<int>() { 1, 2, 3 }
            };
                       
            var channelDest = channelSrc.Adapt<ChannelDestination>();

            channelDest.Thumbnails.Count.ShouldBe(3);
            channelDest.TempThumbnails.Count.ShouldBe(3);
        }


        #region TestClasses
        private static IEnumerable<ThumbnailDestination> MapThumbnailDetailsData(ThumbnailDetailsSource thumbnailDetails)
        {
            yield return MapThumbnail(thumbnailDetails.Default, "Default");
            yield return MapThumbnail(thumbnailDetails.Medium, "Medium");
            yield return MapThumbnail(thumbnailDetails.High, "High");
        }

        private static ThumbnailDestination MapThumbnail(
            ThumbnailSource thumbnail,
            string thumbnailType) =>
            new()
            {
                Type = thumbnailType,
                Url = thumbnail.Url.Trim(),
            };


        public class ChannelDestination
        {
            public string ChannelId { get; set; } = default!;

            [UseDestinationValue]
            public ICollection<ThumbnailDestination> Thumbnails { get; } = new List<ThumbnailDestination>();

            [UseDestinationValue]
            public ICollection<string> TempThumbnails { get; } = new List<string>();
        }

        public class ThumbnailDestination
        {
            public string Type { get; set; } = default!;
            public string Url { get; set; } = default!;
        }

        public class ChannelSource
        {
            public string ChannelId { get; set; } = default!;
            public ThumbnailDetailsSource Thumbnails { get; set; } = default!;
            public ICollection<int> TempThumbnails { get; set; } = new List<int>();
        }

        public class ThumbnailDetailsSource
        {
            public ThumbnailSource? Default { get; set; }
            public ThumbnailSource? Medium { get; set; }
            public ThumbnailSource? High { get; set; }
        }

        public class ThumbnailSource
        {
            public string Url { get; set; } = default!;
        }

        #endregion TestClasses
    }
}
