using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using DrawingPoint = System.Drawing.Point;

namespace GoblinFarmer
{
    public partial class frmMain
    {
        private const int PortRepairStationClickAttemptTimeoutMs = 1500;
        private const int PortRepairWorkflowTimeoutMs = 20000;
        private const int PortSalvageConfirmationTimeoutMs = 100;
        private const int PortSalvageConfirmationFastAttempts = 3;
        private const int PortSalvageConfirmationFastDelayMs = 30;
        private const int PortSalvageExpectedConfirmationTimeoutMs = 900;
        private const int PortSalvageExpectedConfirmationAttempts = 9;
        private const int PortSalvageExpectedConfirmationDelayMs = 75;
        private const int PortSalvageExpectedConfirmationRetryDelayMs = 100;
        private const int PortSalvagePostSlotDelayMs = 35;
        private const int PortSalvageSlotClickSettleMs = 35;
        private const int PortSalvageSlotClickHoldMs = 20;
        private const int PortBulkSalvagePromptTimeoutMs = 900;
        private const int PortBulkSalvagePromptClearTimeoutMs = 900;
        private const int PortBulkSalvageSettleMs = 250;
        private const int PortBulkSalvageButtonSampleSize = 44;
        private const int PortBulkSalvageActivePixelThreshold = 90;

        private sealed record PortRepairReadinessResult(bool VendorPanelAlreadyVisible, bool VendorPanelBecameVisible, bool NewTristramConfirmed, long ReadinessElapsedMs, long PostArrivalWaitMs);
        private sealed record PortBlacksmithOpenResult(bool Opened, int Attempts, long ElapsedMs, long WorkflowElapsedMs);
        private sealed record PortSalvageBulkCategory(string Name, string CoordinateName, Func<Color, bool> ActiveColorPredicate);
        private sealed record PortBulkSalvageColorSample(bool Active, int ActivePixels, int SampledPixels, double ActiveRatio);
        private sealed record PortBulkSalvageResult(string Category, string Outcome, bool ClickSent, bool PromptFound, bool EnterSent, bool PromptCleared, PortBulkSalvageColorSample ColorSample, long ElapsedMs);

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
                    DebugManager.Session.RecordRepairFailure("Repair failed: repair menu or button did not complete");
                    PortCaptureFailureScreenshot("RepairFailed", "Repair");
                    return PortWorkflowFailed("Repairing");
                }

                AddWorkflowStep("Checking inventory for salvage");
                if (!PortSalvageInventoryFromOpenBlacksmith(token, closeAfterSalvage: true))
                {
                    DebugManager.Session.RecordSalvageFailure("Salvage failed during repair flow");
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
            AddWorkflowStep("Salvaging");
            PortSafeLeftClick(PortScaleGamePoint(portSalvageCoords.GetValueOrDefault("Salvage Tab", new DrawingPoint(683, 638))));
            if (!PortWaitForImageInDiablo(Img("Salvage", "Salvage Button.png"), token, 20000, PortVendorUiConfidence))
            {
                DebugManager.Session.RecordSalvageFailure("Salvage failed: Salvage button not visible");
                return false;
            }

            AddWorkflowStep("Inventory open requested: skipped");
            AddWorkflowStep("Inventory open confirmed via vendor/salvage UI");
            AddWorkflowStep("Inventory not required because vendor/salvage UI is open");

            if (AppSettings.Repair.EnableBulkCategorySalvage)
            {
                return PortBulkSalvageCategoriesAndLeftovers(token, closeAfterSalvage);
            }

            return PortSalvageInventoryTargetsFromSingleScan(token, closeAfterSalvage, "PerSlotOnly", Stopwatch.StartNew());
        }

