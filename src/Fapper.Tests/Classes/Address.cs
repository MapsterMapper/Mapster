using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fapper.Tests.Classes
{
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
