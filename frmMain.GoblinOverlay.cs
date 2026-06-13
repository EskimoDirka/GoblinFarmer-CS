using System.Drawing;
using System.Windows.Forms;

namespace GoblinFarmer
{
    public partial class frmMain
    {
        private const int PortGoblinOverlayTopInset = 8;
        private const int PortGoblinOverlayWidth = 1060;
        private const int PortGoblinOverlayHeight = 46;
        private static readonly TimeSpan PortGoblinOverlayDetectedAreaFreshness = TimeSpan.FromSeconds(3);
        private readonly object portGoblinOverlayLock = new();
        private Form? portGoblinOverlayForm;
        private PortGoblinOverlayTextControl? portGoblinOverlayTextControl;
        private PortGoblinOverlayAcceptedCountState? portGoblinOverlayAcceptedCount;
        private string portGoblinOverlayCurrentDetectedAreaKey = "";
        private DateTime portGoblinOverlayCurrentDetectedAreaUtc = DateTime.MinValue;

        private sealed record PortGoblinOverlayAcceptedCountState(
            int Count,
            string GoblinType,
            string AcceptedAreaKey,
            string AcceptedRouteAreaKey,
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

        private sealed class PortGoblinOverlayTextControl : Control
        {
            private int total;
            private string goblinType = "--";
            private string area = "--";
            private bool goNext;

            public PortGoblinOverlayTextControl()
            {
                SetStyle(
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.ResizeRedraw |
                    ControlStyles.UserPaint,
                    true);
                BackColor = Color.FromArgb(24, 24, 24);
                ForeColor = Color.White;
            }

            public void SetOverlayState(int total, string goblinType, string area, bool goNext)
            {
                this.total = Math.Max(0, total);
                this.goblinType = string.IsNullOrWhiteSpace(goblinType) ? "--" : goblinType.Trim();
                this.area = string.IsNullOrWhiteSpace(area) ? "--" : area.Trim();
                this.goNext = goNext;
                Text = PortFormatGoblinOverlayText(this.total, this.goblinType, this.area, goNext);
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                const float sectionGap = 28f;
                (string Text, Color Color, float GapAfter)[] segments =
                [
                    ("Count:", Color.Red, 0f),
                    ($" {total}", Color.Red, sectionGap),
                    ("Goblin:", Color.Gold, 0f),
                    ($" {goblinType}", Color.Gold, sectionGap),
                    ("Area:", Color.LightGreen, 0f),
                    ($" {area}", Color.LightGreen, sectionGap),
                    ("Go Next:", Color.White, 0f),
                    ($" {(goNext ? "Y" : "N")}", goNext ? Color.LimeGreen : Color.Red, 0f),
                ];

                using StringFormat format = StringFormat.GenericTypographic;
                float totalWidth = 0;
                foreach ((string text, _, float gapAfter) in segments)
                {
                    totalWidth += e.Graphics.MeasureString(text, Font, int.MaxValue, format).Width + gapAfter;
                }

                float x = Math.Max(0, (ClientSize.Width - totalWidth) / 2f);
                float y = Math.Max(0, (ClientSize.Height - Font.Height) / 2f) - 1f;
                foreach ((string text, Color color, float gapAfter) in segments)
                {
                    using SolidBrush brush = new(color);
                    e.Graphics.DrawString(text, Font, brush, x, y, format);
                    x += e.Graphics.MeasureString(text, Font, int.MaxValue, format).Width + gapAfter;
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

            portGoblinOverlayTextControl = new PortGoblinOverlayTextControl
            {
                Name = "ctlGoblinOverlayText",
                Dock = DockStyle.Fill,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(24, 24, 24),
                Font = new Font(Font.FontFamily, 16.0f, FontStyle.Bold),
            };
            portGoblinOverlayTextControl.SetOverlayState(0, "--", "--", false);

            portGoblinOverlayForm.Controls.Add(portGoblinOverlayTextControl);
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
            if (portGoblinOverlayForm == null || portGoblinOverlayTextControl == null)
            {
                return;
            }

            PortGoblinOverlayDisplayState displayState = PortBuildGoblinOverlayDisplayState();
            portGoblinOverlayTextControl.SetOverlayState(displayState.Count, displayState.GoblinType, displayState.Area, displayState.GoNext);

            int diabloWidth = rect.Right - rect.Left;
            portGoblinOverlayForm.Left = rect.Left + (diabloWidth - portGoblinOverlayForm.Width) / 2;
            portGoblinOverlayForm.Top = rect.Top + PortGoblinOverlayTopInset;
            if (!portGoblinOverlayForm.Visible)
            {
                portGoblinOverlayForm.Show();
            }
        }

        private PortGoblinOverlayDisplayState PortBuildGoblinOverlayDisplayState()
        {
            PortGoblinOverlayAcceptedCountState? state;
            lock (portGoblinOverlayLock)
            {
                state = portGoblinOverlayAcceptedCount;
            }

            if (state == null)
            {
                return new(0, "--", "--", false);
            }

            string currentAreaForGoNext = PortGoblinOverlayCurrentAreaForGoNext(DateTime.UtcNow);
            bool goNext = PortGoblinOverlayShouldGoNext(state.AcceptedAreaKey, state.AcceptedRouteAreaKey, currentAreaForGoNext);
            return new(state.Count, state.GoblinType, state.DisplayArea, goNext);
        }

        private sealed record PortGoblinOverlayDisplayState(int Count, string GoblinType, string Area, bool GoNext);

        private static string PortFormatGoblinOverlayText(int total, string goblinType, string area, bool goNext)
        {
            string displayGoblinType = string.IsNullOrWhiteSpace(goblinType) ? "--" : goblinType.Trim();
            string displayArea = string.IsNullOrWhiteSpace(area) ? "--" : area.Trim();
            return $"Count: {Math.Max(0, total)}  Goblin: {displayGoblinType}  Area: {displayArea}  Go Next: {(goNext ? "Y" : "N")}";
        }

        private bool PortGoblinOverlayShouldGoNext(string lastAcceptedAreaKey, string acceptedRouteAreaKey, string currentConfirmedLocation)
        {
            if (string.IsNullOrWhiteSpace(lastAcceptedAreaKey) ||
                string.IsNullOrWhiteSpace(currentConfirmedLocation))
            {
                return false;
            }

            string acceptedKey = PortLocationKey(lastAcceptedAreaKey);
            string acceptedRouteKey = PortLocationKey(acceptedRouteAreaKey);
            string currentKey = PortLocationKey(currentConfirmedLocation);
            return !string.IsNullOrWhiteSpace(acceptedKey) &&
                acceptedKey.Equals(currentKey, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(acceptedRouteKey) &&
                acceptedRouteKey.Equals(currentKey, StringComparison.OrdinalIgnoreCase));
        }

        private string PortGoblinOverlayCurrentAreaForGoNext(DateTime nowUtc)
        {
            lock (portGoblinOverlayLock)
            {
                if (!string.IsNullOrWhiteSpace(portGoblinOverlayCurrentDetectedAreaKey) &&
                    portGoblinOverlayCurrentDetectedAreaUtc != DateTime.MinValue &&
                    nowUtc - portGoblinOverlayCurrentDetectedAreaUtc <= PortGoblinOverlayDetectedAreaFreshness)
                {
                    return portGoblinOverlayCurrentDetectedAreaKey;
                }
            }

            return portLastConfirmedLocation;
        }

        private void PortRememberGoblinOverlayDetectedArea(string areaKey, DateTime detectedUtc, string source, string reason)
        {
            if (!AppSettings.IsVsDebugProfile || string.IsNullOrWhiteSpace(areaKey))
            {
                return;
            }

            string normalizedAreaKey = PortLocationKey(areaKey);
            if (string.IsNullOrWhiteSpace(normalizedAreaKey))
            {
                return;
            }

            bool changed;
            lock (portGoblinOverlayLock)
            {
                changed = !normalizedAreaKey.Equals(portGoblinOverlayCurrentDetectedAreaKey, StringComparison.OrdinalIgnoreCase);
                portGoblinOverlayCurrentDetectedAreaKey = normalizedAreaKey;
                portGoblinOverlayCurrentDetectedAreaUtc = detectedUtc;
            }

            if (changed)
            {
                AppLogger.Info(
                    "GoblinOverlayDetectedAreaUpdated: " +
                    $"areaKey={PortLogField(PortDisplayLocation(areaKey))}; " +
                    $"source={PortLogField(source)}; " +
                    $"reason={PortLogField(reason)}; " +
                    $"detectedUtc={detectedUtc:O}");
            }
        }

        private void PortSetGoblinOverlayAcceptedCount(
            int total,
            string goblinType,
            string acceptedAreaKey,
            string acceptedRouteAreaKey,
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
                    PortLocationKey(acceptedRouteAreaKey),
                    string.IsNullOrWhiteSpace(displayArea) ? "--" : displayArea.Trim(),
                    string.IsNullOrWhiteSpace(source) ? "Unknown" : source.Trim(),
                    acceptedUtc);
            }

            AppLogger.Info(
                "GoblinOverlayUpdated: " +
                $"total={total}; " +
                $"goblinType={PortLogField(goblinType)}; " +
                $"acceptedAreaKey={PortLogField(PortDisplayLocation(acceptedAreaKey))}; " +
                $"acceptedRouteAreaKey={PortLogField(PortDisplayLocation(acceptedRouteAreaKey))}; " +
                $"displayArea={PortLogField(displayArea)}; " +
                $"source={PortLogField(source)}; " +
                $"acceptedUtc={acceptedUtc:O}");
        }

        private void PortResetGoblinOverlayState(string reason)
        {
            lock (portGoblinOverlayLock)
            {
                portGoblinOverlayAcceptedCount = null;
                portGoblinOverlayCurrentDetectedAreaKey = "";
                portGoblinOverlayCurrentDetectedAreaUtc = DateTime.MinValue;
            }

            if (portGoblinOverlayTextControl != null && !portGoblinOverlayTextControl.IsDisposed)
            {
                portGoblinOverlayTextControl.SetOverlayState(0, "--", "--", false);
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
            portGoblinOverlayTextControl = null;
        }
    }
}