        private bool PortBulkSalvageCategoriesAndLeftovers(CancellationToken token, bool closeAfterSalvage)
        {
            AddWorkflowStep("Detecting bulk salvage categories");
            Stopwatch salvagePerf = Stopwatch.StartNew();
            List<PortBulkSalvageResult> bulkResults = [];
            foreach (PortSalvageBulkCategory category in PortBulkSalvageCategories())
            {
                if (token.IsCancellationRequested)
                {
                    return false;
                }

                bulkResults.Add(PortTryBulkSalvageCategory(category, token));
            }

            AppLogger.Info(
                "Bulk salvage summary: " +
                $"enabled=True; " +
                $"categoriesAttempted={bulkResults.Count}; " +
                $"categoriesConfirmed={bulkResults.Count(result => result.Outcome.Equals("Confirmed", StringComparison.OrdinalIgnoreCase))}; " +
                $"inactiveCategories={bulkResults.Count(result => result.Outcome.Equals("InactiveCategoryButton", StringComparison.OrdinalIgnoreCase))}; " +
                $"promptFailures={bulkResults.Count(result => result.Outcome.Equals("PromptMissingAfterActiveClick", StringComparison.OrdinalIgnoreCase) || result.Outcome.Equals("PromptDidNotClear", StringComparison.OrdinalIgnoreCase))}; " +
                $"unsafeClicks={bulkResults.Count(result => result.Outcome.Equals("UnsafeClick", StringComparison.OrdinalIgnoreCase))}; " +
                $"elapsedMs={salvagePerf.ElapsedMilliseconds}");

            if (bulkResults.Any(result =>
                result.Outcome.Equals("PromptMissingAfterActiveClick", StringComparison.OrdinalIgnoreCase) ||
                result.Outcome.Equals("PromptDidNotClear", StringComparison.OrdinalIgnoreCase) ||
                result.Outcome.Equals("UnsafeClick", StringComparison.OrdinalIgnoreCase)))
            {
                DebugManager.Session.RecordSalvageFailure("Salvage failed: bulk category salvage did not complete safely");
                PortCaptureFailureScreenshot("BulkSalvageCategoryFailed", "Salvage");
                return false;
            }

            AddWorkflowStep("Scanning salvage leftovers");
            return PortSalvageInventoryTargetsFromSingleScan(token, closeAfterSalvage, "PostBulkLeftoverScan", salvagePerf);
        }

