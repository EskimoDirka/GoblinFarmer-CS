using System;

namespace GoblinFarmer
{
    public partial class frmMain
    {
        /// <summary>
        /// Records a confirmed teleport and advances the queued route target.
        /// </summary>
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
            PortIncrementTeleportsCompleted();
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

    }
}
