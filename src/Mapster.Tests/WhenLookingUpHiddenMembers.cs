using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Shouldly;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenLookingUpHiddenMembers
    {
        [TestMethod]
        public void DropHiddenMembers_CurrentMembers_AreEnumeratedOnce()
        {
            var members = typeof(Source).GetProperties().Cast<MemberInfo>().ToArray();
            var current = new CountingCollection(members);
            var result = Filter(members, current);

            current.Enumerations.ShouldBe(0);
            using (result.GetEnumerator())
                current.Enumerations.ShouldBe(0);
            result.ToArray().ShouldBe(members);
            current.Enumerations.ShouldBe(1);
            result.ToArray().ShouldBe(members);
            current.Enumerations.ShouldBe(2);
        }

        [TestMethod]
        public void DropHiddenMembers_Source_IsEnumeratedOnce()
        {
            var members = typeof(Source).GetProperties().Cast<MemberInfo>().ToArray();
            var visits = 0;
            var source = members.Select(member =>
            {
                visits++;
                return member;
            });

            var result = Filter(source, members);
            visits.ShouldBe(0);
            using (result.GetEnumerator())
                visits.ShouldBe(0);
            result.ToArray().ShouldBe(members);
            visits.ShouldBe(members.Length);
        }

        [TestMethod]
        public void DropHiddenMembers_PartialEnumeration_DoesNotTraverseRemainingSource()
        {
            var member = typeof(Source).GetProperty(nameof(Source.First));
            var visits = 0;
            var source = Enumerable.Repeat<MemberInfo>(member, 10).Select(item =>
            {
                visits++;
                return item;
            });

            Filter(source, new[] { member }).First().ShouldBeSameAs(member);
            visits.ShouldBe(1);
        }

        [TestMethod]
        public void DropHiddenMembers_HiddenProperty_PreservesInheritedMembersAndOrder()
        {
            var inherited = typeof(BaseSource).GetProperty(nameof(BaseSource.Inherited));
            var hidden = typeof(BaseSource).GetProperty(nameof(BaseSource.Value));
            var visible = typeof(DerivedSource).GetProperty(nameof(DerivedSource.Value), BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            MemberInfo[] members = { inherited, hidden, visible, inherited };

            Filter(members, new MemberInfo[] { visible }).ShouldBe(new MemberInfo[] { inherited, visible, inherited });
        }

        [TestMethod]
        public void DropHiddenMembers_FieldHidesProperty_PreservesMetadataTokenSelection()
        {
            var hidden = typeof(BaseSource).GetProperty(nameof(BaseSource.Value));
            var visible = typeof(FieldSource).GetField(nameof(FieldSource.Value));

            Filter(new MemberInfo[] { hidden, visible }, new MemberInfo[] { visible }).ShouldBe(new MemberInfo[] { visible });
        }

        [TestMethod]
        public void DropHiddenMembers_DuplicateCurrentNames_FirstMemberWins()
        {
            var hidden = typeof(BaseSource).GetProperty(nameof(BaseSource.Value));
            var visible = typeof(DerivedSource).GetProperty(nameof(DerivedSource.Value), BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            Filter(new MemberInfo[] { hidden, visible }, new MemberInfo[] { visible, hidden }).ShouldBe(new MemberInfo[] { visible });
            Filter(new MemberInfo[] { hidden, visible }, new MemberInfo[] { hidden, visible }).ShouldBe(new MemberInfo[] { hidden });
        }

        [TestMethod]
        public void DropHiddenMembers_DifferentlyCasedNames_AreDistinct()
        {
            var upper = typeof(BaseSource).GetProperty(nameof(BaseSource.Value));
            var lower = typeof(CaseSource).GetProperty(nameof(CaseSource.value));

            Filter(new MemberInfo[] { upper, lower }, new MemberInfo[] { lower }).ShouldBe(new MemberInfo[] { upper, lower });
        }

        [TestMethod]
        public void DropHiddenMembers_EmptyCurrentMembers_PreservesAllMembers()
        {
            MemberInfo[] members = typeof(BaseSource).GetProperties();

            Filter(members, Array.Empty<MemberInfo>()).ShouldBe(members);
            Filter(Array.Empty<MemberInfo>(), members).ShouldBeEmpty();
            Filter(Array.Empty<MemberInfo>(), Array.Empty<MemberInfo>()).ShouldBeEmpty();
        }

        [TestMethod]
        public void DropHiddenMembers_PrivateMemberHidesPublicMember_PreservesSelection()
        {
            var hidden = typeof(BaseSource).GetProperty(nameof(BaseSource.Value));
            var visible = typeof(PrivateSource).GetProperty("Value", BindingFlags.NonPublic | BindingFlags.Instance);

            Filter(new MemberInfo[] { hidden }, new MemberInfo[] { visible }).ShouldBeEmpty();
        }

        [TestMethod]
        public void DropHiddenMembers_ReenumeratedResult_RecomputesSelection()
        {
            var first = typeof(BaseSource).GetProperty(nameof(BaseSource.Value));
            var second = typeof(DerivedSource).GetProperty(nameof(DerivedSource.Value), BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            var current = new List<MemberInfo> { second };
            var result = Filter(new MemberInfo[] { first, second }, current);

            result.ToArray().ShouldBe(new MemberInfo[] { second });
            current[0] = first;
            result.ToArray().ShouldBe(new MemberInfo[] { first });
        }

        [TestMethod]
        public void Adapt_HiddenProperty_UsesDerivedValueAndInheritedProperty()
        {
            var config = new TypeAdapterConfig();
            config.NewConfig<DerivedSource, Destination>();
            config.Compile();
            var source = new DerivedSource { Value = "derived", Inherited = 42 };
            ((BaseSource)source).Value = 7;

            var result = source.Adapt<Destination>(config);
            var target = source.Adapt(new Destination(), config);

            result.Value.ShouldBe("derived");
            result.Inherited.ShouldBe(42);
            target.Value.ShouldBe("derived");
            target.Inherited.ShouldBe(42);
        }

        [TestMethod]
        public void DropHiddenMembers_PropertyHidesField_PreservesSelection()
        {
            var hidden = typeof(FieldSource).GetField(nameof(FieldSource.Value));
            var visible = typeof(PropertySource).GetProperty(nameof(PropertySource.Value), BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            Filter(new MemberInfo[] { hidden, visible }, new MemberInfo[] { visible })
                .ShouldBe(new MemberInfo[] { visible });
        }

        [TestMethod]
        public void DropHiddenMembers_NameAccesses_GrowLinearly()
        {
            const int count = 64;
            var members = Enumerable.Range(0, count)
                .Select(index => new CountingMember("Member" + index, index)).ToArray();

            Filter(members, members).ToArray().ShouldBe(members);

            // Allow a constant number of name reads per member, but not a scan per match.
            members.Sum(member => member.NameReads).ShouldBeLessThanOrEqualTo(8 * count);
        }

        [TestMethod]
        public void DropHiddenMembers_UnmatchedCurrentMember_DoesNotReadMetadataToken()
        {
            var source = typeof(Source).GetProperty(nameof(Source.First));
            var unmatched = new CountingMember("Unmatched");

            Filter(new MemberInfo[] { source }, new MemberInfo[] { unmatched })
                .ShouldBe(new MemberInfo[] { source });
        }

        private sealed class CountingMember : MemberInfo
        {
            private readonly string _name;
            private readonly int? _token;

            public CountingMember(string name, int? token = null)
            {
                _name = name;
                _token = token;
            }

            public int NameReads { get; private set; }
            public override string Name
            {
                get
                {
                    NameReads++;
                    return _name;
                }
            }

            public override int MetadataToken => _token ?? throw new InvalidOperationException("Unexpected token access");
            public override Type DeclaringType => typeof(Source);
            public override Type ReflectedType => typeof(Source);
            public override MemberTypes MemberType => MemberTypes.Property;
            public override object[] GetCustomAttributes(bool inherit) => throw new NotSupportedException();
            public override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw new NotSupportedException();
            public override bool IsDefined(Type attributeType, bool inherit) => throw new NotSupportedException();
        }

        private static IEnumerable<MemberInfo> Filter(IEnumerable<MemberInfo> source, ICollection<MemberInfo> current)
        {
            return source.DropHiddenMembers(current);
        }

        private sealed class CountingCollection : ICollection<MemberInfo>
        {
            private readonly MemberInfo[] _members;
            public CountingCollection(MemberInfo[] members) => _members = members;
            public int Enumerations { get; private set; }
            public int Count => _members.Length;
            public bool IsReadOnly => true;
            public IEnumerator<MemberInfo> GetEnumerator()
            {
                Enumerations++;
                return ((IEnumerable<MemberInfo>)_members).GetEnumerator();
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(MemberInfo item) => _members.Contains(item);
            public void CopyTo(MemberInfo[] array, int index) => _members.CopyTo(array, index);
            public void Add(MemberInfo item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(MemberInfo item) => throw new NotSupportedException();
        }

        public class BaseSource
        {
            public int Value { get; set; }
            public int Inherited { get; set; }
        }

        public class DerivedSource : BaseSource
        {
            public new string Value { get; set; }
        }

        public class FieldSource : BaseSource
        {
            public new int Value;
        }

        public class CaseSource : BaseSource
        {
            public int value { get; set; }
        }

        public class PropertySource : FieldSource
        {
            public new string Value { get; set; }
        }

        public class PrivateSource : BaseSource
        {
            private new int Value { get; set; }
        }

        public class Destination
        {
            public string Value { get; set; }
            public int Inherited { get; set; }
        }

        public class Source
        {
            public int First { get; set; }
            public int Second { get; set; }
            public int Third { get; set; }
        }
    }
}
