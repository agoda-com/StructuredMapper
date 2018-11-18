using System;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using StructuredMapper.Test.Models;
using StructuredMapper.Test.Services;

namespace StructuredMapper.Test
{
    public class MapperBuilderTests
    {
        private CustomerEntity _customerEntity;
        private AddressEntity _addressEntity;
        private AddressTransformerService _addressService;
        private RecursiveEntity _recursiveEntity;

        [SetUp]
        public void Setup()
        {
            var countryServiceMock = new Mock<ICountryService>();
            countryServiceMock.Setup(s => s.GetCountryName(It.IsAny<int>())).Returns(Task.FromResult("Thailand"));
            _addressService = new AddressTransformerService(countryServiceMock.Object);

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
            
            _addressEntity = new AddressEntity
            {
                Street = "123 Fake Street",
                Area = "Area",
                Province = "Province",
                Zipcode = "0000",
                CountryId = 1
            };

            _recursiveEntity = new RecursiveEntity
            {
                Recursive = new RecursiveEntity()
            };
        }

        [Test]
        public void MapperBuilder_WithDuplicateProperties_Throws()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                new MapperBuilder<CustomerEntity, CustomerDto>()
                    .ForProperty(to => to.DateJoined, from => from.DateJoined)
                    .ForProperty(to => to.DateJoined, from => from.DateJoined);
            });
        }

        [Test]
        public void MapperBuilder_WithDuplicateObjects_Throws()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                new MapperBuilder<CustomerEntity, CustomerDto>()
                    .ForObject(to => to, from => new CustomerDto())
                    .ForObject(to => to, from => new CustomerDto());
            });
        }
        
        [Test]
        public void MapperBuilder_ForPropertyWithNonMemberAccessTypeExpression_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new MapperBuilder<RecursiveEntity, RecursiveDto>()
                    .ForProperty(to => to, from => new RecursiveDto());
            });
        }
        
        [Test]
        public void MapperBuilder_ForObjectWithNonParameterTypeExpression_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new MapperBuilder<RecursiveEntity, RecursiveDto>()
                    .ForObject(to => to.Recursive, from => new RecursiveDto());
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
        public async Task Mapper_ForSimpleProperty_Works()
        {
            var customerContactMapper = new MapperBuilder<CustomerEntity, ContactDto>()
                .ForProperty(to => to.First, from => from.FirstName)
                .Build();

            var mapped = await customerContactMapper(_customerEntity);
            
            Assert.AreEqual("Mike", mapped.First);
        }
        
        [Test]
        public async Task Mapper_ForComplexProperty_Works()
        {
            var customerMapper = new MapperBuilder<CustomerEntity, CustomerDto>()
                .ForProperty(to => to.Contact.First, from => from.FirstName)
                .Build();

            var mapped = await customerMapper(_customerEntity);
            
            Assert.AreEqual("Mike", mapped.Contact.First);
        }
        
        [Test]
        public async Task Mapper_ForPropertyWithStaticMethod_Works()
        {
            var customerContactMapper = new MapperBuilder<CustomerEntity, ContactDto>()
                .ForProperty(to => to.PhoneNumber, from => PhoneNumberFormatter.ToInternational(from.PhoneNumber))
                .Build();

            var mapped = await customerContactMapper(_customerEntity);
            
            Assert.AreEqual("+66971143378", mapped.PhoneNumber);
        }
        
        [Test]
        public async Task Mapper_ForAsyncProperty_Works()
        {   
            var customerContactMapper = new MapperBuilder<CustomerEntity, ContactDto>()
                .ForProperty(to => to.HomeAddress, from => _addressService.Transform(from.HomeAddress))
                .Build();

            var mapped = await customerContactMapper(_customerEntity);
            
            Assert.AreEqual("3 Some Lane", mapped.HomeAddress.Street);
            Assert.AreEqual("Thailand", mapped.HomeAddress.CountryName);
        }
        
        [Test]
        public async Task Mapper_WithComposition_Works()
        {
            var customerContactMapper = new MapperBuilder<CustomerEntity, ContactDto>()
                .ForProperty(to => to.PhoneNumber, from => PhoneNumberFormatter.ToInternational(from.PhoneNumber))
                .Build();

            var customerMapper = new MapperBuilder<CustomerEntity, CustomerDto>()
                .ForProperty(to => to.Contact, customerContactMapper)
                .Build();

            var mapped = await customerMapper(_customerEntity);

            Assert.AreEqual("+66971143378", mapped.Contact.PhoneNumber);
        }
        
        [Test]
        public async Task Mapper_ForObject_Works()
        {
            var addressMapper = new MapperBuilder<AddressEntity, AddressDto>()
                .ForObject(to => to, from => new AddressDto { Street = from.Street })
                .Build();

            var mapped = await addressMapper(_addressEntity);

            Assert.AreEqual(_addressEntity.Street, mapped.Street);
        }
        
        [Test]
        public async Task Mapper_ForAsyncObject_Works()
        {
            var addressMapper = new MapperBuilder<AddressEntity, AddressDto>()
                .ForObject(to => to, from => _addressService.Transform(from))
                .Build();

            var mapped = await addressMapper(_addressEntity);

            Assert.AreEqual(_addressEntity.Street, mapped.Street);
        }
        
        [Test]
        public async Task Mapper_ForRecursiveClass_Works()
        {
            var addressMapper = new MapperBuilder<RecursiveEntity, RecursiveDto>()
                .ForProperty(to => to.Recursive, from => new RecursiveDto())
                .Build();

            var mapped = await addressMapper(_recursiveEntity);

            Assert.IsNotNull(mapped.Recursive);
        }
        
        [Test]
        public async Task Mapper_WithEverything_Works()
        {                
            var customerContactMapper = new MapperBuilder<CustomerEntity, ContactDto>()
                .ForProperty(to => to.First,          from => from.FirstName)
                .ForProperty(to => to.Last,           from => from.Surname)
                .ForProperty(to => to.PhoneNumber,    from => PhoneNumberFormatter.ToInternational(from.PhoneNumber))
                .ForProperty(to => to.HomeAddress,    from => _addressService.Transform(from.HomeAddress))
                .ForProperty(to => to.OtherAddresses, from => Task.WhenAll(_addressService.Transform(from.BusinessAddress), _addressService.Transform(from.ShippingAddress)))
                .Build();

            var customerMapper = new MapperBuilder<CustomerEntity, CustomerDto>()
                .ForProperty(to => to.DateJoined, from => from.DateJoined)
                .ForProperty(to => to.CustomerId, from => from.CustomerNumber.ToString())
                .ForProperty(to => to.Contact,    customerContactMapper)
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