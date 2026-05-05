namespace StaadSteelApp
{
    internal enum LegacySpecMode
    {
        None,
        AngleSpacing,
        ChannelSpacing,
        DoubleProfile,
        Composite,
        TopCover,
        BottomCover,
        TopBottomCover
    }

    internal sealed class LegacyTypeSpecItem
    {
        public string Text { get; }
        public int Value { get; }
        public LegacySpecMode Mode { get; }

        public LegacyTypeSpecItem(string text, int value, LegacySpecMode mode)
        {
            Text = text;
            Value = value;
            Mode = mode;
        }

        public override string ToString()
        {
            return Text;
        }
    }
}
