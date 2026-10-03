# Advanced RimTalk - Prompt

This is the prompt-focused first slice of Advanced RimTalk. It does not replace RimTalk's trigger system, request pipeline, or model client. The settings page provides two prompt integration modes: embed Arti into the existing RimTalk prompt, or let Advanced RimTalk take over prompt message construction.

## Current behavior

In embed mode RimTalk still owns prompt entry order, `system`/`user`/`assistant` roles, chat history placement, and third-party prompt entries. When RimTalk renders an entry, Advanced RimTalk executes formal `{{% ... %}}` Arti blocks first, then preserves the existing Scriban pipeline. The older `art.*` expressions remain available as a compatibility layer.

The expansion runs against a snapshot captured for that render. Inserted text is protected until Scriban finishes, so a value containing `{{ ... }}` is not evaluated a second time.

Supported compatibility placeholders:

```text
{{ art.pawn_name }}
{{ art.recipient_name }}
{{ art.talk_type }}
{{ art.dialogue_type }}
{{ art.intent }}
{{ art.topic }}
{{ art.status }}
{{ art.prompt }}
{{ art.raw_prompt }}
{{ art.context }}
{{ art.pawn_context }}
{{ art.tick }}
{{ art.hour }}
{{ art.date }}
{{ art.season }}
{{ art.weather }}
{{ art.temperature }}
{{ art.wealth }}
{{ art.is_announcement }}
{{ art.is_monologue }}
{{ art.state }}
```

Common functions support both parenthesized and whitespace argument forms:

```text
{{ art.random_int(1, 6) }}
{{ art.random_float(0, 1) }}
{{ art.choose("calm", "urgent", "curious") }}
{{ art.default(art.topic, "unknown topic") }}
{{ art.join(", ", "name", art.pawn_name) }}
```

Multiline calls are supported:

```text
{{ art.random_int(
  1,
  6
) }}
```

This syntax is intentionally separate from native Scriban and from RimTalk's trigger system. The Arti executor uses an allowlisted runtime surface: prompt context values, RimTalk custom variables/hooks, module status, read-only RimWorld game data, output, text/escape helpers, and prompt-local random values. Game objects are exposed through public fields/properties and explicit query selectors; arbitrary methods are not invoked.

## Prompt builder replacement

Takeover is a fully custom message path. It does not promise RimTalk's original decoration semantics. Each enabled Prompt Part executes `{{% ... %}}` Arti first, followed by RimTalk's native `{{ ... }}` Scriban renderer. RimTalk still owns talk triggering, participant selection, AI client calls, and response handling. Its active preset assembly is bypassed, but Scriban is not replaced.

Prompt Parts are the only persisted takeover content source. Their order, enabled state, roles, and content control the messages. Legacy document-only Advanced RimTalk settings are not migrated. Preset import/export preserves template source, custom roles, position, in-chat depth, and main-history metadata without translating expressions. Takeover still uses its own history/order semantics, so importing a preset is not a guarantee of equivalent behavior.

Defaults include base instructions, JSONL output instructions, context, dialogue state, history, and the raw dialogue request. These are editable parts, not mandatory RimTalk decorations. Language instructions, mood/social effects, and the original history message sequence are not automatically preserved. Templates remain responsible for producing output compatible with RimTalk's response parser.

Takeover settings bound pawn context count (default 32), history count (40), and final prompt characters (24,000). Character budgeting shares space between parts, preserves short parts where possible, and logs truncation. It is not a tokenizer or a guarantee that a model's context window will fit. The synchronous build and render use a thread-local settings copy with a separate Context object, preserving RimTalk's memory and compact-history switches without replacing shared settings. Participant selection and previously stored history remain owned by RimTalk; this scope cannot recover participants or history already discarded upstream.

