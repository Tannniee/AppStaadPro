using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

namespace StaadSteelApp
{
    public partial class MainForm
    {
        private const string LegacySectionsExamplePath = @"C:\ProgramData\Bentley\Engineering\STAAD.Pro 2025\Sections";
        private LegacySectionController _legacyController;
        private readonly HashSet<string> _legacyWarnOnceKeys =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private StaadSteelApp.LegacyDatabaseService _legacyService => _legacyController?.Service;
        private bool _legacyUiLoading
        {
            get { return _legacyController != null && _legacyController.UiLoading; }
            set
            {
                if (_legacyController != null)
                    _legacyController.UiLoading = value;
            }
        }

        private void LegacyWarnOnce(string key, string message, Exception ex = null)
        {
            if (!string.IsNullOrWhiteSpace(key) && !_legacyWarnOnceKeys.Add(key))
                return;

            Debug.WriteLine(ex == null ? message : (message + " | " + ex));
            TopMostMessageBox.ShowWarning(ex == null ? message : (message + "\n" + ex.Message), "Legacy Warning");
        }

        protected override void OnMove(EventArgs e)
        {
            base.OnMove(e);
            UpdateSidePanelLocation();
            UpdateMiscPanelLocation();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateSidePanelLocation();
            UpdateMiscPanelLocation();
        }

        private void UpdateSidePanelLocation()
        {
            if (_sidePanel != null && _sidePanel.Visible)
            {
                _sidePanel.Location = new System.Drawing.Point(this.Right - 5, this.Top + 195);
            }
        }

        private void InitializeLegacyService()
        {
            string path = ResolveLegacySectionsPath();
            if (string.IsNullOrWhiteSpace(path))
                return;

            _legacyController = new LegacySectionController(
                this,
                new StaadSteelApp.LegacyDatabaseService(path));

            this.cboZone.SelectedIndexChanged += (s, e) =>
            {
                if (_legacyUiLoading) return;
                LoadLegacyShapes();
            };
            this.cboShape.SelectedIndexChanged += (s, e) =>
            {
                if (_legacyUiLoading) return;
                LoadLegacySeries();
                UpdateLegacyTypeSpecUI();
            };
            this.cboSeries.SelectedIndexChanged += (s, e) =>
            {
                if (_legacyUiLoading) return;
                LoadLegacySections();
            };
            this.cboSection.SelectedIndexChanged += (s, e) =>
            {
                if (_legacyUiLoading) return;
                if (cboSection.SelectedIndex > -1)
                {
                    if (cboSection.SelectedItem is StaadSteelApp.LegacyDatabaseService.SectionItem item)
                        txtName.Text = item.StaadName;
                    else
                        txtName.Text = cboSection.Text;
                    if (cboZone.SelectedItem is StaadSteelApp.LegacyDatabaseService.ZoneInfo)
                        this.Text = "STAAD Tools";
                }
            };
        }

        private string ResolveLegacySectionsPath()
        {
            string configuredPath = (StaadSteelApp.Core.AppSettings.LoadString("LegacySectionsPath") ?? string.Empty).Trim();
            if (IsValidLegacySectionsPath(configuredPath))
                return configuredPath;

            if (!string.IsNullOrWhiteSpace(configuredPath))
            {
                LegacyWarnOnce(
                    "Legacy:ConfiguredPathInvalid",
                    "Legacy sections path is invalid or missing SectionDatabaseInfo.xml.\n" +
                    "Run setup-legacy-sections.cmd next to the app and configure the correct folder.\n\n" +
                    "Example:\n" + LegacySectionsExamplePath);
            }

            if (IsValidLegacySectionsPath(LegacySectionsExamplePath))
                return LegacySectionsExamplePath;

            LegacyWarnOnce(
                "Legacy:PathMissing",
                "Legacy sections path is not configured.\n" +
                "Run setup-legacy-sections.cmd next to the app and set the Sections folder.\n\n" +
                "Example:\n" + LegacySectionsExamplePath);
            return "";
        }

        private static bool IsValidLegacySectionsPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            try
            {
                return System.IO.Directory.Exists(path) &&
                       System.IO.File.Exists(System.IO.Path.Combine(path, "SectionDatabaseInfo.xml"));
            }
            catch
            {
                return false;
            }
        }

        private void LoadLegacyZones()
        {
            _legacyController?.LoadZones();
        }

        private void LoadLegacyShapes()
        {
            _legacyController?.LoadShapes();
        }

        private void LoadLegacySeries()
        {
            _legacyController?.LoadSeries();
        }

        private void LoadLegacySections()
        {
            _legacyController?.LoadSections();
        }

        private static string ExtractSeriesPrefix(string sectionName, string shapeName = null)
        {
            if (TryParseHollowSectionSeriesAndThickness(sectionName, shapeName, out string seriesKey, out _))
                return seriesKey;

            if (string.IsNullOrEmpty(sectionName)) return "";

            var match = System.Text.RegularExpressions.Regex.Match(
                sectionName,
                @"^([A-Za-z]+\d+(?:\.\d+)?)"
            );

            if (match.Success)
                return match.Groups[1].Value.ToUpper();

            return sectionName.Length > 6 ? sectionName.Substring(0, 6) : sectionName;
        }

        private void TryAutoSelectLegacy(string sectionName)
        {
            _legacyController?.TryAutoSelect(sectionName);
        }
    }
}
