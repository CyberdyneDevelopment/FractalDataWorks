# Testing TypeCollections

This guide explains how to write tests for TypeCollection options. The key principle: **test the logic of your options, not the collection infrastructure.**

## The Wrong Approach

Do not test that the source generator registered your options correctly:

```csharp
// WRONG - tests the source generator, not your code
[Fact]
public void FilterOperatorsContainsEqual()
{
    var op = FilterOperators.ByName("Equal");
    op.ShouldNotBeNull();
    op.Name.ShouldBe("Equal");
}

// WRONG - tests registration count
[Fact]
public void FilterOperatorsHasSevenItems()
{
    FilterOperators.All().Count.ShouldBe(7);
}
```

These tests verify that the `Collections.SourceGenerators` package works. That package has its own test suite. Your tests should verify that **your option implementations behave correctly**.

## The Right Approach

Instantiate options directly and test their business logic:

```csharp
// CORRECT - tests the actual behavior of CashPayment
[Fact]
public void CashPaymentCalculatesTotalWithNoFees()
{
    // Arrange
    var sut = new CashPayment();

    // Act
    var total = sut.CalculateTotal(100m);

    // Assert
    total.ShouldBe(100m);
}

// CORRECT - tests CreditCardPayment applies its fee
[Fact]
public void CreditCardPaymentAppliesProcessingFee()
{
    // Arrange
    var sut = new CreditCardPayment();

    // Act
    var total = sut.CalculateTotal(100m);

    // Assert
    total.ShouldBe(102.5m); // 2.5% fee
}
```

## What to Test

TypeOptions are classes. Test them like any other class:

| What to Test | Example |
|--------------|---------|
| Constructor sets properties correctly | `sut.FeePercentage.ShouldBe(0m)` |
| Methods return correct results | `sut.Translate(command).IsSuccess.ShouldBeTrue()` |
| Edge cases and boundaries | Empty input, null values, large datasets |
| Error conditions | Invalid arguments, missing fields |
| Behavioral differences between options | Inner join vs left join logic |

### Example: Testing a Translator Option

The MsSql translators are TypeOptions that contain SQL generation logic. Tests instantiate them directly:

```csharp
public sealed class MsSqlDeleteTranslatorTests
{
    // Instantiate the option directly - no collection lookup needed
    private readonly MsSqlDeleteTranslator _sut = new();

    [Fact]
    public async Task TranslateGeneratesDeleteWithWhereClause()
    {
        // Arrange
        var container = CreateContainer("Customers");
        var command = CreateDeleteCommand(filter: "Id = 1");

        // Act
        var result = await _sut.Translate(
            command, container, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("DELETE FROM");
        result.Value.CommandText.ShouldContain("WHERE");
    }
}
```

### Example: Testing a Configuration Option

```csharp
public sealed class MsSqlConnectionTypeTests
{
    private readonly MsSqlConnectionType _sut = new();

    [Fact]
    public void NameIsMsSql()
    {
        _sut.Name.ShouldBe("MsSql");
    }

    [Fact]
    public void ConfigureBindsOptionsSection()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Connections:MsSql:0:Server"] = "localhost"
            })
            .Build();

        // Act
        _sut.Configure(services, config, NullLoggerFactory.Instance);

        // Assert - verify the options binding was registered
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptionsMonitor<List<MsSqlConnectionConfiguration>>>();
        options.CurrentValue.ShouldNotBeEmpty();
    }
}
```

## RestrictToCurrentCompilation

Many TypeOptions use `RestrictToCurrentCompilation = true`:

```csharp
[TypeOption(typeof(ContainerTypes), "Endpoint", RestrictToCurrentCompilation = true)]
public sealed class EndpointContainerType : ContainerTypeBase { }
```

This flag prevents the `Registration.SourceGenerators` from generating module initializers for this type in downstream assemblies. This means `ContainerTypes.ByName("Endpoint")` will **not work** in a test project -- the type is not registered in the test assembly's collection.

This is by design. It reinforces the correct testing approach: instantiate directly.

```csharp
// Will throw NullReferenceException or return Empty in test projects
var type = ContainerTypes.ByName("Endpoint"); // DON'T

// Works correctly - tests the actual type
var type = new EndpointContainerType(); // DO
```

## When Collection Lookups Are Unavoidable

Sometimes the code under test internally calls `Collection.ByName()`. For example, a translator might resolve join types:

```csharp
// Inside MsSqlCompoundQueryTranslator.Translate():
var joinType = JoinTypes.ByName(joinConfig.Type);
```

In this case, the collection must be initialized before your test runs. Use an xUnit `ICollectionFixture`:

```csharp
// 1. Create a fixture that forces initialization
public sealed class DataMsSqlTypeCollectionFixture
{
    public DataMsSqlTypeCollectionFixture()
    {
        // Force the static constructors to run,
        // populating the FrozenDictionary lookups
        _ = JoinTypes.All();
        _ = FilterOperators.All();
        _ = SortDirections.All();
    }
}

// 2. Define a collection
[CollectionDefinition(nameof(DataMsSqlTestCollection))]
public sealed class DataMsSqlTestCollection
    : ICollectionFixture<DataMsSqlTypeCollectionFixture> { }

// 3. Apply to test classes that need it
[Collection(nameof(DataMsSqlTestCollection))]
public sealed class MsSqlCompoundQueryTranslatorTests
{
    private readonly MsSqlCompoundQueryTranslator _sut = new();
    // ...
}
```

The fixture ensures the collection's static constructor runs before any test in that collection. This prevents race conditions when xUnit runs tests in parallel.

**Only use this pattern when the code under test calls collection lookups internally.** If you control the test setup, prefer direct instantiation.

## Test Naming

Follow the standard FDW test naming convention:

```
{MethodName}{Scenario}{ExpectedResult}
```

Examples for TypeOption tests:
- `CalculateTotalReturnsAmountWithNoFeesForCash`
- `TranslateGeneratesInsertWithParameters`
- `ConfigureBindsMsSqlOptionsSection`
- `RegisterServicesAddsFactoryToContainer`

## Summary

| Principle | Practice |
|-----------|----------|
| Test option logic, not registration | `new MyOption()` not `Collection.ByName("My")` |
| Instantiate directly | Constructor creates the instance without DI |
| Test behavioral differences | Each option's unique logic and edge cases |
| Use fixtures only when necessary | Only when SUT internally calls collection lookups |
| Follow AAA pattern | Arrange, Act, Assert with Shouldly |
