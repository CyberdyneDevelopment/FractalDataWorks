using Fdw.Collections;
using Fdw.Collections.Attributes;
using $namespace$.$serviceName$.Abstractions;

namespace $namespace$.$serviceName$;

/// <summary>
/// ServiceTypeCollection for all $serviceName$ service implementations.
/// </summary>
[ServiceTypeCollection(
    typeof($serviceName$TypeBase<,,>),
    typeof(I$serviceName$Type),
    typeof($serviceName$Types),
    GenerateProvider = true,
    ProviderType = typeof(Default$serviceName$Provider),
    ProviderInterface = typeof(I$serviceName$Provider))]
public partial class $serviceName$Types
    : ServiceTypeCollectionBase<
        $serviceName$TypeBase<I$serviceName$Service, I$serviceName$Factory, I$serviceName$Configuration>,
        I$serviceName$Type>
{
}
