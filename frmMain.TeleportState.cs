using System;
using System.Collections.Generic;

namespace GoblinFarmer
{
    public partial class frmMain
    {
        private void PortRecordTeleport(string location, string confirmedLocation)
        {
            string buttonLocation = PortGetButtonLocationForDetectedLocation(confirmedLocation);
            if (string.IsNullOrWhiteSpace(buttonLocation))
            {
                buttonLocation = PortGetButtonLocationForDetectedLocation(location);
            }

            string next = PortNextTeleportForConfirmedLocation(location, confirmedLocation);

            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => PortRecordTeleport(location, confirmedLocation)));
                return;
            }

            portLastTeleportKey = PortLocationKey(buttonLocation);
            AppLogger.Info($"ConfirmedLocation={PortDisplayLocation(confirmedLocation)}; DisplayLocation={PortDisplayLocation(buttonLocation)}");
            PortSetNextTeleportTarget(next);
        }

        private void PortSetNextTeleportTarget(string location)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => PortSetNextTeleportTarget(location)));
                return;
            }

            portQueuedTeleportKey = PortLocationKey(location);
            AppLogger.Info($"Selected next teleport target: {PortDisplayLocation(PortTeleportLocationForKey(portQueuedTeleportKey))}");
            PortApplyTeleportButtonColors();
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

        private void PortSetQueuedTeleport(string location)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => PortSetQueuedTeleport(location)));
                return;
            }

            portQueuedTeleportKey = PortLocationKey(location);
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
