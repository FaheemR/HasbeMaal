namespace HasbeMaal.Core.Import;

/// <summary>
/// Persists the set of bank/UPI-app ids the user selected as SMS transaction sources so the choice
/// survives across app sessions. Implemented by the persistence layer. Only stable registry ids are
/// stored (see <see cref="BankSenderRegistry"/>); no message content, sender address, or reference
/// ever crosses this boundary.
/// </summary>
public interface ISelectedBanksStore
{
    /// <summary>
    /// Returns the persisted bank ids, or an empty list when the user has not made a selection.
    /// An empty list means "default to the full registry" so scanning works out of the box.
    /// </summary>
    Task<IReadOnlyList<string>> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists <paramref name="bankIds"/> as the user's selected SMS sources. An empty list resets
    /// scanning to the full registry on the next import.
    /// </summary>
    Task SetAsync(IReadOnlyList<string> bankIds, CancellationToken cancellationToken = default);
}
