using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;

[BepInPlugin("com.bespoke.deathpong.trainer", "Death Pong Bespoke Trainer", "1.0.9")]
public class BespokeTrainer : BaseUnityPlugin
{
    public static bool sober = true;
    public static bool infMoney = true;
    public static bool godMode = true;
    public static bool aimbot = false;
    public static bool autoPlay = false;
    public static bool myTurn = false;
    public static bool freezeTimer = false;
    public static bool sabotage = false;
    public static bool ironCups = false;
    public static bool bStays = false;
    public static bool skipAll = true;

    private static bool showMenu = false;
    private static int selIdx = 0;
    private static string[] items = new string[] {
        "NO DRUNKENNESS", "INFINITE CASH", "GOD MODE (INF LIVES)",
        "AUTOPLAY (AFK FARM)", "ALWAYS MY TURN", "AIMBOT (HIGH ARC)",
        "FREEZE MATCH TIMER", "SABOTAGE AI", "IRON CUPS (PLAYER)",
        "UNLIMITED SHOP", "SUMMON BARTENDER", "NUKE ENEMY CUPS",
        "REGEN MY CUPS", "CLOSE MENU"
    };

    private Rect winRect = new Rect(30, 30, 420, 650);
    private float throwTimer = 0f;
    private float wmTimer = 0f;

    void Awake()
    {
        Harmony harmony = new Harmony("com.bespoke.deathpong.trainer");
        harmony.PatchAll();
        Debug.Log("[Trainer] v1.0.9 - Physics & Bartender Overhaul Loaded.");
    }

    void Update()
    {
        if (skipAll) {
            wmTimer += Time.deltaTime;
            if (wmTimer > 2f) {
                wmTimer = 0f;
                TextMeshProUGUI[] allTexts = UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                if (allTexts != null) {
                    for (int i = 0; i < allTexts.Length; i++) {
                        if (allTexts[i].text != null && allTexts[i].text.IndexOf("Demo", StringComparison.OrdinalIgnoreCase) >= 0) {
                            allTexts[i].gameObject.SetActive(false);
                        }
                    }
                }
            }
        }

        if (sober) {
            HandPlayer player = UnityEngine.Object.FindFirstObjectByType<HandPlayer>();
            if (player != null) {
                player.DrunkennessLevel = 0f;
                // reflection
                Material mat = Traverse.Create(player).Field("_drunkenMaterial").GetValue<Material>();
                if (mat != null) { mat.SetFloat("_Speed", 0f); mat.SetFloat("_Rotate", 0f); }
            }
            StrangeEffect strangeFx = UnityEngine.Object.FindFirstObjectByType<StrangeEffect>();
            if (strangeFx != null && strangeFx.enabled) strangeFx.enabled = false;
        }

        if (bStays && Input.GetKey(KeyCode.LeftShift)) {
            DismissBar();
        }

        if (autoPlay) {
            aimbot = true;
            myTurn = true;
            HandPlayer player = UnityEngine.Object.FindFirstObjectByType<HandPlayer>();
            if (player != null && player.ThrowState == ThrowState.Ready) {
                throwTimer += Time.deltaTime;
                if (throwTimer > 0.5f) { player.Throw(); throwTimer = 0f; }
            }
        }

        if (infMoney && GameManager.Instance != null) GameManager.Instance.PlayerMoney = 999999;
        if (sabotage) {
            HandAI[] enemies = UnityEngine.Object.FindObjectsByType<HandAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (enemies != null) { foreach (var e in enemies) { e.DrunkennessLevel = 1.0f; } }
        }
    }

    void OnGUI()
    {
        Event e = Event.current;
        if (e.type == EventType.KeyDown) {
            if (e.keyCode == KeyCode.F2 || e.keyCode == KeyCode.Insert) showMenu = !showMenu;
            if (showMenu) {
                if (e.keyCode == KeyCode.DownArrow || e.keyCode == KeyCode.S) { selIdx = (selIdx + 1) % items.Length; e.Use(); }
                if (e.keyCode == KeyCode.UpArrow || e.keyCode == KeyCode.W) { selIdx = (selIdx - 1 + items.Length) % items.Length; e.Use(); }
                if (e.keyCode == KeyCode.Return) { Execute(selIdx); e.Use(); }
            }
        }
        if (!showMenu) return;
        GUI.backgroundColor = Color.black;
        winRect = GUILayout.Window(0, winRect, DrawMenu, "/// DEATH PONG GOD-MODE ///");
    }

