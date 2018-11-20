using System;
using System.Threading.Tasks;
using StructuredMapper.BL.Geography;

namespace StructuredMapper.BL.Customers
{
    public class CustomerService : ICustomerService
    {
        public CustomerService()
        {
            // inject repo here
        }
        
        public Task<Customer> GetById(int id)
        {
            // IRL we retrieve this from a repo
            var customer = new Customer
            {
                CustomerNumber = id,
                DateJoined = new DateTime(1990, 1, 1),
                FirstName = "Mike",
                Surname = "Chamberlain",
                PhoneNumber = "0971143378",
                HomeAddress = new Address
                {
                    Street = "The Room, 78/54 Thanon Pan",
                    Area = "Silom",
                    Province = "Bangkok",
                    Zipcode = "10500",
                    CountryId = 1
                },
                BusinessAddress = new Address
                {
                    Street = "Agoda, Central World",
                    Area = "Pathum Wan",
                    Province = "Bangkok",
                    Zipcode = "10330",
                    CountryId = 1
                },
                ShippingAddress = new Address
                {
                    Street = "1 Shipping Street",
                    Area = "Shipton",
                    Province = "Shipingsford",
                    Zipcode = "SHP100",
                    CountryId = 2
                },
            };
            return Task.FromResult(customer);
        }
    }
}