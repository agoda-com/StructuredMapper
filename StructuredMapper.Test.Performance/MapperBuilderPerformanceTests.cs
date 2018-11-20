using System;
using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;
using StructuredMapper.BL.Customers;
using StructuredMapper.Test.Api.Customers;

namespace StructuredMapper.Test.Performance
{
    public class MapperBuilderPerformanceTests
    {
        private const int ITERATIONS = 100_000;
        
        private readonly Customer _customer = new Customer
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

        private Stopwatch _stopwatch;

        [SetUp]
        public void SetUp()
        {
            _stopwatch = new Stopwatch();
        }
       
        [Test]
        public async Task Map_WhenAsyncMapperIsReused()
        {
            _stopwatch.Start();
            
            var mapper = new MapperBuilder<Customer, ContactDto>()
                .For(to => to.First, "Mikey")
                .Build();
            
            for (var i = 0; i < ITERATIONS; i++)
            {    
                var mapped = await mapper(_customer);
            }

            _stopwatch.Stop();
            PrintResults();
        }

        [Test]
        public async Task Map_WhenAsyncMapperIsRecreated()
        {
            _stopwatch.Start();
            
            for (var i = 0; i < ITERATIONS; i++)
            {
                var mapper = new MapperBuilder<Customer, ContactDto>()
                    .For(to => to.First, "Mike")
                    .For(to => to.Last,  "Chamberlain")
                    .Build();
                var mapped = await mapper(_customer);
            }
            
            _stopwatch.Stop();
            PrintResults();
        }
        
        [Test]
        public void Map_WhenSynchronousMapperIsReused()
        {
            _stopwatch.Start();
            
            var mapper = new MapperBuilder<Customer, ContactDto>()
                .For(to => to.First, "Mikey")
                .For(to => to.Last,  "Chamberlain")
                .BuildSync();
            
            for (var i = 0; i < ITERATIONS; i++)
            {    
                var mapped = mapper(_customer);
            }

            _stopwatch.Stop();
            PrintResults();
        }

        [Test]
        public void Map_WhenSynchrousMapperIsRecreated()
        {
            _stopwatch.Start();
            
            for (var i = 0; i < ITERATIONS; i++)
            {
                var mapper = new MapperBuilder<Customer, ContactDto>()
                    .For(to => to.First, "Mike")
                    .For(to => to.Last,  "Chamberlain")
                    .Build();
                var mapped = mapper(_customer);
            }
            
            _stopwatch.Stop();
            PrintResults();
        }
        
        private void PrintResults()
        {
            Console.WriteLine(
                $"{_stopwatch.ElapsedMilliseconds}ms = " +              
                $"{(double) _stopwatch.ElapsedMilliseconds / (double) ITERATIONS}ms per map.");
        }
    }
}