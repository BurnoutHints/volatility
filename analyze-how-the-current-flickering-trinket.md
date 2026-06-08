# Volatility — Library Split & Message System Architecture

## Context

Volatility today is a single `net9.0` console `Exe` project. Per AGENTS.md, CLI command classes in [CLI/Commands/](Volatility/CLI/Commands/) are meant to stay thin, delegating to classes in [Operations/](Volatility/Operations/). This boundary is respected in most places but is blurred by four things:

1. **Direct `Console.*` calls scattered across every layer** — commands, operations, [Resources/ResourceFactory.cs](Volatility/Resources/ResourceFactory.cs) line 42, [Utilities/ProcessUtilities.cs](Volatility/Utilities/ProcessUtilities.cs), [Utilities/PS3TextureUtilities.cs](Volatility/Utilities/PS3TextureUtilities.cs), [CLI/Commands/AutotestCommand.cs](Volatility/CLI/Commands/AutotestCommand.cs). There is no `ILogger`, no `IProgress<T>`, no `CancellationToken`, and no DI container.
2. **Environment lookups inside operations** — e.g. [Operations/Resources/CreateResourceOperation.cs](Volatility/Operations/Resources/CreateResourceOperation.cs) calls `EnvironmentUtilities.GetEnvironmentDirectory(EnvironmentDirectory.Resources)` instead of receiving paths.
3. **Static-utility coupling** — operations invoke [Utilities/DxcShaderCompiler.cs](Volatility/Utilities/DxcShaderCompiler.cs), [Utilities/PS3TextureUtilities.cs](Volatility/Utilities/PS3TextureUtilities.cs), [Utilities/ProcessUtilities.cs](Volatility/Utilities/ProcessUtilities.cs) via static methods, which prevents substituting alternate implementations (e.g. a hosted GUI or a CI sandbox with a different DXC location).
4. **Validation logic living in CLI command classes** — [CLI/Commands/CreateResourceCommand.cs](Volatility/CLI/Commands/CreateResourceCommand.cs), [ImportResourceCommand.cs](Volatility/CLI/Commands/ImportResourceCommand.cs), [ExportResourceCommand.cs](Volatility/CLI/Commands/ExportResourceCommand.cs), [ImportStringTableCommand.cs](Volatility/CLI/Commands/ImportStringTableCommand.cs) all carry parse/validate/print-error logic that would need to move into an operation (or a shared validator) to be reusable from a second front-end.

Additionally, [CLI/Commands/AutotestCommand.cs](Volatility/CLI/Commands/AutotestCommand.cs) has no operation backing — all round-trip validation lives in the command and reports via `Console.ForegroundColor` writes.

The empty [Vantage/](Vantage/) directory and the current `vantage` branch indicate a planned second front-end (likely a GUI). That is the motivating use case for making Volatility a library.

**Goal of this plan:** deliver, in phased PRs inside the current single project, the foundation for Volatility-as-a-library — a subscribable message bus, structured operation contracts, DI-friendly service wiring — then split the repo into a two-project solution (`Volatility.Core` library + `Volatility.Cli` executable) once the new contracts are stable.

**Decisions locked in from the clarifying pass:**
- Message system: **custom `IMessageSink` + `MessageBus`** (no MS.Extensions.Logging, no Rx).
- Project split: **2 projects — `Volatility.Core` + `Volatility.Cli`** (contracts live in a `Volatility.Abstractions` *namespace* inside Core, not as a separate DLL).
- Rollout: **phased in-place, then split**.
- DI: **`Microsoft.Extensions.DependencyInjection`**.

## Target Architecture

### Final layout (after Phase 5)

```
Volatility.sln
├── Directory.Build.props              # LangVersion, Nullable, TreatWarningsAsErrors
├── src/
│   ├── Volatility.Core/               # class-library DLL
│   │   ├── Abstractions/              # namespace — contracts (no external deps leak)
│   │   │   ├── Messaging/             # IMessageSink, VolatilityMessage, MessageSeverity, MessageCategory
│   │   │   ├── Operations/            # IOperation<TReq,TRes>, OperationResult<T>, OperationProgress, OperationIssue
│   │   │   └── Services/              # IResourceFactory, IShaderCompiler, IProcessRunner, IPathProvider
│   │   ├── Resources/                 # moved from Volatility/Resources/
│   │   ├── Utilities/                 # moved from Volatility/Utilities/
│   │   ├── Operations/                # moved from Volatility/Operations/
│   │   ├── Messaging/                 # MessageBus impl, built-in sinks (InMemory, Null)
│   │   └── Hosting/
│   │       └── VolatilityServiceCollectionExtensions.cs   # services.AddVolatilityCore()
│   │
│   └── Volatility.Cli/                # OutputType=Exe
│       ├── Program.cs                 # replaces Frontend.Main; composes ServiceCollection
│       ├── CLI/ICommand.cs            # stays internal to CLI
│       ├── CLI/Commands/              # 10 commands, presenters only
│       ├── Messaging/
│       │   └── ConsoleMessageSink.cs  # color/verbose routing; the only place Console.* is allowed
│       ├── tools/dxc/**               # copy-to-output stays here, not in Core
│       └── Volatility.Cli.csproj      # PublishSingleFile, PublishTrimmed, tools/dxc copy
│
└── tests/
    └── Volatility.Core.Tests/         # unit + integration tests against operations
```

