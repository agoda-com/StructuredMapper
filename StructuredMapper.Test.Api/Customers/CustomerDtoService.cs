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
        private readonly AddressDtoService _addressDtoService;

        public CustomerDtoService()
        {
            // should be injected
            _customerService = new CustomerService();
            _addressDtoService = new AddressDtoService();
        }
        
        public async Task<CustomerDto> GetById(int id)
        {
            var customerContactMapper = new MapperBuilder<Customer, ContactDto>()
                .For(to => to.First,          from => from.FirstName)
                .For(to => to.Last,           from => from.Surname)
                .For(to => to.PhoneNumber,    from => PhoneNumberFormatter.ToInternational(from.PhoneNumber, from.HomeAddress.CountryId))
                .For(to => to.HomeAddress,    from => _addressDtoService.Transform(from.HomeAddress))
                .For(to => to.OtherAddresses, from => Task.WhenAll(_addressDtoService.Transform(from.BusinessAddress), _addressDtoService.Transform(from.ShippingAddress)))
                .Build();

            var customerMapper = new MapperBuilder<Customer, CustomerDto>()
                .For(to => to.CustomerId, id)
                .For(to => to.DateJoined, from => from.DateJoined.ToString(new CultureInfo("th-th")))
                .For(to => to.Contact,    customerContactMapper)
                .Build();

            var customer = await _customerService.GetById(id);
            return await customerMapper(customer);
        }
    }
}