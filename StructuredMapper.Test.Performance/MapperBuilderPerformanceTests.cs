using System;
using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;
using StructuredMapper.BL.Customers;
using StructuredMapper.BL.Geography;
using StructuredMapper.Test.Api.Customers;

namespace StructuredMapper.Test.Performance
{
    /// <summary>
    /// Warning! These tests take a while to run, and include a warm up phase of 60 seconds before you'll see anything
    /// happening to get the CPU good and hot, hopefully mitigating any thermal throttling that might skew the results.
    /// </summary>
    public class MapperBuilderPerformanceTests
    {
        private const int ITERATIONS = 1_000_000;
        
        private readonly Customer _customer = new Customer
        {
            FirstName = "Mike",
            Surname = "Chamberlain",
        };

        private Stopwatch _stopwatch;

        public MapperBuilderPerformanceTests()
        {
            // rev up the engines
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            while (true)
            {
                if (stopwatch.Elapsed.TotalSeconds > 60)
                {
                    break;
                }
            }
        }
        
        [SetUp]
        public void SetUp()
        {
            _stopwatch = new Stopwatch();
        }
       
        /// <remarks>
        /// 1255ms = 0.001255ms per map.
        /// </remarks>
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

        /// <remarks>
        /// 363343ms = 0.363343ms per map.
        /// </remarks>
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
        
        /// <remarks>
        /// 984ms = 0.000984ms per map.
        /// </remarks>
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
        
        /// <remarks>
        /// 363282ms = 0.363282ms per map.
        /// </remarks>
        [Test]
        public void Map_WhenSynchronousMapperIsRecreated()
        {
            _stopwatch.Start();
            
            for (var i = 0; i < ITERATIONS; i++)
            {
                var mapper = new MapperBuilder<Customer, ContactDto>()
                    .For(to => to.First, "Mike")
                    .For(to => to.Last,  "Chamberlain")
                    .BuildSync();
                var mapped = mapper(_customer);
            }
            
            _stopwatch.Stop();
            PrintResults();
        }
        
        private void PrintResults()
        {
            Console.WriteLine(
                $"{ITERATIONS} iterations took {_stopwatch.ElapsedMilliseconds}ms @ " +              
                $"{(double) _stopwatch.ElapsedMilliseconds / (double) ITERATIONS}ms per map.");
        }
    }
}