    void DrawMenu(int windowID) {
        GUILayout.Space(20);
        for (int i = 0; i < items.Length; i++) {
            string state = (i < 10) ? (GetState(i) ? ": ON" : ": OFF") : "";
            if (i == 9) state = bStays ? ": ON (SHIFT TO EXIT)" : ": OFF";
            GUI.contentColor = (i == selIdx) ? Color.cyan : Color.white;
            GUILayout.Label(((i == selIdx) ? " > [ " : "   [ ") + items[i] + state + " ]");
        }
        GUI.contentColor = Color.gray;
        GUILayout.FlexibleSpace();
        GUILayout.Label(" ARROWS: Navigate | ENTER: Toggle | F2: Hide");
    }

    bool GetState(int index) {
        if (index == 0) return sober; if (index == 1) return infMoney;
        if (index == 2) return godMode; if (index == 3) return autoPlay;
        if (index == 4) return myTurn; if (index == 5) return aimbot;
        if (index == 6) return freezeTimer; if (index == 7) return sabotage;
        if (index == 8) return ironCups; if (index == 9) return bStays; return false;
    }

    void Execute(int index) {
        if (index == 0) sober = !sober;
        else if (index == 1) infMoney = !infMoney;
        else if (index == 2) godMode = !godMode;
        else if (index == 3) { autoPlay = !autoPlay; if(autoPlay) { aimbot = true; myTurn = true; } }
        else if (index == 4) myTurn = !myTurn;
        else if (index == 5) aimbot = !aimbot;
        else if (index == 6) freezeTimer = !freezeTimer;
        else if (index == 7) sabotage = !sabotage;
        else if (index == 8) ironCups = !ironCups;
        else if (index == 9) { bStays = !bStays; if(!bStays) DismissBar(); }
        else if (index == 10) SummonBar();
        else if (index == 11) Nuke();
        else if (index == 12) Regen();
        else if (index == 13) showMenu = false;
    }

    void DismissBar() {
        ShopController sc = ShopController.Instance;
        if (sc != null) {
            bool prev = bStays;
            bStays = false;
            sc.HideShotsSelection();
            BartenderController bc = UnityEngine.Object.FindFirstObjectByType<BartenderController>();
            if (bc != null) {
                Animator anim = Traverse.Create(bc).Field("_animator").GetValue<Animator>();
                if (anim != null) anim.SetTrigger("Reverse");
            }
            bStays = prev;
        }
    }

    void SummonBar() {
        GameMode gm = UnityEngine.Object.FindFirstObjectByType<GameMode>();
        if (gm != null) {
            AccessTools.Method(typeof(StateMachine<GameState>), "ChangeState").Invoke(gm, new object[] { GameState.Buying });
        }
    }

    void Nuke() {
        GameModeDeathPong gm = UnityEngine.Object.FindFirstObjectByType<GameModeDeathPong>();
        if (gm != null) { foreach (var cup in gm.Cups) { if (cup.cupObject.activeInHierarchy) { cup.cupObject.SetActive(false); gm.AddPoints(Turn.Player, false); } } }
    }

    void Regen() {
        GameModeDeathPong gm = UnityEngine.Object.FindFirstObjectByType<GameModeDeathPong>();
        if (gm != null) { foreach (var cup in gm.AICups) cup.cupObject.SetActive(true); }
    }
}

[HarmonyPatch(typeof(HandPlayer), "HandControls")]
public static class Patch_Aimbot_ArmLock {
    static void Prefix(HandPlayer __instance, ref float minForce, ref float maxForce) {
        if (BespokeTrainer.aimbot && __instance.ThrowState == ThrowState.Ready) {
            GameMode gm = UnityEngine.Object.FindFirstObjectByType<GameMode>();
            if (gm == null || gm.Cups == null) return;

            GameObject tCup = null;
            for (int i = 0; i < gm.Cups.Count; i++) {
                if (gm.Cups[i].cupObject != null && gm.Cups[i].cupObject.activeInHierarchy) {
                    tCup = gm.Cups[i].cupObject; break;
                }
            }

            if (tCup != null) {
                Vector3 sPos = Traverse.Create(__instance).Field("_startTransform").GetValue<Transform>().position;
                Vector3 tPos = tCup.transform.position;

                Vector3 dir = tPos - sPos;
                float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                Traverse.Create(__instance).Field("_currentTx").SetValue(yaw);

                // safe arc
                Traverse.Create(__instance).Field("_rotX").SetValue(-32f);

                float distXZ = new Vector2(dir.x, dir.z).magnitude;
                float force = Mathf.Clamp(9.5f + (distXZ * 2.8f), minForce, maxForce);
                __instance.ThrowForce = force;
            }
        }
    }
}

