namespace HearthstoneAccessPatcher;

/// <summary>
/// Application-wide constants
/// </summary>
public static class Constants
{
    /// <summary>
    /// API endpoint for fetching release channels
    /// </summary>
    public const string ApiEndpoint = "https://hearthstoneaccess.com/api/v1/release-channels";

    /// <summary>
    /// Directory within the patch archive containing files to extract
    /// </summary>
    public const string PatchDirectory = "patch/";

    /// <summary>
    /// Relative path to Hearthstone's main assembly for directory validation
    /// </summary>
    public const string HearthstoneAssemblyPath = "Hearthstone_Data/Managed/Assembly-CSharp.dll";

    /// <summary>
    /// File path for logging unhandled exceptions
    /// </summary>
    public const string ErrorLogFile = "errors.log";

    /// <summary>
    /// Buffer size for downloading files (64KB)
    /// </summary>
    public const int DownloadBufferSize = 65536;

    /// <summary>
    /// Timeout for API requests in seconds
    /// </summary>
    public const int ApiTimeoutSeconds = 10;
}