`Volatility.Vantage` (future GUI) lives outside this plan but will reference `Volatility.Core` the same way `Volatility.Cli` does.

### Message bus (replacing every `Console.*`)

All contracts in `Volatility.Core/Abstractions/Messaging/`:

```csharp
public enum MessageSeverity { Verbose, Info, Success, Warning, Error }
public enum MessageCategory { General, Resource, Texture, Shader, StringTable, Autotest, Process, Cli }

public readonly record struct VolatilityMessage(
    MessageSeverity Severity,
    MessageCategory Category,
    string Text,
    string? Source,                                       // e.g. "ImportResourceOperation"
    IReadOnlyDictionary<string, object?>? Data = null);   // structured fields for GUIs / JSON sinks

public interface IMessageSink
{
    void Publish(in VolatilityMessage message);
}

public interface IMessageBus : IMessageSink
{
    IDisposable Subscribe(IMessageSink sink);
    IDisposable Subscribe(MessageSeverity minSeverity, Action<VolatilityMessage> handler);
}
```

- `MessageBus` (thread-safe multicast) is the default `IMessageBus` registered in DI.
- `ConsoleMessageSink` (in `Volatility.Cli`) is the **only** code in the entire solution that is allowed to call `Console.*`. Color mapping: Error=Red, Warning=Yellow, Success=Green, Info=default, Verbose=DarkGray.
- Library consumers build their own sinks — a WPF `ObservableCollection` sink for Vantage, a JSON-lines sink for CI, a capture sink for tests. Each is a one-class implementation of `IMessageSink`.
- A small `VolatilityLog` helper (extension methods on `IMessageSink` like `.Info(text, source)`, `.Warning(...)`, etc.) keeps call sites concise.

### Operation contract

All contracts in `Volatility.Core/Abstractions/Operations/`:

```csharp
public interface IOperationRequest { }

public interface IOperation<in TRequest, TResult>
    where TRequest : IOperationRequest
{
    Task<OperationResult<TResult>> ExecuteAsync(
        TRequest request,
        IProgress<OperationProgress>? progress,
        CancellationToken cancellationToken);
}

public readonly record struct OperationResult<T>(
    bool Success,
    T? Value,
    IReadOnlyList<OperationIssue> Issues);

public sealed record OperationProgress(string Stage, double? Completion, string? Detail);
public sealed record OperationIssue(MessageSeverity Severity, string Code, string Message, string? Source);
```

Rules enforced across the codebase:
- **No `Console.*` calls** in `Volatility.Core` — ever. Enforced by code review and (eventually) an analyzer; CI grep can catch it cheaply.
- Operations **receive paths on the request**, not via `EnvironmentUtilities`. An `IPathProvider` is injected into the CLI composition root to resolve defaults (`data/ResourceDB`, `data/Resources`, `tools/`, `data/Splicer/`) — the library does not assume any filesystem layout.
- Operations **return `OperationResult<T>`**, not `void` or raw `Task`. Validation failures become issues with severity `Error`; success/warning cases carry a `Value`.
- Every long-running operation **takes `CancellationToken`** and threads it through its loops (today none do).

### DI composition

`Volatility.Core/Hosting/VolatilityServiceCollectionExtensions.cs`:

```csharp
public static IServiceCollection AddVolatilityCore(this IServiceCollection services)
{
    services.TryAddSingleton<IMessageBus, MessageBus>();
    services.TryAddSingleton<IMessageSink>(sp => sp.GetRequiredService<IMessageBus>());

    services.TryAddSingleton<IResourceFactory, DefaultResourceFactory>();
    services.TryAddSingleton<IShaderCompiler, DxcShaderCompilerAdapter>();
    services.TryAddSingleton<IProcessRunner, DefaultProcessRunner>();
    services.TryAddSingleton<IPathProvider, EnvironmentPathProvider>();

    // Operations registered transient — each invocation is a fresh handler
    services.AddTransient<IOperation<CreateResourceRequest, CreateResourceResult>, CreateResourceOperation>();
    services.AddTransient<IOperation<ImportResourceRequest, ImportResourceResult>, ImportResourceOperation>();
    // ... one line per operation

    return services;
}
```

`Volatility.Cli/Program.cs`:

```csharp
var services = new ServiceCollection();
services.AddVolatilityCore();
services.AddSingleton<IMessageSink>(sp => new ConsoleMessageSink(verbose: verboseFlag));
var sp = services.BuildServiceProvider();
sp.GetRequiredService<IMessageBus>().Subscribe(sp.GetRequiredService<IMessageSink>());
// Dispatch command → resolve IOperation<TReq,TRes> → await → render issues.
```

### Public API surface

