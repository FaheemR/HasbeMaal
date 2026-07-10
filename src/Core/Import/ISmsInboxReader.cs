namespace HasbeMaal.Core.Import;

/// <summary>
/// Reads candidate SMS messages from the device inbox. Implemented by the platform layer
/// (Android) in a later slice; Core defines the contract only. Implementations must project
/// each message down to a <see cref="SmsInboxMessage"/> so no sender address ever crosses
/// this boundary.
/// </summary>
public interface ISmsInboxReader
{
    /// <summary>
    /// Reads messages received after <paramref name="since"/> (exclusive) when provided,
    /// otherwise all available messages.
    /// </summary>
    Task<IReadOnlyList<SmsInboxMessage>> ReadAsync(
        DateTimeOffset? since,
        CancellationToken cancellationToken = default);
}
