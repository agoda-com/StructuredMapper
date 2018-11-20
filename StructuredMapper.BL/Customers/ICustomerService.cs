using System.Threading.Tasks;

namespace StructuredMapper.BL.Customers
{
    public interface ICustomerService
    {
        Task<Customer> GetById(int id);
    }
}