using System;
using System.Collections;
using System.Collections.Generic;
using JsonCSharp.Json.Parsing;

namespace JsonCSharp.Json
{
    public sealed class JsonNode : IEnumerable<JsonNode>
    {
        public Kind Kind { get; }

        private readonly List<JsonNode> list;
        private readonly Dictionary<string, JsonNode> map;
        private readonly object value;

        public LinePosition Start { get;  }
        public LinePosition End { get; private set; }

        public int Count => Kind switch
        {
            Kind.Object => map.Count,
            Kind.List => list.Count,
            _ => throw InvalidKind()
        };

        public JsonNode this[int index]
        {
            get => Kind == Kind.List ? list[index] : throw InvalidKind();
        }

        public JsonNode this[string key]
        {
            get => Kind == Kind.Object ? map[key] : throw InvalidKind();
        }

        public IEnumerable<string> Keys => Kind == Kind.Object ? map.Keys : throw InvalidKind();

        public object Value => Kind == Kind.Value ? value : throw InvalidKind();

        public JsonNode(LinePosition start, LinePosition end, object value)
        {
            Kind = Kind.Value;
            Start = start;
            End = end;
            this.value = value;
        }

        public JsonNode(LinePosition start, Kind kind)
        {
            Kind = kind;
            Start = start;
            switch (kind)
            {
                case Kind.List:
                    list = new List<JsonNode>();
                    break;
                case Kind.Object:
                    map = new Dictionary<string, JsonNode>();
                    break;
                default:
                    throw InvalidKind();
            }
        }

        public void UpdateEnd(LinePosition end)
        {
            if (End == null)
            {
                End = end;
            }
            else
            {
                throw new InvalidOperationException("End position is already set");
            }
        }

        public void Add(JsonNode value)
        {
            if (Kind == Kind.List)
            {
                list.Add(value);
            }
            else
            {
                throw InvalidKind();
            }
        }

        public void Add(string key, JsonNode value)
        {
            if (Kind == Kind.Object)
            {
                map[key] = value;
            }
            else
            {
                throw InvalidKind();
            }
        }

        public bool ContainsKey(string key) =>
            Kind == Kind.Object ? map.ContainsKey(key) : throw InvalidKind();

        public IEnumerator<JsonNode> GetEnumerator() =>
            Kind == Kind.List ? list.GetEnumerator() : throw InvalidKind();

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();

        private InvalidOperationException InvalidKind() =>
            new InvalidOperationException("Operation is not valid. Json kind is " + Kind);
    }

}
