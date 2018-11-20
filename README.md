# Structured Mapper

## Motivation
- A large portion of the Agoda front end website is basically a huge mapping engine. So, make mapping the heart of the application, right at the HTTP entry point.
- Untangle our endless layers of mappers and services.
- Make it easy to reason about where a mapped value came from.
- Make it easier for new (and seasoned) developers to understand the application.

## Example
Here is a complete example taken from the [`CustomerDtoService`](/StructuredMapper.Test.Api/Customers/CustomerDtoService.cs) in the [StructuredMapper.Test.Api](/StructuredMapper.Test.Api) project, It shows Structured Mapping from a [`Customer`](/StructuredMapper.BL/Customers/Customer.cs) to a [`CustomerDto`](/StructuredMapper.Test.Api/Customers/CustomerDto.cs). This would be called directly from the [controller](/StructuredMapper.Test.Api/Controllers/CustomersController.cs).

```c#
public async Task<CustomerDto> GetById(int id)
{
    var customerContactMapper = new MapperBuilder<Customer, ContactDto>()
        .For(to => to.First,          from => from.FirstName) // source member access
        .For(to => to.Last,           from => from.Surname)
        .For(to => to.PhoneNumber,    from => PhoneNumberFormatter.ToInternational(from.PhoneNumber, from.HomeAddress.CountryId)) // static method
        .For(to => to.HomeAddress,    from => _addressDtoService.Transform(from.HomeAddress)) // async service call
        .For(to => to.OtherAddresses, from => 
            Task.WhenAll(_addressDtoService.Transform(from.BusinessAddress), _addressDtoService.Transform(from.ShippingAddress)))
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
```

## "IoM" (Inversion of Mapping)
*Mappers call services. Services never call mappers.*
 
Rather than services receiving, and arbitrarily mutating, the target object using mappers (which then gets passed to more services, which themselves call more mappers, etc etc), a Structured Mapper mapper clearly declares how each property is to be calculated, one by one.

Instead of an almost infinitely branching hierarchy of methods that the mapped object must negotiate, it travels "straight down" through a Structured Mapper - or a composition of such mappers - mapping one property at a time.

## Advantages of Structured Mapping
- All mappings are declared in one place (or at least using one paradigm), making it trivial to see how we arrived at the mapped value.
- We don't create the mapped object ourselves, or even have access to it as it is being mapped. This means we can no longer arbitrarily set its properties in random places throughout the code.
- Ideally, this means a property is mapped in one place, and one place only.
- A mapping function has access only to the source object, plus any dependencies it requires to perform its task. Each mapping function cannot read from or write to the target object's properties directly. It can only return the mapped value, which will be automatically be set to the target property by the Structured Mapper.
- This enforces discipline, both in the mappings and the services, making it much harder to accidentally (or purposefully) depend upon - or indeed overwrite - a mapped property. This prevents hard to find bugs, where our mapped value is overwritten somewhere else. Such problems can require hours of careful step-through debugging to diagnose.
- A Structured Mapper produces a function that can be passed into another such mapper. This makes them composable, and therefore reusable.
- We can use the same paradigm to map a user request to a backend request as we can to map the backend response back to a user response. Used correctly, this results in an elegant symmetry that is easy to comprehend.

## Mapping to the target object
The target property that will receive the mapped value is described with an `Expression`. Both top level and nested properties are supported:
- `to => to.FirstName`
- `to => from.Contact.FirstName`    

The latter is discouraged however, as unless the nested property (here `Contact`) is particularly trivial, it is probably better to give it its own mapper and compose it at the top level.

## Mapping from the source object
Each target property can by mapped from one of the following:
- A literal value:
    - `"MyValue"`
- A simple source property:
    - `from => from.GivenName`
- A source property transformed inline, good for trivial mappings:
    - `from => from.CustomerId.ToString()`
- Any method that returns an object of the correct type:
    - `from => PriceFormatter.Format(from.Price, from.CurrencyCode) // synchronous static method call`
    - `from => _countryService.GetCountryName(from.CountryId)       // async service call`
    - Mapping methods can be synchronous or asynchronous. The latter will be transparently run concurrently, potentially improving performance.

## The [`MapperBuilder`](/StructuredMapper/MapperBuilder.cs)
Mappers are created with a builder, which returns a plain old function. The resulting mapping function can be either: 
- asynchronous by calling `Build()`
    - returns `Task<Func<TSource, TTarget>>`
- or synchronous, by calling `BuildSync()` (only if no asynchronous mappers have been declared)
    - returns `Func<TSource, TTarget>`

Example:

```c#
var mapper = new MapperBuilder<Customer, CustomerDto>().For(...);

var asyncMapper = mapper.Build();
var customerDto = await mapper(customer);

var syncMapper = mapper.BuildSync();
var customerDto = mapper(customer);
```

## Race conditions
A [`MapperBuilder`](/StructuredMapper/MapperBuilder.cs) will throw if a property is mapped more than once. However, it cannot check that other mappers do not attempt to map the same property. As long as each property is mapped once and once only, there should be no race conditions. Should two composed mappers attempt to map the same property, the last one to complete wins. For synchronous mappers, this will be the last declared in the chain. For asynchronous mappers, all bets are off. Remapping the same property in composed mapping functions should therefore be avoided.
 
## Performance
Building a mapper is relatively slow (see [MapperBuilderPerformanceTests.cs](/StructuredMapper.Test.Performance/MapperBuilderPerformanceTests.cs)) as each setter expression must be compiled to a lambda. As a performance optimization, compilation is lazy, so will only occur the first time the property is actually mapped. Mapper build time increases linearly for each mapped property.

In performance testing on a trivial mapper of 2 properties, averaged over 1,000,000 iterations:
- To build the mapper each time it is required takes ~0.36ms per mapping.
- But to build once then reuse the mapper takes only 0.0013ms per mapping.
In this case it's about 300 times quicker to reuse a mapper than rebuild it each time, so it is therefore important to build a mapper only once and reuse the resulting mapping function.

## Live example
- In either or VS or Rider, debug the `StructuredMapper.Test.Api` run configuration. 
- A page will open displaying the serialized JSON result of a mapping from [`Customer`](/StructuredMapper.BL/Customers/Customer.cs) to [`CustomerDto`](/StructuredMapper.Test.Api/Customers/CustomerDto.cs). Open those two files to see what was accomplished.
- Open [CustomerController.cs](/StructuredMapper/blob/master/StructuredMapper.Test.Api/Controllers/CustomersController.cs) and trace your way through the simple application.
- Change the URL parameter from 1 to another number to see this reflected in the resulting mapped `CustomerId` property of the [`CustomerDto`](/StructuredMapper.Test.Api/Customers/CustomerDto.cs).
- The values of the [`CustomerDto`](/StructuredMapper.Test.Api/Customers/CustomerDto.cs) source object are hard coded for the purposes of this demo project, as are all dependencies. Obviously, in a real application they would be fetched from the backend or injected respectively.

## Tests
- See the [StructuredMapper.Test](/StructuredMapper.Test) project.
