using System.Reflection;
using System.Text;

using Volatility.Abstractions.Messaging;
using Volatility.Abstractions.Operations;
using Volatility.Abstractions.Services;
using Volatility.CLI;
using Volatility.Core.Utilities;
using Volatility.Operations;
using Volatility.Operations.Autotest;
using Volatility.Operations.Resources;
using Volatility.Resources;

using static Volatility.Utilities.TypeUtilities;
using static Volatility.Utilities.ResourceIDUtilities;

namespace Volatility.CLI.Commands;

internal class AutotestCommand : ICommand
{
    private readonly IPathProvider pathProvider;
    private readonly IOperation<GameAutotestRequest, GameAutotestSummary> gameAutotestOperation;
    private readonly IOperation<TextureRoundTripRequest, TextureRoundTripResult> textureRoundTripOperation;
    private readonly IResourceSerializer resourceSerializer;

    public static string CommandToken => "autotest";
    public static string CommandDescription => "Runs automatic tests to ensure the application is working." +
        " When provided a path & format, will import, export, then reimport specified file to ensure IO parity." +
        " When provided one or more game paths, will probe all bundle-like root files through libbndl by default, or use YAP-style bundle probing when --bundletool=YAP is specified, then run automated resource operations on supported resource types and verify exact binary parity for roundtrip exports.";
    public static string CommandParameters => "[--format=<tub,bpr,x360,ps3>] [--path=<file path>] [--game=<dir>] [--games=<dir1|dir2>] [--bundletool=<file|YAP>] [--workdir=<dir>] [--bundlelimit=<n,0=all>] [--resourcelimit=<n>] [--keepartifacts] [--recap=<file|directory>]";

    public string? Format { get; set; }
    public string? Path { get; set; }
    public string? GamePath { get; set; }
    public string? GamePaths { get; set; }
    public string? BundleToolPath { get; set; }
    public string? WorkingDirectory { get; set; }
    public string? RecapPath { get; set; }
    public int BundleLimit { get; set; }
    public int ResourceLimit { get; set; } = 2;
    public bool KeepArtifacts { get; set; }

