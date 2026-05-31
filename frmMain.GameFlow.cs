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
                if (!PortTownPrepAtBlacksmith(token))
                {
                    PortWorkflowFailed("Blacksmith prep");
                    return false;
                }

                if (!PortLeaveCurrentGame(token))
                {
                    PortWorkflowFailed("Leaving current game");
                    return false;
                }
            }

            PortSetAppStatus("Creating New Game");
            if (!PortCreateNewGame(token))
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

        private bool PortStartDiablo(CancellationToken token)
        {
            AddWorkflowStep("Starting Battle.net");
            StartBattleNet();
            AddWorkflowStep("Waiting for Battle.net Play Button");
            PortSetAppStatus("Waiting For Battle.net Play Button");

            bool clickedPlay = WaitForImageAndClick(
                Img("Start Game", "Battle Net Play Button.png"),
                timeoutMs: 60000,
                confidence: PortStartGameButtonConfidence);

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
                    PortSetAppStatus("Diablo III Started");
                    return true;
                }

                PortSleep(token, 1000);
            }

            MessageBox.Show("Diablo III did not start within the timeout.");
            PortSetAppStatus("Diablo Start Timeout");
            return false;
        }

        private bool PortCreateNewGame(CancellationToken token)
        {
            AddWorkflowStep("Waiting for Start Game button");
            PortSetAppStatus("Waiting For Start Game Button");

            Stopwatch sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < 60000)
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
                    LeftClick(centerPoint);
                    if (PortVerifyStartGameClick(token))
                    {
                        AddWorkflowStep("Clicking Start Game");
                        PortSetAppStatus("Start Game Clicked");
                        return true;
                    }

                    AppLogger.Info("Start Game click had no effect; retrying");
                    AddWorkflowStep("Start Game click had no effect; retrying");
                }

                PortSleep(token, 150);
            }

            MessageBox.Show("Could not find Diablo Start Game button.");
            PortSetAppStatus("Start Game Not Found");
            PortCaptureDebugScreenshot("StartGameTimeout");
            return false;
        }

        private bool PortVerifyStartGameClick(CancellationToken token)
        {
            Stopwatch sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < 3000)
            {
                if (token.IsCancellationRequested)
                {
                    return false;
                }

                if (!PortStartGameButtonVisible() || PortPlayerIsInGame())
                {
                    AppLogger.Info($"Start Game click verified in {sw.ElapsedMilliseconds}ms");
                    return true;
                }

                PortSleep(token, 150);
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
