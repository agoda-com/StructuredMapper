using System;
using System.Threading.Tasks;

namespace StructuredMapper.BL.Countries
{
    public class CountryService : ICountryService
    {
        public Task<string> GetCountryName(int countryId)
        {
            // look up name for countryId from somewhere here
            //...

            switch (countryId)
            {
                case 1: return Task.FromResult("Thailand");
                case 2: return Task.FromResult("UK");
                default: throw new NotImplementedException(countryId.ToString());
            }
        }
    }
}