using System.Globalization;

using Volatility.Operations.Resources;
using Volatility.Resources;
using Volatility.Utilities;
using YamlDotNet.Serialization;

namespace Volatility.Operations.Autotest;

internal sealed class GameAutotestOptions
{
    public required IReadOnlyList<string> GamePaths { get; init; }
    public string? BundleToolPath { get; init; }
    public string? WorkingDirectory { get; init; }
    public int BundleLimitPerGame { get; init; }
    public int ResourcesPerType { get; init; } = 2;
    public bool KeepArtifacts { get; init; }
}

internal sealed class GameAutotestSummary
{
    public int Passed { get; set; }
    public int Failed { get; set; }
    public int Skipped { get; set; }
    public List<GameAutotestCaseResult> Cases { get; } = [];
}

internal sealed record GameAutotestCaseResult(
    string Game,
    string Name,
    string Operation,
    string Outcome,
    string? Details = null,
    ResourceType? TestedResourceType = null);

internal sealed class GameAutotestOperation
{
    private static readonly HashSet<ResourceType> RoundTripTypes =
    [
        ResourceType.Texture,
        ResourceType.GuiPopup,
        ResourceType.InstanceList,
        ResourceType.Model,
        ResourceType.EnvironmentKeyframe,
        ResourceType.EnvironmentTimeLine,
        ResourceType.SnapshotData,
        ResourceType.StreamedDeformationSpec,
    ];

    private static readonly HashSet<ResourceType> ImportOnlyTypes =
    [
        ResourceType.Renderable,
        ResourceType.Splicer,
        ResourceType.AptData,
    ];

    private static readonly string[] PreferredBundleNames =
    [
        "POPUPS.PUP",
        "AI.DAT",
        "PROGRESSION.DAT",
        "BTTPROGRESSION.DAT",
        "STREETDATA.DAT",
        "TRIGGERS.DAT",
        "HUDMESSAGES.HM",
        "HUDMESSAGESEQUENCES.HMSC",
        "B5TRAFFIC.BNDL",
        "BTTB5TRAFFIC.BNDL",
        "GLOBALBACKDROPS.BNDL",
        "GLOBALMODELDICTIONARY.BIN",
        "GLOBALPROPS.BIN",
        "GLOBALTEXTUREDICTIONARY.BIN",
        "GUITEXTURES.BIN",
        "MASSIVETABLE.BIN",
        "MASSIVETEXTUREDICTIONARY.BIN",
        "SURFACELIST.BIN",
        "WORLDVAULT.BIN",
        "CAMERAS.BUNDLE",
        "FLAPTHUD.BUNDLE",
        "PARTICLES.BUNDLE",
        "PLAYBACKREGISTRY.BUNDLE",
        "PVS.BNDL",
        "ONLINECHALLENGES.BNDL",
        "RWACFEATUREREGISTRY.BUNDLE",
        "SHADERS.BNDL",
        "TRK_UNIT0_GR.BNDL",
    ];

    public async Task<GameAutotestSummary> ExecuteAsync(GameAutotestOptions options)
    {
        if (options.GamePaths.Count == 0)
        {
            throw new InvalidOperationException("At least one game path must be provided.");
        }

        string repoRoot = WorkspaceUtilities.FindRepositoryRoot();
        bool useYapBundleTool = IsYapBundleTool(options.BundleToolPath);
        string bundleToolPath = ResolveBundleTool(repoRoot, options.BundleToolPath);
        string sessionRoot = ResolveSessionRoot(repoRoot, options.WorkingDirectory);

        Directory.CreateDirectory(sessionRoot);

        GameAutotestSummary summary = new();
        foreach (string gamePath in options.GamePaths)
        {
            GameInstall game = DetectGameInstall(gamePath);
            await RunGameAsync(game, bundleToolPath, useYapBundleTool, sessionRoot, options, summary);
        }

        return summary;
    }

