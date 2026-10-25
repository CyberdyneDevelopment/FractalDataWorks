using System.Collections.Generic;
using System.Text.Json;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Evaluates one Built transformation with values for its inputs: <c>POST /transformations/{Name}/evaluate</c>.</summary>
public class EvaluateTransformationRequest
{
    /// <summary>Gets or sets the transformation's name, from the route.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value of each input the transformation declares, by input name: a number for a Number input, true or false for
    /// a Flag input. A transformation with no inputs takes an empty object.
    /// </summary>
    public Dictionary<string, JsonElement> Inputs { get; set; } = [];
}
