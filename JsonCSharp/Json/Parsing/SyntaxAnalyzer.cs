using System;
using System.Collections.Generic;
using System.Text;

namespace JsonCSharp.Json.Parsing
{
    public class SyntaxAnalyzer
    {
        // Parse   -> { ParseObject | [ ParseList

        // ParseObject -> } | ObjectKeyValue ObjectRest
        // ObjectRest -> , ObjectKeyValue ObjectRest | }
        // ObjectKeyValue -> String : ParseValue

        // ParseList -> ] | ParseValue ArrayRest
        // ArrayRest -> , ParseValue ] | ]

        // ParseValue -> { ParseObject | [ ParseList | eSymbol.Value (tzn .Value je true | false | null | String | Number)

        private readonly LexicalAnalyzer la;
        private Symbol symbol;

        public SyntaxAnalyzer(string source) =>
            la = new LexicalAnalyzer(source);

        public JsonNode Parse() =>
            ReadNext() switch
            {
                Symbol.OpenCurlyBracket => ParseObject(),
                Symbol.OpenSquareBracket => ParseList(),
                _ => throw Unexpected("{ or [")
            };

        private Symbol ReadNext() =>
            symbol = la.NextSymbol();

        private JsonNode ParseObject()
        {
            var ret = new JsonNode(la.LastStart, Kind.Object);
            if (ReadNext() != Symbol.CloseCurlyBracket)  // Could be empty object {}
            {
                ObjectKeyValue(ret);
                while (ReadNext() == Symbol.Comma)
                {
                    ReadNext();
                    ObjectKeyValue(ret);
                }
            }
            ret.UpdateEnd(la.LastEnd);
            return ret;
        }

        private void ObjectKeyValue(JsonNode target)
        {
            if (symbol == Symbol.Value && la.Value is string key)
            {
                if (ReadNext() != Symbol.Colon)
                {
                    throw Unexpected(":");
                }
                ReadNext(); // Prepare before reading Value
                target.Add(key, ParseValue());
            }
            else
            {
                throw Unexpected("String Value");
            }
        }

        private JsonNode ParseList()
        {
            var ret = new JsonNode(la.LastStart, Kind.List);
            if (ReadNext() != Symbol.CloseSquareBracket)    // Could be empty array []
            {
                ret.Add(ParseValue());
                while (ReadNext() == Symbol.Comma)
                {
                    ReadNext();
                    ret.Add(ParseValue());
                }
                if (symbol != Symbol.CloseSquareBracket)
                {
                    throw Unexpected("]");
                }
            }
            ret.UpdateEnd(la.LastEnd);
            return ret;
        }

        private JsonNode ParseValue() =>
            // Symbol is already read
            symbol switch
            {
                Symbol.OpenCurlyBracket => ParseObject(),
                Symbol.OpenSquareBracket => ParseList(),
                Symbol.Value => new JsonNode(la.LastStart, la.CurrentPosition(1), la.Value),
                _ => throw Unexpected("{, [ or Value (true, false, null, String, Number")
            };

        private JsonException Unexpected(string expected) =>
            Unexpected(expected, symbol.ToString());

        private JsonException Unexpected(string expected, string found) =>
            new JsonException($"{expected} expected, but {found} found.");
    }
}
