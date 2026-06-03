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
            if (portCombatRunning)
            {
                PortStopCombat("hotkey");
                return;
            }

            if (isAutomationRunning || !PortDiabloIsActive())
            {
                AppLogger.Info($"Combat start ignored: isAutomationRunning={isAutomationRunning}; {PortCombatInputContext()}");
                return;
            }

            portCombatCts = new CancellationTokenSource();
            portCombatRunning = true;
            portCombatClass = radWD.Checked ? "witch_doctor" : radDH.Checked ? "demon_hunter" : "monk";
            SetCombatStatus($"{radWD.Checked switch { true => "Witch Doctor", false when radDH.Checked => "Demon Hunter", _ => "Monk" }} Running");
            AddWorkflowStep("Combat started");
            AppLogger.Info($"Combat started: class={portCombatClass}; {PortCombatInputContext()}");
            PortLockCursorToDiablo();
            CancellationToken combatToken = portCombatCts.Token;
            PortStartCombatMenuWatcher(combatToken);

            if (portCombatClass == "witch_doctor")
            {
                PortRunCombatTask("Witch Doctor loop", () => PortWitchDoctorLoop(combatToken));
                PortRunCombatTask("Combat cursor loop", () => PortCombatCursorLoop(combatToken));
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
            while (!token.IsCancellationRequested &&
                   portCombatRunning &&
                   portCombatClass == "monk")
            {
                if (!PortDiabloIsActive())
                {
                    BeginInvoke(new Action(() => PortStopCombat("Diablo inactive")));
                    return;
                }

                PortPressKey(PortVk1);

                Thread.Sleep(10);

                PortPressKey(PortVk2);

                Thread.Sleep(10);

                PortPressKey(0x33);

                Thread.Sleep(10);
            }

            ForceReleaseAllRuntimeInputs("Monk loop exit");
        }

        // Witch Doctor combat loop =================================
        private void PortWitchDoctorLoop(CancellationToken token)
        {
            AddWorkflowStep("Witch Doctor opening rotation");

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

            Stopwatch sw = Stopwatch.StartNew();

            while (!token.IsCancellationRequested &&
                   portCombatRunning &&
                   portCombatClass == "demon_hunter" &&
                   sw.ElapsedMilliseconds < 6000)
            {
                if (!PortDiabloIsActive())
                {
                    BeginInvoke(new Action(() => PortStopCombat("Diablo inactive")));
                    return;
                }

                if (PortDemonHunterMomentumReady())
                {
                    AddWorkflowStep("Demon Hunter momentum build complete");
                    return;
                }

                PortDemonHunterShiftLeftClick(token);
                Thread.Sleep(150);
            }

            AddWorkflowStep("Demon Hunter momentum build finished or timed out");
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

                if (!PortDemonHunterShiftLeftClick(token))
                {
                    AppLogger.Info($"Demon Hunter momentum maintenance skipped unsafe Shift+Left Click; combat remains active; {PortCombatInputContext()}");
                    PortSleep(token, 350);
                    continue;
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

                    if (portCombatClass == "witch_doctor")
                    {
                        PortHandleWitchDoctorCursorInput(clickSafe);
                    }
                    else if (!clickSafe)
                    {
                        if (portCombatClass == "demon_hunter" && portDemonHunterRightHeldFromSafeRegion && portRuntimeRightMouseHeld)
                        {
                            PortLogDemonHunterRightHeldNoClickSuppressionActive();
                        }

                        if (portRuntimeLeftMouseHeld)
                        {
                            PortRuntimeMouseUp(MOUSEEVENTF_LEFTUP);
                        }
                    }
                    else
                    {
                        PortRuntimeMouseDown(MOUSEEVENTF_LEFTDOWN);
                        Thread.Sleep(10);
                        PortRuntimeMouseUp(MOUSEEVENTF_LEFTUP);
                    }

                    PortSleep(token, 50);
                }
            }
            finally
            {
                if (portRuntimeLeftMouseHeld)
                {
                    PortRuntimeMouseUp(MOUSEEVENTF_LEFTUP);
                }

                if (portWitchDoctorHeldInputFromSafeRegion)
                {
                    AppLogger.Info($"WitchDoctorHeldInputReleased: combatInputMode=PhysicalCursorHeldFromSafeRegion; clickSendMethod=left-up-cleanup; held=false; leftDown=false; inputReleased=true; combatRunning={portCombatRunning}; combatClass=witch_doctor; {PortCombatInputContext()}");
                    portWitchDoctorHeldInputFromSafeRegion = false;
                }
            }
        }

        private void PortHandleWitchDoctorCursorInput(bool clickSafe)
        {
            if (clickSafe)
            {
                if (!portRuntimeLeftMouseHeld)
                {
                    PortRuntimeMouseDown(MOUSEEVENTF_LEFTDOWN);
                    portWitchDoctorHeldInputFromSafeRegion = true;
                    AppLogger.Info($"WitchDoctorScrollHeldFromSafeRegion: combatInputMode=PhysicalCursorHeldFromSafeRegion; scrollSendMethod=left-held-channel; held=true; leftDown=true; inputSent=true; combatRunning={portCombatRunning}; combatClass=witch_doctor; {PortCombatInputContext()}");
                }

                return;
            }

            if (portWitchDoctorHeldInputFromSafeRegion && portRuntimeLeftMouseHeld)
            {
                PortLogWitchDoctorHeldInputNoClickSuppressionActive();
                return;
            }

            PortLogWitchDoctorScrollSuppressedNoClickRegion();
            if (portRuntimeLeftMouseHeld)
            {
                PortRuntimeMouseUp(MOUSEEVENTF_LEFTUP);
            }
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

        private void PortLogWitchDoctorHeldInputNoClickSuppressionActive()
        {
            long nowTicks = DateTime.UtcNow.Ticks;
            long lastLogTicks = Interlocked.Read(ref portLastWitchDoctorHeldInputNoClickLogTicks);
            if (nowTicks - lastLogTicks < TimeSpan.FromSeconds(1).Ticks)
            {
                return;
            }

            Interlocked.Exchange(ref portLastWitchDoctorHeldInputNoClickLogTicks, nowTicks);
            AppLogger.Info($"WitchDoctorHeldInputNoClickSuppressionActive: combatInputMode=PhysicalCursorHeldFromSafeRegion; scrollSendMethod=left-held-no-new-click; held=true; leftDown={portRuntimeLeftMouseHeld}; inputSent=false; inputSuppressed=true; inputReleased=false; combatRunning={portCombatRunning}; combatClass=witch_doctor; {PortCombatInputContext()}");
        }

        private void PortLogWitchDoctorScrollSuppressedNoClickRegion()
        {
            long nowTicks = DateTime.UtcNow.Ticks;
            long lastLogTicks = Interlocked.Read(ref portLastWitchDoctorScrollSuppressedNoClickLogTicks);
            if (nowTicks - lastLogTicks < TimeSpan.FromSeconds(1).Ticks)
            {
                return;
            }

            Interlocked.Exchange(ref portLastWitchDoctorScrollSuppressedNoClickLogTicks, nowTicks);
            PortCombatClickDiagnostics diagnostics = PortGetCombatClickDiagnostics();
            string noClickRegionName = diagnostics.InsideNoClickRegion ? diagnostics.NoClickRegionName : "";
            AppLogger.Info($"WitchDoctorScrollSuppressedNoClickRegion: combatInputMode=PhysicalCursorNoClickSuppression; scrollSendMethod=suppressed; held={portWitchDoctorHeldInputFromSafeRegion}; leftDown={portRuntimeLeftMouseHeld}; inputSent=false; inputSuppressed=true; inputReleased=false; noClickRegionName={noClickRegionName}; combatRunning={portCombatRunning}; combatClass=witch_doctor; {PortCombatInputContext()}");
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

            if (confidence < 0.75)
            {
                AppLogger.Info($"Demon Hunter Momentum Count 20 confidence={confidence:0.000}");
            }

            return confidence >= 0.75;
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
                        PortPressEscapeForAutomation("BountyMenuCombatWatcher");
                        AppLogger.Info($"BountyMenuEscapeSent: closeMethod=Escape; source=CombatMenuWatcher; confidence={confidence:0.000}; threshold={PortBountyMenuConfidence:0.000}; imagePath={imagePath}; scanRegionImagePath={scanRegionImagePath}; imageName={imageName}; combatActive={portCombatRunning}; combatStopping={portCombatStopping}; automationCancelled=false; injectedEscape=true");
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

                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;
                for (int i = 0; i < portCombatNoClickRegions.Length; i++)
                {
                    PortCombatNoClickRegion noClickDefinition = portCombatNoClickRegions[i];
                    Rectangle region = new(
                        rect.Left + (int)Math.Round(width * noClickDefinition.Left),
                        rect.Top + (int)Math.Round(height * noClickDefinition.Top),
                        (int)Math.Round(width * noClickDefinition.Width),
                        (int)Math.Round(height * noClickDefinition.Height));

                    if (region.Contains(cursor))
                    {
                        insideNoClickRegion = true;
                        noClickRegionName = noClickDefinition.Name;
                        noClickRegionIndex = i;
                        noClickRegion = region;
                        break;
                    }
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
            bool safe = hasRect && hasCursor && cursorInsideDiablo && !insideNoClickRegion;

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
                AppLogger.Info($"{source}: {button} click suppressed; combatInputMode=PhysicalCursorNoClickSuppression; clickSendMethod=suppressed; blockReason={blockReason}; noClickRegionName={diagnostics.NoClickRegionName}; mouse={intendedClickPoint}; intendedClickPoint={intendedClickPoint}; diabloRect={diabloRect}; regionRect={regionRectangle}; combatActive={portCombatRunning}; blocked=true; foreground=0x{diagnostics.ForegroundWindow.ToInt64():X}; {PortCombatInputContext()}");
                return;
            }

            string inputMode = allowed ? "PhysicalCursor" : "PhysicalCursorNoClickSuppression";
            string clickSendMethod = allowed ? "mouse_event" : "suppressed";
            AppLogger.Info($"{source}: {button} click {(allowed ? "allowed" : "blocked")}; combatInputMode={inputMode}; clickSendMethod={clickSendMethod}; blocked={!allowed}; foreground=0x{diagnostics.ForegroundWindow.ToInt64():X}; {PortCombatInputContext()}");
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
