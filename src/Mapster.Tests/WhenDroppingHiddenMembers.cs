using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenDroppingHiddenMembers
    {
        [TestMethod]
        public void Derived_Properties_Hide_Base_Properties()
        {
            var baseMember = typeof(BaseMembers).GetProperty(nameof(BaseMembers.Value));
            var derivedMember = typeof(DerivedMembers).GetProperty(nameof(DerivedMembers.Value));

            new[] { baseMember, derivedMember }.DropHiddenMembers(new[] { derivedMember })
                .ShouldBe(new[] { derivedMember });
        }

        [TestMethod]
        public void Derived_Fields_Hide_Base_Fields()
        {
            var baseMember = typeof(BaseMembers).GetField(nameof(BaseMembers.Field));
            var derivedMember = typeof(DerivedMembers).GetField(nameof(DerivedMembers.Field));

            new[] { baseMember, derivedMember }.DropHiddenMembers(new[] { derivedMember })
                .ShouldBe(new[] { derivedMember });
        }

        [TestMethod]
        public void Private_Properties_Hide_Public_Fields()
        {
            typeof(PrivateProperty).GetFieldsAndProperties()
                .Select(x => x.Name).ShouldNotContain(nameof(BaseMembers.Field));
        }

        [TestMethod]
        public void Names_Are_Case_Sensitive()
        {
            var upper = typeof(CaseSensitiveMembers).GetProperty(nameof(CaseSensitiveMembers.Name));
            var lower = typeof(CaseSensitiveMembers).GetProperty(nameof(CaseSensitiveMembers.name));

            new[] { upper, lower }.DropHiddenMembers(new[] { upper })
                .ShouldBe(new[] { upper, lower });
        }

        [TestMethod]
        public void Output_Order_And_Duplicates_Are_Preserved()
        {
            var baseMember = typeof(BaseMembers).GetProperty(nameof(BaseMembers.Value));
            var derivedMember = typeof(DerivedMembers).GetProperty(nameof(DerivedMembers.Value));
            var other = typeof(BaseMembers).GetProperty(nameof(BaseMembers.Other));
            var members = new[] { other, baseMember, derivedMember, other, derivedMember };

            members.DropHiddenMembers(new[] { derivedMember })
                .ShouldBe(new[] { other, derivedMember, other, derivedMember });
        }

        [TestMethod]
        public void Empty_Inputs_Are_Supported()
        {
            var member = typeof(BaseMembers).GetProperty(nameof(BaseMembers.Value));

            Array.Empty<MemberInfo>().DropHiddenMembers(new[] { member }).ShouldBeEmpty();
            new[] { member }.DropHiddenMembers(Array.Empty<MemberInfo>()).ShouldBe(new[] { member });
            Array.Empty<MemberInfo>().DropHiddenMembers(Array.Empty<MemberInfo>()).ShouldBeEmpty();
        }

        [TestMethod]
        public void Execution_Is_Deferred_And_Recomputed_For_Each_Enumeration()
        {
            var baseMember = typeof(BaseMembers).GetProperty(nameof(BaseMembers.Value));
            var derivedMember = typeof(DerivedMembers).GetProperty(nameof(DerivedMembers.Value));
            var currentMembers = new List<MemberInfo> { derivedMember };
            var visits = 0;
            var source = CountVisits(new[] { baseMember, derivedMember }, () => visits++);
            var result = source.DropHiddenMembers(currentMembers);

            visits.ShouldBe(0);
            using (var enumerator = result.GetEnumerator())
            {
                visits.ShouldBe(0);
                enumerator.MoveNext().ShouldBeTrue();
                enumerator.Current.ShouldBe(derivedMember);
                enumerator.MoveNext().ShouldBeFalse();
            }

            var firstEnumerationVisits = visits;
            firstEnumerationVisits.ShouldBeGreaterThan(0);
            currentMembers.Clear();
            result.ShouldBe(new[] { baseMember, derivedMember });
            visits.ShouldBeGreaterThan(firstEnumerationVisits);
        }

        [TestMethod]
        public void Comparison_Does_Not_Reenumerate_Source_For_Each_Member()
        {
            var members = typeof(MappingSource).GetProperties();
            var visits = 0;
            var source = CountVisits(members, () => visits++);

            source.DropHiddenMembers(members).ToArray().ShouldBe(members);

            // One traversal to collect comparison names and one to yield the members.
            visits.ShouldBeLessThanOrEqualTo(2 * members.Length);
        }

        [TestMethod]
        public void First_Current_Member_With_Matching_Name_Is_Selected()
        {
            var baseMember = typeof(BaseMembers).GetProperty(nameof(BaseMembers.Value));
            var derivedMember = typeof(DerivedMembers).GetProperty(nameof(DerivedMembers.Value));
            var members = new[] { baseMember, derivedMember };

            members.DropHiddenMembers(new[] { derivedMember, baseMember }).ShouldBe(new[] { derivedMember });
            members.DropHiddenMembers(new[] { baseMember, derivedMember }).ShouldBe(new[] { baseMember });
        }

        [TestMethod]
        public void Mapping_Compiles_And_Adapts_Only_Visible_Members()
        {
            var config = new TypeAdapterConfig();
            config.NewConfig<MappingSource, DerivedMembers>();
            config.Compile();
            var source = new MappingSource { Value = 11, Field = 22, Other = 33 };

            var result = source.Adapt<DerivedMembers>(config);
            result.Value.ShouldBe(11);
            result.Field.ShouldBe(22);
            result.Other.ShouldBe(33);
            ((BaseMembers)result).Value.ShouldBe(0);
            ((BaseMembers)result).Field.ShouldBe(0);

            var target = new DerivedMembers();
            ((BaseMembers)target).Value = 44;
            ((BaseMembers)target).Field = 55;
            source.Adapt(target, config).ShouldBeSameAs(target);
            target.Value.ShouldBe(11);
            target.Field.ShouldBe(22);
            target.Other.ShouldBe(33);
            ((BaseMembers)target).Value.ShouldBe(44);
            ((BaseMembers)target).Field.ShouldBe(55);
        }

        private static IEnumerable<T> CountVisits<T>(IEnumerable<T> source, Action visit)
        {
            foreach (var member in source)
            {
                visit();
                yield return member;
            }
        }

        public class BaseMembers
        {
            public int Value { get; set; }
            public int Field;
            public int Other { get; set; }
        }

        public class DerivedMembers : BaseMembers
        {
            public new int Value { get; set; }
            public new int Field;
        }

        public class PrivateProperty : BaseMembers
        {
            private new int Field { get; set; }
        }

        public class CaseSensitiveMembers
        {
            public int Name { get; set; }
            public int name { get; set; }
        }

        public class MappingSource
        {
            public int Value { get; set; }
            public int Field { get; set; }
            public int Other { get; set; }
        }
    }
}
