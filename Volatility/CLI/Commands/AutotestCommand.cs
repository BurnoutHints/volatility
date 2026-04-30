using System.Reflection;
using System.Text;

using Volatility.Abstractions.Messaging;
using Volatility.Abstractions.Services;
using Volatility.CLI;
using Volatility.Operations.Autotest;
using Volatility.Resources;

using static Volatility.Utilities.TypeUtilities;
using static Volatility.Utilities.ResourceIDUtilities;

namespace Volatility.CLI.Commands;

internal class AutotestCommand : ICommand
{
    private readonly IPathProvider pathProvider;
    private readonly GameAutotestOperation gameAutotestOperation;

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
            GameAutotestSummary summary = await gameAutotestOperation.ExecuteAsync(new GameAutotestOptions
            {
                GamePaths = gamePaths,
                BundleToolPath = BundleToolPath,
                WorkingDirectory = WorkingDirectory,
                BundleLimitPerGame = BundleLimit,
                ResourcesPerType = ResourceLimit,
                KeepArtifacts = KeepArtifacts
            });

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
            return;
        }

        if (!string.IsNullOrEmpty(Path))
        {
            if (!TryParseEnum(Format, out Platform platform))
            {
                throw new InvalidPlatformException();
            }

            string inputPath = pathProvider.GetFullPath(Path);
            TextureBase header = (TextureBase)ResourceFactory.LoadResource(
                ResourceType.Texture,
                platform,
                inputPath,
                resourceDBLookup: null);

            TestHeaderRW($"autotest_{System.IO.Path.GetFileName(inputPath)}", header);

            return;
        }

        /*
         * Right now, the autotest simply creates
         * example texture classes akin to what the parser
         * will interpret from an input format, then write
         * them out to various platform formatted header files.
         */
            
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

        TestHeaderRW("autotest_header_PC.dat", textureHeaderPC);

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
        TestHeaderRW("autotest_header_BPR.dat", textureHeaderBPR);

        textureHeaderBPR.SetResourceArch(Arch.x64);
        textureHeaderBPR.AssetName = "autotest_header_BPRx64";
        textureHeaderBPR.ResourceID = ResourceID.HashFromString(textureHeaderBPR.AssetName);

        // Write 64 bit test BPR header
        TestHeaderRW("autotest_header_BPRx64.dat", textureHeaderBPR);

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
        TestHeaderRW("autotest_header_PS3.dat", textureHeaderPS3);

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
        TestHeaderRW("autotest_header_X360.dat", textureHeaderX360);

        // File name endian flip test case
        string endianFlipTestName = "12_34_56_78_texture.dat";
        CLIMessageUtilities.Info<AutotestCommand>(
            $"AUTOTEST - Endian Test: Flipped endian {endianFlipTestName} to {FlipPathResourceIDEndian(endianFlipTestName)}",
            MessageCategory.Autotest);
    }

    public void SetArgs(Dictionary<string, object> args)
    {
        Format = (args.TryGetValue("format", out object? format) ? format as string : "auto").ToUpper();
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

    public void TestHeaderRW(string name, TextureBase header, bool skipImport = false) 
    {
        using (FileStream fs = new(name, FileMode.Create))
        {
            // We don't want the command runner to catch the error
            try
            {
                header.PushAll();
            }
            catch (NotImplementedException)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"A push isn't implemented for {header.GetType().Name}!");
                Console.ResetColor();
            }

            using (ResourceBinaryWriter writer = new(fs, header.ResourceEndian))
            {
                Console.WriteLine($"AUTOTEST - Writing autotest {name} to working directory...");
                header.WriteToStream(writer);
                writer.Close();
            }

            if (skipImport)
                return;
            
            TextureBase newHeader = (TextureBase)ResourceFactory.LoadResource(
                ResourceType.Texture,
                header.ResourcePlatform,
                fs.Name,
                resourceDBLookup: null,
                x64: header.ResourceArch == Arch.x64);

            TestCompareHeaders(header, newHeader);
        }
    }

    public static void TestCompareHeaders(object exported, object imported)
    {
        Type type = exported.GetType();

        Console.WriteLine(">> Comparing properties and fields of " + type.Name + ":");
    
        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        int mismatches = 0;
        foreach (PropertyInfo property in properties)
        {
            
            object value1 = property.GetValue(exported, null);
            object value2 = property.GetValue(imported, null);
    
            if (IsComplexType(property.PropertyType))
            {
                Console.WriteLine($" >  Inspecting nested type {property.Name}:");
                TestCompareHeaders(value1, value2);
                Console.WriteLine($" >  Finished inspecting nested type {property.Name}");
            }
            else if (!Equals(value1, value2))
            {
                mismatches++;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Mismatch - {property.Name}: Exported = {value1}, Imported = {value2}");
                Console.ResetColor();
            }
        }
    
        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (FieldInfo field in fields)
        {
            object value1 = field.GetValue(exported);
            object value2 = field.GetValue(imported);
    
            if (IsComplexType(field.FieldType))
            {
                Console.WriteLine($" >  Inspecting nested type {field.Name}:");
                TestCompareHeaders(value1, value2);
                Console.WriteLine($" >  Finished inspecting nested type {field.Name}");
            }
            else if (!Equals(value1, value2))
            {
                mismatches++;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Mismatch - {field.Name}: Exported = {value1}, Imported = {value2}");
                Console.ResetColor();
            }
        }

        if (mismatches == 0) 
            Console.ForegroundColor = ConsoleColor.Green;

        Console.WriteLine(">> Finished Comparing properties and fields of " + type.Name + $" - {mismatches} mismatches");
        Console.ResetColor();
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

    public AutotestCommand(IPathProvider pathProvider, GameAutotestOperation gameAutotestOperation)
    {
        this.pathProvider = pathProvider;
        this.gameAutotestOperation = gameAutotestOperation;
    }
}
