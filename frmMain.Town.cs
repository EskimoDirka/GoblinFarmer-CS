using System;
using System.Collections.Generic;
using System.Diagnostics;
using DrawingPoint = System.Drawing.Point;

namespace GoblinFarmer
{
    public partial class frmMain
    {
        private const int PortRepairStationClickAttemptTimeoutMs = 1500;
        private const int PortRepairWorkflowTimeoutMs = 20000;

        private sealed record PortRepairReadinessResult(bool VendorPanelAlreadyVisible, bool VendorPanelBecameVisible, bool NewTristramConfirmed, long ReadinessElapsedMs, long PostArrivalWaitMs);
        private sealed record PortBlacksmithOpenResult(bool Opened, int Attempts, long ElapsedMs, long WorkflowElapsedMs);

        private bool PortRepairGearFromOpenBlacksmith(CancellationToken token, bool closeAfterRepair, Stopwatch repairWorkflow, out long repairMenuOpenedWorkflowElapsedMs)
        {
            repairMenuOpenedWorkflowElapsedMs = -1;
            Stopwatch repairPerf = Stopwatch.StartNew();
            DrawingPoint repairTabReference = portRepairCoords.GetValueOrDefault("Repair Tab", new DrawingPoint(677, 815));
            DrawingPoint repairTabPoint = PortScaleGamePoint(repairTabReference);
            bool repairTabClickSent = PortSafeLeftClick(repairTabPoint);
            AppLogger.Info($"Repair menu timing: repair tab click sent={repairTabClickSent}; reference={repairTabReference.X},{repairTabReference.Y}; screen={repairTabPoint.X},{repairTabPoint.Y}; elapsed={repairPerf.ElapsedMilliseconds}ms");
            if (!PortWaitForImageInDiablo(Img("Repair", "Repair Menu.png"), token, 20000, PortVendorUiConfidence))
            {
                AppLogger.Info($"Repair menu timing: repair menu not visible after repair tab click; elapsed={repairPerf.ElapsedMilliseconds}ms");
                return false;
            }
            repairMenuOpenedWorkflowElapsedMs = repairWorkflow.ElapsedMilliseconds;
            AppLogger.Info($"Repair menu timing: repair menu visible after repair tab click; elapsed={repairPerf.ElapsedMilliseconds}ms");
            AppLogger.Info($"Repair workflow timing: timeUntilRepairMenuOpenedMs={repairMenuOpenedWorkflowElapsedMs}; repairTabElapsedMs={repairPerf.ElapsedMilliseconds}");

            DrawingPoint repairButtonReference = portRepairCoords.GetValueOrDefault("Repair Button", new DrawingPoint(361, 715));
            DrawingPoint repairButtonPoint = PortScaleGamePoint(repairButtonReference);
            bool repairButtonClickSent = PortSafeLeftClick(repairButtonPoint);
            AppLogger.Info($"Repair menu timing: repair button click sent={repairButtonClickSent}; reference={repairButtonReference.X},{repairButtonReference.Y}; screen={repairButtonPoint.X},{repairButtonPoint.Y}; elapsed={repairPerf.ElapsedMilliseconds}ms");
            PortSleep(token, 350);

            if (closeAfterRepair)
            {
                PortPressEscapeForAutomation();
                PortSleep(token, 350);
            }

            PortCaptureSuccessScreenshot("Repair", "RepairComplete");
            return true;
        }

        private bool PortRepairGear(CancellationToken token)
        {
            return PortRunRepairFlow(token);
        }

