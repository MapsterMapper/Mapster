using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenMappingRecordWithOptionalDateTimeConstructor
    {
        [TestMethod]
        public void Record_With_DateTime_Maps_To_Class_With_Optional_DateTime_Constructor()
        {
            var source = new DateTimeFoo(DateTime.Today);
            var destination = source.Adapt<DateTimeFooDto>();

            destination.Timestamp.ShouldBe(source.Timestamp);
        }

        public class DateTimeFooDto
        {
            public DateTime Timestamp { get; set; }

            public DateTimeFooDto(DateTime timestamp = default)
            {
                Timestamp = timestamp;
            }
        }

        public record DateTimeFoo(DateTime Timestamp);
    }
}
