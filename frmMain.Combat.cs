using System;
using System.Collections.Generic;
using System.Diagnostics;
using DrawingPoint = System.Drawing.Point;
using System.Text;

namespace GoblinFarmer
{
    public partial class frmMain
    {
        private void PortToggleCombat()
        {
            if (portCombatRunning)
            {
                PortStopCombat("hotkey");
                return;
            }

            if (isAutomationRunning || !PortDiabloIsActive())
            {
                return;
            }

            portCombatCts = new CancellationTokenSource();
            portCombatRunning = true;
            portMonkKeyIndex = 1;
            portCombatClass = radWD.Checked ? "witch_doctor" : radDH.Checked ? "demon_hunter" : "monk";
            SetCombatStatus($"{radWD.Checked switch { true => "Witch Doctor", false when radDH.Checked => "Demon Hunter", _ => "Monk" }} Running");
            AddWorkflowStep("Combat started");
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

            if (stoppedClass == "witch_doctor" && !PortWitchDoctorHexReady())
            {
                AddWorkflowStep("Witch Doctor not in Hex ready state; pressing 1 before stopping");
                PortPressKey(0x31);
                Thread.Sleep(150);
            }

            portCombatRunning = false;
            portCombatCts?.Cancel();
            portCombatCts?.Dispose();
            portCombatCts = null;
            portCombatClass = "";
            PortReleaseInputs();
            ClipCursor(IntPtr.Zero);
            SetCombatStatus("Idle");
            SetAppStatus($"Combat Stopped ({reason})");
            AddWorkflowStep($"Combat stopped ({reason})");
            return true;
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
            while (!token.IsCancellationRequested && portCombatRunning && portCombatClass == "monk")
            {
                if (!PortDiabloIsActive())
                {
                    BeginInvoke(new Action(() => PortStopCombat("Diablo inactive")));
                    return;
                }

                int vk = 0x30 + portMonkKeyIndex;

                keybd_event((byte)vk, 0, 0, UIntPtr.Zero);
                Thread.Sleep(10);
                keybd_event((byte)vk, 0, PortKeyUp, UIntPtr.Zero);

                portMonkKeyIndex = portMonkKeyIndex >= 3 ? 1 : portMonkKeyIndex + 1;

                Thread.Sleep(50);
            }
        }

        // Witch Doctor combat loop =================================
        private void PortWitchDoctorLoop(CancellationToken token)
        {
            AddWorkflowStep("Witch Doctor opening rotation");

            PortPressKey(0x32); // 2
            PortSleep(token, 50);

            PortPressKey(0x33); // 3
            PortSleep(token, 50);

            PortPressKey(0x31); // 1

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

                    PortPressKey(0x32); // 2
                    PortSleep(token, 50);

                    PortPressKey(0x33); // 3
                    PortSleep(token, 50);

                    PortPressKey(0x31); // 1

                    PortSleep(token, 500);
                }
                else
                {
                    PortSleep(token, 100);
                }
            }
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

            AppLogger.Info($"Witch Doctor Hex Ready confidence={confidence:0.000}");

            return confidence >= 0.55;
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
                    if (!PortCombatClickIsSafe())
                    {
                        PortSleep(token, 90);
                        continue;
                    }

                    if (!rightDown)
                    {
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

                if (PortCombatClickIsSafe())
                {
                    mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                    Thread.Sleep(25);
                    mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                }

                PortSleep(token, 120);
            }
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
            if (!PortTryGetDiabloRect(out RECT rect) || !GetCursorPos(out DrawingPoint cursor))
            {
                return false;
            }

            if (cursor.X < rect.Left || cursor.X >= rect.Right || cursor.Y < rect.Top || cursor.Y >= rect.Bottom)
            {
                return false;
            }

            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;
            foreach ((double left, double top, double regionWidth, double regionHeight) in portCombatNoClickRegions)
            {
                Rectangle region = new(
                    rect.Left + (int)Math.Round(width * left),
                    rect.Top + (int)Math.Round(height * top),
                    (int)Math.Round(width * regionWidth),
                    (int)Math.Round(height * regionHeight));

                if (region.Contains(cursor))
                {
                    return false;
                }
            }

            return true;
        }

        private void PortLockCursorToDiablo()
        {
            if (PortTryGetDiabloRect(out RECT rect))
            {
                ClipCursor(ref rect);
            }
        }
    }
}
