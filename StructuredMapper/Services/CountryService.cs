using System.Threading.Tasks;

namespace StructuredMapper.Services
{
    public interface ICountryService
    {
        Task<string> GetCountryName(int countryId);
    }
    
    public class CountryService : ICountryService
    {
        public Task<string> GetCountryName(int countryId)
        {
            // look up name for countryId from DB here
            //...
            
            return Task.FromResult("Thailand");
        }
    }
}