using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Configuration;
using FluentValidation;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// Validator for DataFieldConfiguration instances.
/// </summary>
public sealed class DataFieldConfigurationValidator : AbstractValidator<DataFieldConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataFieldConfigurationValidator"/> class.
    /// </summary>
    public DataFieldConfigurationValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Field name is required")
            .MaximumLength(50)
            .WithMessage("Field name must not exceed 50 characters");

        RuleFor(x => x.TypeName)
            .NotEmpty()
            .WithMessage("Field type name is required")
            .Must(BeValidTypeName)
            .WithMessage("Field type name must be a valid .NET type name");

        RuleFor(x => x.MaxLength)
            .GreaterThan(0)
            .When(x => x.MaxLength.HasValue)
            .WithMessage("Max length must be greater than zero when specified");
    }

    private static bool BeValidTypeName(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName)) return false;

        // List of common .NET types that are valid
        var commonTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "System.String", "System.Int32", "System.Int64", "System.DateTime", "System.Decimal",
            "System.Double", "System.Boolean", "System.Guid", "System.Byte[]", "string", "int",
            "long", "DateTime", "decimal", "double", "bool", "Guid", "byte[]"
        };

        return commonTypes.Contains(typeName) || (!typeName.Contains(" ") && typeName.Contains("."));
    }
}