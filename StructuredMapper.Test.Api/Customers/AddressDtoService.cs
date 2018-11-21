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

            Transform = new MapperBuilder<Address, AddressDto>()
                .For(to => to.Street, from => from.Street)
                .For(to => to.Area, from => from.Area)
                .For(to => to.State, from => from.Province)
                .For(to => to.Postcode, from => from.Zipcode)
                .For(to => to.CountryName, from => _countryService.GetCountryName(from.CountryId))
                .Build();
        }

        public Func<Address, Task<AddressDto>> Transform { get; }
    }
}