    public async Task Execute()
    {
        IReadOnlyList<string> gamePaths = ParseGamePaths();
        if (gamePaths.Count > 0)
        {
            OperationResult<GameAutotestSummary> result = await gameAutotestOperation.ExecuteAsync(
                new GameAutotestRequest
                {
                    GamePaths = gamePaths,
                    BundleToolPath = BundleToolPath,
                    WorkingDirectory = WorkingDirectory,
                    BundleLimitPerGame = BundleLimit,
                    ResourcesPerType = ResourceLimit,
                    KeepArtifacts = KeepArtifacts
                },
                progress: null,
                cancellationToken: CancellationToken.None);

            CLIMessageUtilities.PublishIssues(result.Issues);
            if (!result.Success || result.Value == null)
            {
                throw OperationResultFactory.CreateException(result, "Game autotest failed to execute.");
            }

            GameAutotestSummary summary = result.Value;

            CLIMessageUtilities.Info<AutotestCommand>(
                $"AUTOTEST - Completed. Passed={summary.Passed}, Failed={summary.Failed}, Skipped={summary.Skipped}",
                MessageCategory.Autotest);

            if (!string.IsNullOrWhiteSpace(RecapPath))
            {
                string recapFilePath = WriteDetailedRecap(gamePaths, summary, RecapPath);
                CLIMessageUtilities.Success<AutotestCommand>(
                    $"AUTOTEST - Detailed recap written to: {recapFilePath}",
                    MessageCategory.Autotest);
            }

            if (summary.Failed > 0)
            {
                throw new InvalidOperationException($"Game autotest completed with {summary.Failed} failed cases.");
            }
            return;
        }

        if (!string.IsNullOrEmpty(Path))
        {
            if (string.IsNullOrEmpty(Format) || !TryParseEnum(Format, out Platform platform))
            {
                throw new InvalidPlatformException();
            }

            string inputPath = pathProvider.GetFullPath(Path);
            TextureBase header;
            using (FileStream fs = File.OpenRead(inputPath))
            {
                header = (TextureBase)resourceSerializer.Deserialize(
                    fs,
                    ResourceType.Texture,
                    platform,
                    new ResourceSerializationOptions
                    {
                        FileName = inputPath
                    });
            }

            (bool Success, int MismatchCount) result = await TestHeaderRW($"autotest_{System.IO.Path.GetFileName(inputPath)}", header);
            if (!result.Success)
            {
                throw new InvalidOperationException("Path autotest failed to execute successfully.");
            }
            if (result.MismatchCount > 0)
            {
                throw new InvalidOperationException($"Path autotest failed: {result.MismatchCount} mismatches detected.");
            }

            return;
        }

        /*
         * Right now, the autotest simply creates
         * example texture classes akin to what the parser
         * will interpret from an input format, then write
         * them out to various platform formatted header files.
         */
        int totalMismatches = 0;
        bool allSuccessful = true;

        // TUB Texture data test case
        TexturePC textureHeaderPC = new()
        {
            AssetName = "autotest_header_PC",
            ResourceID = ResourceID.HashFromString("autotest_header_PC"),
            Format = D3DFORMAT.D3DFMT_DXT1,
            Width = 1024,
            Height = 512,
            MipmapLevels = 11,
            UsageFlags = TextureBaseUsageFlags.GRTexture
        };

        (bool Success, int MismatchCount) pcResult = await TestHeaderRW("autotest_header_PC.dat", textureHeaderPC);
        if (!pcResult.Success) allSuccessful = false;
        totalMismatches += pcResult.MismatchCount;

        // BPR Texture data test case
        TextureBPR textureHeaderBPR = new()
        {
            AssetName = "autotest_header_BPR",
            ResourceID = ResourceID.HashFromString("autotest_header_BPR"),
            Format = DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM,
            Width = 1024,
            Height = 512,
            MipmapLevels = 11,
            UsageFlags = TextureBaseUsageFlags.GRTexture
        };

        // SKIPPING BPR IMPORT AS IT'S NOT SUPPORTED YET

        // Write 32 bit test BPR header
        (bool Success, int MismatchCount) bprResult = await TestHeaderRW("autotest_header_BPR.dat", textureHeaderBPR);
        if (!bprResult.Success) allSuccessful = false;
        totalMismatches += bprResult.MismatchCount;

        textureHeaderBPR.SetResourceArch(Arch.x64);
        textureHeaderBPR.AssetName = "autotest_header_BPRx64";
        textureHeaderBPR.ResourceID = ResourceID.HashFromString(textureHeaderBPR.AssetName);

        // Write 64 bit test BPR header
        (bool Success, int MismatchCount) bprX64Result = await TestHeaderRW("autotest_header_BPRx64.dat", textureHeaderBPR);
        if (!bprX64Result.Success) allSuccessful = false;
        totalMismatches += bprX64Result.MismatchCount;

        // PS3 Texture data test case
        TexturePS3 textureHeaderPS3 = new()
        {
            AssetName = "autotest_header_PS3",
            ResourceID = ResourceID.HashFromString("autotest_header_PS3"),
            Format = CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT45,
            Width = 1024,
            Height = 512,
            MipmapLevels = 11,
            UsageFlags = TextureBaseUsageFlags.GRTexture
        };
        textureHeaderPS3.PushAll();
        (bool Success, int MismatchCount) ps3Result = await TestHeaderRW("autotest_header_PS3.dat", textureHeaderPS3);
        if (!ps3Result.Success) allSuccessful = false;
        totalMismatches += ps3Result.MismatchCount;

        // X360 Texture data test case
        TextureX360 textureHeaderX360 = new()
        {
            AssetName = "autotest_header_X360",
            ResourceID = ResourceID.HashFromString("autotest_header_X360"),
            Format = new()
            {
                Tiled = true,
                SwizzleW = GPUSWIZZLE.GPUSWIZZLE_W,
                SwizzleX = GPUSWIZZLE.GPUSWIZZLE_X,
                SwizzleY = GPUSWIZZLE.GPUSWIZZLE_Y,
                SwizzleZ = GPUSWIZZLE.GPUSWIZZLE_Z,
            },
            Width = 1024,
            Height = 512,
            Depth = 1,
            MipmapLevels = 11,
            UsageFlags = TextureBaseUsageFlags.GRTexture
        };
        textureHeaderX360.PushAll();
        (bool Success, int MismatchCount) x360Result = await TestHeaderRW("autotest_header_X360.dat", textureHeaderX360);
        if (!x360Result.Success) allSuccessful = false;
        totalMismatches += x360Result.MismatchCount;

        // File name endian flip test case
        string endianFlipTestName = "12_34_56_78_texture.dat";
        CLIMessageUtilities.Info<AutotestCommand>(
            $"AUTOTEST - Endian Test: Flipped endian {endianFlipTestName} to {FlipPathResourceIDEndian(endianFlipTestName)}",
            MessageCategory.Autotest);

        if (!allSuccessful)
        {
            throw new InvalidOperationException("One or more synthetic autotests failed to execute successfully.");
        }
        if (totalMismatches > 0)
        {
            throw new InvalidOperationException($"Synthetic autotest failed: {totalMismatches} mismatches detected.");
        }
    }