RimTalk 1.3.2+ is required. Embed retains the native simple/advanced preset and compact/legacy history paths; its context preview uses the same preset helpers. Takeover records RimTalk's causal request summary for subsequent dialogue history. Arti `json.format` and `json.anchor` use the current social-effects, player-request, and memory switches; `is_user` and `is_from_user` identify player requests. Arti-emitted template-looking text remains literal in embed, takeover, and preview.

## Optional HAR integration

Humanoid Alien Races (HAR) is detected without a hard dependency or a bundled `AlienRace.dll`. HAR humanlike races use their localized race Def label instead of the gene tracker's xenotype label, which otherwise reports baseliner for many alien races. This applies to RimTalk pawn context, decorated names, threat labels, native Scriban `pawn.race`, and Arti `pawn.race` / `pawn.info.race` in embed, takeover, and preview paths.

HAR race context works without Biotech and still respects RimTalk's `IncludeRace` setting. Vanilla `Human` and `CreepJoiner` remain xenotype-based even though HAR converts their Def class. Non-HAR behavior is unchanged. A HAR pawn's species label takes precedence even when it has a custom xenotype; its gene tracker and gene context remain available separately and are not modified.


## Optional memory integration

ExpandMemory write helpers are experimental, compatibility-sensitive APIs. They change the provider's memory or common-knowledge data. Callers must check returned results; success is not guaranteed across versions or memory types, and multi-field updates are not transactions. Exceptions routed through the bridge's operation logger are visible outside DevMode. Pinning uses the provider's maintainer; moving out of Active privatizes entries before layer migration. These paths still require focused integration tests and game/save validation.

## Settings UI

When `Nanaloveyuki.IrisMenus` is active, Advanced RimTalk registers its settings page plus separate `Arti Editor`, `Arti REPL`, and `Arti Documentation` SubItems through IrisMenus' public API. The Editor edits and analyzes the takeover document without executing it; the REPL keeps its own history and executes Arti against the current selection/map without creating a RimTalk talk request. The Documentation page embeds the Arti Markdown library, builds its navigation from the maintained documentation indexes, and presents categories and pages in a two-column layout. Enter runs in the REPL and Shift+Enter inserts a newline. The integration is optional and reflection-based: no `IrisMenus.dll` is shipped with this mod, and the original RimWorld settings dialog remains the fallback when IrisMenus is absent or incompatible.

`Arti Editor` is a multi-document workspace: **Open prompt** selects parts from any takeover preset, including inactive presets, without activating or executing them. Tabs show `preset / part`; each document keeps its own cursor, selection, scroll, undo/redo history, and preceding-declaration analysis context. The editor page initially opens the active preset's first System part, then keeps its explicit document bindings even when the active preset changes.

`Prompt Parts` opens the selected preset/part in a floating workspace; subsequent opens add tabs, while reopening an existing document selects its existing session. **Detach to window** moves the current tab into a draggable, resizable peer window for side-by-side reference across different prompts. **Move tab** merges it into another window or docks it back in the editor page without recreating its editor state. **Switch window** raises an existing workspace. Ctrl+Tab / Ctrl+Shift+Tab switches tabs; Ctrl+W closes the active tab. Closing a tab/window does not delete prompt content: edits write directly to the bound part. Deleting a preset or part removes its stale editor tabs.

Floating editors use RimWorld's normal dialog layer and click-to-front ordering, allow sibling windows and the preset browser to be selected, and block input to the game behind them. Only the foreground workspace may claim editor keyboard focus; menus and modal dialogs retain priority. Enter remains a newline rather than closing the editor or an underlying settings window. Tabs/windows are session-only; their layout is not saved across restarts.

The editor's **Docs** button opens a draggable, resizable documentation window. Hold **Control** and left-click a keyword inside an Arti block to open its indexed page in that window; an already-open window is reused. Supported names are language keywords, built-in functions/namespaces, known module aliases, and RimTalk/Mod-registered variables. Qualified members retain their namespace: `core.append`, `core.string.append`, and `memory.pawn.context` open different pages. Player-declared variables, functions, constants, and parameters are excluded, including names that shadow built-ins. Comments, string text, native Scriban, and Markdown examples do not trigger navigation; expressions inside Arti interpolated strings do. Control-hover underlines supported tokens and shows a navigation hint. Ordinary clicks continue editing, and documentation lookup never executes a prompt. The floating reader keeps navigation state separate from the settings documentation page; without IrisMenus it displays selectable Markdown source.

