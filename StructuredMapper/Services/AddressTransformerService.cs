using System.Threading.Tasks;
using StructuredMapper.Models;

namespace StructuredMapper.Services
{
    public class AddressTransformerService
    {
        private readonly ICountryService _countryService;

        public AddressTransformerService(ICountryService countryService)
        {
            _countryService = countryService;
        }

        public async Task<AddressDto> Transform(AddressEntity addressEntity)
        {
            return new AddressDto
            {
                Street = addressEntity.Street,
                Area = addressEntity.Area,
                State = addressEntity.Province,
                Postcode = addressEntity.Zipcode,
                CountryName = await _countryService.GetCountryName(addressEntity.CountryId),
            };
        }
    }
}