        private bool PortRunRepairFlow(CancellationToken token)
        {
            Stopwatch repairWorkflow = Stopwatch.StartNew();
            PortRepairReadinessResult readiness = new(false, false, false, 0, 0);
            PortBlacksmithOpenResult blacksmithOpen = new(false, 0, 0, -1);
            long repairMenuOpenedWorkflowElapsedMs = -1;
            bool completed = false;

            AddWorkflowStep("Starting repair flow");
            AddWorkflowStep("Repairing");
            PortSetAppStatus("Repairing Gear");

            try
            {
                readiness = PortWaitForRepairStationReady(token);
                AppLogger.Info($"Repair workflow timing: waitAfterArrivalMs={readiness.PostArrivalWaitMs}; readinessElapsedMs={readiness.ReadinessElapsedMs}; newTristramConfirmed={readiness.NewTristramConfirmed}; vendorPanelAlreadyVisible={readiness.VendorPanelAlreadyVisible}; vendorPanelBecameVisible={readiness.VendorPanelBecameVisible}; totalElapsedMs={repairWorkflow.ElapsedMilliseconds}");

                blacksmithOpen = PortOpenBlacksmithMenu(token, repairWorkflow);
                if (!blacksmithOpen.Opened)
                {
                    return PortWorkflowFailed("Opening blacksmith menu");
                }

                if (!PortRepairGearFromOpenBlacksmith(token, closeAfterRepair: false, repairWorkflow, out repairMenuOpenedWorkflowElapsedMs))
                {
                    PortCaptureFailureScreenshot("RepairFailed", "Repair");
                    return PortWorkflowFailed("Repairing");
                }

                AddWorkflowStep("Checking inventory for salvage");
                if (!PortSalvageInventoryFromOpenBlacksmith(token, closeAfterSalvage: true))
                {
                    return PortWorkflowFailed("Salvaging");
                }

                AddWorkflowStep("Repair flow completed");
                completed = true;
                return true;
            }
            finally
            {
                AppLogger.Info($"Repair workflow timing: totalRepairWorkflowDurationMs={repairWorkflow.ElapsedMilliseconds}; completed={completed}; waitAfterArrivalMs={readiness.PostArrivalWaitMs}; timeUntilRepairStationDetectedMs={(blacksmithOpen.Opened ? blacksmithOpen.WorkflowElapsedMs.ToString() : "Unknown")}; timeUntilRepairMenuOpenedMs={(repairMenuOpenedWorkflowElapsedMs >= 0 ? repairMenuOpenedWorkflowElapsedMs.ToString() : "Unknown")}; blacksmithAttempts={blacksmithOpen.Attempts}");
            }
        }

        private bool PortSalvageInventoryFromOpenBlacksmith(CancellationToken token, bool closeAfterSalvage)
        {
            DrawingPoint? firstSlot = PortFirstFilledInventorySlot();
            if (firstSlot == null)
            {
                AddWorkflowStep("First filled inventory slot not found");
                AddWorkflowStep("Salvage skipped: no filled inventory slots found.");
                if (closeAfterSalvage)
                {
                    PortPressEscapeForAutomation();
                    PortSleep(token, 350);
                }

                PortCaptureSuccessScreenshot("Salvage", "SalvageSkippedNoFilledSlots");
                return true;
            }

            AddWorkflowStep("Salvaging");
            AddWorkflowStep($"First filled inventory slot found at {firstSlot.Value.X},{firstSlot.Value.Y}");
            PortSafeLeftClick(PortScaleGamePoint(portSalvageCoords.GetValueOrDefault("Salvage Tab", new DrawingPoint(683, 638))));
            if (!PortWaitForImageInDiablo(Img("Salvage", "Salvage Button.png"), token, 20000, PortVendorUiConfidence))
            {
                return false;
            }

            AddWorkflowStep("Inventory open requested: skipped");
            AddWorkflowStep("Inventory open confirmed via vendor/salvage UI");
            AddWorkflowStep("Inventory not required because vendor/salvage UI is open");
            PortSafeLeftClick(PortScaleGamePoint(portSalvageCoords.GetValueOrDefault("Salvage Button", new DrawingPoint(215, 382))));
            PortSleep(token, 150);

            int salvagedCount = 0;
            DrawingPoint? slot = firstSlot;
            for (int i = 0; i < 60; i++)
            {
                if (token.IsCancellationRequested)
                {
                    return false;
                }

                if (slot == null)
                {
                    AddWorkflowStep("No more filled inventory slots found");
                    break;
                }

                PortSafeLeftClick(slot.Value);
                salvagedCount++;
                if (PortWaitForImageInDiablo(Img("Salvage", "Salvage Confirmation Button.png"), token, 800, PortVendorUiConfidence))
                {
                    PortPressKey(PortVkReturn);
                }

                PortSleep(token, 100);
                slot = PortFirstFilledInventorySlot();
            }

            if (closeAfterSalvage)
            {
                PortPressEscapeForAutomation();
                PortSleep(token, 350);
            }

            AddWorkflowStep(salvagedCount == 0 ? "Salvage skipped: no filled inventory slots found." : $"Salvage completed: {salvagedCount} slots clicked");
            PortCaptureSuccessScreenshot("Salvage", "SalvageComplete");
            return true;
        }

