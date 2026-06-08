# Volatility Autotest Recap

Generated (UTC+02:00): 2026-06-09 00:40:15
Games: `D:\Emulation\Emulators\Xenia\Xenia Burnout 5 v6\Burnout_tcartwright` | `C:\Program Files (x86)\Steam\steamapps\common\BurnoutPR`
* Failed: 5
* Passed with binary parity: 8
* Semi-passed (without binary parity): 23
* Skipped: 68

## Test Operation Summary

| Operation | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| binaryparity | 8 | 4 | 0 |
| bundleextract | 0 | 0 | 1 |
| candidate | 0 | 0 | 1 |
| import | 4 | 0 | 0 |
| porttexture | 4 | 0 | 0 |
| roundtrip | 11 | 1 | 0 |
| texturetodds | 4 | 0 | 0 |
| unsupported | 0 | 0 | 66 |

## Resource Type Outcomes

| Resource Type | Passed | Failed | Skipped | Overall |
| --- | ---: | ---: | ---: | --- |
| AISections | 0 | 0 | 2 | SKIP |
| AttribSysVault | 0 | 0 | 2 | SKIP |
| ChallengeList | 0 | 0 | 2 | SKIP |
| FlaptFile | 0 | 0 | 2 | SKIP |
| GuiPopup | 2 | 2 | 0 | FAIL |
| HudMessage | 0 | 0 | 2 | SKIP |
| HudMessageSequence | 0 | 0 | 2 | SKIP |
| HudMessageSequenceDictionary | 0 | 0 | 2 | SKIP |
| ICETakeDictionary | 0 | 0 | 2 | SKIP |
| IdList | 0 | 0 | 2 | SKIP |
| InstanceList | 4 | 0 | 1 | PASS |
| MassiveLookupTable | 0 | 0 | 2 | SKIP |
| Material | 0 | 0 | 2 | SKIP |
| MaterialState | 0 | 0 | 2 | SKIP |
| MaterialTechnique | 0 | 0 | 1 | SKIP |
| ParticleDescription | 0 | 0 | 2 | SKIP |
| ParticleDescriptionCollection | 0 | 0 | 2 | SKIP |
| PolygonSoupList | 0 | 0 | 2 | SKIP |
| ProfileUpgrade | 0 | 0 | 1 | SKIP |
| ProgressionData | 0 | 0 | 2 | SKIP |
| PropGraphicsList | 0 | 0 | 2 | SKIP |
| PropInstanceData | 0 | 0 | 2 | SKIP |
| Registry | 0 | 0 | 2 | SKIP |
| Renderable | 4 | 0 | 0 | PASS |
| RwShaderProgramBuffer | 0 | 0 | 2 | SKIP |
| Scene | 8 | 0 | 0 | PASS |
| ShaderTechnique | 0 | 0 | 2 | SKIP |
| StaticSoundMap | 0 | 0 | 2 | SKIP |
| StreetData | 0 | 0 | 2 | SKIP |
| Texture | 13 | 3 | 0 | FAIL |
| TextureNameMap | 0 | 0 | 2 | SKIP |
| TextureState | 0 | 0 | 2 | SKIP |
| TrafficData | 0 | 0 | 2 | SKIP |
| TriggerData | 0 | 0 | 2 | SKIP |
| VertexDescriptor | 0 | 0 | 2 | SKIP |
| VFXMeshCollection | 0 | 0 | 2 | SKIP |
| VFXPropCollection | 0 | 0 | 2 | SKIP |
| WorldPainter2D | 0 | 0 | 2 | SKIP |
| ZoneList | 0 | 0 | 2 | SKIP |

## Case Details

