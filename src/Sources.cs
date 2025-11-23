using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HearthstoneAccessPatcher;

public struct Source
{
    public readonly string name;
    public readonly string description;
    public readonly string url;

    public Source(string name, string description, string url)
    {
        this.name = name;
        this.description = description;
        this.url = url;
    }
}

public class ReleaseChannel
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("latest_release")]
    public LatestRelease? LatestRelease { get; set; }
}

public class LatestRelease
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("hearthstone_version")]
    public string HearthstoneVersion { get; set; } = string.Empty;

    [JsonPropertyName("accessibility_version")]
    public int AccessibilityVersion { get; set; }

    [JsonPropertyName("changelog")]
    public string Changelog { get; set; } = string.Empty;

    [JsonPropertyName("upload_time")]
    public string UploadTime { get; set; } = string.Empty;
}

public static class SourceManager
{
    private const string API_ENDPOINT = "https://hearthstoneaccess.com/api/v1/release-channels";

    private static Source[] FallbackSources = new Source[]
    {
        new Source("stable", "Recommended for most users", "https://hearthstoneaccess.com/files/pre_patch.zip"),
    };

    public static Source[] Sources { get; private set; } = FallbackSources;

    public static async Task<bool> LoadChannelsAsync()
    {
        try
        {
            using HttpClient client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            string json = await client.GetStringAsync(API_ENDPOINT);
            var channels = JsonSerializer.Deserialize<ReleaseChannel[]>(json);

            if (channels == null || channels.Length == 0)
            {
                return false;
            }

            var sources = new List<Source>();
            foreach (var channel in channels)
            {
                if (channel.LatestRelease?.Url != null)
                {
                    sources.Add(new Source(
                        channel.Name,
                        channel.Description,
                        channel.LatestRelease.Url
                    ));
                }
            }

            if (sources.Count > 0)
            {
                Sources = sources.ToArray();
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}
