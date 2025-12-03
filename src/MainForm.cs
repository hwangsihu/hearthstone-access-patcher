using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace HearthstoneAccessPatcher;
public class MainForm : Form
{
    [DllImport("user32.dll")]
    private static extern void NotifyWinEvent(uint eventType, IntPtr hwnd, int idObject, int idChild);

    private const uint EVENT_OBJECT_STATECHANGE = 0x800A;
    private const int OBJID_CLIENT = -4;

    private TextBox directoryBox = null!;
    private ComboBox cmbChannels = null!;
    private Button btnStart = null!;
    private Button btnExit = null!;
    private FlowLayoutPanel mainPanel = null!;
    private OperationPanel operationPanel = null!;
    private CheckBox chkPlaceChangelogOnDesktop = null!;
    private bool isOperationInProgress = false;
    private FileStream? cachedPatchFile = null;
    private string? cachedPatchFilePath = null;
    private string? cachedSourceUrl = null;

    public MainForm()
    {
        InitializeComponent();
        _ = LoadChannelsAsync(); // Fire and forget with explicit discard
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            CleanupCachedFile();
        }
        base.Dispose(disposing);
    }

    private void CleanupCachedFile()
    {
        cachedPatchFile?.Dispose();
        cachedPatchFile = null;

        if (cachedPatchFilePath != null && File.Exists(cachedPatchFilePath))
        {
            try
            {
                File.Delete(cachedPatchFilePath);
            }
            catch
            {
                // Ignore deletion errors
            }
        }
        cachedPatchFilePath = null;
        cachedSourceUrl = null;
    }

    private void InitializeComponent()
    {
        cmbChannels = new ComboBox();
        btnStart = new Button();
        btnExit = new Button();
        mainPanel = new FlowLayoutPanel();

        this.Text = "HearthstoneAccessPatcher";
        this.Size = new System.Drawing.Size(600, 400);
        this.StartPosition = FormStartPosition.CenterScreen;

        mainPanel.Dock = DockStyle.Fill;
        mainPanel.FlowDirection = FlowDirection.TopDown;
        mainPanel.Padding = new Padding(10);
        mainPanel.AutoSize = true;
        mainPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;

        FlowLayoutPanel pickerPanel = new FlowLayoutPanel();
        pickerPanel.AutoSize = true;
        pickerPanel.FlowDirection = FlowDirection.LeftToRight;

        Label lblPath = new Label();
        lblPath.Text = "Select Folder:";
        lblPath.AutoSize = true;

        directoryBox = new TextBox();
        directoryBox.Width = 250;
        directoryBox.ReadOnly = true;

        Button btnBrowse = new Button();
        btnBrowse.Text = "Browse for Folder...";
        btnBrowse.AutoSize = true;
        btnBrowse.Click += (s, e) =>
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select the folder where Hearthstone is installed:";
                dialog.ShowNewFolderButton = false;
                dialog.OkRequiresInteraction = true;
                dialog.UseDescriptionForTitle = true;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    directoryBox.Text = dialog.SelectedPath;
                }
            }
        };

        pickerPanel.Controls.Add(lblPath);
        pickerPanel.Controls.Add(directoryBox);
        pickerPanel.Controls.Add(btnBrowse);

        mainPanel.Controls.Add(pickerPanel);


        FlowLayoutPanel comboPanel = new FlowLayoutPanel();
        comboPanel.FlowDirection = FlowDirection.LeftToRight;
        comboPanel.AutoSize = true;
        comboPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;

        Label lblSelect = new Label()
        {
            Text = "Select Channel:",
            AutoSize = true,
        };

        cmbChannels.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbChannels.Width = 200;

        PopulateChannels();

        comboPanel.Controls.Add(lblSelect);
        comboPanel.Controls.Add(cmbChannels);

        mainPanel.Controls.Add(comboPanel);

        // Add checkbox for placing changelog on desktop
        chkPlaceChangelogOnDesktop = new CheckBox
        {
            Text = "Place changelog on desktop",
            AutoSize = true,
            Checked = false,
            Margin = new Padding(0, 5, 0, 0),
            AccessibleName = "Place changelog on desktop",
            AccessibleRole = AccessibleRole.CheckButton
        };
        chkPlaceChangelogOnDesktop.CheckedChanged += (s, e) =>
        {
            // Notify screen readers of state change
            NotifyWinEvent(EVENT_OBJECT_STATECHANGE, chkPlaceChangelogOnDesktop.Handle, OBJID_CLIENT, 0);
        };
        mainPanel.Controls.Add(chkPlaceChangelogOnDesktop);

        FlowLayoutPanel buttonPanel = new FlowLayoutPanel();
        buttonPanel.FlowDirection = FlowDirection.LeftToRight;
        buttonPanel.AutoSize = true;
        buttonPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        buttonPanel.Margin = new Padding(0, 10, 0, 0); // Add top margin for spacing

        btnStart.Text = "Patch Hearthstone";
        btnStart.AutoSize = true;
        btnStart.Click += BtnStart_Click;

        btnExit.Text = "Exit";
        btnExit.AutoSize = true;
        btnExit.Margin = new Padding(5, 0, 0, 0); // Add left margin for spacing between buttons
        btnExit.Click += (s, e) => this.Close();

        buttonPanel.Controls.Add(btnStart);
        buttonPanel.Controls.Add(btnExit);

        mainPanel.Controls.Add(buttonPanel);
        operationPanel = new OperationPanel();

        mainPanel.Controls.Add(operationPanel);
        this.Controls.Add(mainPanel);
        string? path = Patcher.LocateHearthstone();
        if (!string.IsNullOrWhiteSpace(path))
        {
            directoryBox.Text = path;
        }
        else
        {
            MessageBox.Show(this, "Could not automatically locate where Hearthstone is installed to apply the patch. On the next screen, please press on the 'change' button and pick where you've installed Hearthstone by choosing the Hearthstone folder.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async void BtnStart_Click(object? sender, EventArgs e)
    {
        // Prevent multiple operations from running simultaneously
        if (isOperationInProgress)
        {
            return;
        }

        string directory = directoryBox.Text;
        if (!Patcher.IsHsDirectory(directory))
        {
            MessageBox.Show(this, "The provided path is not a valid directory to a hearthstone installation.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (cmbChannels.SelectedIndex < 0)
        {
            MessageBox.Show(this, "No release channel is selected. Please select a channel.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        Source source = SourceManager.Sources[cmbChannels.SelectedIndex];

        // Mark operation as in progress
        isOperationInProgress = true;
        btnStart.Text = "Patching Hearthstone...";

        try
        {
            // Check if we need to download (first time or different source)
            if (cachedPatchFile == null || cachedSourceUrl != source.url)
            {
                // Clean up old cached file if exists
                CleanupCachedFile();

                operationPanel.LabelText = "Downloading...";
                Downloader downloader = new Downloader(source.url);

                downloader.ProgressChanged += (sender, progress) =>
                {
                    operationPanel.UpdateProgress(progress, "Downloading...");
                };
                cachedPatchFile = await downloader.Download();
                cachedPatchFilePath = downloader.TempFilePath;
                cachedSourceUrl = source.url;
            }

            operationPanel.LabelText = "Patching...";

            // Reset position to start of file for patching
            cachedPatchFile.Position = 0;
            bool placeChangelog = chkPlaceChangelogOnDesktop.Checked;
            await Task.Run(() => Patcher.UnpackAndPatch(cachedPatchFile, directory, placeChangelog));

            operationPanel.LabelText = "Done.";
            MessageBox.Show("Hearthstone patched successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // Clean up cached file after successful patch
            CleanupCachedFile();
        }
        catch (IOException)
        {
            string message = "There was an error patching your game. Please make sure Hearthstone is closed and try again.";
            MessageBox.Show(this, message, "Patching Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            operationPanel.LabelText = "Error during patching.";
            // Reset file position for retry
            if (cachedPatchFile != null)
            {
                cachedPatchFile.Position = 0;
            }
        }
        catch (UnauthorizedAccessException)
        {
            string message = "Permission denied while patching. Please make sure:\n\n" +
                           "• Hearthstone is closed\n" +
                           "• You have permission to write to the Hearthstone folder\n" +
                           "• Try running this program as Administrator";
            MessageBox.Show(this, message, "Permission Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            operationPanel.LabelText = "Error: Permission denied.";
            // Reset file position for retry
            if (cachedPatchFile != null)
            {
                cachedPatchFile.Position = 0;
            }
        }
        catch (System.Net.Http.HttpRequestException)
        {
            string message = "Failed to download the patch. Please check your internet connection and try again.";
            MessageBox.Show(this, message, "Download Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            operationPanel.LabelText = "Error downloading patch.";
            // Network error during download - discard cached file
            CleanupCachedFile();
        }
        catch (InvalidDataException)
        {
            string message = "The downloaded patch file is invalid or corrupted. Please try again.";
            MessageBox.Show(this, message, "Invalid Patch File", MessageBoxButtons.OK, MessageBoxIcon.Error);
            operationPanel.LabelText = "Error: Invalid patch file.";
            // Invalid file - discard it and force re-download
            CleanupCachedFile();
        }
        catch (Exception ex)
        {
            string message = "An unexpected error occurred while patching. Please try again.\n\n" +
                           $"If the problem persists, please report this error:\n{ex.Message}";
            MessageBox.Show(this, message, "Unexpected Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            operationPanel.LabelText = "Unexpected error occurred.";
            // Reset file position for retry
            if (cachedPatchFile != null)
            {
                cachedPatchFile.Position = 0;
            }
        }
        finally
        {
            // Reset button state
            isOperationInProgress = false;
            btnStart.Text = "Patch Hearthstone";
        }
    }

    private async Task LoadChannelsAsync()
    {
        try
        {
            bool success = await SourceManager.LoadChannelsAsync();

            if (success && cmbChannels != null)
            {
                if (cmbChannels.InvokeRequired)
                {
                    cmbChannels.Invoke(new Action(() => PopulateChannels()));
                }
                else
                {
                    PopulateChannels();
                }
            }
        }
        catch (Exception ex)
        {
            // Log error but don't crash - fallback sources will be used
            System.Diagnostics.Debug.WriteLine($"Failed to load channels: {ex.Message}");
        }
    }

    private void PopulateChannels()
    {
        int previousSelection = cmbChannels.SelectedIndex;

        cmbChannels.Items.Clear();

        foreach (Source source in SourceManager.Sources)
        {
            cmbChannels.Items.Add($"{source.name} - {source.description}");
        }

        if (cmbChannels.Items.Count > 0)
        {
            if (previousSelection >= 0 && previousSelection < cmbChannels.Items.Count)
            {
                cmbChannels.SelectedIndex = previousSelection;
            }
            else
            {
                cmbChannels.SelectedIndex = 0;
            }
        }
    }
}
