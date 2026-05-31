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
                long nextLootAt = 0;
                long nextKadalaAt = 0;
                Stopwatch sw = Stopwatch.StartNew();

                while (portHotkeysRunning)
                {
                    bool escapeDown = (GetAsyncKeyState(PortVkEscape) & 0x8000) != 0;
                    bool backtickDown = (GetAsyncKeyState(PortVkBacktick) & 0x8000) != 0;
                    bool altDown = (GetAsyncKeyState(PortVkAlt) & 0x8000) != 0;
                    long now = sw.ElapsedMilliseconds;
                    bool scriptedEscapeActive = DateTime.UtcNow.Ticks < Interlocked.Read(ref portIgnoreEscapeHotkeyUntilTicks);

                    if (!scriptedEscapeActive && escapeDown && !escapeWasDown && (isAutomationRunning || portCombatRunning))
                    {
                        BeginInvoke(new Action(() => PortStopAllAutomation("Escape")));
                    }

                    escapeWasDown = escapeDown;

                    if (chkCombat.Checked && backtickDown && !backtickWasDown && !altDown)
                    {
                        BeginInvoke(new Action(PortToggleCombat));
                    }

                    backtickWasDown = backtickDown;

                    if (chkLoot.Checked && altDown && backtickDown && PortDiabloIsActive() && now >= nextLootAt)
                    {
                        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                        Thread.Sleep(25);
                        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                        nextLootAt = now + 75;
                    }

                    if (chkKadala.Checked && (GetAsyncKeyState(PortVkUp) & 0x8000) != 0 && PortDiabloIsActive() && now >= nextKadalaAt)
                    {
                        mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, UIntPtr.Zero);
                        Thread.Sleep(25);
                        mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero);
                        nextKadalaAt = now + 100;
                    }

                    Thread.Sleep(30);
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
                bool isSkill1 = vkCode == PortVk1;
                bool keyDown = message == PortWmKeyDown || message == PortWmSysKeyDown;
                bool keyUp = message == PortWmKeyUp || message == PortWmSysKeyUp;

                if (isSkill1 && injected)
                {
                    return CallNextHookEx(portKeyboardHookHandle, nCode, wParam, lParam);
                }

                if (isSkill1 && keyDown && PortShouldHandleTeleportNextHotkey())
                {
                    portSuppressSkill1KeyUp = portBlockSkill1TeleportHotkey;
                    BeginInvoke(new Action(PortRunQueuedTeleportHotkey));
                    return portBlockSkill1TeleportHotkey ? new IntPtr(1) : CallNextHookEx(portKeyboardHookHandle, nCode, wParam, lParam);
                }

                if (isSkill1 && keyUp && portSuppressSkill1KeyUp)
                {
                    portSuppressSkill1KeyUp = false;
                    return new IntPtr(1);
                }
            }

            return CallNextHookEx(portKeyboardHookHandle, nCode, wParam, lParam);
        }

        private bool PortShouldHandleTeleportNextHotkey()
        {
            long ignoreUntil = Interlocked.Read(ref portIgnoreTeleportNextUntilTicks);
            return DateTime.UtcNow.Ticks >= ignoreUntil && !portCombatRunning && !isAutomationRunning && PortDiabloIsActive();
        }

        /// <summary>
        /// Runs the queued teleport target selected by the current route state.
        /// </summary>
        private void PortRunQueuedTeleportHotkey()
        {
            if (portCombatRunning || isAutomationRunning || !PortDiabloIsActive())
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

            string target = PortTeleportLocationForKey(portQueuedTeleportKey);
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

            AppLogger.Info($"Teleport Next hotkey queued/next teleport: {target}");
            AddWorkflowStep($"Teleport Next: {target}");
            _ = PortRunAutomationAsync(token => PortRunTeleportButton(target, token, ignoreBlocking: false, source: "Hotkey"));
        }
    }
}