- **Public in `Volatility.Core`**: the `Abstractions` namespace, `Resource` + concrete resource classes, `ResourceFactory` + `IResourceFactory`, operation classes, utility classes already `public` (binary readers/writers, YAML helpers, texture helpers).
- **Internal in `Volatility.Core`**: `MessageBus`, adapter classes (`DxcShaderCompilerAdapter`, `DefaultProcessRunner`), anything only the DI wiring needs.
- **Internal in `Volatility.Cli`**: everything — CLI is not a library.
- `[assembly: InternalsVisibleTo("Volatility.Cli")]` and `"Volatility.Core.Tests"` added to `Volatility.Core` so internals stay narrow but the CLI and tests can reach what they need.
- New `Directory.Build.props` at repo root: `<LangVersion>latest</LangVersion>`, `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`.

### Utility reorganization

The 31 classes under [Volatility/Utilities/](Volatility/Utilities/) mix four distinct roles today: pure byte/math helpers, stream-wrapping reader/writer tools, environment-coupled glue that reaches into the executable's directory layout or a JSON sidecar, and external-tool shellouts. They all share one namespace and one "`public static` helper" convention, which makes it hard to tell from a call site which helpers are safe inside library code and which smuggle in filesystem, process, or `Console` side-effects. Fixing that is a prerequisite for the message-bus + library split — not a separate stream of work.

**Bucket A — Pure, stateless helpers. Stay as `static class`, move into `Volatility.Core` unchanged.**

Byte-level and math-only; no filesystem, no process, no `Console`.
[EndianUtilities.cs](Volatility/Utilities/EndianUtilities.cs), [DataUtilities.cs](Volatility/Utilities/DataUtilities.cs), [PaddingUtilities.cs](Volatility/Utilities/PaddingUtilities.cs), [MatrixUtilities.cs](Volatility/Utilities/MatrixUtilities.cs), [CgsIDUtilities.cs](Volatility/Utilities/CgsIDUtilities.cs), [DictUtilities.cs](Volatility/Utilities/DictUtilities.cs), [TypeUtilities.cs](Volatility/Utilities/TypeUtilities.cs), [ResourceUtilities.cs](Volatility/Utilities/ResourceUtilities.cs), [DxbcReflectionParser.cs](Volatility/Utilities/DxbcReflectionParser.cs); plus the pure halves of [DDSTextureUtilities.cs](Volatility/Utilities/DDSTextureUtilities.cs) and [X360TextureUtilities.cs](Volatility/Utilities/X360TextureUtilities.cs) (header generation, pixel-format swaps, tiling math) and the Morton encode/decode portions of [PS3TextureUtilities.cs](Volatility/Utilities/PS3TextureUtilities.cs).

**Bucket B — Reader/writer tools. Stateful but library-shape; no extraction.**

[BitReader.cs](Volatility/Utilities/BitReader.cs), [EndianAwareBinaryReader.cs](Volatility/Utilities/EndianAwareBinaryReader.cs) / [Writer.cs](Volatility/Utilities/EndianAwareBinaryWriter.cs), [ResourceBinaryReader.cs](Volatility/Utilities/ResourceBinaryReader.cs) / [Writer.cs](Volatility/Utilities/ResourceBinaryWriter.cs), and all YAML converters/inspectors ([BitArrayYamlTypeConverter.cs](Volatility/Utilities/BitArrayYamlTypeConverter.cs), [StrongIDYamlTypeConverter.cs](Volatility/Utilities/StrongIDYamlTypeConverter.cs), [StringEnumYamlTypeConverter.cs](Volatility/Utilities/StringEnumYamlTypeConverter.cs), [FieldDescriptor.cs](Volatility/Utilities/FieldDescriptor.cs), [IncludeFieldsTypeInspector.cs](Volatility/Utilities/IncludeFieldsTypeInspector.cs), [ResourceYamlDeserializer.cs](Volatility/Utilities/ResourceYamlDeserializer.cs), [ResourceYamlTypeConverter.cs](Volatility/Utilities/ResourceYamlTypeConverter.cs)).

**Bucket C — Environment-coupled. Extract behind a service interface; default impl in `Volatility.Core`, resolved via DI.**

| Current utility | New abstraction (`Volatility.Abstractions.Services`) | Default impl | Consumers today |
|---|---|---|---|
| [EnvironmentUtilities.cs](Volatility/Utilities/EnvironmentUtilities.cs) | `IPathProvider` | `EnvironmentPathProvider` | `CreateResourceOperation`, `DxcShaderCompiler`, autotest, import paths |
| [WorkspaceUtilities.cs](Volatility/Utilities/WorkspaceUtilities.cs) | Folds into `IPathProvider.RepositoryRoot` | same | game-mode autotest |
| `ResourceDB.json` loader inside [ResourceIDUtilities.cs](Volatility/Utilities/ResourceIDUtilities.cs) | `IResourceDbLookup` | `FileResourceDbLookup` | any ID→asset-name resolution in import paths |
| [StringTableStorageUtilities.cs](Volatility/Utilities/StringTableStorageUtilities.cs) | `IStringTableStore` | `FileStringTableStore` | [ImportStringTableOperation.cs](Volatility/Operations/StringTables/ImportStringTableOperation.cs) |
| Resource write-to-disk logic spread across [SaveResourceOperation.cs](Volatility/Operations/Resources/SaveResourceOperation.cs) and command code | `IResourceStore` | `FileResourceStore` | `SaveResourceOperation`, `ExportResourceOperation`, `CreateResourceOperation` |

