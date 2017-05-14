using System;
using System.Collections.Generic;
using Mapster.Tests.Classes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapster.Tests
{
    #region TestMethod Object

    public class ConfigA
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public DateTime BirthDate { get; set; }
    }

    public class ConfigB
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public DateTime BirthDate { get; set; }
    }

    public class ConfigC
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public DateTime BirthDate { get; set; }
    }

    public class ConfigD
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public DateTime BirthDate { get; set; }
    }

    public class Source
    {
        public int Level { get; set; }
        public IList<Source> Children { get; set; }
        public Source Parent { get; set; }

        public Source(int level)
        {
            Children = new List<Source>();
            Level = level;
        }

        public void AddChild(Source child)
        {
            Children.Add(child);
            child.Parent = this;
        }
    }

    public class Destination
    {
        public int Level { get; set; }
        public IList<Destination> Children { get; set; }
        public Destination Parent { get; set; }
    }



    #endregion

    [TestClass]
    public class WhenConfiguringMapping
    {
        [TestMethod]
        public void IgnoreMemberTest()
        {
            var currentDate = DateTime.Now;

            var objA = new ConfigA()
            {
                BirthDate = currentDate,
                Id = 1,
                Name = "Timuçin",
                Surname = "KIVANÇ"
            };

            TypeAdapterConfig<ConfigA, ConfigB>.NewConfig()
                .Ignore(dest => dest.Id);

            var objB = TypeAdapter.Adapt<ConfigA, ConfigB>(objA);

            Assert.IsNotNull(objB);

            Assert.IsTrue(objB.Id == 0 && objB.FullName == null && objB.BirthDate == currentDate);
        }

        [TestMethod]
        public void MapFromTest()
        {
            var currentDate = DateTime.Now;

            var objC = new ConfigC()
            {
                BirthDate = currentDate,
                Id = 1,
                Name = "Timuçin",
                Surname = "KIVANÇ"
            };

            TypeAdapterConfig<ConfigC, ConfigD>.NewConfig()
                //.Map(dest => dest.FullName, (src) => string.Concat(src.Name, " ", src.Surname));
                .Map(dest => dest.FullName, src => string.Concat(src.Name, " ", src.Surname));

            var objD = TypeAdapter.Adapt<ConfigC, ConfigD>(objC);

            Assert.IsNotNull(objD);

            Assert.IsTrue(objD.Id == 1 && objD.FullName == "Timuçin KIVANÇ" && objD.BirthDate == currentDate);
        }


        [TestMethod]
        public void NewInstanceTest()
        {
            TestNewInstanceA obj = new TestNewInstanceA();
            obj.Name = "Tim";
            obj.Child = new TestNewInstanceC() { Name = "Kıvanç" };

            var newObj = TypeAdapter.Adapt<TestNewInstanceA, TestNewInstanceB>(obj);

            Assert.IsTrue(newObj.Name == "Tim");
            Assert.IsTrue(obj.Child.Name == newObj.Child.Name);

            obj.Child.Name = "İstanbul";

            Assert.IsTrue(obj.Child.Name != newObj.Child.Name);
        }

        [TestMethod]
        public void NewInstanceConfigurationTest()
        {
            TestNewInstanceD obj = new TestNewInstanceD();
            obj.Name = "Tim";
            obj.Child = new TestNewInstanceF() { Name = "Kıvanç" };

            TypeAdapterConfig<TestNewInstanceD, TestNewInstanceE>
                .NewConfig()
                .ShallowCopyForSameType(true);

            var newObj2 = TypeAdapter.Adapt<TestNewInstanceD, TestNewInstanceE>(obj);

            Assert.IsTrue(newObj2.Name == "Tim");
            Assert.IsTrue(obj.Child.Name == newObj2.Child.Name);

            obj.Child.Name = "Antalya";

            Assert.IsTrue(newObj2.Child.Name == "Antalya");
        }

        #region Data

        private Source _source;
        public void Initializer()
        {
            var nest = new Source(1);

            nest.AddChild(new Source(2));
            nest.Children[0].AddChild(new Source(3));
            nest.Children[0].AddChild(new Source(3));
            nest.Children[0].Children[1].AddChild(new Source(4));
            nest.Children[0].Children[1].AddChild(new Source(4));
            nest.Children[0].Children[1].AddChild(new Source(4));

            nest.AddChild(new Source(2));
            nest.Children[1].AddChild(new Source(3));

            nest.AddChild(new Source(2));
            nest.Children[2].AddChild(new Source(3));

            _source = nest;
        }

        #endregion
    }
}

