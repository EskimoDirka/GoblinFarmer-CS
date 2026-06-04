using System.Drawing;

namespace GoblinFarmer
{
    internal static class CombatClickSafety
    {
        public const int ReferenceWidth = 2560;
        public const int ReferenceHeight = 1440;

        public static readonly CombatNoClickRegion[] PythonCombatNoClickRegions =
        [
            new("player_portrait", 0, 0, 166, 226),
            new("follower_portrait", 141, 4, 135, 159),
            new("chat_button", 6, 1270, 118, 134),
            new("left_bottom_hud", 430, 1220, 390, 220),
            new("skill_bar", 818, 1300, 930, 130),
            new("right_bottom_hud", 1690, 1220, 410, 220),
            new("right_bottom_menu", 2310, 1278, 214, 124),
            new("objectives_collapse", 2448, 472, 44, 62),
            new("top_right_buff_icons", 2237, 28, 115, 50),
        ];

        public static bool TryGetNoClickRegion(Point cursor, Rectangle diabloRect, out CombatNoClickRegion definition, out Rectangle scaledRegion, out int index)
        {
            for (int i = 0; i < PythonCombatNoClickRegions.Length; i++)
            {
                CombatNoClickRegion candidate = PythonCombatNoClickRegions[i];
                Rectangle scaled = candidate.ScaleTo(diabloRect);
                if (ContainsPythonBoundary(scaled, cursor))
                {
                    definition = candidate;
                    scaledRegion = scaled;
                    index = i;
                    return true;
                }
            }

            definition = default;
            scaledRegion = Rectangle.Empty;
            index = -1;
            return false;
        }

        public static bool CombatMouseClickIsSafe(Point cursor, Rectangle diabloRect)
        {
            return !TryGetNoClickRegion(cursor, diabloRect, out _, out _, out _);
        }

        public static bool ContainsPythonBoundary(Rectangle rectangle, Point point)
        {
            return point.X >= rectangle.Left &&
                point.X < rectangle.Right &&
                point.Y >= rectangle.Top &&
                point.Y < rectangle.Bottom;
        }
    }

    internal readonly record struct CombatNoClickRegion(string Name, int Left, int Top, int Width, int Height)
    {
        public Rectangle ScaleTo(Rectangle diabloRect)
        {
            double scaleX = diabloRect.Width / (double)CombatClickSafety.ReferenceWidth;
            double scaleY = diabloRect.Height / (double)CombatClickSafety.ReferenceHeight;
            return new Rectangle(
                diabloRect.Left + (int)Math.Round(Left * scaleX),
                diabloRect.Top + (int)Math.Round(Top * scaleY),
                (int)Math.Round(Width * scaleX),
                (int)Math.Round(Height * scaleY));
        }
    }

    internal static class DemonHunterCombatPolicy
    {
        public static bool KeyLoopContinuesWhenCursorUnsafe(bool combatRunning, string combatClass, bool diabloActive)
        {
            return combatRunning && combatClass == "demon_hunter" && diabloActive;
        }

        public static bool RightMouseRemainsHeldAfterSafeStart(bool startedFromSafeCursor, bool combatRunning, string combatClass, bool diabloActive)
        {
            return startedFromSafeCursor && combatRunning && combatClass == "demon_hunter" && diabloActive;
        }

        public static bool SafeWaitTimeoutStopsCombat(bool safeFoundWithinTimeout)
        {
            return !safeFoundWithinTimeout;
        }
    }

    internal static class CombatDiagnosticNames
    {
        public const string DemonHunterNoClickSuppressionOutcome = "Diagnostic";
        public const string DemonHunterNoClickSuppressionWorkflow = "Combat";
        public const string DemonHunterNoClickSuppressionAction = "DemonHunterNoClickSuppressionActive";
        public const string DemonHunterNoClickSuppressionEvent = "Diagnostic_Combat_DemonHunterNoClickSuppressionActive";
        public const string DemonHunterNoClickSuppressionSummary = "CombatDiagnosticSummary";
        public const string DemonHunterNoClickSuppressionScreenshotPrefix = "Diagnostic_Combat_DemonHunterNoClickSuppressionActive";
    }
}