        private bool PortSalvageInventoryTargetsFromSingleScan(CancellationToken token, bool closeAfterSalvage, string phase, Stopwatch salvagePerf)
        {
            Stopwatch inventoryScanPerf = Stopwatch.StartNew();
            List<SalvageInventorySlotTarget> cachedSlots = PortFilledInventorySlots();
            inventoryScanPerf.Stop();
            if (cachedSlots.Count == 0)
            {
                AddWorkflowStep("First filled inventory slot not found");
                AddWorkflowStep("Salvage skipped: no filled inventory slots found.");
                AppLogger.Info($"Salvage inventory scan: phase={PortLogField(phase)}; cachedSlotCount=0; regularGemSkips=0; retainedRegularGemCount={portLastRegularGemCandidateCount}; inventoryScanMs={inventoryScanPerf.ElapsedMilliseconds}; cacheMode=SingleInventoryScan");
                if (closeAfterSalvage)
                {
                    PortPressEscapeForAutomation();
                    PortSleep(token, 350);
                }

                PortCaptureSuccessScreenshot("Salvage", "SalvageSkippedNoFilledSlots");
                return true;
            }

            AddWorkflowStep($"Cached salvage slots: {cachedSlots.Count}");
            AppLogger.Info($"Salvage inventory scan: phase={PortLogField(phase)}; cachedSlotCount={cachedSlots.Count}; retainedRegularGemCount={portLastRegularGemCandidateCount}; inventoryScanMs={inventoryScanPerf.ElapsedMilliseconds}; cacheMode=SingleInventoryScan");
            bool salvageButtonClickSent = PortSafeLeftClick(PortScaleGamePoint(portSalvageCoords.GetValueOrDefault("Salvage Button", new DrawingPoint(215, 382))));
            AppLogger.Info($"Salvage mode selected: phase={PortLogField(phase)}; salvageButtonClickSent={salvageButtonClickSent}");
            PortSleep(token, 150);

            int cachedSlotAttempts = 0;
            int slotsClicked = 0;
            int confirmedSalvages = 0;
            int noPromptSalvages = 0;
            int confirmationMisses = 0;
            int expectedConfirmationMisses = 0;
            int regularGemSkips = 0;
            for (int i = 0; i < cachedSlots.Count && i < 60; i++)
            {
                if (token.IsCancellationRequested)
                {
                    return false;
                }

                Stopwatch slotPerf = Stopwatch.StartNew();
                SalvageInventorySlotTarget target = cachedSlots[i];
                if (target.Quality.Equals("RegularGem", StringComparison.OrdinalIgnoreCase))
                {
                    regularGemSkips++;
                    cachedSlotAttempts++;
                    AppLogger.Info($"Salvage timing: slotIndex={cachedSlotAttempts}; cachedSlotIndex={i + 1}; cachedSlotCount={cachedSlots.Count}; row={target.Row}; column={target.Column}; screenPoint={FormatPoint(target.ScreenPoint)}; footprintRows={target.FootprintRows}; quality={PortLogField(target.Quality)}; confirmationExpected={target.ConfirmationExpected}; slotClickSent=False; retryAttempted=False; retryClickSent=False; confirmationFound=False; enterSent=False; outcome=RegularGemSkipped; confirmationWaitMs=0; confirmationScans=0; nextSlotScanMs=0; cacheMode=SingleInventoryScan; slotElapsedMs={slotPerf.ElapsedMilliseconds}; totalSalvageElapsedMs={salvagePerf.ElapsedMilliseconds}");
                    continue;
                }

                bool slotClickSent = PortSafeSalvageSlotClick(target.ScreenPoint);
                cachedSlotAttempts++;
                if (slotClickSent)
                {
                    slotsClicked++;
                }

                bool confirmationFound = PortWaitForSalvageConfirmation(token, target.ConfirmationExpected, out long confirmationWaitMs, out int confirmationScans);
                bool retryAttempted = false;
                bool retryClickSent = false;
                if (!confirmationFound && target.ConfirmationExpected && slotClickSent && !token.IsCancellationRequested)
                {
                    retryAttempted = true;
                    PortSleep(token, PortSalvageExpectedConfirmationRetryDelayMs);
                    retryClickSent = PortSafeSalvageSlotClick(target.ScreenPoint);
                    if (retryClickSent)
                    {
                        slotsClicked++;
                    }

                    bool retryConfirmationFound = PortWaitForSalvageConfirmationExpected(token, out long retryConfirmationWaitMs, out int retryConfirmationScans);
                    confirmationWaitMs += retryConfirmationWaitMs;
                    confirmationScans += retryConfirmationScans;
                    confirmationFound = retryConfirmationFound;
                }

                bool enterSent = false;
                string outcome;
                if (confirmationFound)
                {
                    PortPressKey(PortVkReturn);
                    enterSent = true;
                    confirmedSalvages++;
                    outcome = "Confirmed";
                }
                else
                {
                    confirmationMisses++;
                    if (target.ConfirmationExpected)
                    {
                        expectedConfirmationMisses++;
                        outcome = "ExpectedConfirmationMissing";
                    }
                    else
                    {
                        noPromptSalvages++;
                        outcome = "NoPrompt";
                    }
                }

                PortSleep(token, PortSalvagePostSlotDelayMs);
                AppLogger.Info($"Salvage timing: slotIndex={cachedSlotAttempts}; cachedSlotIndex={i + 1}; cachedSlotCount={cachedSlots.Count}; row={target.Row}; column={target.Column}; screenPoint={FormatPoint(target.ScreenPoint)}; footprintRows={target.FootprintRows}; quality={PortLogField(target.Quality)}; confirmationExpected={target.ConfirmationExpected}; slotClickSent={slotClickSent}; retryAttempted={retryAttempted}; retryClickSent={retryClickSent}; confirmationFound={confirmationFound}; enterSent={enterSent}; outcome={PortLogField(outcome)}; confirmationWaitMs={confirmationWaitMs}; confirmationScans={confirmationScans}; nextSlotScanMs=0; cacheMode=SingleInventoryScan; slotElapsedMs={slotPerf.ElapsedMilliseconds}; totalSalvageElapsedMs={salvagePerf.ElapsedMilliseconds}");

                if (token.IsCancellationRequested)
                {
                    return false;
                }

                if (target.ConfirmationExpected && !confirmationFound)
                {
                    DebugManager.Session.RecordSalvageFailure("Salvage failed: expected confirmation not found");
                    AppLogger.Info($"Salvage timing summary: phase={PortLogField(phase)}; slotsClicked={slotsClicked}; cachedSlotAttempts={cachedSlotAttempts}; cachedSlotCount={cachedSlots.Count}; confirmedSalvages={confirmedSalvages}; noPromptSalvages={noPromptSalvages}; confirmationMisses={confirmationMisses}; expectedConfirmationMisses={expectedConfirmationMisses}; regularGemSkips={regularGemSkips}; retainedRegularGemCount={portLastRegularGemCandidateCount}; inventoryScanMs={inventoryScanPerf.ElapsedMilliseconds}; cacheMode=SingleInventoryScan; salvageSuccess=False; totalSalvageElapsedMs={salvagePerf.ElapsedMilliseconds}; confirmationTimeoutMs={PortSalvageConfirmationTimeoutMs}; expectedConfirmationTimeoutMs={PortSalvageExpectedConfirmationTimeoutMs}; confirmationFastAttempts={PortSalvageConfirmationFastAttempts}; expectedConfirmationAttempts={PortSalvageExpectedConfirmationAttempts}; confirmationFastDelayMs={PortSalvageConfirmationFastDelayMs}; expectedConfirmationDelayMs={PortSalvageExpectedConfirmationDelayMs}; postSlotDelayMs={PortSalvagePostSlotDelayMs}; slotClickSettleMs={PortSalvageSlotClickSettleMs}; slotClickHoldMs={PortSalvageSlotClickHoldMs}");
                    PortCaptureFailureScreenshot("SalvageExpectedConfirmationMissing", "Salvage");
                    return false;
                }
            }

            if (closeAfterSalvage)
            {
                PortPressEscapeForAutomation();
                PortSleep(token, 350);
            }

            AddWorkflowStep(slotsClicked == 0 ? "Salvage skipped: no salvageable filled inventory slots found." : $"Salvage completed: {slotsClicked} slots clicked");
            AppLogger.Info($"Salvage timing summary: phase={PortLogField(phase)}; slotsClicked={slotsClicked}; cachedSlotAttempts={cachedSlotAttempts}; cachedSlotCount={cachedSlots.Count}; confirmedSalvages={confirmedSalvages}; noPromptSalvages={noPromptSalvages}; confirmationMisses={confirmationMisses}; expectedConfirmationMisses={expectedConfirmationMisses}; regularGemSkips={regularGemSkips}; retainedRegularGemCount={portLastRegularGemCandidateCount}; inventoryScanMs={inventoryScanPerf.ElapsedMilliseconds}; cacheMode=SingleInventoryScan; salvageSuccess=True; totalSalvageElapsedMs={salvagePerf.ElapsedMilliseconds}; confirmationTimeoutMs={PortSalvageConfirmationTimeoutMs}; expectedConfirmationTimeoutMs={PortSalvageExpectedConfirmationTimeoutMs}; confirmationFastAttempts={PortSalvageConfirmationFastAttempts}; expectedConfirmationAttempts={PortSalvageExpectedConfirmationAttempts}; confirmationFastDelayMs={PortSalvageConfirmationFastDelayMs}; expectedConfirmationDelayMs={PortSalvageExpectedConfirmationDelayMs}; postSlotDelayMs={PortSalvagePostSlotDelayMs}; slotClickSettleMs={PortSalvageSlotClickSettleMs}; slotClickHoldMs={PortSalvageSlotClickHoldMs}");
            PortCaptureSuccessScreenshot("Salvage", "SalvageComplete");
            return true;
        }

