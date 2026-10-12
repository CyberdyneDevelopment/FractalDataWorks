using System;

namespace Fdw.Web.Calculations.Clients.Models;

/// <summary>
/// Request to read one calculation entity: the body of <c>POST /calculation-entities/get</c>.
/// </summary>
/// <remarks>
/// A read of one record is a POST, not a GET: a GET is a plain paged list and nothing else.
/// </remarks>
public sealed class GetCalculationEntityRequest
{
    /// <summary>
    /// Gets or sets the identifier of the calculation entity.
    /// </summary>
    public Guid Id { get; set; }
}