    private async Task RunGameAsync(
        GameInstall game,
        string bundleToolPath,
        bool useYapBundleTool,
        string sessionRoot,
        GameAutotestOptions options,
        GameAutotestSummary summary)
    {
        string gameWorkRoot = Path.Combine(sessionRoot, $"{SanitizePathSegment(game.Name)}_{game.Platform}");
        Directory.CreateDirectory(gameWorkRoot);

        Console.WriteLine($"AUTOTEST - Game: {game.Name} ({game.Platform})");
        Console.WriteLine($"AUTOTEST - Working directory: {gameWorkRoot}");

        int failuresBefore = summary.Failed;

        List<ResourceTestCandidate> candidates = [];
        candidates.AddRange(GetDirectCandidates(game));

        List<ProbedBundle> probedBundles = ProbeBundleCandidates(game, bundleToolPath, useYapBundleTool, gameWorkRoot, options, summary);
        candidates.AddRange(ExtractSupportedBundleCandidates(game, bundleToolPath, useYapBundleTool, gameWorkRoot, options, probedBundles, summary));

        if (candidates.Count == 0)
        {
            AddCase(summary, new GameAutotestCaseResult(
                game.Name,
                "No candidates",
                "discover",
                "SKIP",
                "No supported resources were discovered after probing bundle-like root files."));

            if (!options.KeepArtifacts && failuresBefore == summary.Failed)
            {
                Directory.Delete(gameWorkRoot, recursive: true);
            }

            return;
        }

        string pass1Resources = Path.Combine(gameWorkRoot, "import_pass1", "Resources");
        string pass2Resources = Path.Combine(gameWorkRoot, "import_pass2", "Resources");
        string splicerPass1 = Path.Combine(gameWorkRoot, "import_pass1", "Splicer");
        string splicerPass2 = Path.Combine(gameWorkRoot, "import_pass2", "Splicer");
        string exportsRoot = Path.Combine(gameWorkRoot, "exports");
        string ddsRoot = Path.Combine(gameWorkRoot, "dds");
        string portRoot = Path.Combine(gameWorkRoot, "port");
        string toolsRoot = EnvironmentUtilities.GetEnvironmentDirectory(EnvironmentUtilities.EnvironmentDirectory.Tools);

        Directory.CreateDirectory(pass1Resources);
        Directory.CreateDirectory(pass2Resources);
        Directory.CreateDirectory(splicerPass1);
        Directory.CreateDirectory(splicerPass2);
        Directory.CreateDirectory(exportsRoot);
        Directory.CreateDirectory(ddsRoot);
        Directory.CreateDirectory(portRoot);

        ImportResourceOperation importPass1 = new(pass1Resources, toolsRoot, splicerPass1, overwrite: true);
        ImportResourceOperation importPass2 = new(pass2Resources, toolsRoot, splicerPass2, overwrite: true);
        SaveResourceOperation saveOperation = new();
        LoadResourceOperation loadOperation = new();
        ExportResourceOperation exportOperation = new();
        TextureToDDSOperation textureToDdsOperation = new();
        PortTextureOperation portTextureOperation = new();

        foreach (ResourceTestCandidate candidate in candidates)
        {
            if (RoundTripTypes.Contains(candidate.ResourceType))
            {
                await RunRoundTripAsync(
                    game,
                    candidate,
                    importPass1,
                    importPass2,
                    saveOperation,
                    loadOperation,
                    exportOperation,
                    exportsRoot,
                    summary);
            }
            else if (ImportOnlyTypes.Contains(candidate.ResourceType))
            {
                await RunImportOnlyAsync(game, candidate, importPass1, saveOperation, summary);
            }

            if (candidate.ResourceType == ResourceType.Texture)
            {
                await RunTextureOperationsAsync(game, candidate, textureToDdsOperation, portTextureOperation, ddsRoot, portRoot, summary);
            }
        }

        if (!options.KeepArtifacts && failuresBefore == summary.Failed)
        {
            Directory.Delete(gameWorkRoot, recursive: true);
        }
    }

    private static async Task RunRoundTripAsync(
        GameInstall game,
        ResourceTestCandidate candidate,
        ImportResourceOperation importPass1,
        ImportResourceOperation importPass2,
        SaveResourceOperation saveOperation,
        LoadResourceOperation loadOperation,
        ExportResourceOperation exportOperation,
        string exportsRoot,
        GameAutotestSummary summary)
    {
        string caseName = $"{candidate.ResourceType}:{candidate.DisplayName}";
        string? exportPath = null;
        bool binaryParityRecorded = false;

        try
        {
            ImportResourceResult firstImport = await importPass1.ExecuteAsync(candidate.ResourceType, game.Platform, candidate.SourcePath, isX64: false);
            await saveOperation.ExecuteAsync(firstImport.Resource, firstImport.ResourcePath);

            Resource loaded = await loadOperation.ExecuteAsync(firstImport.ResourcePath, candidate.ResourceType, game.Platform);
            exportPath = Path.Combine(exportsRoot, Path.GetFileName(candidate.SourcePath));
            await exportOperation.ExecuteAsync(loaded, exportPath, game.Platform);

            BinaryComparisonResult binaryComparison = CompareFilesExactly(candidate.SourcePath, exportPath);
            AddCase(summary, new GameAutotestCaseResult(
                game.Name,
                caseName,
                "binaryparity",
                binaryComparison.Matches ? "PASS" : "FAIL",
                binaryComparison.Details,
                TestedResourceType: candidate.ResourceType));
            binaryParityRecorded = true;

            ImportResourceResult secondImport = await importPass2.ExecuteAsync(candidate.ResourceType, game.Platform, exportPath, isX64: false);
            await saveOperation.ExecuteAsync(secondImport.Resource, secondImport.ResourcePath);

            string firstYaml = NormalizeYamlForComparison(await File.ReadAllTextAsync(firstImport.ResourcePath));
            string secondYaml = NormalizeYamlForComparison(await File.ReadAllTextAsync(secondImport.ResourcePath));

            if (string.Equals(firstYaml, secondYaml, StringComparison.Ordinal))
            {
                AddCase(summary, new GameAutotestCaseResult(game.Name, caseName, "roundtrip", "PASS", TestedResourceType: candidate.ResourceType));
                return;
            }

            AddCase(summary, new GameAutotestCaseResult(
                game.Name,
                caseName,
                "roundtrip",
                "FAIL",
                $"YAML mismatch after reimport. Pass1={firstImport.ResourcePath}, Pass2={secondImport.ResourcePath}",
                TestedResourceType: candidate.ResourceType));
        }
        catch (Exception ex)
        {
            if (!binaryParityRecorded)
            {
                string binaryOutcome = string.IsNullOrWhiteSpace(exportPath) ? "SKIP" : "FAIL";
                string binaryDetails = string.IsNullOrWhiteSpace(exportPath)
                    ? $"Roundtrip failed before binary parity comparison: {ex.Message}"
                    : $"Binary parity comparison failed: {ex.Message}";

                AddCase(summary, new GameAutotestCaseResult(
                    game.Name,
                    caseName,
                    "binaryparity",
                    binaryOutcome,
                    binaryDetails,
                    TestedResourceType: candidate.ResourceType));
            }

            AddCase(summary, new GameAutotestCaseResult(game.Name, caseName, "roundtrip", "FAIL", ex.Message, candidate.ResourceType));
        }
    }

