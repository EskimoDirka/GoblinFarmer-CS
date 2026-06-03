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
                bool isAutomationNumberHotkey = isSkill1 || isSkill2;
                bool isLootReleaseKey = vkCode == PortVkAlt || vkCode == PortVkBacktick;
                bool keyDown = message == PortWmKeyDown || message == PortWmSysKeyDown;
                bool keyUp = message == PortWmKeyUp || message == PortWmSysKeyUp;

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

                if (isSkill1 && (portCombatRunning || portCombatStopping))
                {
                    if (keyDown && !portSkill1TeleportHandled)
                    {
                        portSkill1TeleportHandled = true;
                        portSuppressSkill1KeyUp = true;
                        AppLogger.Info(portCombatStopping
                            ? "Teleport hotkey ignored because combat stop cleanup is active; injected=false; automationHotkey=1; injected combat keys are allowed"
                            : "Teleport hotkey ignored because combat is active; injected=false; automationHotkey=1; injected combat keys are allowed");
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
            long ignoreUntil = Interlocked.Read(ref portIgnoreTeleportNextUntilTicks);
            return portTeleportNextHotkeyEnabled && DateTime.UtcNow.Ticks >= ignoreUntil && !portCombatRunning && !portCombatStopping && PortDiabloIsActive();
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
            if (portCombatRunning || portCombatStopping || !PortDiabloIsActive())
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
            portHotkeyFreshRawLocation = "";
            string freshRawLocation = PortFreshHotkeyRouteLocationScan(queuedBefore);
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

            string freshTarget = routeAdvancedFromFreshScan ? "" : PortNextTeleportForConfirmedLocation("", freshRawLocation);
            if (!string.IsNullOrWhiteSpace(freshTarget))
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
                    AppLogger.Info("Teleport Next hotkey reached route end; starting Make New Game flow");
                    AddWorkflowStep("Teleport Next: Make New Game");
                    _ = PortRunAutomationAsync(PortMakeNewGameFlow);
                    return;
                }

                AppLogger.Info("Teleport Next hotkey ignored: no queued/next teleport");
                AddWorkflowStep("Teleport Next ignored: no queued teleport");
                return;
            }

            AppLogger.Info($"Teleport Next hotkey queued/next teleport: {target}; freshRawLocation={PortDisplayLocation(freshRawLocation)}; freshDisplay={PortDisplayLocation(PortDetectedLocationDisplayName(freshRawLocation))}; queuedBefore={PortDisplayLocation(queuedBefore)}; routeAdvancedFromFreshScan={routeAdvancedFromFreshScan}");
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
