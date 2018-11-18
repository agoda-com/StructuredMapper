using System;

namespace StructuredMapper.Models
{
    public class CustomerDto
    {
        public string CustomerId { get; set; }
        public DateTime DateJoined { get; set; }
        public ContactDto Contact { get; set; }
    }
    
    public class ContactDto
    {
        public string First { get; set; }
        public string Last { get; set; }
        public string PhoneNumber { get; set; }
        public AddressDto[] Addresses { get; set; }
    }
    
    public class AddressDto
    {
        public string Street { get; set; }
        public string Area { get; set; }
        public string State { get; set; }
        public string CountryName { get; set; }
        public string Postcode { get; set; }
    }
}