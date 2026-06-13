using System.Diagnostics;
using System.Globalization;
using DrawingPoint = System.Drawing.Point;

namespace GoblinFarmer
{
    public partial class frmMain
    {
        private GroupBox? portObsStatusGroup;
        private Label? lblObsStatus;
        private Label? lblObsRecordingStarted;
        private Label? lblObsRecordingEnded;

        private void PortInitializeObsStatusGroup()
        {
            if (!AppSettings.IsVsDebugProfile || portObsStatusGroup != null || portSettingsGroup == null)
            {
                return;
            }

            portObsStatusGroup = new GroupBox
            {
                Name = "grpObsStatus",
                Text = "OBS Status",
                Location = new DrawingPoint(portSettingsGroup.Left, portSettingsGroup.Bottom + 10),
                Size = new Size(portSettingsGroup.Width, 104),
                TabStop = false,
            };

            lblObsStatus = PortCreateObsStatusLabel("OBS Status: Unknown", 24);
            lblObsRecordingStarted = PortCreateObsStatusLabel("Recording Started: --", 50);
            lblObsRecordingEnded = PortCreateObsStatusLabel("Recording Ended: --", 76);

            portObsStatusGroup.Controls.Add(lblObsStatus);
            portObsStatusGroup.Controls.Add(lblObsRecordingStarted);
            portObsStatusGroup.Controls.Add(lblObsRecordingEnded);
            Controls.Add(portObsStatusGroup);
            PortUpdateObsStatusGroup();
        }

        private static Label PortCreateObsStatusLabel(string text, int y)
        {
            return new Label
            {
                AutoEllipsis = true,
                Location = new DrawingPoint(14, y),
                Name = text.Split(':')[0].Replace(" ", "", StringComparison.Ordinal),
                Size = new Size(510, 18),
                Text = text,
                UseMnemonic = false,
            };
        }

        private void PortUpdateObsStatusGroup()
        {
            if (lblObsStatus == null || lblObsRecordingStarted == null || lblObsRecordingEnded == null)
            {
                return;
            }

            ObsStatusSnapshot snapshot = PortReadObsStatusSnapshot();
            lblObsStatus.Text = $"OBS Status: {snapshot.Status}";
            lblObsRecordingStarted.Text = $"Recording Started: {PortFormatObsStatusTime(snapshot.LastRecordingStartedLocal)}";
            lblObsRecordingEnded.Text = $"Recording Ended: {PortFormatObsStatusTime(snapshot.LastRecordingEndedLocal)}";
        }

        private static ObsStatusSnapshot PortReadObsStatusSnapshot()
        {
            bool obsRunning = PortIsObsRunning();
            ObsMonitorLogSnapshot log = PortReadObsMonitorLog();
            bool recording = obsRunning &&
                log.LastRecordingStartedLocal.HasValue &&
                (!log.LastRecordingEndedLocal.HasValue || log.LastRecordingStartedLocal.Value > log.LastRecordingEndedLocal.Value);

            string status = recording
                ? "Recording"
                : obsRunning
                    ? "Running"
                    : "Closed";

            return new ObsStatusSnapshot(status, log.LastRecordingStartedLocal, log.LastRecordingEndedLocal);
        }

        private static bool PortIsObsRunning()
        {
            Process[] processes = Array.Empty<Process>();
            try
            {
                processes = Process.GetProcessesByName("obs64");
                return processes.Length > 0;
            }
            catch
            {
                return false;
            }
            finally
            {
                foreach (Process process in processes)
                {
                    process.Dispose();
                }
            }
        }

        private static ObsMonitorLogSnapshot PortReadObsMonitorLog()
        {
            string logPath = PortResolveObsMonitorLogPath();
            if (!File.Exists(logPath))
            {
                return new ObsMonitorLogSnapshot(null, null);
            }

            DateTime? lastStarted = null;
            DateTime? lastEnded = null;
            try
            {
                foreach (string line in File.ReadLines(logPath))
                {
                    if (line.Length < 23 ||
                        !DateTime.TryParseExact(
                            line[..23],
                            "yyyy-MM-dd HH:mm:ss.fff",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.AssumeLocal,
                            out DateTime timestamp))
                    {
                        continue;
                    }

                    if (line.Contains("OBS recording started.", StringComparison.OrdinalIgnoreCase))
                    {
                        lastStarted = timestamp;
                    }
                    else if (line.Contains("OBS recording stopped.", StringComparison.OrdinalIgnoreCase))
                    {
                        lastEnded = timestamp;
                    }
                }
            }
            catch
            {
                return new ObsMonitorLogSnapshot(lastStarted, lastEnded);
            }

            return new ObsMonitorLogSnapshot(lastStarted, lastEnded);
        }

        private static string PortResolveObsMonitorLogPath()
        {
            return Path.Combine(PortResolveProjectRootForObsStatus(), "Video Clip Review", "Auto Record Diablo.log");
        }

        private static string PortResolveProjectRootForObsStatus()
        {
            DirectoryInfo? directory = new(AppContext.BaseDirectory);
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "GoblinFarmer.csproj")) &&
                    Directory.Exists(Path.Combine(directory.FullName, "Video Clip Review")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "GoblinFarmer.csproj")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            return AppContext.BaseDirectory;
        }

        private static string PortFormatObsStatusTime(DateTime? timestamp)
        {
            return timestamp.HasValue
                ? timestamp.Value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
                : "--";
        }

        private sealed record ObsStatusSnapshot(
            string Status,
            DateTime? LastRecordingStartedLocal,
            DateTime? LastRecordingEndedLocal);

        private sealed record ObsMonitorLogSnapshot(
            DateTime? LastRecordingStartedLocal,
            DateTime? LastRecordingEndedLocal);
    }
}
