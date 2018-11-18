using System;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using StructuredMapper.Models;
using StructuredMapper.Services;

namespace StructuredMapper.Test
{
    public class Tests
    {
        private CustomerEntity _customerEntity;

        [SetUp]
        public void Setup()
        {
            _customerEntity = new CustomerEntity
            {
                FirstName = "Mike",
                Surname = "Chamberlain",
                PhoneNumber = "0971143378",
                HomeAddress = new AddressEntity
                {
                    Street = "3 Some Lane",
                    Area = "Area",
                    Province = "Province",
                    Zipcode = "0000",
                    CountryId = 1
                },
                BusinessAddress = new AddressEntity
                {
                    Street = "3 Some Lane",
                    Area = "Area",
                    Province = "Province",
                    Zipcode = "0000",
                    CountryId = 1
                },
                CustomerNumber = 12345,
                Joined = new DateTime(1990, 1, 1)
            };
        }


        [Test]
        public void MapperBuilder_ForDuplicateProperties_Throws()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                new MapperBuilder<CustomerEntity, CustomerDto>()
                    .For(to => to.DateJoined, from => from.Joined)
                    .For(to => to.DateJoined, from => from.Joined);
            });
        }

        [Test]
        public async Task Mapper_WithSimpleProperty_Works()
        {
            var customerContactMapper = new MapperBuilder<CustomerEntity, ContactDto>()
                .For(to => to.First, from => from.FirstName)
                .Build();

            var result = await customerContactMapper(_customerEntity);
            
            Assert.AreEqual("Mike", result.First);
        }
        
        [Test]
        public async Task Mapper_WithStaticMethod_Works()
        {
            var customerContactMapper = new MapperBuilder<CustomerEntity, ContactDto>()
                .For(to => to.PhoneNumber, from => PhoneNumberFormatter.ToInternational(from.PhoneNumber))
                .Build();

            var result = await customerContactMapper(_customerEntity);
            
            Assert.AreEqual("+66971143378", result.PhoneNumber);
        }
        
        [Test]
        public async Task Mapper_WithAsyncMethod_Works()
        {
            var countryServiceMock = new Mock<ICountryService>();
            countryServiceMock.Setup(s => s.GetCountryName(It.IsAny<int>())).Returns(Task.FromResult("Thailand"));
            var addressService = new AddressTransformerService(countryServiceMock.Object);
            
            var customerContactMapper = new MapperBuilder<CustomerEntity, ContactDto>()
                .For(to => to.Addresses, from => Task.WhenAll(addressService.Transform(from.HomeAddress), addressService.Transform(from.BusinessAddress)))
                .Build();

            var result = await customerContactMapper(_customerEntity);
            
            Assert.AreEqual(2, result.Addresses.Length);
            Assert.AreEqual("Thailand", result.Addresses.First().CountryName);
        }
        
        [Test]
        public async Task Mapper_WithComposition_Works()
        {
            var countryServiceMock = new Mock<ICountryService>();
            countryServiceMock.Setup(s => s.GetCountryName(It.IsAny<int>())).Returns(Task.FromResult("Thailand"));
            var addressService = new AddressTransformerService(countryServiceMock.Object);

            var customerContactMapper = new MapperBuilder<CustomerEntity, ContactDto>()
                .For(to => to.First,       from => from.FirstName)
                .For(to => to.Last,        from => from.Surname)
                .For(to => to.PhoneNumber, from => PhoneNumberFormatter.ToInternational(from.PhoneNumber))
                .For(to => to.Addresses,   from => Task.WhenAll(addressService.Transform(from.HomeAddress), addressService.Transform(from.BusinessAddress)))
                .Build();

            var customerMapper = new MapperBuilder<CustomerEntity, CustomerDto>()
                .For(to => to.DateJoined, from => from.Joined)
                .For(to => to.CustomerId, from => from.CustomerNumber.ToString())
                .For(to => to.Contact,    customerContactMapper)
                .Build();

            var customerDto = await customerMapper(_customerEntity);

            Assert.AreEqual("12345", customerDto.CustomerId);
            Assert.AreEqual("Thailand", customerDto.Contact.Addresses.First().CountryName);
            Assert.AreEqual("+66971143378", customerDto.Contact.PhoneNumber);
        }
    }
}