using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Shouldly;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenIgnoreMapping
    {
        [TestInitialize]
        public void Setup()
        {
            TypeAdapterConfig.GlobalSettings.Clear();
        }

        [TestMethod]
        public void TestIgnore()
        {
            TypeAdapterConfig<Poco, Dto>.NewConfig()
                .TwoWays()
                .Ignore(it => it.Name);

            var poco = new Poco { Id = Guid.NewGuid(), Name = "test" };
            var dto = poco.Adapt<Dto>();

            dto.Id.ShouldBe(poco.Id);
            dto.Name.ShouldBeNull();

            dto.Name = "bar";
            var poco2 = dto.Adapt<Poco>();

            poco2.Id.ShouldBe(dto.Id);
            poco2.Name.ShouldBeNull();
        }

        [TestMethod]
        public void TestIgnoreMember()
        {
            TypeAdapterConfig<Poco, Dto>.NewConfig()
                .TwoWays()
                .IgnoreMember((member, side) =>
                    member.GetCustomAttribute<JsonIgnoreAttribute>() != null && side == MemberSide.Destination);

            var poco = new Poco { Id = Guid.NewGuid(), Name = "test" };
            var dto = poco.Adapt<Dto>();

            dto.Id.ShouldBe(poco.Id);
            dto.Name.ShouldBeNull();

            dto.Name = "bar";
            var poco2 = dto.Adapt<Poco>();

            poco2.Id.ShouldBe(dto.Id);
            poco2.Name.ShouldBeNull();
        }

        /// <summary>
        /// https://github.com/MapsterMapper/Mapster/issues/707
        /// </summary>
        [TestMethod]
        public void WhenClassIgnoreCtorParamGetDefaultValue()
        {
            var config = new TypeAdapterConfig() 
            { 
                RequireDestinationMemberSource = true, 
            };
            config.Default
                   .NameMatchingStrategy(new NameMatchingStrategy
                    {
                        SourceMemberNameConverter = input => input.ToLowerInvariant(),
                        DestinationMemberNameConverter = input => input.ToLowerInvariant(),
                    })
                   ;
            config
                .NewConfig<A707, B707>()
                .MapToConstructor(GetConstructor<B707>())
                .Ignore(e => e.Id);

            var source = new A707 { Text = "test" };
            var dest = new B707(123, "Hello");

            var docKind = source.Adapt<B707>(config); 
            var mapTotarget = source.Adapt(dest,config);

            docKind.Id.ShouldBe(0);
            mapTotarget.Id.ShouldBe(123);
            mapTotarget.Text.ShouldBe("test");
        }

        /// <summary>
        /// https://github.com/MapsterMapper/Mapster/issues/723
        /// </summary>
        [TestMethod]
        public void MappingToIntefaceWithIgnorePrivateSetProperty()
        {
            TypeAdapterConfig<InterfaceSource723, InterfaceDestination723>
                .NewConfig()
                .TwoWays()
                .Ignore(dest => dest.Ignore);

            InterfaceDestination723 dataDestination = new Data723() { Inter = "IterDataDestination", Ignore = "IgnoreDataDestination" };

            Should.NotThrow(() =>
            {
                var isourse = dataDestination.Adapt<InterfaceSource723>();
                var idestination = dataDestination.Adapt<InterfaceDestination723>();
            });

        }

        #region TestClasses

        public interface InterfaceDestination723
        {
            public string Inter { get; set; }
            public string Ignore { get; }
        }

        public interface InterfaceSource723
        {
            public string Inter { get; set; }
        }

        private class Data723 : InterfaceSource723, InterfaceDestination723
        {
            public string Ignore { get; set; }

            public string Inter { get; set; }
        }

        static ConstructorInfo? GetConstructor<TDestination>()
        {
            var parameterlessCtorInfo = typeof(TDestination).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, new Type[0]);

            var ctors = typeof(TDestination).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var validCandidateCtors = ctors.Except(new[] { parameterlessCtorInfo }).ToArray();
            var ctorToUse = validCandidateCtors.Length == 1
                ? validCandidateCtors.First()
                : validCandidateCtors.OrderByDescending(c => c.GetParameters().Length).First();

            return ctorToUse;
        }
        public class A707
        {
            public string? Text { get; set; }
        }

        public class B707
        {
            public int Id { get; private set; }
            public string Text { get; private set; }

            public B707(int id, string text)
            {
                Id = id;
                Text = text;
            }
        }

        public class Poco
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
        }
        public class Dto
        {
            public Guid Id { get; set; }

            [JsonIgnore]
            public string Name { get; set; }
        }

        #endregion TestClasses
    }
}
