using System.Threading.Tasks;

namespace StructuredMapper.BL.Countries
{
    public interface ICountryService
    {
        Task<string> GetCountryName(int countryId);
    }
}