using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.ServiceTypes.Results;

/// <summary>
/// A command was dispatched to a service type that declares no service to run it.
/// </summary>
[TypeOption(typeof(ServiceTypeResultCodes), "NoServiceToExecute", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NoServiceToExecuteCode : ServiceTypeResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoServiceToExecuteCode"/> class.
    /// </summary>
    public NoServiceToExecuteCode()
        : base(61015, "NoServiceToExecute",
            ResultSeverities.ByName("Error"),
            "A {CommandType} command was dispatched to {ServiceTypeName}, which declares no service to run it",
            isRetryable: false)
    {
    }
}
