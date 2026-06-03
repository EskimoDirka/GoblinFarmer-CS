using System;
using System.Diagnostics;
using DrawingPoint = System.Drawing.Point;

namespace GoblinFarmer
{
    public partial class frmMain
    {
        /// <summary>
        /// Runs the leave-game/start-game sequence used to reset into a fresh game.
        /// </summary>
        private bool PortMakeNewGameFlow(CancellationToken token)
        {
            AddWorkflowStep("Starting Make New Game flow");
            PortSetAppStatus("Starting New Game Flow");

            if (!IsDiabloRunning() && !PortStartDiablo(token))
            {
                return false;
            }

            if (!ActivateDiabloWindow())
            {
                return false;
            }

            string currentLocation = "";
            bool startGameVisible = PortStartGameButtonVisible(logPerf: true);
            bool playerInGame = false;
            if (!startGameVisible)
            {
                Stopwatch currentLocationPerf = Stopwatch.StartNew();
                currentLocation = PortDetectSpecificLocation("New Tristram");
                if (string.IsNullOrWhiteSpace(currentLocation))
                {
                    currentLocation = PortDetectSpecificLocation("Southern Highlands");
                }

                AppLogger.Info($"PERF MakeNewGame initial current location detection: {PortDisplayLocation(currentLocation)} in {currentLocationPerf.ElapsedMilliseconds}ms");
                playerInGame = !string.IsNullOrWhiteSpace(currentLocation) || PortCharacterLoadConfirmationVisible() || PortGameMenuVisible();
            }

            if (playerInGame)
            {
                if (!PortCloseGameMenuIfOpen(token))
                {
                    PortWorkflowFailed("Closing game menu");
                    return false;
                }

                PortSetAppStatus("Preparing Town Cleanup");
                AddWorkflowStep($"Current location detected: {PortDisplayLocation(currentLocation)}");

                if (PortLocationMatches(currentLocation, "New Tristram"))
                {
                    PortSetAppStatus("Already In New Tristram");
                    if (!PortBounceNewTristramThroughHiddenCamp(token))
                    {
                        return false;
                    }
                }
                else
                {
                    AddWorkflowStep("Teleporting to New Tristram");
                    if (!PortTeleportToLocation("New Tristram", token, verifyArrival: true, bypassFailsafe: true))
                    {
                        PortWorkflowFailed("Teleporting to New Tristram");
                        return false;
                    }

                    AddWorkflowStep("New Tristram arrival confirmed");
                }

                AddWorkflowStep("Starting blacksmith prep");
                if (!PortRunRepairFlow(token))
                {
                    return false;
                }

                if (!PortLeaveCurrentGame(token))
                {
                    PortWorkflowFailed("Leaving current game");
                    return false;
                }
            }

            PortSetAppStatus("Creating New Game");
            bool previousLaunchFlowActive = portBattleNetLaunchFlowActive;
            portBattleNetLaunchFlowActive = true;
            Interlocked.Exchange(ref portLastLaunchFlowMissingLogTicks, 0);
            bool createdGame;
            try
            {
                createdGame = PortCreateNewGame(token);
            }
            finally
            {
                portBattleNetLaunchFlowActive = previousLaunchFlowActive;
            }

            if (!createdGame)
            {
                return false;
            }
            PortIncrementGamesCreated();

            if (!PortWaitForGameLoadAndOpenMap(token))
            {
                return false;
            }

            PortSetAppStatus("Teleporting To Southern Highlands");
            bool arrived = PortTeleportToLocation("Southern Highlands", token, verifyArrival: true, mapAlreadyOpen: true, bypassFailsafe: true);
            if (arrived)
            {
                PortRecordTeleport("Southern Highlands", portLastConfirmedLocation);
            }

            return arrived;
        }

