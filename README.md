# TOR BLT Balance

A compatibility/balance submod for **The Old Realms (TOR)** and **Bannerlord Twitch / BLTAdoptAHero**.

The goal is to keep TOR enchantments powerful while preventing passive enchantment combinations from turning BLT adopted heroes into effectively invulnerable idle kill-farms in dense battles.

## v0.1.0 balance rules

The first release targets the main source of runaway scaling: **defensive triggered-effect enchantments on BLT adopted heroes**.

Default rules:

- **5 second shared cooldown** between damaging/triggered defensive enchantment procs.
- **4 target cap** for passive defensive enemy AoEs.
- **8 second maximum status-effect duration** from passive defensive enchantment procs.
- Ordinary TOR heroes are untouched.
- Offensive enchantments are untouched.
- Non-trigger defensive bonuses (resistances, stats, etc.) are untouched.
- Revive effects such as **Ward of the Lady** are not put on the damaging-proc shared cooldown.

This means examples such as **Curtain of Aqshy** and **Messengers of Shyish** remain meaningful, but stacking multiple retaliation enchants no longer scales linearly with enemy density and incoming hit-rate.

All values are configurable in:

`Modules/TORBLTBalance/ModuleData/TORBLTBalance.xml`

## Why this approach?

A percentage proc chance is not enough protection in BLT battles. A hero receiving many attacks per second eventually procs almost continuously. AoE radius and long DoT duration then multiply the value of one proc by the number of enemies standing nearby.

TOR BLT Balance therefore controls the three quantities responsible for runaway expected value:

1. proc frequency,
2. targets affected per proc,
3. persistent effect duration.

It does **not** simply halve every enchantment.

## Requirements

- Mount & Blade II: Bannerlord
- Bannerlord.Harmony
- Bannerlord Twitch
- BLTAdoptAHero
- The Old Realms / TOR_Core
- .NET Framework 4.8 build tools / Visual Studio Build Tools

The project compiles against the copies of those mods installed in your Bannerlord directory. This is intentional: TOR and BLT can target different Bannerlord releases over time, so the installed DLLs are the source of truth for a release build.

## Build

PowerShell:

```powershell
.\build.ps1 -Configuration Release
```

If Bannerlord is not in the default Steam path:

```powershell
.\build.ps1 -Configuration Release -BannerlordGameDir "D:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord"
```

or set:

```powershell
$env:BANNERLORD_GAME_DIR = "D:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord"
```

The module is produced under `build\`.

## Package for release

After building:

```powershell
.\package.ps1 -Version 0.1.0
```

This creates:

`artifacts\TORBLTBalance-v0.1.0.zip`

with the correct `TORBLTBalance\...` Bannerlord module directory inside the archive.

## Configuration

Default:

```xml
<TORBLTBalanceConfig>
  <Enabled>true</Enabled>
  <DefensiveProcSharedCooldownSeconds>5</DefensiveProcSharedCooldownSeconds>
  <DefensiveProcMaxTargets>4</DefensiveProcMaxTargets>
  <DefensiveProcMaxStatusDurationSeconds>8</DefensiveProcMaxStatusDurationSeconds>
  <VerboseLogging>false</VerboseLogging>
</TORBLTBalanceConfig>
```

For tuning, enable `VerboseLogging`. The mod will report suppressed defensive procs, target caps and duration caps.

## Current technical scope

The mod patches TOR at runtime with Harmony. It does not overwrite TOR XML or BLT source files.

A defensive enchantment is identified by TOR's own `DefenseTriggerEffectScript` inheritance rather than a hard-coded enchantment-name blacklist. That means newly added TOR defensive triggered-effect enchantments automatically receive the same BLT safeguards.

The scope is deliberately conservative for v0.1.0. Static enchantment bonuses and offensive enchantments should be measured in actual BLT sessions before applying broad nerfs.

## Known compatibility note

At the time this repository was created, the public TOR development source and current Bannerlord-Twitch source may target different Bannerlord patch lines. Build this project against the **actual TOR + BLT DLLs that are installed together in the streamer's working Bannerlord setup**. If those two mods run together there, this project should be built against that same installation.
