using System;
using Fdw.Configuration;

namespace Fdw.Services.Credentials.Abstractions;

/// <summary>
/// Marker interface for typed credential service body configurations
/// (SqlCredentialServiceConfiguration, etc.). Each typed body implements this interface directly
/// without inheriting from the parent <c>CredentialServiceConfiguration</c> header.
/// </summary>
/// <remarks>
/// Credential service bodies are persisted in their own tables (sec.SqlCredentialService, etc.)
/// and linked to the parent <c>sec.CredentialService</c> row via a <c>CredentialServiceId</c>
/// foreign key property. The parent header carries an
/// <c>ICredentialServiceConfiguration? Configuration</c> property populated on the read path.
/// </remarks>
public interface ICredentialServiceConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the FK to <c>sec.CredentialService.Id</c>.</summary>
    Guid CredentialServiceId { get; set; }
}