        private bool PortExitGameFlow(CancellationToken token)
        {
            AppLogger.Info("Exit Game flow start");
            AddWorkflowStep("Starting Exit Game flow");
            PortSetAppStatus("Starting Exit Game Flow");

            if (!IsDiabloRunning())
            {
                AppLogger.Info("Exit Game flow: Diablo is not running");
                AddWorkflowStep("Diablo is not running");
                PortSetAppStatus("Diablo Not Running");
                AppLogger.Info("Exit Game flow end: Diablo not running");
                return true;
            }

            if (!ActivateDiabloWindow())
            {
                return false;
            }

            if (PortStartGameButtonVisible(logPerf: true) && !PortPlayerIsInGame())
            {
                AppLogger.Info("Exit Game flow: Diablo is at main menu; closing without repair");
                AddWorkflowStep("Diablo not in game; closing");
                return PortCloseDiabloWindow(token);
            }

            string currentLocation = PortDetectSpecificLocation("New Tristram");
            AddWorkflowStep($"Current location detected: {PortDisplayLocation(currentLocation)}");

            if (PortLocationMatches(currentLocation, "New Tristram"))
            {
                PortSetAppStatus("Already In New Tristram");
                if (!PortBounceNewTristramThroughHiddenCamp(token))
                {
                    return false;
                }
            }
            else
            {
                AddWorkflowStep("Teleporting to New Tristram");
                if (!PortTeleportToLocation("New Tristram", token, verifyArrival: true, bypassFailsafe: true))
                {
                    return PortWorkflowFailed("Teleporting to New Tristram");
                }

                AddWorkflowStep("New Tristram arrival confirmed");
            }

            AddWorkflowStep("Running repair flow before exit");
            if (!PortRunRepairFlow(token))
            {
                return false;
            }

            if (!PortCloseDiabloWindow(token))
            {
                return false;
            }

            AppLogger.Info("Exit Game flow end: success");
            AddWorkflowStep("Exit Game flow completed");
            PortSetAppStatus("Diablo Closed");
            PortCaptureSuccessScreenshot("ExitGame", "ExitGameComplete");
            return true;
        }

        private bool PortCloseDiabloWindow(CancellationToken token)
        {
            AppLogger.Info("Exit Game flow closing Diablo");
            AddWorkflowStep("Closing Diablo");
            PortSetAppStatus("Closing Diablo");

            IntPtr diabloWindow = FindDiabloWindow();
            if (diabloWindow == IntPtr.Zero)
            {
                AppLogger.Info("Exit Game flow closing Diablo: window already closed");
                PortSetAppStatus("Diablo Not Running");
                return true;
            }

            GetWindowThreadProcessId(diabloWindow, out uint processId);
            try
            {
                using Process process = Process.GetProcessById((int)processId);
                process.CloseMainWindow();

                Stopwatch sw = Stopwatch.StartNew();
                while (sw.ElapsedMilliseconds < 15000)
                {
                    if (token.IsCancellationRequested)
                    {
                        return false;
                    }

                    process.Refresh();
                    if (process.HasExited || FindDiabloWindow() == IntPtr.Zero)
                    {
                        AppLogger.Info($"Exit Game flow closing Diablo: closed in {sw.ElapsedMilliseconds}ms");
                        AddWorkflowStep("Diablo closed");
                        return true;
                    }

                    PortSleep(token, 250);
                }

                AppLogger.Info("Exit Game flow closing Diablo: close timed out");
                PortSetAppStatus("Diablo Close Timeout");
                return false;
            }
            catch (Exception ex)
            {
                AppLogger.Error("Exit Game flow closing Diablo failed.", ex);
                PortSetAppStatus("Diablo Close Failed");
                return false;
            }
        }

