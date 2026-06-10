using OpenCvSharp;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using DrawingPoint = System.Drawing.Point;

namespace GoblinFarmer
{
    public partial class frmMain
    {
        /// <summary>
        /// Starts or stops the active class combat loop from the combat hotkey.
        /// </summary>
        private void PortToggleCombat()
        {
            bool cancelledArrivalConfirmation = PortCancelArrivalConfirmationWait("CombatHotkey");
            if (portCombatRunning)
            {
                AppLogger.Info($"Combat stop requested by combat hotkey: key=backtick; combatActive={portCombatRunning}; combatStopping={portCombatStopping}");
                PortStopCombat("hotkey");
                return;
            }

            if (cancelledArrivalConfirmation && isAutomationRunning)
            {
                PortStartCombatAfterArrivalConfirmationCancel();
                return;
            }

            PortStartCombatFromHotkey("hotkey", cancelledArrivalConfirmation);
        }

        private void PortStartCombatAfterArrivalConfirmationCancel()
        {
            AppLogger.Info($"CombatHotkeyStartDeferredUntilArrivalConfirmationCancelComplete: automationRunning={isAutomationRunning}; waitingConfirmation={portTeleportWaitingForConfirmation}; combatActive={portCombatRunning}; combatStopping={portCombatStopping}");
            _ = Task.Run(async () =>
            {
                Stopwatch wait = Stopwatch.StartNew();
                while (wait.ElapsedMilliseconds < 2500 && (isAutomationRunning || portTeleportWaitingForConfirmation))
                {
                    await Task.Delay(25);
                }

                if (IsDisposed)
                {
                    return;
                }

                try
                {
                    BeginInvoke(new Action(() =>
                    {
                        if (portCombatRunning)
                        {
                            AppLogger.Info($"CombatHotkeyStartAfterArrivalConfirmationCancelSkipped: reason=CombatAlreadyRunning; waitMs={wait.ElapsedMilliseconds}; automationRunning={isAutomationRunning}; waitingConfirmation={portTeleportWaitingForConfirmation}");
                            return;
                        }

                        if (isAutomationRunning || portTeleportWaitingForConfirmation)
                        {
                            AppLogger.Info($"CombatHotkeyStartAfterArrivalConfirmationCancelSkipped: reason=AutomationStillRunning; waitMs={wait.ElapsedMilliseconds}; automationRunning={isAutomationRunning}; waitingConfirmation={portTeleportWaitingForConfirmation}");
                            return;
                        }

                        AppLogger.Info($"CombatHotkeyStartAfterArrivalConfirmationCancel: waitMs={wait.ElapsedMilliseconds}; automationRunning={isAutomationRunning}; waitingConfirmation={portTeleportWaitingForConfirmation}");
                        PortStartCombatFromHotkey("hotkey-after-arrival-confirmation-cancel", true);
                    }));
                }
                catch (InvalidOperationException ex)
                {
                    AppLogger.Error("CombatHotkeyStartAfterArrivalConfirmationCancel failed because the form was no longer available.", ex);
                }
            });
        }

        private void PortStartCombatFromHotkey(string source, bool afterArrivalConfirmationCancel)
        {
            if (isAutomationRunning || !PortDiabloIsActive())
            {
                AppLogger.Info($"Combat start ignored: source={source}; afterArrivalConfirmationCancel={afterArrivalConfirmationCancel}; isAutomationRunning={isAutomationRunning}; {PortCombatInputContext()}");
                return;
            }

            portCombatCts = new CancellationTokenSource();
            portCombatRunning = true;
            portCombatClass = radWD.Checked ? "witch_doctor" : radDH.Checked ? "demon_hunter" : "monk";
            portOriginalCursorHandle = PortCurrentCursorHandle();
            Interlocked.Exchange(ref portLastCombatCursorClickTicks, 0);
            DebugManager.Session.BeginCombatActive();
            PortWriteSessionMetadata(logSuccess: false);
            SetCombatStatus($"{radWD.Checked switch { true => "Witch Doctor", false when radDH.Checked => "Demon Hunter", _ => "Monk" }} Running");
            AddWorkflowStep("Combat started");
            AppLogger.Info($"Combat started: source={source}; afterArrivalConfirmationCancel={afterArrivalConfirmationCancel}; class={portCombatClass}; originalCursorHandle=0x{portOriginalCursorHandle.ToInt64():X}; {PortCombatInputContext()}");
            PortLockCursorToDiablo();
            CancellationToken combatToken = portCombatCts.Token;
            PortStartCombatMenuWatcher(combatToken);
            PortStartGoblinEvidenceScanner(combatToken);

            if (portCombatClass == "witch_doctor")
            {
                PortRunCombatTask("Witch Doctor loop", () => PortWitchDoctorLoop(combatToken));
                PortRunCombatTask("Witch Doctor mouse wheel loop", () => PortWitchDoctorMouseWheelLoop(combatToken));
                PortRunCombatTask("Witch Doctor cursor left click loop", () => PortWitchDoctorCursorLeftClickLoop(combatToken));
            }
            else if (portCombatClass == "demon_hunter")
            {
                PortRunCombatTask("Demon Hunter startup loop", () => PortDemonHunterStartupLoop(combatToken));
            }
            else
            {
                PortRunCombatTask("Monk loop", () => PortMonkLoop(combatToken));
                PortRunCombatTask("Combat cursor loop", () => PortCombatCursorLoop(combatToken));
            }
        }

        private bool PortStopCombat(string reason)
        {
            if (!portCombatRunning)
            {
                return false;
            }

            string stoppedClass = portCombatClass;
            CancellationTokenSource? cts = portCombatCts;
            portCombatStopping = true;
            AppLogger.Info($"Combat stopping: reason={reason}; class={stoppedClass}; {PortCombatInputContext()}");

            try
            {
                portCombatRunning = false;
                cts?.Cancel();
                portCombatCts = null;
                portCombatClass = "";

                if (stoppedClass == "monk")
                {
                    PortMonkSkill3Up($"combat stop: {reason}");
                }

                Thread.Sleep(75);
                ForceReleaseAllRuntimeInputs($"combat stop: {reason}");
                Thread.Sleep(50);
                ForceReleaseAllRuntimeInputs($"combat stop confirm release: {reason}");

                if (stoppedClass == "witch_doctor")
                {
                    PortCleanupWitchDoctorHexAfterStop(reason);
                }

                ForceReleaseAllRuntimeInputs($"combat stop complete: {reason}");
                cts?.Dispose();
                ClipCursor(IntPtr.Zero);
                DebugManager.Session.EndCombatActive();
                if (portApplicationClosing)
                {
                    AppLogger.Info($"CombatStopSessionMetadataSkipped: reason={PortLogField(reason)}; appClosing=True; class={PortLogField(stoppedClass)}");
                }
                else
                {
                    PortWriteSessionMetadata(logSuccess: false);
                }
                SetCombatStatus("Idle");
                SetAppStatus($"Combat Stopped ({reason})");
                AddWorkflowStep($"Combat stopped ({reason})");
                AppLogger.Info($"Combat stopped: reason={reason}; class={stoppedClass}; {PortCombatInputContext()}");
                return true;
            }
            finally
            {
                portCombatStopping = false;
            }
        }

