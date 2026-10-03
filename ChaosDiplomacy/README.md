# TOR BLT Chaos Diplomacy

Separate Bannerlord 1.3.15 submod for TOR and BLT. This does not depend on or include the enchantment balance module.

Only the war declarations inside TOR's ChaosCampaignBehavior.EnforceWarWithChaos are guarded. A Chaos kingdom whose current ruler is a BLT adopted hero is exempt, in either declaration direction. An independent Chaos clan led by a BLT adopted hero is also exempt. Clans in kingdoms use their kingdom's ruler for this decision: a viewer vassal does not exempt an NPC-led kingdom. Chaos is identified with TOR's chaos_culture ID, and BLT ownership with Hero.IsAdopted(). Ownership is checked when each declaration is attempted.

NPC-led Chaos factions retain TOR's enforcement. Ordinary AI wars, player wars, peace decisions, and all other diplomacy remain available. Existing wars are not ended automatically; negotiate peace normally. This patch removes the forced-war declarations from this specific TOR routine and does not override any separate diplomacy restrictions elsewhere in TOR.

## Build

Run the root build.ps1 once to prepare the pinned Bannerlord 1.3.15 references. Build this project with Visual Studio MSBuild:

```powershell
msbuild .\ChaosDiplomacy\TORBLTChaosDiplomacy.csproj /t:Rebuild /p:Configuration=Release /p:TOR_CORE_DLL="path\to\TOR_Core.dll"
```

Harmony and BLT reference locations are selected using BANNERLORD_GAME_DIR. Output is in build-chaos. Install its contents into Modules/TORBLTChaosDiplomacy and load after Harmony, BLTAdoptAHero, and TOR_Core. Use BLT compatible with Bannerlord 1.3.15.

## Validation

Built against pinned Bannerlord 1.3.15 game references and official TOR release DLL commit 0c1c495f2c9e22bbdbbef7997d70963407eab953. The TOR DLL was inspected: EnforceWarWithChaos(CampaignGameStarter) contains exactly three FactionManager.DeclareWar(IFaction, IFaction) calls. The transpiler validates this count before replacing them and preserves instruction labels and exception blocks. Other methods are not patched.

Tests/DiplomacyTests.cs is an isolated harness that compiles with the actual SubModule.cs and Harmony, substituting game/BLT models. It covers 12 assertions: viewer and NPC rulers, non-Chaos rulers, null factions, independent viewer clans, viewer vassals, member clans, both declaration directions, NPC declarations, all three replacements, branch labels, and changed method rejection. These tests do not establish live Harmony patch application or in-game diplomacy behavior. Full in-game testing remains pending.
