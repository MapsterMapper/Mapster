using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

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

            //first state (i = 0) Must be configured
            TypeAdapterConfig<WhenAddingCustomMappings.SimplePoco, WeirdPoco>.NewConfig()
                               .Map(dest => dest.IHaveADifferentId, src => src.Id)
                               .Map(dest => dest.MyNamePropertyIsDifferent, src => src.Name)
                               .Ignore(dest => dest.Children);

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

        [TestMethod, TestCategory("speed"), Ignore]
        public void Explicit_Mapping_Requirement_Throws_Before_Mapping_Attempted()
        {
            TypeAdapterConfig.GlobalSettings.RequireExplicitMapping = true;
            TypeAdapterConfig.GlobalSettings.RequireDestinationMemberSource = true;

            //first state (i = 0) Must be configured
            TypeAdapterConfig<WhenAddingCustomMappings.SimplePoco, WeirdPoco>.NewConfig()
                               .Map(dest => dest.IHaveADifferentId, src => src.Id)
                               .Map(dest => dest.MyNamePropertyIsDifferent, src => src.Name)
                               .Ignore(dest => dest.Children);

            var simplePoco = new WhenAddingCustomMappings.SimplePoco { Id = Guid.NewGuid(), Name = "TestName" };

            Should.Throw<AggregateException>(() =>
            {
                for (int i = 0; i < 1000; i++)
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

        [TestMethod]
        public void Race_Condition_Working()
        {
            TypeAdapterConfig.GlobalSettings.RequireExplicitMapping = true;
            TypeAdapterConfig.GlobalSettings.RequireDestinationMemberSource = true;

            var simplePoco = new WhenAddingCustomMappings.SimplePoco { Id = Guid.NewGuid(), Name = "TestName" };

            //first state (i = 0) Must be configured
            TypeAdapterConfigConcurrency<WhenAddingCustomMappings.SimplePoco, WeirdPoco>
                .NewConfig(cfg =>
                {
                    cfg
                        .Map(dest => dest.IHaveADifferentId, src => src.Id)
                        .Map(dest => dest.IHaveADifferentId, src => src.Id)
                        .Map(dest => dest.MyNamePropertyIsDifferent, src => src.Name)
                        .Ignore(dest => dest.Children);
                });

            for (int i = 0; i < 1000; i++)
            {
                Parallel.Invoke(
                    () =>
                    {
                        TypeAdapterConfigConcurrency<WhenAddingCustomMappings.SimplePoco, WeirdPoco>
                            .NewConfig(cfg =>
                            {
                                cfg
                                    .Map(dest => dest.IHaveADifferentId, src => src.Id)
                                    .Map(dest => dest.IHaveADifferentId, src => src.Id)
                                    .Map(dest => dest.MyNamePropertyIsDifferent, src => src.Name)
                                    .Ignore(dest => dest.Children);
                            });
                    },
                    () => { TypeAdapter.Adapt<WeirdPoco>(simplePoco); }
                    );
            }

        }

        [TestMethod]
        public void Scan_Race_Condition_Working()
        {
            TypeAdapterConfig.GlobalSettings.RequireExplicitMapping = true;
            TypeAdapterConfig.GlobalSettings.RequireDestinationMemberSource = true;

            var simplePoco = new WhenAddingCustomMappings.SimplePoco { Id = Guid.NewGuid(), Name = "TestName" };

            //first state (i = 0) Must be configured
            TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());


            for (int i = 0; i < 1000; i++)
            {
                Parallel.Invoke(
                    () =>
                    {
                        TypeAdapterConfig.GlobalSettings.ScanConcurrency(Assembly.GetExecutingAssembly());
                    },
                    () => { TypeAdapter.Adapt<WeirdPoco>(simplePoco); }
                    );
            }

        }
    }


    #region TestClasses

    public class RegData : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<WhenAddingCustomMappings.SimplePoco, WeirdPoco>()
            .Map(dest => dest.IHaveADifferentId, src => src.Id)
            .Map(dest => dest.MyNamePropertyIsDifferent, src => src.Name)
            .Ignore(dest => dest.Children);

        }
    }

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