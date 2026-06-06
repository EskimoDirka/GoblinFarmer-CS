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
            PortLogCaldeumDetection(confirmedLocation, confirmedLocation, 0, location, confirmedLocation);
            string next = PortNextTeleportForConfirmedLocation(location, confirmedLocation);
            portLastRouteDecisionOutput = PortDisplayLocation(next);
            AppLogger.Info($"Route decision output: selectedNext={PortDisplayLocation(next)}");

            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => PortRecordTeleport(location, confirmedLocation)));
                return;
            }

            portLastTeleportKey = PortLocationKey(buttonLocation);
            AppLogger.Info($"ConfirmedLocation={PortDisplayLocation(confirmedLocation)}; NormalizedAppLocation={PortDisplayLocation(PortNormalizeBlockingLocation(confirmedLocation))}; DisplayLocation={PortDisplayLocation(PortDetectedLocationDisplayName(confirmedLocation))}; ButtonLocation={PortDisplayLocation(buttonLocation)}");
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

        private bool PortTryAdvanceQueuedTeleportFromFreshHotkeyScan(string freshRawLocation, string queuedTarget, out string nextTarget)
        {
            nextTarget = "";
            bool matchesQueuedDestination = PortLocationMatchesForArrival(freshRawLocation, queuedTarget);
            AppLogger.Info(
                $"AlreadyAtQueuedDestinationCheck: source=Hotkey; freshRawLocation={PortDisplayLocation(freshRawLocation)}; " +
                $"freshDisplayLocation={PortDisplayLocation(PortDetectedLocationDisplayName(freshRawLocation))}; " +
                $"queuedTarget={PortDisplayLocation(queuedTarget)}; matchesQueuedDestination={matchesQueuedDestination}");

            if (string.IsNullOrWhiteSpace(freshRawLocation) ||
                string.IsNullOrWhiteSpace(queuedTarget) ||
                !matchesQueuedDestination)
            {
                return false;
            }

            string expectedNextFromConfirmed = PortNextTeleportForConfirmedLocation("", portLastConfirmedLocation);
            if (PortLocationMatchesForArrival(expectedNextFromConfirmed, queuedTarget))
            {
                string nextFromFreshQueuedDestination = PortNextTeleportForConfirmedLocation(queuedTarget, freshRawLocation);
                if (string.IsNullOrWhiteSpace(nextFromFreshQueuedDestination))
                {
                    AppLogger.Info(
                        $"AlreadyAtQueuedDestinationAdvanceSkipped: source=Hotkey; " +
                        $"freshRawLocation={PortDisplayLocation(freshRawLocation)}; " +
                        $"queuedTarget={PortDisplayLocation(queuedTarget)}; " +
                        $"lastConfirmed={PortDisplayLocation(portLastConfirmedLocation)}; " +
                        $"expectedNextFromConfirmed={PortDisplayLocation(expectedNextFromConfirmed)}; " +
                        $"reason=queued target is the normal next route step and has no next route target; preserving route order");
                    return false;
                }

                AppLogger.Info(
                    $"AlreadyAtQueuedDestinationAdvanceAllowed: source=Hotkey; " +
                    $"freshRawLocation={PortDisplayLocation(freshRawLocation)}; " +
                    $"queuedTarget={PortDisplayLocation(queuedTarget)}; " +
                    $"lastConfirmed={PortDisplayLocation(portLastConfirmedLocation)}; " +
                    $"expectedNextFromConfirmed={PortDisplayLocation(expectedNextFromConfirmed)}; " +
                    $"nextFromFreshQueuedDestination={PortDisplayLocation(nextFromFreshQueuedDestination)}; " +
                    $"reason=fresh scan confirms the queued route destination and that destination can advance");
            }

            string buttonLocation = PortGetButtonLocationForDetectedLocation(freshRawLocation);
            if (string.IsNullOrWhiteSpace(buttonLocation))
            {
                buttonLocation = queuedTarget;
            }

            nextTarget = PortNextTeleportForConfirmedLocation(queuedTarget, freshRawLocation);
            portLastConfirmedLocation = freshRawLocation;
            portLastTeleportKey = PortLocationKey(buttonLocation);
            portQueuedTeleportKey = PortLocationKey(nextTarget);
            portLastRouteDecisionOutput = PortDisplayLocation(nextTarget);

            AppLogger.Info(
                $"AlreadyAtQueuedDestinationDetected: teleportSkipped=True; source=Hotkey; " +
                $"freshRawLocation={PortDisplayLocation(freshRawLocation)}; " +
                $"freshNormalizedLocation={PortDisplayLocation(PortNormalizeBlockingLocation(freshRawLocation))}; " +
                $"freshDisplayLocation={PortDisplayLocation(PortDetectedLocationDisplayName(freshRawLocation))}; " +
                $"skippedDestination={PortDisplayLocation(queuedTarget)}; " +
                $"newRequestedTarget={PortDisplayLocation(nextTarget)}; " +
                $"reason=fresh current-location scan already matches queued route destination");
            PortClearPreservedTeleportRetry("hotkey route advancement from fresh current-location scan");
            PortApplyTeleportButtonColors();
            return true;
        }

        private bool PortTryCorrectRouteEndFromFreshHotkeyScan(
            string freshRawLocation,
            string queuedTarget,
            PortLocationDetectionResult freshScanResult,
            out string correctedTarget)
        {
            correctedTarget = "";
            string recomputedTarget = PortNextTeleportForConfirmedLocation("", freshRawLocation);
            if (string.IsNullOrWhiteSpace(freshRawLocation) ||
                string.IsNullOrWhiteSpace(recomputedTarget) ||
                !PortIsKnownRouteLocation(freshRawLocation))
            {
                return false;
            }

            string previousConfirmedLocation = portLastConfirmedLocation;
            string buttonLocation = PortGetButtonLocationForDetectedLocation(freshRawLocation);
            if (string.IsNullOrWhiteSpace(buttonLocation))
            {
                buttonLocation = PortGetRouteLocationForDetectedLocation(freshRawLocation);
            }

            portLastConfirmedLocation = freshRawLocation;
            portLastTeleportKey = PortLocationKey(buttonLocation);
            portQueuedTeleportKey = PortLocationKey(recomputedTarget);
            portLastRouteDecisionOutput = PortDisplayLocation(recomputedTarget);
            correctedTarget = recomputedTarget;

            AppLogger.Info(
                $"TeleportNextRouteEndGuardSuppressed: previousConfirmedLocation={PortDisplayLocation(previousConfirmedLocation)}; " +
                $"queuedTargetBeforeHotkey={PortDisplayLocation(queuedTarget)}; " +
                $"freshDetectedLocation={PortDisplayLocation(freshRawLocation)}; " +
                $"freshDisplayLocation={PortDisplayLocation(PortDetectedLocationDisplayName(freshRawLocation))}; " +
                $"freshScanBest={PortDisplayLocation(freshScanResult.BestName)}; " +
                $"freshScanConfidence={freshScanResult.BestConfidence:0.000}; " +
                $"freshScanResult=ValidNonFinalRouteLocation; " +
                $"correctedCurrentLocation={PortDisplayLocation(buttonLocation)}; " +
                $"recomputedQueuedTarget={PortDisplayLocation(recomputedTarget)}; " +
                $"routeEndAccepted=False");
            PortClearPreservedTeleportRetry("route-end guard corrected route state from fresh current-location scan");
            PortApplyTeleportButtonColors();
            return true;
        }

        private bool PortFreshHotkeyScanConfirmsRouteEnd(string freshRawLocation)
        {
            return !string.IsNullOrWhiteSpace(freshRawLocation) &&
                PortIsFinalRouteLocation(freshRawLocation);
        }

        private void PortPreserveTeleportRetry(string intendedLocation, string preservedCurrentKey, string preservedQueuedKey, string reason)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => PortPreserveTeleportRetry(intendedLocation, preservedCurrentKey, preservedQueuedKey, reason)));
                return;
            }

            portLastTeleportKey = preservedCurrentKey;
            portQueuedTeleportKey = preservedQueuedKey;
            portQueuedRetryTeleportKey = PortLocationKey(intendedLocation);
            portLastRequestedTeleportKey = portQueuedRetryTeleportKey;
            portTeleportRetryFailedOrInterrupted = true;

            AppLogger.Info($"Retry target preserved: reason={reason}; retryTarget={PortDisplayLocation(PortTeleportLocationForKey(portQueuedRetryTeleportKey))}; confirmed={PortDisplayLocation(portLastConfirmedLocation)}; display={PortDisplayLocation(PortGetButtonLocationForDetectedLocation(portLastConfirmedLocation))}; blocking={PortDisplayLocation(PortGetConfirmedCurrentLocation())}; current={PortDisplayLocation(PortTeleportLocationForKey(portLastTeleportKey))}; queued={PortDisplayLocation(PortTeleportLocationForKey(portQueuedTeleportKey))}");
            PortApplyTeleportButtonColors();
        }

        private void PortClearPreservedTeleportRetry(string reason)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => PortClearPreservedTeleportRetry(reason)));
                return;
            }

            if (!portTeleportRetryFailedOrInterrupted &&
                string.IsNullOrWhiteSpace(portQueuedRetryTeleportKey))
            {
                return;
            }

            AppLogger.Info($"Retry target cleared: reason={reason}; retryTarget={PortDisplayLocation(PortTeleportLocationForKey(portQueuedRetryTeleportKey))}; lastRequested={PortDisplayLocation(PortTeleportLocationForKey(portLastRequestedTeleportKey))}; failedOrInterrupted={portTeleportRetryFailedOrInterrupted}");
            portQueuedRetryTeleportKey = "";
            portTeleportRetryFailedOrInterrupted = false;
        }

    }
}
