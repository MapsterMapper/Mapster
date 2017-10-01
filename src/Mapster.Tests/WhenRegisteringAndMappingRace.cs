using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenRegisteringAndMappingRace
    {
        [TestCleanup]
        public void TestCleanup()
        {
            TypeAdapterConfig.GlobalSettings.RequireExplicitMapping = false;
            TypeAdapterConfig.GlobalSettings.RequireDestinationMemberSource = false;
        }


        [TestMethod]
        public void Types_Map_Successfully_If_Mapping_Applied_First()
        {
            TypeAdapterConfig.GlobalSettings.RequireDestinationMemberSource = true;

            var simplePoco = new WhenAddingCustomMappings.SimplePoco {Id = Guid.NewGuid(), Name = "TestName"};

            TypeAdapterConfig<WhenAddingCustomMappings.SimplePoco, WeirdPoco>.NewConfig()
                .Map(dest => dest.IHaveADifferentId, src => src.Id)
                .Map(dest => dest.MyNamePropertyIsDifferent, src => src.Name)
                .Ignore(dest => dest.Children);

            TypeAdapter.Adapt<WhenAddingCustomMappings.SimplePoco, WeirdPoco>(simplePoco);
        }

        [TestMethod, TestCategory("speed"), Ignore]
        public void Race_Condition_Produces_Error()
        {
            TypeAdapterConfig.GlobalSettings.RequireDestinationMemberSource = true;

            var simplePoco = new WhenAddingCustomMappings.SimplePoco {Id = Guid.NewGuid(), Name = "TestName"};

            var exception = Should.Throw<AggregateException>(() =>
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
                        () => { TypeAdapter.Adapt<WeirdPoco>(simplePoco); }
                        );
                }
            });

            exception.InnerException.ShouldBeOfType(typeof(CompileException));

        }

        [TestMethod, TestCategory("speed")]
        public void Explicit_Mapping_Requirement_Throws_Before_Mapping_Attempted()
        {
            TypeAdapterConfig.GlobalSettings.RequireExplicitMapping = true;
            TypeAdapterConfig.GlobalSettings.RequireDestinationMemberSource = true;

            var simplePoco = new WhenAddingCustomMappings.SimplePoco { Id = Guid.NewGuid(), Name = "TestName" };

            Should.Throw<AggregateException>(() =>
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
                        () => { TypeAdapter.Adapt<WeirdPoco>(simplePoco); }
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