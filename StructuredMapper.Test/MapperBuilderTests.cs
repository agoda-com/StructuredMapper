using System;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using StructuredMapper.Models;
using StructuredMapper.Services;

namespace StructuredMapper.Test
{
    public class MapperBuilderTests
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
                ShippingAddress = new AddressEntity
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
        public void MapperBuilder_WithDuplicateProperties_Throws()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                new MapperBuilder<CustomerEntity, CustomerDto>()
                    .For(to => to.DateJoined, from => from.DateJoined)
                    .For(to => to.DateJoined, from => from.DateJoined);
            });
        }

        [Test]
        public void MapperBuilder_WithNoMappers_Throws()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                new MapperBuilder<CustomerEntity, CustomerDto>()
                    .Build();
            });
        }
        
        [Test]
        public void MapperBuilder_WithNonMemberAccessExpression_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new MapperBuilder<CustomerEntity, CustomerDto>()
                    .For(to => to, from => new CustomerDto());
            });
        }
        
        [Test]
        public async Task Mapper_WithSimpleProperty_Works()
        {
            var customerContactMapper = new MapperBuilder<CustomerEntity, ContactDto>()
                .For(to => to.First, from => from.FirstName)
                .Build();

            var mapped = await customerContactMapper(_customerEntity);
            
            Assert.AreEqual("Mike", mapped.First);
        }
        
        [Test]
        public async Task Mapper_ComplexProperty_Works()
        {
            var customerMapper = new MapperBuilder<CustomerEntity, CustomerDto>()
                .For(to => to.Contact.First, from => from.FirstName)
                .Build();

            var mapped = await customerMapper(_customerEntity);
            
            Assert.AreEqual("Mike", mapped.Contact.First);
        }
        
        [Test]
        public async Task Mapper_WithStaticMethod_Works()
        {
            var customerContactMapper = new MapperBuilder<CustomerEntity, ContactDto>()
                .For(to => to.PhoneNumber, from => PhoneNumberFormatter.ToInternational(from.PhoneNumber))
                .Build();

            var mapped = await customerContactMapper(_customerEntity);
            
            Assert.AreEqual("+66971143378", mapped.PhoneNumber);
        }
        
        [Test]
        public async Task Mapper_WithAsyncMethod_Works()
        {
            var countryServiceMock = new Mock<ICountryService>();
            countryServiceMock.Setup(s => s.GetCountryName(It.IsAny<int>())).Returns(Task.FromResult("Thailand"));
            var addressService = new AddressTransformerService(countryServiceMock.Object);
            
            var customerContactMapper = new MapperBuilder<CustomerEntity, ContactDto>()
                .For(to => to.HomeAddress, from => addressService.Transform(from.HomeAddress))
                .Build();

            var mapped = await customerContactMapper(_customerEntity);
            
            Assert.AreEqual("3 Some Lane", mapped.HomeAddress.Street);
            Assert.AreEqual("Thailand", mapped.HomeAddress.CountryName);
        }
        
        [Test]
        public async Task Mapper_WithComposition_Works()
        {
            var customerContactMapper = new MapperBuilder<CustomerEntity, ContactDto>()
                .For(to => to.PhoneNumber, from => PhoneNumberFormatter.ToInternational(from.PhoneNumber))
                .Build();

            var customerMapper = new MapperBuilder<CustomerEntity, CustomerDto>()
                .For(to => to.Contact, customerContactMapper)
                .Build();

            var mapped = await customerMapper(_customerEntity);

            Assert.AreEqual("+66971143378", mapped.Contact.PhoneNumber);
        }
        
        [Test]
        public async Task Mapper_WithEverything_Works()
        {
            var countryServiceMock = new Mock<ICountryService>();
            countryServiceMock.Setup(s => s.GetCountryName(It.IsAny<int>())).Returns(Task.FromResult("Thailand"));
            var addressService = new AddressTransformerService(countryServiceMock.Object);

            var customerContactMapper = new MapperBuilder<CustomerEntity, ContactDto>()
                .For(to => to.First,          from => from.FirstName)
                .For(to => to.Last,           from => from.Surname)
                .For(to => to.PhoneNumber,    from => PhoneNumberFormatter.ToInternational(from.PhoneNumber))
                .For(to => to.HomeAddress,    from => addressService.Transform(from.HomeAddress))
                .For(to => to.OtherAddresses, from => Task.WhenAll(addressService.Transform(from.BusinessAddress), addressService.Transform(from.ShippingAddress)))
                .Build();

            var customerMapper = new MapperBuilder<CustomerEntity, CustomerDto>()
                .For(to => to.DateJoined, from => from.DateJoined)
                .For(to => to.CustomerId, from => from.CustomerNumber.ToString())
                .For(to => to.Contact,    customerContactMapper)
                .Build();

            var mapped = await customerMapper(_customerEntity);

            Assert.AreEqual("12345", mapped.CustomerId);
            Assert.AreEqual(_customerEntity.DateJoined, mapped.DateJoined);
            Assert.AreEqual("Thailand", mapped.Contact.OtherAddresses.First().CountryName);
            Assert.AreEqual(_customerEntity.ShippingAddress.Street, mapped.Contact.OtherAddresses.ElementAt(1).Street);
            Assert.AreEqual("+66971143378", mapped.Contact.PhoneNumber);
        }
    }
}