Verification covers workspace selection/transfer and cross-preset declaration isolation in `PromptChecks`, plus the compiled editor's direct writes, independent histories, saved selection, transfer identity, and external edits. Native Unity rendering, pointer/keyboard focus, resizing, and window stacking still require in-game validation; the standalone Mono check cannot exercise Unity's IMGUI runtime.

Documentation lookup checks cover qualified routes, declaration visibility, cross-block player globals, non-code boundaries, and registered-name precedence. A Mono smoke run against the production assembly exercised 16 lookup scenarios with embedded documentation. Native pointer hit-testing, hover visuals, and floating-window interaction still require in-game validation.

Takeover presets are stored separately from RimTalk presets. Existing takeover parts become the first preset without losing their content. The preset list supports creating, duplicating, renaming, deleting, and activating presets. Selecting a preset edits it; only Activate changes the runtime preset. Local/shared imports create a separate takeover preset instead of appending to the current parts. The redundant System document field has been removed from the main settings page.

Prompt Preview identifies the active mode/preset separately from the preview selection. Context previews can render another preset without activating it. Captured messages and generated context previews are separate results, labeled with their source preset and capture/generation time. Output uses a dark, selectable plain-text surface; rich-text-looking prompt content remains literal.

The documentation reader lays out Markdown tables and opens local Markdown links, with a Back action. HTTP(S) links open in the browser. IrisMenus search indexes document titles and paths and opens the selected document directly. Source mode and Copy all retain the Markdown text for copying.

`Prompt Preview` offers raw message-array JSON and plain text, with separate embed/takeover selection. This JSON describes prompt messages, not an AI response or a complete provider request body. By default it shows the latest messages generated in the current game. Viewing a capture does not execute scripts, write memories, or call the model. Outside a running game, or before a matching result exists, the page shows an empty state.

Experimental context execution is off by default. When enabled in a running game, it renders current templates against the selected pawn, or a colonist on the current map, and the entered preview request. Available pawn context and history are included; failed context reads and template errors appear separately from the output. Erroring Arti blocks and unresolved Scriban entries are skipped. Embed uses RimTalk preset assembly and request decoration; takeover uses its configured parts and message budget. Results are labeled as context previews, not captured requests. They are not an exact replay of every event, announcement, participant group, or third-party hook.

Template diagnostics in context previews identify the source preset and Prompt Part / RimTalk entry name, plus its one-based position in that preset. Arti diagnostics retain line/column coordinates relative to the source part. Scriban parse errors, compatibility-placeholder errors, unresolved entries, and render exceptions carry the same source label. Imported templates are stored as preset entries, not independently tracked files; the label points to the editable entry rather than inventing a filesystem path. Identical entry content is attributed separately. Context/history read failures retain their operation labels.

Active previews use isolated Arti/Scriban session variables and reject the bridge's explicit memory mutation calls. This is not a sandbox for arbitrary native Scriban or third-party callbacks, which is why context execution remains experimental. No model request is sent by the preview action.

`core.once(id, action)` runs a zero-argument function once per world and stores the record in mod settings. IrisMenus has a One-time runs subpage; the main settings page exposes the same list when IrisMenus is absent. Deleting a record lets that id run again. Prompt previews skip both the action and the write.

## Arti language design

The formal language and Prompt document design is maintained in [`docs/Arti/zh_cn/index.md`](docs/Arti/zh_cn/index.md). `tmp/dsl.md` remains an experimental sketch. The RimWorld runtime provider exposes game data under `core.game`, `core.world`, `core.maps`, `core.pawns`, `core.factions`, `core.settlements`, `core.world_objects`, `core.mods`, `core.defs`, `core.query`, and `core.find`. When RimTalk - Expand Memory is installed and active, Arti can import the optional `memory` module for its reflected memory and common-knowledge API.

