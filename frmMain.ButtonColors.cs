using System;
using System.Diagnostics;
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
            button.Click += (_, _) => PortQueueTeleportButtonClick(location);
        }

        private void PortQueueTeleportButtonClick(string location)
        {
            AppLogger.Info($"ButtonClickReceived: requested={PortDisplayLocation(location)}; automationRunning={isAutomationRunning}; combatRunning={portCombatRunning}; waitingForConfirmation={portTeleportWaitingForConfirmation}; waitingTarget={PortDisplayLocation(PortTeleportLocationForKey(portTeleportWaitingConfirmationKey))}; retryTarget={PortDisplayLocation(PortTeleportLocationForKey(portQueuedRetryTeleportKey))}; failedOrInterrupted={portTeleportRetryFailedOrInterrupted}; confirmed={PortDisplayLocation(portLastConfirmedLocation)}; current={PortDisplayLocation(PortTeleportLocationForKey(portLastTeleportKey))}; queued={PortDisplayLocation(PortTeleportLocationForKey(portQueuedTeleportKey))}");
            if (isAutomationRunning)
            {
                string cancelReason = $"ManualTeleportButton:{PortDisplayLocation(location)}";
                AppLogger.Info($"ButtonClickWorkflowCancellationRequested: requested={PortDisplayLocation(location)}; reason={PortLogField(cancelReason)}; waitingForConfirmation={portTeleportWaitingForConfirmation}; waitingTarget={PortDisplayLocation(PortTeleportLocationForKey(portTeleportWaitingConfirmationKey))}; currentWorkflow={PortLogField(PortDisplayLocation(portLastWorkflowStep))}");
                AddWorkflowStep($"Teleport button cancelling active flow: {location}");
                DebugManager.Session.RecordWorkflowCancellation($"Teleport button cancelled active workflow: {PortDisplayLocation(location)}");
                portAutomationCts?.Cancel();
                ForceReleaseAllRuntimeInputs($"teleport button cancelled active workflow: {location}");
            }

            if (portCombatRunning)
            {
                AppLogger.Info($"ButtonClickCombatStopRequested: requested={PortDisplayLocation(location)}; reason=ManualTeleportButton; combatRunning={portCombatRunning}; combatStopping={portCombatStopping}");
                PortStopCombat($"teleport button: {location}");
            }

            AppLogger.Info($"ButtonClickQueued: requested={PortDisplayLocation(location)}; source=Button; automationRunning={isAutomationRunning}; combatRunning={portCombatRunning}; waitingForConfirmation={portTeleportWaitingForConfirmation}; retryTarget={PortDisplayLocation(PortTeleportLocationForKey(portQueuedRetryTeleportKey))}");
            AppLogger.Info($"ButtonClickQueuedFeedbackSuppressed: requested={PortDisplayLocation(location)}; source=Button; reason=ReduceTeleportNotificationNoise");
            _ = PortRunTeleportButtonClickAfterCancellationAsync(location);
        }

        private async Task PortRunTeleportButtonClickAfterCancellationAsync(string location)
        {
            Stopwatch wait = Stopwatch.StartNew();
            while (!IsDisposed &&
                wait.ElapsedMilliseconds < 3000 &&
                (isAutomationRunning || portCombatRunning || portCombatStopping))
            {
                await Task.Delay(25);
            }

            if (IsDisposed)
            {
                return;
            }

            if (isAutomationRunning && portAutomationCts?.IsCancellationRequested == true)
            {
                AppLogger.Info($"ButtonClickClearingStaleWorkflowState: requested={PortDisplayLocation(location)}; waitMs={wait.ElapsedMilliseconds}; currentWorkflow={PortLogField(PortDisplayLocation(portLastWorkflowStep))}; reason=CancelledAutomationStillMarkedRunning");
                isAutomationRunning = false;
                portAutomationBlockedByTeleportFailsafe = false;
                ForceReleaseAllRuntimeInputs($"teleport button cleared stale workflow state: {location}");
            }

            if (isAutomationRunning || portCombatRunning || portCombatStopping)
            {
                AppLogger.Info($"ButtonClickStartSkippedAfterCancellation: requested={PortDisplayLocation(location)}; waitMs={wait.ElapsedMilliseconds}; automationRunning={isAutomationRunning}; combatRunning={portCombatRunning}; combatStopping={portCombatStopping}; currentWorkflow={PortLogField(PortDisplayLocation(portLastWorkflowStep))}");
                return;
            }

            AppLogger.Info($"ButtonClickCancellationWaitComplete: requested={PortDisplayLocation(location)}; waitMs={wait.ElapsedMilliseconds}; automationRunning={isAutomationRunning}; combatRunning={portCombatRunning}; combatStopping={portCombatStopping}");
            try
            {
                BeginInvoke(new Action(() => _ = PortRunAutomationAsync(token => PortRunTeleportButtonClick(location, token))));
            }
            catch (InvalidOperationException ex)
            {
                AppLogger.Error($"ButtonClickStartAfterCancellation failed because the form was no longer available: requested={location}", ex);
            }
        }

        private bool PortRunTeleportButtonClick(string location, CancellationToken token)
        {
            string requestedKey = PortLocationKey(location);
            AppLogger.Info($"ButtonClickExecuting: requested={PortDisplayLocation(location)}; automationRunning={isAutomationRunning}; combatRunning={portCombatRunning}; waitingForConfirmation={portTeleportWaitingForConfirmation}; retryTarget={PortDisplayLocation(PortTeleportLocationForKey(portQueuedRetryTeleportKey))}; failedOrInterrupted={portTeleportRetryFailedOrInterrupted}; confirmed={PortDisplayLocation(portLastConfirmedLocation)}; current={PortDisplayLocation(PortTeleportLocationForKey(portLastTeleportKey))}; queued={PortDisplayLocation(PortTeleportLocationForKey(portQueuedTeleportKey))}");
            if (portTeleportRetryFailedOrInterrupted &&
                !string.IsNullOrWhiteSpace(portQueuedRetryTeleportKey) &&
                requestedKey == portQueuedRetryTeleportKey)
            {
                AppLogger.Info($"Button retry detected: requested={PortDisplayLocation(location)}; retryTarget={PortDisplayLocation(PortTeleportLocationForKey(portQueuedRetryTeleportKey))}; confirmed={PortDisplayLocation(portLastConfirmedLocation)}; current={PortDisplayLocation(PortTeleportLocationForKey(portLastTeleportKey))}; queued={PortDisplayLocation(PortTeleportLocationForKey(portQueuedTeleportKey))}");
                AppLogger.Info($"Button retry using preserved state: confirmed={PortDisplayLocation(portLastConfirmedLocation)}; display={PortDisplayLocation(PortDetectedLocationDisplayName(portLastConfirmedLocation))}; buttonLocation={PortDisplayLocation(PortGetButtonLocationForDetectedLocation(portLastConfirmedLocation))}; blocking={PortDisplayLocation(PortGetConfirmedCurrentLocation())}; current={PortDisplayLocation(PortTeleportLocationForKey(portLastTeleportKey))}; queued={PortDisplayLocation(PortTeleportLocationForKey(portQueuedTeleportKey))}");
                return PortRunTeleportButton(location, token, ignoreBlocking: true, source: "ButtonRetry");
            }

            return PortRunTeleportButton(location, token, ignoreBlocking: true, source: "Button");
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
            PortClearPreservedTeleportRetry($"button state cleared: {reason}");
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

            AppLogger.Info($"Teleport button state update: current={PortDisplayLocation(current)}, next={PortDisplayLocation(next)}, currentKey={portLastTeleportKey}, nextKey={portQueuedTeleportKey}");
            AppLogger.Info($"ButtonCurrent={PortDisplayLocation(current)}; ButtonNext={PortDisplayLocation(next)}; ConfirmedLocationRaw={PortDisplayLocation(portLastConfirmedLocation)}; ConfirmedLocationNormalized={PortDisplayLocation(PortNormalizeBlockingLocation(portLastConfirmedLocation))}; DisplayLocation={PortDisplayLocation(PortDetectedLocationDisplayName(portLastConfirmedLocation))}; ButtonLocation={PortDisplayLocation(PortGetButtonLocationForDetectedLocation(portLastConfirmedLocation))}; BlockingLocation={PortDisplayLocation(PortGetConfirmedCurrentLocation())}");
        }
    }
}
