using System;

namespace StructuredMapper.Test.Models
{
    public class CustomerEntity
    {
        public string FirstName { get; set; }
        public string Surname { get; set; }
        public string PhoneNumber { get; set; }
        public AddressEntity HomeAddress { get; set; }
        public AddressEntity BusinessAddress { get; set; }
        public AddressEntity ShippingAddress { get; set; }
        public int CustomerNumber { get; set; }
        public DateTime DateJoined { get; set; }
        
    }

    public class AddressEntity
    {
        public string Street { get; set; }
        public string Area { get; set; }
        public string Province { get; set; }
        public int CountryId { get; set; }
        public string Zipcode { get; set; }
    }
}