    public void SetArgs(Dictionary<string, object> args)
    {
        Format = (args.TryGetValue("format", out object? format) ? format as string : "auto")?.ToUpper() ?? "AUTO";
        Path = args.TryGetValue("path", out object? path) ? path as string : "";
        GamePath = args.TryGetValue("game", out object? game) ? game as string : "";
        GamePaths = args.TryGetValue("games", out object? games) ? games as string : "";
        BundleToolPath = args.TryGetValue("bundletool", out object? bundleTool) ? bundleTool as string : "";
        WorkingDirectory = args.TryGetValue("workdir", out object? workdir) ? workdir as string : "";
        RecapPath = args.TryGetValue("recap", out object? recap) ? recap as string : "";
        KeepArtifacts = args.TryGetValue("keepartifacts", out var keepArtifacts) && (bool)keepArtifacts;

        if (args.TryGetValue("bundlelimit", out object? bundleLimitValue) &&
            int.TryParse(bundleLimitValue?.ToString(), out int bundleLimit))
        {
            BundleLimit = Math.Max(0, bundleLimit);
        }

        if (args.TryGetValue("resourcelimit", out object? resourceLimitValue) &&
            int.TryParse(resourceLimitValue?.ToString(), out int resourceLimit))
        {
            ResourceLimit = Math.Max(1, resourceLimit);
        }
    }