Pure helpers inside those files that don't touch the filesystem stay `static` (e.g. `ResourceIDUtilities.FlipResourceIDEndian`, hex parsing, filename parsing).

**Bucket D — External-tool coupled. Wrap behind a runner.**

- [ProcessUtilities.cs](Volatility/Utilities/ProcessUtilities.cs) → `IProcessRunner`. Default-handler `Console.WriteLine` becomes an `IMessageSink.Publish(Verbose, Process, line)`.
- [DxcShaderCompiler.cs](Volatility/Utilities/DxcShaderCompiler.cs) → `IShaderCompiler`.
- The `gtf2dds.exe` shellout inside [PS3TextureUtilities.cs](Volatility/Utilities/PS3TextureUtilities.cs) → calls `IProcessRunner`; its pure Morton/tile code stays where it is; the verbose `Console.WriteLine` at lines 168, 169, 188, 191, 195 becomes `IMessageSink` publishes.

**Files that mix roles and must be split, not renamed.**

- **[TextureBitmapUtilities.cs](Volatility/Utilities/TextureBitmapUtilities.cs)** — path-resolution helpers (filesystem) move behind `IPathProvider`/`IResourceStore`; byte-normalization helpers (tiling/Morton) stay as static functions in the same file.
- **[PS3TextureUtilities.cs](Volatility/Utilities/PS3TextureUtilities.cs)** and **[ResourceIDUtilities.cs](Volatility/Utilities/ResourceIDUtilities.cs)** — as described in Buckets C and D above.

The full list of services introduced is therefore: `IMessageBus`/`IMessageSink`, `IResourceFactory`, `IShaderCompiler`, `IProcessRunner`, `IPathProvider`, `IResourceDbLookup`, `IResourceStore`, `IStringTableStore`, and a small `IConfirmationProvider` (for the import-overwrite prompt lifted out of [ImportResourceOperation.cs](Volatility/Operations/Resources/ImportResourceOperation.cs) line 128).

**Deferred: the resource-constructor I/O boundary.**

Today most concrete resource classes (`TexturePC`, `RenderableBPR`, etc.) perform file reading and parsing inside their `(string path)` or `(string path, Endian)` constructor, invoked via reflection from [Resources/ResourceFactory.cs](Volatility/Resources/ResourceFactory.cs). This pattern is ingrained and is **not** changed in Phases 1–5 of this plan. It is the largest remaining obstacle to using the library without a filesystem and is called out here only so the earlier phases don't lock in "path string" as the only input.

Follow-up work, tracked separately from this plan:
- Introduce `ResourceReader`/`ResourceWriter` under `Volatility.Core` that take a `Stream` + `(ResourceType, Platform, Endian, Arch)` and return/consume a `Resource`.
- Retain the current path constructors as compatibility shims that open a `FileStream` and delegate to `ResourceReader`.
- Once all operations go through the reader/writer, deprecate the path constructors.

The service abstractions introduced in Phase 1 are designed around `Stream` + metadata where possible so that this later migration does not require a second round of interface churn.

## Phased Migration

Each phase is a single PR, each ships independently green, and `autotest --games=...` on a real Burnout install is the parity gate for anything that touches resource I/O.

### Phase 1 — Introduce abstractions in place

Still one project. No project split yet. Adds:

- `Volatility/Abstractions/Messaging/` — `IMessageSink`, `IMessageBus`, `VolatilityMessage`, `MessageSeverity`, `MessageCategory`, the extension-method facade `VolatilityLog`.
- `Volatility/Abstractions/Operations/` — `IOperation<TReq,TRes>`, `OperationResult<T>`, `OperationProgress`, `OperationIssue`, `IOperationRequest`.
- `Volatility/Messaging/MessageBus.cs` — default impl.
- `Volatility/Messaging/ConsoleMessageSink.cs` — in the `Volatility.CLI` namespace, wired up in [Frontend.cs](Volatility/Frontend.cs) before any command runs.
- `Volatility/Abstractions/Services/` — `IShaderCompiler`, `IProcessRunner`, `IPathProvider`, with thin adapter impls next to them.

Migrate the "cheap" call sites first:
- [Resources/ResourceFactory.cs](Volatility/Resources/ResourceFactory.cs) line 42 — swap `Console.WriteLine` for `sink.Publish(Info, Resource, "Constructing...")`. Inject `IMessageSink` via a new overload; keep the parameterless `CreateResource` delegating to a singleton sink for backward compatibility during the migration.
- [Utilities/ProcessUtilities.cs](Volatility/Utilities/ProcessUtilities.cs) — default output handler becomes `IMessageSink.Publish(Verbose, Process, line)` instead of `Console.WriteLine`.

No command/operation behavior changes yet. At the end of Phase 1, `Console.*` writes remain in commands and operations but the infrastructure is present.

### Phase 2 — Migrate tier-1 operations

Operations that are already close to library-friendly; each gets the `IOperation<TReq,TRes>` contract and drops any env-lookup / console-write it has.

