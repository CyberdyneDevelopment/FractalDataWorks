namespace Fdw.Collections;

/// <summary>
/// Base class for mutable ServiceType collections with runtime registration support.
/// The source generator adds implementation.
/// </summary>
/// <typeparam name="TBase">The base type for all options in this collection.</typeparam>
/// <typeparam name="TInterface">The interface that all options implement.</typeparam>
public abstract class MutableServiceTypeCollectionBase<TBase, TInterface>
    where TBase : class
    where TInterface : class
{
    // Source generator provides:
    // - Static properties for each ServiceTypeOption
    // - RegisterMember(TInterface) method for runtime additions
    // - Unregister(TInterface) method for runtime removal
    // - ById(TKey) method
    // - ByName(string) method
    // - All() method
    // - Empty sentinel
    // - Custom lookup methods from [TypeLookup] attributes
    // - Register(IServiceCollection, ILoggerFactory?) invoker over the swappable RegistrationMethod field
}