    public async Task<(bool Success, int MismatchCount)> TestHeaderRW(string name, TextureBase header, bool skipImport = false)
    {
        OperationResult<TextureRoundTripResult> opResult = await textureRoundTripOperation.ExecuteAsync(
            new TextureRoundTripRequest(name, header, skipImport),
            progress: null,
            cancellationToken: CancellationToken.None);

        if (!opResult.Success || opResult.Value == null)
        {
            CLIMessageUtilities.Error<AutotestCommand>($"Failed to roundtrip header: {name}");
            return (false, 0);
        }

        TextureRoundTripResult result = opResult.Value;
        if (!result.PushImplemented)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"A push isn't implemented for {header.GetType().Name}!");
            Console.ResetColor();
        }

        if (skipImport)
            return (true, 0);

        Console.WriteLine(">> Comparing properties and fields of " + header.GetType().Name + ":");
        foreach (PropertyMismatch mismatch in result.Mismatches)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Mismatch - {mismatch.Path}: Exported = {mismatch.Exported}, Imported = {mismatch.Imported}");
            Console.ResetColor();
        }

        Console.ForegroundColor = result.Mismatches.Count == 0 ? ConsoleColor.Green : ConsoleColor.Red;
        Console.WriteLine(">> Finished Comparing properties and fields of " + header.GetType().Name + $" - {result.Mismatches.Count} mismatches");
        Console.ResetColor();

        return (true, result.Mismatches.Count);
    }

    private IReadOnlyList<string> ParseGamePaths()
    {
        List<string> paths = [];

        if (!string.IsNullOrWhiteSpace(GamePath))
        {
            paths.Add(GamePath);
        }

        if (!string.IsNullOrWhiteSpace(GamePaths))
        {
            paths.AddRange(
                GamePaths
                    .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        return paths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private string WriteDetailedRecap(IReadOnlyList<string> gamePaths, GameAutotestSummary summary, string outputPath)
    {
        string recapPath = ResolveRecapPath(outputPath);
        StringBuilder builder = new();
        int binaryParityPassed = summary.Cases.Count(result =>
            string.Equals(result.Outcome, "PASS", StringComparison.Ordinal) &&
            string.Equals(result.Operation, "binaryparity", StringComparison.OrdinalIgnoreCase));
        int semiPassed = Math.Max(0, summary.Passed - binaryParityPassed);
        DateTime generatedAt = DateTime.Now;

        builder.AppendLine("# Volatility Autotest Recap");
        builder.AppendLine();
        builder.AppendLine($"Generated ({GetLocalTimeZoneLabel(generatedAt)}): {generatedAt:yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine($"Games: `{string.Join("` | `", gamePaths)}`");
        builder.AppendLine($"* Failed: {summary.Failed}");
        builder.AppendLine($"* Passed with binary parity: {binaryParityPassed}");
        builder.AppendLine($"* Semi-passed (without binary parity): {semiPassed}");
        builder.AppendLine($"* Skipped: {summary.Skipped}");
        builder.AppendLine();

        builder.AppendLine("## Test Operation Summary");
        builder.AppendLine();
        
        List<IGrouping<string, GameAutotestCaseResult>> byOperation = summary.Cases
            .GroupBy(result => result.Operation, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (byOperation.Count == 0)
        {
            builder.AppendLine("No test operations were recorded.");
        }
        else
        {
            builder.AppendLine("| Operation | Passed | Failed | Skipped |");
            builder.AppendLine("| --- | ---: | ---: | ---: |");

            foreach (IGrouping<string, GameAutotestCaseResult> group in byOperation)
            {
                int passed = group.Count(result => string.Equals(result.Outcome, "PASS", StringComparison.Ordinal));
                int failed = group.Count(result => string.Equals(result.Outcome, "FAIL", StringComparison.Ordinal));
                int skipped = group.Count(result => !string.Equals(result.Outcome, "PASS", StringComparison.Ordinal) && !string.Equals(result.Outcome, "FAIL", StringComparison.Ordinal));

                builder.AppendLine($"| {group.Key} | {passed} | {failed} | {skipped} |");
            }
        }

        builder.AppendLine();

        List<IGrouping<ResourceType, GameAutotestCaseResult>> byResourceType = summary.Cases
            .Where(result => result.TestedResourceType.HasValue)
            .GroupBy(result => result.TestedResourceType!.Value)
            .OrderBy(group => group.Key.ToString(), StringComparer.OrdinalIgnoreCase)
            .ToList();

        builder.AppendLine("## Resource Type Outcomes");
        builder.AppendLine();

        if (byResourceType.Count == 0)
        {
            builder.AppendLine("No resource-type specific cases were recorded.");
        }
        else
        {
            builder.AppendLine("| Resource Type | Passed | Failed | Skipped | Overall |");
            builder.AppendLine("| --- | ---: | ---: | ---: | --- |");

            foreach (IGrouping<ResourceType, GameAutotestCaseResult> group in byResourceType)
            {
                int passed = group.Count(result => string.Equals(result.Outcome, "PASS", StringComparison.Ordinal));
                int failed = group.Count(result => string.Equals(result.Outcome, "FAIL", StringComparison.Ordinal));
                int skipped = group.Count(result => !string.Equals(result.Outcome, "PASS", StringComparison.Ordinal) && !string.Equals(result.Outcome, "FAIL", StringComparison.Ordinal));
                string overall = failed > 0 ? "FAIL" : passed > 0 ? "PASS" : "SKIP";

                builder.AppendLine($"| {group.Key} | {passed} | {failed} | {skipped} | {overall} |");
            }
        }

        builder.AppendLine();
        builder.AppendLine("## Case Details");
        builder.AppendLine();
        builder.AppendLine("| Game | Resource Type | Operation | Name | Outcome | Details |");
        builder.AppendLine("| --- | --- | --- | --- | --- | --- |");

        foreach (GameAutotestCaseResult result in summary.Cases)
        {
            string resourceType = result.TestedResourceType?.ToString() ?? "-";
            builder.AppendLine($"| {EscapeMarkdownCell(result.Game)} | {EscapeMarkdownCell(resourceType)} | {EscapeMarkdownCell(result.Operation)} | {EscapeMarkdownCell(result.Name)} | {EscapeMarkdownCell(result.Outcome)} | {EscapeMarkdownCell(result.Details ?? string.Empty)} |");
        }

        File.WriteAllText(recapPath, builder.ToString());
        return recapPath;
    }

    private string ResolveRecapPath(string outputPath)
    {
        string fullPath = pathProvider.GetFullPath(outputPath);
        bool looksLikeDirectory =
            outputPath.EndsWith(System.IO.Path.DirectorySeparatorChar) ||
            outputPath.EndsWith(System.IO.Path.AltDirectorySeparatorChar) ||
            string.IsNullOrWhiteSpace(System.IO.Path.GetExtension(fullPath));

        if (pathProvider.DirectoryExists(fullPath) || looksLikeDirectory)
        {
            pathProvider.CreateDirectory(fullPath);
            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            return System.IO.Path.Combine(fullPath, $"autotest_recap_{timestamp}.md");
        }

        string? directory = System.IO.Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            pathProvider.CreateDirectory(directory);
        }

        return fullPath;
    }

    private static string GetLocalTimeZoneLabel(DateTime localTime)
    {
        TimeZoneInfo localTimeZone = TimeZoneInfo.Local;
        TimeSpan offset = localTimeZone.GetUtcOffset(localTime);
        string sign = offset < TimeSpan.Zero ? "-" : "+";
        TimeSpan absoluteOffset = offset.Duration();

        return $"UTC{sign}{absoluteOffset:hh\\:mm}";
    }

    private static string EscapeMarkdownCell(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("|", "\\|", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", "<br>", StringComparison.Ordinal)
            .Trim();
    }

    public AutotestCommand(
        IPathProvider pathProvider,
        IOperation<GameAutotestRequest, GameAutotestSummary> gameAutotestOperation,
        IOperation<TextureRoundTripRequest, TextureRoundTripResult> textureRoundTripOperation,
        IResourceSerializer resourceSerializer)
    {
        this.pathProvider = pathProvider;
        this.gameAutotestOperation = gameAutotestOperation;
        this.textureRoundTripOperation = textureRoundTripOperation;
        this.resourceSerializer = resourceSerializer;
    }
}