    private static async Task RunImportOnlyAsync(
        GameInstall game,
        ResourceTestCandidate candidate,
        ImportResourceOperation importOperation,
        SaveResourceOperation saveOperation,
        GameAutotestSummary summary)
    {
        string caseName = $"{candidate.ResourceType}:{candidate.DisplayName}";

        try
        {
            ImportResourceResult importResult = await importOperation.ExecuteAsync(candidate.ResourceType, game.Platform, candidate.SourcePath, isX64: false);
            await saveOperation.ExecuteAsync(importResult.Resource, importResult.ResourcePath);
            AddCase(summary, new GameAutotestCaseResult(game.Name, caseName, "import", "PASS", TestedResourceType: candidate.ResourceType));
        }
        catch (Exception ex)
        {
            AddCase(summary, new GameAutotestCaseResult(game.Name, caseName, "import", "FAIL", ex.Message, candidate.ResourceType));
        }
    }

    private static async Task RunTextureOperationsAsync(
        GameInstall game,
        ResourceTestCandidate candidate,
        TextureToDDSOperation textureToDdsOperation,
        PortTextureOperation portTextureOperation,
        string ddsRoot,
        string portRoot,
        GameAutotestSummary summary)
    {
        string ddsCaseName = $"{candidate.DisplayName}:dds";
        try
        {
            await textureToDdsOperation.ExecuteAsync([candidate.SourcePath], game.Platform, isX64: false, ddsRoot, overwrite: true, verbose: false);
            AddCase(summary, new GameAutotestCaseResult(game.Name, ddsCaseName, "texturetodds", "PASS", TestedResourceType: ResourceType.Texture));
        }
        catch (Exception ex)
        {
            string outcome = IsSkippableTextureOperation(ex) ? "SKIP" : "FAIL";
            AddCase(summary, new GameAutotestCaseResult(game.Name, ddsCaseName, "texturetodds", outcome, ex.Message, ResourceType.Texture));
        }

        Platform destinationPlatform = GetTexturePortDestination(game.Platform);
        if (destinationPlatform == Platform.Agnostic)
        {
            AddCase(summary, new GameAutotestCaseResult(game.Name, $"{candidate.DisplayName}:port", "porttexture", "SKIP", "No supported destination platform.", ResourceType.Texture));
            return;
        }

        string portCaseName = $"{candidate.DisplayName}:{game.Platform}->{destinationPlatform}";
        try
        {
            string destinationFormat = destinationPlatform == Platform.TUB ? "TUB" : destinationPlatform.ToString().ToUpperInvariant();
            string sourceFormat = game.Platform == Platform.TUB ? "TUB" : game.Platform.ToString().ToUpperInvariant();
            string destinationPath = Path.Combine(portRoot, destinationPlatform.ToString());
            Directory.CreateDirectory(destinationPath);

            await portTextureOperation.ExecuteAsync(
                [candidate.SourcePath],
                sourceFormat,
                candidate.SourcePath,
                destinationFormat,
                destinationPath,
                verbose: false,
                useGtf: false);

            AddCase(summary, new GameAutotestCaseResult(game.Name, portCaseName, "porttexture", "PASS", TestedResourceType: ResourceType.Texture));
        }
        catch (Exception ex)
        {
            AddCase(summary, new GameAutotestCaseResult(game.Name, portCaseName, "porttexture", "FAIL", ex.Message, ResourceType.Texture));
        }
    }

    private static IEnumerable<ResourceTestCandidate> GetDirectCandidates(GameInstall game)
    {
        _ = game;
        yield break;
    }

