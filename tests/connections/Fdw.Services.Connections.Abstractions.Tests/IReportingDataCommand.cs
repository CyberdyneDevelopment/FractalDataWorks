using Fdw.Commands.Data.Abstractions;

namespace Fdw.Services.Connections.Abstractions.Tests;

/// <summary>A data command that reports its unmatched keys (FDW-4988), for mocking in tests: public, so the mocking library can implement it.</summary>
public interface IReportingDataCommand : IDataCommand, IReportsUnmatchedKeys
{
}
