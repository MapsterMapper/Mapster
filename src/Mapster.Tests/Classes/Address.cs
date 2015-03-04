using System.ComponentModel.DataAnnotations.Schema;

namespace Mapster.Tests.Classes
{
    [ComplexType]
    public class Address
    {
        public string Street { get; set; }
        public string ZipCode { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public AddressType AddressType { get; set; }
    }

    public enum AddressType
    {
        Work = 1,
        Home = 2
    }
}
