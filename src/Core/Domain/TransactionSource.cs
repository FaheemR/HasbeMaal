namespace HasbeMaal.Core.Domain;

public enum TransactionSource
{
    ManualCash,
    UpiSms,
    BankSms,
    CreditCardSms,
    WalletSms,
    ImportedStatement
}