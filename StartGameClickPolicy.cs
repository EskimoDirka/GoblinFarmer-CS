namespace GoblinFarmer
{
    internal enum StartGameClickDecision
    {
        Allow,
        BlockAlreadyInGame,
        BlockMainMenuNotConfirmed,
    }

    internal readonly record struct StartGameClickState(
        bool StableStartGameVisible,
        bool LeaveGameVisible,
        bool CharacterLoadVisible,
        bool LoadedLocationVisible,
        bool PlayerInGameVisible,
        bool CurrentLocationVisible,
        long StableElapsedMs)
    {
        public bool HasInGameSignal =>
            LeaveGameVisible ||
            CharacterLoadVisible ||
            LoadedLocationVisible ||
            PlayerInGameVisible ||
            CurrentLocationVisible;
    }

    internal readonly record struct StartGameClickReadiness(
        StartGameClickDecision Decision,
        string Reason,
        bool MainMenuConfirmed);

    internal static class StartGameClickPolicy
    {
        public const int RequiredStableDurationMs = 800;
        public const int RequiredConsecutiveStableScans = 6;

        public static StartGameClickReadiness Evaluate(StartGameClickState state)
        {
            if (state.HasInGameSignal)
            {
                return new StartGameClickReadiness(StartGameClickDecision.BlockAlreadyInGame, BuildInGameReason(state), false);
            }

            if (!state.StableStartGameVisible)
            {
                return new StartGameClickReadiness(StartGameClickDecision.BlockMainMenuNotConfirmed, "Start Game stable match missing", false);
            }

            if (state.StableElapsedMs < RequiredStableDurationMs)
            {
                return new StartGameClickReadiness(
                    StartGameClickDecision.BlockMainMenuNotConfirmed,
                    $"Start Game stability too short ({state.StableElapsedMs}ms < {RequiredStableDurationMs}ms)",
                    false);
            }

            return new StartGameClickReadiness(StartGameClickDecision.Allow, "Main menu confirmed by stable Start Game button and no in-game signals", true);
        }

        private static string BuildInGameReason(StartGameClickState state)
        {
            List<string> reasons = [];
            if (state.LeaveGameVisible)
            {
                reasons.Add("Leave Game visible");
            }
            if (state.CharacterLoadVisible)
            {
                reasons.Add("character load visible");
            }
            if (state.LoadedLocationVisible)
            {
                reasons.Add("loaded location visible");
            }
            if (state.PlayerInGameVisible)
            {
                reasons.Add("player in-game state visible");
            }
            if (state.CurrentLocationVisible)
            {
                reasons.Add("current location visible");
            }

            return reasons.Count == 0 ? "in-game signal visible" : string.Join(", ", reasons);
        }
    }
}
