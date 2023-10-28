using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenMappingPrimitiveCustomMappingRegression
    {
        [TestMethod]
        public void CustomMappingDateTimeToPrimitive()
        {
            TypeAdapterConfig<DateTime, long>
               .NewConfig()
               .MapWith(src => new DateTimeOffset(src).ToUnixTimeSeconds());

            TypeAdapterConfig<DateTime, string>
               .NewConfig()
               .MapWith(src => src.ToShortDateString());

            var _source = new DateTime(2023, 10, 27, 0, 0, 0, DateTimeKind.Utc);

            var _resultToLong = _source.Adapt<long>();
            var _resultToString = _source.Adapt<string>();

            _resultToLong.ShouldBe(new DateTimeOffset(new DateTime(2023, 10, 27, 0, 0, 0, DateTimeKind.Utc)).ToUnixTimeSeconds());
            _resultToString.ShouldNotBe(_source.ToString());
            _resultToString.ShouldBe(_source.ToShortDateString());
        }

        /// <summary>
        /// https://github.com/MapsterMapper/Mapster/issues/561
        /// </summary>
        [TestMethod]
        public void MappingToPrimitiveInsiderWithCustomMapping()
        {
            TypeAdapterConfig<Optional561<string?>, string?>
                .NewConfig()
                .MapToTargetWith((source, target) => source.HasValue ? source.Value : target);

            var sourceNull = new Source561 { Name = new Optional561<string?>(null) };
            var target = new Source561 { Name = new Optional561<string>("John") }.Adapt<Target561>();

            var TargetDestinationFromNull = new Target561() { Name = "Me" };
            var NullToupdateoptional = sourceNull.Adapt(TargetDestinationFromNull);
            var _result = sourceNull.Adapt(target);

            target.Name.ShouldBe("John");
            NullToupdateoptional.Name.ShouldBe("Me");
        }

        /// <summary>
        /// https://github.com/MapsterMapper/Mapster/issues/407
        /// </summary>
        [TestMethod]
        public void MappingDatetimeToLongWithCustomMapping()
        {
            TypeAdapterConfig<DateTime, long>
               .NewConfig()
               .MapWith(src => new DateTimeOffset(src).ToUnixTimeSeconds());

            TypeAdapterConfig<long, DateTime>
              .NewConfig()
              .MapWith(src => new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(src).Date);

            var emptySource = new Source407() { Time = DateTime.UtcNow.Date };
            var fromC1 = new DateTime(2023, 10, 27,0,0,0,DateTimeKind.Utc);
            var fromC2 = new DateTimeOffset(new DateTime(2025, 11, 23, 0, 0, 0, DateTimeKind.Utc)).ToUnixTimeSeconds();
            var c1 = new Source407 { Time = fromC1 };
            var c2 = new Destination407 { Time = fromC2 };

            var _result = c1.Adapt<Destination407>(); // Work 
            var _resultLongtoDateTime = c2.Adapt<Source407>();

            _result.Time.ShouldBe(new DateTimeOffset(new DateTime(2023, 10, 27, 0, 0, 0, DateTimeKind.Utc)).ToUnixTimeSeconds());               
            _resultLongtoDateTime.Time.ShouldBe(new DateTime(2025, 11, 23).Date);
        }

    }


    #region TestClasses

    public class Source407
    {
        public DateTime Time { get; set; }
    }

    public class Destination407
    {
        public long Time { get; set; }
    }

    class Optional561<T>
    {
        public Optional561(T? value)
        {
            if (value != null)
                HasValue = true;

            Value = value;
        }

        public bool HasValue { get; }
        public T? Value { get; }
    }

    class Source561
    {
        public Optional561<string?> Name { get; set; }
    }

    class Target561
    {
        public string? Name { get; set; }
    }

    #endregion TestClasses
}
