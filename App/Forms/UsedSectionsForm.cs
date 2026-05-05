using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using StaadSteelApp.Core;

namespace StaadSteelApp
{
    internal sealed class UsedSectionsListBox : Control
    {
        private readonly VScrollBar _scrollBar;
        private readonly List<UsedSectionEntry> _items = new List<UsedSectionEntry>();
        private readonly HashSet<int> _selectedIndices = new HashSet<int>();
        private bool _isDraggingSelection;
        private int _mouseDownIndex = -1;
        private int _selectionAnchorIndex = -1;
        private int _selectionActiveIndex = -1;
        private int _topIndex;
        private int _updateDepth;

        public event EventHandler SelectedIndexChanged;

        public UsedSectionsListBox()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.Selectable |
                ControlStyles.UserPaint,
                true);

            BackColor = SystemColors.Window;
            ForeColor = SystemColors.WindowText;
            TabStop = true;

            _scrollBar = new VScrollBar
            {
                Dock = DockStyle.Right,
                Visible = false
            };
            _scrollBar.Scroll += (s, e) =>
            {
                _topIndex = _scrollBar.Value;
                Invalidate();
            };
            Controls.Add(_scrollBar);
        }

        public IReadOnlyList<UsedSectionEntry> SelectedItems =>
            GetOrderedSelectedIndices()
                .Select(index => _items[index])
                .ToArray();

        public IReadOnlyList<int> SelectedIndices => GetOrderedSelectedIndices();

        public UsedSectionEntry SelectedItem
        {
            get
            {
                int index = _selectionActiveIndex;
                if (index >= 0 && index < _items.Count && _selectedIndices.Contains(index))
                    return _items[index];

                int[] orderedSelectedIndices = GetOrderedSelectedIndices();
                int fallbackIndex = orderedSelectedIndices.Length > 0
                    ? orderedSelectedIndices[orderedSelectedIndices.Length - 1]
                    : -1;
                return fallbackIndex >= 0 ? _items[fallbackIndex] : null;
            }
        }

        public void BeginUpdate()
        {
            _updateDepth++;
        }

        public void EndUpdate()
        {
            if (_updateDepth <= 0)
                return;

            _updateDepth--;
            if (_updateDepth == 0)
            {
                UpdateScrollBar();
                Invalidate();
            }
        }

        public void SetEntries(IEnumerable<UsedSectionEntry> entries)
        {
            _items.Clear();
            if (entries != null)
                _items.AddRange(entries);

            _selectedIndices.Clear();
            _selectionAnchorIndex = -1;
            _selectionActiveIndex = -1;
            _mouseDownIndex = -1;
            _isDraggingSelection = false;
            _topIndex = 0;

            UpdateScrollBar();
            InvalidateIfReady();
        }

        public void SetSelection(IEnumerable<int> indices, int anchorIndex, int activeIndex, bool notify)
        {
            int[] normalized = (indices ?? Array.Empty<int>())
                .Where(i => i >= 0 && i < _items.Count)
                .Distinct()
                .OrderBy(i => i)
                .ToArray();

            bool changed = !HasSameSelection(normalized);
            _selectedIndices.Clear();
            foreach (int index in normalized)
                _selectedIndices.Add(index);

            _selectionAnchorIndex = normalized.Length == 0 ? -1 : anchorIndex;
            _selectionActiveIndex = normalized.Length == 0 ? -1 : activeIndex;
            EnsureVisible(activeIndex);
            InvalidateIfReady();

            if (notify && (changed || normalized.Length > 0))
                OnSelectedIndexChanged(EventArgs.Empty);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button != MouseButtons.Left)
                return;

            int index = IndexFromPoint(e.Location);
            if (index < 0 || index >= _items.Count)
                return;

            _mouseDownIndex = index;
            Focus();
            bool ctrlPressed = (ModifierKeys & Keys.Control) == Keys.Control;
            bool shiftPressed = (ModifierKeys & Keys.Shift) == Keys.Shift;
            if (ctrlPressed)
            {
                ApplyToggleSelection(index, notify: true);
                _isDraggingSelection = false;
            }
            else if (shiftPressed && _selectionAnchorIndex >= 0)
            {
                ApplyRangeSelection(_selectionAnchorIndex, index, notify: true);
                _isDraggingSelection = true;
            }
            else
            {
                ApplySingleSelection(index, notify: true);
                _isDraggingSelection = true;
            }

            Capture = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (!_isDraggingSelection || e.Button != MouseButtons.Left || _selectionAnchorIndex < 0)
                return;

            int index = GetDragIndex(e.Location);
            if (index < 0 || index >= _items.Count)
                return;

            ApplyRangeSelection(_selectionAnchorIndex, index, notify: true);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            _isDraggingSelection = false;
            _mouseDownIndex = -1;
            Capture = false;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_items.Count == 0)
            {
                base.OnKeyDown(e);
                return;
            }

            int nextIndex = _selectionActiveIndex;
            if (nextIndex < 0)
                nextIndex = _selectedIndices.Count > 0 ? GetOrderedSelectedIndices().Max() : 0;

            if (e.KeyCode == Keys.Up)
            {
                nextIndex = Math.Max(0, nextIndex - 1);
            }
            else if (e.KeyCode == Keys.Down)
            {
                nextIndex = Math.Min(_items.Count - 1, nextIndex + 1);
            }
            else
            {
                base.OnKeyDown(e);
                return;
            }

            ApplySingleSelection(nextIndex, notify: true);
            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        protected override bool IsInputKey(Keys keyData)
        {
            Keys keyCode = keyData & Keys.KeyCode;
            if (keyCode == Keys.Up || keyCode == Keys.Down)
                return true;

            return base.IsInputKey(keyData);
        }

        private void ApplySingleSelection(int index, bool notify)
        {
            SetSelection(index >= 0 ? new[] { index } : Array.Empty<int>(), index, index, notify);
        }

        private void ApplyToggleSelection(int index, bool notify)
        {
            if (index < 0 || index >= _items.Count)
                return;

            var selected = GetOrderedSelectedIndices().ToList();
            if (selected.Contains(index))
            {
                selected.Remove(index);
            }
            else
            {
                selected.Add(index);
            }

            int anchorIndex = _selectionAnchorIndex >= 0 ? _selectionAnchorIndex : index;
            if (selected.Count == 0)
                anchorIndex = -1;
            else if (!selected.Contains(anchorIndex))
                anchorIndex = index;

            int activeIndex = selected.Count == 0 ? -1 : index;
            SetSelection(selected, anchorIndex, activeIndex, notify);
        }

        private void ApplyRangeSelection(int anchorIndex, int activeIndex, bool notify)
        {
            if (anchorIndex < 0 || activeIndex < 0)
                return;

            int start = Math.Min(anchorIndex, activeIndex);
            int end = Math.Max(anchorIndex, activeIndex);
            SetSelection(Enumerable.Range(start, end - start + 1), anchorIndex, activeIndex, notify);
        }

        private bool HasSameSelection(IReadOnlyList<int> expected)
        {
            if (_selectedIndices.Count != expected.Count)
                return false;

            for (int i = 0; i < expected.Count; i++)
            {
                if (!_selectedIndices.Contains(expected[i]))
                    return false;
            }

            return true;
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            UpdateScrollBar();
            InvalidateIfReady();
        }

        private void EnsureVisible(int index)
        {
            if (index < 0 || index >= _items.Count)
                return;

            int visibleCount = GetVisibleRowCount();
            if (index < _topIndex)
            {
                _topIndex = index;
            }
            else if (index >= _topIndex + visibleCount)
            {
                _topIndex = Math.Max(0, index - visibleCount + 1);
            }

            SyncScrollBar();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            int visibleCount = GetVisibleRowCount();
            if (_items.Count <= visibleCount)
                return;

            int deltaRows = e.Delta > 0 ? -3 : 3;
            SetTopIndex(_topIndex + deltaRows);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateScrollBar();
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            InvalidateIfReady();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            InvalidateIfReady();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Rectangle viewport = GetViewportRectangle();
            using (var backgroundBrush = new SolidBrush(BackColor))
            {
                e.Graphics.FillRectangle(backgroundBrush, ClientRectangle);
            }

            if (viewport.Width <= 0 || viewport.Height <= 0)
                return;

            int itemHeight = GetItemHeight();
            int visibleCount = GetVisibleRowCount();
            int lastIndex = Math.Min(_items.Count, _topIndex + visibleCount);

            for (int i = _topIndex; i < lastIndex; i++)
            {
                Rectangle itemBounds = new Rectangle(
                    0,
                    (i - _topIndex) * itemHeight,
                    viewport.Width,
                    itemHeight);

                bool selected = _selectedIndices.Contains(i);
                Color backColor = selected ? SystemColors.Highlight : BackColor;
                Color textColor = selected ? SystemColors.HighlightText : ForeColor;

                using (var itemBrush = new SolidBrush(backColor))
                {
                    e.Graphics.FillRectangle(itemBrush, itemBounds);
                }

                Rectangle textBounds = Rectangle.Inflate(itemBounds, -4, 0);
                TextRenderer.DrawText(
                    e.Graphics,
                    _items[i]?.DisplayText ?? string.Empty,
                    Font,
                    textBounds,
                    textColor,
                    TextFormatFlags.Left |
                    TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis |
                    TextFormatFlags.NoPrefix);

                if (Focused && i == _selectionActiveIndex)
                    ControlPaint.DrawFocusRectangle(e.Graphics, itemBounds, textColor, backColor);
            }
        }

        private int IndexFromPoint(Point location)
        {
            Rectangle viewport = GetViewportRectangle();
            if (!viewport.Contains(location))
                return -1;

            int itemHeight = GetItemHeight();
            int index = _topIndex + (location.Y / itemHeight);
            return index >= 0 && index < _items.Count ? index : -1;
        }

        private int GetDragIndex(Point location)
        {
            Rectangle viewport = GetViewportRectangle();
            if (viewport.Width <= 0 || viewport.Height <= 0 || _items.Count == 0)
                return -1;

            int clampedY = Math.Max(0, Math.Min(viewport.Height - 1, location.Y));
            int index = _topIndex + (clampedY / GetItemHeight());
            return Math.Max(0, Math.Min(_items.Count - 1, index));
        }

        private int GetItemHeight()
        {
            return Math.Max(Font.Height + 4, 18);
        }

        private int GetVisibleRowCount()
        {
            Rectangle viewport = GetViewportRectangle();
            return Math.Max(1, viewport.Height / Math.Max(1, GetItemHeight()));
        }

        private Rectangle GetViewportRectangle()
        {
            int width = ClientSize.Width - (_scrollBar.Visible ? _scrollBar.Width : 0);
            return new Rectangle(0, 0, Math.Max(0, width), ClientSize.Height);
        }

        private int[] GetOrderedSelectedIndices()
        {
            return _selectedIndices.OrderBy(index => index).ToArray();
        }

        private void UpdateScrollBar()
        {
            int visibleCount = GetVisibleRowCount();
            bool showScroll = _items.Count > visibleCount;

            _scrollBar.Visible = showScroll;
            _scrollBar.Enabled = showScroll;

            if (!showScroll)
            {
                _topIndex = 0;
                _scrollBar.Value = 0;
                InvalidateIfReady();
                return;
            }

            int maximumTopIndex = Math.Max(0, _items.Count - visibleCount);
            _topIndex = Math.Max(0, Math.Min(_topIndex, maximumTopIndex));
            _scrollBar.Minimum = 0;
            _scrollBar.LargeChange = Math.Max(1, visibleCount);
            _scrollBar.SmallChange = 1;
            _scrollBar.Maximum = maximumTopIndex + _scrollBar.LargeChange - 1;
            _scrollBar.Value = Math.Max(_scrollBar.Minimum, Math.Min(_topIndex, maximumTopIndex));
            InvalidateIfReady();
        }

        private void SyncScrollBar()
        {
            if (!_scrollBar.Visible)
                return;

            int maximumValue = Math.Max(_scrollBar.Minimum, _scrollBar.Maximum - _scrollBar.LargeChange + 1);
            _scrollBar.Value = Math.Max(_scrollBar.Minimum, Math.Min(_topIndex, maximumValue));
            InvalidateIfReady();
        }

        private void SetTopIndex(int topIndex)
        {
            int maximumTopIndex = Math.Max(0, _items.Count - GetVisibleRowCount());
            _topIndex = Math.Max(0, Math.Min(topIndex, maximumTopIndex));
            SyncScrollBar();
        }

        private void InvalidateIfReady()
        {
            if (_updateDepth == 0)
                Invalidate();
        }

        private void OnSelectedIndexChanged(EventArgs e)
        {
            SelectedIndexChanged?.Invoke(this, e);
        }
    }

    internal sealed class UsedSectionsForm : Form
    {
        private readonly UsedSectionsListBox _lstSections;
        private readonly GroupBox _grpSections;
        private bool _isBinding;
        private bool _locationInitialized;
        private bool _suppressLocationSave;

        public event EventHandler SectionActivated;

        public UsedSectionEntry SelectedEntry => _lstSections.SelectedItem;

        public int SelectedEntryCount => _lstSections.SelectedItems.Count;

        public int[] SelectedPropertyIds
        {
            get
            {
                return _lstSections.SelectedItems
                    .SelectMany(entry => entry.PropertyIds ?? new[] { entry.PropertyId })
                    .Where(id => id > 0)
                    .Distinct()
                    .OrderBy(id => id)
                    .ToArray();
            }
        }

        public int[] SelectedBeamIds
        {
            get
            {
                return _lstSections.SelectedItems
                    .SelectMany(entry => entry.BeamIds ?? Array.Empty<int>())
                    .Where(id => id > 0)
                    .Distinct()
                    .OrderBy(id => id)
                    .ToArray();
            }
        }

        public UsedSectionsForm()
        {
            Text = "Used Sections";
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            ClientSize = new Size(230, 360);

            _grpSections = new GroupBox
            {
                Text = "Used Sections",
                Location = new Point(12, 12),
                Size = new Size(206, 336)
            };

            _lstSections = new UsedSectionsListBox
            {
                Location = new Point(10, 20),
                Size = new Size(186, 306)
            };
            _lstSections.SelectedIndexChanged += (s, e) =>
            {
                if (_isBinding)
                    return;

                SectionActivated?.Invoke(this, EventArgs.Empty);
            };

            _grpSections.Controls.Add(_lstSections);
            Controls.Add(_grpSections);

            Move += (s, e) => SaveLocation();
        }

        public void SetEntries(IReadOnlyList<UsedSectionEntry> entries, int[] selectedPropertyIds = null, int selectedPropertyId = 0)
        {
            var items = entries == null ? new List<UsedSectionEntry>() : entries.ToList();
            var selectedIds = new HashSet<int>((selectedPropertyIds ?? Array.Empty<int>()).Where(id => id > 0));
            if (selectedPropertyId > 0)
            {
                selectedIds.Clear();
                selectedIds.Add(selectedPropertyId);
            }

            _isBinding = true;
            _lstSections.BeginUpdate();
            try
            {
                _lstSections.SetEntries(items);
                _grpSections.Text = $"Used Sections ({items.Count})";

                var indicesToSelect = new List<int>();
                int activeIndex = -1;
                if (selectedIds.Count > 0)
                {
                    for (int i = 0; i < items.Count; i++)
                    {
                        int[] propertyIds = items[i].PropertyIds ?? new[] { items[i].PropertyId };
                        if (propertyIds.Any(selectedIds.Contains))
                        {
                            indicesToSelect.Add(i);
                            activeIndex = i;
                        }
                    }
                }

                _lstSections.SetSelection(indicesToSelect, indicesToSelect.Count > 0 ? indicesToSelect[0] : -1, activeIndex, notify: false);
            }
            finally
            {
                _lstSections.EndUpdate();
                _isBinding = false;
            }
        }

        public void RestoreLocation(Point defaultLocation)
        {
            _suppressLocationSave = true;
            try
            {
                string currentDisplaySignature = WindowPlacementHelper.GetDisplaySignature();
                string savedDisplaySignature = AppSettings.LoadString("UsedSections_DisplaySignature");
                bool hasLocation = string.Equals(AppSettings.LoadString("UsedSections_HasLocation"), "1", StringComparison.Ordinal);

                Point location;
                if (!hasLocation)
                {
                    location = WindowPlacementHelper.EnsureVisibleLocation(defaultLocation, Size);
                }
                else if (!string.IsNullOrWhiteSpace(savedDisplaySignature) &&
                         !string.Equals(savedDisplaySignature, currentDisplaySignature, StringComparison.Ordinal))
                {
                    location = WindowPlacementHelper.GetDefaultVisibleLocation(Size);
                }
                else
                {
                    location = WindowPlacementHelper.EnsureVisibleLocation(AppSettings.Load("UsedSections"), Size);
                }

                StartPosition = FormStartPosition.Manual;
                Location = location;
                _locationInitialized = true;
            }
            finally
            {
                _suppressLocationSave = false;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            SaveLocation();
            base.OnFormClosing(e);
        }

        private void SaveLocation()
        {
            if (!_locationInitialized || _suppressLocationSave)
                return;

            AppSettings.Save("UsedSections", Location);
            AppSettings.SaveString("UsedSections_HasLocation", "1");
            AppSettings.SaveString("UsedSections_DisplaySignature", WindowPlacementHelper.GetDisplaySignature());
        }
    }
}
