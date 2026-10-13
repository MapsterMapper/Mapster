using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System.Collections.Generic;
using System.Linq;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenAddCtorNullablePropagation
    {

        /// <summary>
        /// https://github.com/MapsterMapper/Mapster/issues/898
        /// </summary>
        [TestMethod]
        public void NullablePropagationFromCtorWorking()
        {
            var source = new List<OrderEntity898>
            {
                new() { Id = 1, Cod = new OrderCodEntity898 { Value = 42L } },
                new() { Id = 2, Cod = null },
            };

            Should.NotThrow(() =>
            {
                source.AsQueryable().BuildAdapter().CreateProjectionExpression<OrderDto898>();
            });

            var result = source.AsQueryable().ProjectToType<OrderDto898>().ToList();

            result.Count.ShouldBe(2);
            result[0].Id.ShouldBe(1);
            result[0].Cod.ShouldNotBeNull();
            result[0].Cod!.Value.ShouldBe(42L);
            result[1].Id.ShouldBe(2);
            result[1].Cod.ShouldBeNull();
        }

    }

    #region TestClasses

    public record OrderDto898(int Id, OrderCodDto898? Cod);
    public record OrderCodDto898(long Value);


    public class OrderEntity898
    {
        public int Id { get; set; }
        public int? CodId { get; set; }
        public OrderCodEntity898? Cod { get; set; }
    }

    public class OrderCodEntity898
    {
        public int Id { get; set; }
        public long Value { get; set; }
    }


    #endregion TestClasses
}
