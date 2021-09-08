
namespace JsonCSharp.Json.Parsing
{
    public enum Kind
    {
        Object,
        List,
        Value
    }

    public enum Symbol
    {
        EOI,
        // Special
        OpenCurlyBracket, 
        CloseCurlyBracket,
        OpenSquareBracket,
        CloseSquareBracket,
        Comma,              
        Colon,             
        // Terms
        Value   // String, Number (Integer, Decimal, Double), Null, True, False
    }
}
