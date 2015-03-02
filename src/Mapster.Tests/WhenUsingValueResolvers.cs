using System;
using System.Collections.Generic;
using NUnit.Framework;
using Should;

namespace Mapster.Tests
{
    [TestFixture]
    public class WhenUsingValueResolvers
    {

        [Test]
        public void Simple_String_Value_Is_Converted_With_Resolver_Factory_Function()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .Resolve(dest => dest.Name, () => new SimplePocoNameResolver());


            var source = new SimplePoco
            {
                Id = Guid.NewGuid(),
                Name = "SimplePoco"
            };

            var destination = TypeAdapter.Adapt<SimpleDto>(source);

            destination.Id.ShouldEqual(source.Id);
            destination.Name.ShouldEqual("Resolved:SimplePoco");
        }

        [Test]
        public void Simple_String_Value_Is_Converted_With_Resolver_Type()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .Resolve<SimplePocoNameResolver, string>(dest => dest.Name);


            var source = new SimplePoco
            {
                Id = Guid.NewGuid(),
                Name = "SimplePoco"
            };

            var destination = TypeAdapter.Adapt<SimpleDto>(source);

            destination.Id.ShouldEqual(source.Id);
            destination.Name.ShouldEqual("Resolved:SimplePoco");
        }

        [Test]
        public void Simple_String_Value_Is_Converted_With_Resolver_Instance()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .Resolve(dest => dest.Name, new SimplePocoNameResolver());


            var source = new SimplePoco
            {
                Id = Guid.NewGuid(),
                Name = "SimplePoco"
            };

            var destination = TypeAdapter.Adapt<SimpleDto>(source);

            destination.Id.ShouldEqual(source.Id);
            destination.Name.ShouldEqual("Resolved:SimplePoco");
        }


        #region TestClasses

        public class SimplePocoNameResolver : IValueResolver<SimplePoco, string>
        {
            public string Resolve(SimplePoco source)
            {
                return "Resolved:" + source.Name;
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