        private void PortCleanupWitchDoctorHexAfterStop(string reason)
        {
            AddWorkflowStep("Witch Doctor stop cleanup started");
            AppLogger.Info($"Witch Doctor stop cleanup started: {reason}");

            if (!PortDiabloIsActive())
            {
                AppLogger.Info("Witch Doctor stop cleanup skipped: Diablo inactive");
                return;
            }

            bool ready = PortWitchDoctorHexReady();
            if (ready)
            {
                AppLogger.Info("Witch Doctor stop cleanup complete: Hex already ready");
                AddWorkflowStep("Witch Doctor stop cleanup complete");
                return;
            }

            AppLogger.Info("Witch Doctor Hex not ready during stop cleanup; pressing 1 once to exit chicken mode");
            AddWorkflowStep("Exiting Witch Doctor chicken mode");
            PortPressKey(PortVk1);
            ForceReleaseAllRuntimeInputs("Witch Doctor chicken exit press");
            Thread.Sleep(100);

            if (!PortDiabloIsActive())
            {
                AppLogger.Info("Witch Doctor stop cleanup ended after chicken exit press: Diablo inactive");
                return;
            }

            if (PortWitchDoctorHexReady())
            {
                AppLogger.Info("Witch Doctor stop cleanup complete: Hex ready after best-effort chicken exit press");
                AddWorkflowStep("Witch Doctor stop cleanup complete");
                return;
            }

            AppLogger.Info("Witch Doctor stop cleanup best-effort complete: Hex not confirmed ready; continuing without blocking Teleport Next");
            AddWorkflowStep("Witch Doctor stop cleanup best-effort complete");
        }

        private void PortRunCombatTask(string name, Action work)
        {
            Task.Run(() =>
            {
                try
                {
                    work();
                }
                catch (Exception ex)
                {
                    AppLogger.Error($"{name} failed.", ex);
                    BeginInvoke(new Action(() => PortStopCombat($"{name} failed")));
                }
            });
        }

