using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
namespace HearthstoneAccessPatcher;

/// <summary>
/// Downloads files from a URL with progress tracking
/// </summary>
public class Downloader
{
    /// <summary>
    /// Event raised when download progress changes. The integer parameter represents the progress percentage (0-100).
    /// </summary>
    public event EventHandler<int>? ProgressChanged;

    /// <summary>
    /// Gets the current download progress as a percentage (0-100)
    /// </summary>
    public int Progress { get; private set; }

    /// <summary>
    /// Gets the path to the temporary file created by the last Download() call
    /// </summary>
    public string? TempFilePath { get; private set; }

    private int lastPercent;
    private readonly string url;
    private long downloaded;
    private long length;

    /// <summary>
    /// Initializes a new instance of the Downloader class
    /// </summary>
    /// <param name="url">The URL to download from</param>
    public Downloader(string url)
    {
        this.url = url;
    }

    /// <summary>
    /// Downloads the file from the URL to a temporary FileStream
    /// </summary>
    /// <returns>
    /// A FileStream containing the downloaded content with position reset to 0.
    /// The file is created in the system's temporary directory.
    /// IMPORTANT: The caller is responsible for:
    /// 1. Disposing the returned FileStream
    /// 2. Deleting the temporary file (path available in TempFilePath property)
    /// </returns>
    /// <exception cref="HttpRequestException">Thrown when the HTTP request fails</exception>
    async public Task<FileStream> Download()
    {
        using HttpClient client = new HttpClient();
        using HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        length = response.Content.Headers.ContentLength ?? -1L;

        // Create a temporary file
        TempFilePath = Path.GetTempFileName();
        FileStream fileStream = new FileStream(
            TempFilePath,
            FileMode.Create,
            FileAccess.ReadWrite,
            FileShare.None,
            Constants.DownloadBufferSize);

        using var stream = await response.Content.ReadAsStreamAsync();
        byte[] buffer = new byte[Constants.DownloadBufferSize];
        int read;
        while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
        {
            downloaded += read;
            await fileStream.WriteAsync(buffer, 0, read);
            ReportProgress();
        }

        fileStream.Position = 0;
        return fileStream;
    }

    private void ReportProgress()
    {
        if (length > 0 && downloaded > 0)
        {
            int percent = (int)((double)downloaded / (double)length * 100);
            if (percent == lastPercent)
            {
                return;
            }
            Progress = percent;
            ProgressChanged?.Invoke(this, Progress);
            lastPercent = percent;
        }
    }




}