        private bool PortStartDiablo(CancellationToken token)
        {
            ResetBattleNetLaunchDiagnostics();
            portBattleNetLaunchFlowActive = true;
            Interlocked.Exchange(ref portLastLaunchFlowMissingLogTicks, 0);

            try
            {
                AddWorkflowStep("Starting Battle.net");
                if (!StartBattleNet(token))
                {
                    PortSetAppStatus("Battle.net Launch Failed");
                    return false;
                }

                if (!PrepareBattleNetForDiabloLaunch(token))
                {
                    PortSetAppStatus("Battle.net Setup Failed");
                    return false;
                }

                AddWorkflowStep("Waiting for Battle.net Play Button");
                PortSetAppStatus("Waiting For Battle.net Play Button");

                bool clickedPlay = WaitForBattleNetPlayButtonAndClick(
                    Img("Start Game", "Battle Net Play Button.png"),
                    timeoutMs: AppSettings.Launch.BattleNetPlayButtonTimeoutMs,
                    confidence: AppSettings.ImageRecognition.BattleNetPlayButtonConfidence,
                    token: token);

                if (!clickedPlay)
                {
                    MessageBox.Show("Could not find Battle.net Play button.");
                    PortSetAppStatus("Play Button Not Found");
                    return false;
                }

                if (battleNetPlayClickAcceptedByBattleNet)
                {
                    AddWorkflowStep("Battle.net accepted Play click");
                    PortSleep(token, AppSettings.Launch.BattleNetPostPlayAcceptedDelayMs);
                    CloseBattleNet();
                }
                else
                {
                    AddWorkflowStep("Waiting for Diablo after unconfirmed Play click");
                    AppLogger.Info("Battle.net close skipped before Diablo launch because app Play click was not confirmed accepted.");
                }

                PortSetAppStatus("Launching Diablo III");

                Stopwatch sw = Stopwatch.StartNew();
                AddWorkflowStep("Waiting for Diablo process");

                while (sw.ElapsedMilliseconds < AppSettings.Launch.DiabloStartTimeoutMs)
                {
                    if (token.IsCancellationRequested)
                    {
                        return false;
                    }
                    if (IsDiabloRunning())
                    {
                        AppLogger.Info($"Diablo process detected after {sw.ElapsedMilliseconds}ms");
                        PortSetAppStatus("Diablo III Started");
                        PortRecordDiabloLaunchAfterBattleNet(sw.ElapsedMilliseconds);
                        PortCaptureSuccessScreenshot("DiabloLaunch", "DiabloProcessDetected");
                        return true;
                    }

                    PortSleep(token, AppSettings.Launch.DiabloStartPollIntervalMs);
                }

                MessageBox.Show("Diablo III did not start within the timeout.");
                PortSetAppStatus("Diablo Start Timeout");
                return false;
            }
            finally
            {
                portBattleNetLaunchFlowActive = false;
            }
        }

        private bool PortCreateNewGame(CancellationToken token)
        {
            AddWorkflowStep("Waiting for Start Game button");
            PortSetAppStatus("Waiting For Start Game Button");

            Stopwatch sw = Stopwatch.StartNew();
            int attempts = 0;
            int detectionAttempts = 0;
            const int maxAttempts = 3;
            bool manualStartAccepted = false;
            while (sw.ElapsedMilliseconds < 60000 && attempts < maxAttempts)
            {
                if (token.IsCancellationRequested)
                {
                    return false;
                }

                if (PortStartGameLoadedStateVisible(out string loadedReason))
                {
                    manualStartAccepted = true;
                    AppLogger.Info($"StartGameAcceptedByLoadedGameState: appClicked=False; manualClickSuspected=True; reason={PortLogField(loadedReason)}; clickAttempts={attempts}; detectionAttempts={detectionAttempts}; elapsedMs={sw.ElapsedMilliseconds}");
                    PortSetAppStatus("Start Game Clicked");
                    return true;
                }

                detectionAttempts++;
                AppLogger.Info($"Start Game button detection attempt: attempt={detectionAttempts}; clickAttempts={attempts}/{maxAttempts}; elapsedMs={sw.ElapsedMilliseconds}; confidence={PortStartGameButtonConfidence:0.000}");
                if (PortTryFindStableStartGameButton(token, out DrawingPoint centerPoint, out long stableElapsedMs, detectionAttempts))
                {
                    attempts++;
                    AppLogger.Info($"Start Game stable match confirmed: detectionAttempt={detectionAttempts}; clickAttempt={attempts}/{maxAttempts}; point={centerPoint.X},{centerPoint.Y}; stableElapsedMs={stableElapsedMs}");
                    AddWorkflowStep($"Clicking Start Game (attempt {attempts})");
                    LeftClick(centerPoint);
                    AppLogger.Info($"Start Game click sent: attempt={attempts}/{maxAttempts}; point={centerPoint.X},{centerPoint.Y}; elapsedMs={sw.ElapsedMilliseconds}");
                    if (PortVerifyStartGameClick(token, out string acceptanceReason))
                    {
                        AppLogger.Info($"Start Game click accepted: attempt={attempts}/{maxAttempts}; reason={PortLogField(acceptanceReason)}; elapsedMs={sw.ElapsedMilliseconds}");
                        PortSetAppStatus("Start Game Clicked");
                        PortCaptureSuccessScreenshot("StartGame", "StartGameClicked");
                        return true;
                    }

                    string screenshotPath = PortCaptureFailureScreenshot("StartGameVerificationFailed", "StartGame");
                    PortLogStartGameVerificationFailure(attempts, maxAttempts, centerPoint, screenshotPath);
                    AppLogger.Info($"Start Game click attempt {attempts}/{maxAttempts} not verified; retrying after transition wait; acceptanceReason=Timeout");
                    AddWorkflowStep("Start Game click not verified; retrying");
                    PortSleep(token, 900);
                }

                PortSleep(token, 100);
            }

            if (!manualStartAccepted && PortStartGameLoadedStateVisible(out string finalLoadedReason))
            {
                AppLogger.Info($"StartGameAcceptedByLoadedGameState: appClicked=False; manualClickSuspected=True; reason={PortLogField(finalLoadedReason)}; clickAttempts={attempts}; detectionAttempts={detectionAttempts}; elapsedMs={sw.ElapsedMilliseconds}");
                PortSetAppStatus("Start Game Clicked");
                return true;
            }

            MessageBox.Show("Could not find Diablo Start Game button.");
            PortSetAppStatus("Start Game Not Found");
            AppLogger.Info($"Start Game failed: clickAttempts={attempts}; detectionAttempts={detectionAttempts}; elapsed={sw.ElapsedMilliseconds}ms; buttonVisible={PortStartGameButtonVisible(logPerf: true)}; loaded={PortCharacterLoadConfirmationVisible() || PortGameLoadedLocationTitleVisible()}; reason={(attempts >= maxAttempts ? "max click attempts reached" : "timeout waiting for stable Start Game button")}");
            PortCaptureFailureScreenshot("StartGameButtonNotFound", "StartGame");
            return false;
        }

