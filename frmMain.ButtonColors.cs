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
            button.Click += (_, _) => PortQueueTeleportButtonClick(location);
        }

        private void PortQueueTeleportButtonClick(string location)
        {
            string requestedKey = PortLocationKey(location);
            AppLogger.Info($"ButtonClickReceived: requested={PortDisplayLocation(location)}; automationRunning={isAutomationRunning}; combatRunning={portCombatRunning}; waitingForConfirmation={portTeleportWaitingForConfirmation}; waitingTarget={PortDisplayLocation(PortTeleportLocationForKey(portTeleportWaitingConfirmationKey))}; retryTarget={PortDisplayLocation(PortTeleportLocationForKey(portQueuedRetryTeleportKey))}; failedOrInterrupted={portTeleportRetryFailedOrInterrupted}; confirmed={PortDisplayLocation(portLastConfirmedLocation)}; current={PortDisplayLocation(PortTeleportLocationForKey(portLastTeleportKey))}; queued={PortDisplayLocation(PortTeleportLocationForKey(portQueuedTeleportKey))}");
            if (isAutomationRunning &&
                portTeleportWaitingForConfirmation &&
                requestedKey == portTeleportWaitingConfirmationKey)
            {
                AppLogger.Info($"Button click ignored because teleport is already waiting for confirmation: requested={PortDisplayLocation(location)}; waitingTarget={PortDisplayLocation(PortTeleportLocationForKey(portTeleportWaitingConfirmationKey))}; retryTarget={PortDisplayLocation(PortTeleportLocationForKey(portQueuedRetryTeleportKey))}");
                AddWorkflowStep($"Button click ignored while waiting for {location} confirmation");
                return;
            }

            AppLogger.Info($"ButtonClickQueued: requested={PortDisplayLocation(location)}; source=Button; automationRunning={isAutomationRunning}; combatRunning={portCombatRunning}; waitingForConfirmation={portTeleportWaitingForConfirmation}; retryTarget={PortDisplayLocation(PortTeleportLocationForKey(portQueuedRetryTeleportKey))}");
            PortShowSplash($"Teleport queued\r\n{PortDisplayLocation(location)}", 1500);
            AppLogger.Info($"ButtonClickFeedbackShown: requested={PortDisplayLocation(location)}; source=Button; message=Teleport queued");
            _ = PortRunAutomationAsync(token => PortRunTeleportButtonClick(location, token));
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