[HarmonyPatch(typeof(BallController), "FixedUpdate")]
public static class Patch_Aimbot_MagnetAndSafety {
    private static float aliveTime = 0f;

    static void Postfix(BallController __instance) {
        if (__instance.IsGhost) return;

        // softlock fix
        aliveTime += Time.fixedDeltaTime;
        if (aliveTime > 8.0f) {
            aliveTime = 0f;
            __instance.gameObject.SetActive(false);
            return;
        }

        if (BespokeTrainer.aimbot && __instance.BallOwner == Turn.Player) {
            Rigidbody rb = __instance.GetComponent<Rigidbody>();
            if (rb == null) return;

            GameMode gm = UnityEngine.Object.FindFirstObjectByType<GameMode>();
            if (gm == null || gm.Cups == null) return;

            GameObject tCup = null;
            for (int i = 0; i < gm.Cups.Count; i++) {
                if (gm.Cups[i].cupObject != null && gm.Cups[i].cupObject.activeInHierarchy) {
                    tCup = gm.Cups[i].cupObject; break;
                }
            }

            if (tCup != null) {
                Vector3 cPos = __instance.transform.position;
                Vector3 tPos = tCup.transform.position;
                float dXZ = Vector2.Distance(new Vector2(cPos.x, cPos.z), new Vector2(tPos.x, tPos.z));

                if (dXZ < 0.28f && cPos.y > tPos.y) {
                    rb.linearVelocity = new Vector3(0, -12f, 0);
                } else {
                    Vector3 pull = (tPos - cPos);
                    pull.y = 0;
                    rb.AddForce(pull.normalized * 10f, ForceMode.Acceleration);
                }
            }
        }
    }
}

[HarmonyPatch(typeof(BallController), "OnEnable")]
public static class Patch_ResetSafety { static void Postfix() { Traverse.Create(typeof(Patch_Aimbot_MagnetAndSafety)).Field("aliveTime").SetValue(0f); } }

[HarmonyPatch(typeof(ShopController), "HideShotsSelection")]
public static class Patch_UnlimitedShop {
    static bool Prefix() {
        if (BespokeTrainer.bStays && !Input.GetKey(KeyCode.LeftShift)) return false;
        return true;
    }
}

[HarmonyPatch(typeof(Animator), "SetTrigger", new Type[] { typeof(string) })]
public static class Patch_NoReverseBartender {
    static bool Prefix(string name) {
        if (BespokeTrainer.bStays && name == "Reverse" && !Input.GetKey(KeyCode.LeftShift)) return false;
        return true;
    }
}

[HarmonyPatch(typeof(GameModeDeathPong), "FlipACoin")]
public static class Patch_RigCoinToss { static bool Prefix(GameModeDeathPong __instance) { __instance.HandManager.CurrentTurn = Turn.Player; return false; } }

[HarmonyPatch(typeof(HandManager), "CurrentTurn", MethodType.Setter)]
public static class Patch_AlwaysMyTurn_Setter { static void Prefix(ref Turn value) { if (BespokeTrainer.myTurn) value = Turn.Player; } }

[HarmonyPatch(typeof(HandManager), "ChangeCurrentTurn")]
public static class Patch_AlwaysMyTurn_Method { static bool Prefix(HandManager __instance) { if (BespokeTrainer.myTurn) { __instance.CurrentTurn = Turn.Player; return false; } return true; } }

[HarmonyPatch(typeof(Hand), "DrunkennessLevel", MethodType.Getter)]
public static class Patch_NoDrunkGetter { static void Postfix(ref float __result) { if (BespokeTrainer.sober) __result = 0f; } }

[HarmonyPatch(typeof(HandPlayer), "ProcessShakeHand")]
public static class Patch_NoShake { static bool Prefix() { return !BespokeTrainer.sober; } }

[HarmonyPatch(typeof(StrangeEffect), "OnRenderImage")]
public static class Patch_NoDoubleVision {
    static bool Prefix(RenderTexture source, RenderTexture destination) {
        if (BespokeTrainer.sober) { Graphics.Blit(source, destination); return false; }
        return true;
    }
}

[HarmonyPatch(typeof(CupRemover), "OnTriggerEnter")]
public static class Patch_IronCups { static bool Prefix(CupRemover __instance, Collider other) { if (BespokeTrainer.ironCups) { Turn side = Traverse.Create(__instance).Field("_cupType").GetValue<Turn>(); if (side == Turn.Player && other.CompareTag("Ball")) return false; } return true; } }

[HarmonyPatch(typeof(GameModeDeathPong), "Timer")]
public static class Patch_Timer { static bool Prefix() { return !BespokeTrainer.freezeTimer; } }