        private bool PortTryFindStableStartGameButton(CancellationToken token, out DrawingPoint centerPoint, out long stableElapsedMs, int detectionAttempt = 0)
        {
            centerPoint = DrawingPoint.Empty;
            stableElapsedMs = 0;
            string imagePath = Img("Start Game", "Start Game Button.png");
            Stopwatch sw = Stopwatch.StartNew();
            DrawingPoint? firstPoint = null;
            long firstSeenAt = 0;
            DrawingPoint latestPoint = DrawingPoint.Empty;
            int latestDx = 0;
            int latestDy = 0;
            int scans = 0;
            int visibleCount = 0;
            int consecutiveStableCount = 0;
            const int tolerancePx = 8;
            const int requiredConsecutiveStableScans = 3;
            const int requiredStableDurationMs = 250;

            while (sw.ElapsedMilliseconds < 2500)
            {
                if (token.IsCancellationRequested)
                {
                    return false;
                }

                scans++;
                bool visible = FindImageInDiabloWindow(
                    imagePath,
                    out DrawingPoint foundPoint,
                    confidence: PortStartGameButtonConfidence,
                    matchMode: ImageMatchMode.Color);
                AppLogger.Info($"Start Game stable scan: detectionAttempt={detectionAttempt}; scan={scans}; elapsedMs={sw.ElapsedMilliseconds}; visible={visible}; point={(visible ? $"{foundPoint.X},{foundPoint.Y}" : "unavailable")}; confidence={PortStartGameButtonConfidence:0.000}");
                if (!visible)
                {
                    firstPoint = null;
                    firstSeenAt = 0;
                    visibleCount = 0;
                    consecutiveStableCount = 0;
                    PortSleep(token, 100);
                    continue;
                }

                visibleCount++;
                latestPoint = foundPoint;
                if (!firstPoint.HasValue)
                {
                    firstPoint = foundPoint;
                    firstSeenAt = sw.ElapsedMilliseconds;
                    latestDx = 0;
                    latestDy = 0;
                    consecutiveStableCount = 1;
                    AppLogger.Info($"Start Game stable candidate acquired: detectionAttempt={detectionAttempt}; scan={scans}; point={foundPoint.X},{foundPoint.Y}; elapsedMs={sw.ElapsedMilliseconds}");
                    PortSleep(token, 100);
                    continue;
                }

                latestDx = Math.Abs(foundPoint.X - firstPoint.Value.X);
                latestDy = Math.Abs(foundPoint.Y - firstPoint.Value.Y);
                bool withinTolerance = latestDx <= tolerancePx && latestDy <= tolerancePx;
                long stableDurationMs = sw.ElapsedMilliseconds - firstSeenAt;
                if (withinTolerance)
                {
                    consecutiveStableCount++;
                    AppLogger.Info($"StartGameButtonStableCandidate: detectionAttempt={detectionAttempt}; scan={scans}; first={firstPoint.Value.X},{firstPoint.Value.Y}; current={foundPoint.X},{foundPoint.Y}; dx={latestDx}; dy={latestDy}; tolerance={tolerancePx}; visibleCount={visibleCount}; consecutiveStableCount={consecutiveStableCount}; stableDurationMs={stableDurationMs}");
                }
                else
                {
                    AppLogger.Info($"StartGameButtonUnstable: detectionAttempt={detectionAttempt}; scan={scans}; first={firstPoint.Value.X},{firstPoint.Value.Y}; current={foundPoint.X},{foundPoint.Y}; dx={latestDx}; dy={latestDy}; tolerance={tolerancePx}; visibleCount={visibleCount}; consecutiveStableCount={consecutiveStableCount}; stableDurationMs={stableDurationMs}; reason=outside tolerance");
                    firstPoint = foundPoint;
                    firstSeenAt = sw.ElapsedMilliseconds;
                    consecutiveStableCount = 1;
                    PortSleep(token, 100);
                    continue;
                }

                if (consecutiveStableCount >= requiredConsecutiveStableScans || stableDurationMs >= requiredStableDurationMs)
                {
                    centerPoint = foundPoint;
                    stableElapsedMs = sw.ElapsedMilliseconds;
                    AppLogger.Info($"StartGameButtonStableAccepted: detectionAttempt={detectionAttempt}; scans={scans}; center={centerPoint.X},{centerPoint.Y}; firstPoint={firstPoint.Value.X},{firstPoint.Value.Y}; latestPoint={foundPoint.X},{foundPoint.Y}; stableElapsedMs={stableElapsedMs}; stableDurationMs={stableDurationMs}; dx={latestDx}; dy={latestDy}; tolerance={tolerancePx}; visibleCount={visibleCount}; consecutiveStableCount={consecutiveStableCount}; requiredStableDurationMs={requiredStableDurationMs}; requiredConsecutiveStableScans={requiredConsecutiveStableScans}");
                    return true;
                }

                PortSleep(token, 100);
            }

            string firstPointText = firstPoint.HasValue ? $"{firstPoint.Value.X},{firstPoint.Value.Y}" : "unavailable";
            string latestPointText = visibleCount > 0 ? $"{latestPoint.X},{latestPoint.Y}" : "unavailable";
            long finalStableDurationMs = firstSeenAt > 0 ? sw.ElapsedMilliseconds - firstSeenAt : 0;
            AppLogger.Info($"StartGameButtonStableNotFound: detectionAttempt={detectionAttempt}; scans={scans}; elapsed={sw.ElapsedMilliseconds}ms; buttonVisible={PortStartGameButtonVisible(logPerf: true)}; firstPoint={firstPointText}; latestPoint={latestPointText}; dx={latestDx}; dy={latestDy}; tolerance={tolerancePx}; visibleCount={visibleCount}; consecutiveStableCount={consecutiveStableCount}; stableDurationMs={finalStableDurationMs}; requiredStableDurationMs={requiredStableDurationMs}; requiredConsecutiveStableScans={requiredConsecutiveStableScans}; reason=stable acceptance conditions not met before detection window timeout");
            return false;
        }

