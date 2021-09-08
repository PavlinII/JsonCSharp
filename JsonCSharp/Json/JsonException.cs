using System;

namespace JsonCSharp.Json
{
    public class JsonException : Exception
    {
        public JsonException(string message) : base(message) { }
    }
}
