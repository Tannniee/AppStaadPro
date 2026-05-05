namespace StaadSteelApp.Core
{
    public sealed class UsedSectionEntry
    {
        public int PropertyId { get; set; }
        public int[] PropertyIds { get; set; }
        public int[] BeamIds { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string SourceLabel { get; set; }
        public int SortGroup { get; set; }
        public int SortType { get; set; }
        public int BeamCount { get; set; }
        public string SelectionKey => string.IsNullOrWhiteSpace(DisplayName) ? Name : DisplayName;

        public string DisplayText
        {
            get
            {
                string name = string.IsNullOrWhiteSpace(DisplayName)
                    ? (string.IsNullOrWhiteSpace(Name) ? ("PROP-" + PropertyId) : Name)
                    : DisplayName;
                return name;
            }
        }
    }
}