        private void PortLogStartGameVerificationFailure(int attempt, int maxAttempts, DrawingPoint clickPoint, string screenshotPath)
        {
            string imagePath = Img("Start Game", "Start Game Button.png");
            Rectangle referenceRegion = PortScanRegion("StartGameButton", imagePath);
            string screenRegion = "unavailable";
            if (PortTryGetDiabloRect(out RECT rect))
            {
                Rectangle resolved = PortScaleReferenceRectangle(referenceRegion, rect);
                screenRegion = $"{resolved.Left},{resolved.Top},{resolved.Width},{resolved.Height}";
            }

            bool cursorAvailable = GetCursorPos(out DrawingPoint cursor);
            bool buttonVisible = FindImageInDiabloWindow(
                imagePath,
                out DrawingPoint visibleCenter,
                confidence: PortStartGameButtonConfidence,
                matchMode: ImageMatchMode.Color);
            bool loaded = PortCharacterLoadConfirmationVisible() || PortGameLoadedLocationTitleVisible() || PortPlayerIsInGame();
            string likelyExplanation = buttonVisible
                ? "Start Game button remained visible after click, so the menu did not transition before verification timeout, retry is expected."
                : "Start Game button was no longer visible, but no loading or in-game evidence was detected before verification timeout.";
            AppLogger.Info($"StartGameVerificationFailureSummary: attempt={attempt}/{maxAttempts}; clickPoint={clickPoint.X},{clickPoint.Y}; cursor={(cursorAvailable ? $"{cursor.X},{cursor.Y}" : "unavailable")}; scanRegionReference={FormatRectangle(referenceRegion)}; scanRegionScreen={screenRegion}; buttonVisible={buttonVisible}; visibleButtonCenter={(buttonVisible ? $"{visibleCenter.X},{visibleCenter.Y}" : "unavailable")}; loaded={loaded}; screenshotReason=StartGameVerificationFailed; screenshotPath={PortLogField(PortDisplayLocation(screenshotPath))}; likelyExplanation={PortLogField(likelyExplanation)}");
        }

