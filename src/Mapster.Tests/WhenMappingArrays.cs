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
            var source = new FooArray { Ints = new int[] { 1, 2, 3, 4, 5 } };
            var target = new BarArray();

            TypeAdapter.Adapt(source, target);
            target.Ints.Length.ShouldBe(source.Ints.Length);
            target.Ints.ShouldBe(source.Ints);
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

        #endregion

        #region TestClasses

        private class FooArray
        {
            public int[] Ints { get; set; }
        }

        private class BarArray
        {
            public int[] Ints { get; set; }
        }

        private class FooArrayMultiDimensional
        {
            public int[,] IntsRank2 { get; set; }
            public int[,,] IntsRank3 { get; set; }
        }

        private class BarArrayMultiDimensional
        {
            public int[,] IntsRank2 { get; set; }
            public int[,,] IntsRank3 { get; set; }
        }

        private class FooArrayJagged
        {
            public int[][] IntsRank2 { get; set; }
            public int[][][] IntsRank3 { get; set; }
        }

        private class BarArrayJagged
        {
            public int[][] IntsRank2 { get; set; }
            public int[][][] IntsRank3 { get; set; }
        }

        #endregion

    }
}
