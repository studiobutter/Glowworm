using System.Text;

namespace Glowworm.Core;

/// <summary>
/// Detects Xbox for PC game installations by scanning
/// .GamingRoot files at drive roots.
/// </summary>
public static class XboxGameDetector
{
    private static readonly byte[] GamingRootMagic = { 0x52, 0x47, 0x42, 0x58 }; // "RGBX"

    /// <summary>
    /// Scans all fixed/removable drives for .GamingRoot files and
    /// returns the install path for ZZZ Xbox if found.
    /// </summary>
    public static string? GetZZZXboxInstallPath()
    {
        foreach (var drive in DriveInfo.GetDrives())
        {
            if (drive.DriveType is not (DriveType.Fixed or DriveType.Removable))
                continue;

            if (!drive.IsReady)
                continue;

            string gamingRootPath = Path.Combine(drive.RootDirectory.FullName, ".GamingRoot");
            if (!File.Exists(gamingRootPath))
                continue;

            string? xboxRoot = ParseGamingRoot(gamingRootPath, drive.RootDirectory.FullName);
            if (xboxRoot is null)
                continue;

            // ZZZ Xbox game content lives under:
            //   [xboxRoot]\ZenlessZoneZero\Content\
            string zzzPath = Path.Combine(xboxRoot, "ZenlessZoneZero", "Content");
            if (Directory.Exists(zzzPath))
            {
                return zzzPath;
            }
        }
        return null;
    }

    /// <summary>
    /// Parses a .GamingRoot file and returns the absolute Xbox games root.
    /// Format: [4-byte magic "RGBX"][4-byte version][UTF-16 LE null-terminated path]
    /// </summary>
    private static string? ParseGamingRoot(string filePath, string driveRoot)
    {
        try
        {
            byte[] data = File.ReadAllBytes(filePath);
            if (data.Length < 8)
                return null;

            // Verify magic bytes
            if (!data.AsSpan(0, 4).SequenceEqual(GamingRootMagic))
                return null;

            // Skip version/flags (4 bytes), read UTF-16 LE string
            int strStart = 8;
            if (strStart >= data.Length)
                return null;

            // Find null terminator (two consecutive 0x00 bytes on even boundary)
            int strEnd = strStart;
            while (strEnd + 1 < data.Length)
            {
                if (data[strEnd] == 0 && data[strEnd + 1] == 0)
                    break;
                strEnd += 2;
            }

            if (strEnd <= strStart)
                return null;

            string relativePath = Encoding.Unicode.GetString(data, strStart, strEnd - strStart);
            relativePath = relativePath.TrimEnd('\0').Trim();

            if (string.IsNullOrWhiteSpace(relativePath))
                return null;

            string fullPath = Path.Combine(driveRoot, relativePath);
            return Directory.Exists(fullPath) ? fullPath : null;
        }
        catch
        {
            return null;
        }
    }
}