        private bool PortSalvageInventory(CancellationToken token)
        {
            AddWorkflowStep("Salvaging");
            PortSetAppStatus("Salvaging Inventory");

            if (!PortOpenBlacksmithMenu(token, Stopwatch.StartNew()).Opened)
            {
                return false;
            }

            return PortSalvageInventoryFromOpenBlacksmith(token, closeAfterSalvage: true);
        }

        /// <summary>
        /// Handles the blacksmith repair and salvage prep sequence while in town.
        /// </summary>
        private bool PortTownPrepAtBlacksmith(CancellationToken token)
        {
            AddWorkflowStep("Starting town prep at blacksmith");
            return PortRunRepairFlow(token);
        }

        private PortBlacksmithOpenResult PortOpenBlacksmithMenu(CancellationToken token, Stopwatch repairWorkflow)
        {
            if (!ActivateDiabloWindow())
            {
                return new(false, 0, 0, -1);
            }

            Stopwatch sw = Stopwatch.StartNew();
            if (PortImageVisibleInDiablo(Img("Repair", "Blacksmith Menu.png"), PortVendorUiConfidence))
            {
                AppLogger.Info("Repair station wait/click timing: blacksmith menu already visible before repair station click");
                long workflowElapsed = repairWorkflow.ElapsedMilliseconds;
                AppLogger.Info($"Repair workflow timing: timeUntilRepairStationDetectedMs={workflowElapsed}; blacksmithOpenElapsedMs={sw.ElapsedMilliseconds}; blacksmithAttempts=0; alreadyVisible=True");
                return new(true, 0, sw.ElapsedMilliseconds, workflowElapsed);
            }

            DrawingPoint repairStation = portRepairCoords.GetValueOrDefault("Repair Station", new DrawingPoint(1841, 198));
            int attempts = 0;
            while (sw.ElapsedMilliseconds < PortRepairWorkflowTimeoutMs)
            {
                if (token.IsCancellationRequested)
                {
                    return new(false, attempts, sw.ElapsedMilliseconds, -1);
                }
                if (PortImageVisibleInDiablo(Img("Repair", "Blacksmith Menu.png"), PortVendorUiConfidence))
                {
                    AppLogger.Info($"Repair station wait/click timing: blacksmith menu visible after {attempts} clicks in {sw.ElapsedMilliseconds}ms");
                    long workflowElapsed = repairWorkflow.ElapsedMilliseconds;
                    AppLogger.Info($"Repair workflow timing: timeUntilRepairStationDetectedMs={workflowElapsed}; blacksmithOpenElapsedMs={sw.ElapsedMilliseconds}; blacksmithAttempts={attempts}; alreadyVisible=False");
                    return new(true, attempts, sw.ElapsedMilliseconds, workflowElapsed);
                }

                attempts++;
                DrawingPoint clickPoint = PortScaleGamePoint(repairStation);
                AppLogger.Info($"Repair station wait/click timing: click attempt {attempts} at reference={repairStation.X},{repairStation.Y}; screen={clickPoint.X},{clickPoint.Y}; elapsed={sw.ElapsedMilliseconds}ms; menuVisibleBeforeClick=False");
                bool clickSent = PortSafeLeftClick(clickPoint);
                AppLogger.Info($"Repair station wait/click timing: click attempt {attempts} sent={clickSent}; elapsedAfterClick={sw.ElapsedMilliseconds}ms");

                long waitStart = sw.ElapsedMilliseconds;
                bool menuVisibleAfterClick = PortWaitForImageInDiablo(
                    Img("Repair", "Blacksmith Menu.png"),
                    token,
                    PortRepairStationClickAttemptTimeoutMs,
                    PortVendorUiConfidence);
                long waitedMs = sw.ElapsedMilliseconds - waitStart;
                AppLogger.Info($"Repair station wait/click timing: click attempt {attempts} menuVisibleAfterClick={menuVisibleAfterClick}; visualWaitMs={waitedMs}; elapsedAfterWait={sw.ElapsedMilliseconds}ms");
                if (menuVisibleAfterClick)
                {
                    AppLogger.Info($"Repair station wait/click timing: blacksmith menu visible after {attempts} clicks in {sw.ElapsedMilliseconds}ms");
                    long workflowElapsed = repairWorkflow.ElapsedMilliseconds;
                    AppLogger.Info($"Repair workflow timing: timeUntilRepairStationDetectedMs={workflowElapsed}; blacksmithOpenElapsedMs={sw.ElapsedMilliseconds}; blacksmithAttempts={attempts}; alreadyVisible=False");
                    return new(true, attempts, sw.ElapsedMilliseconds, workflowElapsed);
                }
            }

            AppLogger.Info($"Repair station wait/click timing: blacksmith menu not visible after {attempts} clicks in {sw.ElapsedMilliseconds}ms");
            PortCaptureFailureScreenshot("RepairStationNotFound", "Repair");
            return new(false, attempts, sw.ElapsedMilliseconds, -1);
        }

