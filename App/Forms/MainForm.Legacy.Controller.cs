using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace StaadSteelApp
{
    public partial class MainForm
    {
        private sealed class LegacySectionController
        {
            private readonly MainForm _owner;
            private readonly StaadSteelApp.LegacyDatabaseService _service;
            private bool _cacheBuilt;
            private List<StaadSteelApp.LegacyDatabaseService.ZoneInfo> _zones =
                new List<StaadSteelApp.LegacyDatabaseService.ZoneInfo>();
            private Dictionary<string, List<string>> _shapesByZone =
                new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            private Dictionary<string, List<StaadSteelApp.LegacyDatabaseService.SectionItem>> _sectionsByZoneShape =
                new Dictionary<string, List<StaadSteelApp.LegacyDatabaseService.SectionItem>>(StringComparer.OrdinalIgnoreCase);
            private Dictionary<string, List<string>> _seriesByZoneShape =
                new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            private Dictionary<string, LegacySectionRef> _sectionIndex =
                new Dictionary<string, LegacySectionRef>(StringComparer.OrdinalIgnoreCase);
            private List<StaadSteelApp.LegacyDatabaseService.SectionItem> _allSectionsForShape =
                new List<StaadSteelApp.LegacyDatabaseService.SectionItem>();

            private sealed class LegacySectionRef
            {
                public string ZoneDb;
                public string ZoneName;
                public int CountryCode;
                public string Shape;
                public string SectionName;
                public string DisplayName;
            }

            internal LegacySectionController(MainForm owner, StaadSteelApp.LegacyDatabaseService service)
            {
                _owner = owner ?? throw new ArgumentNullException(nameof(owner));
                _service = service ?? throw new ArgumentNullException(nameof(service));
            }

            internal StaadSteelApp.LegacyDatabaseService Service
            {
                get { return _service; }
            }

            internal bool UiLoading { get; set; }

            internal void EnsureCache()
            {
                if (_cacheBuilt || _service == null) return;

                _cacheBuilt = true;

                try
                {
                    _zones = _service.GetZones() ?? new List<StaadSteelApp.LegacyDatabaseService.ZoneInfo>();
                }
                catch (Exception ex)
                {
                    _owner.LegacyWarnOnce(
                        "Legacy:Cache:Zones",
                        "Legacy: failed to read zones from SectionDatabaseInfo.xml. Legacy dropdowns may be empty.",
                        ex);
                    _zones = new List<StaadSteelApp.LegacyDatabaseService.ZoneInfo>();
                }

                _shapesByZone.Clear();
                _sectionsByZoneShape.Clear();
                _seriesByZoneShape.Clear();
                _sectionIndex.Clear();

                Debug.WriteLine($"[Legacy] EnsureLegacyCache: building cache for {_zones.Count} zones");

                foreach (var z in _zones)
                {
                    if (z == null || string.IsNullOrEmpty(z.DbFileName)) continue;

                    List<string> shapes;
                    try
                    {
                        shapes = _service.GetShapes(z.DbFileName) ?? new List<string>();
                    }
                    catch (Exception ex)
                    {
                        _owner.LegacyWarnOnce(
                            $"Legacy:Cache:Shapes:{z.DbFileName}",
                            $"Legacy: failed to read shapes from '{z.DbFileName}'.",
                            ex);
                        shapes = new List<string>();
                    }

                    _shapesByZone[z.DbFileName] = CopyList(shapes);

                    foreach (var shape in shapes)
                    {
                        if (string.IsNullOrEmpty(shape)) continue;

                        string key = $"{z.DbFileName}|{shape}";
                        List<StaadSteelApp.LegacyDatabaseService.SectionItem> sections;
                        try
                        {
                            sections = _service.GetSections(z.DbFileName, shape) ??
                                new List<StaadSteelApp.LegacyDatabaseService.SectionItem>();
                        }
                        catch (Exception ex)
                        {
                            _owner.LegacyWarnOnce(
                                $"Legacy:Cache:Sections:{z.DbFileName}|{shape}",
                                $"Legacy: failed to read sections from '{z.DbFileName}' table '{shape}'.",
                                ex);
                            sections = new List<StaadSteelApp.LegacyDatabaseService.SectionItem>();
                        }

                        _sectionsByZoneShape[key] = CopyList(sections);

                        var seriesSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var sec in sections)
                        {
                            string display = GetSectionText(sec);
                            string series = ExtractSeriesPrefix(display, shape);
                            if (!string.IsNullOrEmpty(series))
                                seriesSet.Add(series);

                            string nameKey = sec?.StaadName ?? display ?? "";
                            if (!string.IsNullOrEmpty(nameKey) && !_sectionIndex.ContainsKey(nameKey))
                            {
                                _sectionIndex[nameKey] = new LegacySectionRef
                                {
                                    ZoneDb = z.DbFileName,
                                    ZoneName = z.Name,
                                    CountryCode = z.StaadCountryCode,
                                    Shape = shape,
                                    SectionName = sec?.StaadName ?? "",
                                    DisplayName = sec?.DisplayName ?? ""
                                };
                            }
                        }

                        _seriesByZoneShape[key] = SortSeriesValues(seriesSet);
                    }
                }

                Debug.WriteLine($"[Legacy] EnsureLegacyCache: done — {_sectionIndex.Count} sections indexed");
            }

            internal void LoadZones()
            {
                if (_service == null) return;

                EnsureCache();

                _owner.cboZone.BeginUpdate();
                try
                {
                    _owner.cboZone.Items.Clear();
                    _owner.cboZone.DisplayMember = "Name";
                    _owner.cboZone.ValueMember = "DbFileName";
                    foreach (var z in _zones) _owner.cboZone.Items.Add(z);

                    if (_owner.cboZone.Items.Count > 0)
                    {
                        UiLoading = true;
                        try
                        {
                            _owner.cboZone.SelectedIndex = 0;
                        }
                        finally
                        {
                            UiLoading = false;
                        }

                        LoadShapes();
                    }
                }
                finally
                {
                    _owner.cboZone.EndUpdate();
                }
            }

            internal void LoadShapes()
            {
                _owner.cboShape.BeginUpdate();
                _owner.cboSection.BeginUpdate();
                try
                {
                    _owner.cboShape.Items.Clear();
                    _owner.cboSection.Items.Clear();

                    var z = _owner.cboZone.SelectedItem as StaadSteelApp.LegacyDatabaseService.ZoneInfo;
                    if (z == null) return;

                    try
                    {
                        EnsureCache();
                        if (!_shapesByZone.TryGetValue(z.DbFileName, out var shapes))
                        {
                            shapes = _service.GetShapes(z.DbFileName);
                            _shapesByZone[z.DbFileName] = CopyList(shapes);
                        }

                        var ordered = SortShapesForZone(z.Name, shapes);
                        foreach (var s in ordered) _owner.cboShape.Items.Add(s);
                        if (_owner.cboShape.Items.Count > 0)
                        {
                            UiLoading = true;
                            try
                            {
                                _owner.cboShape.SelectedIndex = 0;
                            }
                            finally
                            {
                                UiLoading = false;
                            }

                            LoadSeries();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"LoadLegacyShapes failed for zone '{z?.Name}': {ex}");
                        _owner.LegacyWarnOnce(
                            $"Legacy:Shapes:{z?.DbFileName}",
                            $"Legacy: could not load shapes for zone '{z?.Name}'. Sections list may be empty.",
                            ex);
                    }
                }
                finally
                {
                    _owner.cboShape.EndUpdate();
                    _owner.cboSection.EndUpdate();
                }

                _owner.UpdateLegacyTypeSpecUI();
            }

            internal void LoadSeries()
            {
                _owner.cboSeries.BeginUpdate();
                _owner.cboSection.BeginUpdate();
                try
                {
                    _owner.cboSeries.Items.Clear();
                    _owner.cboSection.Items.Clear();
                    _allSectionsForShape = new List<StaadSteelApp.LegacyDatabaseService.SectionItem>();

                    var z = _owner.cboZone.SelectedItem as StaadSteelApp.LegacyDatabaseService.ZoneInfo;
                    var shape = _owner.cboShape.SelectedItem as string;
                    if (z == null || string.IsNullOrEmpty(shape))
                    {
                        Debug.WriteLine($"[Legacy] LoadLegacySeries: skipped — zone={z?.Name}, shape='{shape}'");
                        return;
                    }

                    try
                    {
                        EnsureCache();
                        string key = $"{z.DbFileName}|{shape}";
                        if (!_sectionsByZoneShape.TryGetValue(key, out var sections) || sections == null || sections.Count == 0)
                        {
                            Debug.WriteLine($"[Legacy] LoadLegacySeries: cache MISS for key='{key}', reading from DB");
                            sections = _service.GetSections(z.DbFileName, shape) ??
                                new List<StaadSteelApp.LegacyDatabaseService.SectionItem>();
                            _sectionsByZoneShape[key] = CopyList(sections);
                        }
                        else
                        {
                            Debug.WriteLine($"[Legacy] LoadLegacySeries: cache HIT for key='{key}', count={sections.Count}");
                        }

                        _allSectionsForShape = CopyList(sections);
                        Debug.WriteLine($"[Legacy] LoadLegacySeries: _allSectionsForShape.Count={_allSectionsForShape.Count}");
                        if (_allSectionsForShape.Count == 0)
                        {
                            _owner.LegacyWarnOnce(
                                $"Legacy:SectionsEmpty:{key}",
                                $"Legacy: no sections loaded for Zone '{z.Name}', Shape '{shape}'. DB='{z.DbFileName}'. This usually indicates a database read issue.");
                        }

                        if (!_seriesByZoneShape.TryGetValue(key, out var seriesList) || seriesList == null || seriesList.Count == 0)
                        {
                            var seriesSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            foreach (var sec in _allSectionsForShape)
                            {
                                string series = ExtractSeriesPrefix(GetSectionText(sec), shape);
                                if (!string.IsNullOrEmpty(series))
                                    seriesSet.Add(series);
                            }

                            seriesList = SortSeriesValues(seriesSet);
                            _seriesByZoneShape[key] = seriesList;
                        }

                        _owner.cboSeries.Items.Add("(All)");
                        foreach (var s in seriesList)
                            _owner.cboSeries.Items.Add(s);

                        if (_owner.cboSeries.Items.Count > 0)
                        {
                            UiLoading = true;
                            try
                            {
                                _owner.cboSeries.SelectedIndex = 0;
                            }
                            finally
                            {
                                UiLoading = false;
                            }

                            LoadSections();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"LoadLegacySeries failed for zone '{z?.Name}' and shape '{shape}': {ex}");
                        _owner.LegacyWarnOnce(
                            $"Legacy:Series:{z?.DbFileName}|{shape}",
                            $"Legacy: could not load sections/series for zone '{z?.Name}', shape '{shape}'.",
                            ex);
                    }
                }
                finally
                {
                    _owner.cboSeries.EndUpdate();
                    _owner.cboSection.EndUpdate();
                }
            }

            internal void LoadSections()
            {
                _owner.cboSection.BeginUpdate();
                try
                {
                    _owner.cboSection.Items.Clear();

                    string selectedSeries = _owner.cboSeries.SelectedItem as string;
                    string shape = _owner.cboShape.SelectedItem as string ?? "";
                    if (string.IsNullOrEmpty(selectedSeries))
                    {
                        Debug.WriteLine("[Legacy] LoadLegacySections: skipped — no series selected");
                        return;
                    }

                    if ((_allSectionsForShape == null || _allSectionsForShape.Count == 0) && _service != null)
                    {
                        var z = _owner.cboZone.SelectedItem as StaadSteelApp.LegacyDatabaseService.ZoneInfo;
                        if (z != null && !string.IsNullOrEmpty(shape))
                        {
                            string key = $"{z.DbFileName}|{shape}";
                            Debug.WriteLine($"[Legacy] LoadLegacySections: _allSectionsForShape was empty, re-reading DB for key='{key}'");
                            try
                            {
                                var fresh = _service.GetSections(z.DbFileName, shape) ??
                                    new List<StaadSteelApp.LegacyDatabaseService.SectionItem>();
                                _sectionsByZoneShape[key] = CopyList(fresh);
                                _allSectionsForShape = CopyList(fresh);
                                Debug.WriteLine($"[Legacy] LoadLegacySections: re-read got {fresh.Count} sections");
                            }
                            catch (Exception ex)
                            {
                                _owner.LegacyWarnOnce(
                                    $"Legacy:ReloadSections:{key}",
                                    $"Legacy: could not reload sections for Zone '{z.Name}', Shape '{shape}'.",
                                    ex);
                            }
                        }
                    }

                    var filtered = _allSectionsForShape ?? new List<StaadSteelApp.LegacyDatabaseService.SectionItem>();
                    if (selectedSeries != "(All)")
                    {
                        filtered = (_allSectionsForShape ?? new List<StaadSteelApp.LegacyDatabaseService.SectionItem>())
                            .Where(s => ExtractSeriesPrefix(GetSectionText(s), shape)
                                .Equals(selectedSeries, StringComparison.OrdinalIgnoreCase))
                            .ToList();

                        if ((filtered == null || filtered.Count == 0) && _allSectionsForShape != null && _allSectionsForShape.Count > 0)
                        {
                            var z = _owner.cboZone.SelectedItem as StaadSteelApp.LegacyDatabaseService.ZoneInfo;
                            _owner.LegacyWarnOnce(
                                $"Legacy:SeriesFilterEmpty:{z?.DbFileName}|{shape}|{selectedSeries}",
                                $"Legacy: no sections matched Series '{selectedSeries}'. Showing all sections instead.");
                            filtered = _allSectionsForShape;
                        }
                    }

                    var ordered = SortSections(filtered, shape);
                    foreach (var s in ordered)
                        _owner.cboSection.Items.Add(s);

                    Debug.WriteLine($"[Legacy] LoadLegacySections: series='{selectedSeries}', filtered={filtered.Count}, final cboSection.Items={_owner.cboSection.Items.Count}");

                    if (_owner.cboSection.Items.Count > 0)
                    {
                        _owner.cboSection.SelectedIndex = 0;
                    }
                    else
                    {
                        var z = _owner.cboZone.SelectedItem as StaadSteelApp.LegacyDatabaseService.ZoneInfo;
                        Debug.WriteLine($"[Legacy] LoadLegacySections: EMPTY! zone='{z?.Name}', shape='{shape}', _allSectionsForShape.Count={_allSectionsForShape?.Count ?? -1}");
                        _owner.LegacyWarnOnce(
                            $"Legacy:SectionsDropdownEmpty:{z?.DbFileName}|{shape}",
                            $"Legacy: sections list is empty for Zone '{z?.Name}', Shape '{shape}'. This indicates a legacy database read issue or missing tables.");
                    }
                }
                finally
                {
                    _owner.cboSection.EndUpdate();
                }
            }

            internal void TryAutoSelect(string sectionName)
            {
                if (_service == null || string.IsNullOrEmpty(sectionName)) return;

                EnsureCache();
                if (!_sectionIndex.TryGetValue(sectionName, out var match))
                {
                    Debug.WriteLine($"[Legacy] TryAutoSelectLegacy: section '{sectionName}' not found in index");
                    return;
                }

                Debug.WriteLine($"[Legacy] TryAutoSelectLegacy: found '{sectionName}' → zone='{match.ZoneName}', shape='{match.Shape}'");

                UiLoading = true;
                try
                {
                    for (int i = 0; i < _owner.cboZone.Items.Count; i++)
                    {
                        if (_owner.cboZone.Items[i] is StaadSteelApp.LegacyDatabaseService.ZoneInfo z
                            && z.DbFileName == match.ZoneDb)
                        {
                            _owner.cboZone.SelectedIndex = i;
                            break;
                        }
                    }

                    LoadShapes();

                    for (int i = 0; i < _owner.cboShape.Items.Count; i++)
                    {
                        if (string.Equals(_owner.cboShape.Items[i].ToString(), match.Shape, StringComparison.OrdinalIgnoreCase))
                        {
                            _owner.cboShape.SelectedIndex = i;
                            break;
                        }
                    }

                    LoadSeries();
                }
                finally
                {
                    UiLoading = false;
                }

                for (int i = 0; i < _owner.cboSection.Items.Count; i++)
                {
                    if (_owner.cboSection.Items[i] is StaadSteelApp.LegacyDatabaseService.SectionItem item)
                    {
                        if (string.Equals(item.StaadName, sectionName, StringComparison.OrdinalIgnoreCase))
                        {
                            _owner.cboSection.SelectedIndex = i;
                            return;
                        }

                        if (!string.IsNullOrEmpty(match.DisplayName)
                            && string.Equals(item.DisplayName, match.DisplayName, StringComparison.OrdinalIgnoreCase))
                        {
                            _owner.cboSection.SelectedIndex = i;
                            return;
                        }
                    }
                }

                Debug.WriteLine($"[Legacy] TryAutoSelectLegacy: could not find section '{sectionName}' in cboSection ({_owner.cboSection.Items.Count} items)");
            }

            internal void PopulateSectionMetadata(StaadSteelApp.Core.SectionInfo info)
            {
                if (info == null || info.PropType != StaadSteelApp.Core.eFramePropType.Legacy)
                    return;

                string sectionName = (info.Name ?? string.Empty).Trim();
                if (sectionName.Length == 0 || _service == null)
                    return;

                EnsureCache();

                if (_sectionIndex.TryGetValue(sectionName, out var match))
                {
                    if (info.CountryCode <= 0)
                        info.CountryCode = match.CountryCode;

                    if (string.IsNullOrWhiteSpace(info.ShapeName))
                        info.ShapeName = match.Shape;

                    return;
                }

                foreach (var entry in _sectionIndex.Values)
                {
                    if (!string.Equals(entry?.DisplayName, sectionName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (info.CountryCode <= 0)
                        info.CountryCode = entry.CountryCode;

                    if (string.IsNullOrWhiteSpace(info.ShapeName))
                        info.ShapeName = entry.Shape;

                    return;
                }
            }

            private static List<T> CopyList<T>(IEnumerable<T> source)
            {
                return source == null ? new List<T>() : new List<T>(source);
            }
        }
    }
}
