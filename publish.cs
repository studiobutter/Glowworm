using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

if (args.Length is 0)
{
    Console.WriteLine("No parameters input.");
    return 0;
}

var config = new ConfigurationBuilder().AddCommandLine(args).Build();

string? version = config.GetValue<string>("version");
string? archOption = config.GetValue<string>("arch");

if (string.IsNullOrWhiteSpace(version))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Error: Version is required. Use --version to specify the version.");
    Console.ResetColor();
    return 0;
}

string[] targetArchitectures = string.IsNullOrWhiteSpace(archOption) 
    ? new[] { "x64", "arm64" } 
    : new[] { archOption.ToLower() };

var gitProcess = Process.Start(new ProcessStartInfo("git", "branch --show-current") { RedirectStandardOutput = true });
await gitProcess.WaitForExitAsync();
string branchName = gitProcess.StandardOutput.ReadToEnd().Trim();

string releaseDir = "publish/Releases";

foreach (var arch in targetArchitectures)
{
    string archPath = $"publish/{arch}";
    if (Directory.Exists(archPath))
    {
        Directory.Delete(archPath, true);
    }

    Console.WriteLine($"Building {arch} release...");
    var buildProcess = Process.Start("dotnet", $"publish src/Glowworm -c Release -r win-{arch} -o {archPath}/Glowworm/app -p:Platform={arch} -p:Version={version}");
    await buildProcess.WaitForExitAsync();
    
    if (buildProcess.ExitCode != 0)
    {
        throw new Exception($"Publish exited with code {buildProcess.ExitCode}");
    }

    Console.WriteLine($"Packing {arch} with Velopack...");
    var vpkArgs = $"pack -u Glowworm -v {version} -p {archPath}/Glowworm/app -o {releaseDir} -c win-{arch} -e Glowworm.exe -i src/logo.ico";
    var vpkProcess = Process.Start("vpk", vpkArgs);
    await vpkProcess.WaitForExitAsync();

    if (vpkProcess.ExitCode != 0)
    {
        throw new Exception($"vpk pack exited with code {vpkProcess.ExitCode}");
    }
}

string targetPublicationDir = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "..", "Glowworm-Publication", "velopack", branchName));

Console.WriteLine($"Copying release artifacts to {targetPublicationDir}...");

if (!Directory.Exists(targetPublicationDir))
{
    Directory.CreateDirectory(targetPublicationDir);
}

foreach (var file in Directory.GetFiles(releaseDir))
{
    string fileName = Path.GetFileName(file);
    string destFile = Path.Combine(targetPublicationDir, fileName);
    File.Copy(file, destFile, true);
    Console.WriteLine($"Copied: {fileName}");
}

Console.WriteLine("Update publication complete.");

return 0;