    private static List<ProbedBundle> ProbeBundleCandidates(
        GameInstall game,
        string bundleToolPath,
        bool useYapBundleTool,
        string gameWorkRoot,
        GameAutotestOptions options,
        GameAutotestSummary summary)
    {
        string probeRoot = Path.Combine(gameWorkRoot, "bundle_probes");
        Directory.CreateDirectory(probeRoot);

        HashSet<ResourceType> reportedUnsupportedTypes = [];
        List<ProbedBundle> probes = [];

        foreach (string bundlePath in ApplyBundleLimit(GetBundleCandidates(game.RootPath), options.BundleLimitPerGame))
        {
            string bundleName = Path.GetFileName(bundlePath);
            string outputDirectory = Path.Combine(probeRoot, SanitizePathSegment(bundleName));
            string manifestPath = Path.Combine(outputDirectory, "manifest.tsv");

            RecreateDirectory(outputDirectory);

            try
            {
                if (useYapBundleTool)
                {
                    ProcessUtilities.RunAndCapture(
                        bundleToolPath,
                        $"e \"{bundlePath}\" \"{outputDirectory}\"",
                        Path.GetDirectoryName(bundleToolPath));
                }
                else
                {
                    ProcessUtilities.RunAndCapture(
                        bundleToolPath,
                        $"--bundle \"{bundlePath}\" --output \"{outputDirectory}\" --manifest \"{manifestPath}\" --metadataonly",
                        Path.GetDirectoryName(bundleToolPath));
                }
            }
            catch (Exception ex)
            {
                string outcome = useYapBundleTool && IsSkippableBundleExtractionFailure(ex) ? "SKIP" : "FAIL";
                AddCase(summary, new GameAutotestCaseResult(game.Name, bundleName, "bundleprobe", outcome, ex.Message));
                continue;
            }

            List<BundleManifestEntry> entries = useYapBundleTool
                ? ParseBundleMetadata(bundlePath, outputDirectory)
                : ParseManifest(bundlePath, outputDirectory, manifestPath).ToList();
            int supportedCount = entries.Count(entry => IsSupportedResourceType(entry.ResourceType));

            Console.WriteLine(
                $"AUTOTEST - Probed {bundleName}: Resources={entries.Count}, Supported={supportedCount}, Types={FormatTypeSummary(entries.Select(entry => entry.ResourceType))}");

            foreach (ResourceType unsupportedType in entries
                         .Select(entry => entry.ResourceType)
                         .Where(type => !IsSupportedResourceType(type))
                         .Distinct())
            {
                if (reportedUnsupportedTypes.Add(unsupportedType))
                {
                    AddCase(summary, new GameAutotestCaseResult(
                        game.Name,
                        GetResourceTypeLabel(unsupportedType),
                        "unsupported",
                        "SKIP",
                        $"Discovered in {bundleName}. No Volatility autotest handler exists for this resource type.",
                        TestedResourceType: unsupportedType));
                }
            }

            probes.Add(new ProbedBundle(bundlePath, entries));
        }

        return probes;
    }

