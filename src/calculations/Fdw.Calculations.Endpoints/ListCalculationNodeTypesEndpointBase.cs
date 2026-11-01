using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.CalculationNodeOptions;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Calculations.Endpoints;

/// <summary>
/// Lists the operations a formula node can name -- the <c>CalculationNodeTypes</c> options -- with the arity, operand names,
/// optional members and modes each takes, so a client builds a formula editor from the platform's own vocabulary (FDW-1280).
/// </summary>
public abstract class ListCalculationNodeTypesEndpointBase : CrudListEndpointBase<CalculationNodeTypeResponse>
{
    /// <summary>Initializes a new instance of the <see cref="ListCalculationNodeTypesEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    protected ListCalculationNodeTypesEndpointBase(ILogger<ListCalculationNodeTypesEndpointBase> logger) : base(logger)
    {
    }

    /// <summary>Gets the resource name used for route generation.</summary>
    protected override string ResourceName => "calculation-node-types";

    /// <inheritdoc />
    /// <remarks>
    /// Any signed-in caller may list the operations: they are the platform's vocabulary, the same for every caller, and
    /// anyone who writes a formula has to be able to name one. The same reasoning as the data types.
    /// </remarks>
    protected override bool ResourceDecidesAccess => true;

    /// <summary>Gets the OpenAPI summary for this endpoint.</summary>
    protected override string EndpointSummary => "List formula operations";

    /// <summary>Gets the OpenAPI description for this endpoint.</summary>
    protected override string EndpointDescription =>
        "Returns every operation a formula node can name, each with its operand count, operand names, the optional members it reads (name, value, flag, places, mode) and the modes it accepts.";

    /// <summary>Loads the operations from the CalculationNodeTypes type collection.</summary>
    protected override Task<IGenericResult<List<CalculationNodeTypeResponse>>> LoadItems(CancellationToken ct)
    {
        var items = CalculationNodeTypes.All()
            .OrderBy(type => type.Name, StringComparer.Ordinal)
            .Select(type => new CalculationNodeTypeResponse
            {
                Name = type.Name,
                Summary = type.Summary,
                MinOperands = type.MinOperands,
                MaxOperands = type.MaxOperands,
                OperandNames = type.OperandNames.ToList(),
                Members = type.Members.ToList(),
                Modes = type.Modes.ToList(),
            })
            .ToList();
        return Task.FromResult(GenericResult<List<CalculationNodeTypeResponse>>.Success(items));
    }
}
