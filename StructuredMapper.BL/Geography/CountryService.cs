using System;
using System.Threading.Tasks;

namespace StructuredMapper.BL.Geography
{
    public class CountryService : ICountryService
    {
        public CountryService()
        {
            // inject CMS here
        }
        
        public Task<string> GetCountryName(int countryId)
        {
            // look up name for countryId from somewhere

            switch (countryId)
            {
                case 1: return Task.FromResult("Thailand");
                case 2: return Task.FromResult("UK");
                default: throw new NotImplementedException(countryId.ToString());
            }
        }
    }
}