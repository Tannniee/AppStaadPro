using System;
using System.Windows.Forms;
using StaadSteelApp.Core;

namespace StaadSteelApp
{
    public partial class LoadTreeForm : Form
    {
        private string _boundModelKey = string.Empty;

        public LoadTreeForm()
        {
            InitializeComponent();
            RefreshTree();
        }

        public void RefreshTree()
        {
            LoadTree();
        }

        internal void HandleActiveModelChanged(bool connected)
        {
            treeLoads.Nodes.Clear();
            _boundModelKey = string.Empty;
            if (connected)
                RefreshTree();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            RefreshTree();
        }

        private void btnExpandAll_Click(object sender, EventArgs e)
        {
            treeLoads.ExpandAll();
        }

        private void btnCollapseAll_Click(object sender, EventArgs e)
        {
            treeLoads.CollapseAll();
            if (treeLoads.Nodes.Count > 0)
                treeLoads.Nodes[0].Expand();
        }

        private void LoadTree()
        {
            _boundModelKey = string.Empty;
            try
            {
                if (!StaadManager.TryGetConnection(out StaadConnectionSnapshot connection, out string connectionError))
                    throw new InvalidOperationException(connectionError);

                UseWaitCursor = true;
                treeLoads.BeginUpdate();
                treeLoads.Nodes.Clear();

                var root = new TreeNode("Load Cases Details");
                foreach (var loadCase in LoadService.GetPrimaryLoadTree(connection.OpenStaad))
                {
                    string title = string.IsNullOrWhiteSpace(loadCase.Title)
                        ? loadCase.Number.ToString()
                        : $"{loadCase.Number}: {loadCase.Title}";

                    var caseNode = new TreeNode(title);

                    foreach (var item in loadCase.Items)
                    {
                        caseNode.Nodes.Add(new TreeNode(item.Summary) { Tag = item });
                    }

                    root.Nodes.Add(caseNode);
                }

                treeLoads.Nodes.Add(root);
                _boundModelKey = connection.Identity.CacheKey;
                root.Expand();
                if (root.Nodes.Count > 0)
                    root.Nodes[0].Expand();
            }
            catch (Exception ex)
            {
                TopMostMessageBox.ShowError("Could not read load tree: " + ex.Message, "Load Tree");
            }
            finally
            {
                treeLoads.EndUpdate();
                UseWaitCursor = false;
            }
        }

        private void treeLoads_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (!(e.Node?.Tag is LoadService.LoadTreeItem item))
                return;

            try
            {
                string expectedModelKey = _boundModelKey;
                if (!StaadManager.TryGetConnection(out StaadConnectionSnapshot connection, out string connectionError))
                    throw new InvalidOperationException(connectionError);

                if (string.IsNullOrWhiteSpace(expectedModelKey) ||
                    !string.Equals(expectedModelKey, connection.Identity.CacheKey, StringComparison.OrdinalIgnoreCase))
                {
                    TopMostMessageBox.ShowWarning(
                        "The active STAAD model changed. The load tree was refreshed; select the load item again.",
                        "Load Tree");
                    return;
                }

                LoadService.HighlightLoadAssignments(connection.OpenStaad, item);
            }
            catch (Exception ex)
            {
                TopMostMessageBox.ShowError("Could not highlight load item: " + ex.Message, "Load Tree");
            }
        }
    }
}
