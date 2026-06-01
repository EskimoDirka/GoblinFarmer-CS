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

            AppLogger.Info($"Route decision input: requested={location}; rawConfirmed={PortDisplayLocation(confirmedLocation)}; normalizedConfirmed={PortDisplayLocation(PortNormalizeBlockingLocation(confirmedLocation))}; buttonLocation={buttonLocation}");
            string next = PortNextTeleportForConfirmedLocation(location, confirmedLocation);
            AppLogger.Info($"Route decision output: selectedNext={PortDisplayLocation(next)}");

            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => PortRecordTeleport(location, confirmedLocation)));
                return;
            }

            portLastTeleportKey = PortLocationKey(buttonLocation);
            AppLogger.Info($"ConfirmedLocation={PortDisplayLocation(confirmedLocation)}; NormalizedAppLocation={PortDisplayLocation(PortNormalizeBlockingLocation(confirmedLocation))}; DisplayLocation={PortDisplayLocation(buttonLocation)}");
            if (PortLocationKey(buttonLocation) == PortLocationKey("Ancient Waterway"))
            {
                AppLogger.Info($"Successful Ancient Waterway button state update: confirmed={PortDisplayLocation(confirmedLocation)}; current={buttonLocation}");
            }
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
            AppLogger.Info($"Selected next teleport target: {PortDisplayLocation(PortTeleportLocationForKey(portQueuedTeleportKey))}; requested={PortDisplayLocation(location)}; queuedKey={portQueuedTeleportKey}");
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
