# clover pit ultimate trainer

bepinex trainer for clover pit. has a bunch of cheats, a charm spawner, slot machine rigging, resource editing etc. open with f2 in-game.

probably has bugs. i don't care and i'm not fixing them.

---

## features

- infinite charm slots (capped at 50, game limitation)
- free shop + infinite restocks
- infinite red button charges
- deal never turns off
- 5x turbo speed
- unlock all charms and drawers
- cancel death countdown
- edit coins, deposited coins, clover tickets
- edit spins (current + permanent base)
- edit luck values and multipliers (symbol, pattern, powerup, activation, store)
- edit debt level
- slot machine rigging (7s jackpot, specific symbol jackpot, devil/angel events, pattern forcing)
- charm spawner (browse and equip any charm by name)

---

## requirements

- [bepinex 5.x](https://github.com/BepInEx/BepInEx/releases) installed in the game folder
- .net framework 4.x (you probably already have it)

---

## install

drop `BespokeTrainer.dll` into `BepInEx/plugins/` and launch the game.

---

## building from source

requires the game to be installed at `F:\SteamLibrary\steamapps\common\Death Pong Demo` or you'll need to edit the path at the top of `build.ps1`.

1. put `Trainer.cs` and `build.ps1` in the same folder
2. right-click `build.ps1` -> run with powershell
3. it'll compile and drop the dll straight into your plugins folder

the script uses the game's own managed dlls as references so make sure the game is actually installed first. it'll tell you if anything's missing.

---

## controls

| key | action |
|-----|--------|
| f2 | toggle menu |
| up / down | navigate |
| left / right | adjust values |
| shift + left/right | adjust x10 |
| ctrl + left/right | adjust x100 |
| enter | execute / toggle |
| backspace / escape | go back |

---

## notes

- turbo speed (5x) affects the entire game including ui, use at your own risk
- charm spawner tries to equip the charm, if slots are full it puts it in a drawer instead
- slot rig patches directly into the symbol spawn call so it should work without visual glitches but no promises
- this was made for the demo version, no idea if it works on any other build
