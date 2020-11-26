using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace Mapster.Tests
{
    #region TestMethod Objects

    public class XProject
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class YProject
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public enum Projects
    {
        A = 1,
        B = 2,
        C = 3
    }

    public class Person
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public Projects Project { get; set; }

        public int[] X { get; set; }
        public List<int> Y { get; set; }        
        public ArrayList Z { get; set; }
        public ICollection<Guid> Ids { get; set; }
        public Nullable<Guid> CityId { get; set; }
        public byte[] Picture { get; set; }
        public List<string> Countries { get; set; }
        public IReadOnlyList<string> ReadOnlyCountries { get; set; }
        public ICollection<string> XX { get; set; }
        public List<int> YY { get; set; }
        public IList<int> ZZ { get; set; }
        public Departments[] RelatedDepartments { get; set; }
        public ICollection<XProject> Projects { get; set; }
        public ISet<string> Sets { get; set; }
    }

    public class PersonDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Projects Project { get; set; }

        public HashSet<int> X { get; set; }
        public int[] Y { get; set; }
        public ICollection<Guid> Z { get; set; }
        public ArrayList Ids { get; set; }
        public Nullable<Guid> CityId { get; set; }
        public byte[] Picture { get; set; }
        public ICollection<string> Countries { get; set; }
        public IReadOnlyList<string> ReadOnlyCountries { get; set; }
        public IEnumerable<string> XX { get; set; }
        public IList<int> YY { get; set; }
        public List<int> ZZ { get; set; }
        public Departments[] RelatedDepartments { get; set; }
        public List<YProject> Projects { get; set; }
        public ISet<string> Sets { get; set; }

    }

    public class Foo
    {
        public List<Foo> Foos { get; set; }
        public Foo[] FooArray { get; set; }
        public IEnumerable<int> Ints { get; set; }
        public int[] IntArray { get; set; }
    }

    #endregion

    [TestClass]
    public class WhenMappingCollections
    {
        [TestMethod]
        public void MapCollectionProperty()
        {
            var person = new Person()
            {
                Id = Guid.NewGuid(),
                Name = "Timuçin",
                Surname = "KIVANÇ",
                Project = Projects.A,
                X = new int[] { 1, 2, 3, 4 },
                Y = new List<int>() { 5, 6, 7 },
                Z = new ArrayList((ICollection)(new List<Guid>() { Guid.NewGuid(), Guid.NewGuid() })),
                Ids = new List<Guid>() { Guid.Empty, Guid.NewGuid() },
                CityId = Guid.NewGuid(),
                Picture = new byte[] { 0, 1, 2 },
                Countries = new List<string> { "Turkey", "Germany" },
                XX = new List<string> { "Nederland", "USA" },
                YY = new List<int> { 22, 33 },
                ZZ = new List<int> { 44, 55 },
                ReadOnlyCountries = new List<string> { "Turkey", "Germany" },
                RelatedDepartments = new Departments[] { Departments.IT, Departments.Finance },
                Sets = new HashSet<string> { "Foo", "Bar"}
            };

            var dto = TypeAdapter.Adapt<Person, PersonDTO>(person);
            Assert.IsNotNull(dto);
            Assert.IsTrue(dto.Id == person.Id && 
                dto.Name == person.Name &&
                dto.Project == person.Project);

            Assert.IsNotNull(dto.X);
            Assert.IsTrue(dto.X.Count == 4 && dto.X.Contains(1) && dto.X.Contains(2) && dto.X.Contains(3) && dto.X.Contains(4));

            Assert.IsNotNull(dto.Y);
            Assert.IsTrue(dto.Y.Length == 3 && dto.Y[0] == 5 && dto.Y[1] == 6 && dto.Y[2] == 7);

            Assert.IsNotNull(dto.Z);
            Assert.IsTrue(dto.Z.Count == 2 && dto.Z.Contains((Guid)person.Z[0]) && dto.Z.Contains((Guid)person.Z[1]));

            Assert.IsNotNull(dto.Ids);
            Assert.IsTrue(dto.Ids.Count == 2);

            Assert.IsTrue(dto.CityId == person.CityId);

            Assert.IsNotNull(dto.Picture);
            Assert.IsTrue(dto.Picture.Length == 3 && dto.Picture[0] == 0 && dto.Picture[1] == 1 && dto.Picture[2] == 2);

            Assert.IsNotNull(dto.Countries);
            Assert.IsTrue(dto.Countries.Count == 2 && dto.Countries.First() == "Turkey" && dto.Countries.Last() == "Germany");

            Assert.IsNotNull(dto.ReadOnlyCountries);
            Assert.IsTrue(dto.ReadOnlyCountries.Count == 2 && dto.ReadOnlyCountries.First() == "Turkey" && dto.ReadOnlyCountries.Last() == "Germany");

            Assert.IsNotNull(dto.XX);
            Assert.IsTrue(dto.XX.Count() == 2 && dto.XX.First() == "Nederland" && dto.XX.Last() == "USA");

            Assert.IsNotNull(dto.YY);
            Assert.IsTrue(dto.YY.Count == 2 && dto.YY.First() == 22 && dto.YY.Last() == 33);

            Assert.IsNotNull(dto.ZZ);
            Assert.IsTrue(dto.ZZ.Count == 2 && dto.ZZ.First() == 44 && dto.ZZ.Last() == 55);

            Assert.IsNotNull(dto.RelatedDepartments);
            Assert.IsTrue(dto.RelatedDepartments.Length == 2 && dto.RelatedDepartments[0] == Departments.IT && dto.RelatedDepartments[1] == Departments.Finance);    
            
            Assert.IsNotNull(dto.Sets);
            Assert.IsTrue(dto.Sets.Count == 2 && dto.Sets.Contains("Foo") && dto.Sets.Contains("Bar"));
        }

        [TestMethod]
        public void MapCollection()
        {
            var person = new Person()
            {
                Id = Guid.NewGuid(),
                Name = "Timuçin",
                Surname = "KIVANÇ",
                Project = Projects.A,
                X = new int[] { 1, 2, 3, 4 },
                Y = new List<int>() { 5, 6, 7 },
                Z = new ArrayList((ICollection)(new List<Guid>() { Guid.NewGuid(), Guid.NewGuid() })),
                Ids = new List<Guid>() { Guid.Empty, Guid.NewGuid() },
                CityId = Guid.NewGuid(),
                Picture = new byte[] { 0, 1, 2 },
                Countries = new List<string> { "Turkey", "Germany" },
                ReadOnlyCountries = new List<string> { "Turkey", "Germany" },
                XX = new List<string> { "Nederland", "USA" },
                YY = new List<int> { 22, 33 },
                ZZ = new List<int> { 44, 55 },
                RelatedDepartments = new Departments[] { Departments.IT, Departments.Finance },
                Projects = new List<XProject>() { new XProject { Id = 1, Name = "Project X" } }
            };

            var persons = new List<Person>() { person };

            var dtos = TypeAdapter.Adapt<List<Person>, Person[]>(persons);

            Assert.IsNotNull(dtos);
            Assert.IsTrue(dtos.Length == 1);
            Assert.IsTrue(dtos.First().Id == person.Id &&
                dtos.First().Name == "Timuçin" &&
                dtos.First().Surname == "KIVANÇ");

            Assert.IsNotNull(dtos[0].Projects);

            Assert.IsTrue(dtos[0].Projects.First().Id == 1 && dtos[0].Projects.First().Name == "Project X");
        }

        [TestMethod]
        public void ShouldNotUsingTheSameEnumerable()
        {
            var src = new Foo
            {
                Foos = new List<Foo> { new Foo() },
                FooArray = new[] { new Foo() },
                Ints = new[] { 1, 2, 3 },
                IntArray = new[] { 4, 5, 6 },
            };
            var dest = src.Adapt<Foo>();
            Assert.IsFalse(src.Foos == dest.Foos);
            Assert.IsFalse(src.Foos[0] == dest.Foos[0]);
            Assert.IsFalse(src.FooArray == dest.FooArray);
            Assert.IsFalse(src.FooArray[0] == dest.FooArray[0]);
            Assert.IsFalse(src.Ints == dest.Ints);
            Assert.IsFalse(src.IntArray == dest.IntArray);
        }

        [TestMethod]
        public void MapToArrayEnumerateOnlyOnce()
        {
            var i = 0;

            IEnumerable<int> GetInts()
            {
                i++;
                yield return 1;
                yield return 2;
            }

            GetInts().Adapt<int[]>();
            i.ShouldBe(1);
        }

        [TestMethod]
        public void MapToListEnumerateOnlyOnce()
        {
            var i = 0;

            IEnumerable<int> GetInts()
            {
                i++;
                yield return 1;
                yield return 2;
            }

            GetInts().Adapt<List<int>>();
            i.ShouldBe(1);
        }

        [TestMethod]
        public void MapToMultiRanksArrayEnumerateOnlyOnce()
        {
            var i = 0;

            IEnumerable<int> GetInts()
            {
                i++;
                yield return 1;
                yield return 2;
            }

            GetInts().Adapt<int[,]>();
            i.ShouldBe(1);
        }

        [TestMethod]
        public void TestEnumList()
        {
            var testClass = new CloneTestEnumContainerListContainer
            {
                List1 = new List<CloneTestEnumContainer>
                {
                    new CloneTestEnumContainer
                    {
                        Type = CloneTestEnum.Value1,
                        Value = 500
                    }
                },
                List2 = new List<CloneTestEnumContainer>
                {
                    new CloneTestEnumContainer
                    {
                        Type = CloneTestEnum.Value5,
                        Value = 500
                    }
                }
            };

            var cloneTest = testClass.Adapt<CloneTestEnumContainerListContainer>();
            foreach (var paymentCompoent in testClass.CombinedLists)
            {
                cloneTest.CombinedLists.ShouldContain(x => x.Type == paymentCompoent.Type && x.Value == paymentCompoent.Value);
            }
        }

        #region TestClass

        [Flags]
        public enum CloneTestEnum
        {
            Value1 = 1,
            Value2 = 2,
            Value3 = 100,
            Value4 = 200,
            Value5 = 300
        }

        public class CloneTestEnumContainer
        {
            public CloneTestEnumContainer()
            {

            }

            public CloneTestEnumContainer(CloneTestEnum type, decimal value)
            {
                Type = type;
                Value = value;
            }

            public CloneTestEnum Type { get; set; }
            public decimal Value { get; set; }
        }

        public class CloneTestEnumContainerListContainer
        {
            public List<CloneTestEnumContainer> List1 { get; set; } = new List<CloneTestEnumContainer>(); //parts of the pricing calcs that need VAT added
            public List<CloneTestEnumContainer> List2 { get; set; } = new List<CloneTestEnumContainer>(); //parts of the pricing calcs that must NOT have VAT
            public ReadOnlyCollection<CloneTestEnumContainer> CombinedLists => List1.Concat(List2).ToList().AsReadOnly();
        }

        #endregion
    }
}
