namespace Fdw.Services.Calculations.Abstractions.Sets;

/// <summary>
/// Decides what becomes of one output row a set could not build. The set never decides this itself: its caller does.
/// </summary>
/// <param name="failure">The row and why it failed.</param>
/// <returns><see langword="true"/> when the row is dropped and the set carries on; <see langword="false"/> when the set fails with the row's refusal.</returns>
public delegate bool SetRowFailureHandler(SetRowFailure failure);
