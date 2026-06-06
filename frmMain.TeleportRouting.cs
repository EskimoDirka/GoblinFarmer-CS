using System.Text.RegularExpressions;

namespace GoblinFarmer
{
    public partial class frmMain
    {
        private readonly Dictionary<string, string> portRouteNextTeleports = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Southern Highlands"] = "Northern Highlands",
            ["Northern Highlands"] = "The Weeping Hollow",
            ["The Weeping Hollow"] = "The Festering Woods",
            ["The Festering Woods"] = "Cathedral",
            ["Cathedral"] = "Royal Crypts",
            ["Royal Crypts"] = "City Of Caldeum",
            ["City Of Caldeum"] = "Ancient Waterway",
            ["Ancient Waterway"] = "Stinging Winds",
            ["Stinging Winds"] = "Battlefields",
            ["Battlefields"] = "Rakkis Crossing",
            ["Rakkis Crossing"] = "Pandemonium Fortress Level 1",
            ["Pandemonium Fortress Level 1"] = "Pandemonium Fortress Level 2",
        };

        private readonly Dictionary<string, string[]> portArrivalAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Ancient Waterway"] = ["Ancient Waterway", "Eastern Channel Level 1", "Eastern Channel Level 2", "Western Channel Level 1", "Western Channel Level 2"],
            ["Battlefields"] = ["Battlefields", "The Battlefields", "Fields of Slaughter", "Caverns of Frost Level 1", "Caverns of Frost Level 2"],
            ["Cathedral"] = ["Cathedral", "Cathedral Level 1", "Cathedral Level 2", "Cathedral Level 3"],
            ["City Of Caldeum"] = ["City Of Caldeum", "Gates of Caldeum", "Caldeum Bazaar", "Sewers of Caldeum", "Flooded Causeway", "Ruined Cistern"],
            ["Northern Highlands"] = ["Northern Highlands", "Highlands Cave", "Leoric's Hunting Grounds"],
            ["Pandemonium Fortress Level 1"] = ["Pandemonium Fortress Level 1", "Pandemonium Fortress Level 2"],
            ["Pandemonium Fortress Level 2"] = ["Pandemonium Fortress Level 1", "Pandemonium Fortress Level 2"],
            ["Royal Crypts"] = ["Royal Crypts", "The Royal Crypts"],
            ["Stinging Winds"] = ["Stinging Winds", "Black Canyon Mines"],
        };

        private string PortTeleportLocationForKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return "";
            }

            if (portTeleportButtons.TryGetValue(key, out Button? button))
            {
                return button.Text;
            }

            foreach (PortMapPoint point in portLocationCoords.Values)
            {
                if (PortLocationKey(point.Name) == key)
                {
                    return point.Name;
                }
            }

            return "";
        }

        private string PortLocationKey(string name)
        {
            return Regex.Replace(PortNormalizeLocation(name).ToLowerInvariant(), @"[^a-z0-9]+", " ").Trim();
        }

        private string PortTitleCase(string value)
        {
            return string.Join(" ", value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(part => char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant()));
        }

        private bool PortDiabloIsActive()
        {
            IntPtr diabloWindow = FindDiabloWindow();
            return diabloWindow != IntPtr.Zero && GetForegroundWindow() == diabloWindow;
        }

        private bool PortLocationMatches(string currentLocation, string targetLocation)
        {
            string currentKey = PortLocationKey(currentLocation);
            if (string.IsNullOrWhiteSpace(currentKey))
            {
                return false;
            }

            string targetKey = PortLocationKey(targetLocation);
            if (PortIsPandemoniumLocation(targetLocation))
            {
                return currentKey == targetKey;
            }

            if (portArrivalAliases.TryGetValue(targetLocation, out string[]? aliases))
            {
                return aliases.Select(PortLocationKey).Contains(currentKey);
            }

            return currentKey == targetKey;
        }

        private bool PortLocationMatchesForArrival(string currentLocation, string targetLocation)
        {
            string currentKey = PortLocationKey(currentLocation);
            if (string.IsNullOrWhiteSpace(currentKey))
            {
                return false;
            }

            string targetKey = PortLocationKey(targetLocation);
            if (targetKey == PortLocationKey("Ancient Waterway"))
            {
                return currentKey == targetKey;
            }

            if (PortIsWaypointExactMatchRequired(targetLocation))
            {
                return currentKey == targetKey;
            }

            return PortLocationMatches(currentLocation, targetLocation);
        }

        private bool PortLocationIsAlreadyAtTarget(string currentLocation, string targetLocation)
        {
            string currentKey = PortLocationKey(currentLocation);
            if (string.IsNullOrWhiteSpace(currentKey))
            {
                return false;
            }

            string targetKey = PortLocationKey(targetLocation);
            if (targetKey == PortLocationKey("Ancient Waterway"))
            {
                return currentKey == targetKey;
            }

            if (PortIsWaypointExactMatchRequired(targetLocation))
            {
                return currentKey == targetKey;
            }

            return PortLocationMatches(currentLocation, targetLocation);
        }

        private bool PortIsCaldeumRouteLocation(string location)
        {
            string key = PortLocationKey(location);
            return key == PortLocationKey("City Of Caldeum") ||
                key == PortLocationKey("Gates of Caldeum") ||
                key == PortLocationKey("Caldeum Bazaar") ||
                key == PortLocationKey("Sewers of Caldeum") ||
                key == PortLocationKey("Flooded Causeway") ||
                key == PortLocationKey("Ruined Cistern");
        }

        private bool PortIsCityOfCaldeumTitleAlias(string location)
        {
            string key = PortLocationKey(location);
            return key == PortLocationKey("Gates of Caldeum") ||
                key == PortLocationKey("Caldeum Bazaar") ||
                key == PortLocationKey("Sewers of Caldeum") ||
                key == PortLocationKey("Flooded Causeway");
        }

        private void PortLogCaldeumDetection(string rawLocation, string bestMatch, double confidence, string requestedLocation = "", string blockingLocation = "")
        {
            if (!PortIsCaldeumRouteLocation(rawLocation))
            {
                return;
            }

            string routeLocation = PortGetRouteLocationForDetectedLocation(rawLocation);
            string displayLocation = PortDetectedLocationDisplayName(rawLocation);
            string effectiveBlockingLocation = string.IsNullOrWhiteSpace(blockingLocation) ? rawLocation : blockingLocation;
            string effectiveRequestedLocation = string.IsNullOrWhiteSpace(requestedLocation) ? "Unknown" : requestedLocation;
            AppLogger.Info(
                $"CaldeumDetection: raw={PortLogField(PortDisplayLocation(rawLocation))}; " +
                $"display={PortLogField(PortDisplayLocation(displayLocation))}; " +
                $"blocking={PortLogField(PortDisplayLocation(effectiveBlockingLocation))}; " +
                $"requested={PortLogField(PortDisplayLocation(effectiveRequestedLocation))}; " +
                $"normalized={PortLogField(PortDisplayLocation(PortDetectedLocationDisplayName(routeLocation)))}; " +
                $"bestMatch={PortLogField(PortDisplayLocation(bestMatch))}; " +
                $"confidence={confidence:0.000}");
        }

        private static string PortDisplayLocation(string location)
        {
            return string.IsNullOrWhiteSpace(location) ? "Unknown" : location;
        }

        private bool PortTeleportFailsafeAllows(out string blockedLocation)
        {
            string rawLocation = PortGetConfirmedCurrentLocation();
            blockedLocation = rawLocation;
            string normalizedBlockedLocation = PortNormalizeBlockingLocation(rawLocation);

            AppLogger.Info($"BlockingLocationRaw={PortDisplayLocation(rawLocation)}; BlockingLocationNormalized={PortDisplayLocation(normalizedBlockedLocation)}");

            (bool blocked, string reason) = PortEvaluateTeleportBlock("", rawLocation, normalizedBlockedLocation, portLastTeleportSource);
            bool allowed = !blocked;
            PortRecordBlockingDecision("Unknown", rawLocation, normalizedBlockedLocation, blocked, allowed, reason);
            AppLogger.Info($"Teleport blocking decision: requested=Unknown; source={PortDisplayLocation(portLastTeleportSource)}; raw={PortDisplayLocation(rawLocation)}; normalized={PortDisplayLocation(normalizedBlockedLocation)}; display={PortDisplayLocation(PortDetectedLocationDisplayName(rawLocation))}; blocked={blocked}; allowed={allowed}; result={(allowed ? "Allowed" : "Blocked")}; reason={reason}");

            return allowed;
        }

        private bool PortTeleportFailsafeAllows(string targetLocation, out string blockedLocation)
        {
            if (portLastTeleportSource.Equals("Hotkey", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(portHotkeyFreshRawLocation))
            {
                string hotkeyRawLocation = portHotkeyFreshRawLocation;
                string hotkeyNormalizedLocation = PortNormalizeBlockingLocation(hotkeyRawLocation);
                blockedLocation = hotkeyRawLocation;

                AppLogger.Info(
                    $"Blocking location using hotkey fresh scan: raw={hotkeyRawLocation}; " +
                    $"normalized={hotkeyNormalizedLocation}; display={PortDetectedLocationDisplayName(hotkeyRawLocation)}; requested={targetLocation}");
                PortLogCaldeumDetection(hotkeyRawLocation, hotkeyRawLocation, 0, targetLocation, hotkeyRawLocation);

                (bool hotkeyBlocked, string hotkeyReason) = PortEvaluateTeleportBlock(targetLocation, hotkeyRawLocation, hotkeyNormalizedLocation, portLastTeleportSource);
                bool hotkeyAllowed = !hotkeyBlocked;
                PortRecordBlockingDecision(targetLocation, hotkeyRawLocation, hotkeyNormalizedLocation, hotkeyBlocked, hotkeyAllowed, hotkeyReason);

                AppLogger.Info(
                    $"Teleport blocking decision: requested={targetLocation}; source={PortDisplayLocation(portLastTeleportSource)}; raw={hotkeyRawLocation}; normalized={hotkeyNormalizedLocation}; display={PortDisplayLocation(PortDetectedLocationDisplayName(hotkeyRawLocation))}; blocked={hotkeyBlocked}; allowed={hotkeyAllowed}; result={(hotkeyAllowed ? "Allowed" : "Blocked")}; reason={hotkeyReason}");
                if (hotkeyAllowed)
                {
                    PortLogWesternChannelLevel2AllowsAncientWaterway(targetLocation, portLastTeleportSource, hotkeyRawLocation, hotkeyNormalizedLocation, hotkeyReason);
                    PortLogRouteDebugSummary(
                        "TeleportAllowed",
                        "Allowed",
                        targetLocation,
                        portLastTeleportSource,
                        hotkeyRawLocation,
                        hotkeyNormalizedLocation,
                        PortDetectedLocationDisplayName(hotkeyRawLocation),
                        hotkeyRawLocation,
                        hotkeyReason,
                        PortLikelyRouteExplanation(targetLocation, hotkeyRawLocation, hotkeyReason, false));
                }

                return hotkeyAllowed;
            }

            string refreshedLocation = PortRefreshBlockingLocationForTarget(targetLocation);

            if (!string.IsNullOrWhiteSpace(refreshedLocation))
            {
                string normalizedLocation = PortNormalizeBlockingLocation(refreshedLocation);
                AppLogger.Info(
                    $"Blocking location using latest title scan: raw={refreshedLocation}; " +
                    $"normalized={normalizedLocation}; display={PortDetectedLocationDisplayName(refreshedLocation)}");
                PortLogCaldeumDetection(refreshedLocation, refreshedLocation, 0, targetLocation, refreshedLocation);

                blockedLocation = refreshedLocation;

                (bool blocked, string reason) = PortEvaluateTeleportBlock(targetLocation, refreshedLocation, normalizedLocation, portLastTeleportSource);
                bool allowed = !blocked;
                PortRecordBlockingDecision(targetLocation, refreshedLocation, normalizedLocation, blocked, allowed, reason);

                AppLogger.Info(
                    $"Teleport blocking decision: requested={targetLocation}; source={PortDisplayLocation(portLastTeleportSource)}; raw={refreshedLocation}; normalized={normalizedLocation}; display={PortDisplayLocation(PortDetectedLocationDisplayName(refreshedLocation))}; blocked={blocked}; allowed={allowed}; result={(allowed ? "Allowed" : "Blocked")}; reason={reason}");
                if (allowed)
                {
                    PortLogWesternChannelLevel2AllowsAncientWaterway(targetLocation, portLastTeleportSource, refreshedLocation, normalizedLocation, reason);
                    PortLogRouteDebugSummary(
                        "TeleportAllowed",
                        "Allowed",
                        targetLocation,
                        portLastTeleportSource,
                        refreshedLocation,
                        normalizedLocation,
                        PortDetectedLocationDisplayName(refreshedLocation),
                        refreshedLocation,
                        reason,
                        PortLikelyRouteExplanation(targetLocation, refreshedLocation, reason, false));
                }

                return allowed;
            }

            string rawLocation = PortGetConfirmedCurrentLocation();
            blockedLocation = rawLocation;
            string normalizedRawLocation = PortNormalizeBlockingLocation(rawLocation);
            PortLogCaldeumDetection(rawLocation, rawLocation, 0, targetLocation, rawLocation);
            (bool fallbackBlocked, string fallbackReason) = PortEvaluateTeleportBlock(targetLocation, rawLocation, normalizedRawLocation, portLastTeleportSource);
            bool fallbackAllowed = !fallbackBlocked;
            PortRecordBlockingDecision(targetLocation, rawLocation, normalizedRawLocation, fallbackBlocked, fallbackAllowed, fallbackReason);
            AppLogger.Info($"Teleport blocking decision: requested={targetLocation}; source={PortDisplayLocation(portLastTeleportSource)}; raw={PortDisplayLocation(rawLocation)}; normalized={PortDisplayLocation(normalizedRawLocation)}; display={PortDisplayLocation(PortDetectedLocationDisplayName(rawLocation))}; blocked={fallbackBlocked}; allowed={fallbackAllowed}; result={(fallbackAllowed ? "Allowed" : "Blocked")}; reason={fallbackReason}");
            if (fallbackAllowed)
            {
                PortLogWesternChannelLevel2AllowsAncientWaterway(targetLocation, portLastTeleportSource, rawLocation, normalizedRawLocation, fallbackReason);
                PortLogRouteDebugSummary(
                    "TeleportAllowed",
                    "Allowed",
                    targetLocation,
                    portLastTeleportSource,
                    rawLocation,
                    normalizedRawLocation,
                    PortDetectedLocationDisplayName(rawLocation),
                    rawLocation,
                    fallbackReason,
                    PortLikelyRouteExplanation(targetLocation, rawLocation, fallbackReason, false));
            }
            return fallbackAllowed;
        }

        private void PortRecordBlockingDecision(string targetLocation, string rawLocation, string blockingLocation, bool blocked, bool allowed, string reason)
        {
            portLastBlockingDecision = $"Requested={PortDisplayLocation(targetLocation)}; Raw={PortDisplayLocation(rawLocation)}; Normalized={PortDisplayLocation(blockingLocation)}; Blocked={blocked}; Allowed={allowed}";
            portLastBlockingReason = PortDisplayLocation(reason);
            portLastBlockingRawLocation = PortDisplayLocation(rawLocation);
            portLastBlockingNormalizedLocation = PortDisplayLocation(blockingLocation);
            portLastBlockingDisplayLocation = PortDisplayLocation(PortDetectedLocationDisplayName(rawLocation));
        }

        private string PortRouteStateFields(
            string result,
            string requestedTarget,
            string source,
            string rawLocation,
            string normalizedLocation,
            string displayLocation,
            string blockingLocation,
            string blockingReason,
            string screenshotPath)
        {
            return $"result={PortLogField(PortDisplayLocation(result))}; requestedTarget={PortLogField(PortDisplayLocation(requestedTarget))}; source={PortLogField(PortDisplayLocation(source))}; rawLocation={PortLogField(PortDisplayLocation(rawLocation))}; normalizedLocation={PortLogField(PortDisplayLocation(normalizedLocation))}; displayLocation={PortLogField(PortDisplayLocation(displayLocation))}; blockingLocation={PortLogField(PortDisplayLocation(blockingLocation))}; currentButton={PortLogField(PortDisplayLocation(PortTeleportLocationForKey(portLastTeleportKey)))}; nextButton={PortLogField(PortDisplayLocation(PortTeleportLocationForKey(portQueuedTeleportKey)))}; queuedRetryTarget={PortLogField(PortDisplayLocation(PortTeleportLocationForKey(portQueuedRetryTeleportKey)))}; blockingReason={PortLogField(PortDisplayLocation(blockingReason))}; screenshotPath={PortLogField(PortDisplayLocation(screenshotPath))}";
        }

        private static string PortLogField(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "Unknown"
                : value.Replace(";", ",").Replace(Environment.NewLine, " ");
        }

        private void PortLogRouteFailureSummary(
            string eventType,
            string result,
            string requestedTarget,
            string source,
            string rawLocation,
            string normalizedLocation,
            string displayLocation,
            string blockingLocation,
            string blockingReason,
            string screenshotPath,
            string likelyExplanation)
        {
            AppLogger.Info($"RouteFailureSummary: event={PortLogField(eventType)}; {PortRouteStateFields(result, requestedTarget, source, rawLocation, normalizedLocation, displayLocation, blockingLocation, blockingReason, screenshotPath)}; likelyExplanation={PortLogField(likelyExplanation)}");
        }

        private void PortLogRouteDebugSummary(
            string eventType,
            string result,
            string requestedTarget,
            string source,
            string rawLocation,
            string normalizedLocation,
            string displayLocation,
            string blockingLocation,
            string blockingReason,
            string likelyExplanation)
        {
            AppLogger.Info($"RouteDebugSummary: event={PortLogField(eventType)}; {PortRouteStateFields(result, requestedTarget, source, rawLocation, normalizedLocation, displayLocation, blockingLocation, blockingReason, "")}; likelyExplanation={PortLogField(likelyExplanation)}");
        }

        private void PortLogWesternChannelLevel2AllowsAncientWaterway(string requestedTarget, string source, string rawLocation, string normalizedLocation, string reason)
        {
            if (!source.Equals("Hotkey", StringComparison.OrdinalIgnoreCase) ||
                PortLocationKey(requestedTarget) != PortLocationKey("Ancient Waterway") ||
                PortLocationKey(rawLocation) != PortLocationKey("Western Channel Level 2"))
            {
                return;
            }

            AppLogger.Info(
                $"WesternChannelLevel2AllowsAncientWaterway: rawLocation={PortLogField(PortDisplayLocation(rawLocation))}; " +
                $"normalizedLocation={PortLogField(PortDisplayLocation(normalizedLocation))}; " +
                $"displayLocation={PortLogField(PortDisplayLocation(PortDetectedLocationDisplayName(rawLocation)))}; " +
                $"blockingLocation={PortLogField(PortDisplayLocation(rawLocation))}; " +
                $"requestedTarget={PortLogField(PortDisplayLocation(requestedTarget))}; " +
                $"source={PortLogField(PortDisplayLocation(source))}; allowed=True; reason={PortLogField(PortDisplayLocation(reason))}");
        }

        private (bool Blocked, string Reason) PortEvaluateTeleportBlock(string targetLocation, string rawLocation, string blockingLocation, string source)
        {
            if (string.IsNullOrWhiteSpace(blockingLocation))
            {
                return (false, "no blocking location detected");
            }

            string targetKey = PortLocationKey(targetLocation);
            string rawKey = PortLocationKey(rawLocation);
            string blockingKey = PortLocationKey(blockingLocation);
            bool isHotkey = source.Equals("Hotkey", StringComparison.OrdinalIgnoreCase);

            if (isHotkey)
            {
                if (rawKey == PortLocationKey("Western Channel Level 1"))
                {
                    return (true, "Western Channel Level 1 is blocked for hotkey teleport routing.");
                }

                if (rawKey == PortLocationKey("Eastern Channel Level 1"))
                {
                    return (true, "Eastern Channel Level 1 is blocked for hotkey teleport routing.");
                }

                if (rawKey == PortLocationKey("Ancient Waterway"))
                {
                    if (targetKey == PortLocationKey("Stinging Winds"))
                    {
                        return (false, "Ancient Waterway allows hotkey teleportation to Stinging Winds");
                    }

                    return (true, "Ancient Waterway blocks hotkey teleport routing.");
                }

                if (rawKey == PortLocationKey("WhimsyDale"))
                {
                    return (true, "WhimsyDale blocks hotkey teleport routing.");
                }

                if (rawKey == PortLocationKey("Cave Of The Moon Clan Level 1"))
                {
                    return (true, "Cave Of The Moon Clan Level 1 blocks hotkey teleport routing.");
                }

                if (rawKey == PortLocationKey("Caverns of Frost Level 1"))
                {
                    return (true, "Caverns of Frost Level 1 blocks hotkey teleport routing.");
                }
            }

            if (targetKey == PortLocationKey("Royal Crypts"))
            {
                if (rawKey == PortLocationKey("Cathedral Level 3"))
                {
                    return (false, "Cathedral Level 3 allows Royal Crypts");
                }

                if (blockingKey == PortLocationKey("Cathedral") ||
                    rawKey == PortLocationKey("Cathedral Level 1") ||
                    rawKey == PortLocationKey("Cathedral Level 2") ||
                    rawKey == PortLocationKey("Leoric's Passage"))
                {
                    return (true, rawKey == PortLocationKey("Leoric's Passage")
                        ? "Leoric's Passage blocks Royal Crypts until Cathedral Level 3"
                        : "Cathedral blocks Royal Crypts until Cathedral Level 3");
                }

                return (false, "non-Cathedral location does not block Royal Crypts");
            }

            if (targetKey == PortLocationKey("Ancient Waterway"))
            {
                if (rawKey == PortLocationKey("Ruined Cistern"))
                {
                    return (false, "Ruined Cistern allows Ancient Waterway");
                }

                if (rawKey == PortLocationKey("Western Channel Level 2"))
                {
                    return (false, "Western Channel Level 2 allows teleport back to Ancient Waterway");
                }

                if (blockingKey == PortLocationKey("City Of Caldeum") ||
                    rawKey == PortLocationKey("Caldeum Bazaar") ||
                    rawKey == PortLocationKey("Gates of Caldeum") ||
                    rawKey == PortLocationKey("Sewers of Caldeum") ||
                    rawKey == PortLocationKey("Flooded Causeway"))
                {
                    return (true, "City Of Caldeum sublocation blocks Ancient Waterway; Ruined Cistern is required");
                }

                if (rawKey == PortLocationKey("Ancient Waterway"))
                {
                    return (true, "Already inside Ancient Waterway; Ancient Waterway button is blocked");
                }

                if (rawKey == PortLocationKey("Western Channel Level 1") ||
                    rawKey == PortLocationKey("Eastern Channel Level 1") ||
                    rawKey == PortLocationKey("Eastern Channel Level 2"))
                {
                    return (true, $"{rawLocation} does not allow teleporting to Ancient Waterway");
                }

                return (false, "location does not block Ancient Waterway");
            }

            if (targetKey == PortLocationKey("Stinging Winds"))
            {
                if (rawKey == PortLocationKey("Eastern Channel Level 2"))
                {
                    return (false, "Eastern Channel Level 2 allows hotkey teleportation to Stinging Winds");
                }

                if (rawKey == PortLocationKey("Western Channel Level 1") ||
                    rawKey == PortLocationKey("Eastern Channel Level 1"))
                {
                    return (true, $"{rawLocation} blocks Stinging Winds");
                }

                if (rawKey == PortLocationKey("Western Channel Level 2"))
                {
                    return (true, "Western Channel Level 2 should return to Ancient Waterway, not Stinging Winds");
                }

                return (false, "location does not block Stinging Winds");
            }

            if (targetKey == PortLocationKey("Battlefields"))
            {
                if (rawKey == PortLocationKey("Stinging Winds"))
                {
                    return (true, "Stinging Winds blocks Battlefields; Black Canyon Mines is required");
                }

                if (rawKey == PortLocationKey("Black Canyon Mines"))
                {
                    return (false, "Black Canyon Mines allows Battlefields");
                }

                return (false, "location does not block Battlefields");
            }

            return (false, "target has no blocking rule");
        }

        private string PortNormalizeBlockingLocation(string detectedLocation)
        {
            return PortNormalizeLocation(detectedLocation);
        }

        private void PortNotifyTeleportBlocked(string blockedLocation, string targetLocation, string source)
        {
            portAutomationBlockedByTeleportFailsafe = true;
            PortIncrementBlockedTeleports();
            string screenshotPath = PortCaptureFailureScreenshot("TeleportBlocked", "Teleport");

            string exactBlockedLocation = string.IsNullOrWhiteSpace(blockedLocation)
                ? "Unknown"
                : PortDetectedLocationDisplayName(blockedLocation).Trim();
            string normalizedBlockedLocation = PortNormalizeBlockingLocation(blockedLocation).Trim();

            AppLogger.Info($"Teleport blocked location: raw={PortDisplayLocation(blockedLocation)}; normalized={PortDisplayLocation(normalizedBlockedLocation)}; notificationDisplay={exactBlockedLocation}; target={targetLocation}; source={source}");
            PortLogRouteFailureSummary(
                "TeleportBlocked",
                "Blocked",
                targetLocation,
                source,
                blockedLocation,
                normalizedBlockedLocation,
                PortDetectedLocationDisplayName(blockedLocation),
                blockedLocation,
                portLastBlockingReason,
                screenshotPath,
                PortLikelyRouteExplanation(targetLocation, blockedLocation, portLastBlockingReason, false));
            PortSetAppStatus("Teleport Blocked");
            AddWorkflowStep($"Teleport blocked at {exactBlockedLocation}; target {targetLocation}");
            PortShowSplash($"Clear {exactBlockedLocation} before teleporting next.", 4500);
        }

        private string PortLikelyRouteExplanation(string requestedTarget, string rawLocation, string reason, bool cancelled)
        {
            if (cancelled)
            {
                return "Automation was cancelled before arrival confirmation completed; route state should remain on the previous confirmed location.";
            }

            string targetKey = PortLocationKey(requestedTarget);
            string rawKey = PortLocationKey(rawLocation);
            if (targetKey == PortLocationKey("Ancient Waterway") &&
                (rawKey == PortLocationKey("Western Channel Level 1") ||
                 rawKey == PortLocationKey("Western Channel Level 2") ||
                 rawKey == PortLocationKey("Eastern Channel Level 1") ||
                 rawKey == PortLocationKey("Eastern Channel Level 2")))
            {
                return "Ancient Waterway target confirmation requires the exact Ancient Waterway title; channel children are used for route/blocking decisions but should not complete the waypoint click.";
            }

            if (targetKey == PortLocationKey("Ancient Waterway") &&
                rawKey == PortLocationKey("Ruined Cistern"))
            {
                return "Ruined Cistern is the intended City Of Caldeum sublocation that allows Ancient Waterway.";
            }

            if (targetKey == PortLocationKey("Battlefields") &&
                rawKey == PortLocationKey("Black Canyon Mines"))
            {
                return "Black Canyon Mines is the Stinging Winds sublocation that allows Battlefields.";
            }

            if (rawKey == PortLocationKey("Cave Of The Moon Clan Level 1"))
            {
                return "Cave Of The Moon Clan Level 1 is not part of the farming route and blocks Teleport Next hotkey routing.";
            }

            if (rawKey == PortLocationKey("Caverns of Frost Level 1"))
            {
                return "Caverns of Frost Level 1 blocks Teleport Next hotkey routing; Caverns of Frost Level 2 may continue to Rakkis Crossing.";
            }

            if (!string.IsNullOrWhiteSpace(reason))
            {
                return reason;
            }

            return "Review requested target, detected raw location, blocking reason, and screenshot together.";
        }

        private void PortNotifyAlreadyHere(string currentLocation, string targetLocation, string source)
        {
            portTeleportAlreadyHereNotified = true;
            AppLogger.Info($"Already-here notification shown without route state change: current={currentLocation}; target={targetLocation}; source={source}; confirmed={PortDisplayLocation(portLastConfirmedLocation)}; display={PortDisplayLocation(PortGetButtonLocationForDetectedLocation(portLastConfirmedLocation))}; currentKey={portLastTeleportKey}; nextKey={portQueuedTeleportKey}");
            PortSetAppStatus("Already Here");
            AddWorkflowStep($"Already here: {currentLocation}; target {targetLocation}");
            PortShowSplash($"Already here: {currentLocation}", 3000);
        }

        private string PortGetConfirmedCurrentLocation()
        {
            return portLastConfirmedLocation;
        }

        private string PortRefreshBlockingLocationForTarget(string targetLocation)
        {
            if (PortLocationKey(targetLocation) != PortLocationKey("Ancient Waterway") &&
                PortLocationKey(targetLocation) != PortLocationKey("Stinging Winds") &&
                PortLocationKey(targetLocation) != PortLocationKey("Royal Crypts") &&
                PortLocationKey(targetLocation) != PortLocationKey("Battlefields"))
            {
                return "";
            }

            Dictionary<string, string> refreshTemplates = PortCurrentLocationTemplatesForNames(PortRouteBlockingRefreshNames());
            PortLocationDetectionResult result = PortDetectCurrentLocationFromTemplatesDetailed(refreshTemplates, $"blocking refresh for {targetLocation}", logPerf: true, PortBlockedLocationConfidence);
            if (!string.IsNullOrWhiteSpace(result.Detected))
            {
                AppLogger.Info($"Blocking location refreshed: previous={PortDisplayLocation(portLastConfirmedLocation)}; detected={result.Detected}; target={targetLocation}");
            }

            return result.Detected;
        }

        private string PortFreshHotkeyRouteLocationScan(string queuedTarget = "")
        {
            return PortFreshHotkeyRouteLocationScan(queuedTarget, out _);
        }

        private string PortFreshHotkeyRouteLocationScan(string queuedTarget, out PortLocationDetectionResult result)
        {
            HashSet<string> refreshNames = PortRouteBlockingRefreshNames();
            foreach (string targetName in PortLocationNamesForTarget(queuedTarget))
            {
                if (!string.IsNullOrWhiteSpace(targetName))
                {
                    refreshNames.Add(targetName);
                }
            }

            if (PortQueuedTargetLooksLikeRouteEnd(queuedTarget))
            {
                foreach (string routeName in PortRouteEndGuardScanNames())
                {
                    refreshNames.Add(routeName);
                }
            }

            Dictionary<string, string> refreshTemplates = PortCurrentLocationTemplatesForNames(refreshNames);
            result = PortDetectCurrentLocationFromTemplatesDetailed(refreshTemplates, "hotkey current-location scan", logPerf: true, PortBlockedLocationConfidence);
            if (!string.IsNullOrWhiteSpace(result.Detected))
            {
                AppLogger.Info($"Hotkey fresh current-location scan: raw={result.Detected}; normalized={PortNormalizeBlockingLocation(result.Detected)}; display={PortDisplayLocation(PortDetectedLocationDisplayName(result.Detected))}; confidence={result.BestConfidence:0.000}; previousConfirmed={PortDisplayLocation(portLastConfirmedLocation)}; queuedTarget={PortDisplayLocation(queuedTarget)}; templatesScanned={result.TemplateCount}");
                return result.Detected;
            }

            if (PortLocationKey(result.BestName) == PortLocationKey("Western Channel Level 2") && result.BestConfidence >= 0.50)
            {
                AppLogger.Info($"Hotkey fresh current-location scan low-confidence best preserved as raw sublocation: raw={result.BestName}; confidence={result.BestConfidence:0.000}; previousConfirmed={PortDisplayLocation(portLastConfirmedLocation)}; reason=best location is Western Channel Level 2, avoiding Ancient Waterway alias fallback");
                return result.BestName;
            }

            if (PortIsRawChannelOrWaterwaySublocation(portLastConfirmedLocation))
            {
                AppLogger.Info($"Hotkey fresh current-location scan no confident match; preserving previous raw sublocation for route decision: previousRaw={PortDisplayLocation(portLastConfirmedLocation)}; best={PortDisplayLocation(result.BestName)}; confidence={result.BestConfidence:0.000}; reason=no grouped Ancient Waterway alias fallback");
                return portLastConfirmedLocation;
            }

            AppLogger.Info($"Hotkey fresh current-location scan did not detect a route location: best={PortDisplayLocation(result.BestName)}; confidence={result.BestConfidence:0.000}; previousConfirmed={PortDisplayLocation(portLastConfirmedLocation)}; queuedTarget={PortDisplayLocation(queuedTarget)}; templatesScanned={result.TemplateCount}");
            return "";
        }

        private bool PortQueuedTargetLooksLikeRouteEnd(string queuedTarget)
        {
            return string.IsNullOrWhiteSpace(queuedTarget) ||
                PortLocationKey(queuedTarget) == PortLocationKey("Unknown");
        }

        private HashSet<string> PortRouteEndGuardScanNames()
        {
            HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);
            foreach ((string currentLocation, string nextLocation) in portRouteNextTeleports)
            {
                names.Add(currentLocation);
                names.Add(nextLocation);
            }

            return names;
        }

        private bool PortIsKnownRouteLocation(string location)
        {
            string key = PortLocationKey(PortGetRouteLocationForDetectedLocation(location));
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            return portRouteNextTeleports.Keys.Any(routeLocation => key == PortLocationKey(routeLocation)) ||
                portRouteNextTeleports.Values.Any(routeLocation => key == PortLocationKey(routeLocation));
        }

        private bool PortIsFinalRouteLocation(string location)
        {
            string routeLocation = PortGetRouteLocationForDetectedLocation(location);
            if (!PortIsKnownRouteLocation(routeLocation))
            {
                return false;
            }

            return !portRouteNextTeleports.ContainsKey(routeLocation);
        }

        private HashSet<string> PortRouteBlockingRefreshNames()
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "City Of Caldeum",
                "Gates of Caldeum",
                "Caldeum Bazaar",
                "Sewers of Caldeum",
                "Flooded Causeway",
                "Ruined Cistern",
                "Ancient Waterway",
                "Western Channel Level 1",
                "Western Channel Level 2",
                "Eastern Channel Level 1",
                "Eastern Channel Level 2",
                "Stinging Winds",
                "Black Canyon Mines",
                "Cathedral",
                "Cathedral Level 1",
                "Cathedral Level 2",
                "Cathedral Level 3",
                "Leoric's Passage",
                "Leoric's Hunting Grounds",
                "Highlands Cave",
                "WhimsyDale",
                "Cave Of The Moon Clan Level 1",
                "Cave Of The Moon Clan Level 2",
                "Caverns of Frost Level 1",
                "Caverns of Frost Level 2",
            };
        }

        private bool PortIsRawChannelOrWaterwaySublocation(string location)
        {
            string key = PortLocationKey(location);
            return key == PortLocationKey("Western Channel Level 1") ||
                key == PortLocationKey("Western Channel Level 2") ||
                key == PortLocationKey("Eastern Channel Level 1") ||
                key == PortLocationKey("Eastern Channel Level 2") ||
                key == PortLocationKey("Ancient Waterway");
        }

        private string PortGetRouteLocationForDetectedLocation(string detectedLocation)
        {
            string key = PortLocationKey(detectedLocation);
            if (string.IsNullOrWhiteSpace(key))
            {
                return "";
            }

            if (PortIsCaldeumRouteLocation(detectedLocation))
            {
                string cityButtonLocation = PortTeleportLocationForKey(PortLocationKey("City Of Caldeum"));
                return string.IsNullOrWhiteSpace(cityButtonLocation) ? "City of Caldeum" : cityButtonLocation;
            }

            if (key == PortLocationKey("Ancient Waterway"))
            {
                return "Ancient Waterway";
            }

            if (key == PortLocationKey("Stinging Winds") ||
                key == PortLocationKey("Black Canyon Mines"))
            {
                return "Stinging Winds";
            }

            if (key == PortLocationKey("Cave Of The Moon Clan Level 2"))
            {
                return "Southern Highlands";
            }

            return PortGetMappedLocationForDetectedLocation(detectedLocation);
        }

        private string PortGetButtonLocationForDetectedLocation(string detectedLocation)
        {
            return PortGetMappedLocationForDetectedLocation(detectedLocation);
        }

        private string PortDetectedLocationDisplayName(string detectedLocation)
        {
            string key = PortLocationKey(detectedLocation);
            if (string.IsNullOrWhiteSpace(key))
            {
                return "";
            }

            if (PortIsCaldeumRouteLocation(detectedLocation))
            {
                if (key == PortLocationKey("City Of Caldeum"))
                {
                    return "City of Caldeum";
                }

                return PortNormalizeLocation(detectedLocation);
            }

            return PortGetButtonLocationForDetectedLocation(detectedLocation);
        }

        private string PortGetMappedLocationForDetectedLocation(string detectedLocation)
        {
            string key = PortLocationKey(detectedLocation);
            if (string.IsNullOrWhiteSpace(key))
            {
                return "";
            }

            if (portTeleportButtons.ContainsKey(key))
            {
                return PortTeleportLocationForKey(key);
            }

            if (key == PortLocationKey("Cathedral Level 1") ||
                key == PortLocationKey("Cathedral Level 2") ||
                key == PortLocationKey("Cathedral Level 3"))
            {
                return "Cathedral";
            }

            if (PortIsCaldeumRouteLocation(detectedLocation))
            {
                return "City Of Caldeum";
            }

            if (key == PortLocationKey("Ancient Waterway") ||
                key == PortLocationKey("Western Channel Level 1") ||
                key == PortLocationKey("Western Channel Level 2") ||
                key == PortLocationKey("Eastern Channel Level 1") ||
                key == PortLocationKey("Eastern Channel Level 2"))
            {
                return "Ancient Waterway";
            }

            if (key == PortLocationKey("Cave Of The Moon Clan Level 2"))
            {
                return "Southern Highlands";
            }

            foreach (string routeLocation in portRouteNextTeleports.Keys)
            {
                if (key == PortLocationKey(routeLocation))
                {
                    return routeLocation;
                }

                if (portArrivalAliases.TryGetValue(routeLocation, out string[]? aliases) &&
                    aliases.Any(alias => key == PortLocationKey(alias)))
                {
                    return routeLocation;
                }
            }

            return detectedLocation;
        }

        private string PortFriendlyBlockedLocation(string blockedLocation)
        {
            string mappedBlockedLocation = PortGetButtonLocationForDetectedLocation(blockedLocation);
            if (PortLocationKey(mappedBlockedLocation) == PortLocationKey("Ancient Waterway"))
            {
                return "Ancient Waterway";
            }

            return blockedLocation;
        }

        private string PortNextTeleportForConfirmedLocation(string requestedLocation, string confirmedLocation)
        {
            string confirmedKey = PortLocationKey(confirmedLocation);
            string routeLocation = PortGetRouteLocationForDetectedLocation(confirmedLocation);
            if (string.IsNullOrWhiteSpace(routeLocation))
            {
                routeLocation = PortGetRouteLocationForDetectedLocation(requestedLocation);
            }

            if (confirmedKey == PortLocationKey("Eastern Channel Level 2"))
            {
                AppLogger.Info("Ancient Waterway child route: Eastern Channel Level 2 may continue to Stinging Winds");
                return "Stinging Winds";
            }

            if (confirmedKey == PortLocationKey("Western Channel Level 2"))
            {
                AppLogger.Info("Ancient Waterway child route: Western Channel Level 2 returns to Ancient Waterway");
                return "Ancient Waterway";
            }

            if (confirmedKey == PortLocationKey("Eastern Channel Level 1") ||
                confirmedKey == PortLocationKey("Western Channel Level 1"))
            {
                AppLogger.Info($"Ancient Waterway child route at {PortDisplayLocation(confirmedLocation)} is blocked for hotkey routing; next corrective target remains Ancient Waterway");
                return "Ancient Waterway";
            }

            return portRouteNextTeleports.TryGetValue(routeLocation, out string? nextLocation)
                ? nextLocation
                : "";
        }

        private string PortNormalizeLocation(string name)
        {
            name = Regex.Replace(name.Replace("\ufeff", "").Trim(), @"\s+", " ");
            Dictionary<string, string> aliases = new(StringComparer.OrdinalIgnoreCase)
            {
                ["battelfields"] = "Battlefields",
                ["city of caldeum"] = "City Of Caldeum",
                ["the battlefields"] = "Battlefields",
                ["the royal crypts"] = "Royal Crypts",
                ["whimsydale"] = "WhimsyDale",
            };

            return aliases.TryGetValue(name, out string? alias) ? alias : name;
        }
    }
}
