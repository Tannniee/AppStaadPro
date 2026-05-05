namespace StaadSteelApp
{
    public partial class MainForm
    {
        private void EnsureLegacyCache()
        {
            _legacyController?.EnsureCache();
        }

        private void PopulateLegacySectionMetadata(StaadSteelApp.Core.SectionInfo info)
        {
            _legacyController?.PopulateSectionMetadata(info);
        }
    }
}
