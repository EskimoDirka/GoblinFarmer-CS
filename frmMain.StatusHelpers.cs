namespace GoblinFarmer
{
    public partial class frmMain
    {
        private void SetAppStatus(string status)
        {
            RunOnUiThread(() =>
            {
                string text = $"App Status: {status}";
                bool changed = lblAppStatus.Text != text;
                lblAppStatus.Text = text;
                if (changed)
                {
                    AppLogger.Info(text);
                }
            });
        }

        private void SetCombatStatus(string status)
        {
            RunOnUiThread(() =>
            {
                string text = $"Combat Status: {status}";
                bool changed = lblCombatStatus.Text != text;
                lblCombatStatus.Text = text;
                if (changed)
                {
                    AppLogger.Info(text);
                }
            });
        }

        private void SetDiabloStatus(string status)
        {
            RunOnUiThread(() =>
            {
                string text = $"Diablo Status: {status}";
                bool changed = lblDiabloStatus.Text != text;
                lblDiabloStatus.Text = text;
                if (changed)
                {
                    AppLogger.Info(text);
                }
            });
        }

        private void AddWorkflowStep(string message)
        {
            RunOnUiThread(() =>
            {
                portLastWorkflowStep = message;
                AppLogger.Info($"Workflow: {message}");

                TextBox? workflowTextBox = FindWorkflowTextBox(this);
                if (workflowTextBox == null)
                {
                    return;
                }

                workflowTextBox.AppendText($"{DateTime.Now:HH:mm:ss} {message}{Environment.NewLine}");
            });
        }

        private void RunOnUiThread(Action action)
        {
            if (IsDisposed || Disposing)
            {
                return;
            }

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(action);
                }
                catch (InvalidOperationException ex)
                {
                    AppLogger.Error("Unable to marshal UI update.", ex);
                }

                return;
            }

            action();
        }

        private static TextBox? FindWorkflowTextBox(Control parent)
        {
            foreach (Control control in parent.Controls)
            {
                if (control is TextBox textBox && IsWorkflowTextBox(textBox))
                {
                    return textBox;
                }

                TextBox? childMatch = FindWorkflowTextBox(control);
                if (childMatch != null)
                {
                    return childMatch;
                }
            }

            return null;
        }

        private static bool IsWorkflowTextBox(TextBox textBox)
        {
            string name = textBox.Name;
            return name.Equals("txtWorkflow", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("txtWorkflowLog", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("txtWorkflowSteps", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("txtLog", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("txtDebugLog", StringComparison.OrdinalIgnoreCase);
        }
    }
}