    private static List<ResourceTestCandidate> ExtractSupportedBundleCandidates(
        GameInstall game,
        string bundleToolPath,
        bool useYapBundleTool,
        string gameWorkRoot,
        GameAutotestOptions options,
        IReadOnlyList<ProbedBundle> probedBundles,
        GameAutotestSummary summary)
    {
        string extractedRoot = Path.Combine(gameWorkRoot, "bundles");
        Directory.CreateDirectory(extractedRoot);

        HashSet<ResourceType> blockedTypes = [];
        Dictionary<ResourceType, int> selectedCounts = new();
        List<ResourceTestCandidate> candidates = [];
        foreach (ProbedBundle probedBundle in probedBundles)
        {
            Dictionary<ResourceType, int> pendingCounts = new();
            List<BundleManifestEntry> selectedEntries = [];

            foreach (BundleManifestEntry entry in probedBundle.Entries.DistinctBy(entry => entry.ResourceIdHex, StringComparer.OrdinalIgnoreCase))
            {
                if (!IsSupportedResourceType(entry.ResourceType) || blockedTypes.Contains(entry.ResourceType))
                {
                    continue;
                }

                int currentCount = selectedCounts.GetValueOrDefault(entry.ResourceType);
                int pendingCount = pendingCounts.GetValueOrDefault(entry.ResourceType);
                if (currentCount + pendingCount >= options.ResourcesPerType)
                {
                    continue;
                }

                selectedEntries.Add(entry);
                pendingCounts[entry.ResourceType] = pendingCount + 1;
            }

            if (selectedEntries.Count == 0)
            {
                continue;
            }

            string bundleName = Path.GetFileName(probedBundle.BundlePath);
            List<BundleManifestEntry> extractedEntries;
            if (useYapBundleTool)
            {
                extractedEntries = selectedEntries
                    .Where(entry => !string.IsNullOrWhiteSpace(entry.PrimaryPath))
                    .ToList();
            }
            else
            {
                string outputDirectory = Path.Combine(extractedRoot, SanitizePathSegment(bundleName));
                string manifestPath = Path.Combine(outputDirectory, "manifest.tsv");

                RecreateDirectory(outputDirectory);

                try
                {
                    ProcessUtilities.RunAndCapture(
                        bundleToolPath,
                        $"--bundle \"{probedBundle.BundlePath}\" --output \"{outputDirectory}\" --manifest \"{manifestPath}\"",
                        Path.GetDirectoryName(bundleToolPath));
                }
                catch (Exception ex)
                {
                    string outcome = IsSkippableBundleExtractionFailure(ex) ? "SKIP" : "FAIL";

                    if (outcome == "SKIP")
                    {
                        foreach (ResourceType blockedType in selectedEntries.Select(entry => entry.ResourceType).Distinct())
                        {
                            blockedTypes.Add(blockedType);
                        }
                    }

                    AddCase(summary, new GameAutotestCaseResult(game.Name, bundleName, "bundleextract", outcome, ex.Message));
                    continue;
                }

                extractedEntries = ParseManifest(probedBundle.BundlePath, outputDirectory, manifestPath)
                    .Where(entry => !string.IsNullOrWhiteSpace(entry.PrimaryPath))
                    .ToList();

                Console.WriteLine(
                    $"AUTOTEST - Extracted {bundleName}: Resources={extractedEntries.Count}, Selected={selectedEntries.Count}");
            }

            Dictionary<string, BundleManifestEntry> extractedById = extractedEntries
                .GroupBy(entry => entry.ResourceIdHex, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

            foreach (BundleManifestEntry selectedEntry in selectedEntries.DistinctBy(entry => entry.ResourceIdHex, StringComparer.OrdinalIgnoreCase))
            {
                if (selectedCounts.GetValueOrDefault(selectedEntry.ResourceType) >= options.ResourcesPerType)
                {
                    continue;
                }

                if (!extractedById.TryGetValue(selectedEntry.ResourceIdHex, out BundleManifestEntry? extractedEntry) ||
                    string.IsNullOrWhiteSpace(extractedEntry.PrimaryPath))
                {
                    AddCase(summary, new GameAutotestCaseResult(
                        game.Name,
                        $"{selectedEntry.ResourceType}:{selectedEntry.DisplayName}",
                        "candidate",
                        "FAIL",
                        $"Failed to resolve extracted primary data from {bundleName}.",
                        TestedResourceType: selectedEntry.ResourceType));
                    continue;
                }

                candidates.Add(new ResourceTestCandidate(extractedEntry.DisplayName, extractedEntry.PrimaryPath, extractedEntry.ResourceType));
                selectedCounts[extractedEntry.ResourceType] = selectedCounts.GetValueOrDefault(extractedEntry.ResourceType) + 1;
            }
        }

        foreach (ResourceType blockedType in blockedTypes.Where(type => selectedCounts.GetValueOrDefault(type) == 0))
        {
            AddCase(summary, new GameAutotestCaseResult(
                game.Name,
                GetResourceTypeLabel(blockedType),
                "candidate",
                "SKIP",
                "No fully extractable bundle candidate was available for this supported resource type.",
                TestedResourceType: blockedType));
        }

        return candidates;
    }

    private static IEnumerable<string> GetBundleCandidates(string rootPath)
    {
        List<string> candidates = Directory
            .EnumerateFiles(rootPath, "*", SearchOption.TopDirectoryOnly)
            .Where(IsBundleLikeFile)
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        List<string> ordered = [];
        foreach (string preferredName in PreferredBundleNames)
        {
            string? match = candidates.FirstOrDefault(path =>
                string.Equals(Path.GetFileName(path), preferredName, StringComparison.OrdinalIgnoreCase));

            if (match != null)
            {
                ordered.Add(match);
            }
        }

        ordered.AddRange(candidates.Where(path => !ordered.Contains(path, StringComparer.OrdinalIgnoreCase)));
        return ordered;
    }

    private static IEnumerable<string> ApplyBundleLimit(IEnumerable<string> candidates, int bundleLimitPerGame)
    {
        return bundleLimitPerGame > 0 ? candidates.Take(bundleLimitPerGame) : candidates;
    }

    private static bool IsBundleLikeFile(string path)
    {
        try
        {
            using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            if (stream.Length < 4)
            {
                return false;
            }

            byte[] magic = new byte[4];
            if (stream.Read(magic, 0, magic.Length) != magic.Length)
            {
                return false;
            }

            return magic[0] == (byte)'b' &&
                   magic[1] == (byte)'n' &&
                   magic[2] == (byte)'d' &&
                   (magic[3] == (byte)'2' || magic[3] == (byte)'l');
        }
        catch
        {
            return false;
        }
    }

    private static void RecreateDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }

