using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace HearthstoneAccessPatcher;
public class MainForm : Form
{
    private TextBox directoryBox = null!;
    private ComboBox cmbChannels = null!;
    private Button btnStart = null!;
    private FlowLayoutPanel mainPanel = null!;
    private OperationPanel operationPanel = null!;
    private bool isOperationInProgress = false;
    private FileStream? cachedPatchFile = null;
    private string? cachedSourceUrl = null;

    public MainForm()
    {
        InitializeComponent();
        LoadChannelsAsync();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            cachedPatchFile?.Dispose();
            cachedPatchFile = null;
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        cmbChannels = new ComboBox();
        btnStart = new Button();
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

        btnStart.Text = "Start";
        btnStart.AutoSize = true;
        btnStart.Margin = new Padding(0, 10, 0, 0); // Add top margin for spacing
        btnStart.Click += BtnStart_Click;

        mainPanel.Controls.Add(comboPanel);
        mainPanel.Controls.Add(btnStart);
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
            MessageBox.Show(this, "No release channel is  selected. Please select a channel.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        Source source = SourceManager.Sources[cmbChannels.SelectedIndex];

        // Mark operation as in progress
        isOperationInProgress = true;
        btnStart.Text = "Patching...";

        try
        {
            // Check if we need to download (first time or different source)
            if (cachedPatchFile == null || cachedSourceUrl != source.url)
            {
                // Clean up old cached file if exists
                cachedPatchFile?.Dispose();
                cachedPatchFile = null;

                operationPanel.LabelText = "Downloading...";
                Downloader downloader = new Downloader(source.url);

                downloader.ProgressChanged += (sender, progress) =>
                {
                    operationPanel.UpdateProgress(progress, "Downloading...");
                };
                cachedPatchFile = await downloader.Download();
                cachedSourceUrl = source.url;
            }

            operationPanel.LabelText = "Patching...";
            await Task.Yield();

            // Reset position to start of file for patching
            cachedPatchFile.Position = 0;
            Patcher.UnpackAndPatch(cachedPatchFile, directory);

            operationPanel.LabelText = "Done.";
            MessageBox.Show("Hearthstone patched successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // Clean up cached file after successful patch
            cachedPatchFile.Dispose();
            cachedPatchFile = null;
            cachedSourceUrl = null;
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
            cachedPatchFile?.Dispose();
            cachedPatchFile = null;
            cachedSourceUrl = null;
        }
        catch (InvalidDataException)
        {
            string message = "The downloaded patch file is invalid or corrupted. Please try again.";
            MessageBox.Show(this, message, "Invalid Patch File", MessageBoxButtons.OK, MessageBoxIcon.Error);
            operationPanel.LabelText = "Error: Invalid patch file.";
            // Invalid file - discard it and force re-download
            cachedPatchFile?.Dispose();
            cachedPatchFile = null;
            cachedSourceUrl = null;
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
            btnStart.Text = "Start";
        }
    }

    private async void LoadChannelsAsync()
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
