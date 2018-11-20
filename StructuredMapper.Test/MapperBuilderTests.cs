using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using StructuredMapper.BL.Customers;
using StructuredMapper.BL.Geography;
using StructuredMapper.Test.Api.Customers;
using StructuredMapper.Test.Api.Helpers;

namespace StructuredMapper.Test
{
    public class MapperBuilderTests
    {
        private Customer _customer;
        private AddressDtoService _addressService;

        [SetUp]
        public void Setup()
        {
            _addressService = new AddressDtoService();

            _customer = new Customer
            {
                FirstName = "Mike",
                Surname = "Chamberlain",
                PhoneNumber = "0971143378",
                HomeAddress = new Address
                {
                    Street = "3 Some Lane",
                    Area = "Area",
                    Province = "Province",
                    Zipcode = "0000",
                    CountryId = 1
                },
                BusinessAddress = new Address
                {
                    Street = "3 Some Lane",
                    Area = "Area",
                    Province = "Province",
                    Zipcode = "0000",
                    CountryId = 1
                },
                ShippingAddress = new Address
                {
                    Street = "1 Ship Lane",
                    Area = "Area",
                    Province = "Province",
                    Zipcode = "0000",
                    CountryId = 1
                },
                CustomerNumber = 12345,
                DateJoined = new DateTime(1990, 1, 1)
            };
        }

        [Test]
        public void For_WithDuplicateExpressions_Throws()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                new MapperBuilder<Customer, CustomerDto>()
                    .For(to => to.DateJoined, from => from.DateJoined.ToString(CultureInfo.InvariantCulture))
                    .For(to => to.DateJoined, from => from.DateJoined.ToString(CultureInfo.InvariantCulture));
            });
        }
        
        [Test]
        public void For_WithNonMemberAccessTypeExpression_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new MapperBuilder<RecursiveEntity, RecursiveDto>()
                    .For(to => to, from => new RecursiveDto());
            });
        }
        
        [Test]
        public void Build_WithNoMappers_Throws()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                new MapperBuilder<Customer, CustomerDto>()
                    .Build();
            });
        }
        
        [Test]
        public void BuildSync_WithAsynchronousMappers_Throws()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                new MapperBuilder<Customer, CustomerDto>()
                    .For(to => to.DateJoined, Task.FromResult(new DateTime().ToString(CultureInfo.InvariantCulture)))
                    .BuildSync();
            });
        }
        
        [Test]
        public async Task For_WithLiteralValue_MapsCorrectly()
        {
            var customerContactMapper = new MapperBuilder<Customer, ContactDto>()
                .For(to => to.First, "Mike")
                .Build();

            var mapped = await customerContactMapper(_customer);
            
            Assert.AreEqual("Mike", mapped.First);
        }
        
        [Test]
        public async Task For_WithAsyncLiteralValue_MapsCorrectly()
        {
            var customerContactMapper = new MapperBuilder<Customer, ContactDto>()
                .For(to => to.First, Task.FromResult("Mike"))
                .Build();

            var mapped = await customerContactMapper(_customer);
            
            Assert.AreEqual("Mike", mapped.First);
        }
        
        [Test]
        public async Task For_WithMemberAccess_MapsCorrectly()
        {
            var customerContactMapper = new MapperBuilder<Customer, ContactDto>()
                .For(to => to.First, from => "Mike")
                .Build();

            var mapped = await customerContactMapper(_customer);
            
            Assert.AreEqual("Mike", mapped.First);
        }
        
        [Test]
        public async Task For_WithNestedMemberAccess_MapsCorrectly()
        {
            var customerMapper = new MapperBuilder<Customer, CustomerDto>()
                .For(to => to.Contact.First, from => from.FirstName)
                .Build();

            var mapped = await customerMapper(_customer);
            
            Assert.AreEqual("Mike", mapped.Contact.First);
        }
        
        [Test]
        public async Task For_WithSynchronousMapper_MapsCorrectly()
        {
            var customerContactMapper = new MapperBuilder<Customer, ContactDto>()
                .For(to => to.PhoneNumber, from => PhoneNumberFormatter.ToInternational(from.PhoneNumber, from.HomeAddress.CountryId))
                .Build();

            var mapped = await customerContactMapper(_customer);
            
            Assert.AreEqual("+66971143378", mapped.PhoneNumber);
        }
        
        [Test]
        public async Task For_WithAsyncMapper_MapsCorrectly()
        {   
            var customerContactMapper = new MapperBuilder<Customer, ContactDto>()
                .For(to => to.HomeAddress, from => _addressService.Transform(from.HomeAddress))
                .Build();

            var mapped = await customerContactMapper(_customer);
            
            Assert.AreEqual("3 Some Lane", mapped.HomeAddress.Street);
            Assert.AreEqual("Thailand", mapped.HomeAddress.CountryName);
        }
        
        [Test]
        public async Task For_WithComposition_MapsCorrectly()
        {
            var customerContactMapper = new MapperBuilder<Customer, ContactDto>()
                .For(to => to.PhoneNumber, from => PhoneNumberFormatter.ToInternational(from.PhoneNumber, from.HomeAddress.CountryId))
                .Build();

            var customerMapper = new MapperBuilder<Customer, CustomerDto>()
                .For(to => to.Contact, customerContactMapper)
                .Build();

            var mapped = await customerMapper(_customer);

            Assert.AreEqual("+66971143378", mapped.Contact.PhoneNumber);
        }

        [Test]
        public void BuildSync_MapsCorrectly()
        {
            var mapper = new MapperBuilder<Customer, CustomerDto>()
                .For(to => to.DateJoined, from => from.DateJoined.ToString(CultureInfo.InvariantCulture))
                .BuildSync();

            var mapped = mapper(_customer);
            
            Assert.AreEqual(_customer.DateJoined.ToString(CultureInfo.InvariantCulture), mapped.DateJoined);
        }
        
        [Test]
        public async Task Build_WithFullExample_MapsCorrectly()
        {                
            var customerContactMapper = new MapperBuilder<Customer, ContactDto>()
                .For(to => to.First,          "Mikey")
                .For(to => to.Last,           from => from.Surname)
                .For(to => to.PhoneNumber,    from => PhoneNumberFormatter.ToInternational(from.PhoneNumber, from.HomeAddress.CountryId))
                .For(to => to.HomeAddress,    from => _addressService.Transform(from.HomeAddress))
                .For(to => to.OtherAddresses, from => Task.WhenAll(_addressService.Transform(from.BusinessAddress), _addressService.Transform(from.ShippingAddress)))
                .Build();

            var customerMapper = new MapperBuilder<Customer, CustomerDto>()
                .For(to => to.DateJoined, from => from.DateJoined.ToString(CultureInfo.InvariantCulture))
                .For(to => to.CustomerId, from => from.CustomerNumber)
                .For(to => to.Contact,    customerContactMapper)
                .Build();

            var mapped = await customerMapper(_customer);

            Assert.AreEqual("Mikey", mapped.Contact.First);
            Assert.AreEqual(12345, mapped.CustomerId);
            Assert.AreEqual(_customer.DateJoined.ToString(CultureInfo.InvariantCulture), mapped.DateJoined);
            Assert.AreEqual("Thailand", mapped.Contact.OtherAddresses.First().CountryName);
            Assert.AreEqual(_customer.ShippingAddress.Street, mapped.Contact.OtherAddresses.ElementAt(1).Street);
            Assert.AreEqual("+66971143378", mapped.Contact.PhoneNumber);
        }
    }
}