## Motivation
- Dictator is basically a huge mapping engine.
- So make mapping the heart of the application.
- Untangles our endless layers of mappers.
- Makes it easy to reason about where a mapped value comes from.
- Makes it easier for new developers to understand the application.

## Example
```c#
var customerContactMapper = new MapperBuilder<Customer, ContactDto>()
    // from simple member access
    .For(to => to.First,          from => from.FirstName)
    .For(to => to.Last,           from => from.Surname)
    // retrieved from a static method
    .For(to => to.PhoneNumber,    from => PhoneNumberFormatter.ToInternational(from.PhoneNumber, from.HomeAddress.CountryId))
    // retrieved asynchronously from a service
    .For(to => to.HomeAddress,    from => _addressDtoService.Transform(from.HomeAddress))
    .For(to => to.OtherAddresses, from => Task.WhenAll(_addressDtoService.Transform(from.BusinessAddress), _addressDtoService.Transform(from.ShippingAddress)))
    .Build();

var customerMapper = new MapperBuilder<Customer, CustomerDto>()
    // specified literally 
    .For(to => to.CustomerId, id)
    // from a simple transformation
    .For(to => to.DateJoined, from => from.DateJoined.ToString(new CultureInfo("th-th")))
    // composed from the mapper defined above
    .For(to => to.Contact,    customerContactMapper)
    .Build();

var customer = await _customerService.GetById(id);
var customerDto = await customerMapper(customer);
```

## Advantages
"IoM" (inversion of mapping).
- All mappings are done in one place, making it easy to see where a mapped value comes from.
- The mapper itself defines how the mapped values are calculated, rather than the services arbitrarily mutating the object to be mapped.
- We don't create the mapped object ourselves, or have access to any of it as it is being mapped. This means we we don't pass it through various layers of services, merrily setting properties as we go. This prevents hard to find bugs where our mapped value is overwritten by someone else.
- Each mapper only has access to the property for which it it declared, ideally meaning property is mapped in one place and one place only.
- A mapper/transformer only has access to the source object, plus any dependencies it requires. It cannot read from or write to the target object's properties directly. It can only calculate the mapped value to be set to the target property.
- This enforces a disciplined approach, making it much harder to accidentally (or purposefully) depend upon or overwrite a target property.
- Mappers are composable, and therefore reusable.

## Target properties
The target property that will receive the mapped value is described with an `Expression`:
- A top level source property:
    - `to => to.FirstName`
- A nested property:
    - `to => from.Contact.FirstName`
    
The latter is discouraged however, as unless the `Contact` property is particularly trivial, it is probably better to define and compose a contact mapper.  

## Mapper types
Each target property can by mapped from one of the following:
- A literal value:
    - `"MyValue"`
- A simple source property:
    - `from => from.GivenName`
- A source property transformed inline, good for simple mappings:
    - `from => from.CustomerId.ToString()`
- From any method that returns an object of the correct type:
    - `from => PriceFormatter.Format(from.Price, from.CurrencyCode) // synchronous static method call`
    - `from => _countryService.GetCountryName(from.CountryId)       // async service call`
    - Mapping methods can be synchronous or asynchronous. The latter will be transparently run concurrently, potentially improving performance.

## The builder
- Mappers are created with a builder, which returns a plain old function that takes an object of the  source type and creates a mapped object of the target type.
- The resulting mapping function can be either: 
	- asynchronous by calling `Build()`
		- returns `Task<Func<TSource, TTarget>>`
	- or synchronous, by calling `BuildSync()` (only if no asynchronous mappers have been defined)
		- returns `Func<TSource, TTarget>`
- The builder will throw if a property is mapped more than once. However, it cannot check that other builders do not attempt to map the same property.

## Race conditions?
As long as each property is mapped only once, I believe no race conditions exist. Should two different mappers map attempt to map the same property, the last one to complete wins. For synchronous mappers this will be the one defined last in the chain. For asynchronous mappers all bets are off. Remapping the same property using different mappers should therefore be avoided at all costs.
 
## Performance
- Building a mapper is relatively slow (see [MapperBuilderPerformanceTests.cs]()) as each setter expression must be compiled to a lambda.
    - As a performance optimization, compilation is lazy, so will only occur the first time the property is actually mapped.
- Build time increases linearly for each mapped property, therefore it is important to build a mapper only once and reuse the resulting mapping function.
- For a trivial mapper of 2 properties, averaged over 1,000,000 iterations:
    - To build the mapper each time it is required takes ~0.36ms per mapping.
    - But to build once then reuse the mapper takes only 0.0013ms per mapping.
    - So in this case it's ~300 times quicker to reuse the mapper than rebuild it each time.

## Live example
- In either or VS or Rider, run the `StructuredMapper.Test.Api` run configuration. Open [CustomerController.cs]() and trace your way through the simple application.
- A page will open displaying the serialized JSON result of a mapping from `Customer` to `CustomerDto`. Open those two files to see what was accomplished.
- Change the URL parameter from 1 to another number to see this reflected in the resulting mapped `CustomerId` property of the `CustomerDto`.
- The values of the `Customer` source object are hard coded for the purposes of this demo project, as are all dependencies. Obviously, in a real application they would be injected.

## Tests
- A full test suite in the `StructuredMapper.Test` project.
