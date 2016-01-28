using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mapster.Tests.Classes
{
    public class Product
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public decimal UnitPrice { get; set; }
        public int AmountInStock { get; set; }

        public DateTime CreatedDate { get; set; }
        public Nullable<DateTime> ModifiedDate { get; set; }
        public User CreatedUser { get; set; }
        public User ModifiedUser { get; set; }

        public ICollection<OrderLine> OrderLines { get; set; }
    }

    public class User
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Email { get; set; }

        public DateTime CreatedDate { get; set; }
        public Nullable<DateTime> ModifiedDate { get; set; }
    }

    public class OrderLine
    {
        public Guid Id { get; set; }
        public decimal UnitPrice { get; set; }
        public int Amount { get; set; }
        public decimal Discount { get; set; }
        public Guid OrderId { get; set; }
        public Guid ProductId { get; set; }

        public Order Order { get; set; }
        public Product Product { get; set; }
    }

    public class Order
    {
        public Guid Id { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public bool IsDelivered { get; set; }
        public int CustomerId { get; set; }
        public int SequenceNumberOrder { get; set; }
        public string ShippingInformationShippingName { get; set; }
        public string ShippingInformationShippingAddress { get; set; }
        public string ShippingInformationShippingCity { get; set; }
        public string ShippingInformationShippingZipCode { get; set; }

        public ICollection<OrderLine> OrderLines { get; set; }

        public Customer Customer { get; set; }
    }

    public class ProductDTO
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string CreatedUserName { get; set; }
        public UserDTO ModifiedUser { get; set; }
        public List<OrderLineListDTO> OrderLines { get; set; }
    }

    public class ProductListDTO
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public decimal UnitPrice { get; set; }
        public int AmountInStock { get; set; }

        public DateTime CreatedDate { get; set; }
        public Nullable<DateTime> ModifiedDate { get; set; }
        public string CreatedUserName { get; set; }
        public string ModifiedUserName { get; set; }
    }

    public class UserDTO
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
    }

    public class ProductNestedDTO
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public UserDTO CreatedUser { get; set; }
        public UserDTO ModifiedUser { get; set; }
    }

    public class OrderDTO
    {
        public Guid Id { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public bool IsDelivered { get; set; }

        public IEnumerable<OrderLineListDTO> OrderLines { get; set; }
    }

    public class OrderLineDTO
    {
        public Guid Id { get; set; }
        public decimal UnitPrice { get; set; }
        public int Amount { get; set; }
        public decimal Discount { get; set; }

        public OrderDTO Order { get; set; }
    }

    public class OrderLineListDTO
    {
        public Guid Id { get; set; }
        public decimal UnitPrice { get; set; }
        public int Amount { get; set; }
        public decimal Discount { get; set; }
    }

    public class ProductCollectionDTO
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public IEnumerable<OrderLineDTO> OrderLines { get; set; }

        public UserDTO CreatedUser { get; set; }
        public UserDTO ModifiedUser { get; set; }

        public string CreatedUserEmail { get; set; }
    }
}