        private PortRepairReadinessResult PortWaitForRepairStationReady(CancellationToken token)
        {
            if (PortVendorPanelVisible())
            {
                AppLogger.Info("Repair station wait/click timing: vendor panel already visible; skipping town stability wait");
                return new(true, false, false, 0, 0);
            }

            Stopwatch sw = Stopwatch.StartNew();
            AppLogger.Info("Repair station wait/click timing: waiting for New Tristram/repair station readiness before first click");
            while (sw.ElapsedMilliseconds < 8000)
            {
                if (token.IsCancellationRequested)
                {
                    return new(false, false, false, sw.ElapsedMilliseconds, 0);
                }

                if (PortVendorPanelVisible())
                {
                    AppLogger.Info($"Repair station wait/click timing: vendor panel became visible after {sw.ElapsedMilliseconds}ms");
                    return new(false, true, false, sw.ElapsedMilliseconds, 0);
                }

                string currentLocation = PortDetectSpecificLocation("New Tristram");
                if (!string.IsNullOrWhiteSpace(currentLocation))
                {
                    long confirmedAt = sw.ElapsedMilliseconds;
                    int postArrivalSettleMs = AppSettings.Repair.PostArrivalSettleDelayMs;
                    AppLogger.Info($"Repair station wait/click timing: New Tristram confirmed as {currentLocation} after {confirmedAt}ms; minimal input settle before blacksmith click={postArrivalSettleMs}ms");
                    PortSleep(token, postArrivalSettleMs);
                    long postArrivalWaitMs = sw.ElapsedMilliseconds - confirmedAt;
                    AppLogger.Info($"Repair station wait/click timing: post-arrival settle complete; waitAfterArrivalMs={postArrivalWaitMs}; elapsed={sw.ElapsedMilliseconds}ms");
                    return new(false, false, true, sw.ElapsedMilliseconds, postArrivalWaitMs);
                }

                PortSleep(token, 250);
            }

            AppLogger.Info("Repair station wait/click timing: readiness wait timed out; proceeding with repair station coordinate fallback");
            return new(false, false, false, sw.ElapsedMilliseconds, 0);
        }

        private bool PortVendorPanelVisible()
        {
            return PortImageVisibleInDiablo(Img("Repair", "Blacksmith Menu.png"), PortVendorUiConfidence) ||
                PortImageVisibleInDiablo(Img("Repair", "Repair Menu.png"), PortVendorUiConfidence) ||
                PortImageVisibleInDiablo(Img("Salvage", "Salvage Button.png"), PortVendorUiConfidence) ||
                PortImageVisibleInDiablo(Img("Salvage", "Salvage Tab.png"), PortVendorUiConfidence);
        }
    }
}
