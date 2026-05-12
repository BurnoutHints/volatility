using System.Globalization;

using Volatility.Abstractions.Operations;
using Volatility.Abstractions.Services;
using Volatility.Operations;
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
    private readonly IPathProvider pathProvider;
    private readonly IProcessRunner processRunner;
    private readonly ImportResourceOperation importOperation;
    private readonly IOperation<SaveResourceRequest, SaveResourceResult> saveOperation;
    private readonly IOperation<LoadResourceRequest, LoadResourceResult> loadOperation;
    private readonly ExportResourceOperation exportOperation;
    private readonly TextureToDDSOperation textureToDdsOperation;
    private readonly PortTextureOperation portTextureOperation;

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

    public GameAutotestOperation(
        IPathProvider pathProvider,
        IProcessRunner processRunner,
        ImportResourceOperation importOperation,
        IOperation<SaveResourceRequest, SaveResourceResult> saveOperation,
        IOperation<LoadResourceRequest, LoadResourceResult> loadOperation,
        ExportResourceOperation exportOperation,
        TextureToDDSOperation textureToDdsOperation,
        PortTextureOperation portTextureOperation)
    {
        this.pathProvider = pathProvider;
        this.processRunner = processRunner;
        this.importOperation = importOperation;
        this.saveOperation = saveOperation;
        this.loadOperation = loadOperation;
        this.exportOperation = exportOperation;
        this.textureToDdsOperation = textureToDdsOperation;
        this.portTextureOperation = portTextureOperation;
    }

    public async Task<GameAutotestSummary> ExecuteAsync(GameAutotestOptions options)
    {
        if (options.GamePaths.Count == 0)
        {
            throw new InvalidOperationException("At least one game path must be provided.");
        }

        string repoRoot = pathProvider.GetRepositoryRoot();
        bool useYapBundleTool = IsYapTool(options.BundleToolPath);
        string bundleToolPath = GetBundleTool(repoRoot, options.BundleToolPath);
        string sessionRoot = GetSessionRoot(repoRoot, options.WorkingDirectory);

        pathProvider.CreateDirectory(sessionRoot);

        GameAutotestSummary summary = new();
        foreach (string gamePath in options.GamePaths)
        {
            GameInstall game = DetectGame(gamePath);
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
        string gameWorkRoot = Path.Combine(sessionRoot, $"{SanitizePath(game.Name)}_{game.Platform}");
        pathProvider.CreateDirectory(gameWorkRoot);

        Console.WriteLine($"AUTOTEST - Game: {game.Name} ({game.Platform})");
        Console.WriteLine($"AUTOTEST - Working directory: {gameWorkRoot}");

        int failuresBefore = summary.Failed;

        List<ResourceTestCandidate> candidates = [];
        candidates.AddRange(GetDirect(game));

        List<ProbedBundle> probedBundles = ProbeBundles(game, bundleToolPath, useYapBundleTool, gameWorkRoot, options, summary);
        candidates.AddRange(GetBundleTests(game, bundleToolPath, useYapBundleTool, gameWorkRoot, options, probedBundles, summary));

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
        string toolsRoot = pathProvider.GetDirectory(VolatilityPathLocation.Tools);

        pathProvider.CreateDirectory(pass1Resources);
        pathProvider.CreateDirectory(pass2Resources);
        pathProvider.CreateDirectory(splicerPass1);
        pathProvider.CreateDirectory(splicerPass2);
        pathProvider.CreateDirectory(exportsRoot);
        pathProvider.CreateDirectory(ddsRoot);
        pathProvider.CreateDirectory(portRoot);

        foreach (ResourceTestCandidate candidate in candidates)
        {
            if (RoundTripTypes.Contains(candidate.ResourceType))
            {
                await RunRoundTripAsync(
                    game,
                    candidate,
                    pass1Resources,
                    pass2Resources,
                    toolsRoot,
                    splicerPass1,
                    splicerPass2,
                    exportsRoot,
                    summary);
            }
            else if (ImportOnlyTypes.Contains(candidate.ResourceType))
            {
                await RunImportOnlyAsync(game, candidate, pass1Resources, toolsRoot, splicerPass1, summary);
            }

            if (candidate.ResourceType == ResourceType.Texture)
            {
                await RunTextureOperationsAsync(game, candidate, ddsRoot, portRoot, summary);
            }
        }

        if (!options.KeepArtifacts && failuresBefore == summary.Failed)
        {
            Directory.Delete(gameWorkRoot, recursive: true);
        }
    }

    private async Task RunRoundTripAsync(
        GameInstall game,
        ResourceTestCandidate candidate,
        string pass1Resources,
        string pass2Resources,
        string toolsRoot,
        string splicerPass1,
        string splicerPass2,
        string exportsRoot,
        GameAutotestSummary summary)
    {
        string caseName = $"{candidate.ResourceType}:{candidate.DisplayName}";
        string? exportPath = null;
        bool binaryParityRecorded = false;

        try
        {
            ImportResourceResult firstImport = await importOperation.ExecuteAsync(new ImportResourceRequest(
                candidate.ResourceType,
                game.Platform,
                candidate.SourcePath,
                IsX64: false,
                pass1Resources,
                toolsRoot,
                splicerPass1,
                Overwrite: true));
            await SaveAsync(firstImport.Resource, firstImport.ResourcePath);

            Resource loaded = await LoadAsync(firstImport.ResourcePath, candidate.ResourceType, game.Platform);
            exportPath = Path.Combine(exportsRoot, Path.GetFileName(candidate.SourcePath));
            await exportOperation.ExecuteAsync(loaded, exportPath, game.Platform, splicerDirectory: splicerPass1);

            BinaryComparisonResult binaryComparison = CompareFiles(candidate.SourcePath, exportPath);
            AddCase(summary, new GameAutotestCaseResult(
                game.Name,
                caseName,
                "binaryparity",
                binaryComparison.Matches ? "PASS" : "FAIL",
                binaryComparison.Details,
                TestedResourceType: candidate.ResourceType));
            binaryParityRecorded = true;

            ImportResourceResult secondImport = await importOperation.ExecuteAsync(new ImportResourceRequest(
                candidate.ResourceType,
                game.Platform,
                exportPath,
                IsX64: false,
                pass2Resources,
                toolsRoot,
                splicerPass2,
                Overwrite: true));
            await SaveAsync(secondImport.Resource, secondImport.ResourcePath);

            string firstYaml = NormalizeYaml(await File.ReadAllTextAsync(firstImport.ResourcePath));
            string secondYaml = NormalizeYaml(await File.ReadAllTextAsync(secondImport.ResourcePath));

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

    private async Task RunImportOnlyAsync(
        GameInstall game,
        ResourceTestCandidate candidate,
        string resourcesDirectory,
        string toolsRoot,
        string splicerDirectory,
        GameAutotestSummary summary)
    {
        string caseName = $"{candidate.ResourceType}:{candidate.DisplayName}";

        try
        {
            ImportResourceResult importResult = await importOperation.ExecuteAsync(new ImportResourceRequest(
                candidate.ResourceType,
                game.Platform,
                candidate.SourcePath,
                IsX64: false,
                resourcesDirectory,
                toolsRoot,
                splicerDirectory,
                Overwrite: true));
            await SaveAsync(importResult.Resource, importResult.ResourcePath);
            AddCase(summary, new GameAutotestCaseResult(game.Name, caseName, "import", "PASS", TestedResourceType: candidate.ResourceType));
        }
        catch (Exception ex)
        {
            AddCase(summary, new GameAutotestCaseResult(game.Name, caseName, "import", "FAIL", ex.Message, candidate.ResourceType));
        }
    }

    private async Task RunTextureOperationsAsync(
        GameInstall game,
        ResourceTestCandidate candidate,
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
            string outcome = IsSkippableTextureOp(ex) ? "SKIP" : "FAIL";
            AddCase(summary, new GameAutotestCaseResult(game.Name, ddsCaseName, "texturetodds", outcome, ex.Message, ResourceType.Texture));
        }

        Platform destinationPlatform = GetPortTarget(game.Platform);
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
            pathProvider.CreateDirectory(destinationPath);

            await portTextureOperation.ExecuteAsync(
                [candidate.SourcePath],
                sourceFormat,
                candidate.SourcePath,
                destinationFormat,
                destinationPath,
                verbose: false,
                useGTF: false);

            AddCase(summary, new GameAutotestCaseResult(game.Name, portCaseName, "porttexture", "PASS", TestedResourceType: ResourceType.Texture));
        }
        catch (Exception ex)
        {
            AddCase(summary, new GameAutotestCaseResult(game.Name, portCaseName, "porttexture", "FAIL", ex.Message, ResourceType.Texture));
        }
    }

    private async Task<Resource> LoadAsync(string sourceFile, ResourceType resourceType, Platform platform)
    {
        OperationResult<LoadResourceResult> result = await loadOperation.ExecuteAsync(
            new LoadResourceRequest(sourceFile, resourceType, platform),
            progress: null,
            cancellationToken: CancellationToken.None);

        if (!result.Success || result.Value == null)
        {
            throw OperationResultFactory.CreateException(result, "Failed to load resource.");
        }

        return result.Value.Resource;
    }

    private async Task SaveAsync(Resource resource, string filePath)
    {
        OperationResult<SaveResourceResult> result = await saveOperation.ExecuteAsync(
            new SaveResourceRequest(resource, filePath, Overwrite: true),
            progress: null,
            cancellationToken: CancellationToken.None);

        if (!result.Success)
        {
            throw OperationResultFactory.CreateException(result, "Failed to save resource.");
        }
    }

    private static IEnumerable<ResourceTestCandidate> GetDirect(GameInstall game)
    {
        _ = game;
        yield break;
    }

    private List<ProbedBundle> ProbeBundles(
        GameInstall game,
        string bundleToolPath,
        bool useYapBundleTool,
        string gameWorkRoot,
        GameAutotestOptions options,
        GameAutotestSummary summary)
    {
        if (useYapBundleTool)
        {
            return ProbeYapBundles(game, bundleToolPath, gameWorkRoot, options, summary);
        }

        string probeRoot = Path.Combine(gameWorkRoot, "bundle_probes");
        pathProvider.CreateDirectory(probeRoot);

        HashSet<ResourceType> reportedUnsupportedTypes = [];
        List<ProbedBundle> probes = [];

        foreach (string bundlePath in LimitBundles(FindBundles(game.RootPath), options.BundleLimitPerGame))
        {
            string bundleName = Path.GetFileName(bundlePath);
            string outputDirectory = Path.Combine(probeRoot, SanitizePath(bundleName));
            string manifestPath = Path.Combine(outputDirectory, "manifest.tsv");

            ResetDirectory(outputDirectory);

            try
            {
                if (useYapBundleTool)
                {
                    processRunner.RunAndCapture(
                        bundleToolPath,
                        $"e \"{bundlePath}\" \"{outputDirectory}\"",
                        Path.GetDirectoryName(bundleToolPath));
                }
                else
                {
                    processRunner.RunAndCapture(
                        bundleToolPath,
                        $"--bundle \"{bundlePath}\" --output \"{outputDirectory}\" --manifest \"{manifestPath}\" --metadataonly",
                        Path.GetDirectoryName(bundleToolPath));
                }
            }
            catch (Exception ex)
            {
                string outcome = useYapBundleTool && IsSkippableBundleError(ex) ? "SKIP" : "FAIL";
                AddCase(summary, new GameAutotestCaseResult(game.Name, bundleName, "bundleprobe", outcome, ex.Message));
                continue;
            }

            List<BundleManifestEntry> entries = useYapBundleTool
                ? ParseMeta(bundlePath, outputDirectory)
                : ParseManifest(bundlePath, outputDirectory, manifestPath).ToList();
            int supportedCount = entries.Count(entry => IsSupportedType(entry.ResourceType));

            Console.WriteLine(
                $"AUTOTEST - Probed {bundleName}: Resources={entries.Count}, Supported={supportedCount}, Types={GetTypeSummary(entries.Select(entry => entry.ResourceType))}");

            foreach (ResourceType unsupportedType in entries
                         .Select(entry => entry.ResourceType)
                         .Where(type => !IsSupportedType(type))
                         .Distinct())
            {
                if (reportedUnsupportedTypes.Add(unsupportedType))
                {
                    AddCase(summary, new GameAutotestCaseResult(
                        game.Name,
                        GetTypeLabel(unsupportedType),
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

    private List<ProbedBundle> ProbeYapBundles(
        GameInstall game,
        string bundleToolPath,
        string gameWorkRoot,
        GameAutotestOptions options,
        GameAutotestSummary summary)
    {
        string probeRoot = Path.Combine(gameWorkRoot, "bundle_probes");
        string inputRoot = Path.Combine(gameWorkRoot, "bundle_probe_input");

        List<string> bundlePaths = LimitBundles(FindBundles(game.RootPath), options.BundleLimitPerGame).ToList();
        if (bundlePaths.Count == 0)
        {
            return [];
        }

        ResetDirectory(inputRoot);
        ResetDirectory(probeRoot);

        foreach (string bundlePath in bundlePaths)
        {
            string stagedPath = Path.Combine(inputRoot, Path.GetFileName(bundlePath));
            File.Copy(bundlePath, stagedPath, overwrite: true);
        }

        try
        {
            processRunner.RunAndCapture(
                bundleToolPath,
                $"-d e \"{inputRoot}\" \"{probeRoot}\"",
                Path.GetDirectoryName(bundleToolPath));
        }
        catch (Exception ex)
        {
            string outcome = IsSkippableBundleError(ex) ? "SKIP" : "FAIL";
            AddCase(summary, new GameAutotestCaseResult(game.Name, "YAP directory probe", "bundleprobe", outcome, ex.Message));
            return [];
        }

        HashSet<ResourceType> reportedUnsupportedTypes = [];
        List<ProbedBundle> probes = ParseYapMeta(bundlePaths, probeRoot);
        foreach (ProbedBundle probe in probes)
        {
            int supportedCount = probe.Entries.Count(entry => IsSupportedType(entry.ResourceType));

            Console.WriteLine(
                $"AUTOTEST - Probed {Path.GetFileName(probe.BundlePath)}: Resources={probe.Entries.Count}, Supported={supportedCount}, Types={GetTypeSummary(probe.Entries.Select(entry => entry.ResourceType))}");

            foreach (ResourceType unsupportedType in probe.Entries
                         .Select(entry => entry.ResourceType)
                         .Where(type => !IsSupportedType(type))
                         .Distinct())
            {
                if (reportedUnsupportedTypes.Add(unsupportedType))
                {
                    AddCase(summary, new GameAutotestCaseResult(
                        game.Name,
                        GetTypeLabel(unsupportedType),
                        "unsupported",
                        "SKIP",
                        $"Discovered in {Path.GetFileName(probe.BundlePath)}. No Volatility autotest handler exists for this resource type.",
                        TestedResourceType: unsupportedType));
                }
            }
        }

        return probes;
    }

    private List<ResourceTestCandidate> GetBundleTests(
        GameInstall game,
        string bundleToolPath,
        bool useYapBundleTool,
        string gameWorkRoot,
        GameAutotestOptions options,
        IReadOnlyList<ProbedBundle> probedBundles,
        GameAutotestSummary summary)
    {
        string extractedRoot = Path.Combine(gameWorkRoot, "bundles");
        pathProvider.CreateDirectory(extractedRoot);

        HashSet<ResourceType> blockedTypes = [];
        Dictionary<ResourceType, int> selectedCounts = new();
        List<ResourceTestCandidate> candidates = [];
        foreach (ProbedBundle probedBundle in probedBundles)
        {
            Dictionary<ResourceType, int> pendingCounts = new();
            List<BundleManifestEntry> selectedEntries = [];

            foreach (BundleManifestEntry entry in probedBundle.Entries.DistinctBy(entry => entry.ResourceIdHex, StringComparer.OrdinalIgnoreCase))
            {
                if (!IsSupportedType(entry.ResourceType) || blockedTypes.Contains(entry.ResourceType))
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
                string outputDirectory = Path.Combine(extractedRoot, SanitizePath(bundleName));
                string manifestPath = Path.Combine(outputDirectory, "manifest.tsv");

                ResetDirectory(outputDirectory);

                try
                {
                    processRunner.RunAndCapture(
                        bundleToolPath,
                        $"--bundle \"{probedBundle.BundlePath}\" --output \"{outputDirectory}\" --manifest \"{manifestPath}\"",
                        Path.GetDirectoryName(bundleToolPath));
                }
                catch (Exception ex)
                {
                    string outcome = IsSkippableBundleError(ex) ? "SKIP" : "FAIL";

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
                GetTypeLabel(blockedType),
                "candidate",
                "SKIP",
                "No fully extractable bundle candidate was available for this supported resource type.",
                TestedResourceType: blockedType));
        }

        return candidates;
    }

    private static IEnumerable<string> FindBundles(string rootPath)
    {
        List<string> candidates = Directory
            .EnumerateFiles(rootPath, "*", SearchOption.TopDirectoryOnly)
            .Where(IsBundleFile)
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

    private static IEnumerable<string> LimitBundles(IEnumerable<string> candidates, int bundleLimitPerGame)
    {
        return bundleLimitPerGame > 0 ? candidates.Take(bundleLimitPerGame) : candidates;
    }

    private static bool IsBundleFile(string path)
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

    private void ResetDirectory(string path)
    {
        if (pathProvider.DirectoryExists(path))
        {
            Directory.Delete(path, recursive: true);
        }

        pathProvider.CreateDirectory(path);
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

    private static List<BundleManifestEntry> ParseMeta(string bundlePath, string outputDirectory)
    {
        string metaPath = FindMetaYaml(outputDirectory);
        return ParseMeta(bundlePath, outputDirectory, metaPath);
    }

    private static List<BundleManifestEntry> ParseMeta(string bundlePath, string outputDirectory, string metaPath)
    {
        IDeserializer deserializer = new DeserializerBuilder().Build();

        using StringReader reader = new(File.ReadAllText(metaPath));
        Dictionary<object, object>? document = deserializer.Deserialize<Dictionary<object, object>>(reader);
        if (document == null ||
            !TryGetMap(document, "resources", out Dictionary<object, object>? resources))
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
                !TryGetScalar(resourceData, "type", out object? rawTypeValue) ||
                !TryParseUInt(rawTypeValue, out uint typeValue) ||
                !Enum.IsDefined(typeof(ResourceType), (int)typeValue))
            {
                continue;
            }

            entries.Add(new BundleManifestEntry(
                bundlePath,
                resourceIdHex,
                resourceIdHex,
                (ResourceType)typeValue,
                FindPrimaryPath(outputDirectory, extractedFiles, resourceIdHex)));
        }

        return entries;
    }

    private static List<ProbedBundle> ParseYapMeta(IReadOnlyList<string> bundlePaths, string probeRoot)
    {
        List<string> metaPaths = FindMetaYamls(probeRoot).ToList();
        if (metaPaths.Count == 0)
        {
            throw new FileNotFoundException($"No .meta.yaml file was found under {probeRoot}");
        }

        List<ProbedBundle> probes = [];
        foreach (string metaPath in metaPaths)
        {
            string outputDirectory = Path.GetDirectoryName(metaPath) ?? probeRoot;
            string bundleLabel = GetYapBundleName(bundlePaths, probeRoot, metaPath);
            List<BundleManifestEntry> entries = ParseMeta(bundleLabel, outputDirectory, metaPath);
            probes.Add(new ProbedBundle(bundleLabel, entries));
        }

        return probes;
    }

    private static string FindMetaYaml(string outputDirectory)
    {
        string? discoveredMetaPath = FindMetaYamls(outputDirectory)
            .OrderBy(path => path.Length)
            .FirstOrDefault();

        if (discoveredMetaPath == null)
        {
            throw new FileNotFoundException($"No .meta.yaml file was found under {outputDirectory}");
        }

        return discoveredMetaPath;
    }

    private static IEnumerable<string> FindMetaYamls(string rootDirectory)
    {
        string directMetaPath = Path.Combine(rootDirectory, ".meta.yaml");
        if (File.Exists(directMetaPath))
        {
            yield return directMetaPath;
        }

        foreach (string metaPath in Directory
                     .EnumerateFiles(rootDirectory, ".meta.yaml", SearchOption.AllDirectories)
                     .Concat(Directory.EnumerateFiles(rootDirectory, "*.meta.yaml", SearchOption.AllDirectories))
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .Where(path => !string.Equals(path, directMetaPath, StringComparison.OrdinalIgnoreCase)))
        {
            yield return metaPath;
        }
    }

    private static string GetYapBundleName(IReadOnlyList<string> bundlePaths, string probeRoot, string metaPath)
    {
        if (bundlePaths.Count == 1)
        {
            return bundlePaths[0];
        }

        string relativeDirectory = Path.GetRelativePath(probeRoot, Path.GetDirectoryName(metaPath) ?? probeRoot);
        if (string.IsNullOrWhiteSpace(relativeDirectory) || string.Equals(relativeDirectory, ".", StringComparison.Ordinal))
        {
            return Path.GetFileName(metaPath);
        }

        string directoryName = relativeDirectory
            .Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault() ?? relativeDirectory;
        string? matchingBundlePath = bundlePaths.FirstOrDefault(path =>
            string.Equals(Path.GetFileName(path), directoryName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Path.GetFileNameWithoutExtension(path), directoryName, StringComparison.OrdinalIgnoreCase));

        return matchingBundlePath ?? directoryName;
    }

    private static bool TryGetMap(
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

    private static bool TryGetScalar(
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

    private static bool TryParseUInt(object? value, out uint parsed)
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

    private static string? FindPrimaryPath(
        string outputDirectory,
        IReadOnlyList<string> extractedFiles,
        string resourceIdHex)
    {
        string normalizedId = NormalizeId(resourceIdHex);
        string prefixedId = $"0x{normalizedId}";

        return extractedFiles
            .Where(path => PathMatchesId(outputDirectory, path, normalizedId, prefixedId))
            .OrderBy(path => GetPathRank(path, normalizedId, prefixedId))
            .ThenBy(path => Path.GetFileName(path).Length)
            .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static string NormalizeId(string resourceIdHex)
    {
        return resourceIdHex
            .Trim()
            .ToLowerInvariant()
            .Replace("0x", string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private static bool PathMatchesId(
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
            return MatchesIdPrefix(lowerSegment, prefixedId) || MatchesIdPrefix(lowerSegment, normalizedId);
        });
    }

    private static bool MatchesIdPrefix(string candidate, string resourceIdPrefix)
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

    private static int GetPathRank(string path, string normalizedId, string prefixedId)
    {
        string fileName = Path.GetFileName(path).ToLowerInvariant();
        string fileStem = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();

        if (fileName == prefixedId || fileName == normalizedId || fileStem == prefixedId || fileStem == normalizedId)
        {
            return 0;
        }

        if (MatchesIdPrefix(fileName, prefixedId) ||
            MatchesIdPrefix(fileName, normalizedId) ||
            MatchesIdPrefix(fileStem, prefixedId) ||
            MatchesIdPrefix(fileStem, normalizedId))
        {
            return 1;
        }

        return 2;
    }

    private static bool IsYapTool(string? bundleToolPath)
    {
        return string.Equals(bundleToolPath, "YAP", StringComparison.OrdinalIgnoreCase);
    }

    private string GetBundleTool(string repoRoot, string? bundleToolPath)
    {
        if (IsYapTool(bundleToolPath))
        {
            return "YAP";
        }

        if (!string.IsNullOrWhiteSpace(bundleToolPath))
        {
            string explicitPath = pathProvider.GetFullPath(bundleToolPath);
            if (!pathProvider.FileExists(explicitPath))
            {
                throw new FileNotFoundException($"Bundle extractor not found: {explicitPath}");
            }

            return explicitPath;
        }

        string defaultTool = Path.Combine(repoRoot, "tools", "libbndl-extractor", "build", "volatility_libbndl_extract.exe");
        if (pathProvider.FileExists(defaultTool))
        {
            return defaultTool;
        }

        string buildScript = Path.Combine(repoRoot, "tools", "libbndl-extractor", "build.ps1");
        processRunner.RunAndCapture("powershell", $"-ExecutionPolicy Bypass -File \"{buildScript}\"", repoRoot);

        if (!pathProvider.FileExists(defaultTool))
        {
            throw new FileNotFoundException($"Failed to build bundle extractor at {defaultTool}");
        }

        return defaultTool;
    }

    private string GetSessionRoot(string repoRoot, string? workingDirectory)
    {
        if (!string.IsNullOrWhiteSpace(workingDirectory))
        {
            return pathProvider.GetFullPath(workingDirectory);
        }

        string stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        return Path.Combine(repoRoot, ".tmp", "game-autotest", stamp);
    }

    private GameInstall DetectGame(string gamePath)
    {
        string fullPath = pathProvider.GetFullPath(gamePath);
        if (!pathProvider.DirectoryExists(fullPath))
        {
            throw new DirectoryNotFoundException($"Game directory not found: {fullPath}");
        }

        if (pathProvider.FileExists(Path.Combine(fullPath, "BurnoutPR.exe")) ||
            pathProvider.FileExists(Path.Combine(fullPath, "BurnoutPR_trial.exe")))
        {
            return new GameInstall(Path.GetFileName(fullPath), fullPath, Platform.TUB);
        }

        if (Directory.EnumerateFiles(fullPath, "*.xex", SearchOption.TopDirectoryOnly).Any())
        {
            return new GameInstall(Path.GetFileName(fullPath), fullPath, Platform.X360);
        }

        throw new InvalidOperationException($"Unable to infer platform for game directory: {fullPath}");
    }

    private static bool IsSupportedType(ResourceType resourceType)
    {
        return RoundTripTypes.Contains(resourceType) || ImportOnlyTypes.Contains(resourceType);
    }

    private static string GetTypeSummary(IEnumerable<ResourceType> resourceTypes)
    {
        List<string> labels = resourceTypes
            .Distinct()
            .Select(GetTypeLabel)
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

    private static string GetTypeLabel(ResourceType resourceType)
    {
        return Enum.IsDefined(typeof(ResourceType), resourceType)
            ? resourceType.ToString()
            : $"0x{(uint)resourceType:X8}";
    }

    private static Platform GetPortTarget(Platform sourcePlatform)
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

    private static string NormalizeYaml(string yaml)
    {
        IEnumerable<string> lines = yaml
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n')
            .Where(line => !line.TrimStart().StartsWith("ImportedFileName:", StringComparison.Ordinal));

        return string.Join('\n', lines).Trim();
    }

    private static BinaryComparisonResult CompareFiles(string originalPath, string exportedPath)
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

    private static bool IsSkippableTextureOp(Exception ex)
    {
        return ex.Message.Contains("DDS export is not supported", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("Failed to find associated bitmap data", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSkippableBundleError(Exception ex)
    {
        return ex.Message.Contains("Assertion failed: m_flags & Compressed", StringComparison.OrdinalIgnoreCase);
    }

    private static string SanitizePath(string value)
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