        private IReadOnlyList<PortSalvageBulkCategory> PortBulkSalvageCategories()
        {
            return
            [
                new("Blue", "Salvage All Blue", PortBulkSalvageBlueActiveColor),
                new("Yellow", "Salvage All Yellow", PortBulkSalvageYellowActiveColor),
            ];
        }

        private PortBulkSalvageResult PortTryBulkSalvageCategory(PortSalvageBulkCategory category, CancellationToken token)
        {
            Stopwatch sw = Stopwatch.StartNew();
            DrawingPoint referencePoint = portSalvageCoords.GetValueOrDefault(category.CoordinateName, category.Name.Equals("Blue", StringComparison.OrdinalIgnoreCase)
                ? new DrawingPoint(424, 382)
                : new DrawingPoint(511, 382));
            DrawingPoint screenPoint = PortScaleGamePoint(referencePoint);
            PortBulkSalvageColorSample colorSample = PortSampleBulkSalvageButton(screenPoint, category.ActiveColorPredicate);
            if (!colorSample.Active)
            {
                PortBulkSalvageResult inactive = new(category.Name, "InactiveCategoryButton", false, false, false, true, colorSample, sw.ElapsedMilliseconds);
                PortLogBulkSalvageResult(inactive, screenPoint);
                return inactive;
            }

            bool clickSent = PortSafeLeftClick(screenPoint);
            if (!clickSent)
            {
                PortBulkSalvageResult unsafeClick = new(category.Name, "UnsafeClick", false, false, false, false, colorSample, sw.ElapsedMilliseconds);
                PortLogBulkSalvageResult(unsafeClick, screenPoint);
                return unsafeClick;
            }

            bool promptFound = PortWaitForSalvageConfirmationWithBudget(
                token,
                PortBulkSalvagePromptTimeoutMs,
                PortSalvageExpectedConfirmationAttempts,
                PortSalvageExpectedConfirmationDelayMs,
                out long promptWaitMs,
                out int promptScans);
            if (!promptFound)
            {
                PortBulkSalvageResult missingPrompt = new(category.Name, "PromptMissingAfterActiveClick", true, false, false, false, colorSample, sw.ElapsedMilliseconds);
                PortLogBulkSalvageResult(missingPrompt, screenPoint, promptWaitMs, promptScans);
                return missingPrompt;
            }

            PortPressKey(PortVkReturn);
            bool promptCleared = PortWaitForSalvageConfirmationCleared(token, PortBulkSalvagePromptClearTimeoutMs);
            PortSleep(token, PortBulkSalvageSettleMs);
            PortBulkSalvageResult result = new(
                category.Name,
                promptCleared ? "Confirmed" : "PromptDidNotClear",
                true,
                true,
                true,
                promptCleared,
                colorSample,
                sw.ElapsedMilliseconds);
            PortLogBulkSalvageResult(result, screenPoint, promptWaitMs, promptScans);
            return result;
        }

