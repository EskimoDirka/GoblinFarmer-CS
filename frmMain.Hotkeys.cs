using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace GoblinFarmer
{
    public partial class frmMain
    {
        private void PortHotkeyLoop()
        {
            try
            {
                bool backtickWasDown = false;
                bool escapeWasDown = false;
                long nextKadalaAt = 0;
                Stopwatch sw = Stopwatch.StartNew();

                while (portHotkeysRunning)
                {
                bool escapeDown = (GetAsyncKeyState(PortVkEscape) & 0x8000) != 0;
                bool backtickDown = (GetAsyncKeyState(PortVkBacktick) & 0x8000) != 0;
                bool altDown = (GetAsyncKeyState(PortVkAlt) & 0x8000) != 0;
                long now = sw.ElapsedMilliseconds;
                    bool scriptedEscapeActive = DateTime.UtcNow.Ticks < Interlocked.Read(ref portIgnoreEscapeHotkeyUntilTicks);
                    bool diabloActive = PortDiabloIsActive();

                    if (scriptedEscapeActive && escapeDown && !escapeWasDown && (isAutomationRunning || portCombatRunning))
                    {
                        PortLogInjectedEscapeIgnoredByStopWatcher("polling", portAutomationEscapeReason);
                    }
                    else if (!scriptedEscapeActive && escapeDown && !escapeWasDown && (isAutomationRunning || portCombatRunning))
                    {
                        BeginInvoke(new Action(() => PortStopAllAutomation("Escape")));
                    }

                    escapeWasDown = escapeDown;

                    if (chkCombat.Checked && backtickDown && !backtickWasDown && !altDown)
                    {
                        AppLogger.Info($"Combat hotkey accepted: key=backtick; automationRunning={isAutomationRunning}; combatActive={portCombatRunning}; combatStopping={portCombatStopping}; diabloActive={diabloActive}");
                        BeginInvoke(new Action(PortToggleCombat));
                    }

                    backtickWasDown = backtickDown;

                    bool shouldSpamLootClick = chkLoot.Checked && altDown && backtickDown && diabloActive;
                    if (shouldSpamLootClick && !portLootSpamLeftClickDown)
                    {
                        portLootSpamLeftClickDown = true;
                        AppLogger.Info($"Loot spam started; {PortCombatInputContext()}");
                    }
                    else if (!shouldSpamLootClick && portLootSpamLeftClickDown)
                    {
                        portLootSpamLeftClickDown = false;
                        if (portRuntimeLeftMouseHeld)
                        {
                            PortRuntimeMouseUp(MOUSEEVENTF_LEFTUP);
                        }
                        AppLogger.Info($"Loot spam stopped; altDown={altDown}; backtickDown={backtickDown}; diabloActive={diabloActive}; {PortCombatInputContext()}");
                    }

                    if (shouldSpamLootClick && portLootSpamLeftClickDown)
                    {
                        bool clickAllowed = !portCombatRunning || PortCombatClickIsSafe();
                        PortLogCombatClickDecision(
                            "Loot spam",
                            clickAllowed,
                            "left",
                            ref portLastLootSpamDecisionLogTicks,
                            ref portLastLootSpamDecisionAllowed);

                        if (!clickAllowed)
                        {
                            if (portRuntimeLeftMouseHeld)
                            {
                                PortRuntimeMouseUp(MOUSEEVENTF_LEFTUP);
                            }
                        }
                        else
                        {
                            PortRuntimeMouseDown(MOUSEEVENTF_LEFTDOWN);
                            Thread.Sleep(12);
                            PortRuntimeMouseUp(MOUSEEVENTF_LEFTUP);
                        }
                    }

                    if (!diabloActive && portLootSpamLeftClickDown)
                    {
                        AppLogger.Info($"Loot spam stopping because Diablo is not active/focused; {PortCombatInputContext()}");
                        ForceReleaseAllRuntimeInputs("Diablo lost focus");
                    }

                    if (chkKadala.Checked && (GetAsyncKeyState(PortVkUp) & 0x8000) != 0 && diabloActive && now >= nextKadalaAt)
                    {
                        PortRuntimeMouseDown(MOUSEEVENTF_RIGHTDOWN);
                        Thread.Sleep(25);
                        PortRuntimeMouseUp(MOUSEEVENTF_RIGHTUP);
                        nextKadalaAt = now + 100;
                    }

                    Thread.Sleep(15);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("Hotkey loop failed.", ex);
            }
        }

        private void PortInstallKeyboardHook()
        {
            if (portKeyboardHookHandle != IntPtr.Zero)
            {
                return;
            }

            portKeyboardProc = PortKeyboardHookCallback;
            portKeyboardHookHandle = SetWindowsHookEx(PortWhKeyboardLl, portKeyboardProc, GetModuleHandle(null), 0);
            if (portKeyboardHookHandle == IntPtr.Zero)
            {
                AppLogger.Error($"Failed to install keyboard hook. Win32Error={Marshal.GetLastWin32Error()}");
            }
        }

        private void PortUninstallKeyboardHook()
        {
            if (portKeyboardHookHandle == IntPtr.Zero)
            {
                return;
            }

            UnhookWindowsHookEx(portKeyboardHookHandle);
            portKeyboardHookHandle = IntPtr.Zero;
            portKeyboardProc = null;
        }

        private IntPtr PortKeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int message = wParam.ToInt32();
                KBDLLHOOKSTRUCT keyInfo = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                int vkCode = keyInfo.vkCode;
                bool injected = (keyInfo.flags & 0x10) != 0;
                bool isEscape = vkCode == PortVkEscape;
                bool isSkill1 = vkCode == PortVk1;
                bool isSkill2 = vkCode == PortVk2;
                bool isGoblinTrackerHotkey = vkCode == PortVkX;
                bool isGoblinCalibrationHotkey = vkCode == PortVkG;
                bool isAutomationNumberHotkey = isSkill1 || isSkill2;
                bool isLootReleaseKey = vkCode == PortVkAlt || vkCode == PortVkBacktick;
                bool keyDown = message == PortWmKeyDown || message == PortWmSysKeyDown;
                bool keyUp = message == PortWmKeyUp || message == PortWmSysKeyUp;
                bool ctrlDown = (GetAsyncKeyState(PortVkCtrl) & 0x8000) != 0;
                bool shiftDown = (GetAsyncKeyState(PortVkShift) & 0x8000) != 0;

                if (isGoblinCalibrationHotkey && ctrlDown && shiftDown)
                {
                    if (keyDown && !portGoblinCalibrationHotkeyHandled)
                    {
                        portGoblinCalibrationHotkeyHandled = true;
                        AppLogger.Info("GoblinEvidence: Save calibration snapshot");
                        BeginInvoke(new Action(PortCaptureGoblinEvidenceCalibrationSnapshot));
                    }
                    else if (keyUp)
                    {
                        portGoblinCalibrationHotkeyHandled = false;
                    }

                    return CallNextHookEx(portKeyboardHookHandle, nCode, wParam, lParam);
                }
                else if (isGoblinCalibrationHotkey && keyUp)
                {
                    portGoblinCalibrationHotkeyHandled = false;
                }

                if (isGoblinTrackerHotkey)
                {
                    if (keyDown && !portGoblinTrackerHotkeyHandled)
                    {
                        portGoblinTrackerHotkeyHandled = true;
                        BeginInvoke(new Action(PortIncrementGoblinCount));
                    }
                    else if (keyUp)
                    {
                        portGoblinTrackerHotkeyHandled = false;
                    }

                    return CallNextHookEx(portKeyboardHookHandle, nCode, wParam, lParam);
                }

                if (isEscape && injected && keyDown && (isAutomationRunning || portCombatRunning))
                {
                    PortLogInjectedEscapeIgnoredByStopWatcher("keyboardHook", portAutomationEscapeReason);
                    return CallNextHookEx(portKeyboardHookHandle, nCode, wParam, lParam);
                }

                if (isLootReleaseKey && keyUp && portLootSpamLeftClickDown)
                {
                    portLootSpamLeftClickDown = false;
                    if (portRuntimeLeftMouseHeld)
                    {
                        PortRuntimeMouseUp(MOUSEEVENTF_LEFTUP);
                    }
                    AppLogger.Info($"Loot spam stopped by key release; vk={vkCode}; {PortCombatInputContext()}");
                }

                if (isAutomationNumberHotkey && injected)
                {
                    return CallNextHookEx(portKeyboardHookHandle, nCode, wParam, lParam);
                }

                if (isAutomationNumberHotkey && DateTime.UtcNow.Ticks < Interlocked.Read(ref portIgnoreAutomationNumberHotkeysUntilTicks))
                {
                    PortLogAutomationNumberHotkeyGuard(vkCode, keyDown, keyUp);
                    return CallNextHookEx(portKeyboardHookHandle, nCode, wParam, lParam);
                }

                if (isSkill2 && (portCombatRunning || portCombatStopping))
                {
                    if (keyDown && !portSkill2CombatHandled)
                    {
                        portSkill2CombatHandled = true;
                        portSuppressSkill2KeyUp = true;
                        AppLogger.Info(portCombatStopping
                            ? "Exit Game hotkey suppressed because combat stop cleanup is active; injected=false; automationHotkey=2; injected combat keys are allowed; exitGameHotkeySuppressed=True"
                            : "Exit Game hotkey suppressed because combat is active; injected=false; automationHotkey=2; injected combat keys are allowed; exitGameHotkeySuppressed=True");
                    }
                    else if (keyUp)
                    {
                        portSkill2CombatHandled = false;
                        portSuppressSkill2KeyUp = false;
                    }

                    return new IntPtr(1);
                }

                if (isSkill1 && portCombatRunning)
                {
                    if (keyDown && !portSkill1TeleportHandled)
                    {
                        portSkill1TeleportHandled = true;
                        portSuppressSkill1KeyUp = true;
                        AppLogger.Info("Teleport hotkey ignored because combat is active; injected=false; automationHotkey=1; injected combat keys are allowed");
                    }
                    else if (keyUp)
                    {
                        portSkill1TeleportHandled = false;
                        portSuppressSkill1KeyUp = false;
                    }

                    return new IntPtr(1);
                }

                if (isSkill1 && keyDown && PortShouldHandleTeleportNextHotkey())
                {
                    if (!portSkill1TeleportHandled)
                    {
                        portSkill1TeleportHandled = true;
                        portSuppressSkill1KeyUp = true;
                        AppLogger.Info($"Teleport Next hotkey accepted: injected=false; automationHotkey=1; combatActive={portCombatRunning}; combatStopping={portCombatStopping}; automationRunning={isAutomationRunning}");
                        BeginInvoke(new Action(PortRunQueuedTeleportHotkey));
                    }

                    if (PortBlockSkill1DuringTeleportHotkey)
                    {
                        return new IntPtr(1);
                    }
                }

                if (isSkill2 && keyDown && PortShouldHandleExitGameHotkey())
                {
                    if (!portSkill2CombatHandled)
                    {
                        portSkill2CombatHandled = true;
                        portSuppressSkill2KeyUp = true;
                        AppLogger.Info("Exit Game hotkey accepted: injected=false; automationHotkey=2; combatActive=False; combatStopping=False");
                        BeginInvoke(new Action(PortRunExitGameHotkey));
                    }

                    return new IntPtr(1);
                }

                if (isSkill2 && keyUp && portSuppressSkill2KeyUp)
                {
                    portSuppressSkill2KeyUp = false;
                    portSkill2CombatHandled = false;
                    return new IntPtr(1);
                }

                if (isSkill1 && keyUp && portSuppressSkill1KeyUp)
                {
                    portSuppressSkill1KeyUp = false;
                    portSkill1TeleportHandled = false;
                    return new IntPtr(1);
                }
                else if (isSkill1 && keyUp)
                {
                    portSkill1TeleportHandled = false;
                }

                if (isSkill2 && keyUp)
                {
                    portSkill2CombatHandled = false;
                }
            }

            return CallNextHookEx(portKeyboardHookHandle, nCode, wParam, lParam);
        }

        private void PortLogAutomationNumberHotkeyGuard(int vkCode, bool keyDown, bool keyUp)
        {
            long nowTicks = DateTime.UtcNow.Ticks;
            long lastLogTicks = Interlocked.Read(ref portLastAutomationNumberHotkeyGuardLogTicks);
            if (nowTicks - lastLogTicks < TimeSpan.FromMilliseconds(500).Ticks)
            {
                return;
            }

            Interlocked.Exchange(ref portLastAutomationNumberHotkeyGuardLogTicks, nowTicks);
            string phase = keyDown ? "down" : keyUp ? "up" : "other";
            AppLogger.Info($"Automation number hotkey ignored by self-injection guard: vk={vkCode}; phase={phase}; injectedFlag=false; combatActive={portCombatRunning}; combatStopping={portCombatStopping}; automationRunning={isAutomationRunning}");
        }

        private void PortLogInjectedEscapeIgnoredByStopWatcher(string watcher, string reason)
        {
            long nowTicks = DateTime.UtcNow.Ticks;
            long lastLogTicks = Interlocked.Read(ref portLastInjectedEscapeIgnoredLogTicks);
            if (nowTicks - lastLogTicks < TimeSpan.FromMilliseconds(250).Ticks)
            {
                return;
            }

            Interlocked.Exchange(ref portLastInjectedEscapeIgnoredLogTicks, nowTicks);
            string source = string.IsNullOrWhiteSpace(reason) ? "Automation" : reason;
            AppLogger.Info($"InjectedEscapeIgnoredByStopWatcher: watcher={watcher}; source={source}; injectedEscape=true; combatActive={portCombatRunning}; automationRunning={isAutomationRunning}; ignoreUntilTicks={Interlocked.Read(ref portIgnoreEscapeHotkeyUntilTicks)}");
        }

        private bool PortShouldHandleTeleportNextHotkey()
        {
            return portTeleportNextHotkeyEnabled && !portCombatRunning && PortDiabloIsActive();
        }

        private bool PortShouldHandleExitGameHotkey()
        {
            return portExitGameHotkeyEnabled && !portCombatRunning && !portCombatStopping && PortDiabloIsActive();
        }

        private void PortRunExitGameHotkey()
        {
            if (portCombatRunning || portCombatStopping)
            {
                AppLogger.Info($"Exit Game hotkey ignored after dispatch because combat is active/stopping; combatActive={portCombatRunning}; combatStopping={portCombatStopping}; exitGameHotkeySuppressed=True");
                return;
            }

            if (!PortDiabloIsActive())
            {
                AppLogger.Info("Exit Game hotkey ignored: Diablo is not active/focused");
                return;
            }

            AppLogger.Info($"Exit Game hotkey starting flow: automationRunning={isAutomationRunning}; combatActive={portCombatRunning}; combatStopping={portCombatStopping}; source=Hotkey2");
            AddWorkflowStep("Exit Game hotkey: starting flow");
            _ = PortRunAutomationAsync(PortExitGameFlow);
        }

        /// <summary>
        /// Runs the queued teleport target selected by the current route state.
        /// </summary>
        private void PortRunQueuedTeleportHotkey()
        {
            if (portCombatRunning || !PortDiabloIsActive())
            {
                return;
            }

            long now = Stopwatch.GetTimestamp();
            long previous = Interlocked.Read(ref portLastTeleportNextHotkeyTicks);
            if (previous != 0 && (now - previous) * 1000 / Stopwatch.Frequency < 350)
            {
                return;
            }

            Interlocked.Exchange(ref portLastTeleportNextHotkeyTicks, now);
            if (isAutomationRunning)
            {
                AppLogger.Info("Teleport Next hotkey preempting active non-combat automation");
                portAutomationCts?.Cancel();
                isAutomationRunning = false;
                ForceReleaseAllRuntimeInputs("teleport hotkey preempted automation");
            }

            string queuedBefore = PortTeleportLocationForKey(portQueuedTeleportKey);
            string previousConfirmedBeforeHotkey = portLastConfirmedLocation;
            portHotkeyFreshRawLocation = "";
            string freshRawLocation = PortFreshHotkeyRouteLocationScan(queuedBefore, out PortLocationDetectionResult freshScanResult);
            if (!string.IsNullOrWhiteSpace(freshRawLocation))
            {
                portHotkeyFreshRawLocation = freshRawLocation;
            }

            string target = queuedBefore;
            bool routeAdvancedFromFreshScan = PortTryAdvanceQueuedTeleportFromFreshHotkeyScan(freshRawLocation, queuedBefore, out string advancedTarget);
            if (routeAdvancedFromFreshScan)
            {
                target = advancedTarget;
            }

            bool routeEndGuardCorrectedState = false;
            if (!routeAdvancedFromFreshScan &&
                PortQueuedTargetLooksLikeRouteEnd(queuedBefore) &&
                portLastTeleportKey == PortLocationKey("Pandemonium Fortress Level 2"))
            {
                AppLogger.Info(
                    $"TeleportNextRouteEndGuardEvaluate: previousConfirmedLocation={PortDisplayLocation(previousConfirmedBeforeHotkey)}; " +
                    $"queuedTargetBeforeHotkey={PortDisplayLocation(queuedBefore)}; " +
                    $"cachedCurrentLocation={PortDisplayLocation(PortTeleportLocationForKey(portLastTeleportKey))}; " +
                    $"freshDetectedLocation={PortDisplayLocation(freshRawLocation)}; " +
                    $"freshDisplayLocation={PortDisplayLocation(PortDetectedLocationDisplayName(freshRawLocation))}; " +
                    $"freshScanBest={PortDisplayLocation(freshScanResult.BestName)}; " +
                    $"freshScanConfidence={freshScanResult.BestConfidence:0.000}; " +
                    $"freshScanResult={(string.IsNullOrWhiteSpace(freshRawLocation) ? "NoValidRouteLocation" : "ValidRouteLocation")}");
                routeEndGuardCorrectedState = PortTryCorrectRouteEndFromFreshHotkeyScan(
                    freshRawLocation,
                    queuedBefore,
                    freshScanResult,
                    out string correctedRouteEndTarget);
                if (routeEndGuardCorrectedState)
                {
                    target = correctedRouteEndTarget;
                }
            }

            bool freshScanMatchesQueuedTarget = PortLocationMatchesForArrival(freshRawLocation, queuedBefore);
            string freshTarget = routeAdvancedFromFreshScan || freshScanMatchesQueuedTarget
                ? ""
                : PortNextTeleportForConfirmedLocation("", freshRawLocation);
            if (!routeEndGuardCorrectedState && !string.IsNullOrWhiteSpace(freshTarget))
            {
                target = freshTarget;
            }

            if (string.IsNullOrWhiteSpace(target) &&
                portRouteNextTeleports.TryGetValue(PortTeleportLocationForKey(portLastTeleportKey), out string? nextLocation))
            {
                target = nextLocation;
            }

            if (string.IsNullOrWhiteSpace(target))
            {
                if (portLastTeleportKey == PortLocationKey("Pandemonium Fortress Level 2"))
                {
                    if (!PortFreshHotkeyScanConfirmsRouteEnd(freshRawLocation))
                    {
                        AppLogger.Info(
                            $"TeleportNextRouteEndGuardSuppressed: previousConfirmedLocation={PortDisplayLocation(previousConfirmedBeforeHotkey)}; " +
                            $"queuedTargetBeforeHotkey={PortDisplayLocation(queuedBefore)}; " +
                            $"cachedCurrentLocation={PortDisplayLocation(PortTeleportLocationForKey(portLastTeleportKey))}; " +
                            $"freshDetectedLocation={PortDisplayLocation(freshRawLocation)}; " +
                            $"freshDisplayLocation={PortDisplayLocation(PortDetectedLocationDisplayName(freshRawLocation))}; " +
                            $"freshScanBest={PortDisplayLocation(freshScanResult.BestName)}; " +
                            $"freshScanConfidence={freshScanResult.BestConfidence:0.000}; " +
                            $"freshScanResult={(string.IsNullOrWhiteSpace(freshRawLocation) ? "NoValidRouteLocation" : "NotFinalRouteLocation")}; " +
                            $"routeEndAccepted=False; recomputedQueuedTarget=Unknown; reason=fresh scan did not confirm final route location");
                        AddWorkflowStep("Teleport Next ignored: route end not confirmed");
                        return;
                    }

                    string freshRouteEndButtonLocation = PortGetButtonLocationForDetectedLocation(freshRawLocation);
                    if (!string.IsNullOrWhiteSpace(freshRouteEndButtonLocation))
                    {
                        portLastConfirmedLocation = freshRawLocation;
                        portLastTeleportKey = PortLocationKey(freshRouteEndButtonLocation);
                        portQueuedTeleportKey = "";
                        portLastRouteDecisionOutput = "Unknown";
                        PortApplyTeleportButtonColors();
                    }

                    AppLogger.Info(
                        $"TeleportNextRouteEndGuardAccepted: previousConfirmedLocation={PortDisplayLocation(previousConfirmedBeforeHotkey)}; " +
                        $"queuedTargetBeforeHotkey={PortDisplayLocation(queuedBefore)}; " +
                        $"cachedCurrentLocation={PortDisplayLocation(PortTeleportLocationForKey(portLastTeleportKey))}; " +
                        $"freshDetectedLocation={PortDisplayLocation(freshRawLocation)}; " +
                        $"freshDisplayLocation={PortDisplayLocation(PortDetectedLocationDisplayName(freshRawLocation))}; " +
                        $"freshScanBest={PortDisplayLocation(freshScanResult.BestName)}; " +
                        $"freshScanConfidence={freshScanResult.BestConfidence:0.000}; " +
                        $"freshScanResult=FinalRouteLocationConfirmed; routeEndAccepted=True; recomputedQueuedTarget=Unknown");
                    AppLogger.Info("Teleport Next hotkey reached route end; starting Make New Game flow");
                    AddWorkflowStep("Teleport Next: Make New Game");
                    _ = PortRunAutomationAsync(PortMakeNewGameFlow);
                    return;
                }

                AppLogger.Info("Teleport Next hotkey ignored: no queued/next teleport");
                AddWorkflowStep("Teleport Next ignored: no queued teleport");
                return;
            }

            AppLogger.Info($"Teleport Next hotkey queued/next teleport: {target}; freshRawLocation={PortDisplayLocation(freshRawLocation)}; freshDisplay={PortDisplayLocation(PortDetectedLocationDisplayName(freshRawLocation))}; queuedBefore={PortDisplayLocation(queuedBefore)}; routeAdvancedFromFreshScan={routeAdvancedFromFreshScan}; routeEndGuardCorrectedState={routeEndGuardCorrectedState}");
            AddWorkflowStep($"Teleport Next: {target}");
            bool teleportStarted = !isAutomationRunning && !portCombatRunning && IsDiabloRunning();
            if (routeAdvancedFromFreshScan)
            {
                AppLogger.Info(
                    $"AlreadyAtQueuedDestinationTeleportStart: skippedDestination={PortDisplayLocation(queuedBefore)}; " +
                    $"newRequestedTarget={PortDisplayLocation(target)}; teleportStarted={teleportStarted}; " +
                    $"source=Hotkey; automationRunning={isAutomationRunning}; combatRunning={portCombatRunning}; diabloRunning={IsDiabloRunning()}");
            }

            if (teleportStarted)
            {
                _ = PortRunAutomationAsync(token => PortRunTeleportButton(target, token, ignoreBlocking: false, source: "Hotkey"));
            }
        }
    }
}