- [Operations/Resources/CreateResourceOperation.cs](Volatility/Operations/Resources/CreateResourceOperation.cs)
- [Operations/Resources/LoadResourceOperation.cs](Volatility/Operations/Resources/LoadResourceOperation.cs)
- [Operations/Resources/SaveResourceOperation.cs](Volatility/Operations/Resources/SaveResourceOperation.cs)
- [Operations/Resources/CreateShaderProgramBufferOperation.cs](Volatility/Operations/Resources/CreateShaderProgramBufferOperation.cs)
- [Operations/StringTables/MergeStringTableEntriesOperation.cs](Volatility/Operations/StringTables/MergeStringTableEntriesOperation.cs)
- [Operations/StringTables/LoadResourceDictionaryOperation.cs](Volatility/Operations/StringTables/LoadResourceDictionaryOperation.cs)

Each CLI command that uses these is updated to resolve the new interface and render `OperationResult<T>.Issues` through the message sink.

### Phase 2.5 — Stabilize contracts before tier-2 migration

This is a short corrective phase before Phase 3. It addresses issues discovered after Phase 2 that would otherwise make Phase 3 inconsistent and make Phase 4 non-mechanical. The goal is not to migrate the heavy operations yet; it is to make the architecture rules enforceable before more code is moved onto them.

Root cause from the post-Phase-2 review:
- The new abstractions exist, but there are now two operation shapes: canonical `IOperation<TReq,TRes>` handlers and older concrete `ExecuteAsync(...)` methods returning raw results or `Task`.
- Several request/result DTOs are `internal`, which works only because the project is still one assembly. After the split, external consumers cannot resolve or call the intended operation APIs.
- `ResourceFactory` is still a static dependency even though the plan's DI model requires `IResourceFactory`.
- Messaging has both DI (`IMessageBus`/`IMessageSink`) and a static global host (`VolatilityMessageHost`), so code can bypass the host-provided sink.
- Core-bound files still contain `Console.*`, including services and resource parsers that Phase 4 intends to move into `Volatility.Core`.
- `SaveResourceOperation` writes unconditionally, while import/create commands enforce overwrite behavior outside the save operation. This leaves overwrite semantics duplicated and inconsistent.
- `Directory.Build.props` with `TreatWarningsAsErrors=true` cannot be added safely while the current project still emits many warnings.

Phase 2.5 changes:

1. **Make operation DTOs library-visible.**
   - Change request/result records for already-migrated operations from `internal` to `public` where those operations are intended public Core API:
     - `CreateResourceRequest` / `CreateResourceResult`
     - `LoadResourceRequest` / `LoadResourceResult`
     - `SaveResourceRequest` / `SaveResourceResult`
     - `CreateShaderProgramBufferRequest` / `CreateShaderProgramBufferResult`
     - `LoadResourceDictionaryRequest` / `LoadResourceDictionaryResult`
     - `MergeStringTableEntriesRequest` / `MergeStringTableEntriesResult`
   - Keep implementation-only helper types internal.
   - Do not rely on future `InternalsVisibleTo` for operation contracts. `InternalsVisibleTo` is only for CLI/test access to true internals.

2. **Introduce `IResourceFactory` and make it the canonical construction path.**
   - Add `Volatility/Abstractions/Services/IResourceFactory.cs`.
   - Add a default adapter (`DefaultResourceFactory` or equivalent) that delegates to the current static `ResourceFactory`.
   - Register `IResourceFactory` in `AddVolatilityCore()`.
   - Update migrated operations to depend on `IResourceFactory` instead of calling `ResourceFactory.CreateResource` / `ResourceFactory.LoadResource` directly.
   - Keep the static `ResourceFactory` for now as the lower-level registry and compatibility surface, but new operation code must use `IResourceFactory`.
   - This is a transitional internal boundary, not a second behavior path: both routes must call the same registration/activation logic.

3. **Normalize `AddVolatilityCore()` registrations.**
   - Use `TryAddSingleton` for overridable services (`IMessageBus`, `IMessageSink`, `IPathProvider`, `IProcessRunner`, `IShaderCompiler`, `IResourceFactory`, stores/lookups).
   - Register all migrated operations by their `IOperation<TReq,TRes>` interface and avoid requiring command code to ask for concrete operation classes when an interface exists.
   - Leave not-yet-migrated Phase 3 operations registered as concrete types, but list them clearly as temporary exceptions.

4. **Decouple abstractions from messaging implementations.**
   - Remove the `Volatility.Abstractions.Messaging.VolatilityLog` dependency on `Volatility.Messaging.NullMessageSink`.
   - Either make a null sink implementation part of the abstractions contract area, or have `VolatilityLog` no-op when the sink is null.
   - Keep `VolatilityMessageHost` only as a temporary CLI bridge. New operation and service code must receive `IMessageSink`/`IMessageBus` through DI.

5. **Establish overwrite as an operation-level contract.**
   - Extend `SaveResourceRequest` with an `Overwrite` flag.
   - Make `SaveResourceOperation` return an `OperationResult<SaveResourceResult>` failure issue when the target exists and overwrite is false.
   - Update `CreateResourceCommand` and `ImportResourceCommand` to pass overwrite into the save operation instead of duplicating the file-exists check.
   - If an interactive confirmation prompt is still desired, that belongs in CLI before invoking the operation or in a deliberate `IConfirmationProvider`; the operation itself must have deterministic request-driven behavior.

