using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Http.Operations;

/// <summary>The type the endpoint method operation collection is built on.</summary>
[ExcludeFromCodeCoverage]
public abstract class EndpointMethodOperationBase : TypeOptionBase<EndpointMethodOperationBase>, IEndpointMethodOperation
{
    /// <summary>Initializes the option with its name, the HTTP method it is found by and the data operation it stands for.</summary>
    /// <param name="name">The option's name.</param>
    /// <param name="method">The HTTP method, in upper case.</param>
    /// <param name="operation">The data operation the method stands for.</param>
    protected EndpointMethodOperationBase(string name, string method, string operation)
        : base(name)
    {
        Method = method;
        Operation = operation;
    }

    /// <inheritdoc/>
    [TypeLookup("ByMethod")]
    public string Method { get; }

    /// <inheritdoc/>
    public string Operation { get; }
}