The actual Arti document parser extracts only `{{% ... %}}` blocks. It preserves source locations, ignores Markdown fenced/inline code and native Scriban blocks, and leaves all text outside Arti blocks unchanged. The RimTalk symbol adapter reads the installed RimTalk variable registry at runtime; RimTalk - Expand Memory is discovered through reflection when present, so no compile-time reference is shipped.

Arti functions and `const` declarations are registered in a per-game global instance and can be referenced by later Arti blocks. The registry is recreated when the loaded game changes and is not serialized. Repeating the same declaration is idempotent; a different owner or source cannot replace an existing name. Ordinary `let` variables are block-local. Global functions resolve runtime data when called, so use a function such as `fn numbers() { return a }` for dynamic values instead of exposing mutable public state. `const` initializers must be static expressions; dynamic game values belong in functions.

For detailed Pawn data, use the read-only `pawn.info` object (or `core.pawn.info` for the current Pawn). It exposes structured age decomposition, health state, all Hediffs including hidden ones, needs, prompt context, and the public Pawn trackers; existing `pawn.age`, `pawn.health`, and raw tracker paths remain compatible.

## Build

`Directory.Build.props` shares the current RimTalk/Harmony reference paths between the mod and checks, with Windows and WSL defaults. RimTalk/Harmony come from `D:\References\Rimworld\Mods`; game assemblies come from `E:\Apps\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed`. Override `RimWorldDir` to select the game installation, or `RimWorldReferencesDir`, `RimWorldManagedDir`, `RimTalkAssemblyDir`, or `HarmonyAssemblyPath` to select individual reference locations.

```powershell
dotnet build AdvancedRimTalk.csproj -c Release
dotnet run --project Tests/PromptChecks.csproj -c Release
```

With only a newer .NET runtime installed, run the net9 checks using `dotnet run --project Tests/PromptChecks.csproj -c Release --roll-forward Major`. Compatibility smoke runs installed all production Harmony patches against RimTalk 1.3.2 and exercised actual Scriban/takeover builds, preview preset assembly, session isolation, and preset JSON round-tripping. These are headless managed-assembly checks with game-only environment reads isolated, not a loaded-save, Unity UI, or live model/streaming verification.

### Build and deploy

The scripts build Release and copy `AdvancedRimTalk.dll`, its optional PDB, `About`, `Languages`, `docs`, `README.md`, and `LICENSE` into `Mods/AdvancedRimTalk`. They refuse deployment while RimWorld is running or when the destination belongs to another mod, and verify every copied file with SHA-256. RimTalk, Scriban, Harmony, and game assemblies are not bundled. Files outside the copy manifest, including an existing `About/PublishedFileId.txt`, are preserved.

WSL/Linux (default game directory: `/mnt/e/Apps/Steam/steamapps/common/RimWorld`):

```bash
./scripts/deploy.sh
./scripts/deploy.sh --build-only
RIMWORLD_DIR=/path/to/RimWorld CONFIGURATION=Debug ./scripts/deploy.sh
```

Windows PowerShell (default game directory: `E:\Apps\Steam\steamapps\common\RimWorld`):

```powershell
.\scripts\build-and-deploy.ps1
.\scripts\build-and-deploy.ps1 -BuildOnly
.\scripts\build-and-deploy.ps1 -RimWorldDir 'E:\Apps\Steam\steamapps\common\RimWorld' -Configuration Debug
```

PowerShell also accepts `-GameModPath` for a custom destination and `-SkipBuild` to deploy existing `tmp/build` output. Repository paths reached through WSL UNC shares are normalized before deriving package-relative paths. Building in Windows PowerShell requires a Windows .NET SDK; a WSL-only SDK works with `deploy.sh`, not Windows `dotnet.exe`.
