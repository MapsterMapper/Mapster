using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Should;

namespace Mapster.Tests
{
    [TestFixture]
    public class WhenRegisteringAndMappingRace
    {
        [TestFixtureTearDown]
        public void TearDown()
        {
            BaseTypeAdapterConfig.GlobalSettings.RequireExplicitMapping = false;
            BaseTypeAdapterConfig.GlobalSettings.RequireDestinationMemberSource = false;
        }


        [Test]
        public void Types_Map_Successfully_If_Mapping_Applied_First()
        {
            BaseTypeAdapterConfig.GlobalSettings.RequireDestinationMemberSource = true;

            var simplePoco = new WhenAddingCustomMappings.SimplePoco {Id = Guid.NewGuid(), Name = "TestName"};

            TypeAdapterConfig<WhenAddingCustomMappings.SimplePoco, WeirdPoco>.NewConfig()
                .Map(dest => dest.IHaveADifferentId, src => src.Id)
                .Map(dest => dest.MyNamePropertyIsDifferent, src => src.Name)
                .Ignore(dest => dest.Children);

            TypeAdapter.Adapt<WhenAddingCustomMappings.SimplePoco, WeirdPoco>(simplePoco);
        }

        [Test, Explicit]
        public void Race_Condition_Produces_Error()
        {
            BaseTypeAdapterConfig.GlobalSettings.RequireDestinationMemberSource = true;

            var simplePoco = new WhenAddingCustomMappings.SimplePoco {Id = Guid.NewGuid(), Name = "TestName"};

            var exception = Assert.Throws<AggregateException>(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    Parallel.Invoke(
                        () =>
                        {
                            TypeAdapterConfig<WhenAddingCustomMappings.SimplePoco, WeirdPoco>.NewConfig()
                                .Map(dest => dest.IHaveADifferentId, src => src.Id)
                                .Map(dest => dest.MyNamePropertyIsDifferent, src => src.Name)
                                .Ignore(dest => dest.Children);
                        },
                        () => { TypeAdapter.Adapt<WhenAddingCustomMappings.SimplePoco, WeirdPoco>(simplePoco); }
                        );
                }
            });

            exception.InnerException.ShouldBeType(typeof(ArgumentOutOfRangeException));

        }

        [Test, Explicit]
        public void Explicit_Mapping_Requirement_Throws_Before_Mapping_Attempted()
        {
            BaseTypeAdapterConfig.GlobalSettings.RequireExplicitMapping = true;
            BaseTypeAdapterConfig.GlobalSettings.RequireDestinationMemberSource = true;

            var simplePoco = new WhenAddingCustomMappings.SimplePoco { Id = Guid.NewGuid(), Name = "TestName" };

            Assert.Throws<AggregateException>(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                   Parallel.Invoke(
                        () =>
                        {
                            TypeAdapterConfig<WhenAddingCustomMappings.SimplePoco, WeirdPoco>.NewConfig()
                                .Map(dest => dest.IHaveADifferentId, src => src.Id)
                                .Map(dest => dest.MyNamePropertyIsDifferent, src => src.Name)
                                .Ignore(dest => dest.Children);
                        },
                        () => { TypeAdapter.Adapt<WhenAddingCustomMappings.SimplePoco, WeirdPoco>(simplePoco); }
                        );
                }
            });

            //Type should map at the end because mapping has completed.
            TypeAdapter.Adapt<WhenAddingCustomMappings.SimplePoco, WeirdPoco>(simplePoco);
        }


    }


    #region TestClasses

    public class SimplePoco
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
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


    public class WeirdPoco
    {
        public Guid IHaveADifferentId { get; set; }

        public string MyNamePropertyIsDifferent { get; set; }

        public List<ChildDto> Children { get; set; }
    }

    #endregion




}