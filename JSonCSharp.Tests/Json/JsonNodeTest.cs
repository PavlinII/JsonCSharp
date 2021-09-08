using FluentAssertions;
using JsonCSharp.Json;
using JsonCSharp.Json.Parsing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;

namespace JSonCSharp.Json.Tests
{
    [TestClass]
    public class JsonNodeTest
    {
        [TestMethod]
        public void BehavesAsValue()
        {
            var sut = new JsonNode(LinePosition.Zero, LinePosition.Zero, 42);
            sut.Kind.Should().Be(Kind.Value);
            sut.Value.Should().Be(42);
            sut.Invoking(x => x.Count).Should().Throw<InvalidOperationException>();
            sut.Invoking(x => x[0]).Should().Throw<InvalidOperationException>();
            sut.Invoking(x => x["Key"]).Should().Throw<InvalidOperationException>();
            sut.Invoking(x => x.Keys).Should().Throw<InvalidOperationException>();
            sut.Invoking(x => x.Add(sut)).Should().Throw<InvalidOperationException>();
            sut.Invoking(x => x.Add("Key", sut)).Should().Throw<InvalidOperationException>();
            sut.Invoking(x => x.ContainsKey("Key")).Should().Throw<InvalidOperationException>();
            sut.Invoking(x => ((IEnumerable)x).GetEnumerator()).Should().Throw<InvalidOperationException>();
        }

        [TestMethod]
        public void BehavesAsList()
        {
            var a = new JsonNode(LinePosition.Zero, LinePosition.Zero, "a");
            var b = new JsonNode(LinePosition.Zero, LinePosition.Zero, "b");
            var sut = new JsonNode(LinePosition.Zero, Kind.List);
            sut.Add(a);
            sut.Add(b);
            sut.Kind.Should().Be(Kind.List);
            sut.Count.Should().Be(2);
            ((object)sut[0]).Should().Be(a);
            ((object)sut[1]).Should().Be(b);
            var cnt = 0;
            foreach (var item in sut)
            {
                new[] { a, b }.Should().Contain(item);
                cnt++;
            }
            cnt.Should().Be(2);
            sut.Invoking(x => x.Value).Should().Throw<InvalidOperationException>();
            sut.Invoking(x => x["Key"]).Should().Throw<InvalidOperationException>();
            sut.Invoking(x => x.Keys).Should().Throw<InvalidOperationException>();
            sut.Invoking(x => x.Add("Key", sut)).Should().Throw<InvalidOperationException>();
            sut.Invoking(x => x.ContainsKey("Key")).Should().Throw<InvalidOperationException>();
        }

        [TestMethod]
        public void BehavesAsDictionary()
        {
            var a = new JsonNode(LinePosition.Zero, LinePosition.Zero, "a");
            var b = new JsonNode(LinePosition.Zero, LinePosition.Zero, "b");
            var sut = new JsonNode(LinePosition.Zero, Kind.Object);
            sut.Add("KeyA", a);
            sut.Add("KeyB", b);
            sut.Kind.Should().Be(Kind.Object);
            sut.Count.Should().Be(2);
            ((object)sut["KeyA"]).Should().Be(a);
            ((object)sut["KeyB"]).Should().Be(b);
            sut.Keys.Should().BeEquivalentTo("KeyA", "KeyB");
            sut.ContainsKey("KeyA").Should().BeTrue();
            sut.ContainsKey("KeyB").Should().BeTrue();
            sut.ContainsKey("KeyC").Should().BeFalse();
            sut.Invoking(x => x.Value).Should().Throw<InvalidOperationException>();
            sut.Invoking(x => x[0]).Should().Throw<InvalidOperationException>();
            sut.Invoking(x => x.Add(sut)).Should().Throw<InvalidOperationException>();
            sut.Invoking(x => ((IEnumerable)x).GetEnumerator()).Should().Throw<InvalidOperationException>();
        }

        [TestMethod]
        public void UpdateEnd()
        {
            var start = new LinePosition(1, 42);
            var end = new LinePosition(2, 10);
            var sut = new JsonNode(start, Kind.List);
            sut.Start.Should().Be(start);
            sut.End.Should().BeNull();

            sut.UpdateEnd(end);
            sut.End.Should().Be(end);

            sut.Invoking(x => x.UpdateEnd(end)).Should().Throw<InvalidOperationException>();
        }
    }
}
