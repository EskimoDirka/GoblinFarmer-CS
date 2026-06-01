using OpenCvSharp;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
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
            portMonkKeyIndex = 1;
            portCombatClass = radWD.Checked ? "witch_doctor" : radDH.Checked ? "demon_hunter" : "monk";
            SetCombatStatus($"{radWD.Checked switch { true => "Witch Doctor", false when radDH.Checked => "Demon Hunter", _ => "Monk" }} Running");
            AddWorkflowStep("Combat started");
            AppLogger.Info($"Combat started: class={portCombatClass}; {PortCombatInputContext()}");
            PortLockCursorToDiablo();
            CancellationToken combatToken = portCombatCts.Token;

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

            Interlocked.Exchange(ref portIgnoreTeleportNextUntilTicks, DateTime.UtcNow.AddMilliseconds(900).Ticks);
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
            if (!ready)
            {
                AppLogger.Info("Witch Doctor Hex not ready during stop cleanup; pressing 1 once to exit chicken mode");
                AddWorkflowStep("Exiting Witch Doctor chicken mode");
                PortPressKey(PortVk1);
                ForceReleaseAllRuntimeInputs("Witch Doctor chicken exit press");
                Thread.Sleep(250);
            }

            Stopwatch sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < 3000)
            {
                if (!PortDiabloIsActive())
                {
                    AppLogger.Info("Witch Doctor stop cleanup ended: Diablo inactive");
                    return;
                }

                if (PortWitchDoctorHexReady())
                {
                    AppLogger.Info($"Witch Doctor stop cleanup complete: Hex ready after {sw.ElapsedMilliseconds}ms");
                    AddWorkflowStep("Witch Doctor stop cleanup complete");
                    return;
                }

                Thread.Sleep(150);
            }

            AppLogger.Info("Witch Doctor stop cleanup timeout: Hex not ready");
            AddWorkflowStep("Witch Doctor stop cleanup timed out");
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

                keybd_event((byte)'1', 0, 0, UIntPtr.Zero);
                Thread.Sleep(10);
                keybd_event((byte)'1', 0, PortKeyUp, UIntPtr.Zero);

                Thread.Sleep(10);

                keybd_event((byte)'2', 0, 0, UIntPtr.Zero);
                Thread.Sleep(10);
                keybd_event((byte)'2', 0, PortKeyUp, UIntPtr.Zero);

                Thread.Sleep(10);

                keybd_event((byte)'3', 0, 0, UIntPtr.Zero);
                Thread.Sleep(10);
                keybd_event((byte)'3', 0, PortKeyUp, UIntPtr.Zero);

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

            PortRunCombatTask("Demon Hunter loop", () => PortDemonHunterLoop(token));
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

                PortDemonHunterShiftLeftClick();
                Thread.Sleep(150);
            }

            AddWorkflowStep("Demon Hunter momentum build finished or timed out");
        }

        private void PortDemonHunterShiftLeftClick()
        {
            bool clickSafe = PortCombatClickIsSafe();
            PortLogCombatClickDecision(
                "Demon Hunter Shift+Left Click",
                clickSafe,
                "left",
                ref portLastDemonHunterDecisionLogTicks,
                ref portLastDemonHunterDecisionAllowed);
            if (!clickSafe)
            {
                AppLogger.Info($"Demon Hunter Shift+Left Click safety check failed, but existing rotation still sends the click; {PortCombatInputContext()}");
            }

            keybd_event(PortVkShift, 0, 0, UIntPtr.Zero);
            Thread.Sleep(30);

            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
            Thread.Sleep(30);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);

            keybd_event(PortVkShift, 0, PortKeyUp, UIntPtr.Zero);
        }

        private void PortDemonHunterLoop(CancellationToken token)
        {
            int[] sequence = [0x31, 0x32, 0x33, 0x34];
            int index = 0;
            bool rightDown = false;

            try
            {
                while (!token.IsCancellationRequested && portCombatRunning && portCombatClass == "demon_hunter")
                {
                    bool clickSafe = PortCombatClickIsSafe();
                    PortLogCombatClickDecision(
                        "Demon Hunter right hold",
                        clickSafe,
                        "right",
                        ref portLastDemonHunterDecisionLogTicks,
                        ref portLastDemonHunterDecisionAllowed);

                    if (!clickSafe)
                    {
                        if (rightDown)
                        {
                            AppLogger.Info($"Combat right click blocked: PortCombatClickIsSafe=false while rightDown=true; no new RIGHTDOWN sent; {PortCombatInputContext()}");
                        }
                        PortSleep(token, 90);
                        continue;
                    }

                    if (!rightDown)
                    {
                        AppLogger.Info($"Combat right click allowed: sending RIGHTDOWN; {PortCombatInputContext()}");
                        mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, UIntPtr.Zero);
                        rightDown = true;
                    }

                    PortPressKey(sequence[index]);
                    index = (index + 1) % sequence.Length;

                    // Image recognition is the source of truth for Momentum.
                    if (!PortDemonHunterMomentumReady())
                    {
                        AddWorkflowStep("Demon Hunter momentum not detected; sending Shift+Left Click");

                        PortDemonHunterShiftLeftClick();

                        PortSleep(token, 100);
                    }
                    else
                    {
                        PortSleep(token, 75);
                    }
                }
            }
            finally
            {
                if (rightDown)
                {
                    AppLogger.Info($"Combat right click cleanup: sending RIGHTUP; {PortCombatInputContext()}");
                    mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero);
                }

                keybd_event(PortVkShift, 0, PortKeyUp, UIntPtr.Zero);
            }
        }

        private void PortCombatCursorLoop(CancellationToken token)
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
                    mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                }
                else
                {
                    mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                    Thread.Sleep(10);
                    mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                }

                PortSleep(token, 50);
            }

            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
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
                    (double left, double top, double regionWidth, double regionHeight) = portCombatNoClickRegions[i];
                    Rectangle region = new(
                        rect.Left + (int)Math.Round(width * left),
                        rect.Top + (int)Math.Round(height * top),
                        (int)Math.Round(width * regionWidth),
                        (int)Math.Round(height * regionHeight));

                    if (region.Contains(cursor))
                    {
                        insideNoClickRegion = true;
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
                ? $"true(index={diagnostics.NoClickRegionIndex}, rect={diagnostics.NoClickRegion.Left},{diagnostics.NoClickRegion.Top},{diagnostics.NoClickRegion.Right},{diagnostics.NoClickRegion.Bottom})"
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
            AppLogger.Info($"{source}: {button} click {(allowed ? "allowed" : "blocked")}; {PortCombatInputContext()}");
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
            int NoClickRegionIndex,
            Rectangle NoClickRegion,
            bool HasCursorInfo,
            IntPtr CursorHandle,
            int CursorFlags);
    }
}
