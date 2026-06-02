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

        private static string PortDisplayLocation(string location)
        {
            return string.IsNullOrWhiteSpace(location) ? "Unknown" : location;
        }

        private bool PortTeleportFailsafeAllows(out string blockedLocation)
        {
            string rawLocation = PortGetConfirmedCurrentLocation();
            blockedLocation = PortNormalizeBlockingLocation(rawLocation);

            AppLogger.Info($"BlockingLocationRaw={PortDisplayLocation(rawLocation)}; BlockingLocationNormalized={PortDisplayLocation(blockedLocation)}");

            (bool blocked, string reason) = PortEvaluateTeleportBlock("", rawLocation, blockedLocation);
            bool allowed = !blocked;
            PortRecordBlockingDecision("Unknown", rawLocation, blockedLocation, blocked, allowed, reason);
            AppLogger.Info($"Teleport blocking decision: requested=Unknown; raw={PortDisplayLocation(rawLocation)}; normalized={PortDisplayLocation(blockedLocation)}; blocked={blocked}; allowed={allowed}; reason={reason}");

            return allowed;
        }

        private bool PortTeleportFailsafeAllows(string targetLocation, out string blockedLocation)
        {
            string refreshedLocation = PortRefreshBlockingLocationForTarget(targetLocation);

            if (!string.IsNullOrWhiteSpace(refreshedLocation))
            {
                string normalizedLocation = PortNormalizeBlockingLocation(refreshedLocation);
                AppLogger.Info(
                    $"Blocking location using latest title scan: raw={refreshedLocation}; " +
                    $"normalized={normalizedLocation}; displayGroup={PortGetButtonLocationForDetectedLocation(refreshedLocation)}");

                blockedLocation = normalizedLocation;

                (bool blocked, string reason) = PortEvaluateTeleportBlock(targetLocation, refreshedLocation, normalizedLocation);
                bool allowed = !blocked;
                PortRecordBlockingDecision(targetLocation, refreshedLocation, blockedLocation, blocked, allowed, reason);

                AppLogger.Info(
                    $"Teleport blocking decision: requested={targetLocation}; raw={refreshedLocation}; normalized={blockedLocation}; blocked={blocked}; allowed={allowed}; reason={reason}");
                if (allowed)
                {
                    PortLogRouteDebugSummary(
                        "TeleportAllowed",
                        targetLocation,
                        portLastTeleportSource,
                        refreshedLocation,
                        blockedLocation,
                        PortGetButtonLocationForDetectedLocation(refreshedLocation),
                        refreshedLocation,
                        reason,
                        PortLikelyRouteExplanation(targetLocation, refreshedLocation, reason, false));
                }

                return allowed;
            }

            string rawLocation = PortGetConfirmedCurrentLocation();
            blockedLocation = PortNormalizeBlockingLocation(rawLocation);
            (bool fallbackBlocked, string fallbackReason) = PortEvaluateTeleportBlock(targetLocation, rawLocation, blockedLocation);
            bool fallbackAllowed = !fallbackBlocked;
            PortRecordBlockingDecision(targetLocation, rawLocation, blockedLocation, fallbackBlocked, fallbackAllowed, fallbackReason);
            AppLogger.Info($"Teleport blocking decision: requested={targetLocation}; raw={PortDisplayLocation(rawLocation)}; normalized={PortDisplayLocation(blockedLocation)}; blocked={fallbackBlocked}; allowed={fallbackAllowed}; reason={fallbackReason}");
            if (fallbackAllowed)
            {
                PortLogRouteDebugSummary(
                    "TeleportAllowed",
                    targetLocation,
                    portLastTeleportSource,
                    rawLocation,
                    blockedLocation,
                    PortGetButtonLocationForDetectedLocation(rawLocation),
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
        }

        private string PortRouteStateFields(
            string requestedTarget,
            string source,
            string rawLocation,
            string normalizedLocation,
            string displayLocation,
            string blockingLocation,
            string blockingReason,
            string screenshotPath)
        {
            return $"requestedTarget={PortLogField(PortDisplayLocation(requestedTarget))}; source={PortLogField(PortDisplayLocation(source))}; rawLocation={PortLogField(PortDisplayLocation(rawLocation))}; normalizedLocation={PortLogField(PortDisplayLocation(normalizedLocation))}; displayLocation={PortLogField(PortDisplayLocation(displayLocation))}; blockingLocation={PortLogField(PortDisplayLocation(blockingLocation))}; currentButton={PortLogField(PortDisplayLocation(PortTeleportLocationForKey(portLastTeleportKey)))}; nextButton={PortLogField(PortDisplayLocation(PortTeleportLocationForKey(portQueuedTeleportKey)))}; queuedRetryTarget={PortLogField(PortDisplayLocation(PortTeleportLocationForKey(portQueuedRetryTeleportKey)))}; blockingReason={PortLogField(PortDisplayLocation(blockingReason))}; screenshotPath={PortLogField(PortDisplayLocation(screenshotPath))}";
        }

        private static string PortLogField(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "Unknown"
                : value.Replace(";", ",").Replace(Environment.NewLine, " ");
        }

        private void PortLogRouteFailureSummary(
            string eventType,
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
            AppLogger.Info($"RouteFailureSummary: event={PortLogField(eventType)}; {PortRouteStateFields(requestedTarget, source, rawLocation, normalizedLocation, displayLocation, blockingLocation, blockingReason, screenshotPath)}; likelyExplanation={PortLogField(likelyExplanation)}");
        }

        private void PortLogRouteDebugSummary(
            string eventType,
            string requestedTarget,
            string source,
            string rawLocation,
            string normalizedLocation,
            string displayLocation,
            string blockingLocation,
            string blockingReason,
            string likelyExplanation)
        {
            AppLogger.Info($"RouteDebugSummary: event={PortLogField(eventType)}; {PortRouteStateFields(requestedTarget, source, rawLocation, normalizedLocation, displayLocation, blockingLocation, blockingReason, "")}; likelyExplanation={PortLogField(likelyExplanation)}");
        }

        private (bool Blocked, string Reason) PortEvaluateTeleportBlock(string targetLocation, string rawLocation, string blockingLocation)
        {
            if (string.IsNullOrWhiteSpace(blockingLocation))
            {
                return (false, "no blocking location detected");
            }

            string targetKey = PortLocationKey(targetLocation);
            string rawKey = PortLocationKey(rawLocation);
            string blockingKey = PortLocationKey(blockingLocation);

            if (targetKey == PortLocationKey("Royal Crypts"))
            {
                if (rawKey == PortLocationKey("Cathedral Level 3"))
                {
                    return (false, "Cathedral Level 3 allows Royal Crypts");
                }

                if (blockingKey == PortLocationKey("Cathedral") ||
                    rawKey == PortLocationKey("Cathedral Level 1") ||
                    rawKey == PortLocationKey("Cathedral Level 2"))
                {
                    return (true, "Cathedral blocks Royal Crypts until Cathedral Level 3");
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
                if (rawKey == PortLocationKey("Eastern Channel Level 2") ||
                    rawKey == PortLocationKey("Ancient Waterway"))
                {
                    return (false, $"{rawLocation} allows Stinging Winds");
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
            string normalized = PortNormalizeLocation(detectedLocation);
            string key = PortLocationKey(normalized);

            if (key == PortLocationKey("Gates of Caldeum"))
            {
                return "City Of Caldeum";
            }

            return normalized;
        }

        private void PortNotifyTeleportBlocked(string blockedLocation, string targetLocation, string source)
        {
            portAutomationBlockedByTeleportFailsafe = true;
            PortIncrementBlockedTeleports();
            string screenshotPath = PortCaptureFailureScreenshot("TeleportBlocked", "Teleport");

            string exactBlockedLocation = string.IsNullOrWhiteSpace(blockedLocation)
                ? "Unknown"
                : PortNormalizeBlockingLocation(blockedLocation).Trim();

            AppLogger.Info($"Teleport blocked location: raw={PortDisplayLocation(blockedLocation)}; normalized={exactBlockedLocation}; target={targetLocation}; source={source}");
            PortLogRouteFailureSummary(
                "TeleportBlocked",
                targetLocation,
                source,
                blockedLocation,
                exactBlockedLocation,
                PortGetButtonLocationForDetectedLocation(blockedLocation),
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

            HashSet<string> refreshNames = new(StringComparer.OrdinalIgnoreCase)
            {
                "City Of Caldeum",
                "Gates of Caldeum",
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
            };

            Dictionary<string, string> refreshTemplates = PortCurrentLocationTemplatesForNames(refreshNames);
            PortLocationDetectionResult result = PortDetectCurrentLocationFromTemplatesDetailed(refreshTemplates, $"blocking refresh for {targetLocation}", logPerf: true, PortBlockedLocationConfidence);
            if (!string.IsNullOrWhiteSpace(result.Detected))
            {
                AppLogger.Info($"Blocking location refreshed: previous={PortDisplayLocation(portLastConfirmedLocation)}; detected={result.Detected}; target={targetLocation}");
            }

            return result.Detected;
        }

        private string PortGetRouteLocationForDetectedLocation(string detectedLocation)
        {
            string key = PortLocationKey(detectedLocation);
            if (string.IsNullOrWhiteSpace(key))
            {
                return "";
            }

            if (key == PortLocationKey("City Of Caldeum") ||
                key == PortLocationKey("Gates of Caldeum") ||
                key == PortLocationKey("Ruined Cistern") ||
                key == PortLocationKey("Sewers of Caldeum") ||
                key == PortLocationKey("Flooded Causeway"))
            {
                return "City Of Caldeum";
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

            return PortGetMappedLocationForDetectedLocation(detectedLocation);
        }

        private string PortGetButtonLocationForDetectedLocation(string detectedLocation)
        {
            return PortGetMappedLocationForDetectedLocation(detectedLocation);
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

            if (key == PortLocationKey("City Of Caldeum") ||
                key == PortLocationKey("Gates of Caldeum") ||
                key == PortLocationKey("Ruined Cistern") ||
                key == PortLocationKey("Sewers of Caldeum") ||
                key == PortLocationKey("Flooded Causeway"))
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

            if (PortLocationKey(blockedLocation) == PortLocationKey("Gates of Caldeum") &&
                portLastTeleportKey == PortLocationKey("City Of Caldeum"))
            {
                return "City of Caldeum";
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
                AppLogger.Info($"Ancient Waterway child route at {PortDisplayLocation(confirmedLocation)}; next teleport target remains Stinging Winds and blocking decides whether it is allowed");
                return "Stinging Winds";
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
            };

            return aliases.TryGetValue(name, out string? alias) ? alias : name;
        }
    }
}
