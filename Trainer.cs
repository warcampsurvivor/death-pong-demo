using BepInEx;
using UnityEngine;
using HarmonyLib; 
using System;
using System.Collections.Generic;
using System.Numerics;
using Panik;

namespace BespokeTrainer
{
    [BepInPlugin("com.bespoketrainer.cloverpit", "Clover Pit Ultimate Trainer", "3.1.0")]
    public class TrainerPlugin : BaseUnityPlugin
    {
        public static bool ShowMenu = false;
        
        public static bool infSlots = false;
        public static bool infRed = false;
        private static bool dealOn = false;
        private static bool turbo = false;
        private static bool freeShop = false;

        public enum RigMode { Off, Jackpot7s, JackpotSpecific, PatternAnySymbol, PatternSpecific }
        public static RigMode rigMode = RigMode.Off;
        public static SymbolScript.Kind rigSym = SymbolScript.Kind.seven;
        public static PatternScript.Kind rigPat = PatternScript.Kind.jackpot;

        private UnityEngine.Vector2 scrollPos = UnityEngine.Vector2.zero;
        private int selIdx = 0;

        private float hTimer = 0f;
        private const float H_DELAY = 0.4f;
        private const float H_RATE = 0.05f;

        private enum MenuState { Main, CharmSpawner }
        private MenuState menuState = MenuState.Main;
        
        private List<MenuItem> menu;
        private Stack<List<MenuItem>> menuStack = new Stack<List<MenuItem>>();
        private Stack<int> idxStack = new Stack<int>();

        private int charmIdx = 0;
        private UnityEngine.Vector2 charmScroll = UnityEngine.Vector2.zero;
        private List<PowerupScript.Identifier> charms;

        private enum ItemType { Action, Adjustment, Toggle, Submenu }
        
        private class MenuItem
        {
            public string Label;
            public ItemType Type;
            public Action OnExec;
            public Action<int> OnAdj;
            public Func<string> GetVal;
            public Func<bool> GetState;
        }

        private void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(TrainerPlugin));

            charms = new List<PowerupScript.Identifier>();
            for (int i = 0; i < (int)PowerupScript.Identifier.count; i++)
            {
                charms.Add((PowerupScript.Identifier)i);
            }

            menu = BuildMainMenu();
            
