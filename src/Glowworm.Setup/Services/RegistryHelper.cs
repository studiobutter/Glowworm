using Microsoft.Win32;

namespace Glowworm.Setup.Services;

public static class RegistryHelper
{

    public static void WriteUninstallInfo(string folder, string version, long size)
    {
        string exe = Path.Combine(folder, "Glowworm.exe");
        string setupExe = Path.Combine(folder, "Glowworm.Setup.exe");
        using var subkey = Registry.LocalMachine.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\Glowworm");
        subkey.SetValue("Publisher", "Scighost", RegistryValueKind.String);
        subkey.SetValue("DisplayName", "Glowworm", RegistryValueKind.String);
        subkey.SetValue("DisplayIcon", exe, RegistryValueKind.String);
        subkey.SetValue("DisplayVersion", version, RegistryValueKind.String);
        subkey.SetValue("InstallLocation", folder, RegistryValueKind.String);
        subkey.SetValue("EstimatedSize", (int)(size / 1024), RegistryValueKind.DWord);
        subkey.SetValue("InstallDate", $"{DateTime.Now:yyyyMMdd}", RegistryValueKind.String);
        subkey.SetValue("UninstallString", $"""
            "{setupExe}" uninstall
            """, RegistryValueKind.String);
    }


    public static void DeleteUninstallInfo()
    {
        Registry.LocalMachine.DeleteSubKeyTree(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\Glowworm", false);
    }


    public static void WriteUrlProtocol(string folder)
    {
        Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\Glowworm", false);
        Registry.SetValue(@"HKEY_LOCAL_MACHINE\Software\Classes\Glowworm", "", "URL:Glowworm Protocol");
        Registry.SetValue(@"HKEY_LOCAL_MACHINE\Software\Classes\Glowworm", "URL Protocol", "");
        Registry.SetValue(@"HKEY_LOCAL_MACHINE\Software\Classes\Glowworm\DefaultIcon", "", "Glowworm.exe,1");
        Registry.SetValue(@"HKEY_LOCAL_MACHINE\Software\Classes\Glowworm\Shell\Open\Command", "", $"""
            "{Path.Combine(folder, "Glowworm.exe")}" "%1"
            """);
    }


    public static void DeleteUrlProtocol()
    {
        Registry.LocalMachine.DeleteSubKeyTree(@"Software\Classes\Glowworm", false);
    }


    public static string? GetInstallLocation()
    {
        using var subkey = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\Glowworm");
        return subkey?.GetValue("InstallLocation") as string;
    }


    public static void DeleteRegistrySetting()
    {
        Registry.CurrentUser.DeleteSubKeyTree(@"Software\Glowworm", false);
    }

}








