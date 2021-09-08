using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace JsonCSharp.Json.Parsing
{
    public class LexicalAnalyzer
    {
        private readonly List<string> lines = new List<string>();
        private int line = 0;
        private int position = -1;

        public object Value { get; private set; }
        public LinePosition LastStart { get; private set; }
        public LinePosition LastEnd { get; private set; }
        private char CurrentChar => lines[line][position];
        private bool CanReadCurrentChar => line < lines.Count - 1 || (line == lines.Count - 1 && position < lines[line].Length);
        private string AtLocationSuffix => $" at line {LastStart.Line + 1} position {LastStart.Character + 1}";

        public LexicalAnalyzer(string source)
        {
            var sb = new StringBuilder();
            foreach (var c in source.Replace("\r\n", "\n"))
            {
                if (c == '\n'
                    || c == '\r'
                    || (char.GetUnicodeCategory(c) is var category && (category == UnicodeCategory.LineSeparator || category == UnicodeCategory.ParagraphSeparator)))
                {
                    lines.Add(sb.ToString());
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }
            lines.Add(sb.ToString());
        }

        public LinePosition CurrentPosition(int increment) =>
            new LinePosition(line, position + increment);

        public Symbol NextSymbol()
        {
            Value = null;
            NextPosition(false);
            SkipWhiteSpace();
            if (!CanReadCurrentChar)
            {
                return Symbol.EOI;
            }
            LastStart = CurrentPosition(0);
            LastEnd = CurrentPosition(1);   // ToDo: Verify that Roslyn needs to be one character behind the end, otherwise, we don't need LastEnd
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
                    throw new JsonException($"Unexpected character '{CurrentChar}'{AtLocationSuffix}");
            }
        }

        private void NextPosition(bool checkEOI = true)
        {
            if (line < lines.Count && position < lines[line].Length - 1)
            {
                position++;
            }
            else
            {
                do
                {
                    line++;
                } 
                while (line < lines.Count && lines[line].Length == 0);
                position = 0;
                if (checkEOI && !CanReadCurrentChar)
                {
                    throw new JsonException("Unexpected EOI" + AtLocationSuffix);
                }
            }
        }

        private void SkipWhiteSpace()
        {
            while (CanReadCurrentChar && char.IsWhiteSpace(CurrentChar))
            {
                NextPosition(false);
            }
        }

        private void ReadKeyword(string keyword)
        {
            for (var i = 0; i < keyword.Length; i++)
            {
                if (lines[line][position + i] != keyword[i])
                {
                    throw new JsonException($"Unexpected character '{lines[line][position + i]}'{AtLocationSuffix}. Keyword '{keyword}' was expected.");
                }
            }
            position += keyword.Length - 1;
        }

        private string ReadStringValue()
        {
            var sb = new StringBuilder();
            NextPosition();  // Skip quote
            while (CurrentChar != '"')
            {
                if (CurrentChar == '\\')
                {
                    NextPosition();
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
                            if (position + 4 >= lines[line].Length)
                            {
                                throw new JsonException(@"Unexpected EOI, \uXXX escape expected.");
                            }
                            sb.Append(char.ConvertFromUtf32(int.Parse(lines[line].Substring(position + 1, 4), NumberStyles.HexNumber)));
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
                NextPosition();
            }
            return sb.ToString();
        }

        private object ReadNumberValue()
        {
            StringBuilder integer = new StringBuilder(), @decimal = null, exponent = null, current;
            current = integer;
            while (CanReadCurrentChar)
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
                            throw new JsonException("Unexpected number format: Unexpected '-'" + AtLocationSuffix);
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
                            throw new JsonException("Unexpected number format: Unexpected '.'" + AtLocationSuffix);
                        }
                        break;
                    case '+':
                        if (current != exponent || current.Length != 0)
                        {
                            throw new JsonException("Unexpected number format" + AtLocationSuffix);
                        }
                        break;
                    case 'e':
                    case 'E':
                        exponent = new StringBuilder();
                        current = exponent;
                        break;
                    default:
                        //Remain on the last digit
                        if (position > 0)
                        {
                            position--;
                        }
                        else
                        {
                            line--;
                            position = lines[line].Length - 1;
                        }
                        return BuildResult();
                }
                NextPosition(false);
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
                    throw new JsonException($"Unexpected number exponent format: {exponent}{AtLocationSuffix}");
                }
                else
                {
                    return Convert.ToDouble(baseValue) * Math.Pow(10, int.Parse(exponent.ToString()));
                }
            }
        }
    }
}
