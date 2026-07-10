using HasbeMaal.Core.Import;

namespace HasbeMaal.Mobile.Services;

public sealed class UnsupportedSmsInboxReader : ISmsInboxReader
{
    public Task<IReadOnlyList<SmsInboxMessage>> ReadAsync(
        DateTimeOffset? since,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SmsInboxMessage>>(Array.Empty<SmsInboxMessage>());
}