6. **Move Phase-4-blocking `Console.*` out of Core-bound services/resources now.**
   - Replace `Console.*` in service files that will move to Core in Phase 4, especially `FileTextureBitmapStore`.
   - Replace parser/resource warnings in `Resources/` with `IMessageSink` only if the sink can be threaded without distorting parse/write APIs; otherwise convert these to structured warnings on a result-bearing operation path or defer them behind a clearly tracked pre-Phase-4 blocker.
   - CLI-only `Console.*` in `Frontend`, `ConsoleMessageSink`, `ClearCommand`, and `AutotestCommand` may remain until their planned phases, but they must not be moved to Core in Phase 4.

7. **Do a warnings-as-errors readiness pass without enabling it yet.**
   - Record the current build warning count.
   - Fix warnings introduced by Phases 1-2.5 in the abstraction, hosting, service, and migrated-operation files.
   - Leave legacy parser/YAML/trim warnings for a separate warning cleanup if they are outside the current contract work.
   - Do not add root `Directory.Build.props` with `TreatWarningsAsErrors=true` until the warning count is low enough that Phase 4 will not immediately fail.

8. **Add cheap enforcement checks.**
   - Add a documented verification command for source-level `Console.` grep against future Core-bound folders.
   - Add a documented verification command for raw operation shapes (`Task ExecuteAsync`, raw `ImportResourceResult`, concrete operation injection) so Phase 3 does not add more exceptions.

Phase 2.5 acceptance criteria:
- `dotnet build Volatility.sln` is green.
- All migrated Phase 2 operations have public request/result DTOs and are resolvable through `IOperation<TReq,TRes>`.
- Migrated operations use `IResourceFactory` rather than static `ResourceFactory` calls.
- `AddVolatilityCore()` has a single clear registration style and host-overridable services.
- `SaveResourceOperation` owns overwrite behavior.
- No `Console.*` remains in service files that are planned to move to Core in Phase 4.
- Remaining `Console.*` locations are intentionally classified as CLI-only, Phase-3 targets, Phase-5 targets, or explicit pre-Phase-4 blockers.
- No new compatibility shims or fallback behavior are introduced.

### Phase 3 — Migrate tier-2 operations

- [Operations/Resources/ImportResourceOperation.cs](Volatility/Operations/Resources/ImportResourceOperation.cs) — replace `Console.WriteLine` (lines 89, 94, 107, 114) with `IMessageSink`; replace the overwrite prompt (line 128) with an `IConfirmationProvider` injected from the host (CLI = console prompt, GUI = modal, tests = auto-answer).
- [Operations/Resources/ExportResourceOperation.cs](Volatility/Operations/Resources/ExportResourceOperation.cs) — route shader-compile calls through `IShaderCompiler`.
- [Operations/StringTables/ImportStringTableOperation.cs](Volatility/Operations/StringTables/ImportStringTableOperation.cs) — drop verbose `Console.WriteLine` calls (lines 42, 63) for `IMessageSink.Verbose`.
- [Operations/Resources/TextureToDDSOperation.cs](Volatility/Operations/Resources/TextureToDDSOperation.cs) — extract a pure `byte[] ConvertToDDS(TextureBase, byte[])` that has no side effects; the operation becomes a thin orchestrator.

### Phase 3.5 — Remove pre-split Core blockers

This phase exists because Phase 4 moves `Operations/` into `Volatility.Core`, while `Volatility.Core` is not allowed to contain `Console.*` or concrete-only operation registrations. Do this before the project split so Phase 4 stays mechanical.

Scope is deliberately narrower than the original Phase 5 heavy refactors: make the remaining Core-bound operations contract-shaped and message-bus-backed, but do **not** rewrite texture conversion semantics unless verification coverage is available.

- **[Operations/Resources/PortTextureOperation.cs](Volatility/Operations/Resources/PortTextureOperation.cs).**
  - Add `PortTextureRequest : IOperationRequest` and `PortTextureResult`.
  - Implement `IOperation<PortTextureRequest, PortTextureResult>`.
  - Inject `IResourceFactory` and replace direct `ResourceFactory.LoadResource` / `ResourceFactory.CreateResource` calls.
  - Inject `IMessageSink` and replace all `Console.WriteLine` calls with `Verbose`, `Info`, `Success`, or `Warning` messages under `MessageCategory.Texture`.
  - Thread `CancellationToken` through the file loop and per-file tasks; stop launching unbounded `Task.Run` work if a simple async loop is sufficient.
  - Return output paths and issues through `OperationResult<PortTextureResult>`.
  - Keep the existing format-pair switch and bitmap conversion logic in place for now; the pure `TextureFormatConverter` extraction remains Phase 5.
- **[CLI/Commands/PortTextureCommand.cs](Volatility/CLI/Commands/PortTextureCommand.cs).**
  - Resolve `IOperation<PortTextureRequest, PortTextureResult>` instead of the concrete operation.
  - Keep command responsibility to arg parsing, request construction, issue rendering, and exit behavior.
