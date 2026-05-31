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
            ["Battlefields"] = "The Bridge of Korsikk",
            ["The Bridge of Korsikk"] = "Rakkis Crossing",
            ["Rakkis Crossing"] = "Pandemonium Fortress Level 1",
            ["Pandemonium Fortress Level 1"] = "Pandemonium Fortress Level 2",
        };

        private readonly HashSet<string> portTeleportBlockedLocations = new(StringComparer.OrdinalIgnoreCase)
        {
            "Caldeum Bazaar",
            "Caverns of Frost Level 1",
            "Cave Of The Moon Clan Level 1",
            "Cathedral Level 1",
            "Cathedral Level 2",
            "City Of Caldeum",
            "Eastern Channel Level 1",
            "Flooded Causeway",
            "Gates of Caldeum",
            "Leoric's Passage",
            "Sewers of Caldeum",
            "Stinging Winds",
            "Western Channel Level 1",
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

        private bool PortLocationIsAlreadyAtTarget(string currentLocation, string targetLocation)
        {
            string currentKey = PortLocationKey(currentLocation);
            if (string.IsNullOrWhiteSpace(currentKey))
            {
                return false;
            }

            string targetKey = PortLocationKey(targetLocation);
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
            blockedLocation = PortGetConfirmedCurrentLocation();
            AppLogger.Info($"BlockingLocation={PortDisplayLocation(blockedLocation)}");
            return string.IsNullOrWhiteSpace(blockedLocation) || !portTeleportBlockedLocations.Contains(blockedLocation);
        }

        private bool PortTeleportFailsafeAllows(string targetLocation, out string blockedLocation)
        {
            string refreshedLocation = PortRefreshBlockingLocationForTarget(targetLocation);
            if (!string.IsNullOrWhiteSpace(refreshedLocation))
            {
                portLastConfirmedLocation = refreshedLocation;
            }

            return PortTeleportFailsafeAllows(out blockedLocation);
        }

        private void PortNotifyTeleportBlocked(string blockedLocation, string targetLocation, string source)
        {
            portAutomationBlockedByTeleportFailsafe = true;
            PortIncrementBlockedTeleports();
            PortCaptureDebugScreenshot("TeleportBlocked");
            string displayBlockedLocation = PortFriendlyBlockedLocation(blockedLocation);
            AppLogger.Info($"Teleport blocked location: {blockedLocation}; display={displayBlockedLocation}; target={targetLocation}; source={source}");
            PortSetAppStatus("Teleport Blocked");
            AddWorkflowStep($"Teleport blocked at {displayBlockedLocation}; target {targetLocation}");
            PortShowSplash($"Clear {displayBlockedLocation} before teleporting next.", 4500);
        }

        private string PortGetConfirmedCurrentLocation()
        {
            return portLastConfirmedLocation;
        }

        private string PortRefreshBlockingLocationForTarget(string targetLocation)
        {
            if (PortLocationKey(targetLocation) != PortLocationKey("Ancient Waterway"))
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
            if (PortLocationKey(blockedLocation) == PortLocationKey("Gates of Caldeum") &&
                portLastTeleportKey == PortLocationKey("City Of Caldeum"))
            {
                return "City of Caldeum";
            }

            return blockedLocation;
        }

        private string PortNextTeleportForConfirmedLocation(string requestedLocation, string confirmedLocation)
        {
            string routeLocation = PortGetRouteLocationForDetectedLocation(confirmedLocation);
            if (string.IsNullOrWhiteSpace(routeLocation))
            {
                routeLocation = PortGetRouteLocationForDetectedLocation(requestedLocation);
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
