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

string? r2Endpoint = Environment.GetEnvironmentVariable("R2_S3_ENDPOINT");
string? r2Bucket = Environment.GetEnvironmentVariable("R2_BUCKET");
string? r2AccessKey = Environment.GetEnvironmentVariable("R2_ACCESS_KEY_ID");
string? r2Secret = Environment.GetEnvironmentVariable("R2_SECRET_ACCESS_KEY");
string? r2Region = Environment.GetEnvironmentVariable("R2_REGION");

if (!string.IsNullOrWhiteSpace(r2Endpoint) && !string.IsNullOrWhiteSpace(r2Bucket) && !string.IsNullOrWhiteSpace(r2AccessKey) && !string.IsNullOrWhiteSpace(r2Secret))
{
    Console.WriteLine($"Uploading releases to Cloudflare R2 (Prefix: glowworm/{branchName})...");
    foreach (var arch in targetArchitectures)
    {
        Console.WriteLine($"Uploading channel win-{arch}...");
        var uploadArgs = $"upload s3 --endpoint {r2Endpoint} --bucket {r2Bucket} --keyId {r2AccessKey} --secret {r2Secret} --region {r2Region ?? "auto"} --prefix glowworm/{branchName} --channel win-{arch} -o {releaseDir}";
        var uploadProcess = Process.Start("vpk", uploadArgs);
        await uploadProcess.WaitForExitAsync();

        if (uploadProcess.ExitCode != 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Warning: vpk upload s3 exited with code {uploadProcess.ExitCode} for channel win-{arch}");
            Console.ResetColor();
        }
    }
}
else
{
    Console.WriteLine("Skipping R2 upload (Missing one or more R2 environment variables).");
}

return 0;
