using HasbeMaal.Core.Domain;
using HasbeMaal.Core.Parsing;

namespace HasbeMaal.Core.Import;

/// <summary>
/// A parsed transaction that was not confidently identified and therefore requires the user to
/// review it before it is committed. Carries the parse confidence so the UI can order or explain
/// candidates. These are never persisted by the importer in this slice.
/// </summary>
public sealed record SmsImportReviewCandidate(FinancialTransaction Transaction, ParseConfidence Confidence);
