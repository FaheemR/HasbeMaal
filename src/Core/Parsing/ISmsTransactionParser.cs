namespace HasbeMaal.Core.Parsing;

public interface ISmsTransactionParser
{
    ParsedTransaction? TryParse(string message);
}