- **[Operations/Autotest/GameAutotestOperation.cs](Volatility/Operations/Autotest/GameAutotestOperation.cs).**
  - Convert `GameAutotestOptions` into `GameAutotestRequest : IOperationRequest` or add a request DTO that preserves the same fields.
  - Implement `IOperation<GameAutotestRequest, GameAutotestSummary>`.
  - Inject `IMessageSink` and replace all `Console.WriteLine`, `Console.ForegroundColor`, and `Console.ResetColor` calls with structured messages. Use severity for result coloring at the CLI sink boundary: pass/success = `Success`, fail = `Error`, skip = `Warning`, progress = `Info` or `Verbose`.
  - Depend on `IOperation<PortTextureRequest, PortTextureResult>` instead of concrete `PortTextureOperation`.
  - Keep the current game-mode extraction, parity logic, and summary model intact; deeper recap/report reshaping is out of scope.
- **[CLI/Commands/AutotestCommand.cs](Volatility/CLI/Commands/AutotestCommand.cs).**
  - Resolve `IOperation<GameAutotestRequest, GameAutotestSummary>` for game-mode autotest.
  - Leave synthetic header comparison in the command until Phase 5 extracts `TextureRoundTripOperation`.
- **[Hosting/VolatilityServiceCollectionExtensions.cs](Volatility/Hosting/VolatilityServiceCollectionExtensions.cs).**
  - Replace concrete `AddTransient<PortTextureOperation>()` and `AddTransient<GameAutotestOperation>()` registrations with `IOperation<,>` registrations.

Phase 3.5 acceptance criteria:
- `dotnet build Volatility.sln` is green.
- `dotnet run --project Volatility/Volatility.csproj -- autotest --format=TUB` still exits successfully.
- Source grep for `Console.` under future Core-bound folders (`Volatility/Operations`, `Volatility/Resources`, `Volatility/Utilities`, `Volatility/Services`, `Volatility/Hosting`, `Volatility/Abstractions`) has no hits except files explicitly deferred outside Core.
- Source grep confirms no direct `ResourceFactory.CreateResource` / `ResourceFactory.LoadResource` calls remain in `PortTextureOperation`.
- Source grep confirms no concrete DI registrations or constructor injections remain for `PortTextureOperation` or `GameAutotestOperation`.
- If a real Burnout install is available, run `autotest --games=<real Burnout install>` because this phase touches game-mode orchestration and texture porting orchestration.

### Phase 4 — Split into two projects

Purely mechanical once the Phase 3.5 blockers are clean:

1. Create `src/Volatility.Core/Volatility.Core.csproj` (OutputType=Library, deps: Newtonsoft.Json, YamlDotNet, System.IO.Hashing, Microsoft.Extensions.DependencyInjection.Abstractions).
2. Create `src/Volatility.Cli/Volatility.Cli.csproj` (OutputType=Exe, PublishSingleFile, PublishTrimmed, `tools/dxc/**` copy, ref Volatility.Core, dep: Microsoft.Extensions.DependencyInjection).
3. Move files: `Resources/`, `Utilities/`, `Operations/`, `Abstractions/`, `Messaging/`, `Attributes/`, `Exceptions/`, root-level `Endian.cs`/`StrongID.cs`/`Types.cs` → `Volatility.Core`.
4. Move: `CLI/`, `Frontend.cs` (→ `Program.cs`), `ConsoleMessageSink.cs`, `volatility_icon.ico`, `tools/dxc/**` → `Volatility.Cli`.
5. Add `[assembly: InternalsVisibleTo("Volatility.Cli")]` and `"Volatility.Core.Tests"` to Core.
6. Add root `Directory.Build.props`.
7. Update `Volatility.sln` and [.github/workflows/dotnet.yml](.github/workflows/dotnet.yml) publish command to point at `Volatility.Cli.csproj`.

Namespaces: keep the existing `Volatility.Resources`, `Volatility.Utilities`, `Volatility.Operations.*`, `Volatility.CLI.Commands` — no renames, minimize churn. The `Abstractions` folder uses `Volatility.Abstractions.{Messaging,Operations,Services}` sub-namespaces.

### Phase 5 — Heavy refactors

- **[Operations/Resources/PortTextureOperation.cs](Volatility/Operations/Resources/PortTextureOperation.cs) (~500 lines).** With the operation already behind `IOperation<PortTextureRequest, PortTextureResult>`, extract the format-pair switch (lines 40–156) into a pure `TextureFormatConverter` class with no side effects. The orchestrator becomes a thin loop over files that calls the converter, streams progress, and publishes only orchestration messages.
- **[CLI/Commands/AutotestCommand.cs](Volatility/CLI/Commands/AutotestCommand.cs).** Extract a `TextureRoundTripOperation` that returns `RoundTripResult { PassedProperties, Mismatches }`; the command becomes a presenter. The reflection-based `TestCompareHeaders` logic moves into a `ResourcePropertyComparer` helper in `Volatility.Core`.
- Add a `Volatility.Core.Tests` project with a smoke test that resolves `IOperation<ImportResourceRequest,ImportResourceResult>` via DI, registers a capture sink, runs against a small fixture, and asserts on both the result and the messages published. This validates the library is actually library-shaped.