        private void PortLogBulkSalvageResult(PortBulkSalvageResult result, DrawingPoint screenPoint, long promptWaitMs = 0, int promptScans = 0)
        {
            AppLogger.Info(
                "Bulk salvage category: " +
                $"category={PortLogField(result.Category)}; " +
                $"outcome={PortLogField(result.Outcome)}; " +
                $"screenPoint={FormatPoint(screenPoint)}; " +
                $"active={result.ColorSample.Active}; " +
                $"activePixels={result.ColorSample.ActivePixels}; " +
                $"sampledPixels={result.ColorSample.SampledPixels}; " +
                $"activeRatio={result.ColorSample.ActiveRatio:0.000}; " +
                $"clickSent={result.ClickSent}; " +
                $"promptFound={result.PromptFound}; " +
                $"enterSent={result.EnterSent}; " +
                $"promptCleared={result.PromptCleared}; " +
                $"promptWaitMs={promptWaitMs}; " +
                $"promptScans={promptScans}; " +
                $"elapsedMs={result.ElapsedMs}");
        }

        private PortBulkSalvageColorSample PortSampleBulkSalvageButton(DrawingPoint centerPoint, Func<Color, bool> activeColorPredicate)
        {
            if (!PortTryGetDiabloRect(out RECT rect))
            {
                return new(false, 0, 0, 0);
            }

            int half = PortBulkSalvageButtonSampleSize / 2;
            Rectangle sample = Rectangle.FromLTRB(
                centerPoint.X - half,
                centerPoint.Y - half,
                centerPoint.X + half,
                centerPoint.Y + half);
            Rectangle diablo = Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
            sample = Rectangle.Intersect(sample, diablo);
            sample = Rectangle.Intersect(sample, SystemInformation.VirtualScreen);
            if (sample.Width <= 0 || sample.Height <= 0)
            {
                return new(false, 0, 0, 0);
            }

            using Bitmap screenshot = new(sample.Width, sample.Height);
            using (Graphics graphics = Graphics.FromImage(screenshot))
            {
                graphics.CopyFromScreen(sample.Left, sample.Top, 0, 0, screenshot.Size);
            }

            int activePixels = 0;
            int sampledPixels = 0;
            for (int y = 0; y < screenshot.Height; y++)
            {
                for (int x = 0; x < screenshot.Width; x++)
                {
                    Color color = screenshot.GetPixel(x, y);
                    sampledPixels++;
                    if (activeColorPredicate(color))
                    {
                        activePixels++;
                    }
                }
            }

            double ratio = sampledPixels == 0 ? 0 : activePixels / (double)sampledPixels;
            return new(activePixels >= PortBulkSalvageActivePixelThreshold, activePixels, sampledPixels, ratio);
        }

