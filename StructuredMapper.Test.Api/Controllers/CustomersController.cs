using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using StructuredMapper.Test.Api.Customers;

namespace StructuredMapper.Test.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomersController : ControllerBase
    {
        private readonly CustomerDtoService _customerDtoService;

        public CustomersController()
        {
            // should be injected
            _customerDtoService = new CustomerDtoService();
        }

        [HttpGet("{id}")]
        public async Task<JsonResult> Get(string id)
        {
            var customerDto = await _customerDtoService.GetById(id);
            return new JsonResult(customerDto);
        }
    }
}
