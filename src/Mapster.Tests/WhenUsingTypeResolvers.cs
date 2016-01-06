using System;
using System.Collections.Generic;
using NUnit.Framework;
using Should;

namespace Mapster.Tests
{
    [TestFixture]
    public class WhenUsingTypeResolvers
    {
        [Test]
        public void Simple_Type_Is_Converted_With_Adapter()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .MapWith<SimplePocoResolver>();


            var source = new SimplePoco
            {
                Id = Guid.NewGuid(),
                Name = "SimplePoco"
            };

            var dest = TypeAdapter.Adapt<SimpleDto>(source);

            dest.Id.ShouldEqual(source.Id);
            dest.Name.ShouldEqual("I got converted!");
        }

        [Test]
        public void Simple_Type_Is_Converted_With_Adapter_Instance()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .MapWith(new SimplePocoResolver());


            var source = new SimplePoco
            {
                Id = Guid.NewGuid(),
                Name = "SimplePoco"
            };

            var dest = TypeAdapter.Adapt<SimpleDto>(source);

            dest.Id.ShouldEqual(source.Id);
            dest.Name.ShouldEqual("I got converted!");
        }

        [Test]
        public void Simple_Type_Is_Converted_With_Adapter_Function()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .MapWith(() => new SimplePocoResolver());


            var source = new SimplePoco
            {
                Id = Guid.NewGuid(),
                Name = "SimplePoco"
            };

            var dest = TypeAdapter.Adapt<SimpleDto>(source);

            dest.Id.ShouldEqual(source.Id);
            dest.Name.ShouldEqual("I got converted!");
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
                    UnmappedMember = "Unmapped1",
                    UnmappedMember2 = "Unmapped2"
                };
            }

            public SimpleDto Resolve(SimplePoco source, SimpleDto destination)
            {
                return Resolve(source);
            }
        }

        public class SimplePoco
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


        public class ParentDto
        {
            public Guid Id { get; set; }
            public string Name { get; set; }

            public List<ChildDto> Children { get; set; }

            public List<ChildDto> UnmappedChildren { get; set; }
        }

     

        #endregion
    }
}