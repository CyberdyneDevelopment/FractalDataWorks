using Fdw.Configuration;
using Fdw.Data.Abstractions;
using FluentValidation;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// Validator for DataSetFieldConfiguration instances.
/// </summary>
/// <remarks>
/// A field's <see cref="DataSetFieldConfiguration.TypeName"/> is the name of a <see cref="DataTypes"/> option,
/// matched exactly (FDW-1159). A CLR type name such as <c>System.String</c> or a C# keyword such as <c>int</c> is refused.
/// </remarks>
public sealed class DataSetFieldConfigurationValidator : AbstractValidator<DataSetFieldConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataSetFieldConfigurationValidator"/> class.
    /// </summary>
    public DataSetFieldConfigurationValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Field name is required")
            .MaximumLength(50)
            .WithMessage("Field name must not exceed 50 characters");

        RuleFor(x => x.TypeName)
            .NotEmpty()
            .WithMessage("Field type name is required")
            .Must(typeName => DataTypeNames.Find(typeName) is not null)
            .WithMessage("Field data type must be the name of a DataTypes option");

        RuleFor(x => x.MaxLength)
            .GreaterThan(0)
            .When(x => x.MaxLength.HasValue)
            .WithMessage("Max length must be greater than zero when specified");
    }
}