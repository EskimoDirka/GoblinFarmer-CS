using System;
using System.Collections.Generic;
using System.Diagnostics;
using DrawingPoint = System.Drawing.Point;

namespace GoblinFarmer
{
    public partial class frmMain
    {
        private bool PortRepairGearFromOpenBlacksmith(CancellationToken token, bool closeAfterRepair)
        {
            PortSafeLeftClick(PortScaleGamePoint(portRepairCoords.GetValueOrDefault("Repair Tab", new DrawingPoint(677, 815))));
            if (!PortWaitForImageInDiablo(Img("Repair", "Repair Menu.png"), token, 20000, PortVendorUiConfidence))
            {
                return false;
            }

            PortSafeLeftClick(PortScaleGamePoint(portRepairCoords.GetValueOrDefault("Repair Button", new DrawingPoint(361, 715))));
            PortSleep(token, 350);

            if (closeAfterRepair)
            {
                PortPressEscapeForAutomation();
                PortSleep(token, 350);
            }

            return true;
        }

        private bool PortRepairGear(CancellationToken token)
        {
            return PortRunRepairFlow(token);
        }

        private bool PortRunRepairFlow(CancellationToken token)
        {
            AddWorkflowStep("Starting repair flow");
            AddWorkflowStep("Repairing");
            PortSetAppStatus("Repairing Gear");

            PortWaitForRepairStationReady(token);

            if (!PortOpenBlacksmithMenu(token))
            {
                return PortWorkflowFailed("Opening blacksmith menu");
            }

            if (!PortRepairGearFromOpenBlacksmith(token, closeAfterRepair: false))
            {
                PortCaptureFailureScreenshot("RepairFailed");
                return PortWorkflowFailed("Repairing");
            }

            AddWorkflowStep("Checking inventory for salvage");
            if (!PortSalvageInventoryFromOpenBlacksmith(token, closeAfterSalvage: true))
            {
                return PortWorkflowFailed("Salvaging");
            }

            AddWorkflowStep("Repair flow completed");
            return true;
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
            return true;
        }

        private bool PortSalvageInventory(CancellationToken token)
        {
            AddWorkflowStep("Salvaging");
            PortSetAppStatus("Salvaging Inventory");

            if (!PortOpenBlacksmithMenu(token))
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

        private bool PortOpenBlacksmithMenu(CancellationToken token)
        {
            if (!ActivateDiabloWindow())
            {
                return false;
            }

            if (PortImageVisibleInDiablo(Img("Repair", "Blacksmith Menu.png"), PortVendorUiConfidence))
            {
                AppLogger.Info("Repair station wait/click timing: blacksmith menu already visible before repair station click");
                return true;
            }

            DrawingPoint repairStation = portRepairCoords.GetValueOrDefault("Repair Station", new DrawingPoint(1841, 198));
            Stopwatch sw = Stopwatch.StartNew();
            int attempts = 0;
            while (sw.ElapsedMilliseconds < 20000)
            {
                if (token.IsCancellationRequested)
                {
                    return false;
                }
                if (PortImageVisibleInDiablo(Img("Repair", "Blacksmith Menu.png"), PortVendorUiConfidence))
                {
                    AppLogger.Info($"Repair station wait/click timing: blacksmith menu visible after {attempts} clicks in {sw.ElapsedMilliseconds}ms");
                    return true;
                }

                attempts++;
                AppLogger.Info($"Repair station wait/click timing: click attempt {attempts} at {repairStation.X},{repairStation.Y}; elapsed={sw.ElapsedMilliseconds}ms");
                PortSafeLeftClick(PortScaleGamePoint(repairStation));
                PortSleep(token, 2000);
            }

            AppLogger.Info($"Repair station wait/click timing: blacksmith menu not visible after {attempts} clicks in {sw.ElapsedMilliseconds}ms");
            PortCaptureFailureScreenshot("RepairStationNotFound");
            return false;
        }

        private void PortWaitForRepairStationReady(CancellationToken token)
        {
            if (PortVendorPanelVisible())
            {
                AppLogger.Info("Repair station wait/click timing: vendor panel already visible; skipping town stability wait");
                return;
            }

            Stopwatch sw = Stopwatch.StartNew();
            AppLogger.Info("Repair station wait/click timing: waiting for New Tristram/repair station readiness before first click");
            while (sw.ElapsedMilliseconds < 8000)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                if (PortVendorPanelVisible())
                {
                    AppLogger.Info($"Repair station wait/click timing: vendor panel became visible after {sw.ElapsedMilliseconds}ms");
                    return;
                }

                string currentLocation = PortDetectSpecificLocation("New Tristram");
                if (!string.IsNullOrWhiteSpace(currentLocation))
                {
                    AppLogger.Info($"Repair station wait/click timing: New Tristram confirmed as {currentLocation} after {sw.ElapsedMilliseconds}ms; settling before blacksmith click");
                    PortSleep(token, 700);
                    return;
                }

                PortSleep(token, 250);
            }

            AppLogger.Info("Repair station wait/click timing: readiness wait timed out; proceeding with repair station coordinate fallback");
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
