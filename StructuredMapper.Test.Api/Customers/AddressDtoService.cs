using System;
using System.Diagnostics;
using System.Threading.Tasks;
using StructuredMapper.BL.Customers;
using StructuredMapper.BL.Geography;

namespace StructuredMapper.Test.Api.Customers
{
    /// <summary>
    /// As this mapping is so simple, it would be better to create it using a MapperBuilder too. It is used here to
    /// demonstrate support for services. Imagine the logic is far more complex, requiring multiple dependencies.
    /// </summary>
    public class AddressDtoService
    {
        private readonly ICountryService _countryService;
        private readonly Stopwatch _stopwatch;
        private int _count;

        public AddressDtoService()
        {
            // should be injected
            _countryService = new CountryService();
            
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
            _count = 0;
        }

        public async Task<AddressDto> Transform(Address address)
        {   
            var dto = new AddressDto
            {
                Street = address.Street,
                Area = address.Area,
                State = address.Province,
                Postcode = address.Zipcode,
                // if this class was replaced with a MapperBuilder, this call would also run concurrently
                CountryName = await _countryService.GetCountryName(address.CountryId),
            };
            
            // proving that async mappers run concurrently
            await Task.Delay(1000);
            Console.WriteLine($"Got address {++_count} after {_stopwatch.ElapsedMilliseconds}ms");
            
            return dto;
        }
    }
}