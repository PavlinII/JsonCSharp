namespace JsonCSharp.Json
{
    public class LinePosition   // FIXME: Use the one from Roslyn
    {
        public int Line { get; }
        public int Character { get; }

        public static LinePosition Zero { get; } = new LinePosition(0, 0);

        public LinePosition(int line, int character)
        {
            Line = line;
            Character = character;
        }
    }
}
