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
            catch (IOException)
            {
                // Ignore file deletion errors - file may be locked or already deleted
            }
            catch (UnauthorizedAccessException)
            {
                // Ignore permission errors during cleanup
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

        this.Text = "HearthstoneAccess 패처";
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
        lblPath.Text = "폴더 선택:";
        lblPath.AutoSize = true;

        directoryBox = new TextBox();
        directoryBox.Width = 250;
        directoryBox.ReadOnly = true;

        Button btnBrowse = new Button();
        btnBrowse.Text = "폴더 찾아보기...";
        btnBrowse.AutoSize = true;
        btnBrowse.Click += (s, e) =>
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "하스스톤이 설치된 폴더를 선택하세요";
                dialog.ShowNewFolderButton = false;
                dialog.OkRequiresInteraction = true;
                dialog.UseDescriptionForTitle = true;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string selectedPath = dialog.SelectedPath;
                    if (Patcher.IsHsDirectory(selectedPath))
                    {
                        directoryBox.Text = selectedPath;
                        Patcher.SaveHearthstonePath(selectedPath);
                    }
                    else
                    {
                        MessageBox.Show(this, "선택한 폴더는 올바른 하스스톤 설치 폴더가 아닙니다.", "잘못된 폴더", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
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
            Text = "채널 선택:",
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
            Text = "변경 내역을 바탕 화면에 저장",
            AutoSize = true,
            Checked = false,
            Margin = new Padding(0, 5, 0, 0),
            AccessibleName = "변경 내역을 바탕 화면에 저장",
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

        btnStart.Text = "하스스톤 패치";
        btnStart.AutoSize = true;
        btnStart.Click += BtnStart_Click;

        btnExit.Text = "종료";
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
            MessageBox.Show(this, "패치를 적용할 하스스톤 설치 위치를 자동으로 찾지 못했습니다. 다음 화면에서 '폴더 찾아보기...' 버튼을 눌러 하스스톤이 설치된 'Hearthstone' 폴더를 선택해 주세요.", "경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            MessageBox.Show(this, "지정된 경로가 올바른 하스스톤 설치 폴더가 아닙니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (cmbChannels.SelectedIndex < 0)
        {
            MessageBox.Show(this, "릴리스 채널이 선택되지 않았습니다. 채널을 선택해 주세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        Source source = SourceManager.Sources[cmbChannels.SelectedIndex];

        // Mark operation as in progress
        isOperationInProgress = true;
        btnStart.Text = "하스스톤 패치 중...";

        try
        {
            // Check if we need to download (first time or different source)
            if (cachedPatchFile == null || cachedSourceUrl != source.url)
            {
                // Clean up old cached file if exists
                CleanupCachedFile();

                operationPanel.LabelText = "다운로드 중...";
                Downloader downloader = new Downloader(source.url);

                downloader.ProgressChanged += (sender, progress) =>
                {
                    operationPanel.UpdateProgress(progress, "다운로드 중...");
                };
                cachedPatchFile = await downloader.Download();
                cachedPatchFilePath = downloader.TempFilePath;
                cachedSourceUrl = source.url;
            }

            operationPanel.LabelText = "패치 중...";

            // Reset position to start of file for patching
            cachedPatchFile.Position = 0;
            bool placeChangelog = chkPlaceChangelogOnDesktop.Checked;
            await Task.Run(() => Patcher.UnpackAndPatch(cachedPatchFile, directory, placeChangelog));

            operationPanel.LabelText = "완료.";
            Patcher.SaveHearthstonePath(directory);
            MessageBox.Show("하스스톤 패치가 완료되었습니다!", "성공", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // Clean up cached file after successful patch
            CleanupCachedFile();
        }
        catch (IOException)
        {
            string message = "게임을 패치하는 중 오류가 발생했습니다. 하스스톤이 종료되었는지 확인한 후 다시 시도해 주세요.";
            MessageBox.Show(this, message, "패치 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            operationPanel.LabelText = "패치 중 오류가 발생했습니다.";
            // Reset file position for retry
            if (cachedPatchFile != null)
            {
                cachedPatchFile.Position = 0;
            }
        }
        catch (UnauthorizedAccessException)
        {
            string message = "패치하는 중 권한이 거부되었습니다. 다음 사항을 확인해 주세요.\n\n" +
                           "• 하스스톤이 종료되어 있는지 확인하세요\n" +
                           "• 하스스톤 폴더에 쓰기 권한이 있는지 확인하세요\n" +
                           "• 이 프로그램을 관리자 권한으로 실행해 보세요";
            MessageBox.Show(this, message, "권한 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            operationPanel.LabelText = "오류: 권한이 거부되었습니다.";
            // Reset file position for retry
            if (cachedPatchFile != null)
            {
                cachedPatchFile.Position = 0;
            }
        }
        catch (System.Net.Http.HttpRequestException)
        {
            string message = "패치를 다운로드하지 못했습니다. 인터넷 연결을 확인한 후 다시 시도해 주세요.";
            MessageBox.Show(this, message, "다운로드 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            operationPanel.LabelText = "패치 다운로드 중 오류가 발생했습니다.";
            // Network error during download - discard cached file
            CleanupCachedFile();
        }
        catch (InvalidDataException)
        {
            string message = "다운로드한 패치 파일이 올바르지 않거나 손상되었습니다. 다시 시도해 주세요.";
            MessageBox.Show(this, message, "잘못된 패치 파일", MessageBoxButtons.OK, MessageBoxIcon.Error);
            operationPanel.LabelText = "오류: 잘못된 패치 파일입니다.";
            // Invalid file - discard it and force re-download
            CleanupCachedFile();
        }
        catch (Exception ex)
        {
            string message = "패치하는 중 예기치 않은 오류가 발생했습니다. 다시 시도해 주세요.\n\n" +
                           $"문제가 계속되면 다음 오류를 제보해 주세요:\n{ex.Message}";
            MessageBox.Show(this, message, "예기치 않은 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            operationPanel.LabelText = "예기치 않은 오류가 발생했습니다.";
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
            btnStart.Text = "하스스톤 패치";
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