            Logger.LogInfo("Bespoke Trainer v3.1 Loaded! Press F2 in-game.");
        }

        private void OpenMenu(List<MenuItem> m)
        {
            menuStack.Push(menu);
            idxStack.Push(selIdx);
            menu = m;
            selIdx = 0;
            MenuSound();
        }

        private void GoBack()
        {
            if (menuStack.Count > 0)
            {
                menu = menuStack.Pop();
                selIdx = idxStack.Pop();
                MenuSound();
            }
            else
            {
                ShowMenu = false;
            }
        }

        private List<MenuItem> BuildMainMenu()
        {
            return new List<MenuItem>
            {
                new MenuItem { Label = "--- SUB-MENUS ---", Type = ItemType.Action },
                new MenuItem {
                    Label = "Open Charm Spawner...", Type = ItemType.Submenu,
                    OnExec = delegate() { 
                        menuState = MenuState.CharmSpawner; 
                        charmIdx = 0; 
                    }
                },
                new MenuItem {
                    Label = "Rig Slot Machine...", Type = ItemType.Submenu,
                    OnExec = delegate() { OpenMenu(BuildRigMenu()); }
                },

                new MenuItem { Label = "", Type = ItemType.Action },
                new MenuItem { Label = "--- CHEAT TOGGLES ---", Type = ItemType.Action },
                
                new MenuItem {
                    Label = "Infinite Charm Capacity (50 Slots Max)", Type = ItemType.Toggle,
                    GetState = delegate() { return infSlots; },
                    OnExec = delegate() { infSlots = !infSlots; }
                },
                new MenuItem {
                    Label = "Shop is Free & Infinite Restocks", Type = ItemType.Toggle,
                    GetState = delegate() { return freeShop; },
                    OnExec = delegate() { 
                        freeShop = !freeShop; 
                        if (freeShop) {
                            GameplayData.StoreTemporaryDiscountSet(999999999, true);
                            GameplayData.StoreRestockExtraCostSet(0);
                            GameplayData.StoreFreeRestocksSet(999);
                        } else {
                            GameplayData.StoreTemporaryDiscountSet(0, true);
                        }
                    }
                },
                new MenuItem {
                    Label = "Infinite Red Button Charges", Type = ItemType.Toggle,
                    GetState = delegate() { return infRed; },
                    OnExec = delegate() { infRed = !infRed; }
                },
                new MenuItem {
                    Label = "The Deal Is Never Off", Type = ItemType.Toggle,
                    GetState = delegate() { return dealOn; },
                    OnExec = delegate() { dealOn = !dealOn; }
                },
                new MenuItem {
                    Label = "Turbo Game Speed (5x)", Type = ItemType.Toggle,
                    GetState = delegate() { return turbo; },
                    OnExec = delegate() { 
                        turbo = !turbo; 
                        Time.timeScale = turbo ? 5f : 1f;
                    }
                },

                new MenuItem { Label = "", Type = ItemType.Action },
                new MenuItem { Label = "--- INSTANT ACTIONS ---", Type = ItemType.Action },

                new MenuItem {
                    Label = "Unlock All Charms & Drawers", Type = ItemType.Action,
                    OnExec = delegate() {
                        for (int i = 0; i < (int)PowerupScript.Identifier.count; i++) PowerupScript.Unlock((PowerupScript.Identifier)i);
                        for (int j = 0; j < 4; j++) DrawersScript.Unlock(j);
                    }
                },
                new MenuItem {
                    Label = "Cancel Death Countdown", Type = ItemType.Action,
                    OnExec = delegate() { GameplayMaster.DeathCountdownResetRequest(false); }
                },
                
                new MenuItem { Label = "", Type = ItemType.Action },
                new MenuItem { Label = "--- RESOURCES ---", Type = ItemType.Action },
                
                new MenuItem {
                    Label = "Coins", Type = ItemType.Adjustment,
                    GetVal = delegate() { return GameplayData.Instance != null ? GameplayData.CoinsGet().ToString() : "-"; },
                    OnAdj = delegate(int step) { 
                        if (GameplayData.Instance != null) { 
                            GameplayData.CoinsAdd(new BigInteger(step * 1000), false); 
                            if (GeneralUiScript.instance != null) GeneralUiScript.CoinsTextForceUpdate(); 
                        } 
                    }
                },
                new MenuItem {
                    Label = "Deposited Coins", Type = ItemType.Adjustment,
                    GetVal = delegate() { return GameplayData.Instance != null ? GameplayData.DepositGet().ToString() : "-"; },
                    OnAdj = delegate(int step) { 
                        if (GameplayData.Instance != null) { 
                            GameplayData.DepositAdd(new BigInteger(step * 1000)); 
                            if (GeneralUiScript.instance != null) GeneralUiScript.CoinsTextForceUpdate(); 
                        } 
                    }
                },
                new MenuItem {
                    Label = "Clover Tickets", Type = ItemType.Adjustment,
                    GetVal = delegate() { return GameplayData.Instance != null ? GameplayData.CloverTicketsGet().ToString() : "-"; },
                    OnAdj = delegate(int step) { 
                        if (GameplayData.Instance != null) { 
                            GameplayData.CloverTicketsAdd((long)(step * 10), false); 
                            if (GeneralUiScript.instance != null) GeneralUiScript.TicketsTextForceUpdate(); 
                        } 
                    }
                },

                new MenuItem { Label = "", Type = ItemType.Action },
                new MenuItem { Label = "--- SPINS ---", Type = ItemType.Action },

                new MenuItem {
                    Label = "Current Spins Left", Type = ItemType.Adjustment,
                    GetVal = delegate() { return GameplayData.Instance != null ? GameplayData.SpinsLeftGet().ToString() : "-"; },
                    OnAdj = delegate(int step) { 
                        if (GameplayData.Instance != null) {
                            GameplayData.SpinsLeftAdd(step); 
                            if (GeneralUiScript.instance != null) GeneralUiScript.CoinsTextInstantUpdate(); 
                        }
                    }
                },
                new MenuItem {
                    Label = "Permanent Base Spins", Type = ItemType.Adjustment,
                    GetVal = delegate() { return GameplayData.Instance != null ? GameplayData.ExtraSpinsGet(false).ToString() : "-"; },
                    OnAdj = delegate(int step) { if (GameplayData.Instance != null) GameplayData.ExtraSpinsAdd(step); }
                },

                new MenuItem { Label = "", Type = ItemType.Action },
                new MenuItem { Label = "--- LUCK & MULTIPLIERS ---", Type = ItemType.Action },

                new MenuItem {
                    Label = "Powerup Luck", Type = ItemType.Adjustment,
                    GetVal = delegate() { return GameplayData.Instance != null ? GameplayData.PowerupLuckGet().ToString("0.0") : "-"; },
                    OnAdj = delegate(int step) { if (GameplayData.Instance != null) GameplayData.PowerupLuckAdd(step * 0.1f); }
                },
                new MenuItem {
                    Label = "Activation Luck", Type = ItemType.Adjustment,
                    GetVal = delegate() { return GameplayData.Instance != null ? GameplayData.ActivationLuckGet().ToString("0.0") : "-"; },
                    OnAdj = delegate(int step) { if (GameplayData.Instance != null) GameplayData.ActivationLuckAdd(step * 0.1f); }
                },
                new MenuItem {
                    Label = "Store Luck", Type = ItemType.Adjustment,
                    GetVal = delegate() { return GameplayData.Instance != null ? GameplayData.StoreLuckGet().ToString("0.0") : "-"; },
                    OnAdj = delegate(int step) { if (GameplayData.Instance != null) GameplayData.StoreLuckAdd(step * 0.1f); }
                },
                new MenuItem {
                    Label = "Global Symbol Multiplier", Type = ItemType.Adjustment,
                    GetVal = delegate() { return GameplayData.Instance != null ? GameplayData.AllSymbolsMultiplierGet(false).ToString() : "-"; },
                    OnAdj = delegate(int step) { if (GameplayData.Instance != null) GameplayData.AllSymbolsMultiplierAdd(new BigInteger(step)); }
                },
                new MenuItem {
                    Label = "Global Pattern Multiplier", Type = ItemType.Adjustment,
                    GetVal = delegate() { return GameplayData.Instance != null ? GameplayData.AllPatternsMultiplierGet(false).ToString() : "-"; },
                    OnAdj = delegate(int step) { if (GameplayData.Instance != null) GameplayData.AllPatternsMultiplierAdd(new BigInteger(step)); }
                },

                new MenuItem { Label = "", Type = ItemType.Action },
                new MenuItem { Label = "--- PROGRESSION ---", Type = ItemType.Action },

                new MenuItem {
                    Label = "Debt Level Index", Type = ItemType.Adjustment,
                    GetVal = delegate() { return GameplayData.Instance != null ? GameplayData.DebtIndexGet().ToString() : "-"; },
                    OnAdj = delegate(int step) { 
                        if (GameplayData.Instance != null) {
                            BigInteger val = GameplayData.DebtIndexGet() + new BigInteger(step);
                            if (val < 0) val = 0;
                            GameplayData.DebtIndexSet(val); 
                        }
                    }
                }
            };
        }

        private List<MenuItem> BuildRigMenu()
        {
            return new List<MenuItem>
            {
                new MenuItem { Label = "--- RIG SLOT MACHINE ---", Type = ItemType.Action },
                new MenuItem { Label = "Current State: " + rigMode.ToString(), Type = ItemType.Action },
                new MenuItem { Label = "", Type = ItemType.Action },
                new MenuItem {
                    Label = "Disable Rigging (Return to Normal)", Type = ItemType.Action,
                    OnExec = delegate() { rigMode = RigMode.Off; GoBack(); }
                },
                new MenuItem {
                    Label = "Auto-Jackpot (Always 7s)", Type = ItemType.Action,
                    OnExec = delegate() { rigMode = RigMode.Jackpot7s; GoBack(); }
                },
                new MenuItem {
                    Label = "Force Devil Event (Always 666)", Type = ItemType.Action,
                    OnExec = delegate() { rigSym = SymbolScript.Kind.six; rigMode = RigMode.JackpotSpecific; GoBack(); }
                },
                new MenuItem {
                    Label = "Force Angel Event (Always 999)", Type = ItemType.Action,
                    OnExec = delegate() { rigSym = SymbolScript.Kind.nine; rigMode = RigMode.JackpotSpecific; GoBack(); }
                },
                new MenuItem {
                    Label = "Auto-Jackpot (Choose Specific Symbol)...", Type = ItemType.Submenu,
                    OnExec = delegate() { 
                        OpenMenu(SymbolMenu(delegate(SymbolScript.Kind s) {
                            rigSym = s;
                            rigMode = RigMode.JackpotSpecific;
                            GoBack();
                            GoBack();
                        }));
                    }
                },
                new MenuItem {
                    Label = "Force Pattern (Any Random Symbol)...", Type = ItemType.Submenu,
                    OnExec = delegate() { 
                        OpenMenu(PatternMenu(delegate(PatternScript.Kind p) {
                            rigPat = p;
                            rigMode = RigMode.PatternAnySymbol;
                            GoBack(); 
                            GoBack(); 
                        }));
                    }
                },
                new MenuItem {
                    Label = "Force Pattern (Specific Symbol)...", Type = ItemType.Submenu,
                    OnExec = delegate() { 
                        OpenMenu(SymbolMenu(delegate(SymbolScript.Kind s) {
                            rigSym = s;
                            OpenMenu(PatternMenu(delegate(PatternScript.Kind p) {
                                rigPat = p;
                                rigMode = RigMode.PatternSpecific;
                                GoBack();
                                GoBack();
                                GoBack();
                            }));
                        }));
                    }
                }
            };
        }

        private List<MenuItem> SymbolMenu(Action<SymbolScript.Kind> cb)
        {
            List<MenuItem> list = new List<MenuItem>
            {
                new MenuItem { Label = "--- CHOOSE SYMBOL ---", Type = ItemType.Action }
            };

            foreach (SymbolScript.Kind kind in Enum.GetValues(typeof(SymbolScript.Kind)))
            {
                if (kind == SymbolScript.Kind.undefined || kind == SymbolScript.Kind.count) continue;
                
                list.Add(new MenuItem {
                    Label = kind.ToString(), Type = ItemType.Action,
                    OnExec = delegate() { cb(kind); }
                });
            }
            return list;
        }

        private List<MenuItem> PatternMenu(Action<PatternScript.Kind> cb)
        {
            List<MenuItem> list = new List<MenuItem>
            {
                new MenuItem { Label = "--- CHOOSE PATTERN ---", Type = ItemType.Action }
            };

            foreach (PatternScript.Kind kind in Enum.GetValues(typeof(PatternScript.Kind)))
            {
                if (kind == PatternScript.Kind.undefined || kind == PatternScript.Kind.count) continue;
                
                list.Add(new MenuItem {
                    Label = kind.ToString(), Type = ItemType.Action,
                    OnExec = delegate() { cb(kind); }
                });
            }
            return list;
        }

        private void Update()
        {
            if (infRed && RedButtonScript.instance != null)
            {
                RedButtonScript.RestoreCharges(99);
            }
            if (dealOn && GameplayMaster.instance != null && GameplayData.Instance != null)
            {
                GameplayMaster.MemoryPack_TheDealIsOff_FlagSet(false, false);
                GameplayData.RunModifier_DealIsAvailable_Set(true);
            }

            if (Input.GetKeyDown(KeyCode.F2))
            {
                ShowMenu = !ShowMenu;
            }

            if (!ShowMenu) return;

            if (menuState == MenuState.Main)
                UpdateMain();
            else if (menuState == MenuState.CharmSpawner)
                UpdateCharm();
        }

        private void UpdateMain()
        {
            if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Escape))
            {
                GoBack();
                return;
            }

            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                do {
                    selIdx--;
                    if (selIdx < 0) selIdx = menu.Count - 1;
                } while (menu[selIdx].Label == "" || menu[selIdx].Label.StartsWith("---"));
            }
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                do {
                    selIdx++;
                    if (selIdx >= menu.Count) selIdx = 0;
                } while (menu[selIdx].Label == "" || menu[selIdx].Label.StartsWith("---"));
            }

            int adjDir = 0;
            if (Input.GetKeyDown(KeyCode.LeftArrow)) { adjDir = -1; hTimer = H_DELAY; }
            else if (Input.GetKeyDown(KeyCode.RightArrow)) { adjDir = 1; hTimer = H_DELAY; }
            else if (Input.GetKey(KeyCode.LeftArrow))
            {
                hTimer -= Time.deltaTime;
                if (hTimer <= 0) { adjDir = -1; hTimer = H_DELAY * H_RATE; }
            }
            else if (Input.GetKey(KeyCode.RightArrow))
            {
                hTimer -= Time.deltaTime;
                if (hTimer <= 0) { adjDir = 1; hTimer = H_DELAY * H_RATE; }
            }

            if (adjDir != 0)
            {
                MenuItem item = menu[selIdx];
                if (item.Type == ItemType.Adjustment && item.OnAdj != null)
                {
                    int mult = 1;
                    if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) mult = 100;
                    else if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) mult = 10;

                    item.OnAdj(adjDir * mult);
                    MenuSound();
                }
                else if (item.Type != ItemType.Adjustment && adjDir == -1)
                {
                    GoBack();
                }
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                MenuItem item = menu[selIdx];
                if ((item.Type == ItemType.Action || item.Type == ItemType.Toggle || item.Type == ItemType.Submenu) && item.OnExec != null)
                {
                    item.OnExec();
                    MenuSound();
                }
            }
        }

        private void UpdateCharm()
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                charmIdx--;
                if (charmIdx < 0) charmIdx = charms.Count - 1;
            }
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                charmIdx++;
                if (charmIdx >= charms.Count) charmIdx = 0;
            }

            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Escape))
            {
                menuState = MenuState.Main;
                MenuSound();
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                PowerupScript.Identifier tCharm = charms[charmIdx];
                
                PowerupScript.EquipFlag_IgnoreSpaceCondition();
                bool ok = PowerupScript.Equip(tCharm, false, false);
                
                if (!ok)
                {
                    PowerupScript.PutInDrawer(tCharm, false, 0);
                }

                MenuSound();
            }
        }

        private void MenuSound()
        {
            if (PlatformMaster.IsInitialized())
            {
                Sound.Play_Unpausable("SoundMenuSelect", 1f, 1f);
            }
        }

        private void OnGUI()
        {
            if (!ShowMenu) return;

            Color origCol = GUI.contentColor;
            GUI.skin.box.fontSize = 14;
            GUI.skin.label.fontSize = 14;
            GUI.skin.label.richText = true;

            float w = 480;
            float h = Screen.height * 0.8f;
            float sx = 20;
            float sy = 20;

            if (menuState == MenuState.Main)
            {
                string hdr = menuStack.Count == 0 ? "<b>Clover Pit Ultimate Trainer</b>" : "<b>Trainer Sub-Menu (Backspace to Return)</b>";
                GUI.Box(new Rect(sx, sy, w, h), hdr + "\n\n");

                float innerH = menu.Count * 25 + 20;
                float tScroll = (selIdx * 25) - ((h - 120) / 2);
                if (tScroll < 0) tScroll = 0;
                if (tScroll > innerH - (h - 120)) tScroll = innerH - (h - 120);
                scrollPos.y = tScroll;

                scrollPos = GUI.BeginScrollView(
                    new Rect(sx + 10, sy + 40, w - 20, h - 120), 
                    scrollPos, 
                    new Rect(0, 0, w - 40, innerH)
                );

                for (int i = 0; i < menu.Count; i++)
                {
                    MenuItem item = menu[i];
                    float yPos = i * 25;

                    if (item.Label == "" || item.Label.StartsWith("---"))
                    {
                        GUI.contentColor = Color.gray;
                        GUI.Label(new Rect(10, yPos, w - 60, 25), "<b>" + item.Label + "</b>");
                        continue;
                    }

                    if (i == selIdx)
                    {
                        GUI.contentColor = Color.yellow;
                        GUI.Label(new Rect(0, yPos, 20, 25), "►");
                    }
                    else
                    {
                        GUI.contentColor = Color.white;
                    }

                    GUI.Label(new Rect(20, yPos, 250, 25), item.Label);

                    if (item.Type == ItemType.Adjustment && item.GetVal != null)
                    {
                        GUI.Label(new Rect(280, yPos, 140, 25), "< " + item.GetVal() + " >");
                    }
                    else if (item.Type == ItemType.Toggle && item.GetState != null)
                    {
                        GUI.contentColor = item.GetState() ? Color.green : Color.red;
                        GUI.Label(new Rect(280, yPos, 140, 25), item.GetState() ? "[ ON ]" : "[ OFF ]");
                    }
                    else if (item.Type == ItemType.Action)
                    {
                        GUI.contentColor = (i == selIdx) ? Color.green : Color.gray;
                        GUI.Label(new Rect(280, yPos, 140, 25), "[Press Enter]");
                    }
                    else if (item.Type == ItemType.Submenu)
                    {
                        GUI.contentColor = (i == selIdx) ? Color.cyan : Color.gray;
                        GUI.Label(new Rect(280, yPos, 140, 25), "[Open Menu...]");
                    }
                }

                GUI.EndScrollView();

                GUI.contentColor = Color.white;
                string inst = "<b>Controls:</b>\n" +
                              "Up/Down: Navigate\n" +
                              "Left/Right: Adjust Values  |  Hold Shift (x10) or Ctrl (x100)\n" +
                              "Enter: Execute Action      |  F2: Toggle Menu";
                GUI.Label(new Rect(sx + 15, sy + h - 80, w - 30, 80), inst);
            }
            else if (menuState == MenuState.CharmSpawner)
            {
                GUI.Box(new Rect(sx, sy, w, h), "<b>Charm Spawner (Press Left/Backspace to Return)</b>\n\n");

                float innerH = charms.Count * 25 + 20;
                
                float tScroll = (charmIdx * 25) - ((h - 120) / 2);
                if (tScroll < 0) tScroll = 0;
                charmScroll.y = tScroll;

                charmScroll = GUI.BeginScrollView(
                    new Rect(sx + 10, sy + 40, w - 20, h - 120), 
                    charmScroll, 
                    new Rect(0, 0, w - 40, innerH)
                );

                for (int i = 0; i < charms.Count; i++)
                {
                    float yPos = i * 25;

                    if (i == charmIdx)
                    {
                        GUI.contentColor = Color.yellow;
                        GUI.Label(new Rect(0, yPos, 20, 25), "►");
                    }
                    else
                    {
                        GUI.contentColor = Color.white;
                    }

                    string cName = charms[i].ToString();
                    try {
                        PowerupScript p = PowerupScript.GetPowerup_Quick(charms[i]);
                        if (p != null) cName = p.NameGet(false, false, false);
                    } catch {}

                    GUI.Label(new Rect(20, yPos, 300, 25), cName);
                    
                    GUI.contentColor = (i == charmIdx) ? Color.green : Color.gray;
                    GUI.Label(new Rect(320, yPos, 140, 25), "[Spawn]");
                }

                GUI.EndScrollView();

                GUI.contentColor = Color.white;
                string inst = "<b>Controls:</b>\n" +
                              "Up/Down: Navigate\n" +
                              "Enter: Spawn & Equip Charm\n" +
                              "Left Arrow / Backspace: Back to Main Menu";
                GUI.Label(new Rect(sx + 15, sy + h - 80, w - 30, 80), inst);
            }
        }

        [HarmonyPatch(typeof(Controls), "ActionButton_PressedGet")]
        public class Patch_ActionButton_PressedGet { public static bool Prefix(ref bool __result) { if (TrainerPlugin.ShowMenu) { __result = false; return false; } return true; } }

        [HarmonyPatch(typeof(Controls), "ActionButton_HoldGet")]
        public class Patch_ActionButton_HoldGet { public static bool Prefix(ref bool __result) { if (TrainerPlugin.ShowMenu) { __result = false; return false; } return true; } }

        [HarmonyPatch(typeof(Controls), "ActionAxisPair_GetValue")]
        public class Patch_ActionAxisPair_GetValue { public static bool Prefix(ref float __result) { if (TrainerPlugin.ShowMenu) { __result = 0f; return false; } return true; } }

        [HarmonyPatch(typeof(Controls), "KeyboardButton_PressedGet")]
        public class Patch_KeyboardButton_PressedGet { public static bool Prefix(ref bool __result) { if (TrainerPlugin.ShowMenu) { __result = false; return false; } return true; } }

        [HarmonyPatch(typeof(Controls), "KeyboardButton_HoldGet")]
        public class Patch_KeyboardButton_HoldGet { public static bool Prefix(ref bool __result) { if (TrainerPlugin.ShowMenu) { __result = false; return false; } return true; } }

        [HarmonyPatch(typeof(SlotMachineScript), "_SymbolsSpawn")]
        public class Patch_SymbolsSpawn
        {
            public static void Prefix(SlotMachineScript __instance)
            {
                if (TrainerPlugin.rigMode == TrainerPlugin.RigMode.Off) return;

                // reflection
                var field = typeof(SlotMachineScript).GetField("lines", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field == null) return;

                SymbolScript.Kind[][] lines = (SymbolScript.Kind[][])field.GetValue(__instance);
                SymbolScript.Kind tSym = TrainerPlugin.rigSym;

                if (TrainerPlugin.rigMode == TrainerPlugin.RigMode.Jackpot7s)
                {
                    for (int y = 0; y < 3; y++) for (int x = 0; x < 5; x++) lines[y][x] = SymbolScript.Kind.seven;
                }
                else if (TrainerPlugin.rigMode == TrainerPlugin.RigMode.JackpotSpecific)
                {
                    for (int y = 0; y < 3; y++) for (int x = 0; x < 5; x++) lines[y][x] = tSym;
                }
                else if (TrainerPlugin.rigMode == TrainerPlugin.RigMode.PatternAnySymbol || TrainerPlugin.rigMode == TrainerPlugin.RigMode.PatternSpecific)
                {
                    if (TrainerPlugin.rigMode == TrainerPlugin.RigMode.PatternAnySymbol)
                    {
                        tSym = GameplayData.Symbol_GetRandom_BasedOnSymbolChance();
                    }

                    // noise fill
                    SymbolScript.Kind junk = tSym == SymbolScript.Kind.lemon ? SymbolScript.Kind.cherry : SymbolScript.Kind.lemon;
                    for (int y = 0; y < 3; y++) for (int x = 0; x < 5; x++) lines[y][x] = junk;

                    bool[][] mask = PatternScript.GetPatternMask(TrainerPlugin.rigPat, false);
                    for (int y = 0; y < 3; y++) {
                        for (int x = 0; x < 5; x++) {
                            if (mask[y][x]) lines[y][x] = tSym;
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(GameplayData), "MaxEquippablePowerupsGet")]
        public class Patch_MaxEquippablePowerupsGet
        {
            public static void Postfix(ref int __result)
            {
                if (TrainerPlugin.infSlots) __result = 50;
            }
        }
    }
}
