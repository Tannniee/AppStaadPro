using System;
using System.Drawing;
using System.Windows.Forms;

namespace StaadSteelApp
{
    internal sealed partial class UsedSectionsListBox
    {
        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            UpdateScrollBar();
            InvalidateIfReady();
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
    }
}
