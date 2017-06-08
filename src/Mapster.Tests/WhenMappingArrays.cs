using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenMappingArrays
    {

        #region Tests

        [TestMethod]
        public void Single_Dimensional_Array_Is_Mapped()
        {
            var a = new Foo { Ints = new int[] { 1, 2, 3, 4, 5 } };
            var b = new Bar();

            TypeAdapter.Adapt(a, b);
            b.Ints.Length.ShouldBe(a.Ints.Length);
            b.Ints.ShouldBe(a.Ints);
        }

        [TestMethod]
        public void Multi_Dimensional_Array_Is_Mapped()
        {
            var source = new FooMulti
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
            var target = new BarMulti();

            TypeAdapter.Adapt(source, target);

            target.IntsRank2.Rank.ShouldBe(source.IntsRank2.Rank);
            target.IntsRank2.ShouldBe(source.IntsRank2);

            target.IntsRank3.Rank.ShouldBe(source.IntsRank3.Rank);
            target.IntsRank3.ShouldBe(source.IntsRank3);
        }

        [TestMethod]
        public void Jagged_Array_Is_Mapped()
        {
            var source = new FooJagged
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
            var target = new BarJagged();

            TypeAdapter.Adapt(source, target);

            target.IntsRank2.Rank.ShouldBe(source.IntsRank2.Rank);
            target.IntsRank2.ShouldBe(source.IntsRank2);

            target.IntsRank3.Rank.ShouldBe(source.IntsRank3.Rank);
            target.IntsRank3.ShouldBe(source.IntsRank3);
        }

        #endregion

        #region TestClasses

        private class Foo
        {
            public int[] Ints { get; set; }
        }

        private class Bar
        {
            public int[] Ints { get; set; }
        }

        private class FooMulti
        {
            public int[,] IntsRank2 { get; set; }
            public int[,,] IntsRank3 { get; set; }
        }

        private class BarMulti
        {
            public int[,] IntsRank2 { get; set; }
            public int[,,] IntsRank3 { get; set; }
        }

        private class FooJagged
        {
            public int[][] IntsRank2 { get; set; }
            public int[][][] IntsRank3 { get; set; }
        }

        private class BarJagged
        {
            public int[][] IntsRank2 { get; set; }
            public int[][][] IntsRank3 { get; set; }
        }

        #endregion

    }
}
