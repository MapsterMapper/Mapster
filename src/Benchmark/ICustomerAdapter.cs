using System;
using System.Linq.Expressions;
using Benchmark.Classes;
using Mapster;

namespace Benchmark
{
    [Mapper]
    public interface ICustomerAdapter
    {
        Expression<Func<Customer, CustomerDTO>> Project { get; }
        
        CustomerDTO Adapt(Customer customer);
        Customer Adapt(CustomerDTO dto);
        Customer Adapt(CustomerDTO dto, Customer customer);
    }
}