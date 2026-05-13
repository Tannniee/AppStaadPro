using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using StaadSteelApp.AISC360;
using StaadSteelApp.Core;

namespace StaadSteelApp
{
    public partial class CheckForm : Form
    {
        private readonly HashSet<string> _warnOnceKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private void WarnOnce(string key, string message, Exception ex = null)
        {
            if (!string.IsNullOrWhiteSpace(key) && !_warnOnceKeys.Add(key))
                return;

            System.Diagnostics.Debug.WriteLine(ex == null ? message : (message + " | " + ex));
            TopMostMessageBox.ShowWarning(ex == null ? message : (message + "\n" + ex.Message), "STAAD Warning");
        }

        private List<MemberForceData> _allForces;
        private object _os;
        private DesignResults _currentResults;
        private Dictionary<string, CheckDetail> _checkMap = new Dictionary<string, CheckDetail>();
        private System.Windows.Forms.Timer _selectionTimer;
        private string _lastDetectedFrame = "";
        private bool _isAutoUpdateEnabled = true;
        private double? _staadDesignRatio;
        private int? _staadCriticalLoadCase;
        private DesignResultInfo _currentSteelInfo;
        private MemberForceData _currentGoverningForce;
        private string _currentManualSource = "";
        private List<ForceEnvelopeEntry> _currentForceEnvelope = new List<ForceEnvelopeEntry>();

        public CheckForm()
        {
            InitializeComponent();
            
            this.cboFrame.SelectedIndexChanged += new System.EventHandler(this.cboFrame_SelectedIndexChanged);
            this.dgvSummary.SelectionChanged += new System.EventHandler(this.dgvSummary_SelectionChanged);
            this.btnClose.Click += (s, e) => this.Close();
            this.btnCopy.Click += BtnCopy_Click;
            this.btnCopyDetail.Click += BtnCopyDetail_Click;
            this.btnDetect.Click += btnDetect_Click;
            this.chkIsBuiltUp.CheckedChanged += chkIsBuiltUp_CheckedChanged;
            this.FormClosing += CheckForm_FormClosing;
            this.Load += CheckForm_Load;
            this.StartPosition = FormStartPosition.CenterScreen;

            InitializeAutoDetect();
            
            var btnDiag = new Button();
            btnDiag.Text = "Self-Diag";
            btnDiag.Size = new Size(80, 26);
            btnDiag.Location = new Point(this.Width - 120, 10);
            btnDiag.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnDiag.Click += (s, e) => {
                string report = StaadSteelApp.AISC360.AISC360Verifier.RunVerification();
                txtDetails.Text = report;
                TopMostMessageBox.ShowSuccess("Verification completed. See Details pane.", "Diagnostics");
            };
            this.Controls.Add(btnDiag);
            btnDiag.BringToFront();

        }

        public void LoadData(List<MemberForceData> forces, object openStaad)
        {
            _allForces = forces;
            _os = openStaad;

            if (_allForces == null || _allForces.Count == 0) return;

            var members = _allForces.Select(f => f.MemberNo).Distinct().OrderBy(m => m).ToList();
            
            cboFrame.Items.Clear();
            foreach (var m in members)
            {
                cboFrame.Items.Add(m.ToString());
            }

            if (cboFrame.Items.Count > 0)
                cboFrame.SelectedIndex = 0;
        }

        public void LoadMembers(IEnumerable<int> memberIds, object openStaad)
        {
            _os = openStaad;
            if (_allForces == null)
                _allForces = new List<MemberForceData>();

            var members = (memberIds ?? Enumerable.Empty<int>())
                .Where(m => m > 0)
                .Distinct()
                .OrderBy(m => m)
                .ToList();

            cboFrame.SelectedIndexChanged -= cboFrame_SelectedIndexChanged;
            cboFrame.Items.Clear();
            foreach (int member in members)
                cboFrame.Items.Add(member.ToString());

            if (cboFrame.Items.Count > 0)
                cboFrame.SelectedIndex = 0;

            cboFrame.SelectedIndexChanged += cboFrame_SelectedIndexChanged;

            if (members.Count > 0)
            {
                UpdateBuiltUpOption(members[0]);
                PerformCalculation();
            }
        }

        private void CheckForm_Load(object sender, EventArgs e)
        {
            if (_os == null && !TryGetCheckOpenStaad(out _os))
            {
                Close();
                return;
            }

            if ((_allForces == null || _allForces.Count == 0) && cboFrame.Items.Count == 0)
                LoadSelectedBeams(false);

            _selectionTimer?.Start();
        }

        private void CheckForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _selectionTimer?.Stop();
        }

        private void cboFrame_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboFrame.SelectedIndex < 0) return;
            
            if (int.TryParse(cboFrame.SelectedItem.ToString(), out int memberNo))
                UpdateBuiltUpOption(memberNo);
            PerformCalculation();
        }

        private void btnDetect_Click(object sender, EventArgs e)
        {
            RunTaperedSizing();
        }

        private void chkIsBuiltUp_CheckedChanged(object sender, EventArgs e)
        {
            if (cboFrame.SelectedIndex >= 0)
                PerformCalculation();
        }

        private void lblCompareSub_Click(object sender, EventArgs e)
        {

        }
    }
}
