using FluentAssertions;
using JsonCSharp.Json.Parsing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace JSonCSharp.Json.Tests
{
    [TestClass]
    public class SyntaxAnalyzerTest
    {
        [DataTestMethod]
        [DataRow("[null]", null)]
        [DataRow("[true]", true)]
        [DataRow("[false]", false)]
        [DataRow("[42]", 42)]
        [DataRow("[42.42]", 42.42)]
        [DataRow("[\"Lorem Ipsum\"]", "Lorem Ipsum")]
        public void StandaloneValue(string source, object expected)
        {
            var sut = new SyntaxAnalyzer(source);
            var ret = sut.Parse();
            ret.Kind.Should().Be(Kind.List);
            ret.Count.Should().Be(1);
            var value = ret.Single();
            value.Kind.Should().Be(Kind.Value);
            value.Value.Should().Be(expected);
        }

        [DataTestMethod]
        [DataRow("{}")]
        [DataRow("{ }")]
        [DataRow(" { \t\n\r } ")]
        public void EmptyObject(string source)
        {
            var sut = new SyntaxAnalyzer(source);
            var ret = sut.Parse();
            ret.Kind.Should().Be(Kind.Object);
            ret.Count.Should().Be(0);
        }

        [DataTestMethod]
        [DataRow("[]")]
        [DataRow("[ ]")]
        [DataRow(" [ \t\n\r ] ")]
        public void EmptyList(string source)
        {
            var sut = new SyntaxAnalyzer(source);
            var ret = sut.Parse();
            ret.Kind.Should().Be(Kind.List);
            ret.Count.Should().Be(0);
        }

        [TestMethod]
        public void ParseObject()
        {
            const string json = @"
{
    ""a"": ""aaa"",
    ""b"": 42,
    ""c"": true,
    ""d"": null
}";
            var sut = new SyntaxAnalyzer(json);
            var ret = sut.Parse();
            ret.Kind.Should().Be(Kind.Object);
            ret.ContainsKey("a").Should().BeTrue();
            ret.ContainsKey("b").Should().BeTrue();
            ret.ContainsKey("c").Should().BeTrue();
            ret.ContainsKey("d").Should().BeTrue();
            ret["a"].Value.Should().Be("aaa");
            ret["b"].Value.Should().Be(42);
            ret["c"].Value.Should().Be(true);
            ret["d"].Value.Should().Be(null);
        }

        [TestMethod]
        public void ParseList()
        {
            const string json = @"[""aaa"", 42, true, null]";
            var sut = new SyntaxAnalyzer(json);
            var ret = sut.Parse();
            ret.Kind.Should().Be(Kind.List);
            ret.Select(x => x.Value).Should().ContainInOrder("aaa", 42, true, null);
        }

        [TestMethod]
        public void ParseNested()
        {
            const string json = @"
{
    ""a"": [""aaa"", ""bbb"", ""ccc"", { ""x"": true}],
    ""b"": 42,
    ""c"": {""1"": 111, ""2"": ""222"", ""list"": [42, 43, 44]}
}";
            var sut = new SyntaxAnalyzer(json);
            var root = sut.Parse();
            root.Kind.Should().Be(Kind.Object);
            root.ContainsKey("a").Should().BeTrue();
            root.ContainsKey("b").Should().BeTrue();
            root.ContainsKey("c").Should().BeTrue();
            root.ContainsKey("d").Should().BeFalse();

            var a = root["a"];
            a.Kind.Should().Be(Kind.List);
            a.Where(x => x.Kind == Kind.Value).Select(x => x.Value).Should().ContainInOrder("aaa", "bbb", "ccc");
            var objectInList = a.Single(x => x.Kind == Kind.Object);
            objectInList.ContainsKey("x").Should().BeTrue();
            objectInList["x"].Value.Should().Be(true);
            
            root["b"].Value.Should().Be(42);
            
            var c = root["c"];
            c.Kind.Should().Be(Kind.Object);
            c.ContainsKey("1").Should().BeTrue();
            c.ContainsKey("2").Should().BeTrue();
            c.ContainsKey("list").Should().BeTrue();
            c["1"].Kind.Should().Be(Kind.Value);
            c["1"].Value.Should().Be(111);
            c["2"].Kind.Should().Be(Kind.Value);
            c["2"].Value.Should().Be("222");
            c["list"].Kind.Should().Be(Kind.List);
            c["list"].Select(x => x.Value).Should().ContainInOrder(42, 43, 44);
        }
    }
}
