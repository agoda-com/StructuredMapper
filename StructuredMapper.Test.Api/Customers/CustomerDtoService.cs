using System.Globalization;
using System.Threading.Tasks;
using StructuredMapper.BL.Customers;
using StructuredMapper.BL.Geography;
using StructuredMapper.Test.Api.Helpers;

namespace StructuredMapper.Test.Api.Customers
{
    public class CustomerDtoService
    {
        private readonly ICustomerService _customerService;
        private readonly AddressDtoService _addressDtoSvc;

        public CustomerDtoService()
        {
            // should be injected
            _customerService = new CustomerService();
            _addressDtoSvc = new AddressDtoService();
        }
        
        public async Task<CustomerDto> GetById(string id)
        {
            var customerContactMapper = new MapperBuilder<Customer, ContactDto>()
                .For(to => to.First,          from => from.FirstName) // source member access
                .For(to => to.Last,           from => from.Surname)
                .For(to => to.PhoneNumber,    from => Formatter.ToInternational(from.PhoneNumber, from.Address.CountryId)) // static method
                .For(to => to.HomeAddress,    from => _addressDtoSvc.Transform(from.Address)) // async service call
                .For(to => to.OtherAddresses, from => 
                    Task.WhenAll(_addressDtoSvc.Transform(from.BusinessAddress), _addressDtoSvc.Transform(from.ShippingAddress)))
                .Build();

            var customerMapper = new MapperBuilder<Customer, CustomerDto>()
                .For(to => to.CustomerNumber, "123") // literal
                .For(to => to.DateJoined,     from => from.DateJoined.ToString(new CultureInfo("th-th"))) // inline transformation
                .For(to => to.Contact,        customerContactMapper) // composition with the mapper above
                .Build();

            var customer = await _customerService.GetById(id); // defined in StructuredMapper.BL
            var customerDto = await customerMapper(customer);
            return customerDto;
        }
    }
}