        private bool PortVerifyStartGameClick(CancellationToken token, out string acceptanceReason)
        {
            acceptanceReason = "Timeout";
            Stopwatch sw = Stopwatch.StartNew();
            long buttonMissingSince = -1;
            int attempts = 0;
            while (sw.ElapsedMilliseconds < 5000)
            {
                if (token.IsCancellationRequested)
                {
                    acceptanceReason = "Cancelled";
                    return false;
                }

                attempts++;
                bool startVisible = PortStartGameButtonVisible();
                bool loadingOrLoaded = PortStartGameLoadedStateVisible(out string loadedReason);
                bool characterLoadVisible = loadedReason == "Character load confirmation detected";
                bool gameLoadedLocationVisible = loadedReason == "Game loaded location title detected";
                bool playerInGame = loadedReason == "Player in-game state detected";
                if (!startVisible)
                {
                    buttonMissingSince = buttonMissingSince < 0 ? sw.ElapsedMilliseconds : buttonMissingSince;
                }
                else
                {
                    buttonMissingSince = -1;
                }

                bool buttonGoneSteady = buttonMissingSince >= 0 && sw.ElapsedMilliseconds - buttonMissingSince >= 1000;
                AppLogger.Info($"Start Game click acceptance attempt: attempt={attempts}; elapsedMs={sw.ElapsedMilliseconds}; startVisible={startVisible}; characterLoadVisible={characterLoadVisible}; gameLoadedLocationVisible={gameLoadedLocationVisible}; playerInGame={playerInGame}; buttonGoneSteady={buttonGoneSteady}");
                if (characterLoadVisible)
                {
                    acceptanceReason = loadedReason;
                    AppLogger.Info($"Start Game click accepted: reason={PortLogField(acceptanceReason)}; attempts={attempts}; elapsedMs={sw.ElapsedMilliseconds}");
                    return true;
                }

                if (gameLoadedLocationVisible)
                {
                    acceptanceReason = loadedReason;
                    AppLogger.Info($"Start Game click accepted: reason={PortLogField(acceptanceReason)}; attempts={attempts}; elapsedMs={sw.ElapsedMilliseconds}");
                    return true;
                }

                if (playerInGame)
                {
                    acceptanceReason = loadedReason;
                    AppLogger.Info($"Start Game click accepted: reason={PortLogField(acceptanceReason)}; attempts={attempts}; elapsedMs={sw.ElapsedMilliseconds}");
                    return true;
                }

                if (buttonGoneSteady)
                {
                    acceptanceReason = "Start Game button disappeared";
                    AppLogger.Info($"Start Game click accepted: reason={PortLogField(acceptanceReason)}; attempts={attempts}; elapsedMs={sw.ElapsedMilliseconds}; startVisible={startVisible}; loadingOrLoaded={loadingOrLoaded}; buttonGoneSteady={buttonGoneSteady}");
                    return true;
                }

                PortSleep(token, 100);
            }

            AppLogger.Info($"Start Game click acceptance timeout: attempts={attempts}; elapsedMs={sw.ElapsedMilliseconds}; reason={PortLogField(acceptanceReason)}");
            return false;
        }

        private bool PortStartGameLoadedStateVisible(out string reason)
        {
            if (PortCharacterLoadConfirmationVisible())
            {
                reason = "Character load confirmation detected";
                return true;
            }

            if (PortGameLoadedLocationTitleVisible())
            {
                reason = "Game loaded location title detected";
                return true;
            }

            if (PortPlayerIsInGame())
            {
                reason = "Player in-game state detected";
                return true;
            }

            reason = "";
            return false;
        }

