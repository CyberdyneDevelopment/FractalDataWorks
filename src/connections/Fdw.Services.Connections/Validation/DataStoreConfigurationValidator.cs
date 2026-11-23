using System;
using System.Linq;
using FluentValidation;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Builders;
using Fdw.Validation;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections.Validation;

/// <summary>
/// Validator for <see cref="DataStoreImplementationConfiguration"/>.
/// </summary>
public sealed class DataStoreConfigurationValidator : FdwConfigurationValidator<DataStoreImplementationConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataStoreConfigurationValidator"/> class.
    /// </summary>
    public DataStoreConfigurationValidator()
    {
        RuleFor(x => x.Name)
            .IsValidName(200);

        RuleFor(x => x.ConnectionId)
            .IsNotEmpty();

        When(x => x.Description is not null, () =>
        {
            RuleFor(x => x.Description!)
                .IsSafeString(1000);
        });

        // The store's working areas (FDW-6394): each a valid name, and no name stated twice whatever its case.
        RuleForEach(x => x.WorkingPathRows)
            .SetValidator(new DataStoreWorkingPathConfigurationValidator());

        RuleFor(x => x.WorkingPathRows)
            .Must(rows => rows.Select(row => row.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() == rows.Count)
            .WithMessage("A working area is named once; two rows state the same name");

        // A container is checked when its store is written, by the rule the builders ask too (FDW-6428): it states a type, the type is registered, and the store's
        // data store type holds it. A store whose data store type is not registered is refused elsewhere (the selector), so only the type the container states is checked then.
        RuleFor(x => x)
            .Custom(ContainerTypeProblems);
    }

    private static void ContainerTypeProblems(DataStoreImplementationConfiguration store, ValidationContext<DataStoreImplementationConfiguration> context)
    {
        var storeType = DataStoreTypes.ByName(store.Implementation);
        var storeTypeKnown = !ReferenceEquals(storeType, DataStoreTypes.NotFound);

        for (var path = 0; path < store.Paths.Count; path++)
        {
            for (var container = 0; container < store.Paths[path].Containers.Count; container++)
            {
                var configuration = store.Paths[path].Containers[container];
                var refused = storeTypeKnown
                    ? DataContainerTypeRule.Check(configuration, storeType, NullLogger.Instance)
                    : DataContainerTypeRule.Stated(configuration, NullLogger.Instance);

                if (refused.IsFailure)
                {
                    foreach (var message in refused.Messages)
                        context.AddFailure($"Paths[{path}].Containers[{container}].TypeId", message.Message);
                }
            }
        }
    }
}