        private static bool PortBulkSalvageBlueActiveColor(Color color)
        {
            return color.B >= 95 &&
                color.B >= color.R + 28 &&
                color.B >= color.G + 16 &&
                color.G >= 35;
        }

        private static bool PortBulkSalvageYellowActiveColor(Color color)
        {
            return color.R >= 95 &&
                color.G >= 75 &&
                color.B <= 95 &&
                Math.Abs(color.R - color.G) <= 70 &&
                color.R >= color.B + 35 &&
                color.G >= color.B + 25;
        }

        private bool PortWaitForSalvageConfirmationCleared(CancellationToken token, int timeoutMs)
        {
            Stopwatch sw = Stopwatch.StartNew();
            string confirmationImage = Img("Salvage", "Salvage Confirmation Button.png");
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (token.IsCancellationRequested)
                {
                    return false;
                }

                if (!PortImageVisibleInDiablo(confirmationImage, PortVendorUiConfidence))
                {
                    return true;
                }

                PortSleep(token, PortSalvageConfirmationFastDelayMs);
            }

            return false;
        }

        private bool PortSafeSalvageSlotClick(DrawingPoint point)
        {
            if (!PortClickPointIsSafe(point))
            {
                PortSetAppStatus("Unsafe Click Blocked");
                return false;
            }

            SetCursorPos(point.X, point.Y);
            Thread.Sleep(PortSalvageSlotClickSettleMs);
            PortRuntimeMouseDown(MOUSEEVENTF_LEFTDOWN);
            Thread.Sleep(PortSalvageSlotClickHoldMs);
            PortRuntimeMouseUp(MOUSEEVENTF_LEFTUP);
            return true;
        }

        private bool PortWaitForSalvageConfirmationFast(CancellationToken token, out long elapsedMs, out int scans)
        {
            return PortWaitForSalvageConfirmationWithBudget(
                token,
                PortSalvageConfirmationTimeoutMs,
                PortSalvageConfirmationFastAttempts,
                PortSalvageConfirmationFastDelayMs,
                out elapsedMs,
                out scans);
        }

        private bool PortWaitForSalvageConfirmationExpected(CancellationToken token, out long elapsedMs, out int scans)
        {
            return PortWaitForSalvageConfirmationWithBudget(
                token,
                PortSalvageExpectedConfirmationTimeoutMs,
                PortSalvageExpectedConfirmationAttempts,
                PortSalvageExpectedConfirmationDelayMs,
                out elapsedMs,
                out scans);
        }

        private bool PortWaitForSalvageConfirmation(CancellationToken token, bool confirmationExpected, out long elapsedMs, out int scans)
        {
            return confirmationExpected
                ? PortWaitForSalvageConfirmationExpected(token, out elapsedMs, out scans)
                : PortWaitForSalvageConfirmationFast(token, out elapsedMs, out scans);
        }

        private bool PortWaitForSalvageConfirmationWithBudget(
            CancellationToken token,
            int timeoutMs,
            int attempts,
            int delayMs,
            out long elapsedMs,
            out int scans)
        {
            Stopwatch sw = Stopwatch.StartNew();
            scans = 0;
            string confirmationImage = Img("Salvage", "Salvage Confirmation Button.png");

            for (int attempt = 0; attempt < attempts && sw.ElapsedMilliseconds < timeoutMs; attempt++)
            {
                if (token.IsCancellationRequested)
                {
                    elapsedMs = sw.ElapsedMilliseconds;
                    return false;
                }

                scans++;
                if (PortImageVisibleInDiablo(confirmationImage, PortVendorUiConfidence))
                {
                    elapsedMs = sw.ElapsedMilliseconds;
                    return true;
                }

                if (attempt + 1 < attempts)
                {
                    PortSleep(token, delayMs);
                }
            }

            elapsedMs = sw.ElapsedMilliseconds;
            return false;
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
                DebugManager.Session.RecordRepairFailure("Repair failed: Diablo activation failed before blacksmith menu");
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
            DebugManager.Session.RecordRepairFailure("Repair failed: repair station/blacksmith menu not found");
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
