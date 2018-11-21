using System;

namespace StructuredMapper.Test.Api.Customers
{
    public class CustomerDto
    {
        public string CustomerNumber { get; set; }
        public string DateJoined { get; set; }
        public ContactDto Contact { get; set; } = new ContactDto();
    }
    
    public class ContactDto
    {
        public string First { get; set; }
        public string Last { get; set; }
        public string PhoneNumber { get; set; }
        public AddressDto HomeAddress { get; set; }
        public AddressDto[] OtherAddresses { get; set; }
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