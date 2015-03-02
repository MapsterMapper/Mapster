using System;
using System.Collections.Generic;
using NUnit.Framework;
using Should;

namespace Mapster.Tests
{
    [TestFixture]
    public class WhenValidatingMappings
    {
        [SetUp]
        public void Setup()
        {
            TypeAdapterConfig<SimplePocoBase, SimpleDto>.Clear();
            TypeAdapterConfig<SimplePoco, SimpleDto>.Clear();
            TypeAdapterConfig<SimplePoco, SimpleDtoWithoutMissingMembers>.Clear();
            TypeAdapterConfig<SimpleFlattenedPoco, SimpleDto>.Clear();
        }

        [Test]
        public void Simple_Poco_With_Missing_Member_Throws_On_Mapping_Validate()
        {
            var config = TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig();

            var exception = Assert.Throws<ArgumentOutOfRangeException>(config.Validate);

            exception.Message.ShouldContain("SimplePoco");
            exception.Message.ShouldContain("SimpleDto");
            exception.Message.ShouldContain("UnmappedMember");
            exception.Message.ShouldContain("UnmappedMember2");

            Console.WriteLine(exception.Message);
       }

        [Test]
        public void Poco_Without_Missing_Members_Doesnt_Throw()
        {
            var config = TypeAdapterConfig<SimplePoco, SimpleDtoWithoutMissingMembers>.NewConfig();

            config.Validate();
        }

        [Test]
        public void Poco_With_Missing_Members_Represented_In_Inherited_Mapping_Doesnt_Throw()
        {
            TypeAdapterConfig<SimplePocoBase, SimpleDto>.NewConfig()
                .Ignore(dest => dest.UnmappedMember)
                .Ignore(dest => dest.UnmappedMember2);

            var config = TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .Inherits<SimplePocoBase, SimpleDto>();

            config.Validate();
        }

        [Test]
        public void Poco_With_Flattened_Members_Doesnt_Throw()
        {
            var config = TypeAdapterConfig<SimpleFlattenedPoco, SimpleDto>.NewConfig();

            config.Validate();
        }

        [Test]
        public void Poco_With_Deep_Flattened_Members_Doesnt_Throw()
        {
            var config = TypeAdapterConfig<CFlat, DFlat>.NewConfig();

            config.Validate();
        }

        [Test]
        public void Poco_With_Ignored_Missing_Members_Doesnt_Throw()
        {
            var config = TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .IgnoreMember(dest => dest.UnmappedMember)
                .IgnoreMember(dest => dest.UnmappedMember2);

            config.Validate();
        }

        [Test]
        public void Poco_With_Resolved_Missing_Members_Doesnt_Throw()
        {
            var config = TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .Map(dest => dest.UnmappedMember, src => src.Name)
                .Map(dest => dest.UnmappedMember2, src => src.Name);

            config.Validate();
        }

        [Test]
        public void Poco_With_Missing_Const_Doesnt_Throw()
        {
            var config = TypeAdapterConfig<SimplePoco, SimpleDtoWithConst>.NewConfig();

            config.Validate();
        }

        [Test]
        public void Poco_With_Missing_Member_With_Protected_Setter_Doesnt_Throw()
        {
            var config = TypeAdapterConfig<SimplePoco, SimpleDtoWithProtectedSetter>.NewConfig();

            config.Validate();
        }

        [Test]
        public void Poco_With_Unmapped_Child_With_Same_Destination_Type_Doesnt_Throw()
        {
            var config = TypeAdapterConfig<ParentPoco, ParentPoco2>.NewConfig();

            config.Validate();
        }

        [Test]
        public void Poco_With_Type_Converter_Doesnt_Throw()
        {
            var config = TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .MapWith(() => new SimplePocoResolver());

            config.Validate();
        }

        [Test]
        public void Poco_With_Unmapped_Child_Collection_Throws()
        {
            var config = TypeAdapterConfig<ParentPoco, ParentDto>.NewConfig();

            var exception = Assert.Throws<ArgumentOutOfRangeException>(config.Validate);

            exception.Message.ShouldContain("ParentPoco");
            exception.Message.ShouldContain("ParentDto");
            exception.Message.ShouldContain("UnmappedChildren");

            Console.WriteLine(exception.Message);

        }

