using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using StaadSteelApp.Core;

namespace StaadSteelApp
{
    internal sealed partial class UsedSectionsListBox : Control
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

        private int[] GetOrderedSelectedIndices()
        {
            return _selectedIndices.OrderBy(index => index).ToArray();
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
}
