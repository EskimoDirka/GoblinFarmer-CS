using System.Drawing;
using System.Windows.Forms;

namespace GoblinFarmer
{
    public partial class frmMain
    {
        private const int PortGoblinOverlayTopInset = 8;
        private const int PortGoblinOverlayWidth = 760;
        private const int PortGoblinOverlayHeight = 34;
        private readonly object portGoblinOverlayLock = new();
        private Form? portGoblinOverlayForm;
        private Label? portGoblinOverlayLabel;
        private PortGoblinOverlayAcceptedCountState? portGoblinOverlayAcceptedCount;

        private sealed record PortGoblinOverlayAcceptedCountState(
            int Count,
            string GoblinType,
            string AcceptedAreaKey,
            string DisplayArea,
            string Source,
            DateTime AcceptedUtc);

        private sealed class PortNoActivateGoblinOverlayForm : Form
        {
            private const int WS_EX_NOACTIVATE = 0x08000000;
            private const int WS_EX_TOOLWINDOW = 0x00000080;
            private const int WS_EX_TRANSPARENT = 0x00000020;

            protected override bool ShowWithoutActivation => true;

            protected override CreateParams CreateParams
            {
                get
                {
                    CreateParams createParams = base.CreateParams;
                    createParams.ExStyle |= WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW | WS_EX_TRANSPARENT;
                    return createParams;
                }
            }
        }

        private void PortInitializeGoblinOverlay()
        {
            if (!AppSettings.IsVsDebugProfile ||
                portGoblinOverlayForm is { IsDisposed: false })
            {
                return;
            }

            portGoblinOverlayForm = new PortNoActivateGoblinOverlayForm
            {
                Name = "frmGoblinOverlay",
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                ShowInTaskbar = false,
                TopMost = true,
                BackColor = Color.FromArgb(24, 24, 24),
                Opacity = 0.82,
                Width = PortGoblinOverlayWidth,
                Height = PortGoblinOverlayHeight,
            };

            portGoblinOverlayLabel = new Label
            {
                Name = "lblGoblinOverlay",
                Dock = DockStyle.Fill,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font(Font.FontFamily, 12.0f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoEllipsis = true,
                UseMnemonic = false,
                Text = PortFormatGoblinOverlayText(0, "--", "--", false),
            };

            portGoblinOverlayForm.Controls.Add(portGoblinOverlayLabel);
        }

        private void PortUpdateGoblinOverlay(bool diabloRunning)
        {
            if (!AppSettings.IsVsDebugProfile)
            {
                return;
            }

            IntPtr diabloWindow = FindDiabloWindow();
            if (!diabloRunning ||
                diabloWindow == IntPtr.Zero ||
                IsIconic(diabloWindow) ||
                !PortTryGetDiabloRect(out RECT rect))
            {
                PortHideGoblinOverlay();
                return;
            }

            PortInitializeGoblinOverlay();
            if (portGoblinOverlayForm == null || portGoblinOverlayLabel == null)
            {
                return;
            }

            string text = PortBuildGoblinOverlayText();
            if (!string.Equals(portGoblinOverlayLabel.Text, text, StringComparison.Ordinal))
            {
                portGoblinOverlayLabel.Text = text;
            }

            int diabloWidth = rect.Right - rect.Left;
            portGoblinOverlayForm.Left = rect.Left + (diabloWidth - portGoblinOverlayForm.Width) / 2;
            portGoblinOverlayForm.Top = rect.Top + PortGoblinOverlayTopInset;
            if (!portGoblinOverlayForm.Visible)
            {
                portGoblinOverlayForm.Show();
            }
        }

        private string PortBuildGoblinOverlayText()
        {
            PortGoblinOverlayAcceptedCountState? state;
            lock (portGoblinOverlayLock)
            {
                state = portGoblinOverlayAcceptedCount;
            }

            if (state == null)
            {
                return PortFormatGoblinOverlayText(0, "--", "--", false);
            }

            bool goNext = PortGoblinOverlayShouldGoNext(state.AcceptedAreaKey, portLastConfirmedLocation);
            return PortFormatGoblinOverlayText(state.Count, state.GoblinType, state.DisplayArea, goNext);
        }

        private static string PortFormatGoblinOverlayText(int total, string goblinType, string area, bool goNext)
        {
            string displayGoblinType = string.IsNullOrWhiteSpace(goblinType) ? "--" : goblinType.Trim();
            string displayArea = string.IsNullOrWhiteSpace(area) ? "--" : area.Trim();
            return $"Count: {Math.Max(0, total)}  Goblin: {displayGoblinType}  Area: {displayArea}  Go Next: {(goNext ? "Y" : "N")}";
        }

        private bool PortGoblinOverlayShouldGoNext(string lastAcceptedAreaKey, string currentConfirmedLocation)
        {
            if (string.IsNullOrWhiteSpace(lastAcceptedAreaKey) ||
                string.IsNullOrWhiteSpace(currentConfirmedLocation))
            {
                return false;
            }

            string acceptedKey = PortLocationKey(lastAcceptedAreaKey);
            string currentKey = PortLocationKey(currentConfirmedLocation);
            return !string.IsNullOrWhiteSpace(acceptedKey) &&
                acceptedKey.Equals(currentKey, StringComparison.OrdinalIgnoreCase);
        }

        private void PortSetGoblinOverlayAcceptedCount(
            int total,
            string goblinType,
            string acceptedAreaKey,
            string displayArea,
            string source,
            DateTime acceptedUtc)
        {
            if (!AppSettings.IsVsDebugProfile || total <= 0)
            {
                return;
            }

            lock (portGoblinOverlayLock)
            {
                portGoblinOverlayAcceptedCount = new PortGoblinOverlayAcceptedCountState(
                    total,
                    string.IsNullOrWhiteSpace(goblinType) ? "--" : goblinType.Trim(),
                    PortLocationKey(acceptedAreaKey),
                    string.IsNullOrWhiteSpace(displayArea) ? "--" : displayArea.Trim(),
                    string.IsNullOrWhiteSpace(source) ? "Unknown" : source.Trim(),
                    acceptedUtc);
            }

            AppLogger.Info(
                "GoblinOverlayUpdated: " +
                $"total={total}; " +
                $"goblinType={PortLogField(goblinType)}; " +
                $"acceptedAreaKey={PortLogField(PortDisplayLocation(acceptedAreaKey))}; " +
                $"displayArea={PortLogField(displayArea)}; " +
                $"source={PortLogField(source)}; " +
                $"acceptedUtc={acceptedUtc:O}");
        }

        private void PortResetGoblinOverlayState(string reason)
        {
            lock (portGoblinOverlayLock)
            {
                portGoblinOverlayAcceptedCount = null;
            }

            if (portGoblinOverlayLabel != null && !portGoblinOverlayLabel.IsDisposed)
            {
                portGoblinOverlayLabel.Text = PortFormatGoblinOverlayText(0, "--", "--", false);
            }

            AppLogger.Info($"GoblinOverlayReset: reason={PortLogField(reason)}");
        }

        private void PortHideGoblinOverlay()
        {
            if (portGoblinOverlayForm != null && !portGoblinOverlayForm.IsDisposed)
            {
                portGoblinOverlayForm.Hide();
            }
        }

        private void PortDisposeGoblinOverlay()
        {
            PortHideGoblinOverlay();
            portGoblinOverlayForm?.Dispose();
            portGoblinOverlayForm = null;
            portGoblinOverlayLabel = null;
        }
    }
}
