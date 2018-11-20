using System;
using System.Diagnostics;
using StructuredMapper.BL.Countries;
using System.Threading.Tasks;
using StructuredMapper.BL.Customers;

namespace StructuredMapper.Test.Api.Customers
{
    public class AddressDtoService
    {
        private readonly ICountryService _countryService;
        private readonly Stopwatch _stopwatch;

        public AddressDtoService(ICountryService countryService)
        {
            _countryService = countryService;
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
        }

        public async Task<AddressDto> Transform(Address address)
        {
            await Task.Delay(1000);
            
            var dto = new AddressDto
            {
                Street = address.Street,
                Area = address.Area,
                State = address.Province,
                Postcode = address.Zipcode,
                CountryName = await _countryService.GetCountryName(address.CountryId),
            };
            
            Console.WriteLine($"Got address after {_stopwatch.ElapsedMilliseconds}ms");
            
            return dto;
        }
    }
}