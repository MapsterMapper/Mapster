using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenMappingArrays
    {

        #region Tests

        [TestMethod]
        public void Single_Dimensional_Array_Is_Mapped()
        {
            var source = new FooArray { Ints = new int[] { 1, 2, 3, 4, 5 } };
            var target = new BarArray();

            TypeAdapter.Adapt(source, target);
            target.Ints.Length.ShouldBe(source.Ints.Length);
            target.Ints.ShouldBe(source.Ints);
        }

        [TestMethod]
        public void MapToTarget_With_Null_Value()
        {
            var source = new FooArray { Ints = null };
            var target = new BarArray { Ints = new int[] { 1, 2, 3, 4, 5 } };

            TypeAdapter.Adapt(source, target);
            target.Ints.ShouldBeNull();
        }

        [TestMethod]
        public void Multi_Dimensional_Array_Is_Mapped()
        {
            var source = new FooArrayMultiDimensional
            {
                IntsRank2 = new int[3, 2] {
                    { 10, 20 },
                    { 100, 200 },
                    { 1000, 2000 }
                },
                IntsRank3 = new int[3, 2, 5] {
                    { {10, 20, 30, 40 , 50}, { 60, 70, 80, 90, 100 } },
                    { {100, 200, 300, 400 , 500}, { 600, 700, 800, 900, 1000 } },
                    { {1000, 2000, 3000, 4000 , 5000}, { 6000, 7000, 8000, 9000, 10000 } }
                }
            };
            var target = new BarArrayMultiDimensional();

            TypeAdapter.Adapt(source, target);

            target.IntsRank2.Rank.ShouldBe(source.IntsRank2.Rank);
            target.IntsRank2.ShouldBe(source.IntsRank2);

            target.IntsRank3.Rank.ShouldBe(source.IntsRank3.Rank);
            target.IntsRank3.ShouldBe(source.IntsRank3);
        }

        [TestMethod]
        public void Jagged_Array_Is_Mapped()
        {
            var source = new FooArrayJagged
            {
                IntsRank2 = new int[][] {
                    new int[] { 10, 20 },
                    new int[] { 100, 200 , 300, 400, 500 },
                    new int[] { 1000, 2000, 3000 }
                },
                IntsRank3 = new int[][][] {
                    new int[][] {
                        new int[]{ 10, 20, 30, 40, 50 },
                        new int[]{ 60, 70 }
                    },
                    new int[][] {
                        new int[]{ 100, 200, 300 },
                        new int[]{ 400 },
                        new int[]{ 500, 600, 700, 800 },
                    }
                }
            };
            var target = new BarArrayJagged();

            TypeAdapter.Adapt(source, target);

            target.IntsRank2.Rank.ShouldBe(source.IntsRank2.Rank);
            target.IntsRank2.ShouldBe(source.IntsRank2);

            target.IntsRank3.Rank.ShouldBe(source.IntsRank3.Rank);
            target.IntsRank3.ShouldBe(source.IntsRank3);
        }

        [TestMethod]
        public void List_To_Array_Is_Mapped()
        {
            var source = new FooList { Ints = new List<int>(new int[] { 1, 2, 3, 4, 5 }) };
            var target = new BarArray();

            TypeAdapter.Adapt(source, target);
            target.Ints.Length.ShouldBe(source.Ints.Count);
            target.Ints.ShouldBe(source.Ints);
        }

        [TestMethod]
        public void List_To_Multi_Dimensional_Array_Is_Mapped()
        {
            var source = new List<int> { 1, 2, 3, 4, 5 };
            var target = source.Adapt<int[,,]>();
            target.GetLength(0).ShouldBe(1);
            target.GetLength(1).ShouldBe(1);
            target.GetLength(2).ShouldBe(5);
        }

        [TestMethod]
        public void Multi_Dimensional_Array_To_List_Is_Mapped()
        {
            var source = new [,] {{1, 2}, {3, 4}};
            var target = source.Adapt<List<int>>();
            target[0].ShouldBe(1);
            target[1].ShouldBe(2);
            target[2].ShouldBe(3);
            target[3].ShouldBe(4);
        }

        [TestMethod]
        public void Can_Map_Multi_Dimensional_Array_Of_Poco()
        {
            var source = new [,] {{new SimplePoco {Id = Guid.NewGuid(), Name = "Test"}}};
            var target = source.Adapt<SimpleDto[,]>();
            target[0, 0].Id.ShouldBe(source[0, 0].Id);
        }

        [TestMethod]
        public void Unmatch_Rank_Is_Mapped()
        {
            var source = new[] {1, 2, 3, 4, 5};
            var target = source.Adapt<int[,]>();
            target.GetLength(0).ShouldBe(1);
            target.GetLength(1).ShouldBe(5);
        }

        [TestMethod]
        public void Array_To_List_Is_Mapped()
        {
            var source = new FooArray { Ints = new int[] { 1, 2, 3, 4, 5 } };
            var target = new BarList();

            TypeAdapter.Adapt(source, target);
            target.Ints.Count.ShouldBe(source.Ints.Length);
            target.Ints.ShouldBe(source.Ints);
        }

        #endregion

        #region TestClasses

        internal class FooArray
        {
            public int[] Ints { get; set; }
        }

        internal class BarArray
        {
            public int[] Ints { get; set; }
        }

        internal class FooList
        {
            public List<int> Ints { get; set; }
        }

        internal class BarList
        {
            public List<int> Ints { get; set; }
        }

        internal class FooArrayMultiDimensional
        {
            public int[,] IntsRank2 { get; set; }
            public int[,,] IntsRank3 { get; set; }
        }

        internal class BarArrayMultiDimensional
        {
            public int[,] IntsRank2 { get; set; }
            public int[,,] IntsRank3 { get; set; }
        }

        internal class FooArrayJagged
        {
            public int[][] IntsRank2 { get; set; }
            public int[][][] IntsRank3 { get; set; }
        }

        internal class BarArrayJagged
        {
            public int[][] IntsRank2 { get; set; }
            public int[][][] IntsRank3 { get; set; }
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
        }

        #endregion

    }
}
