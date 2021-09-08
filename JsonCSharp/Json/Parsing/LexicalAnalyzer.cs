using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace JsonCSharp.Json.Parsing
{
    public class LexicalAnalyzer
    {
        private readonly string source;
        private int position = -1;

        public object Value { get; private set; }
        private char CurrentChar => source[position];

        public LexicalAnalyzer(string source) =>
            this.source = source;

        public Symbol NextSymbol()
        {
            Value = null;
            position++;
            SkipWhiteSpace();
            if (position >= source.Length)
            {
                return Symbol.EOI;
            }
            switch (CurrentChar)
            {
                case '{':
                    return Symbol.OpenCurlyBracket;
                case '}':
                    return Symbol.CloseCurlyBracket;
                case '[':
                    return Symbol.OpenSquareBracket;
                case ']':
                    return Symbol.CloseSquareBracket;
                case ',':
                    return Symbol.Comma;
                case ':':
                    return Symbol.Colon;
                case '"':
                    Value = ReadStringValue();
                    return Symbol.Value;
                case '-':
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    Value = ReadNumberValue();
                    return Symbol.Value;
                case 'n':
                    ReadKeyword("null");
                    Value = null;
                    return Symbol.Value;
                case 't':
                    ReadKeyword("true");
                    Value = true;
                    return Symbol.Value;
                case 'f':
                    ReadKeyword("false");
                    Value = false;
                    return Symbol.Value;
                default:
                    throw new JsonException($"Unexpected char '{CurrentChar}' at position {position}");
            }
        }

        private void CheckEOI()
        {
            if (position >= source.Length)
            {
                throw new JsonException("Unexpected EOI at position " + position);
            }
        }

        private void SkipWhiteSpace()
        {
            while (position < source.Length && char.IsWhiteSpace(CurrentChar))
            {
                position++;
            }
        }

        private void ReadKeyword(string keyword)
        {
            for (var i = 0; i < keyword.Length; i++)
            {
                if (source[position + i] != keyword[i])
                {
                    throw new JsonException($"Unexpected character '{source[position + i]}' at position {position + i}. Keyword '{keyword}' was expected.");
                }
            }
            position += keyword.Length - 1;
        }

        private string ReadStringValue()
        {
            var sb = new StringBuilder();
            position += 1;  // Skip quote
            while (CurrentChar != '"')
            {
                if (CurrentChar == '\\')
                {
                    position++;
                    CheckEOI();
                    switch (CurrentChar)
                    {
                        case '"':
                            sb.Append('"');
                            break;
                        case '\\':
                            sb.Append('\\');
                            break;
                        case '/':
                            sb.Append('/');
                            break;
                        case 'b':
                            sb.Append('\b');
                            break;
                        case 'f':
                            sb.Append('\f');
                            break;
                        case 'n':
                            sb.Append('\n');
                            break;
                        case 'r':
                            sb.Append('\r');
                            break;
                        case 't':
                            sb.Append('\t');
                            break;
                        case 'u':
                            if (position + 4 >= source.Length)
                            {
                                throw new JsonException(@"Unexpected EOI, \uXXX escape expected.");
                            }
                            sb.Append(char.ConvertFromUtf32(int.Parse(source.Substring(position + 1, 4), NumberStyles.HexNumber)));
                            position += 4;
                            break;
                        default:
                            throw new JsonException(@"Unexpected escape sequence \" + CurrentChar);
                    }
                }
                else
                {
                    sb.Append(CurrentChar);
                }
                position++;
                CheckEOI();
            }
            return sb.ToString();
        }

        private object ReadNumberValue()
        {
            StringBuilder integer = new StringBuilder(), @decimal = null, exponent = null, current;
            current = integer;
            while (position < source.Length)
            {
                switch (CurrentChar)
                {
                    case '-':
                        if (current.Length == 0)
                        {
                            current.Append('-');
                        }
                        else
                        {
                            throw new JsonException("Unexpected number format: Unexpected '-' at position " + position);
                        }
                        break;
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        current.Append(CurrentChar);
                        break;
                    case '.':
                        if (current == integer && @decimal == null && current.ToString().TrimStart('-').Any())
                        {
                            @decimal = new StringBuilder();
                            current = @decimal;
                        }
                        else
                        {
                            throw new JsonException("Unexpected number format: Unexpected '.' at position " + position);
                        }
                        break;
                    case '+':
                        if (current != exponent || current.Length != 0)
                        {
                            throw new JsonException("Unexpected number format at position " + position);
                        }
                        break;
                    case 'e':
                    case 'E':
                        exponent = new StringBuilder();
                        current = exponent;
                        break;
                    default:
                        position--; //Remain on the last digit
                        return BuildResult();
                }
                position++;
            }
            return BuildResult();

            object BuildResult()
            {
                var baseValue = @decimal == null
                    ? (object)int.Parse(integer.ToString())
                    : decimal.Parse(integer.ToString() + CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator + @decimal.ToString());
                if (exponent == null)   // Integer or Decimal
                {
                    return baseValue;
                }
                else if (exponent.Length == 0 || exponent.ToString() == "-")
                {
                    throw new JsonException($"Unexpected number exponent format: {exponent} at position {position}");
                }
                else
                {
                    return Convert.ToDouble(baseValue) * Math.Pow(10, int.Parse(exponent.ToString()));
                }
            }
        }
    }
}
