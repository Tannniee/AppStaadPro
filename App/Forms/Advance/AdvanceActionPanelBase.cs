using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using StaadSteelApp.Core;

namespace StaadSteelApp
{
    internal abstract class AdvanceActionPanelBase : UserControl, IAdvanceActionPanel
    {
        protected AdvanceActionContext Context { get; private set; }
        protected int[] SelectedNodes { get; private set; } = Array.Empty<int>();

        public abstract string ActionText { get; }
        public abstract string ApplyText { get; }
        public virtual bool UsesNodeSelection => true;
        public Control View => this;

        protected AdvanceActionPanelBase()
        {
            Size = new Size(328, 160);
            Margin = Padding.Empty;
        }

        public virtual void AttachContext(AdvanceActionContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public virtual void OnSelectionChanged(int[] selectedNodes)
        {
            SelectedNodes = selectedNodes == null
                ? Array.Empty<int>()
                : selectedNodes.ToArray();
            NotifyHintChanged();
        }

        public abstract AdvanceActionHint GetHint();
        public abstract bool Apply();

        protected void NotifyHintChanged()
        {
            Context?.RequestHintUpdate();
        }

        protected Label AddLabel(Control parent, string name, string text, int x, int y, int width = 0, int height = 15)
        {
            var label = new Label
            {
                AutoSize = width <= 0,
                Location = new Point(x, y),
                Name = name,
                Size = width <= 0 ? new Size(10, height) : new Size(width, height),
                Text = text
            };
            parent.Controls.Add(label);
            return label;
        }

        protected TextBox AddTextBox(Control parent, string name, int x, int y, int width, bool readOnly = false)
        {
            var textBox = new TextBox
            {
                BackColor = readOnly ? Color.WhiteSmoke : SystemColors.Window,
                Location = new Point(x, y),
                Name = name,
                ReadOnly = readOnly,
                Size = new Size(width, 23)
            };
            parent.Controls.Add(textBox);
            return textBox;
        }

        protected Button AddButton(Control parent, string name, string text, int x, int y, EventHandler click)
        {
            var button = new Button
            {
                Location = new Point(x, y),
                Name = name,
                Size = new Size(76, 25),
                Text = text,
                UseVisualStyleBackColor = true
            };
            if (click != null)
                button.Click += click;
            parent.Controls.Add(button);
            return button;
        }

        protected RadioButton AddRadio(Control parent, string name, string text, int x, bool isChecked, EventHandler changed = null)
        {
            var radio = new RadioButton
            {
                AutoSize = true,
                Checked = isChecked,
                Location = new Point(x, 2),
                Name = name,
                Text = text,
                UseVisualStyleBackColor = true
            };
            if (changed != null)
                radio.CheckedChanged += changed;
            parent.Controls.Add(radio);
            return radio;
        }

        protected CheckBox AddCheck(Control parent, string name, string text, int x, int y, bool isChecked, EventHandler changed = null)
        {
            var check = new CheckBox
            {
                AutoSize = true,
                Checked = isChecked,
                Location = new Point(x, y),
                Name = name,
                Text = text,
                UseVisualStyleBackColor = true
            };
            if (changed != null)
                check.CheckedChanged += changed;
            parent.Controls.Add(check);
            return check;
        }

        protected Panel AddPanel(Control parent, string name, int x, int y, int width, int height)
        {
            var panel = new Panel
            {
                Location = new Point(x, y),
                Name = name,
                Size = new Size(width, height)
            };
            parent.Controls.Add(panel);
            return panel;
        }

        protected bool TryPickSingleSelectedNode(string role, out int nodeId)
        {
            nodeId = 0;
            if (!AdvanceService.TryGetSelectedNodes(out int[] picked, out string error))
            {
                Context.ShowWarning(error);
                return false;
            }

            if (picked.Length != 1)
            {
                Context.ShowWarning($"Please highlight exactly 1 node to pick as {role}. Current: {picked.Length}.");
                return false;
            }

            nodeId = picked[0];
            return true;
        }

        protected bool TryPickTwoSelectedNodes(string role, out int firstNodeId, out int secondNodeId)
        {
            firstNodeId = 0;
            secondNodeId = 0;
            if (!AdvanceService.TryGetSelectedNodes(out int[] picked, out string error))
            {
                Context.ShowWarning(error);
                return false;
            }

            if (picked.Length != 2)
            {
                Context.ShowWarning($"Please highlight exactly 2 nodes to define the {role}. Current: {picked.Length}.");
                return false;
            }

            firstNodeId = picked[0];
            secondNodeId = picked[1];
            return true;
        }

        protected static string FormatNodeListPreview(int[] nodeIds, int maxShown)
        {
            if (nodeIds == null || nodeIds.Length == 0)
                return "None";

            int take = Math.Max(1, maxShown);
            string text = string.Join(", ", nodeIds.Take(take));
            if (nodeIds.Length > take)
                text += ", ...";
            return text;
        }

        protected static bool TryParsePositiveDouble(string text, out double value)
        {
            return TryParseDoubleCore(text, out value) && value > 0;
        }

        protected static bool TryParseDouble(string text, out double value)
        {
            return TryParseDoubleCore(text, out value);
        }

        private static bool TryParseDoubleCore(string text, out double value)
        {
            string raw = (text ?? string.Empty).Trim();
            if (double.TryParse(raw, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
                return true;

            if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                return true;

            raw = raw.Replace(',', '.');
            return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        protected static AdvanceActionHint Error(string text)
        {
            return new AdvanceActionHint(true, text);
        }

        protected static AdvanceActionHint Info(string text)
        {
            return new AdvanceActionHint(false, text);
        }
    }
}