        // Monk combat loop =================================
        private void PortMonkLoop(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested &&
                       portCombatRunning &&
                       portCombatClass == "monk")
                {
                    if (!PortDiabloIsActive())
                    {
                        BeginInvoke(new Action(() => PortStopCombat("Diablo inactive")));
                        return;
                    }

                    PortMonkSkill3Down();

                    PortPressKey(PortVk1);

                    Thread.Sleep(10);

                    PortPressKey(PortVk2);

                    Thread.Sleep(10);
                }
            }
            finally
            {
                PortMonkSkill3Up("Monk loop exit");
                ForceReleaseAllRuntimeInputs("Monk loop exit");
            }
        }

        private void PortMonkSkill3Down()
        {
            lock (portRuntimeInputLock)
            {
                if (portMonkSkill3Held)
                {
                    return;
                }

                PortMarkAutomationNumberKeyInjection(PortVk3);
                keybd_event(PortVk3, 0, 0, UIntPtr.Zero);
                portMonkSkill3Held = true;
                AppLogger.Info($"Monk Skill 3 hold started: key=3; vk={PortVk3}; keyDownSent=true; held=true; combatRunning={portCombatRunning}; combatClass={portCombatClass}; {PortCombatInputContext()}");
            }
        }

        private bool PortMonkSkill3Up(string reason)
        {
            lock (portRuntimeInputLock)
            {
                if (!portMonkSkill3Held)
                {
                    return false;
                }

                PortMarkAutomationNumberKeyInjection(PortVk3);
                keybd_event(PortVk3, 0, PortKeyUp, UIntPtr.Zero);
                portMonkSkill3Held = false;
                bool safetyRelease = PortIsMonkSkill3SafetyRelease(reason);
                AppLogger.Info($"Monk Skill 3 hold released: reason={reason}; safetyRelease={safetyRelease}; key=3; vk={PortVk3}; keyUpSent=true; held=false; combatRunning={portCombatRunning}; combatClass={portCombatClass}; {PortCombatInputContext()}");
                return true;
            }
        }

        private static bool PortIsMonkSkill3SafetyRelease(string reason)
        {
            return reason.Contains("stop", StringComparison.OrdinalIgnoreCase) ||
                reason.Contains("pause", StringComparison.OrdinalIgnoreCase) ||
                reason.Contains("closing", StringComparison.OrdinalIgnoreCase) ||
                reason.Contains("dispose", StringComparison.OrdinalIgnoreCase) ||
                reason.Contains("exit", StringComparison.OrdinalIgnoreCase) ||
                reason.Contains("lost focus", StringComparison.OrdinalIgnoreCase);
        }

        // Witch Doctor combat loop =================================
        private void PortWitchDoctorLoop(CancellationToken token)
        {
            AddWorkflowStep("Witch Doctor opening rotation");
            AppLogger.Info($"Witch Doctor key loop started: combatClass=witch_doctor; keyOrder=2,3,1; combatInputMode=MouseWheelScroll; heldLeftMode=false; heldRightMode=false; {PortCombatInputContext()}");

            if (!PortCombatPressKey(token, 0x32)) return; // 2
            if (!PortCombatSleep(token, 50)) return;

            if (!PortCombatPressKey(token, 0x33)) return; // 3
            if (!PortCombatSleep(token, 50)) return;

            if (!PortCombatPressKey(token, 0x31)) return; // 1

            while (!token.IsCancellationRequested && portCombatRunning && portCombatClass == "witch_doctor")
            {
                if (!PortDiabloIsActive())
                {
                    BeginInvoke(new Action(() => PortStopCombat("Diablo inactive")));
                    return;
                }

                if (PortWitchDoctorHexReady())
                {
                    AddWorkflowStep("Hex ready; running Witch Doctor rotation");

                    if (!PortCombatPressKey(token, 0x32)) return; // 2
                    if (!PortCombatSleep(token, 50)) return;

                    if (!PortCombatPressKey(token, 0x33)) return; // 3
                    if (!PortCombatSleep(token, 50)) return;

                    if (!PortCombatPressKey(token, 0x31)) return; // 1

                    if (!PortCombatSleep(token, 500)) return;
                }
                else
                {
                    if (!PortCombatSleep(token, 100)) return;
                }
            }
        }

        private void PortWitchDoctorMouseWheelLoop(CancellationToken token)
        {
            AppLogger.Info($"WitchDoctorMouseWheelLoopStarted: combatClass=witch_doctor; combatInputMode=MouseWheelScroll; heldLeftMode=false; heldRightMode=false; keyLoopOrder=2,3,1; {PortCombatInputContext()}");

            while (!token.IsCancellationRequested && portCombatRunning && portCombatClass == "witch_doctor")
            {
                if (!PortDiabloIsActive())
                {
                    BeginInvoke(new Action(() => PortStopCombat("Diablo inactive in Witch Doctor mouse wheel loop")));
                    return;
                }

                bool scrollSafe = PortWitchDoctorMouseWheelIsSafe();
                PortHandleWitchDoctorMouseWheelInput(scrollSafe);
                PortSleep(token, 90);
            }

            AppLogger.Info($"WitchDoctorMouseWheelLoopStopped: combatClass=witch_doctor; combatInputMode=MouseWheelScroll; heldLeftMode=false; heldRightMode=false; leftDown={portRuntimeLeftMouseHeld}; rightDown={portRuntimeRightMouseHeld}; {PortCombatInputContext()}");
        }

        private void PortWitchDoctorCursorLeftClickLoop(CancellationToken token)
        {
            AppLogger.Info($"WitchDoctorCursorChangeLeftClickLoopStarted: combatClass=witch_doctor; combatInputMode=MouseWheelScroll; clickSendMethod=suppressed; heldLeftMode=false; heldRightMode=false; keyLoopOrder=2,3,1; {PortCombatInputContext()}");

            while (!token.IsCancellationRequested && portCombatRunning && portCombatClass == "witch_doctor")
            {
                if (!PortDiabloIsActive())
                {
                    BeginInvoke(new Action(() => PortStopCombat("Diablo inactive in Witch Doctor cursor left click loop")));
                    return;
                }

                bool clickSafe = PortCombatClickIsSafe();
                if (!clickSafe)
                {
                    bool unsafeCursorChanged = PortCombatCursorChanged(out string unsafeSkipReason);
                    PortLogWitchDoctorCursorChangeLeftClickCheck(unsafeCursorChanged);
                    PortLogWitchDoctorCursorChangeLeftClickSkipped(unsafeSkipReason, unsafeCursorChanged);
                    PortSleep(token, 90);
                    continue;
                }

                bool shouldClick = PortCombatCursorShouldSendClick(out string skipReason, out bool cursorChanged);
                PortLogWitchDoctorCursorChangeLeftClickCheck(cursorChanged);
                if (shouldClick)
                {
                    PortRuntimeMouseDown(MOUSEEVENTF_LEFTDOWN);
                    Thread.Sleep(10);
                    PortRuntimeMouseUp(MOUSEEVENTF_LEFTUP);
                    PortLogWitchDoctorCursorChangeLeftClickSent(cursorChanged);
                }
                else
                {
                    PortLogWitchDoctorCursorChangeLeftClickSkipped(skipReason, cursorChanged);
                }

                PortSleep(token, 90);
            }

            AppLogger.Info($"WitchDoctorCursorChangeLeftClickLoopStopped: combatClass=witch_doctor; combatInputMode=MouseWheelScroll; heldLeftMode=false; heldRightMode=false; leftDown={portRuntimeLeftMouseHeld}; rightDown={portRuntimeRightMouseHeld}; {PortCombatInputContext()}");
        }

        private bool PortCombatShouldContinue(CancellationToken token)
        {
            return !token.IsCancellationRequested && portCombatRunning;
        }

        private bool PortCombatPressKey(CancellationToken token, int vk)
        {
            if (!PortCombatShouldContinue(token))
            {
                return false;
            }

            PortPressKey(vk);
            return PortCombatShouldContinue(token);
        }

        private bool PortCombatSleep(CancellationToken token, int milliseconds)
        {
            PortSleep(token, milliseconds);
            return PortCombatShouldContinue(token);
        }

        private bool PortWitchDoctorHexReady()
        {
            string imagePath = Img("Combat", "Hex Ready.png");

            if (!File.Exists(imagePath))
            {
                AddWorkflowStep("Witch Doctor Hex template missing: Hex Ready.png");
                return false;
            }

            Rectangle hexRegion = PortScanRegion("WitchDoctorHex", imagePath);

            double confidence = PortBestTemplateConfidenceInDiabloRegion(imagePath, hexRegion);

            bool ready = confidence >= 0.55;
            long nowTicks = DateTime.UtcNow.Ticks;
            long lastLogTicks = Interlocked.Read(ref portLastWitchDoctorHexLogTicks);
            bool stateChanged = ready != portWitchDoctorLastHexReady;
            bool throttled = nowTicks - lastLogTicks >= TimeSpan.FromSeconds(2).Ticks;
            if (stateChanged || throttled)
            {
                AppLogger.Info($"Witch Doctor Hex Ready confidence={confidence:0.000}; ready={ready}");
                portWitchDoctorLastHexReady = ready;
                Interlocked.Exchange(ref portLastWitchDoctorHexLogTicks, nowTicks);
            }

            return ready;
        }

        // Demon Hunter combat loop =================================
        private void PortDemonHunterStartupLoop(CancellationToken token)
        {
            PortDemonHunterMomentumBuildLoop(token);

            if (token.IsCancellationRequested ||
                !portCombatRunning ||
                portCombatClass != "demon_hunter")
            {
                return;
            }

            AddWorkflowStep("Demon Hunter sustained combat started");

            PortRunCombatTask("Demon Hunter key loop", () => PortDemonHunterKeyLoop(token));
            PortRunCombatTask("Demon Hunter right mouse loop", () => PortDemonHunterRightMouseLoop(token));
            PortRunCombatTask("Demon Hunter shift click loop", () => PortDemonHunterShiftClickLoop(token));
            PortRunCombatTask("Combat cursor loop", () => PortCombatCursorLoop(token));
        }

        private void PortDemonHunterMomentumBuildLoop(CancellationToken token)
        {
            AddWorkflowStep("Demon Hunter momentum build started");

            bool waitForCountTemplate = PortDemonHunterMomentumCountTemplateAvailable();
            Stopwatch? missingTemplateStopwatch = waitForCountTemplate ? null : Stopwatch.StartNew();
            bool holdingClick = false;

            try
            {
                while (!token.IsCancellationRequested &&
                       portCombatRunning &&
                       portCombatClass == "demon_hunter")
                {
                    if (!PortDiabloIsActive())
                    {
                        BeginInvoke(new Action(() => PortStopCombat("Diablo inactive during Demon Hunter momentum build")));
                        return;
                    }

                    if (!PortCombatClickIsSafe())
                    {
                        PortLogCombatClickDecision(
                            "Demon Hunter momentum build",
                            false,
                            "left",
                            ref portLastDemonHunterDecisionLogTicks,
                            ref portLastDemonHunterDecisionAllowed);

                        if (holdingClick)
                        {
                            PortRuntimeMouseUp(MOUSEEVENTF_LEFTUP);
                            PortRuntimeShiftUp();
                            holdingClick = false;
                        }

                        PortSleep(token, 50);
                        continue;
                    }

                    if (!holdingClick)
                    {
                        PortRuntimeShiftDown();
                        PortRuntimeMouseDown(MOUSEEVENTF_LEFTDOWN);
                        holdingClick = true;
                    }

                    if (waitForCountTemplate && PortDemonHunterMomentumReady())
                    {
                        AddWorkflowStep("Demon Hunter momentum build complete");
                        return;
                    }

                    if (!waitForCountTemplate && missingTemplateStopwatch?.ElapsedMilliseconds >= 6000)
                    {
                        AppLogger.Info("Demon Hunter momentum count template missing; continuing after timed startup build");
                        AddWorkflowStep("Demon Hunter momentum build finished after missing-template timeout");
                        return;
                    }

                    PortSleep(token, 50);
                }
            }
            finally
            {
                if (holdingClick)
                {
                    PortRuntimeMouseUp(MOUSEEVENTF_LEFTUP);
                    PortRuntimeShiftUp();
                }
            }
        }

        private bool PortDemonHunterShiftLeftClick(CancellationToken token, bool stopCombatWhenUnsafe = false)
        {
            bool clickSafe = PortDemonHunterWaitForCombatMouseClickSafe(token);
            PortLogCombatClickDecision(
                "Demon Hunter Shift+Left Click",
                clickSafe,
                "left",
                ref portLastDemonHunterDecisionLogTicks,
                ref portLastDemonHunterDecisionAllowed);
            if (!clickSafe)
            {
                AppLogger.Info($"Demon Hunter Shift+Left Click blocked by combat safety; no click sent; {PortCombatInputContext()}");
                if (stopCombatWhenUnsafe && !token.IsCancellationRequested && portCombatRunning && portCombatClass == "demon_hunter")
                {
                    BeginInvoke(new Action(() => PortStopCombat("Demon Hunter Shift+Left Click could not find safe region")));
                }

                return false;
            }

            lock (portRuntimeInputLock)
            {
                bool rightHeldDuringShiftClick = portRuntimeRightMouseHeld;
                AppLogger.Info($"Demon Hunter Shift+Left Click sending serialized input; rightMouseHeld={rightHeldDuringShiftClick}; {PortCombatInputContext()}");
                PortRuntimeShiftDown();
                try
                {
                    Thread.Sleep(30);

                    PortRuntimeMouseDown(MOUSEEVENTF_LEFTDOWN);
                    Thread.Sleep(30);
                    PortRuntimeMouseUp(MOUSEEVENTF_LEFTUP);
                }
                finally
                {
                    PortRuntimeShiftUp();
                }
            }

            return true;
        }

        private bool PortDemonHunterWaitForCombatMouseClickSafe(CancellationToken token, int timeoutMilliseconds = 2000)
        {
            Stopwatch sw = Stopwatch.StartNew();
            while (!token.IsCancellationRequested &&
                   portCombatRunning &&
                   portCombatClass == "demon_hunter" &&
                   sw.ElapsedMilliseconds <= timeoutMilliseconds)
            {
                if (!PortDiabloIsActive())
                {
                    BeginInvoke(new Action(() => PortStopCombat("Diablo inactive while waiting for Demon Hunter safe click")));
                    return false;
                }

                if (PortCombatClickIsSafe())
                {
                    return true;
                }

                PortSleep(token, 90);
            }

            return false;
        }

        private void PortDemonHunterKeyLoop(CancellationToken token)
        {
            int[] sequence = [0x31, 0x32, 0x33, 0x34];
            int index = 0;

            while (!token.IsCancellationRequested && portCombatRunning && portCombatClass == "demon_hunter")
            {
                if (!PortDiabloIsActive())
                {
                    BeginInvoke(new Action(() => PortStopCombat("Diablo inactive in Demon Hunter key loop")));
                    return;
                }

                PortPressKey(sequence[index]);
                index = (index + 1) % sequence.Length;
                PortSleep(token, 100);
            }
        }

        private void PortDemonHunterRightMouseLoop(CancellationToken token)
        {
            bool rightDown = false;

            try
            {
                bool clickSafe = PortDemonHunterWaitForCombatMouseClickSafe(token);
                PortLogCombatClickDecision(
                    "Demon Hunter right hold",
                    clickSafe,
                    "right",
                    ref portLastDemonHunterDecisionLogTicks,
                    ref portLastDemonHunterDecisionAllowed);

                if (!clickSafe)
                {
                    BeginInvoke(new Action(() => PortStopCombat("Demon Hunter right mouse could not start in safe region")));
                    return;
                }

                AppLogger.Info($"Combat right click allowed: sending RIGHTDOWN; {PortCombatInputContext()}");
                PortRuntimeMouseDown(MOUSEEVENTF_RIGHTDOWN);
                portDemonHunterRightHeldFromSafeRegion = true;
                rightDown = true;

                while (!token.IsCancellationRequested && portCombatRunning && portCombatClass == "demon_hunter")
                {
                    if (!PortDiabloIsActive())
                    {
                        BeginInvoke(new Action(() => PortStopCombat("Diablo inactive in Demon Hunter right mouse loop")));
                        return;
                    }

                    PortSleep(token, 90);
                }
            }
            finally
            {
                if (rightDown && portRuntimeRightMouseHeld)
                {
                    AppLogger.Info($"Combat right click cleanup: sending RIGHTUP; {PortCombatInputContext()}");
                    PortRuntimeMouseUp(MOUSEEVENTF_RIGHTUP);
                }
                else if (rightDown)
                {
                    AppLogger.Info($"Combat right click cleanup skipped: localRightDown=true; runtimeRightHeld=false; {PortCombatInputContext()}");
                }

                portDemonHunterRightHeldFromSafeRegion = false;

                if (portRuntimeShiftHeld)
                {
                    PortRuntimeShiftUp();
                }
            }
        }

        private void PortDemonHunterShiftClickLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested && portCombatRunning && portCombatClass == "demon_hunter")
            {
                if (!PortDiabloIsActive())
                {
                    BeginInvoke(new Action(() => PortStopCombat("Diablo inactive in Demon Hunter momentum maintenance loop")));
                    return;
                }

                // Image recognition is the source of truth for Momentum.
                if (PortDemonHunterMomentumReady())
                {
                    PortSleep(token, 50);
                    continue;
                }

                AddWorkflowStep("Demon Hunter momentum not detected; sending Shift+Left Click");

                if (!PortDemonHunterShiftLeftClick(token, stopCombatWhenUnsafe: true))
                {
                    return;
                }

                PortSleep(token, 350);
            }
        }

        private void PortCombatCursorLoop(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested && portCombatRunning)
                {
                    if (!PortDiabloIsActive())
                    {
                        BeginInvoke(new Action(() => PortStopCombat("Diablo inactive")));
                        return;
                    }

                    bool clickSafe = PortCombatClickIsSafe();
                    PortLogCombatClickDecision(
                        "Combat cursor loop",
                        clickSafe,
                        "left",
                        ref portLastCombatCursorDecisionLogTicks,
                        ref portLastCombatCursorDecisionAllowed);

                    if (!clickSafe)
                    {
                        if (portCombatClass == "demon_hunter" && portDemonHunterRightHeldFromSafeRegion && portRuntimeRightMouseHeld)
                        {
                            PortLogDemonHunterRightHeldNoClickSuppressionActive();
                        }

                        if (portRuntimeLeftMouseHeld)
                        {
                            PortRuntimeMouseUp(MOUSEEVENTF_LEFTUP);
                        }

                        if (PortTrySendDemonHunterFallbackLeftClickFromBlockedCursor())
                        {
                            PortSleep(token, 90);
                            continue;
                        }
                    }
                    else
                    {
                        if (PortCombatCursorShouldSendLeftClick(out string skipReason))
                        {
                            PortRuntimeMouseDown(MOUSEEVENTF_LEFTDOWN);
                            Thread.Sleep(10);
                            PortRuntimeMouseUp(MOUSEEVENTF_LEFTUP);
                        }
                        else
                        {
                            PortLogCombatCursorClickSkipped(skipReason);
                        }
                    }

                    PortSleep(token, 90);
                }
            }
            finally
            {
                if (portRuntimeLeftMouseHeld)
                {
                    PortRuntimeMouseUp(MOUSEEVENTF_LEFTUP);
                }
            }
        }

        private bool PortTrySendDemonHunterFallbackLeftClickFromBlockedCursor()
        {
            PortCombatClickDiagnostics diagnostics = PortGetCombatClickDiagnostics();
            if (!DemonHunterCombatPolicy.ShouldUseFallbackClickWhileRightHeld(
                    portCombatRunning,
                    portCombatClass,
                    diagnostics.DiabloActive,
                    portRuntimeRightMouseHeld,
                    portDemonHunterRightHeldFromSafeRegion))
            {
                return false;
            }

            if (!PortCombatCursorShouldSendLeftClick(out string skipReason))
            {
                long nowTicks = DateTime.UtcNow.Ticks;
                long lastLogTicks = Interlocked.Read(ref portLastCombatCursorDecisionLogTicks);
                if (nowTicks - lastLogTicks >= TimeSpan.FromSeconds(1).Ticks)
                {
                    Interlocked.Exchange(ref portLastCombatCursorDecisionLogTicks, nowTicks);
                    AppLogger.Info($"DemonHunterBlockedCursorFallbackSkipped: skipReason={PortLogField(skipReason)}; rightMouseHeld={portRuntimeRightMouseHeld}; rightHeldFromSafeRegion={portDemonHunterRightHeldFromSafeRegion}; {PortCombatInputContext()}");
                }

                return false;
            }

            if (!diagnostics.HasDiabloRect)
            {
                AppLogger.Info($"DemonHunterBlockedCursorFallbackSkipped: skipReason=DiabloRectUnavailable; rightMouseHeld={portRuntimeRightMouseHeld}; rightHeldFromSafeRegion={portDemonHunterRightHeldFromSafeRegion}; {PortCombatInputContext()}");
                return false;
            }

            Rectangle diabloRectangle = new(
                diagnostics.DiabloRect.Left,
                diagnostics.DiabloRect.Top,
                diagnostics.DiabloRect.Right - diagnostics.DiabloRect.Left,
                diagnostics.DiabloRect.Bottom - diagnostics.DiabloRect.Top);
            if (!CombatClickSafety.TryGetDemonHunterFallbackClickPoint(diabloRectangle, out DrawingPoint fallbackPoint))
            {
                AppLogger.Info($"DemonHunterBlockedCursorFallbackSkipped: skipReason=NoSafeFallbackPoint; rightMouseHeld={portRuntimeRightMouseHeld}; rightHeldFromSafeRegion={portDemonHunterRightHeldFromSafeRegion}; {PortCombatInputContext()}");
                return false;
            }

            DrawingPoint originalPoint = diagnostics.Cursor;
            lock (portRuntimeInputLock)
            {
                SetCursorPos(fallbackPoint.X, fallbackPoint.Y);
                Thread.Sleep(20);
                PortRuntimeMouseDown(MOUSEEVENTF_LEFTDOWN);
                Thread.Sleep(10);
                PortRuntimeMouseUp(MOUSEEVENTF_LEFTUP);
            }

            AppLogger.Info($"DemonHunterBlockedCursorFallbackLeftClickSent: combatInputMode=PhysicalCursorHeldFromSafeRegion; clickSendMethod=safe-fallback; originalPoint={originalPoint.X},{originalPoint.Y}; fallbackPoint={fallbackPoint.X},{fallbackPoint.Y}; blockedRegion={PortLogField(diagnostics.NoClickRegionName)}; rightMouseHeld={portRuntimeRightMouseHeld}; rightHeldFromSafeRegion={portDemonHunterRightHeldFromSafeRegion}; {PortCombatInputContext()}");
            return true;
        }

        private bool PortCombatCursorShouldSendLeftClick(out string skipReason)
        {
            return PortCombatCursorShouldSendClick(out skipReason, out _);
        }

        private bool PortCombatCursorShouldSendClick(out string skipReason, out bool cursorChanged)
        {
            skipReason = "";
            cursorChanged = false;
            IntPtr cursorHandle = PortCurrentCursorHandle();
            if (portOriginalCursorHandle == IntPtr.Zero)
            {
                skipReason = "original cursor handle unavailable";
                return false;
            }

            if (cursorHandle == IntPtr.Zero)
            {
                skipReason = "current cursor handle unavailable";
                return false;
            }

            cursorChanged = cursorHandle != portOriginalCursorHandle;
            if (cursorHandle == portOriginalCursorHandle)
            {
                skipReason = "cursor handle unchanged";
                return false;
            }

            long nowTicks = DateTime.UtcNow.Ticks;
            long lastClickTicks = Interlocked.Read(ref portLastCombatCursorClickTicks);
            if (nowTicks - lastClickTicks < TimeSpan.FromMilliseconds(120).Ticks)
            {
                skipReason = "cursor click gap active";
                return false;
            }

            Interlocked.Exchange(ref portLastCombatCursorClickTicks, nowTicks);
            return true;
        }

        private bool PortCombatCursorChanged(out string skipReason)
        {
            skipReason = "";
            IntPtr cursorHandle = PortCurrentCursorHandle();
            if (portOriginalCursorHandle == IntPtr.Zero)
            {
                skipReason = "original cursor handle unavailable";
                return false;
            }

            if (cursorHandle == IntPtr.Zero)
            {
                skipReason = "current cursor handle unavailable";
                return false;
            }

            if (cursorHandle == portOriginalCursorHandle)
            {
                skipReason = "cursor handle unchanged";
                return false;
            }

            return true;
        }

        private void PortLogCombatCursorClickSkipped(string reason)
        {
            long nowTicks = DateTime.UtcNow.Ticks;
            long lastLogTicks = Interlocked.Read(ref portLastCombatCursorDecisionLogTicks);
            if (nowTicks - lastLogTicks < TimeSpan.FromSeconds(1).Ticks)
            {
                return;
            }

            Interlocked.Exchange(ref portLastCombatCursorDecisionLogTicks, nowTicks);
            PortCombatClickDiagnostics diagnostics = PortGetCombatClickDiagnostics();
            AppLogger.Info($"Combat cursor loop: left click skipped; combatInputMode=PhysicalCursor; clickSendMethod=skipped; skipReason={PortLogField(reason)}; originalCursorHandle=0x{portOriginalCursorHandle.ToInt64():X}; currentCursorHandle=0x{diagnostics.CursorHandle.ToInt64():X}; blocked=False; foreground=0x{diagnostics.ForegroundWindow.ToInt64():X}; {PortCombatInputContext()}");
        }

        private void PortLogDemonHunterRightHeldNoClickSuppressionActive()
        {
            long nowTicks = DateTime.UtcNow.Ticks;
            long lastLogTicks = Interlocked.Read(ref portLastDemonHunterRightHeldNoClickLogTicks);
            if (nowTicks - lastLogTicks < TimeSpan.FromSeconds(1).Ticks)
            {
                return;
            }

            Interlocked.Exchange(ref portLastDemonHunterRightHeldNoClickLogTicks, nowTicks);
            AppLogger.Info($"DemonHunterRightHeldNoClickSuppressionActive: combatInputMode=PhysicalCursorHeldFromSafeRegion; clickSendMethod=left-suppressed-right-held; rightDown=true; leftClickSent=false; rightReleased=false; combatRunning={portCombatRunning}; combatClass={portCombatClass}; {PortCombatInputContext()}");
        }

        private void PortHandleWitchDoctorMouseWheelInput(bool scrollSafe)
        {
            if (!scrollSafe)
            {
                PortLogWitchDoctorMouseWheelSkippedNoClickRegion();
                return;
            }

            PortRuntimeMouseWheel(-120);
            PortLogWitchDoctorMouseWheelSent();
        }

        private bool PortWitchDoctorMouseWheelIsSafe()
        {
            PortCombatClickDiagnostics diagnostics = PortGetCombatClickDiagnostics();
            return diagnostics.DiabloActive &&
                diagnostics.ForegroundIsDiablo &&
                diagnostics.HasCursor &&
                diagnostics.CursorInsideDiablo;
        }

        private void PortLogWitchDoctorMouseWheelSent()
        {
            long nowTicks = DateTime.UtcNow.Ticks;
            long lastLogTicks = Interlocked.Read(ref portLastWitchDoctorWheelSentLogTicks);
            if (nowTicks - lastLogTicks < TimeSpan.FromSeconds(1).Ticks)
            {
                return;
            }

            Interlocked.Exchange(ref portLastWitchDoctorWheelSentLogTicks, nowTicks);
            AppLogger.Info($"WitchDoctorMouseWheelScrollSent: combatClass=witch_doctor; combatInputMode=MouseWheelScroll; wheelScrollSent=true; wheelScrollSkipped=false; wheelDelta=-120; heldLeftMode=false; heldRightMode=false; leftDown={portRuntimeLeftMouseHeld}; rightDown={portRuntimeRightMouseHeld}; noHeldLeftState=True; noHeldRightState=True; {PortCombatInputContext()}");
        }

        private void PortLogWitchDoctorMouseWheelSkippedNoClickRegion()
        {
            long nowTicks = DateTime.UtcNow.Ticks;
            long lastLogTicks = Interlocked.Read(ref portLastWitchDoctorWheelSkippedLogTicks);
            if (nowTicks - lastLogTicks < TimeSpan.FromSeconds(1).Ticks)
            {
                return;
            }

            Interlocked.Exchange(ref portLastWitchDoctorWheelSkippedLogTicks, nowTicks);
            PortCombatClickDiagnostics diagnostics = PortGetCombatClickDiagnostics();
            string noClickRegionName = diagnostics.InsideNoClickRegion ? diagnostics.NoClickRegionName : "";
            string skipReason = !diagnostics.DiabloActive
                ? "DiabloInactive"
                : !diagnostics.ForegroundIsDiablo
                    ? "ForegroundNotDiablo"
                    : !diagnostics.HasCursor
                        ? "CursorUnavailable"
                        : !diagnostics.CursorInsideDiablo
                            ? "CursorOutsideDiablo"
                            : diagnostics.InsideNoClickRegion
                                ? "NoClickRegion"
                                : "UnsafeCursor";
            AppLogger.Info($"WitchDoctorMouseWheelScrollSkipped: combatClass=witch_doctor; combatInputMode=MouseWheelScroll; wheelScrollSent=false; wheelScrollSkipped=true; skipReason={skipReason}; noClickRegionName={noClickRegionName}; heldLeftMode=false; heldRightMode=false; leftDown={portRuntimeLeftMouseHeld}; rightDown={portRuntimeRightMouseHeld}; noHeldLeftState=True; noHeldRightState=True; {PortCombatInputContext()}");
        }

        private void PortLogWitchDoctorCursorChangeLeftClickCheck(bool cursorChanged)
        {
            long nowTicks = DateTime.UtcNow.Ticks;
            long lastLogTicks = Interlocked.Read(ref portLastWitchDoctorCursorLeftClickCheckLogTicks);
            if (nowTicks - lastLogTicks < TimeSpan.FromSeconds(1).Ticks)
            {
                return;
            }

            Interlocked.Exchange(ref portLastWitchDoctorCursorLeftClickCheckLogTicks, nowTicks);
            PortCombatClickDiagnostics diagnostics = PortGetCombatClickDiagnostics();
            AppLogger.Info($"WitchDoctorCursorChangeLeftClickCheck: combatClass=witch_doctor; combatInputMode=MouseWheelScroll; clickSendMethod=suppressed; heldLeftMode=false; heldRightMode=false; cursorChanged={cursorChanged}; originalCursorHandle=0x{portOriginalCursorHandle.ToInt64():X}; currentCursorHandle=0x{diagnostics.CursorHandle.ToInt64():X}; leftDown={portRuntimeLeftMouseHeld}; rightDown={portRuntimeRightMouseHeld}; {PortCombatInputContext()}");
        }

        private void PortLogWitchDoctorCursorChangeLeftClickSent(bool cursorChanged)
        {
            long nowTicks = DateTime.UtcNow.Ticks;
            long lastLogTicks = Interlocked.Read(ref portLastWitchDoctorCursorLeftClickSentLogTicks);
            if (nowTicks - lastLogTicks < TimeSpan.FromSeconds(1).Ticks)
            {
                return;
            }

            Interlocked.Exchange(ref portLastWitchDoctorCursorLeftClickSentLogTicks, nowTicks);
            PortCombatClickDiagnostics diagnostics = PortGetCombatClickDiagnostics();
            AppLogger.Info($"WitchDoctorCursorChangeLeftClickSent: combatClass=witch_doctor; combatInputMode=MouseWheelScroll; clickSendMethod=mouse_event; mouseEventSequence=LEFTDOWN,LEFTUP; leftClickSent=true; leftClickSkipped=false; heldLeftMode=false; heldRightMode=false; cursorChanged={cursorChanged}; originalCursorHandle=0x{portOriginalCursorHandle.ToInt64():X}; currentCursorHandle=0x{diagnostics.CursorHandle.ToInt64():X}; noClickRegionName=; blockReason=; leftDown={portRuntimeLeftMouseHeld}; rightDown={portRuntimeRightMouseHeld}; noHeldLeftState=True; noHeldRightState=True; {PortCombatInputContext()}");
        }

        private void PortLogWitchDoctorCursorChangeLeftClickSkipped(string reason, bool cursorChanged)
        {
            long nowTicks = DateTime.UtcNow.Ticks;
            long lastLogTicks = Interlocked.Read(ref portLastWitchDoctorCursorLeftClickSkippedLogTicks);
            if (nowTicks - lastLogTicks < TimeSpan.FromSeconds(1).Ticks)
            {
                return;
            }

            Interlocked.Exchange(ref portLastWitchDoctorCursorLeftClickSkippedLogTicks, nowTicks);
            PortCombatClickDiagnostics diagnostics = PortGetCombatClickDiagnostics();
            string blockReason = diagnostics.InsideNoClickRegion
                ? diagnostics.NoClickRegionName
                : reason;
            string noClickRegionName = diagnostics.InsideNoClickRegion ? diagnostics.NoClickRegionName : "";
            AppLogger.Info($"WitchDoctorCursorChangeLeftClickSkipped: combatClass=witch_doctor; combatInputMode=MouseWheelScroll; clickSendMethod=suppressed; leftClickSent=false; leftClickSkipped=true; heldLeftMode=false; heldRightMode=false; cursorChanged={cursorChanged}; originalCursorHandle=0x{portOriginalCursorHandle.ToInt64():X}; currentCursorHandle=0x{diagnostics.CursorHandle.ToInt64():X}; blockReason={PortLogField(blockReason)}; noClickRegionName={PortLogField(noClickRegionName)}; noClickRegionIndex={diagnostics.NoClickRegionIndex}; blocked={!diagnostics.Safe}; leftDown={portRuntimeLeftMouseHeld}; rightDown={portRuntimeRightMouseHeld}; noHeldLeftState=True; noHeldRightState=True; {PortCombatInputContext()}");
        }

        private bool PortDemonHunterMomentumReady()
        {
            string imagePath = Img("Combat", "Momentum Count 20.png");

            if (!File.Exists(imagePath))
            {
                AddWorkflowStep("Demon Hunter momentum template missing: Momentum Count 20.png");
                return false;
            }

            Rectangle momentumRegion = PortScanRegion("MomentumStack", imagePath);

            double confidence = PortBestTemplateConfidenceInDiabloRegion(imagePath, momentumRegion);

            if (confidence < 0.86)
            {
                AppLogger.Info($"Demon Hunter Momentum Count 20 confidence={confidence:0.000}");
            }

            return confidence >= 0.86;
        }

        private bool PortDemonHunterMomentumCountTemplateAvailable()
        {
            return File.Exists(Img("Combat", "Momentum Count 20.png"));
        }

        private void PortStartCombatMenuWatcher(CancellationToken token)
        {
            if (portCombatMenuWatcherTask != null && !portCombatMenuWatcherTask.IsCompleted)
            {
                return;
            }

            portCombatMenuWatcherTask = Task.Run(() =>
            {
                try
                {
                    PortCombatMenuWatcherLoop(token);
                }
                catch (Exception ex)
                {
                    AppLogger.Error("Combat menu watcher failed.", ex);
                }
            }, CancellationToken.None);
        }

        private void PortCombatMenuWatcherLoop(CancellationToken token)
        {
            string imagePath = Img("Combat", "Bounty Menu Title.png");
            string scanRegionImagePath = Img("Combat", "Bounty Complete Scan Region.png");
            if (!File.Exists(imagePath))
            {
                AppLogger.Info($"CombatMenuWatcherDisabled: reason=bounty title template missing; imagePath={imagePath}; combatActive={portCombatRunning}");
                return;
            }

            Rectangle? referenceRegion = PortScanRegionForImage(imagePath);
            if (!referenceRegion.HasValue)
            {
                AppLogger.Info($"CombatMenuWatcherDisabled: reason=bounty scan region missing; imagePath={imagePath}; scanRegionImagePath={scanRegionImagePath}; scanRegionImageExists={File.Exists(scanRegionImagePath)}; combatActive={portCombatRunning}");
                return;
            }

            string imageName = Path.GetFileName(imagePath);
            AppLogger.Info($"CombatMenuWatcherStarted: target=Bounty menu; imagePath={imagePath}; scanRegionImagePath={scanRegionImagePath}; scanRegionImageExists={File.Exists(scanRegionImagePath)}; referenceRegion={FormatRectangle(referenceRegion.Value)}; threshold={PortBountyMenuConfidence:0.000}; pollIntervalMs={AppSettings.Bounty.PollIntervalMs}; escapeCooldownMs={AppSettings.Bounty.EscapeCooldownMs}; combatActive={portCombatRunning}");

            while (!token.IsCancellationRequested && portCombatRunning)
            {
                if (!PortDiabloIsActive())
                {
                    Thread.Sleep(AppSettings.Bounty.PollIntervalMs);
                    continue;
                }

                double confidence = PortBestTemplateConfidenceInDiabloRegion(imagePath, referenceRegion.Value);
                if (confidence >= PortBountyMenuConfidence)
                {
                    long nowTicks = DateTime.UtcNow.Ticks;
                    long lastCloseTicks = Interlocked.Read(ref portLastBountyMenuCloseTicks);
                    if (nowTicks - lastCloseTicks >= TimeSpan.FromMilliseconds(AppSettings.Bounty.EscapeCooldownMs).Ticks)
                    {
                        Interlocked.Exchange(ref portLastBountyMenuCloseTicks, nowTicks);
                        AppLogger.Info($"BountyMenuDetected: confidence={confidence:0.000}; threshold={PortBountyMenuConfidence:0.000}; detectionSource=CombatMenuWatcher; imagePath={imagePath}; scanRegionImagePath={scanRegionImagePath}; imageName={imageName}; combatActive={portCombatRunning}; combatStopping={portCombatStopping}; diabloActive=True; automationCancelled=false");
                        PortPressKey(PortVkReturn);
                        Thread.Sleep(80);
                        double postEnterConfidence = PortBestTemplateConfidenceInDiabloRegion(imagePath, referenceRegion.Value);
                        bool escapeSent = false;
                        if (postEnterConfidence >= PortBountyMenuConfidence)
                        {
                            PortPressEscapeForAutomation("BountyMenuCombatWatcher");
                            escapeSent = true;
                        }

                        AppLogger.Info($"BountyMenuClearSent: closeMethod=EnterThenEscapeIfStillVisible; source=CombatMenuWatcher; confidence={confidence:0.000}; postEnterConfidence={postEnterConfidence:0.000}; threshold={PortBountyMenuConfidence:0.000}; enterSent=True; escapeSent={escapeSent}; imagePath={imagePath}; scanRegionImagePath={scanRegionImagePath}; imageName={imageName}; combatActive={portCombatRunning}; combatStopping={portCombatStopping}; automationCancelled=false; injectedEscape={escapeSent}");
                    }
                }

                Thread.Sleep(AppSettings.Bounty.PollIntervalMs);
            }

            AppLogger.Info($"CombatMenuWatcherStopped: target=Bounty menu; combatActive={portCombatRunning}; combatStopping={portCombatStopping}; cancelled={token.IsCancellationRequested}");
        }

        private bool PortCombatClickIsSafe()
        {
            return PortGetCombatClickDiagnostics().Safe;
        }

        private void PortLockCursorToDiablo()
        {
            if (PortTryGetDiabloRect(out RECT rect))
            {
                ClipCursor(ref rect);
            }
        }

        private PortCombatClickDiagnostics PortGetCombatClickDiagnostics()
        {
            bool hasRect = PortTryGetDiabloRect(out RECT rect);
            bool hasCursor = GetCursorPos(out DrawingPoint cursor);
            bool cursorInsideDiablo = false;
            bool insideNoClickRegion = false;
            string noClickRegionName = "";
            int noClickRegionIndex = -1;
            Rectangle noClickRegion = Rectangle.Empty;

            if (hasRect && hasCursor)
            {
                cursorInsideDiablo = cursor.X >= rect.Left &&
                    cursor.X < rect.Right &&
                    cursor.Y >= rect.Top &&
                    cursor.Y < rect.Bottom;

                Rectangle diabloRectangle = new(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
                if (CombatClickSafety.TryGetNoClickRegion(cursor, diabloRectangle, out CombatNoClickRegion noClickDefinition, out Rectangle region, out int regionIndex))
                {
                    insideNoClickRegion = true;
                    noClickRegionName = noClickDefinition.Name;
                    noClickRegionIndex = regionIndex;
                    noClickRegion = region;
                }
            }

            bool active = PortDiabloIsActive();
            IntPtr foreground = GetForegroundWindow();
            IntPtr diabloWindow = FindDiabloWindow();
            CURSORINFO cursorInfo = new()
            {
                cbSize = Marshal.SizeOf<CURSORINFO>()
            };
            bool hasCursorInfo = GetCursorInfo(out cursorInfo);
            bool safe = hasCursor && !insideNoClickRegion;

            return new PortCombatClickDiagnostics(
                safe,
                hasRect,
                hasCursor,
                active,
                foreground == diabloWindow && diabloWindow != IntPtr.Zero,
                foreground,
                diabloWindow,
                cursor,
                rect,
                cursorInsideDiablo,
                insideNoClickRegion,
                noClickRegionName,
                noClickRegionIndex,
                noClickRegion,
                hasCursorInfo,
                hasCursorInfo ? cursorInfo.hCursor : IntPtr.Zero,
                hasCursorInfo ? cursorInfo.flags : 0);
        }

        private string PortCombatInputContext()
        {
            PortCombatClickDiagnostics diagnostics = PortGetCombatClickDiagnostics();
            string cursorPosition = diagnostics.HasCursor ? $"{diagnostics.Cursor.X},{diagnostics.Cursor.Y}" : "unavailable";
            string diabloRect = diagnostics.HasDiabloRect
                ? $"{diagnostics.DiabloRect.Left},{diagnostics.DiabloRect.Top},{diagnostics.DiabloRect.Right},{diagnostics.DiabloRect.Bottom}"
                : "unavailable";
            string noClickRegion = diagnostics.InsideNoClickRegion
                ? $"true(blockReason={diagnostics.NoClickRegionName}, index={diagnostics.NoClickRegionIndex}, rect={diagnostics.NoClickRegion.Left},{diagnostics.NoClickRegion.Top},{diagnostics.NoClickRegion.Right},{diagnostics.NoClickRegion.Bottom}, intendedClickPoint={cursorPosition})"
                : "false";

            return $"mouse={cursorPosition}; diabloRect={diabloRect}; diabloActive={diagnostics.DiabloActive}; foregroundIsDiablo={diagnostics.ForegroundIsDiablo}; foreground=0x{diagnostics.ForegroundWindow.ToInt64():X}; diabloWindow=0x{diagnostics.DiabloWindow.ToInt64():X}; cursorInsideDiablo={diagnostics.CursorInsideDiablo}; insideNoClickRegion={noClickRegion}; PortCombatClickIsSafe={diagnostics.Safe}; cursorInfo={diagnostics.HasCursorInfo}; cursorHandle=0x{diagnostics.CursorHandle.ToInt64():X}; cursorFlags={diagnostics.CursorFlags}";
        }

        private void PortLogCombatClickDecision(
            string source,
            bool allowed,
            string button,
            ref long lastLogTicks,
            ref bool? lastAllowed)
        {
            long nowTicks = DateTime.UtcNow.Ticks;
            bool stateChanged = lastAllowed != allowed;
            bool throttled = nowTicks - Interlocked.Read(ref lastLogTicks) >= TimeSpan.FromSeconds(1).Ticks;
            if (!stateChanged && !throttled)
            {
                return;
            }

            lastAllowed = allowed;
            Interlocked.Exchange(ref lastLogTicks, nowTicks);
            PortCombatClickDiagnostics diagnostics = PortGetCombatClickDiagnostics();
            string blockReason = !allowed && diagnostics.InsideNoClickRegion
                ? diagnostics.NoClickRegionName
                : "";
            string intendedClickPoint = diagnostics.HasCursor ? $"{diagnostics.Cursor.X},{diagnostics.Cursor.Y}" : "unavailable";
            string diabloRect = diagnostics.HasDiabloRect
                ? $"{diagnostics.DiabloRect.Left},{diagnostics.DiabloRect.Top},{diagnostics.DiabloRect.Right},{diagnostics.DiabloRect.Bottom}"
                : "unavailable";
            string regionRectangle = diagnostics.InsideNoClickRegion
                ? $"{diagnostics.NoClickRegion.Left},{diagnostics.NoClickRegion.Top},{diagnostics.NoClickRegion.Right},{diagnostics.NoClickRegion.Bottom}"
                : "unavailable";

            if (!string.IsNullOrWhiteSpace(blockReason))
            {
                AppLogger.Info($"{source}: {button} click suppressed; combatInputMode=PhysicalCursorNoClickSuppression; clickSendMethod=suppressed; blockReason={blockReason}; noClickRegionName={diagnostics.NoClickRegionName}; activeRegionName={diagnostics.NoClickRegionName}; mouse={intendedClickPoint}; cursor={intendedClickPoint}; intendedClickPoint={intendedClickPoint}; diabloRect={diabloRect}; regionRect={regionRectangle}; activeRegionRect={regionRectangle}; rightMouseHeld={portRuntimeRightMouseHeld}; shiftHeld={portRuntimeShiftHeld}; leftClickSent=false; combatRunning={portCombatRunning}; combatClass={portCombatClass}; combatActive={portCombatRunning}; blocked=true; foreground=0x{diagnostics.ForegroundWindow.ToInt64():X}; {PortCombatInputContext()}");
                PortMaybeLogDemonHunterSuppressionEvidence(source, button, diagnostics, intendedClickPoint, diabloRect, regionRectangle);
                return;
            }

            if (allowed && portCombatClass == "demon_hunter")
            {
                int previousSuppressedCount = Interlocked.Exchange(ref portDemonHunterConsecutiveSuppressedDecisionLogs, 0);
                if (previousSuppressedCount > 0)
                {
                    AppLogger.Info($"DemonHunterClickSafeRecovery: source={PortLogField(source)}; button={PortLogField(button)}; previousSuppressedDecisionLogs={previousSuppressedCount}; rightMouseHeld={portRuntimeRightMouseHeld}; rightHeldFromSafeRegion={portDemonHunterRightHeldFromSafeRegion}; {PortCombatInputContext()}");
                }
            }

            string inputMode = allowed ? "PhysicalCursor" : "PhysicalCursorNoClickSuppression";
            string clickSendMethod = allowed ? "mouse_event" : "suppressed";
            AppLogger.Info($"{source}: {button} click {(allowed ? "allowed" : "blocked")}; combatInputMode={inputMode}; clickSendMethod={clickSendMethod}; blocked={!allowed}; foreground=0x{diagnostics.ForegroundWindow.ToInt64():X}; {PortCombatInputContext()}");
        }

        private void PortMaybeLogDemonHunterSuppressionEvidence(
            string source,
            string button,
            PortCombatClickDiagnostics diagnostics,
            string intendedClickPoint,
            string diabloRect,
            string regionRectangle)
        {
            if (portCombatClass != "demon_hunter" || !portCombatRunning)
            {
                return;
            }

            int suppressedCount = Interlocked.Increment(ref portDemonHunterConsecutiveSuppressedDecisionLogs);
            long nowTicks = DateTime.UtcNow.Ticks;
            long lastEvidenceTicks = Interlocked.Read(ref portLastDemonHunterSuppressionEvidenceTicks);
            if (suppressedCount < 3 || nowTicks - lastEvidenceTicks < TimeSpan.FromSeconds(15).Ticks)
            {
                return;
            }

            Interlocked.Exchange(ref portLastDemonHunterSuppressionEvidenceTicks, nowTicks);
            PortScreenshotPair pair = PortCaptureCombatDiagnosticScreenshot(CombatDiagnosticNames.DemonHunterNoClickSuppressionAction);
            string screenshotPath = !string.IsNullOrWhiteSpace(pair.DiabloPath) ? pair.DiabloPath : pair.AppPath;
            if (string.IsNullOrWhiteSpace(screenshotPath))
            {
                screenshotPath = "None";
            }

            string summary = $"{CombatDiagnosticNames.DemonHunterNoClickSuppressionSummary}: event={CombatDiagnosticNames.DemonHunterNoClickSuppressionEvent}; workflow={CombatDiagnosticNames.DemonHunterNoClickSuppressionWorkflow}; result=Active; class=demon_hunter; source={PortLogField(source)}; button={PortLogField(button)}; consecutiveSuppressedDecisionLogs={suppressedCount}; suppressionReason={PortLogField(diagnostics.NoClickRegionName)}; noClickRegionName={PortLogField(diagnostics.NoClickRegionName)}; noClickRegionIndex={diagnostics.NoClickRegionIndex}; intendedClickPoint={PortLogField(intendedClickPoint)}; diabloRect={PortLogField(diabloRect)}; regionRect={PortLogField(regionRectangle)}; clickSendMethod=suppressed; combatActive={portCombatRunning}; rightMouseHeld={portRuntimeRightMouseHeld}; rightHeldFromSafeRegion={portDemonHunterRightHeldFromSafeRegion}; screenshotPath={PortLogField(PortDisplayLocation(screenshotPath))}; likelyExplanation=Demon Hunter combat remained active while repeated combat clicks were suppressed because the cursor stayed in no-click UI regions.";
            AppLogger.Info(summary);
        }

        private sealed record PortCombatClickDiagnostics(
            bool Safe,
            bool HasDiabloRect,
            bool HasCursor,
            bool DiabloActive,
            bool ForegroundIsDiablo,
            IntPtr ForegroundWindow,
            IntPtr DiabloWindow,
            DrawingPoint Cursor,
            RECT DiabloRect,
            bool CursorInsideDiablo,
            bool InsideNoClickRegion,
            string NoClickRegionName,
            int NoClickRegionIndex,
            Rectangle NoClickRegion,
            bool HasCursorInfo,
            IntPtr CursorHandle,
            int CursorFlags);
    }
}
