using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.Windows.Forms.Automation;
namespace HearthstoneAccessPatcher;
public class OperationPanel : FlowLayoutPanel
{
    private ProgressBar progressBar;
    private Label label;

    public OperationPanel()
    {
        this.FlowDirection = FlowDirection.TopDown;
        this.AutoSize = true;
        this.WrapContents = false;

        progressBar = new ProgressBar
        {
            Width = 50,
            Height = 20
        };

        label = new Label
        {
            Text = "Operation Status",
            AutoSize = true,
            Visible = false  // Hidden - status announced via automation notifications only
        };

        this.Controls.Add(progressBar);
        this.Controls.Add(label);
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ProgressBar ProgressBar
    {
        get { return progressBar; }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string LabelText
    {
        get { return label.Text; }
        set
        {
            if (label.Text != value)
            {
                label.Text = value;
                label.AccessibilityObject.RaiseAutomationNotification(AutomationNotificationKind.Other, AutomationNotificationProcessing.ImportantMostRecent, value);
            }
        }
    }

    public void UpdateProgress(int progressValue, string? text = null)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() => UpdateProgress(progressValue, text)));
            return;
        }

        if (text != null)
        {
            LabelText = text;
        }

        // Clamp progress value to valid range
        int clampedValue = Math.Clamp(progressValue, progressBar.Minimum, progressBar.Maximum);
        if (clampedValue != progressBar.Value)
        {
            progressBar.Value = clampedValue;
        }
    }
}
