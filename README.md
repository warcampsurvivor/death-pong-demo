# death pong trainer

bepinex/harmony trainer for the death pong demo. adds a god-mode menu with
the usual stuff plus a few game-specific toggles.

## features

- no drunkenness
- god mode
- autoplay
- always my turn
- aimbot (high arc lock on the nearest cup)
- freeze match timer
- sabotage ai (maxes enemy drunkenness)
- iron cups (your cups can't be knocked out)
- unlimited shop / summon bartender
- skips intro, dialogue, loading screens, and demo watermark popups

## controls

- f2 or insert: toggle menu
- w/s or arrow keys: move selection
- enter: toggle selected option

## building it

you need:

- the game installed (death pong demo)
- bepinex already set up in the game folder
- .net framework 4.0 (csc.exe, comes with windows normally)

steps:

1. put `Trainer.cs` and `build.ps1` in the same folder
2. open `build.ps1` and change this line to wherever your game is installed:

   ```powershell
   $gamePath = "C:\PATH\TO\Death Pong Demo"
   ```

   e.g. `C:\Program Files (x86)\Steam\steamapps\common\Death Pong Demo`

3. right click `build.ps1` -> run with powershell (or `powershell -ExecutionPolicy Bypass -File build.ps1`)
4. if it says missing dll, double check your bepinex install and the path above
5. on success it drops `BespokeTrainer.dll` straight into `BepInEx\plugins`

## install (prebuilt)

drop `Trainer.dll` into `BepInEx\plugins` in your game folder. that's it.

## notes

buggy. some toggles can desync from the actual game state if you flip them
mid-animation, aimbot can whiff on weird camera angles,
not actively maintaining this so don't expect fixes, its delivered as is.
if you ask why didn't i precompile it for you, its because i believe in open source and if u have to open ghidra its against what im all about
