using System.Globalization;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Database;
using Android.Provider;
using AndroidX.Core.Content;
using HasbeMaal.Core.Import;
using HasbeMaal.Presentation.ViewModels;

namespace HasbeMaal.Mobile.Platforms.Android;

/// <summary>
/// Reads candidate transaction SMS messages from the Android inbox. The sender address is
/// used only for the allowlist check and is dropped before a <see cref="SmsInboxMessage"/>
/// is created, so no sender identifier crosses the Core boundary. Nothing here logs or stores
/// message bodies, sender addresses, phone numbers, or account hints.
/// </summary>
public sealed class AndroidSmsInboxReader : ISmsInboxReader
{
    private readonly SmsSenderAllowlist _allowlist;

    public AndroidSmsInboxReader(SmsSenderAllowlist allowlist)
    {
        ArgumentNullException.ThrowIfNull(allowlist);
        _allowlist = allowlist;
    }

    public async Task<IReadOnlyList<SmsInboxMessage>> ReadAsync(
        DateTimeOffset? since,
        CancellationToken cancellationToken = default)
    {
        var context = global::Android.App.Application.Context;

        // Permission guard: never query the inbox unless READ_SMS is currently granted.
        if (ContextCompat.CheckSelfPermission(context, global::Android.Manifest.Permission.ReadSms)
            != Permission.Granted)
        {
            return Array.Empty<SmsInboxMessage>();
        }

        return await Task.Run(
            () => ReadInbox(context, since, cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }

    private IReadOnlyList<SmsInboxMessage> ReadInbox(
        Context context,
        DateTimeOffset? since,
        CancellationToken cancellationToken)
    {
        var resolver = context.ContentResolver;
        var inboxUri = Telephony.Sms.Inbox.ContentUri;
        if (resolver is null || inboxUri is null)
        {
            return Array.Empty<SmsInboxMessage>();
        }

        var projection = new[]
        {
            Telephony.ITextBasedSmsColumns.Address,
            Telephony.ITextBasedSmsColumns.Body,
            Telephony.ITextBasedSmsColumns.Date
        };

        string? selection = null;
        string[]? selectionArgs = null;
        if (since.HasValue)
        {
            selection = $"{Telephony.ITextBasedSmsColumns.Date} > ?";
            selectionArgs =
            [
                since.Value.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture)
            ];
        }

        var sortOrder = $"{Telephony.ITextBasedSmsColumns.Date} ASC";

        // Single streamed query. The ContentResolver returns a windowed cursor (CursorWindow),
        // so the platform pages rows into memory as we iterate; the whole inbox is not held at
        // once. We deliberately do NOT use ?limit/?offset URI parameters for manual paging:
        // several OEM SMS providers silently ignore them, which makes each "page" return the same
        // rows and turns the loop into a non-terminating scan. Only allowlisted messages are kept,
        // so the retained list stays small regardless of total inbox size.
        using var cursor = resolver.Query(inboxUri, projection, selection, selectionArgs, sortOrder);
        if (cursor is null)
        {
            return Array.Empty<SmsInboxMessage>();
        }

        var addressColumn = cursor.GetColumnIndex(Telephony.ITextBasedSmsColumns.Address);
        var bodyColumn = cursor.GetColumnIndex(Telephony.ITextBasedSmsColumns.Body);
        var dateColumn = cursor.GetColumnIndex(Telephony.ITextBasedSmsColumns.Date);

        var messages = new List<SmsInboxMessage>();
        while (cursor.MoveToNext())
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Sender address is read only to run the allowlist check, then discarded.
            var address = addressColumn >= 0 ? cursor.GetString(addressColumn) : null;
            if (!_allowlist.IsAllowed(address))
            {
                continue;
            }

            var body = bodyColumn >= 0 ? cursor.GetString(bodyColumn) : null;
            var dateMs = dateColumn >= 0 ? cursor.GetLong(dateColumn) : 0L;

            // Only the body and received timestamp cross the boundary; the address is dropped.
            messages.Add(new SmsInboxMessage(
                body ?? string.Empty,
                DateTimeOffset.FromUnixTimeMilliseconds(dateMs)));
        }

        return messages;
    }
}
