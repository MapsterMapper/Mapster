using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenMapUsingOverrideTypesSettings
    {
        [TestMethod]
        public void OverrideDestinationTramsformIsWorked()
        {
            var config = new TypeAdapterConfig();
            config.Default.AddDestinationTransform(DestinationTransform.EmptyCollectionIfNull);

            config
                .NewConfig<CollectionPocoOverride, CollectionDtoOverride>()
                .MapUsing(src => src.Children, dest => dest.Children,
                cfg =>
                {
                    cfg.SkipDestinationTransforms();
                })
                .MapUsing(src => src.Array, dest => dest.Array,
                cfg =>
                {
                    cfg
                        .ReConfigurate()
                        .MapWith(x => x ?? new[] { 42 });
                });


            var source = new CollectionPocoOverride();
            var destination = source.Adapt<CollectionDtoOverride>(config);

            destination.MultiDimentionalArray.Length.ShouldBe(0);
            destination.ChildDict.Count.ShouldBe(0);
            destination.Set.Count.ShouldBe(0);

             
            destination.Children.ShouldBeNull(); // Destination Transforms from global context settings is skipped for this property
            destination.Array[0].ShouldBe(42); // Custom converter for types is worked, Destination Transforms is not achievable because the custom converter never returns null

           
            var destWithNotTypesSettingOverride = new CollectionPocoOverride().Adapt<CollectionDtoWithArray>(config);

            // Destination Transforms correct work from other mapping types
            destWithNotTypesSettingOverride.Array.Length.ShouldBe(0);
        }

        [TestMethod]
        public void UsingDefaultValueIsWorked()
        {
            var config = new TypeAdapterConfig();

            config.ForDestinationType<int>()
                .DefaultValue(x => 32);

            config.ForDestinationType<int?>()
                .DefaultValue(x=>42);

            int? src = null;
            var srcInsaider = new NullableIntInsaider() { Data = null };
            

            var resultCD = src.Adapt<int>(config);
            var resultCDInsaider = srcInsaider.Adapt<NullableIntInsaider>(config);

            resultCD.ShouldBe(32);
            resultCDInsaider.Data.ShouldBe(42);

            config.
                NewConfig<NullableIntInsaider, NullableIntInsaiderReconfig>()
                .MapUsing(dest => dest.Data, src => src.Data, cfg =>
                {
                    cfg.ReConfigurate()
                    .DefaultValue(x => 35);
                });

            var resultCDInsaiderReconfig = srcInsaider.Adapt<NullableIntInsaiderReconfig>(config);

            resultCDInsaiderReconfig.Data.ShouldBe(35);
        }

        [TestMethod]
        public void CustomDefaultValueIsWorkedWhenUsingAsCtorParam()
        {
            var config = new TypeAdapterConfig();

            config.ForDestinationType<int?>()
                .DefaultValue(x => 42);

            config.
               NewConfig<NullableIntInsaider, NullableIntCtorParam>()
               .MapUsing(dest => dest.Data, src => src.Data, cfg =>
               {
                   cfg.ReConfigurate()
                   .DefaultValue(x => 35);
               });

            var src = new NullableIntInsaider() { Data = null };

            var result = src.Adapt<NullableIntCtorParam>(config);

            result.Data.ShouldBe(35);
        }

        [TestMethod]
        public void ExtraSourceUsingCustomConfig()
        {
            var config = new TypeAdapterConfig();
            config.Default.AddDestinationTransform(DestinationTransform.EmptyCollectionIfNull);
            config.NewConfig<SourceFlattentInsaider, DestinationFlattentData>()
                .MapUsing(dest=> dest, src => src.SrcData, cfg =>
                {
                    cfg.SkipDestinationTransforms()
                    .ReConfigurate()
                    .Map(dest=>dest.Data, src => 42)
                    .MapUsing(dest => dest.Collection, src => src.Collection, cfg =>
                    {
                        cfg
                        .SkipDestinationTransforms();
                    })
                    ;
                });

            var src = new SourceFlattentInsaider() { SrcData = new() { Value = "Hello" } };

            var result = src.Adapt<DestinationFlattentData>(config);

            result.Collection.ShouldBeNull();
            result.Data.ShouldBe(42);
        }


        [TestMethod]
        public void ReMapSettersIsWorked()
        {
            var config = new TypeAdapterConfig();
            config.Default.AddDestinationTransform(DestinationTransform.EmptyCollectionIfNull);
            config.ForDestinationType<DestinationFlattentData>()
                .Ignore(x => x.Value)
                .Ignore(x => x.Data);
            config.NewConfig<SourceFlattentInsaider, DestinationFlattentData>()
                .ReMap(dest => dest, src => src.SrcData, true);
            config.NewConfig<RemapMemberMappings, DestinationFlattentData>()
                .ReMap(dest => dest.Data, src => src.Data);

            var src = new SourceFlattentInsaider() { SrcData = new() { Value = "Hello", Data = 42 } };
            var reMapSrc = new RemapMemberMappings { Data = 21, Value = "World" };



            var result = src.Adapt<DestinationFlattentData>(config);

            result.Collection.ShouldBeNull();
            result.Value.ShouldBe("Hello");
            result.Data.ShouldBe(42);

            var reMapResut = reMapSrc.Adapt<DestinationFlattentData>(config);

            reMapResut.Data.ShouldBe(21);
            reMapResut.Value.ShouldBe(default);
        }

        [TestMethod]
        public void ApplyPropagantionUsingDeepSrcAnalize()
        {
            var config = new TypeAdapterConfig();

            config.NewConfig<Source1004, Destination1004>()
                .Map(dest => dest.ProductNames, src => src.Products.Select(x => x.Name).ToArray());

            config.NewConfig<Source1004, DestinationCtor1004>()
                .Map(dest => dest.ProductNames, src => src.Products.Select(x => x.Name).ToArray());

            config.NewConfig<NullableStrings, NullableStringsDest>()
                .Map(dest => dest.Result, src => $"{src.Value1.ToString()}");

            config.NewConfig<NullableStrings, NullableStringsDestCtor>()
                .Map(dest => dest.Result, src => $"{src.Value1.ToString()}");

            var src = new Source1004();
            var srcStrings = new NullableStrings();

            //var str = src.BuildAdapter(config).CreateMapExpression<DestinationCtor1004>();
            //var str2 = src.BuildAdapter(config).CreateMapExpression<NullableStringsDestCtor>();

            Should.NotThrow(() =>
            {
                src.Adapt<Destination1004>(config);
                src.Adapt<DestinationCtor1004>(config);

                srcStrings.Adapt<NullableStringsDest>(config);
                srcStrings.Adapt<NullableStringsDestCtor>(config);
            });
        }

        #region TestClasses

        class Source1004
        {
            public Product1004[]? Products { get; set; }
        }

        class Product1004
        {
            public required string Name { get; set; }
        }

        class Destination1004
        {
            public string[]? ProductNames { get; set; }
        }

        class DestinationCtor1004
        {
            public DestinationCtor1004(string[]? productNames)
            {
                ProductNames = productNames;
            }

            public string[]? ProductNames { get; }
        }

        public class NullableStrings
        {
            public string Value1 { get; set; }

            public string Value2 { get; set; }
        }

        public class NullableStringsDest
        {
            public string Result { get; set; }
        }
        public class NullableStringsDestCtor
        {
            public NullableStringsDestCtor(string result)
            {
                Result = result;
            }

            public string Result { get;}
        }

        public class RemapMemberMappings
        {
            public int Data { get; set; }
            public string Value { get; set; }
        }

        public class DestinationFlattentData
        {
            public int Data { get; set; }
            public string Value { get; set; }
            public List<string> Collection { get; set; }
        }

        public class SourceFlattentData
        {
            public int Data { get; set; }
            public string Value { get; set; }
            public List<string> Collection { get; set; }
        }

        public class SourceFlattentInsaider
        {
            public SourceFlattentData SrcData { get; set; }
        }

        public class NullableIntCtorParam
        {
            public NullableIntCtorParam(int? data)
            {
                Data = data;
            }
            public int? Data { get; }
        }


        public class NullableIntInsaider
        {
            public int? Data { get; set; }
        }

        public class NullableIntInsaiderReconfig
        {
            public int? Data { get; set; }
        }
        class CollectionPocoWithArray
        {
            public int[] Array { get; set; }
        }

        class CollectionDtoWithArray
        {
            public int[] Array { get; set; }
        }

        class CollectionPocoOverride
        {
            public Guid Id { get; set; }
            public string Name { get; set; }

            public List<ChildPoco> Children { get; set; }
            public int[] Array { get; set; }
            public double[,] MultiDimentionalArray { get; set; }
            public Dictionary<string, ChildPoco> ChildDict { get; set; }
            public HashSet<string> Set { get; set; }
        }

        class CollectionDtoOverride
        {
            public Guid Id { get; set; }
            public string Name { get; set; }

            public IReadOnlyList<ChildDto> Children { get; internal set; }
            public int[] Array { get; set; }
            public double[,] MultiDimentionalArray { get; set; }
            public IReadOnlyDictionary<string, ChildDto> ChildDict { get; set; }
            public ISet<string> Set { get; set; }
        }
        #endregion TestClasses
    }
}
