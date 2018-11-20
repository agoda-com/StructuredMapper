using System;

namespace StructuredMapper.BL.Customers
{
    public class Customer
    {
        public int CustomerNumber { get; set; }
        public DateTime DateJoined { get; set; }
        
        public string FirstName { get; set; }
        public string Surname { get; set; }
        public string PhoneNumber { get; set; }
        
        public Address HomeAddress { get; set; }
        public Address BusinessAddress { get; set; }
        public Address ShippingAddress { get; set; }
        
    }
}