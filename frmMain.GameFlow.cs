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
                StartBattleNet();
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
            const int maxAttempts = 3;
            while (sw.ElapsedMilliseconds < 60000 && attempts < maxAttempts)
            {
                if (token.IsCancellationRequested)
                {
                    return false;
                }

                if (PortTryFindStableStartGameButton(token, out DrawingPoint centerPoint, out long stableElapsedMs))
                {
                    attempts++;
                    AppLogger.Info($"Start Game click attempt {attempts}/{maxAttempts}: clicking stable center at {centerPoint.X},{centerPoint.Y}; stableElapsedMs={stableElapsedMs}");
                    AddWorkflowStep($"Clicking Start Game (attempt {attempts})");
                    LeftClick(centerPoint);
                    PortSleep(token, 1200);
                    if (PortVerifyStartGameClick(token))
                    {
                        PortSetAppStatus("Start Game Clicked");
                        PortCaptureSuccessScreenshot("StartGame", "StartGameClicked");
                        return true;
                    }

                    string screenshotPath = PortCaptureFailureScreenshot("StartGameVerificationFailed", "StartGame");
                    PortLogStartGameVerificationFailure(attempts, maxAttempts, centerPoint, screenshotPath);
                    AppLogger.Info($"Start Game click attempt {attempts}/{maxAttempts} not verified; retrying after transition wait");
                    AddWorkflowStep("Start Game click not verified; retrying");
                    PortSleep(token, 900);
                }

                PortSleep(token, 150);
            }

            MessageBox.Show("Could not find Diablo Start Game button.");
            PortSetAppStatus("Start Game Not Found");
            AppLogger.Info($"Start Game failed: attempts={attempts}; elapsed={sw.ElapsedMilliseconds}ms; buttonVisible={PortStartGameButtonVisible(logPerf: true)}; loaded={PortCharacterLoadConfirmationVisible() || PortGameLoadedLocationTitleVisible()}");
            PortCaptureFailureScreenshot("StartGameButtonNotFound", "StartGame");
            return false;
        }

        private bool PortTryFindStableStartGameButton(CancellationToken token, out DrawingPoint centerPoint, out long stableElapsedMs)
        {
            centerPoint = DrawingPoint.Empty;
            stableElapsedMs = 0;
            string imagePath = Img("Start Game", "Start Game Button.png");
            Stopwatch sw = Stopwatch.StartNew();
            DrawingPoint? firstPoint = null;
            long firstSeenAt = 0;

            while (sw.ElapsedMilliseconds < 2500)
            {
                if (token.IsCancellationRequested)
                {
                    return false;
                }

                bool visible = FindImageInDiabloWindow(
                    imagePath,
                    out DrawingPoint foundPoint,
                    confidence: PortStartGameButtonConfidence,
                    matchMode: ImageMatchMode.Color);
                if (!visible)
                {
                    firstPoint = null;
                    firstSeenAt = 0;
                    PortSleep(token, 100);
                    continue;
                }

                if (!firstPoint.HasValue)
                {
                    firstPoint = foundPoint;
                    firstSeenAt = sw.ElapsedMilliseconds;
                    PortSleep(token, 250);
                    continue;
                }

                int dx = Math.Abs(foundPoint.X - firstPoint.Value.X);
                int dy = Math.Abs(foundPoint.Y - firstPoint.Value.Y);
                if (dx <= 8 && dy <= 8 && sw.ElapsedMilliseconds - firstSeenAt >= 250)
                {
                    centerPoint = foundPoint;
                    stableElapsedMs = sw.ElapsedMilliseconds;
                    AppLogger.Info($"StartGameButtonStable: center={centerPoint.X},{centerPoint.Y}; stableElapsedMs={stableElapsedMs}; dx={dx}; dy={dy}");
                    return true;
                }

                AppLogger.Info($"StartGameButtonUnstable: first={firstPoint.Value.X},{firstPoint.Value.Y}; current={foundPoint.X},{foundPoint.Y}; dx={dx}; dy={dy}; elapsed={sw.ElapsedMilliseconds}ms");
                firstPoint = foundPoint;
                firstSeenAt = sw.ElapsedMilliseconds;
                PortSleep(token, 150);
            }

            AppLogger.Info($"StartGameButtonStableNotFound: elapsed={sw.ElapsedMilliseconds}ms; buttonVisible={PortStartGameButtonVisible(logPerf: true)}");
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

        private bool PortVerifyStartGameClick(CancellationToken token)
        {
            Stopwatch sw = Stopwatch.StartNew();
            long buttonMissingSince = -1;
            while (sw.ElapsedMilliseconds < 5000)
            {
                if (token.IsCancellationRequested)
                {
                    return false;
                }

                bool startVisible = PortStartGameButtonVisible();
                bool loadingOrLoaded = PortCharacterLoadConfirmationVisible() ||
                    PortGameLoadedLocationTitleVisible() ||
                    PortPlayerIsInGame();
                if (!startVisible)
                {
                    buttonMissingSince = buttonMissingSince < 0 ? sw.ElapsedMilliseconds : buttonMissingSince;
                }
                else
                {
                    buttonMissingSince = -1;
                }

                bool buttonGoneSteady = buttonMissingSince >= 0 && sw.ElapsedMilliseconds - buttonMissingSince >= 1000;
                if (loadingOrLoaded || buttonGoneSteady)
                {
                    AppLogger.Info($"Start Game click verified in {sw.ElapsedMilliseconds}ms; startVisible={startVisible}; loadingOrLoaded={loadingOrLoaded}; buttonGoneSteady={buttonGoneSteady}");
                    return true;
                }

                PortSleep(token, 250);
            }

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