        private bool PortWaitForGameLoadAndOpenMap(CancellationToken token)
        {
            AddWorkflowStep("Waiting for game load");
            Stopwatch perf = Stopwatch.StartNew();
            int scans = 0;
            Stopwatch sw = Stopwatch.StartNew();

            while (sw.ElapsedMilliseconds < 90000)
            {
                if (token.IsCancellationRequested)
                {
                    AppLogger.Info($"PERF PortWaitForGameLoadAndOpenMap: cancelled after {scans} scans in {perf.ElapsedMilliseconds}ms");
                    return false;
                }

                scans++;
                if (PortCharacterLoadConfirmationVisible())
                {
                    PortSetAppStatus("Opening Map");
                    AppLogger.Info($"PERF PortWaitForGameLoadAndOpenMap: loaded after {scans} scans in {perf.ElapsedMilliseconds}ms");
                    return PortOpenMapAndWait(token);
                }

                PortSleep(token, 500);
            }

            AppLogger.Info($"PERF PortWaitForGameLoadAndOpenMap: timeout after {scans} scans in {perf.ElapsedMilliseconds}ms");
            return false;
        }

        private bool PortLeaveCurrentGame(CancellationToken token)
        {
            bool succeeded = false;
            AddWorkflowStep("Leaving game");
            PortSetAppStatus("Leaving Game");
            try
            {
                if (!ActivateDiabloWindow())
                {
                    return false;
                }

                if (!PortCloseOpenPanels(token))
                {
                    return PortWorkflowFailed("Closing open panels");
                }

                if (!PortOpenGameMenu(token))
                {
                    return PortWorkflowFailed("Opening game menu");
                }

                AddWorkflowStep("Waiting for Leave Game button");
                if (!PortWaitForLeaveGameButtonAndClick(token, 12000))
                {
                    return PortWorkflowFailed("Leave Game button not found");
                }

                AddWorkflowStep("Leave Game button found/clicked");
                AddWorkflowStep("Waiting for main menu");
                if (!PortWaitForMainMenuAfterLeave(token, 45000))
                {
                    return PortWorkflowFailed("Waiting for main menu");
                }

                AddWorkflowStep("Main menu confirmed");
                PortCaptureSuccessScreenshot("ExitGame", "LeaveGameMainMenuConfirmed");
                succeeded = true;
                return true;
            }
            finally
            {
                if (!succeeded && !token.IsCancellationRequested)
                {
                    PortCaptureDebugScreenshot("LeaveGameFailed");
                }
            }
        }

        private bool PortWaitForMainMenuAfterLeave(CancellationToken token, int timeoutMs)
        {
            Stopwatch perf = Stopwatch.StartNew();
            int startButtonChecks = 0;
            bool fallbackUsed = false;
            Stopwatch sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (token.IsCancellationRequested)
                {
                    AppLogger.Info($"PERF Leave game confirmation: cancelled after {startButtonChecks} Start Game checks in {perf.ElapsedMilliseconds}ms");
                    return false;
                }

                startButtonChecks++;
                if (PortStartGameButtonVisible())
                {
                    if (PortTryFindStableStartGameButton(token, out DrawingPoint stableStartGamePoint, out long stableElapsedMs))
                    {
                        AppLogger.Info($"PERF Leave game confirmation: stable Start Game detected after {startButtonChecks} checks in {perf.ElapsedMilliseconds}ms; point={stableStartGamePoint.X},{stableStartGamePoint.Y}; stableElapsedMs={stableElapsedMs}");
                        return true;
                    }

                    AppLogger.Info($"PERF Leave game confirmation: Start Game visible but not stable after {startButtonChecks} checks in {perf.ElapsedMilliseconds}ms");
                }

                if (!fallbackUsed && sw.ElapsedMilliseconds >= 12000)
                {
                    fallbackUsed = true;
                    AddWorkflowStep("Start Game button not seen; running fallback in-game check");
                    string fallbackLocation = PortDetectCurrentLocation();
                    if (string.IsNullOrWhiteSpace(fallbackLocation) && !PortCharacterLoadConfirmationVisible() && !PortGameMenuVisible())
                    {
                        AppLogger.Info($"PERF Leave game confirmation: fallback confirmed after {startButtonChecks} Start Game checks in {perf.ElapsedMilliseconds}ms");
                        return true;
                    }
                }

                PortSleep(token, 300);
            }

            AppLogger.Info($"PERF Leave game confirmation: timeout after {startButtonChecks} Start Game checks in {perf.ElapsedMilliseconds}ms");
            return false;
        }
    }
}
