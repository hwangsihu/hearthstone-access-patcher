using System;
using System.IO;
using System.IO.Compression;
namespace HearthstoneAccessPatcher;
static class Patcher
{

    public static bool IsHsDirectory(string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory)) return false;
        directory = Path.GetFullPath(directory);
        if (Directory.Exists(directory) && Path.GetFileName(directory) == "Hearthstone" && File.Exists(Path.Combine(directory, Constants.HearthstoneAssemblyPath)))
        {
            return true;
        }
        return false;
    }

    private static string ConfigFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "HearthstoneAccessPatcher",
        "hearthstone_path.txt"
    );

    public static void SaveHearthstonePath(string path)
    {
        string dir = Path.GetDirectoryName(ConfigFilePath)!;
        Directory.CreateDirectory(dir);
        File.WriteAllText(ConfigFilePath, path);
    }

    public static string? LocateHearthstone()
    {
        if (File.Exists(ConfigFilePath))
        {
            string? saved = File.ReadAllText(ConfigFilePath).Trim();
            if (IsHsDirectory(saved))
                return Path.GetFullPath(saved);
        }

        string programFiles = Environment.Is64BitOperatingSystem
            ? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            : Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        string path = Path.Combine(programFiles, "Hearthstone");
        if (IsHsDirectory(path))
        {
            return path;
        }
        return null;
    }

    static public void UnpackAndPatch(Stream downloaded, string directory, bool placeChangelogOnDesktop)
    {
        // Normalize the target directory for security validation
        string normalizedDirectory = Path.GetFullPath(directory);

        using ZipArchive archive = new ZipArchive(downloaded, ZipArchiveMode.Read, leaveOpen: true);
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            string entryPath = entry.FullName;

            // Check if this is the changelog.md file at the root of the archive
            if (entryPath.Equals("changelog.md", StringComparison.OrdinalIgnoreCase))
            {
                if (placeChangelogOnDesktop)
                {
                    string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    string changelogPath = Path.Combine(desktopPath, "HearthstoneAccess Changelog.md");

                    using (Stream entryStream = entry.Open())
                    using (FileStream fileStream = new(changelogPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        entryStream.CopyTo(fileStream);
                    }
                }
                continue;
            }

            if (string.IsNullOrWhiteSpace(entryPath) || entryPath.EndsWith('/') || !entryPath.StartsWith(Constants.PatchDirectory, StringComparison.OrdinalIgnoreCase)) continue;
            entryPath = entry.FullName.Substring(Constants.PatchDirectory.Length);
            entryPath = Path.Join(entryPath.Split('/'));
            entryPath = Path.GetFullPath(Path.Join(directory, entryPath));

            // Security check: Prevent path traversal attacks
            if (!entryPath.StartsWith(normalizedDirectory, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"Archive entry attempts to write outside target directory: {entry.FullName}");
            }

            string entryDirectory = Path.GetDirectoryName(entryPath)!;
            Directory.CreateDirectory(entryDirectory);
            using (FileStream fileStream = new(entryPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (Stream entryStream = entry.Open())
                {
                    entryStream.CopyTo(fileStream);
                }
            }
        }
    }
}