        Directory.CreateDirectory(path);
    }

    private static IEnumerable<BundleManifestEntry> ParseManifest(string bundlePath, string outputDirectory, string manifestPath)
    {
        foreach (string line in File.ReadLines(manifestPath).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            string[] parts = line.Split('\t');
            if (parts.Length < 4)
            {
                continue;
            }

            if (!uint.TryParse(parts[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint typeValue))
            {
                continue;
            }

            if (!Enum.IsDefined(typeof(ResourceType), (int)typeValue))
            {
                continue;
            }

            string resourceIdHex = parts[0].Trim();
            string displayName = !string.IsNullOrWhiteSpace(parts[2])
                ? parts[2]
                : parts.Length > 3 && !string.IsNullOrWhiteSpace(parts[3])
                    ? parts[3]
                    : resourceIdHex;

            string? primaryPath = null;
            if (parts.Length > 4 && !string.IsNullOrWhiteSpace(parts[4]))
            {
                primaryPath = Path.Combine(outputDirectory, parts[4]);
            }

            yield return new BundleManifestEntry(
                bundlePath,
                resourceIdHex,
                displayName,
                (ResourceType)typeValue,
                primaryPath);
        }
    }

    private static List<BundleManifestEntry> ParseBundleMetadata(string bundlePath, string outputDirectory)
    {
        string metaPath = FindMetaYamlPath(outputDirectory);
        IDeserializer deserializer = new DeserializerBuilder().Build();

        using StringReader reader = new(File.ReadAllText(metaPath));
        Dictionary<object, object>? document = deserializer.Deserialize<Dictionary<object, object>>(reader);
        if (document == null ||
            !TryGetYamlMapValue(document, "resources", out Dictionary<object, object>? resources))
        {
            throw new InvalidDataException($"Bundle metadata file '{metaPath}' does not contain a valid resources mapping.");
        }

        List<string> extractedFiles = Directory
            .EnumerateFiles(outputDirectory, "*", SearchOption.AllDirectories)
            .Where(path => !path.EndsWith(".meta.yaml", StringComparison.OrdinalIgnoreCase))
            .ToList();

        List<BundleManifestEntry> entries = [];
        foreach ((object rawResourceId, object rawResourceData) in resources)
        {
            string? resourceIdHex = Convert.ToString(rawResourceId, CultureInfo.InvariantCulture)?.Trim();
            if (string.IsNullOrWhiteSpace(resourceIdHex) ||
                rawResourceData is not Dictionary<object, object> resourceData ||
                !TryGetYamlScalarValue(resourceData, "type", out object? rawTypeValue) ||
                !TryParseYamlUInt(rawTypeValue, out uint typeValue) ||
                !Enum.IsDefined(typeof(ResourceType), (int)typeValue))
            {
                continue;
            }

            entries.Add(new BundleManifestEntry(
                bundlePath,
                resourceIdHex,
                resourceIdHex,
                (ResourceType)typeValue,
                ResolvePrimaryPath(outputDirectory, extractedFiles, resourceIdHex)));
        }

        return entries;
    }

    private static string FindMetaYamlPath(string outputDirectory)
    {
        string directMetaPath = Path.Combine(outputDirectory, ".meta.yaml");
        if (File.Exists(directMetaPath))
        {
            return directMetaPath;
        }

        string? discoveredMetaPath = Directory
            .EnumerateFiles(outputDirectory, ".meta.yaml", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(outputDirectory, "*.meta.yaml", SearchOption.AllDirectories))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path.Length)
            .FirstOrDefault();

        if (discoveredMetaPath == null)
        {
            throw new FileNotFoundException($"No .meta.yaml file was found under {outputDirectory}");
        }

        return discoveredMetaPath;
    }

    private static bool TryGetYamlMapValue(
        Dictionary<object, object> mapping,
        string key,
        out Dictionary<object, object>? value)
    {
        value = null;
        foreach ((object rawKey, object rawValue) in mapping)
        {
            string? parsedKey = Convert.ToString(rawKey, CultureInfo.InvariantCulture);
            if (!string.Equals(parsedKey, key, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            value = rawValue as Dictionary<object, object>;
            return value != null;
        }

        return false;
    }

    private static bool TryGetYamlScalarValue(
        Dictionary<object, object> mapping,
        string key,
        out object? value)
    {
        value = null;
        foreach ((object rawKey, object rawValue) in mapping)
        {
            string? parsedKey = Convert.ToString(rawKey, CultureInfo.InvariantCulture);
            if (!string.Equals(parsedKey, key, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            value = rawValue;
            return true;
        }

        return false;
    }

    private static bool TryParseYamlUInt(object? value, out uint parsed)
    {
        switch (value)
        {
            case byte byteValue:
                parsed = byteValue;
                return true;
            case sbyte sbyteValue when sbyteValue >= 0:
                parsed = (uint)sbyteValue;
                return true;
            case short shortValue when shortValue >= 0:
                parsed = (uint)shortValue;
                return true;
            case ushort ushortValue:
                parsed = ushortValue;
                return true;
            case int intValue when intValue >= 0:
                parsed = (uint)intValue;
                return true;
            case uint uintValue:
                parsed = uintValue;
                return true;
            case long longValue when longValue >= 0 && longValue <= uint.MaxValue:
                parsed = (uint)longValue;
                return true;
            case ulong ulongValue when ulongValue <= uint.MaxValue:
                parsed = (uint)ulongValue;
                return true;
            case string stringValue:
            {
                string trimmed = stringValue.Trim();
                if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    return uint.TryParse(trimmed[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out parsed);
                }

                if (uint.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
                {
                    return true;
                }

                return uint.TryParse(trimmed, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out parsed);
            }
            default:
                parsed = 0;
                return false;
        }
    }

    private static string? ResolvePrimaryPath(
        string outputDirectory,
        IReadOnlyList<string> extractedFiles,
        string resourceIdHex)
    {
        string normalizedId = NormalizeResourceId(resourceIdHex);
        string prefixedId = $"0x{normalizedId}";

        return extractedFiles
            .Where(path => PathMatchesResourceId(outputDirectory, path, normalizedId, prefixedId))
            .OrderBy(path => GetPrimaryPathMatchRank(path, normalizedId, prefixedId))
            .ThenBy(path => Path.GetFileName(path).Length)
            .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static string NormalizeResourceId(string resourceIdHex)
    {
        return resourceIdHex
            .Trim()
            .ToLowerInvariant()
            .Replace("0x", string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private static bool PathMatchesResourceId(
        string outputDirectory,
        string path,
        string normalizedId,
        string prefixedId)
    {
        string relativePath = Path.GetRelativePath(outputDirectory, path);
        string[] segments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return segments.Any(segment =>
        {
            string lowerSegment = segment.ToLowerInvariant();
            return MatchesResourcePrefix(lowerSegment, prefixedId) || MatchesResourcePrefix(lowerSegment, normalizedId);
        });
    }

    private static bool MatchesResourcePrefix(string candidate, string resourceIdPrefix)
    {
        if (!candidate.StartsWith(resourceIdPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (candidate.Length == resourceIdPrefix.Length)
        {
            return true;
        }

        char separator = candidate[resourceIdPrefix.Length];
        return separator == '.' || separator == '_' || separator == '-';
    }

    private static int GetPrimaryPathMatchRank(string path, string normalizedId, string prefixedId)
    {
        string fileName = Path.GetFileName(path).ToLowerInvariant();
        string fileStem = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();

        if (fileName == prefixedId || fileName == normalizedId || fileStem == prefixedId || fileStem == normalizedId)
        {
            return 0;
        }

        if (MatchesResourcePrefix(fileName, prefixedId) ||
            MatchesResourcePrefix(fileName, normalizedId) ||
            MatchesResourcePrefix(fileStem, prefixedId) ||
            MatchesResourcePrefix(fileStem, normalizedId))
        {
            return 1;
        }

        return 2;
    }

    private static bool IsYapBundleTool(string? bundleToolPath)
    {
        return string.Equals(bundleToolPath, "YAP", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveBundleTool(string repoRoot, string? bundleToolPath)
    {
        if (IsYapBundleTool(bundleToolPath))
        {
            return "YAP";
        }

        if (!string.IsNullOrWhiteSpace(bundleToolPath))
        {
            string explicitPath = Path.GetFullPath(bundleToolPath);
            if (!File.Exists(explicitPath))
            {
                throw new FileNotFoundException($"Bundle extractor not found: {explicitPath}");
            }

            return explicitPath;
        }

        string defaultTool = Path.Combine(repoRoot, "tools", "libbndl-extractor", "build", "volatility_libbndl_extract.exe");
        if (File.Exists(defaultTool))
        {
            return defaultTool;
        }

        string buildScript = Path.Combine(repoRoot, "tools", "libbndl-extractor", "build.ps1");
        ProcessUtilities.RunAndCapture("powershell", $"-ExecutionPolicy Bypass -File \"{buildScript}\"", repoRoot);

        if (!File.Exists(defaultTool))
        {
            throw new FileNotFoundException($"Failed to build bundle extractor at {defaultTool}");
        }

        return defaultTool;
    }

    private static string ResolveSessionRoot(string repoRoot, string? workingDirectory)
    {
        if (!string.IsNullOrWhiteSpace(workingDirectory))
        {
            return Path.GetFullPath(workingDirectory);
        }

        string stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        return Path.Combine(repoRoot, ".tmp", "game-autotest", stamp);
    }

    private static GameInstall DetectGameInstall(string gamePath)
    {
        string fullPath = Path.GetFullPath(gamePath);
        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException($"Game directory not found: {fullPath}");
        }

        if (File.Exists(Path.Combine(fullPath, "BurnoutPR.exe")) ||
            File.Exists(Path.Combine(fullPath, "BurnoutPR_trial.exe")))
        {
            return new GameInstall(Path.GetFileName(fullPath), fullPath, Platform.TUB);
        }

        if (Directory.EnumerateFiles(fullPath, "*.xex", SearchOption.TopDirectoryOnly).Any())
        {
            return new GameInstall(Path.GetFileName(fullPath), fullPath, Platform.X360);
        }

        throw new InvalidOperationException($"Unable to infer platform for game directory: {fullPath}");
    }

    private static bool IsSupportedResourceType(ResourceType resourceType)
    {
        return RoundTripTypes.Contains(resourceType) || ImportOnlyTypes.Contains(resourceType);
    }

    private static string FormatTypeSummary(IEnumerable<ResourceType> resourceTypes)
    {
        List<string> labels = resourceTypes
            .Distinct()
            .Select(GetResourceTypeLabel)
            .OrderBy(label => label, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (labels.Count == 0)
        {
            return "None";
        }

        const int maxDisplayedTypes = 6;
        if (labels.Count <= maxDisplayedTypes)
        {
            return string.Join(", ", labels);
        }

        return $"{string.Join(", ", labels.Take(maxDisplayedTypes))}, +{labels.Count - maxDisplayedTypes} more";
    }

    private static string GetResourceTypeLabel(ResourceType resourceType)
    {
        return Enum.IsDefined(typeof(ResourceType), resourceType)
            ? resourceType.ToString()
            : $"0x{(uint)resourceType:X8}";
    }

    private static Platform GetTexturePortDestination(Platform sourcePlatform)
    {
        return sourcePlatform switch
        {
            Platform.TUB => Platform.BPR,
            Platform.X360 => Platform.TUB,
            Platform.PS3 => Platform.TUB,
            Platform.BPR => Platform.TUB,
            _ => Platform.Agnostic,
        };
    }

    private static string NormalizeYamlForComparison(string yaml)
    {
        IEnumerable<string> lines = yaml
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n')
            .Where(line => !line.TrimStart().StartsWith("ImportedFileName:", StringComparison.Ordinal));

        return string.Join('\n', lines).Trim();
    }

    private static BinaryComparisonResult CompareFilesExactly(string originalPath, string exportedPath)
    {
        FileInfo originalInfo = new(originalPath);
        FileInfo exportedInfo = new(exportedPath);

        if (originalInfo.Length != exportedInfo.Length)
        {
            return new BinaryComparisonResult(
                Matches: false,
                Details: $"Binary size mismatch. Original={originalInfo.Length} bytes, Exported={exportedInfo.Length} bytes.");
        }

        using FileStream originalStream = new(originalPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using FileStream exportedStream = new(exportedPath, FileMode.Open, FileAccess.Read, FileShare.Read);

        const int bufferSize = 81920;
        byte[] originalBuffer = new byte[bufferSize];
        byte[] exportedBuffer = new byte[bufferSize];
        long offset = 0;

        while (true)
        {
            int originalRead = originalStream.Read(originalBuffer, 0, originalBuffer.Length);
            int exportedRead = exportedStream.Read(exportedBuffer, 0, exportedBuffer.Length);

            if (originalRead != exportedRead)
            {
                return new BinaryComparisonResult(
                    Matches: false,
                    Details: $"Binary read mismatch at offset 0x{offset:X}. OriginalRead={originalRead}, ExportedRead={exportedRead}.");
            }

            if (originalRead == 0)
            {
                break;
            }

            for (int i = 0; i < originalRead; i++)
            {
                if (originalBuffer[i] != exportedBuffer[i])
                {
                    long mismatchOffset = offset + i;
                    return new BinaryComparisonResult(
                        Matches: false,
                        Details: $"Binary mismatch at offset 0x{mismatchOffset:X}. Original=0x{originalBuffer[i]:X2}, Exported=0x{exportedBuffer[i]:X2}.");
                }
            }

            offset += originalRead;
        }

        return new BinaryComparisonResult(
            Matches: true,
            Details: "Binary files are identical.");
    }

    private static bool IsSkippableTextureOperation(Exception ex)
    {
        return ex.Message.Contains("DDS export is not supported", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("Failed to find associated bitmap data", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSkippableBundleExtractionFailure(Exception ex)
    {
        return ex.Message.Contains("Assertion failed: m_flags & Compressed", StringComparison.OrdinalIgnoreCase);
    }

    private static string SanitizePathSegment(string value)
    {
        foreach (char invalidChar in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(invalidChar, '_');
        }

        return string.IsNullOrWhiteSpace(value) ? "game" : value;
    }

    private static void AddCase(GameAutotestSummary summary, GameAutotestCaseResult result)
    {
        summary.Cases.Add(result);

        switch (result.Outcome)
        {
            case "PASS":
                summary.Passed++;
                Console.ForegroundColor = ConsoleColor.Green;
                break;
            case "FAIL":
                summary.Failed++;
                Console.ForegroundColor = ConsoleColor.Red;
                break;
            default:
                summary.Skipped++;
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                break;
        }

        Console.WriteLine($"[{result.Outcome}] {result.Game} {result.Operation} {result.Name}" +
            (string.IsNullOrWhiteSpace(result.Details) ? string.Empty : $" - {result.Details}"));
        Console.ResetColor();
    }

    private sealed record GameInstall(string Name, string RootPath, Platform Platform);

    private sealed record ResourceTestCandidate(string DisplayName, string SourcePath, ResourceType ResourceType);

    private sealed record BinaryComparisonResult(bool Matches, string Details);

    private sealed record ProbedBundle(string BundlePath, List<BundleManifestEntry> Entries);

    private sealed record BundleManifestEntry(
        string BundlePath,
        string ResourceIdHex,
        string DisplayName,
        ResourceType ResourceType,
        string? PrimaryPath);
}