[HarmonyPatch(typeof(GameMode), "IsPersistentIngredientOnList", new Type[] { typeof(Ingredients), typeof(HandType) })]
public static class Patch_GodMode { static void Postfix(Ingredients ingredient, ref bool __result) { if (BespokeTrainer.godMode && ingredient == Ingredients.SecondChance) __result = true; } }

[HarmonyPatch(typeof(IntroSceneManager), "Awake")]
public static class Patch_IntroKill { static bool Prefix(IntroSceneManager __instance) { if (BespokeTrainer.skipAll) { int nextScene = Traverse.Create(__instance).Field("_nextScene").GetValue<int>(); SceneManager.LoadScene(nextScene); return false; } return true; } }

[HarmonyPatch(typeof(LobbyDialogueController), "Start")]
public static class Patch_SkipLobbyIntro {
    static bool Prefix(LobbyDialogueController __instance) {
        if (BespokeTrainer.skipAll) {
            var score = Traverse.Create(__instance).Field("_playerScore").GetValue<PlayerScore>();
            if (score != null) { score.isIntroFinished = true; score.isWinDialogueFinished = true; }
            var bs = Traverse.Create(__instance).Field("_blackScreen").GetValue<GameObject>();
            if (bs != null) bs.SetActive(false);
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            if (player != null) {
                var anim = Traverse.Create(player).Field("_animator").GetValue<Animator>();
                if (anim != null) { anim.applyRootMotion = true; anim.SetTrigger("StandUp"); }
                Traverse.Create(player).Field("_dialogueFinished").SetValue(true);
                Traverse.Create(player).Field("_canMove").SetValue(true);
                AccessTools.Method(typeof(PlayerController), "EnablePlayerInput").Invoke(player, null);
            }
            var lobbyEvent = Traverse.Create(typeof(LobbyDialogueController)).Field("LobbyDialogueFinished").GetValue<Action<int>>();
            if (lobbyEvent != null) { lobbyEvent(1); lobbyEvent(2); }
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(DialogueSO), "LettersSpeed", MethodType.Getter)]
public static class Patch_DialogueSpeed { static bool Prefix(ref float __result) { if(BespokeTrainer.skipAll) { __result = 0f; return false; } return true; } }

[HarmonyPatch(typeof(DialogueSO), "ShouldAutoComplete", MethodType.Getter)]
public static class Patch_DialogueAuto { static bool Prefix(ref bool __result) { if(BespokeTrainer.skipAll) { __result = true; return false; } return true; } }

[HarmonyPatch(typeof(DialogueSO), "AutoCompleteTime", MethodType.Getter)]
public static class Patch_DialogueTime { static bool Prefix(ref float __result) { if(BespokeTrainer.skipAll) { __result = 0f; return false; } return true; } }

[HarmonyPatch(typeof(LoadingScreenManager), "Start")]
public static class Patch_SkipFadeInOnStart {
    static void Postfix(LoadingScreenManager __instance) {
        if (BespokeTrainer.skipAll) {
            GameObject foc = Traverse.Create(__instance).Field("_fadeOutCanvas").GetValue<GameObject>();
            if (foc != null) {
                foc.SetActive(false);
                var helper = foc.GetComponentInChildren<LoadingScreenHelper>(true);
                if (helper != null) helper.OnFadeInFinished();
            }
        }
    }
}

[HarmonyPatch(typeof(LoadingScreenManager), "PrepareSceneLoading")]
public static class Patch_FastLoadScene {
    static bool Prefix(LoadingScreenManager __instance, SceneType sceneType) {
        if (BespokeTrainer.skipAll) {
            var ev = Traverse.Create(typeof(LoadingScreenManager)).Field("SceneLoadStart").GetValue<Action>();
            if (ev != null) ev();
            __instance.StartCoroutine(FastLoad(__instance, sceneType));
            return false;
        }
        return true;
    }
    static IEnumerator FastLoad(LoadingScreenManager __instance, SceneType sceneType) {
        var dict = Traverse.Create(__instance).Field("_scenesDict").GetValue<Dictionary<SceneType, int>>();
        int bIdx = dict[sceneType];
        var op = SceneManager.LoadSceneAsync(bIdx);
        Traverse.Create(__instance).Field("_sceneLoadAsync").SetValue(op);
        op.allowSceneActivation = true;
        while (!op.isDone) { yield return null; }
        var fin = Traverse.Create(typeof(LoadingScreenManager)).Field("SceneLoadFinished").GetValue<Action>();
        if (fin != null) fin();
    }
}
