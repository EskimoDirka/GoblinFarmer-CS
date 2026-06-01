using System;
using System.Drawing;
using System.Windows.Forms;

namespace GoblinFarmer
{
    public partial class frmMain
    {
        private void PortWireTeleportButton(Button button, string location)
        {
            string key = PortLocationKey(location);
            portTeleportButtons[key] = button;
            portButtonDefaultBackColors.TryAdd(button, button.BackColor);
            portButtonDefaultForeColors.TryAdd(button, button.ForeColor);
            button.UseVisualStyleBackColor = false;
            button.TextAlign = ContentAlignment.MiddleCenter;
            button.AutoEllipsis = true;
            button.Click += (_, _) => _ = PortRunAutomationAsync(token => PortRunTeleportButton(location, token, ignoreBlocking: true, source: "Button"));
        }

        private void PortClearTeleportButtonStates(string reason)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => PortClearTeleportButtonStates(reason)));
                return;
            }

            portLastTeleportKey = "";
            portQueuedTeleportKey = "";
            AppLogger.Info($"Teleport button state cleared: {reason}");
            PortApplyTeleportButtonColors();
        }

        private void PortRestoreTeleportButtonStateFromLastConfirmedLocation(string reason)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => PortRestoreTeleportButtonStateFromLastConfirmedLocation(reason)));
                return;
            }

            string buttonLocation = PortGetButtonLocationForDetectedLocation(portLastConfirmedLocation);
            if (string.IsNullOrWhiteSpace(buttonLocation))
            {
                PortClearTeleportButtonStates($"no last confirmed location after {reason}");
                return;
            }

            portLastTeleportKey = PortLocationKey(buttonLocation);
            portQueuedTeleportKey = PortLocationKey(PortNextTeleportForConfirmedLocation(buttonLocation, portLastConfirmedLocation));
            AppLogger.Info($"Restoring teleport button state after failed teleport: reason={reason}; confirmed={PortDisplayLocation(portLastConfirmedLocation)}; current={PortDisplayLocation(buttonLocation)}; next={PortDisplayLocation(PortTeleportLocationForKey(portQueuedTeleportKey))}");
            PortApplyTeleportButtonColors();
        }

        private void PortApplyTeleportButtonColors()
        {
            string current = "";
            string next = "";
            foreach ((string key, Button button) in portTeleportButtons)
            {
                if (key == portLastTeleportKey)
                {
                    current = button.Text;
                    button.BackColor = Color.FromArgb(92, 122, 52);
                    button.ForeColor = Color.White;
                    button.Font = new Font(button.Font, FontStyle.Bold);
                }
                else if (key == portQueuedTeleportKey)
                {
                    next = button.Text;
                    button.BackColor = Color.FromArgb(200, 111, 31);
                    button.ForeColor = Color.White;
                    button.Font = new Font(button.Font, FontStyle.Bold);
                }
                else
                {
                    button.BackColor = portButtonDefaultBackColors.TryGetValue(button, out Color backColor)
                        ? backColor
                        : SystemColors.Control;
                    button.ForeColor = portButtonDefaultForeColors.TryGetValue(button, out Color foreColor)
                        ? foreColor
                        : SystemColors.ControlText;
                    button.Font = new Font(button.Font, FontStyle.Regular);
                }
            }

            AppLogger.Info($"Teleport button state update: current={PortDisplayLocation(current)}, next={PortDisplayLocation(next)}");
            AppLogger.Info($"ButtonCurrent={PortDisplayLocation(current)}; ButtonNext={PortDisplayLocation(next)}; ConfirmedLocation={PortDisplayLocation(portLastConfirmedLocation)}; DisplayLocation={PortDisplayLocation(PortGetButtonLocationForDetectedLocation(portLastConfirmedLocation))}; BlockingLocation={PortDisplayLocation(PortGetConfirmedCurrentLocation())}");
        }
    }
}
