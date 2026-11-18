using System;
using System.Linq;
using FluentValidation;
using Fdw.Validation;

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
    }
}
