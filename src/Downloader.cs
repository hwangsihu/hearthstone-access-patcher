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
    /// Downloads the file from the URL to a MemoryStream
    /// </summary>
    /// <returns>
    /// A MemoryStream containing the downloaded content with position reset to 0.
    /// IMPORTANT: The caller is responsible for disposing the returned MemoryStream.
    /// </returns>
    /// <exception cref="HttpRequestException">Thrown when the HTTP request fails</exception>
    async public Task<MemoryStream> Download()
    {
        using HttpClient client = new HttpClient();
        using HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        length = response.Content.Headers.ContentLength ?? -1L;
        MemoryStream memoryStream = new MemoryStream();
        using var stream = await response.Content.ReadAsStreamAsync();
        byte[] buffer = new byte[Constants.DownloadBufferSize];
        int read;
        while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
        {
            downloaded += read;
            memoryStream.Write(new ReadOnlySpan<byte>(buffer, 0, read));
            ReportProgress();
        }

        memoryStream.Position = 0;
        return memoryStream;
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
