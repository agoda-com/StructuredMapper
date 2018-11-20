using System;

namespace StructuredMapper.BL.Customers
{
    public class CustomerService : ICustomerService
    {
        public Customer GetById(int id)
        {
            return new Customer
            {
                CustomerNumber = id,
                DateJoined = new DateTime(1990, 1, 1),
                FirstName = "Mike",
                Surname = "Chamberlain",
                PhoneNumber = "0971143378",
                HomeAddress = new Address
                {
                    Street = "3 Some Lane",
                    Area = "Area",
                    Province = "Province",
                    Zipcode = "1234",
                    CountryId = 1
                },
                BusinessAddress = new Address
                {
                    Street = "Agoda, Central World",
                    Area = "Pathum Wan",
                    Province = "Bangkok",
                    Zipcode = "15000",
                    CountryId = 1
                },
                ShippingAddress = new Address
                {
                    Street = "1 Shipping Street",
                    Area = "Shipton",
                    Province = "Shingsford",
                    Zipcode = "SHP100",
                    CountryId = 2
                },
            };
        }
    }
}