| Game | Resource Type | Operation | Name | Outcome | Details |
| --- | --- | --- | --- | --- | --- |
| Burnout_tcartwright | AISections | unsupported | AISections | SKIP | Discovered in AI.DAT. No Volatility autotest handler exists for this resource type. |
| Burnout_tcartwright | ProgressionData | unsupported | ProgressionData | SKIP | Discovered in PROGRESSION.DAT. No Volatility autotest handler exists for this resource type. |
| Burnout_tcartwright | StreetData | unsupported | StreetData | SKIP | Discovered in STREETDATA.DAT. No Volatility autotest handler exists for this resource type. |
| Burnout_tcartwright | TriggerData | unsupported | TriggerData | SKIP | Discovered in TRIGGERS.DAT. No Volatility autotest handler exists for this resource type. |
| Burnout_tcartwright | HudMessage | unsupported | HudMessage | SKIP | Discovered in HUDMESSAGES.HM. No Volatility autotest handler exists for this resource type. |
| Burnout_tcartwright | HudMessageSequence | unsupported | HudMessageSequence | SKIP | Discovered in HUDMESSAGESEQUENCES.HMSC. No Volatility autotest handler exists for this resource type. |
| Burnout_tcartwright | HudMessageSequenceDictionary | unsupported | HudMessageSequenceDictionary | SKIP | Discovered in HUDMESSAGESEQUENCES.HMSC. No Volatility autotest handler exists for this resource type. |
| Burnout_tcartwright | TrafficData | unsupported | TrafficData | SKIP | Discovered in B5TRAFFIC.BNDL. No Volatility autotest handler exists for this resource type. |
| Burnout_tcartwright | MaterialTechnique | unsupported | MaterialTechnique | SKIP | Discovered in GLOBALBACKDROPS.BNDL. No Volatility autotest handler exists for this resource type. |
| Burnout_tcartwright | TextureState | unsupported | TextureState | SKIP | Discovered in GLOBALBACKDROPS.BNDL. No Volatility autotest handler exists for this resource type. |
| Burnout_tcartwright | Material | unsupported | Material | SKIP | Discovered in GLOBALBACKDROPS.BNDL. No Volatility autotest handler exists for this resource type. |
| Burnout_tcartwright | VertexDescriptor | unsupported | VertexDescriptor | SKIP | Discovered in GLOBALBACKDROPS.BNDL. No Volatility autotest handler exists for this resource type. |
| Burnout_tcartwright | MaterialState | unsupported | MaterialState | SKIP | Discovered in GLOBALBACKDROPS.BNDL. No Volatility autotest handler exists for this resource type. |
| Burnout_tcartwright | MassiveLookupTable | unsupported | MassiveLookupTable | SKIP | Discovered in MASSIVETABLE.BIN. No Volatility autotest handler exists for this resource type. |
| Burnout_tcartwright | AttribSysVault | unsupported | AttribSysVault | SKIP | Discovered in SURFACELIST.BIN. No Volatility autotest handler exists for this resource type. |
| Burnout_tcartwright | ICETakeDictionary | unsupported | ICETakeDictionary | SKIP | Discovered in CAMERAS.BUNDLE. No Volatility autotest handler exists for this resource type. |
| Burnout_tcartwright | FlaptFile | unsupported | FlaptFile | SKIP | Discovered in FLAPTHUD.BUNDLE. No Volatility autotest handler exists for this resource type. |
| Burnout_tcartwright | ParticleDescription | unsupported | ParticleDescription | SKIP | Discovered in PARTICLES.BUNDLE. No Volatility autotest handler exists for this resource type. |
| Burnout_tcartwright | TextureNameMap | unsupported | TextureNameMap | SKIP | Discovered in PARTICLES.BUNDLE. No Volatility autotest handler exists for this resource type. |
| Burnout_tcartwright | VFXMeshCollection | unsupported | VFXMeshCollection | SKIP | Discovered in PARTICLES.BUNDLE. No Volatility autotest handler exists for this resource type. |
| Burnout_tcartwright | VFXPropCollection | unsupported | VFXPropCollection | SKIP | Discovered in PARTICLES.BUNDLE. No Volatility autotest handler exists for this resource type. |
| Burnout_tcartwright | ParticleDescriptionCollection | unsupported | ParticleDescriptionCollection | SKIP | Discovered in PARTICLES.BUNDLE. No Volatility autotest handler exists for this resource type. |
| Burnout_tcartwright | Registry | unsupported | Registry | SKIP | Discovered in PLAYBACKREGISTRY.BUNDLE. No Volatility autotest handler exists for this resource type. |
| Burnout_tcartwright | ZoneList | unsupported | ZoneList | SKIP | Discovered in PVS.BNDL. No Volatility autotest handler exists for this resource type. |
| Burnout_tcartwright | ChallengeList | unsupported | ChallengeList | SKIP | Discovered in ONLINECHALLENGES.BNDL. No Volatility autotest handler exists for this resource type. |
| Burnout_tcartwright | RwShaderProgramBuffer | unsupported | RwShaderProgramBuffer | SKIP | Discovered in SHADERS.BNDL. No Volatility autotest handler exists for this resource type. |
| Burnout_tcartwright | ShaderTechnique | unsupported | ShaderTechnique | SKIP | Discovered in SHADERS.BNDL. No Volatility autotest handler exists for this resource type. |
| Burnout_tcartwright | StaticSoundMap | unsupported | StaticSoundMap | SKIP | Discovered in TRK_UNIT0_GR.BNDL. No Volatility autotest handler exists for this resource type. |
| Burnout_tcartwright | PropInstanceData | unsupported | PropInstanceData | SKIP | Discovered in TRK_UNIT0_GR.BNDL. No Volatility autotest handler exists for this resource type. |
| Burnout_tcartwright | PropGraphicsList | unsupported | PropGraphicsList | SKIP | Discovered in TRK_UNIT0_GR.BNDL. No Volatility autotest handler exists for this resource type. |
| Burnout_tcartwright | WorldPainter2D | unsupported | WorldPainter2D | SKIP | Discovered in DISTRICTS.DAT. No Volatility autotest handler exists for this resource type. |
| Burnout_tcartwright | ProfileUpgrade | unsupported | ProfileUpgrade | SKIP | Discovered in PROFILEUPG.BIN. No Volatility autotest handler exists for this resource type. |
| Burnout_tcartwright | PolygonSoupList | unsupported | PolygonSoupList | SKIP | Discovered in WORLDCOL.BIN. No Volatility autotest handler exists for this resource type. |
| Burnout_tcartwright | IdList | unsupported | IdList | SKIP | Discovered in WORLDCOL.BIN. No Volatility autotest handler exists for this resource type. |
| Burnout_tcartwright | GuiPopup | binaryparity | GuiPopup:2718168B | FAIL | Binary mismatch at offset 0x195. Original=0x48, Exported=0x00. |
| Burnout_tcartwright | GuiPopup | roundtrip | GuiPopup:2718168B | PASS |  |
| Burnout_tcartwright | Renderable | import | Renderable:gamedb://burnout5/Burnout/Content_World/Scenes/Backdrops/PvsZone/BdZone272.BackDropScene?ID=409963_LOD0 | PASS |  |
| Burnout_tcartwright | Scene | binaryparity | Scene:gamedb://burnout5/Burnout/Content_World/Scenes/Backdrops/PvsZone/BdZone99.BackDropScene?ID=508161 | PASS | Binary files are identical. |
| Burnout_tcartwright | Scene | roundtrip | Scene:gamedb://burnout5/Burnout/Content_World/Scenes/Backdrops/PvsZone/BdZone99.BackDropScene?ID=508161 | PASS |  |
| Burnout_tcartwright | Renderable | import | Renderable:gamedb://burnout5/Burnout/Content_World/Scenes/Backdrops/PvsZone/BdZone136.BackDropScene?ID=558369_LOD0 | PASS |  |
| Burnout_tcartwright | Texture | binaryparity | Texture:gamedb://burnout5/Burnout/Content_World/Images/Backdrops/Striped_Glass_Building.TextureConfig2d?ID=388298 | FAIL | Binary mismatch at offset 0x34. Original=0x5D, Exported=0x56. |
| Burnout_tcartwright | Texture | roundtrip | Texture:gamedb://burnout5/Burnout/Content_World/Images/Backdrops/Striped_Glass_Building.TextureConfig2d?ID=388298 | PASS |  |
| Burnout_tcartwright | Texture | texturetodds | gamedb://burnout5/Burnout/Content_World/Images/Backdrops/Striped_Glass_Building.TextureConfig2d?ID=388298:dds | PASS |  |
| Burnout_tcartwright | Texture | porttexture | gamedb://burnout5/Burnout/Content_World/Images/Backdrops/Striped_Glass_Building.TextureConfig2d?ID=388298:X360->TUB | PASS |  |
| Burnout_tcartwright | Texture | binaryparity | Texture:gamedb://burnout5/Burnout/Content_World/Images_Final/cladding08_window03.TextureConfig2d?ID=331076 | FAIL | Binary mismatch at offset 0x32. Original=0x2A, Exported=0x4A. |
| Burnout_tcartwright | Texture | roundtrip | Texture:gamedb://burnout5/Burnout/Content_World/Images_Final/cladding08_window03.TextureConfig2d?ID=331076 | FAIL | YAML mismatch after reimport. Pass1=C:\\Users\\adri1\\Documents\\Github\\volatility\\.tmp\\game-autotest\\20260608_223817\\Burnout_tcartwright_X360\\import_pass1\\Resources\\03D5700E.Texture, Pass2=C:\\Users\\adri1\\Documents\\Github\\volatility\\.tmp\\game-autotest\\20260608_223817\\Burnout_tcartwright_X360\\import_pass2\\Resources\\03D5700E.Texture |
| Burnout_tcartwright | Texture | texturetodds | gamedb://burnout5/Burnout/Content_World/Images_Final/cladding08_window03.TextureConfig2d?ID=331076:dds | PASS |  |
| Burnout_tcartwright | Texture | porttexture | gamedb://burnout5/Burnout/Content_World/Images_Final/cladding08_window03.TextureConfig2d?ID=331076:X360->TUB | PASS |  |
| Burnout_tcartwright | Scene | binaryparity | Scene:gamedb://burnout5/Burnout/Content_World/Scenes/Backdrops/BD_Mountains_03.RoadScene?ID=197487 | PASS | Binary files are identical. |
| Burnout_tcartwright | Scene | roundtrip | Scene:gamedb://burnout5/Burnout/Content_World/Scenes/Backdrops/BD_Mountains_03.RoadScene?ID=197487 | PASS |  |
| Burnout_tcartwright | InstanceList | binaryparity | InstanceList:TRK_UNIT0_list | PASS | Binary files are identical. |
| Burnout_tcartwright | InstanceList | roundtrip | InstanceList:TRK_UNIT0_list | PASS |  |
| Burnout_tcartwright | InstanceList | binaryparity | InstanceList:TRK_UNIT100_list | PASS | Binary files are identical. |
| Burnout_tcartwright | InstanceList | roundtrip | InstanceList:TRK_UNIT100_list | PASS |  |
| BurnoutPR | AISections | unsupported | AISections | SKIP | Discovered in AI.DAT. No Volatility autotest handler exists for this resource type. |
| BurnoutPR | ProgressionData | unsupported | ProgressionData | SKIP | Discovered in PROGRESSION.DAT. No Volatility autotest handler exists for this resource type. |
| BurnoutPR | StreetData | unsupported | StreetData | SKIP | Discovered in STREETDATA.DAT. No Volatility autotest handler exists for this resource type. |
| BurnoutPR | TriggerData | unsupported | TriggerData | SKIP | Discovered in TRIGGERS.DAT. No Volatility autotest handler exists for this resource type. |
| BurnoutPR | HudMessage | unsupported | HudMessage | SKIP | Discovered in HUDMESSAGES.HM. No Volatility autotest handler exists for this resource type. |
| BurnoutPR | HudMessageSequence | unsupported | HudMessageSequence | SKIP | Discovered in HUDMESSAGESEQUENCES.HMSC. No Volatility autotest handler exists for this resource type. |
| BurnoutPR | HudMessageSequenceDictionary | unsupported | HudMessageSequenceDictionary | SKIP | Discovered in HUDMESSAGESEQUENCES.HMSC. No Volatility autotest handler exists for this resource type. |
| BurnoutPR | TrafficData | unsupported | TrafficData | SKIP | Discovered in B5TRAFFIC.BNDL. No Volatility autotest handler exists for this resource type. |
| BurnoutPR | TextureState | unsupported | TextureState | SKIP | Discovered in GLOBALBACKDROPS.BNDL. No Volatility autotest handler exists for this resource type. |
| BurnoutPR | Material | unsupported | Material | SKIP | Discovered in GLOBALBACKDROPS.BNDL. No Volatility autotest handler exists for this resource type. |
| BurnoutPR | MaterialState | unsupported | MaterialState | SKIP | Discovered in GLOBALBACKDROPS.BNDL. No Volatility autotest handler exists for this resource type. |
| BurnoutPR | VertexDescriptor | unsupported | VertexDescriptor | SKIP | Discovered in GLOBALBACKDROPS.BNDL. No Volatility autotest handler exists for this resource type. |
| BurnoutPR | MassiveLookupTable | unsupported | MassiveLookupTable | SKIP | Discovered in MASSIVETABLE.BIN. No Volatility autotest handler exists for this resource type. |
| BurnoutPR | AttribSysVault | unsupported | AttribSysVault | SKIP | Discovered in SURFACELIST.BIN. No Volatility autotest handler exists for this resource type. |
| BurnoutPR | ICETakeDictionary | unsupported | ICETakeDictionary | SKIP | Discovered in CAMERAS.BUNDLE. No Volatility autotest handler exists for this resource type. |
| BurnoutPR | FlaptFile | unsupported | FlaptFile | SKIP | Discovered in FLAPTHUD.BUNDLE. No Volatility autotest handler exists for this resource type. |
| BurnoutPR | ParticleDescription | unsupported | ParticleDescription | SKIP | Discovered in PARTICLES.BUNDLE. No Volatility autotest handler exists for this resource type. |
| BurnoutPR | TextureNameMap | unsupported | TextureNameMap | SKIP | Discovered in PARTICLES.BUNDLE. No Volatility autotest handler exists for this resource type. |
| BurnoutPR | VFXMeshCollection | unsupported | VFXMeshCollection | SKIP | Discovered in PARTICLES.BUNDLE. No Volatility autotest handler exists for this resource type. |
| BurnoutPR | VFXPropCollection | unsupported | VFXPropCollection | SKIP | Discovered in PARTICLES.BUNDLE. No Volatility autotest handler exists for this resource type. |
| BurnoutPR | ParticleDescriptionCollection | unsupported | ParticleDescriptionCollection | SKIP | Discovered in PARTICLES.BUNDLE. No Volatility autotest handler exists for this resource type. |
| BurnoutPR | Registry | unsupported | Registry | SKIP | Discovered in PLAYBACKREGISTRY.BUNDLE. No Volatility autotest handler exists for this resource type. |
| BurnoutPR | ZoneList | unsupported | ZoneList | SKIP | Discovered in PVS.BNDL. No Volatility autotest handler exists for this resource type. |
| BurnoutPR | ChallengeList | unsupported | ChallengeList | SKIP | Discovered in ONLINECHALLENGES.BNDL. No Volatility autotest handler exists for this resource type. |
| BurnoutPR | RwShaderProgramBuffer | unsupported | RwShaderProgramBuffer | SKIP | Discovered in SHADERS.BNDL. No Volatility autotest handler exists for this resource type. |
| BurnoutPR | ShaderTechnique | unsupported | ShaderTechnique | SKIP | Discovered in SHADERS.BNDL. No Volatility autotest handler exists for this resource type. |
| BurnoutPR | StaticSoundMap | unsupported | StaticSoundMap | SKIP | Discovered in TRK_UNIT0_GR.BNDL. No Volatility autotest handler exists for this resource type. |
| BurnoutPR | PropInstanceData | unsupported | PropInstanceData | SKIP | Discovered in TRK_UNIT0_GR.BNDL. No Volatility autotest handler exists for this resource type. |
| BurnoutPR | PropGraphicsList | unsupported | PropGraphicsList | SKIP | Discovered in TRK_UNIT0_GR.BNDL. No Volatility autotest handler exists for this resource type. |
| BurnoutPR | WorldPainter2D | unsupported | WorldPainter2D | SKIP | Discovered in DISTRICTS.DAT. No Volatility autotest handler exists for this resource type. |
| BurnoutPR | PolygonSoupList | unsupported | PolygonSoupList | SKIP | Discovered in WORLDCOL.BIN. No Volatility autotest handler exists for this resource type. |
| BurnoutPR | IdList | unsupported | IdList | SKIP | Discovered in WORLDCOL.BIN. No Volatility autotest handler exists for this resource type. |
| BurnoutPR | - | bundleextract | TRK_UNIT0_GR.BNDL | SKIP | Process 'C:\\Users\\adri1\\Documents\\Github\\volatility\\tools\\libbndl-extractor\\build\\volatility_libbndl_extract.exe --bundle "C:\\Program Files (x86)\\Steam\\steamapps\\common\\BurnoutPR\\TRK_UNIT0_GR.BNDL" --output "C:\\Users\\adri1\\Documents\\Github\\volatility\\.tmp\\game-autotest\\20260608_223817\\BurnoutPR_BPR\\bundles\\TRK_UNIT0_GR.BNDL" --manifest "C:\\Users\\adri1\\Documents\\Github\\volatility\\.tmp\\game-autotest\\20260608_223817\\BurnoutPR_BPR\\bundles\\TRK_UNIT0_GR.BNDL\\manifest.tsv"' failed with exit code 3. <br>Assertion failed: m_flags & Compressed, file C:\\Users\\adri1\\Documents\\Github\\volatility\\tools\\libbndl-extractor\\third_party\\libbndl\\src\\bundle.cpp, line 892 <br> |
| BurnoutPR | InstanceList | candidate | InstanceList | SKIP | No fully extractable bundle candidate was available for this supported resource type. |
| BurnoutPR | GuiPopup | binaryparity | GuiPopup:POPUPS.pup | FAIL | Binary mismatch at offset 0x2B1. Original=0xF9, Exported=0x00. |
| BurnoutPR | GuiPopup | roundtrip | GuiPopup:POPUPS.pup | PASS |  |
| BurnoutPR | Renderable | import | Renderable:gamedb://burnout5/Burnout/Content_World/Scenes/Backdrops/PvsZone/BdZone272.BackDropScene?ID=409963_LOD0 | PASS |  |
| BurnoutPR | Scene | binaryparity | Scene:gamedb://burnout5/Burnout/Content_World/Scenes/Backdrops/PvsZone/BdZone99.BackDropScene?ID=508161 | PASS | Binary files are identical. |
| BurnoutPR | Scene | roundtrip | Scene:gamedb://burnout5/Burnout/Content_World/Scenes/Backdrops/PvsZone/BdZone99.BackDropScene?ID=508161 | PASS |  |
| BurnoutPR | Renderable | import | Renderable:gamedb://burnout5/Burnout/Content_World/Scenes/Backdrops/PvsZone/BdZone136.BackDropScene?ID=558369_LOD0 | PASS |  |
| BurnoutPR | Texture | binaryparity | Texture:gamedb://burnout5/Burnout/Content_World/Images/Backdrops/Striped_Glass_Building.TextureConfig2d?ID=388298 | PASS | Binary files are identical. |
| BurnoutPR | Texture | roundtrip | Texture:gamedb://burnout5/Burnout/Content_World/Images/Backdrops/Striped_Glass_Building.TextureConfig2d?ID=388298 | PASS |  |
| BurnoutPR | Texture | texturetodds | gamedb://burnout5/Burnout/Content_World/Images/Backdrops/Striped_Glass_Building.TextureConfig2d?ID=388298:dds | PASS |  |
| BurnoutPR | Texture | porttexture | gamedb://burnout5/Burnout/Content_World/Images/Backdrops/Striped_Glass_Building.TextureConfig2d?ID=388298:BPR->TUB | PASS |  |
| BurnoutPR | Texture | binaryparity | Texture:gamedb://burnout5/Burnout/Content_World/Images_Final/cladding08_window03.TextureConfig2d?ID=331076 | PASS | Binary files are identical. |
| BurnoutPR | Texture | roundtrip | Texture:gamedb://burnout5/Burnout/Content_World/Images_Final/cladding08_window03.TextureConfig2d?ID=331076 | PASS |  |
| BurnoutPR | Texture | texturetodds | gamedb://burnout5/Burnout/Content_World/Images_Final/cladding08_window03.TextureConfig2d?ID=331076:dds | PASS |  |
| BurnoutPR | Texture | porttexture | gamedb://burnout5/Burnout/Content_World/Images_Final/cladding08_window03.TextureConfig2d?ID=331076:BPR->TUB | PASS |  |
| BurnoutPR | Scene | binaryparity | Scene:gamedb://burnout5/Burnout/Content_World/Scenes/Backdrops/BD_Mountains_03.RoadScene?ID=197487 | PASS | Binary files are identical. |
| BurnoutPR | Scene | roundtrip | Scene:gamedb://burnout5/Burnout/Content_World/Scenes/Backdrops/BD_Mountains_03.RoadScene?ID=197487 | PASS |  |
