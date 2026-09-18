using Mapster.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenCachingAttributeMetadata
    {
        [TestMethod]
        public void Metadata_Is_Shared_By_Wrappers_Only_Within_A_Context()
        {
            var first = new CompileContext(new TypeAdapterConfig());
            var second = new CompileContext(new TypeAdapterConfig());
            var property = typeof(Source).GetProperty(nameof(Source.Original));
            var field = typeof(Source).GetField(nameof(Source.Field));
            var propertyModel = new PropertyModel(property, first.AttributeMetadata);
            var fieldModel = new FieldModel(field, first.AttributeMetadata);

            propertyModel.GetType().ShouldBe(typeof(PropertyModel));
            fieldModel.GetType().ShouldBe(typeof(FieldModel));
            propertyModel.GetCustomAttributesData().ShouldBeSameAs(
                new PropertyModel(property, first.AttributeMetadata).GetCustomAttributesData());
            fieldModel.GetCustomAttributesData().ShouldBeSameAs(
                new FieldModel(field, first.AttributeMetadata).GetCustomAttributesData());
            propertyModel.GetCustomAttributesData().ShouldNotBeSameAs(
                new PropertyModel(property, second.AttributeMetadata).GetCustomAttributesData());
            fieldModel.GetCustomAttributesData().ShouldNotBeSameAs(
                new FieldModel(field, second.AttributeMetadata).GetCustomAttributesData());
            new CompileArgument { Context = first }.CloneWith(MapType.MapToTarget).Context.ShouldBeSameAs(first);
        }

        [TestMethod]
        public void Metadata_Is_Read_Only_And_Preserves_Reflection_Order()
        {
            var cache = new AttributeMetadataCache();
            foreach (var member in new MemberInfo[]
            {
                typeof(Source).GetProperty(nameof(Source.Original)),
                typeof(Source).GetField(nameof(Source.Field)),
                typeof(Source).GetProperty(nameof(Source.Plain))
            })
            {
                var metadata = cache.Get(member);
                metadata.ShouldBeSameAs(cache.Get(member));
                metadata.Select(x => x.AttributeType).ShouldBe(member.GetCustomAttributesData().Select(x => x.AttributeType));
                var list = (IList<CustomAttributeData>)metadata;
                list.IsReadOnly.ShouldBeTrue();
                Should.Throw<NotSupportedException>(() => list.Add(null));
            }
        }

        [TestMethod]
        public void Metadata_Distinguishes_Closed_Generic_And_Hidden_Members()
        {
            var cache = new AttributeMetadataCache();
            var members = new MemberInfo[]
            {
                typeof(GenericSource<int>).GetProperty(nameof(GenericSource<int>.Value)),
                typeof(GenericSource<string>).GetProperty(nameof(GenericSource<string>.Value)),
                typeof(BaseSource).GetProperty(nameof(BaseSource.Value)),
                typeof(DerivedSource).GetProperty(nameof(DerivedSource.Value)),
                typeof(BaseSource).GetProperty(nameof(BaseSource.Inherited)),
                typeof(DerivedSource).GetProperty(nameof(BaseSource.Inherited))
            };
            for (var i = 0; i < members.Length; i++)
            {
                cache.Get(members[i]).Select(x => x.AttributeType)
                    .ShouldBe(members[i].GetCustomAttributesData().Select(x => x.AttributeType));
                for (var j = 0; j < i; j++)
                    cache.Get(members[i]).ShouldNotBeSameAs(cache.Get(members[j]));
            }
        }

        [TestMethod]
        public void Models_Are_Lazy_And_Public_Construction_Remains_Uncached()
        {
            var property = new CountingProperty(typeof(Source).GetProperty(nameof(Source.Original)));
            var cache = new AttributeMetadataCache();
            var cached = new PropertyModel(property, cache);
            var uncached = new PropertyModel(property);
            property.Reads.ShouldBe(0);
            cached.GetCustomAttributesData();
            cached.GetCustomAttributesData();
            property.Reads.ShouldBe(1);
            uncached.GetCustomAttributesData();
            uncached.GetCustomAttributesData();
            property.Reads.ShouldBe(3);
            cache.Complete();
            cache.Complete();
            cached.GetCustomAttributesData();
            cached.GetCustomAttributesData();
            property.Reads.ShouldBe(5);
        }

        [TestMethod]
        public void Failed_Metadata_Retrieval_Is_Not_Cached()
        {
            var property = new CountingProperty(typeof(Source).GetProperty(nameof(Source.Original))) { Fail = true };
            var cache = new AttributeMetadataCache();
            Should.Throw<InvalidOperationException>(() => cache.Get(property));
            property.Fail = false;
            cache.Get(property).ShouldHaveSingleItem();
            cache.Get(property);
            property.Reads.ShouldBe(2);
        }

        [TestMethod]
        public void Attribute_Instances_Are_Created_Per_Lookup()
        {
            var model = new PropertyModel(typeof(Source).GetProperty(nameof(Source.Original)), new AttributeMetadataCache());
            var first = model.GetCustomAttributeFromData<AdaptMemberAttribute>();
            var second = model.GetCustomAttributeFromData<AdaptMemberAttribute>();
            first.ShouldNotBeSameAs(second);
            first.Name.ShouldBe(second.Name);
            model.GetCustomAttributes(true).Single().ShouldNotBeSameAs(model.GetCustomAttributes(true).Single());
        }

        [TestMethod]
        [DataRow(MapType.Map, false)]
        [DataRow(MapType.MapToTarget, false)]
        [DataRow(MapType.Projection, false)]
        [DataRow(MapType.Map, true)]
        [DataRow(MapType.MapToTarget, true)]
        [DataRow(MapType.Projection, true)]
        public void Compilation_Releases_Metadata_Even_With_Retained_Member_And_Exception(MapType mapType, bool fail)
        {
            var retained = CompileAndRetain(mapType, fail);
            Collect();
            retained.Metadata.IsAlive.ShouldBeFalse();
            retained.Member.GetCustomAttributesData().Single().AttributeType.ShouldBe(typeof(AdaptMemberAttribute));
            var property = new CountingProperty(typeof(Source).GetProperty(nameof(Source.Original)));
            retained.Context.AttributeMetadata.Get(property);
            retained.Context.AttributeMetadata.Get(property);
            property.Reads.ShouldBe(2);
            GC.KeepAlive(retained);
        }

        // Keep stack locals out of the collection assertion; retain the same objects a callback or exception can expose.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static RetainedCompilation CompileAndRetain(MapType mapType, bool fail)
        {
            var retained = new RetainedCompilation();
            var config = new TypeAdapterConfig();
            config.Default.Settings.ValueAccessingStrategies.Add((source, destination, arg) =>
            {
                retained.Context = arg.Context;
                return null;
            });
            config.NewConfig<Source, Destination>().IgnoreMember((member, side) =>
            {
                if (side == MemberSide.Source && member.Name == nameof(Source.Original))
                {
                    var metadata = member.GetCustomAttributesData();
                    // Flattening also invokes this callback, but deliberately uses uncached models.
                    if (!ReferenceEquals(metadata, retained.Context.AttributeMetadata.Get((MemberInfo)member.Info)))
                        return false;
                    retained.Member = member;
                    retained.Metadata = new WeakReference(metadata);
                    if (fail)
                        throw new InvalidOperationException("Expected test failure");
                }
                return false;
            });
            var tuple = new TypeTuple(typeof(Source), typeof(Destination));
            if (fail)
            {
                retained.Exception = Should.Throw<CompileException>(() => config.CreateMapExpression(tuple, mapType));
                retained.Exception.Argument.Context.ShouldBeSameAs(retained.Context);
            }
            else
            {
                config.CreateMapExpression(tuple, mapType);
            }
            retained.Member.ShouldNotBeNull();
            retained.Context.ShouldNotBeNull();
            return retained;
        }

        [TestMethod]
        public void Completed_Cache_Releases_Member_Keys_As_Well_As_Values()
        {
            var cache = new AttributeMetadataCache();
            var references = Populate(cache);
            cache.Complete();
            Collect();
            references.All(x => !x.IsAlive).ShouldBeTrue();
            GC.KeepAlive(cache);
        }

        [TestMethod]
        public void Generated_Delegate_Does_Not_Retain_The_Compilation_Cache()
        {
            var references = new List<WeakReference>();
            var map = CompileAndObserve(references);
            references.Count.ShouldBeGreaterThan(0);
            Collect();
            references.All(x => !x.IsAlive).ShouldBeTrue();
            map(new Source { Original = 7 }).Renamed.ShouldBe(7);
            GC.KeepAlive(map);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Func<Source, Destination> CompileAndObserve(List<WeakReference> references)
        {
            var config = new TypeAdapterConfig();
            config.Default.Settings.ValueAccessingStrategies.Add((source, destination, arg) =>
            {
                references.Add(new WeakReference(arg.Context));
                references.Add(new WeakReference(arg.Context.AttributeMetadata));
                return null;
            });
            config.NewConfig<Source, Destination>();
            return config.GetMapFunction<Source, Destination>();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference[] Populate(AttributeMetadataCache cache)
        {
            var member = new CountingProperty(typeof(Source).GetProperty(nameof(Source.Original)));
            return new[] { new WeakReference(member), new WeakReference(cache.Get(member)) };
        }

        private static void Collect()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        [TestMethod]
        public void Mapping_Preserves_Renames_Ignores_And_Configuration_Isolation()
        {
            var source = new Source { Original = 7, Field = 8, Ignored = 9, Plain = 10 };
            var first = new TypeAdapterConfig();
            first.NewConfig<Source, Destination>();
            first.Compile();
            var result = source.Adapt<Destination>(first);
            var target = source.Adapt(new Destination { Ignored = 42 }, first);
            result.Renamed.ShouldBe(7);
            result.Field.ShouldBe(8);
            result.Ignored.ShouldBe(0);
            result.Plain.ShouldBe(10);
            target.Renamed.ShouldBe(7);
            target.Field.ShouldBe(8);
            target.Ignored.ShouldBe(42);
            var second = new TypeAdapterConfig();
            second.NewConfig<Source, Destination>().Map(x => x.Renamed, x => x.Original + 1);
            second.Compile();
            source.Adapt<Destination>(second).Renamed.ShouldBe(8);
            source.Adapt<Destination>(first).Renamed.ShouldBe(7);
            first.CompileProjection();
            new[] { source }.AsQueryable().ProjectToType<Destination>(first).Single().Renamed.ShouldBe(7);
        }

        [TestMethod]
        public void Nested_Mappings_And_Forks_Share_Only_The_Root_Context()
        {
            var contexts = new List<CompileContext>();
            var sourceTypes = new HashSet<Type>();
            var configs = new List<TypeAdapterConfig>();
            var config = new TypeAdapterConfig();
            config.Default.Settings.ValueAccessingStrategies.Add((source, destination, arg) =>
            {
                contexts.Add(arg.Context);
                sourceTypes.Add(arg.SourceType);
                configs.Add(arg.Context.Config);
                return null;
            });
            config.NewConfig<ContainerSource, ContainerDestination>()
                .Fork(child => child.ForType<Source, Destination>().Ignore(x => x.Plain));
            var tuple = new TypeTuple(typeof(ContainerSource), typeof(ContainerDestination));
            var roots = new List<CompileContext>();
            foreach (var mapType in new[] { MapType.Map, MapType.MapToTarget, MapType.Projection })
            {
                contexts.Clear();
                config.CreateMapExpression(tuple, mapType);
                contexts.Count.ShouldBeGreaterThan(1);
                contexts.Distinct().ShouldHaveSingleItem();
                roots.Add(contexts[0]);
            }
            roots.Distinct().Count().ShouldBe(3);
            sourceTypes.ShouldContain(typeof(ContainerSource));
            sourceTypes.ShouldContain(typeof(Source));
            configs.All(x => x != config).ShouldBeTrue();
            var source = new ContainerSource { First = new Source { Original = 7, Plain = 9 }, Second = new Source { Original = 8 } };
            var result = source.Adapt<ContainerDestination>(config);
            result.First.Renamed.ShouldBe(7);
            result.Second.Renamed.ShouldBe(8);
            result.First.Plain.ShouldBe(0);
            source.First.Adapt<Destination>(config).Plain.ShouldBe(9);
        }

        [TestMethod]
        public void Independent_Compilations_Do_Not_Share_Caches_Or_Decisions()
        {
            var contexts = new CompileContext[8];
            Parallel.For(0, contexts.Length, i =>
            {
                var config = new TypeAdapterConfig();
                config.Default.Settings.ValueAccessingStrategies.Add((source, destination, arg) =>
                {
                    contexts[i] = arg.Context;
                    return null;
                });
                config.NewConfig<Source, Destination>().Map(x => x.Plain, x => x.Plain + i);
                config.Compile();
                new Source { Original = 7, Plain = 10 }.Adapt<Destination>(config).Plain.ShouldBe(10 + i);
            });
            contexts.All(x => x != null).ShouldBeTrue();
            contexts.Select(x => x.AttributeMetadata).Distinct().Count().ShouldBe(contexts.Length);
        }

        private sealed class RetainedCompilation
        {
            public CompileContext Context;
            public IMemberModel Member;
            public WeakReference Metadata;
            public CompileException Exception;
        }

        private sealed class CountingProperty : PropertyInfo
        {
            private readonly PropertyInfo _property;
            public int Reads { get; private set; }
            public bool Fail { get; set; }
            public CountingProperty(PropertyInfo property) => _property = property;
            public override IList<CustomAttributeData> GetCustomAttributesData()
            {
                Reads++;
                if (Fail)
                    throw new InvalidOperationException("Expected metadata failure");
                return _property.GetCustomAttributesData();
            }
            public override string Name => _property.Name;
            public override Type DeclaringType => _property.DeclaringType;
            public override Type ReflectedType => _property.ReflectedType;
            public override Type PropertyType => _property.PropertyType;
            public override PropertyAttributes Attributes => _property.Attributes;
            public override bool CanRead => _property.CanRead;
            public override bool CanWrite => _property.CanWrite;
            public override MethodInfo[] GetAccessors(bool nonPublic) => _property.GetAccessors(nonPublic);
            public override MethodInfo GetGetMethod(bool nonPublic) => _property.GetGetMethod(nonPublic);
            public override MethodInfo GetSetMethod(bool nonPublic) => _property.GetSetMethod(nonPublic);
            public override ParameterInfo[] GetIndexParameters() => _property.GetIndexParameters();
            public override object[] GetCustomAttributes(bool inherit) => _property.GetCustomAttributes(inherit);
            public override object[] GetCustomAttributes(Type attributeType, bool inherit) => _property.GetCustomAttributes(attributeType, inherit);
            public override bool IsDefined(Type attributeType, bool inherit) => _property.IsDefined(attributeType, inherit);
            public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, System.Globalization.CultureInfo culture)
                => _property.GetValue(obj, invokeAttr, binder, index, culture);
            public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, System.Globalization.CultureInfo culture)
                => _property.SetValue(obj, value, invokeAttr, binder, index, culture);
        }

        public class Source
        {
            [AdaptMember("Renamed")]
            public int Original { get; set; }
            [AdaptMember("Field"), System.ComponentModel.Description("Metadata ordering fixture")]
            public int Field;
            [AdaptIgnore]
            public int Ignored { get; set; }
            public int Plain { get; set; }
        }

        public class Destination
        {
            public int Renamed { get; set; }
            public int Field;
            public int Ignored { get; set; }
            public int Plain { get; set; }
        }

        public class GenericSource<T>
        {
            [AdaptIgnore]
            public T Value { get; set; }
        }

        public class BaseSource
        {
            [AdaptIgnore]
            public int Value { get; set; }
            [AdaptMember("Name")]
            public int Inherited { get; set; }
        }

        public class DerivedSource : BaseSource
        {
            [AdaptMember("Renamed")]
            public new int Value { get; set; }
        }

        public class ContainerSource
        {
            public Source First { get; set; }
            public Source Second { get; set; }
        }

        public class ContainerDestination
        {
            public Destination First { get; set; }
            public Destination Second { get; set; }
        }
    }
}
