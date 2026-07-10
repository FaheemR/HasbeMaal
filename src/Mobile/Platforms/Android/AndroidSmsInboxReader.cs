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
    private const int PageSize = 500;

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

        var messages = new List<SmsInboxMessage>();
        var offset = 0;

        // Bounded paging off the UI thread: read fixed-size pages until a short page is seen.
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var pageUri = inboxUri.BuildUpon()
                ?.AppendQueryParameter("limit", PageSize.ToString(CultureInfo.InvariantCulture))
                ?.AppendQueryParameter("offset", offset.ToString(CultureInfo.InvariantCulture))
                ?.Build();
            if (pageUri is null)
            {
                break;
            }

            using var cursor = resolver.Query(pageUri, projection, selection, selectionArgs, sortOrder);
            if (cursor is null)
            {
                break;
            }

            var addressColumn = cursor.GetColumnIndex(Telephony.ITextBasedSmsColumns.Address);
            var bodyColumn = cursor.GetColumnIndex(Telephony.ITextBasedSmsColumns.Body);
            var dateColumn = cursor.GetColumnIndex(Telephony.ITextBasedSmsColumns.Date);

            var rowsInPage = 0;
            while (rowsInPage < PageSize && cursor.MoveToNext())
            {
                rowsInPage++;

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

            if (rowsInPage < PageSize)
            {
                break;
            }

            offset += PageSize;
        }

        return messages;
    }
}
