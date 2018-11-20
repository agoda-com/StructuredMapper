using System.Threading.Tasks;

namespace StructuredMapper.BL.Geography
{
    public interface ICountryService
    {
        Task<string> GetCountryName(int countryId);
    }
}