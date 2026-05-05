using System;
using System.Globalization;
using System.Drawing;
using System.Windows.Forms;
using StaadSteelApp.Core;

namespace StaadSteelApp
{
    public partial class AssignLoadForm : Form
    {
        private const int LoadTreeShiftWidth = 350;
        private readonly int _collapsedClientWidth;
        private LoadTreeForm _loadTreeForm;

        public AssignLoadForm()
        {
            InitializeComponent();

            _collapsedClientWidth = ClientSize.Width;

            rbMemberLoad.Checked = true;
            rbSelectedSource.Checked = true;
            rbGlobalY.Checked = true;
            UpdateMode();
            LoadCases();
        }

        private void LoadCases()
        {
            try
            {
                cboLoadCases.BeginUpdate();
                cboLoadCases.Items.Clear();

                foreach (var loadCase in LoadService.GetPrimaryLoadCases())
                {
                    cboLoadCases.Items.Add(loadCase);
                }

                if (cboLoadCases.Items.Count > 0)
                {
                    cboLoadCases.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                TopMostMessageBox.ShowError("Could not load STAAD load cases: " + ex.Message, "Assign Load");
            }
            finally
            {
                cboLoadCases.EndUpdate();
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            LoadCases();
            RefreshEmbeddedLoadTree();
        }

        private void btnAssign_Click(object sender, EventArgs e)
        {
            if (!(cboLoadCases.SelectedItem is LoadService.LoadCaseItem selectedCase))
            {
                TopMostMessageBox.ShowWarning("Please select a load case.", "Assign Load");
                return;
            }

            try
            {
                bool useGroup = rbGroupSource.Checked;
                string groupName = useGroup ? cboGroups.SelectedItem as string : string.Empty;

                if (useGroup && string.IsNullOrWhiteSpace(groupName))
                {
                    TopMostMessageBox.ShowWarning("Please select a group.", "Assign Load");
                    return;
                }

                if (rbMemberLoad.Checked)
                {
                    double force = ParseDouble(txtMemberForce, "Uniform load");
                    var direction = GetSelectedDirection();

                    if (useGroup)
                    {
                        LoadService.AssignUniformMemberLoadToGroup(selectedCase.Number, groupName, direction, force);
                    }
                    else
                    {
                        LoadService.AssignUniformMemberLoad(selectedCase.Number, direction, force);
                    }

                    TopMostMessageBox.ShowSuccess(
                        useGroup
                            ? $"Uniform member load assigned to group '{groupName}' in load case {selectedCase.Number}."
                            : $"Uniform member load assigned to selected members in load case {selectedCase.Number}.",
                        "Assign Load");
                }
                else
                {
                    double fx = ParseDouble(txtFx, "Fx");
                    double fy = ParseDouble(txtFy, "Fy");
                    double fz = ParseDouble(txtFz, "Fz");
                    double mx = ParseDouble(txtMx, "Mx");
                    double my = ParseDouble(txtMy, "My");
                    double mz = ParseDouble(txtMz, "Mz");

                    if (useGroup)
                    {
                        LoadService.AssignNodalLoadToGroup(selectedCase.Number, groupName, fx, fy, fz, mx, my, mz);
                    }
                    else
                    {
                        LoadService.AssignNodalLoad(selectedCase.Number, fx, fy, fz, mx, my, mz);
                    }

                    TopMostMessageBox.ShowSuccess(
                        useGroup
                            ? $"Nodal load assigned to group '{groupName}' in load case {selectedCase.Number}."
                            : $"Nodal load assigned to selected nodes in load case {selectedCase.Number}.",
                        "Assign Load");
                }

                RefreshEmbeddedLoadTree();
            }
            catch (Exception ex)
            {
                TopMostMessageBox.ShowError(ex.Message, "Assign Load");
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnLoadTree_Click(object sender, EventArgs e)
        {
            try
            {
                if (pnlLoadTreeHost.Visible)
                {
                    HideEmbeddedLoadTree();
                }
                else
                {
                    ShowEmbeddedLoadTree();
                }
            }
            catch (Exception ex)
            {
                TopMostMessageBox.ShowError("Could not open load tree: " + ex.Message, "Load Tree");
            }
        }

        private void ShowEmbeddedLoadTree()
        {
            EnsureLoadTreeHost();

            if (pnlLoadTreeHost.Visible)
            {
                RefreshEmbeddedLoadTree();
                return;
            }

            SuspendLayout();
            ShiftMainControls(LoadTreeShiftWidth);
            ClientSize = new Size(_collapsedClientWidth + LoadTreeShiftWidth, ClientSize.Height);
            pnlLoadTreeHost.Visible = true;
            btnLoadTree.Text = "Hide Tree";
            ResumeLayout();

            RefreshEmbeddedLoadTree();
        }

        private void HideEmbeddedLoadTree()
        {
            if (!pnlLoadTreeHost.Visible)
                return;

            SuspendLayout();
            pnlLoadTreeHost.Visible = false;
            ShiftMainControls(-LoadTreeShiftWidth);
            ClientSize = new Size(_collapsedClientWidth, ClientSize.Height);
            btnLoadTree.Text = "Load Tree";
            ResumeLayout();
        }

        private void EnsureLoadTreeHost()
        {
            if (_loadTreeForm != null && !_loadTreeForm.IsDisposed)
                return;

            pnlLoadTreeHost.Controls.Clear();

            _loadTreeForm = new LoadTreeForm
            {
                TopLevel = false,
                FormBorderStyle = FormBorderStyle.None,
                Dock = DockStyle.Fill
            };

            pnlLoadTreeHost.Controls.Add(_loadTreeForm);
            _loadTreeForm.Show();
        }

        private void RefreshEmbeddedLoadTree()
        {
            if (_loadTreeForm != null && !_loadTreeForm.IsDisposed)
            {
                _loadTreeForm.RefreshTree();
            }
        }

        private void ShiftMainControls(int offset)
        {
            lblLoadCase.Left += offset;
            cboLoadCases.Left += offset;
            btnRefresh.Left += offset;
            grpApplyTo.Left += offset;
            grpAssignType.Left += offset;
            grpMemberLoad.Left += offset;
            grpNodalLoad.Left += offset;
            btnAssign.Left += offset;
            btnClose.Left += offset;
            btnLoadTree.Left += offset;
        }

        private void rbAssignType_CheckedChanged(object sender, EventArgs e)
        {
            UpdateMode();
        }

        private void rbSource_CheckedChanged(object sender, EventArgs e)
        {
            UpdateMode();
        }

        private void UpdateMode()
        {
            bool memberMode = rbMemberLoad.Checked;
            bool groupMode = rbGroupSource.Checked;

            grpMemberLoad.Enabled = memberMode;
            grpNodalLoad.Enabled = !memberMode;
            lblGroup.Enabled = groupMode;
            cboGroups.Enabled = groupMode;

            if (groupMode)
            {
                LoadGroups();
            }
        }

        private void LoadGroups()
        {
            try
            {
                string selectedGroup = cboGroups.SelectedItem as string;
                var groupType = rbMemberLoad.Checked
                    ? LoadService.GroupType.Member
                    : LoadService.GroupType.Node;

                cboGroups.BeginUpdate();
                cboGroups.Items.Clear();

                foreach (string groupName in LoadService.GetGroupNames(groupType))
                {
                    cboGroups.Items.Add(groupName);
                }

                if (!string.IsNullOrWhiteSpace(selectedGroup))
                {
                    int existingIndex = cboGroups.FindStringExact(selectedGroup);
                    if (existingIndex >= 0)
                    {
                        cboGroups.SelectedIndex = existingIndex;
                        return;
                    }
                }

                if (cboGroups.Items.Count > 0)
                {
                    cboGroups.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                TopMostMessageBox.ShowError("Could not load STAAD groups: " + ex.Message, "Assign Load");
            }
            finally
            {
                cboGroups.EndUpdate();
            }
        }

        private LoadService.MemberLoadDirection GetSelectedDirection()
        {
            if (rbLocalX.Checked) return LoadService.MemberLoadDirection.LocalX;
            if (rbLocalY.Checked) return LoadService.MemberLoadDirection.LocalY;
            if (rbLocalZ.Checked) return LoadService.MemberLoadDirection.LocalZ;
            if (rbGlobalX.Checked) return LoadService.MemberLoadDirection.GlobalX;
            if (rbGlobalY.Checked) return LoadService.MemberLoadDirection.GlobalY;
            if (rbGlobalZ.Checked) return LoadService.MemberLoadDirection.GlobalZ;
            if (rbProjectedX.Checked) return LoadService.MemberLoadDirection.ProjectedX;
            if (rbProjectedY.Checked) return LoadService.MemberLoadDirection.ProjectedY;
            if (rbProjectedZ.Checked) return LoadService.MemberLoadDirection.ProjectedZ;
            return LoadService.MemberLoadDirection.GlobalY;
        }

        private static double ParseDouble(TextBox textBox, string fieldName)
        {
            string text = (textBox.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(text))
                return 0.0;

            if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out double current))
                return current;

            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double invariant))
                return invariant;

            throw new InvalidOperationException(fieldName + " must be a number.");
        }
    }
}
