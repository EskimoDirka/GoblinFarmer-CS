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
                    timeoutMs: 60000,
                    confidence: PortStartGameButtonConfidence,
                    token: token);

                if (!clickedPlay)
                {
                    MessageBox.Show("Could not find Battle.net Play button.");
                    PortSetAppStatus("Play Button Not Found");
                    return false;
                }

                AddWorkflowStep("Clicking Play button");
                PortSleep(token, 2000);
                CloseBattleNet();
                PortSetAppStatus("Launching Diablo III");

                Stopwatch sw = Stopwatch.StartNew();
                AddWorkflowStep("Waiting for Diablo process");

                while (sw.ElapsedMilliseconds < 120000)
                {
                    if (token.IsCancellationRequested)
                    {
                        return false;
                    }
                    if (IsDiabloRunning())
                    {
                        AppLogger.Info($"Diablo process detected after {sw.ElapsedMilliseconds}ms");
                        PortSetAppStatus("Diablo III Started");
                        return true;
                    }

                    PortSleep(token, 1000);
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

                if (FindImageInDiabloWindow(
                    Img("Start Game", "Start Game Button.png"),
                    out DrawingPoint centerPoint,
                    confidence: PortStartGameButtonConfidence,
                    matchMode: ImageMatchMode.Color))
                {
                    attempts++;
                    AppLogger.Info($"Start Game click attempt {attempts}/{maxAttempts}: clicking center at {centerPoint.X},{centerPoint.Y}");
                    AddWorkflowStep($"Clicking Start Game (attempt {attempts})");
                    LeftClick(centerPoint);
                    PortSleep(token, 1200);
                    if (PortVerifyStartGameClick(token))
                    {
                        PortSetAppStatus("Start Game Clicked");
                        return true;
                    }

                    AppLogger.Info($"Start Game click attempt {attempts}/{maxAttempts} not verified; retrying after transition wait");
                    AddWorkflowStep("Start Game click not verified; retrying");
                    PortSleep(token, 900);
                }

                PortSleep(token, 150);
            }

            MessageBox.Show("Could not find Diablo Start Game button.");
            PortSetAppStatus("Start Game Not Found");
            AppLogger.Info($"Start Game failed: attempts={attempts}; elapsed={sw.ElapsedMilliseconds}ms; buttonVisible={PortStartGameButtonVisible(logPerf: true)}; loaded={PortCharacterLoadConfirmationVisible() || PortGameLoadedLocationTitleVisible()}");
            PortCaptureDebugScreenshot("StartGameTimeout");
            return false;
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
                    AppLogger.Info($"PERF Leave game confirmation: Start Game detected after {startButtonChecks} checks in {perf.ElapsedMilliseconds}ms");
                    return true;
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