## Critical Files

Today's single project:

- [Volatility/Volatility.csproj](Volatility/Volatility.csproj) — splits into two csproj files in Phase 4.
- [Volatility/Frontend.cs](Volatility/Frontend.cs) — becomes `Volatility.Cli/Program.cs`; command registry stays, arg parser stays, REPL stays.
- [Volatility/CLI/ICommand.cs](Volatility/CLI/ICommand.cs) — `ShowUsage()` drops its `Console.WriteLine` (line 21) in favor of `IMessageSink`.
- [Volatility/Resources/ResourceFactory.cs](Volatility/Resources/ResourceFactory.cs) — Phase 1 target; line 42 `Console.WriteLine` removed.
- [Volatility/Utilities/ProcessUtilities.cs](Volatility/Utilities/ProcessUtilities.cs) — Phase 1 target; default handler becomes a sink publish.
- [Volatility/Utilities/EnvironmentUtilities.cs](Volatility/Utilities/EnvironmentUtilities.cs) — wrapped by `IPathProvider`; stays as the default impl.
- [Volatility/Utilities/DxcShaderCompiler.cs](Volatility/Utilities/DxcShaderCompiler.cs) — wrapped by `IShaderCompiler`.
- [Volatility/Operations/Resources/PortTextureOperation.cs](Volatility/Operations/Resources/PortTextureOperation.cs) — Phase 3.5 target; must be message-bus-backed and registered through `IOperation<PortTextureRequest, PortTextureResult>` before the project split.
- [Volatility/Operations/Autotest/GameAutotestOperation.cs](Volatility/Operations/Autotest/GameAutotestOperation.cs) — Phase 3.5 target; game-mode autotest must stop writing to `Console.*` before moving operations into Core.
- Operation classes per phase — listed above.

Existing boilerplate to reuse:
- `EndianAwareBinaryReader/Writer`, `ResourceBinaryReader/Writer` — untouched; library-ready.
- `TypeUtilities.GetStaticPropertyValue` — used by `ICommand.ShowUsage`, keep.
- `YamlDotNet` custom inspectors/converters — untouched; library-ready.
- Existing `CreateResourceResult` / `ImportResourceResult` records — extend this pattern into `OperationResult<T>.Value`.

## Verification

Per-phase gate:
- `dotnet build Volatility.sln` must be green.
- `dotnet run --project <csproj> -- <existing-command>` must produce byte-identical outputs to pre-phase for the commands exercised.
- `dotnet run --project <csproj> -- autotest --format=TUB` (synthetic mode) must pass.

Phase 2.5 specific checks:
- Source grep for `Console.` in future Core-bound service/resource/operation folders is reviewed and every hit is either removed or assigned to Phase 3/Phase 5/pre-Phase-4 cleanup explicitly.
- Source grep confirms migrated operations no longer call `ResourceFactory.CreateResource` / `ResourceFactory.LoadResource` directly.
- A small DI smoke check resolves each migrated `IOperation<TReq,TRes>` from `AddVolatilityCore()`.
- `createresource` and `importresource` still honor `--overwrite` through `SaveResourceOperation`, not command-side duplicate file checks.

Phase 3.5 specific checks:
- Source grep for `Console.` in future Core-bound folders is clean before Phase 4 starts:
  `rg -n "Console\\." Volatility/Operations Volatility/Resources Volatility/Utilities Volatility/Services Volatility/Hosting Volatility/Abstractions`
- Source grep confirms `PortTextureOperation` no longer calls static `ResourceFactory` APIs:
  `rg -n "ResourceFactory\\.(CreateResource|LoadResource)" Volatility/Operations/Resources/PortTextureOperation.cs`
- Source grep confirms no concrete registrations/injections remain:
  `rg -n "AddTransient<PortTextureOperation|AddTransient<GameAutotestOperation|private readonly PortTextureOperation|private readonly GameAutotestOperation" Volatility`
- `porttexture` is smoke-tested with a representative texture pair when fixtures are available.

End-to-end gate after each phase that touches resource I/O (Phases 2, 3, 3.5, 5):
- `autotest --games=<real Burnout install>` — the authoritative parity check per AGENTS.md. A green synthetic run is not sufficient evidence for non-texture types.

Post-split (Phase 4) specific checks:
- `dotnet publish --configuration Release --runtime win-x64 --self-contained true src/Volatility.Cli/Volatility.Cli.csproj` produces a self-contained exe with `tools/dxc/**` present.
- `Volatility.Core.dll` has **zero** references to `System.Console` (verifiable with `ildasm` or `dotnet-ildasm` / a grep across the decompiled IL, plus a source-level grep for `Console.` in `src/Volatility.Core/`).
- A minimal consumer project that references only `Volatility.Core` compiles, resolves `IOperation<ImportResourceRequest, ImportResourceResult>` from `IServiceProvider`, registers a custom `IMessageSink`, and runs an import end-to-end against a fixture.

Phase 5 specific:
- `Volatility.Core.Tests` runs under `dotnet test`; at minimum: one round-trip test per resource type listed in `RoundTripTypes`, one capture-sink test proving operations emit the expected messages, one cancellation test proving a token aborts an in-flight import.
