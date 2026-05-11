namespace StaadSteelApp.Core
{
    public sealed class GroupEntry
    {
        public string Name { get; set; }
        public int EntityCount { get; set; }
        public int[] EntityIds { get; set; }

        public string DisplayText
        {
            get
            {
                string name = string.IsNullOrWhiteSpace(Name) ? "(unnamed)" : Name;
                return EntityCount > 0 ? name + " (" + EntityCount + ")" : name;
            }
        }

        public override string ToString()
        {
            return DisplayText;
        }
    }
}
