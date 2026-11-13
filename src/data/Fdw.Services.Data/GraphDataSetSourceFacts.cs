using System;
using System.Collections.Generic;

namespace Fdw.Services.Data;

/// <summary>What the check before save needs to know of one data set a draft reads (FDW-5290).</summary>
/// <param name="Exists">Whether a data set of that name exists.</param>
/// <param name="IsUnboundGraph">Whether it is a Graph data set that is still abstract: nothing feeds its fields yet.</param>
/// <param name="Reads">The names of the data sets it reads in turn: a Graph data set's components, a Variation's root, a Compound or Federated data set's sources. Empty when it does not exist.</param>
/// <param name="Id">Its id, which an intended source that names it states; null when it does not exist.</param>
public sealed record GraphDataSetSourceFacts(bool Exists, bool IsUnboundGraph, IReadOnlyList<string> Reads, Guid? Id = null);