        [Test]
        public void Global_Validate_With_Unmapped_Members_Throws()
        {
            TypeAdapterConfig<ParentPoco, ParentDto>.NewConfig();
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig();

            var exception = Assert.Throws<ArgumentOutOfRangeException>(TypeAdapterConfig.Validate);

            exception.Message.ShouldContain("SimplePoco");
            exception.Message.ShouldContain("SimpleDto");
            exception.Message.ShouldContain("UnmappedMember");
            exception.Message.ShouldContain("UnmappedMember2");

            exception.Message.ShouldContain("ParentPoco");
            exception.Message.ShouldContain("ParentDto");
            exception.Message.ShouldContain("UnmappedChildren");

            Console.WriteLine(exception.Message);
        }


        [Test]
        public void Validate_With_Unmapped_Classes_Throws_If_Explicit_Mapping_Enabled()
        {
            TypeAdapterConfig.GlobalSettings.RequireExplicitMapping = true;
            var config = TypeAdapterConfig<ParentPoco, ParentDto>.NewConfig();

            var exception = Assert.Throws<ArgumentOutOfRangeException>(config.Validate);

            exception.Message.ShouldContain("ParentPoco");
            exception.Message.ShouldContain("ParentDto");
            exception.Message.ShouldContain("ChildPoco");
            exception.Message.ShouldContain("ChildDto");

            Console.WriteLine(exception.Message);
        }

        [Test]
        public void Global_Validate_With_Unmapped_Classes_Throws_If_Explicit_Mapping_Enabled()
        {
            TypeAdapterConfig.ClearConfigurationCache();
            TypeAdapterConfig.GlobalSettings.RequireExplicitMapping = true;
            TypeAdapterConfig<ParentPoco, ParentDto>.NewConfig();

            var exception = Assert.Throws<ArgumentOutOfRangeException>(TypeAdapterConfig.Validate);

            Console.WriteLine(exception.Message);

            exception.Message.ShouldContain("ParentPoco");
            exception.Message.ShouldContain("ParentDto");
            exception.Message.ShouldContain("ChildPoco");
            exception.Message.ShouldContain("ChildDto");
        }

        #region TestClasses

        public class SimplePocoResolver : ITypeResolver<SimplePoco, SimpleDto>
        {
            public SimpleDto Resolve(SimplePoco source)
            {
                return new SimpleDto
                {
                    Id = source.Id,
                    Name = "I got converted!",
                };
            }
        }


        public class SimplePocoBase
        {
        }

        public class SimplePoco : SimplePocoBase
        {
            public Guid Id { get; set; }

            public string Name { get; set; }
        }

        public class SimpleFlattenedPoco
        {
            public Guid Id { get; set; }
            public string Name { get; set; }

            public string GetUnmappedMember()
            {
                return null;
            }

            public string GetUnmappedMember2()
            {
                return null;
            }
        }

        public class SimpleDtoWithoutMissingMembers
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
        }

        public class SimpleDto
        {
            public Guid Id { get; set; }
            public string Name { get; set; }

            public string UnmappedMember { get; set; }

            public string UnmappedMember2 { get; set; }
        }

        public class SimpleDtoWithConst
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
        }

        public class SimpleDtoWithProtectedSetter
        {
            public Guid Id { get; set; }
            public string Name { get; set; }

            public string UnmappedMember { get; protected set; }
        }


        public class ChildPoco
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
        }

        public class ChildDto
        {
            public Guid Id { get; set; }
            public string Name { get; set; }

            public string UnmappedChildMember { get; set; }
        }

        public class ParentPoco
        {
            public Guid Id { get; set; }
            public string Name { get; set; }

            public List<ChildPoco> Children { get; set; }
        }

        public class ParentPoco2
        {
            public Guid Id { get; set; }
            public string Name { get; set; }

            public List<ChildPoco> Children { get; set; }
        }

        public class ParentDto
        {
            public Guid Id { get; set; }
            public string Name { get; set; }

            public List<ChildDto> Children { get; set; }

            public List<ChildDto> UnmappedChildren { get; set; }
        }

        public class BFlat
        {
            public decimal Total { get; set; }
        }

        public class CFlat
        {
            public B BClass { get; set; }
        }

        public class DFlat
        {
            public decimal BClassTotal { get; set; }
        }

        #endregion

         
    }
}