namespace HasbeMaal.Core.Import;

/// <summary>
/// The only inbound contract Core accepts for an SMS-sourced transaction candidate.
/// Deliberately carries no sender address, subscription id, SIM slot, or any other
/// identifier: Core must never be able to attribute a message to a sender. The platform
/// layer is responsible for reading the inbox and reducing each message to its body and
/// the timestamp it was received.
/// </summary>
public sealed record SmsInboxMessage(string Body, DateTimeOffset ReceivedAt);
