namespace Gunfiguration;

/*
  Configuration options for general quality-of-life changes, documented for API reference.
*/

using Gunfiguration; // Make sure you're using the Gunfiguration API

public static class QoLConfig
{
  // It is highly recommended to call Gunfig.Get() once for your mod and cache the result in a static variable.
  internal static Gunfig _Gunfig = null;

  // It is highly recommended to use constant strings for option keys, as it greatly simplifies working with options.
  internal const string FINGER_SAVER    = "Auto-fire Semi-Automatic Weapons";
  internal const string PLAYER_TWO_CHAR = "Co-op Character";
  internal const string QUICKSTART      = "Quick Start Behavior";
  internal const string COOP_FOYER_SEL  = "Breach Co-op Character Select";
  internal const string STATIC_CAMERA   = "Static Camera While Aiming";
  internal const string MENU_SOUNDS     = "Better Menu Sounds";
  internal const string HEROBRINE       = "Disable Herobrine";
  internal const string HEALTH_BARS     = "Show Enemy Health Bars";
  internal const string DAMAGE_NUMS     = "Show Damage Numbers";
  internal const string CHEATS_LABEL    = "Cheats / Debug Stuff";
  internal const string SPAWN_ITEMS     = "Spawn Items from Ammonomicon";
  internal const string FAST_POKEDEX    = "Ammonomicon Opens Instantly";
  internal const string TARGET_POKEDEX  = "Ammonomicon Opens To Targeted Item";
  internal const string ALL_THE_ITEMS   = "Unlimited Active Items";
  internal const string INFINITE_META   = "Infinite Hegemony Credits";
  internal const string OPEN_DEBUG_LOG  = "Open Debug Log on Exit";

  // Note the formatting applied to individual labels. Formatting can be applied to all menus strings, but NOT to option keys.
  private static readonly List<string> _QUICKSTART_OPTIONS = new() {
    "Vanilla",
    "Extended".Yellow(),
    "Extended + Co-op".Yellow(),
  };

  private static readonly List<string> _QUICKSTART_DESCRIPTIONS = new() {
    "Vanilla quickstart behavior".Green(),
    "Allows quickstarting on the main menu\nafter the title sequence".Green(),
    "Quick start will automatically start co-op\nif a second controller is plugged in".Green(),
  };

  private static readonly List<string> _OPEN_LOG_OPTIONS = new() {
    "Never",
    "After 1 error".Yellow(),
    "After 10 errors".Yellow(),
    "After 100 errors".Yellow(),
    "After 1000 errors".Yellow(),
  };

  private static readonly List<string> _OPEN_LOG_DESCRIPTIONS = new() {
    "The debug log will never open automatically.".Green(),
    "Opens the debug log when closing Gungeon\nif at least 1 error has occurred.".Green(),
    "Opens the debug log when closing Gungeon\nif at least 10 errors have occurred.".Green(),
    "Opens the debug log when closing Gungeon\nif at least 100 errors have occurred.".Green(),
    "Opens the debug log when closing Gungeon\nif at least 1000 errors have occurred.".Green(),
  };

  private static readonly Dictionary<string, string> _PLAYER_MAP = new() {
    { "Cultist",    "coopcultist" },
    { "Pilot",      "rogue"       },
    { "Marine",     "marine"      },
    { "Convict",    "convict"     },
    { "Hunter",     "guide"       },
    { "Bullet",     "bullet"      },
    { "Robot",      "robot"       },
    { "Paradox",    "eevee"       },
    { "Gunslinger", "gunslinger"  },
  };

  internal static void Init()
  {
    // Sets up a gunfig page named "Quality of Life", loads any existing "Quality of Life.gunfig" configuration from disk, and adds it to the Mod Config menu.
    // It is recommended (but not necessary) to call Gunfig.Get() once and store the result in a static variable.
    // You can replace WithColor() with any color you want to change the appearance on the mod menu. Defaults to white if nothing is specified.
    // Calling Gunfig.Get() with the same page name will always return the same Gunfig instance, ignoring color markup.
    // E.g., "Quality of Life".Red() will return the same page as "Quality of Life".Green() or simply "Quality of Life".
    _Gunfig = Gunfig.Get(modName: "Quality of Life".WithColor(Color.white));

    // Build up a list of options for co-op players, highlight non-default (non-Cultist) characters in yellow, and add a scrollbox selector to the menu.
    // We can get the value of scrollbox items later using Gunfig.Value().
    List<string> players = new();
    foreach (string player in _PLAYER_MAP.Keys)
      players.Add((player == "Cultist") ? player : player.Yellow());
    _Gunfig.AddScrollBox(key: PLAYER_TWO_CHAR, options: players);

    // Add another scrollbox selector with description text for each item.
    _Gunfig.AddScrollBox(key: QUICKSTART, options: _QUICKSTART_OPTIONS, info: _QUICKSTART_DESCRIPTIONS);

    // Simple toggles can be created with extremely minimal setup! We can get toggle options later with, e.g., Gunfig.Enabled() or Gunfig.Disabled().
    _Gunfig.AddToggle(key: COOP_FOYER_SEL);
    _Gunfig.AddToggle(key: FINGER_SAVER);
    _Gunfig.AddToggle(key: STATIC_CAMERA);
    _Gunfig.AddToggle(key: HEALTH_BARS);
    _Gunfig.AddToggle(key: DAMAGE_NUMS);
    _Gunfig.AddToggle(key: FAST_POKEDEX);

    // Add a toggle that's enabled by default
    _Gunfig.AddToggle(key: TARGET_POKEDEX, enabled: true);

    // Add a toggle that goes into effect immediately without awaiting confirmation from the player.
    _Gunfig.AddToggle(key: MENU_SOUNDS, updateType: Gunfig.Update.Immediate);

    // Add a colorized submenu button
    Gunfig cheats = _Gunfig.AddSubMenu(CHEATS_LABEL.Magenta());

    // Add colorized toggles to our submenu
    cheats.AddToggle(key: SPAWN_ITEMS, label: SPAWN_ITEMS.Magenta());
    cheats.AddToggle(key: ALL_THE_ITEMS, label: ALL_THE_ITEMS.Magenta());
    cheats.AddToggle(key: INFINITE_META, label: INFINITE_META.Magenta());
    cheats.AddScrollBox(key: OPEN_DEBUG_LOG, options: _OPEN_LOG_OPTIONS, info: _OPEN_LOG_DESCRIPTIONS);

    // Add a button with a custom callback when processed. Buttons always trigger their callbacks immediately when pressed.
    // Note that we have to explicitly specify a label to color the button text Red, as we cannot add colors to the key.
    cheats.AddButton(key: HEROBRINE, label: HEROBRINE.Red(),
      callback: (optionKey, optionValue) => ETGModConsole.Log($"Clicked the {optionKey} button...but you can't disable Herobrine :/"));

    // Do some extra setup once all mods are loaded in
    Gunfig.OnAllModsLoaded += LateInit;

    // See the hooks and functions throughout the remainder of this file to see examples of how configuration options are used in practice.
    InitQoLHooks();
  }

  private static void LateInit()
  {
    GunfigDebug.Log($"doing late init for QoL config");
    Application.logMessageReceived += CountErrors;
    _QuitListener = GameManager.Instance.gameObject.AddComponent<GunfigApplicationQuitListener>();
  }

  private class GunfigApplicationQuitListener : MonoBehaviour
  {
    private void OnApplicationQuit() => MaybeOpenDebugLog();
  }

  private static int _ErrorCount = 0;
  private static GunfigApplicationQuitListener _QuitListener = null;
  private static void CountErrors(string text, string stackTrace, LogType type)
  {
    if (type == LogType.Exception)
      ++_ErrorCount;
  }

  private static void MaybeOpenDebugLog()
  {
    UnityEngine.Object.DestroyImmediate(_QuitListener);
    string openDebugLogOption = _Gunfig.Value(OPEN_DEBUG_LOG);
    if (openDebugLogOption == "Never")
      return;

    int threshold = Int32.Parse(openDebugLogOption.Split(' ')[1]);
  #if DEBUG
    System.Console.WriteLine($"attempting to open debug log at {threshold} errors, have {_ErrorCount}");
  #endif
    if (_ErrorCount < threshold)
      return;

    _ErrorCount = 0;
    string logFilePath = Path.Combine(BepInEx.Paths.BepInExRootPath, "LogOutput.log");

    System.Diagnostics.ProcessStartInfo pi = new();
    pi.UseShellExecute = true;
    if (Application.platform == RuntimePlatform.WindowsPlayer)
    {
      pi.FileName = "explorer.exe";
      pi.Arguments = logFilePath;
    }
    else
    {
      pi.FileName = logFilePath;
    }

    System.Diagnostics.Process.Start(pi);
  }

  private static void InitQoLHooks()
  {
    // Hook into PlayerController Awake to set up some QoL events
    new Hook(
      typeof(PlayerController).GetMethod("Awake", BindingFlags.Instance | BindingFlags.Public),
      typeof(QoLConfig).GetMethod("OnPlayerAwake", BindingFlags.Static | BindingFlags.NonPublic)
      );

    // Make toggles play UI sounds when they're pressed
    new Hook(
      typeof(BraveOptionsMenuItem).GetMethod("ToggleCheckbox", BindingFlags.Instance | BindingFlags.NonPublic),
      typeof(QoLConfig).GetMethod("OnToggleCheckbox", BindingFlags.Static | BindingFlags.NonPublic)
      );

    // Make fillbars play UI sounds when they're pressed
    new Hook(
      typeof(BraveOptionsMenuItem).GetMethod("HandleFillbarValueChanged", BindingFlags.Instance | BindingFlags.NonPublic),
      typeof(QoLConfig).GetMethod("OnHandleFillbarValueChanged", BindingFlags.Static | BindingFlags.NonPublic)
      );

    // Remove cooldown for semiautomatic weapons
    new Hook(
      typeof(BraveInput).GetMethod("get_ControllerFakeSemiAutoCooldown", BindingFlags.Static | BindingFlags.Public),
      typeof(QoLConfig).GetMethod("OverrideSemiAutoCooldown", BindingFlags.Static | BindingFlags.NonPublic)
      );

    // Change default coop character
    new Hook(
      typeof(HutongGames.PlayMaker.Actions.ChangeCoopMode).GetMethod("GeneratePlayer", BindingFlags.Instance | BindingFlags.NonPublic),
      typeof(QoLConfig).GetMethod("OnGenerateCoopPlayer", BindingFlags.Static | BindingFlags.NonPublic)
      );

    // Allow quick starting on main menu after title sequence has finished
    new Hook(
      typeof(MainMenuFoyerController).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic),
      typeof(QoLConfig).GetMethod("OnMainMenuUpdate", BindingFlags.Static | BindingFlags.NonPublic)
      );

    // Coop 2nd player preload fixer
    new Hook(
      typeof(Dungeonator.Dungeon).GetMethod("GeneratePlayerIfNecessary", BindingFlags.Instance | BindingFlags.NonPublic),
      typeof(QoLConfig).GetMethod("OnGeneratePlayerIfNecessary", BindingFlags.Static | BindingFlags.NonPublic)
      );

    // Static Camera
    new Hook(
      typeof(CameraController).GetMethod("GetCoreOffset", BindingFlags.Instance | BindingFlags.NonPublic),
      typeof(QoLConfig).GetMethod("OnGetCoreOffset", BindingFlags.Static | BindingFlags.NonPublic)
      );
  }

  private static void OnPlayerAwake(Action<PlayerController> orig, PlayerController player)
  {
    orig(player);
    player.OnAnyEnemyReceivedDamage += DoHealthEffects; // health bars and damage numbers (borrowed from Scouter)
  }

  private static void OnToggleCheckbox(Action<BraveOptionsMenuItem, dfControl, dfMouseEventArgs> orig, BraveOptionsMenuItem item, dfControl control, dfMouseEventArgs args)
  {
    orig(item, control, args);
    if (_Gunfig.Enabled(MENU_SOUNDS))
      AkSoundEngine.PostEvent("Play_UI_menu_select_01", item.gameObject);
  }

  private static void OnHandleFillbarValueChanged(Action<BraveOptionsMenuItem> orig, BraveOptionsMenuItem item)
  {
    orig(item);
    if (_Gunfig.Enabled(MENU_SOUNDS))
      AkSoundEngine.PostEvent("Play_UI_menu_select_01", item.gameObject);
  }

  private static float OverrideSemiAutoCooldown(Func<float> orig)
  {
    if (_Gunfig.Enabled(FINGER_SAVER))
        return 0f; // replace the value we're checking against with 0f to completely remove semi-automatic fake cooldown
    return orig(); // return the original value
  }

  // Allow Blasphemy to benefit from SemiAutomatic weapon autofire since it counts as an "Empty" gun normally
  [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.HandleGunFiringInternal))]
  private static class PlayerControllerHandleGunFiringInternalPatch
  {
      [HarmonyILManipulator]
      private static void PlayerControllerHandleGunFiringInternalIL(ILContext il)
      {
          ILCursor cursor = new ILCursor(il);
          if (!cursor.TryGotoNext(MoveType.After,
              instr => instr.MatchCallvirt<Gun>("get_IsEmpty")))
              return;
          cursor.Emit(OpCodes.Ldarg_1); // Gun
          cursor.Emit(OpCodes.Call, typeof(PlayerControllerHandleGunFiringInternalPatch).GetMethod(
            nameof(AllowBlasphemyAutofire), BindingFlags.Static | BindingFlags.NonPublic));
      }

      private static bool AllowBlasphemyAutofire(bool orig, Gun gun) => orig && !gun.IsHeroSword;
  }

  internal static string _ManualCoopPrefabOverride = null;
  private static PlayerController OnGenerateCoopPlayer(Func<HutongGames.PlayMaker.Actions.ChangeCoopMode, PlayerController> orig, HutongGames.PlayMaker.Actions.ChangeCoopMode coop)
  {
    coop.PlayerPrefabPath = _ManualCoopPrefabOverride ?? $"Player{_PLAYER_MAP[_Gunfig.Value(PLAYER_TWO_CHAR)]}";
    return orig(coop);
  }

  private static FullOptionsMenuController _OptionsMenu = null;
  private static void OnMainMenuUpdate(Action<MainMenuFoyerController> orig, MainMenuFoyerController menu)
  {
    orig(menu);

    if (!_OptionsMenu)
      _OptionsMenu = GameUIRoot.Instance.PauseMenuPanel.GetComponent<PauseMenuController>().OptionsMenu;
    if (_OptionsMenu.IsVisible || (_OptionsMenu.PreOptionsMenu && _OptionsMenu.PreOptionsMenu.IsVisible))
      return; // disallow extended quickstarting if the game is paused

    if (!Foyer.DoIntroSequence && !Foyer.DoMainMenu)
      return; // disallow extended quickstarting if we're actively in the Breach

    if (_Gunfig.Value(QUICKSTART) == "Vanilla")
      return; // disallow extended quickstarting if the option isn't toggled on

    FinalIntroSequenceManager introManager = Foyer.Instance?.IntroDoer;
    if (introManager?.gameObject == null || introManager.m_isDoingQuickStart || !introManager.QuickStartAvailable())
      return; // disallow extended quickstarting if we're already in the middle of a quickstart

    if (!(BraveInput.PlayerlessInstance?.ActiveActions?.Device?.Action4?.WasPressed ?? false) && !Input.GetKeyDown(KeyCode.Q))
      return; // if we're not trying to quickstart, there's nothing else to do

    if (GameManager.HasValidMidgameSave())
      return; // disallow extended quickstarting when we have a midgame save

    // logic yoinked from Foyer.Start() and FinalIntroSequenceManager.HandleBackgroundSkipChecks()
    menu.DisableMainMenu();
    UnityEngine.Object.FindObjectOfType<TitleDioramaController>()?.ForceHideFadeQuad();
    introManager.gameObject.transform.parent?.gameObject?.SetActive(true);
    introManager.m_skipCycle = true;
    introManager.m_isDoingQuickStart = true;

    if (InControl.InputManager.Devices.Count > 0 && _Gunfig.Value(QUICKSTART).Contains("Co-op"))
      GameManager.Instance.StartCoroutine(DoCoopQuickStart(introManager));
    else
      introManager.StartCoroutine(introManager.DoQuickStart());
  }

  private static void OnGeneratePlayerIfNecessary(Action<Dungeonator.Dungeon, MidGameSaveData> orig, Dungeonator.Dungeon dungeon, MidGameSaveData midgameSave)
  {
    if (midgameSave != null)
      { orig(dungeon, midgameSave); return; } // don't interfere with loading svaes
    if (GameManager.Instance.CurrentLevelOverrideState == GameManager.LevelOverrideState.FOYER)
      { orig(dungeon, midgameSave); return; } // don't interfere with loading the breach
    if (GameManager.Instance.CurrentGameType != GameManager.GameType.COOP_2_PLAYER)
      { orig(dungeon, midgameSave); return; } // don't interfere with single player

    if (GameManager.Instance.AllPlayers.Length == 0) // regenerate both players for custom quick restarts in coop
    {
      GeneratePlayerOne();
      GeneratePlayerTwo();
      return;
    }

    orig(dungeon, midgameSave);
  }

  private static IEnumerator DoCoopQuickStart(FinalIntroSequenceManager introManager)
  {
    introManager.QuickStartObject?.SetActive(false);
    introManager.StartCoroutine(introManager.FadeToBlack(0.1f, true, true));

    GameManager.PreventGameManagerExistence = false;
    GameManager.SKIP_FOYER = true;
    Foyer.DoMainMenu = false;
    AkSoundEngine.LoadBank("SFX.bnk", -1, out uint out_bankID);
    GameManager.EnsureExistence();

    GameManager.PlayerPrefabForNewGame = (GameObject)BraveResources.Load(CharacterSelectController.GetCharacterPathFromQuickStart());
    GameManager.Instance.GlobalInjectionData.PreprocessRun();
    GameManager.Instance.IsSelectingCharacter = false;
    GameManager.Instance.IsFoyer = false;

    GeneratePlayerOne();
    yield return null;  // these yields are necessary to make sure Unity has a change to register the instantiation of each player

    GameManager.Instance.CurrentGameType = GameManager.GameType.COOP_2_PLAYER;
    GeneratePlayerTwo();
    yield return null;

    GameManager.Instance.RefreshAllPlayers();
    GameManager.Instance.FlushMusicAudio();
    GameManager.Instance.SetNextLevelIndex(1);
    yield return null;

    Foyer.Instance.OnDepartedFoyer();
    yield return null;

    GameManager.Instance.LoadNextLevel();
  }

  private static void GeneratePlayerOne()
  {
    PlayerController playerController = GameManager.PlayerPrefabForNewGame.GetComponent<PlayerController>();
    GameStatsManager.Instance.BeginNewSession(playerController);
    GameObject instantiatedPlayer = UnityEngine.Object.Instantiate(GameManager.PlayerPrefabForNewGame, Vector3.zero, Quaternion.identity);
    GameManager.PlayerPrefabForNewGame = null;
    instantiatedPlayer.SetActive(true);
    PlayerController extantPlayer = instantiatedPlayer.GetComponent<PlayerController>();
    extantPlayer.PlayerIDX = 0;
    GameManager.Instance.PrimaryPlayer = extantPlayer;
  }

  private static void GeneratePlayerTwo()
  {
    string p2Prefab = _ManualCoopPrefabOverride ?? $"Player{_PLAYER_MAP[_Gunfig.Value(PLAYER_TWO_CHAR)]}";
    GameObject instantiatedCoopPlayer = UnityEngine.Object.Instantiate((GameObject)BraveResources.Load(p2Prefab), Vector3.zero, Quaternion.identity);
    instantiatedCoopPlayer.SetActive(true);
    PlayerController extantCoopPlayer = instantiatedCoopPlayer.GetComponent<PlayerController>();
    extantCoopPlayer.PlayerIDX = 1;
    GameManager.Instance.SecondaryPlayer = extantCoopPlayer;
  }

  private static Vector2 OnGetCoreOffset(Func<CameraController, Vector2, bool, bool, Vector2> orig, CameraController cam, Vector2 currentBasePosition, bool isUpdate, bool allowAimOffset)
  {
    if (_Gunfig.Enabled(STATIC_CAMERA))
      return Vector2.zero;
    return orig(cam, currentBasePosition, isUpdate, allowAimOffset);
  }

  private static bool HasPassive(this PlayerController pc, int pickupId)
  {
    int n = pc.passiveItems.Count;
    for (int i = 0; i < n; i++)
      if (pc.passiveItems[i].PickupObjectId == pickupId)
        return true;
    return false;
  }

  private static GameObject VFXHealthBar = null;
  private static readonly int ScouterId = 821;
  private static void DoHealthEffects(float damageAmount, bool fatal, HealthHaver target)
  {
    bool showHealth = _Gunfig.Enabled(HEALTH_BARS);
    bool showDamage = _Gunfig.Enabled(DAMAGE_NUMS);

    if (!showHealth && !showDamage)
      return;
    if (GameManager.Instance.PrimaryPlayer.HasPassive(ScouterId))
      return;
    if (GameManager.Instance.CurrentGameType == GameManager.GameType.COOP_2_PLAYER && GameManager.Instance.SecondaryPlayer.HasPassive(ScouterId))
      return;

    VFXHealthBar ??= (PickupObjectDatabase.GetById(ScouterId) as RatchetScouterItem).VFXHealthBar;

    Vector3 worldPosition = target.transform.position;
    float heightOffGround = 1f;

    if (target.GetComponent<SpeculativeRigidbody>() is SpeculativeRigidbody body)
    {
      worldPosition = body.UnitCenter.ToVector3ZisY();
      heightOffGround = worldPosition.y - body.UnitBottomCenter.y;
      if (showHealth && (bool)body.healthHaver && !body.healthHaver.HasHealthBar && !body.healthHaver.HasRatchetHealthBar && !body.healthHaver.IsBoss)
      {
        body.healthHaver.HasRatchetHealthBar = true;
        UnityEngine.Object.Instantiate(VFXHealthBar).GetComponent<SimpleHealthBarController>().Initialize(body, body.healthHaver);
      }
    }
    else if (target.GetComponent<AIActor>() is AIActor actor)
    {
      worldPosition = actor.CenterPosition.ToVector3ZisY();
      if (actor.sprite)
        heightOffGround = worldPosition.y - actor.sprite.WorldBottomCenter.y;
    }

    if (showDamage)
      GameUIRoot.Instance.DoDamageNumber(worldPosition, heightOffGround, Mathf.Max(Mathf.RoundToInt(damageAmount), 1));
  }

  [HarmonyPatch(typeof(AmmonomiconPokedexEntry), nameof(AmmonomiconPokedexEntry.Awake))]
  class SpawnItemsFromAmmonomiconPatch
  {
      static void Postfix(AmmonomiconPokedexEntry __instance)
      {
          __instance.m_button.KeyDown += OnKeyDown;
      }

      private static void OnKeyDown(dfControl control, dfKeyEventArgs keyEvent)
      {
          if (keyEvent.KeyCode != KeyCode.Return)
              return;
          if (!_Gunfig.Enabled(SPAWN_ITEMS))
              return;
          if (control.GetComponent<AmmonomiconPokedexEntry>() is not AmmonomiconPokedexEntry ammonomiconEntry)
              return;
          if (ammonomiconEntry.encounterState != AmmonomiconPokedexEntry.EncounterState.ENCOUNTERED)
              return;
          if (ammonomiconEntry.linkedEncounterTrackable is not EncounterDatabaseEntry databaseEntry)
              return;
          if (databaseEntry.pickupObjectId == -1)
              return;
          LootEngine.SpawnItem(PickupObjectDatabase.GetById(databaseEntry.pickupObjectId).gameObject, GameManager.Instance.BestActivePlayer.CenterPosition, Vector2.up, 1f);
          AkSoundEngine.PostEvent("Play_OBJ_power_up_01", GameManager.Instance.BestActivePlayer.gameObject);
      }
  }

  [HarmonyPatch(typeof(RoomHandler), nameof(RoomHandler.GetNearestInteractable))]
  private class GetNearestInteractablePatch
  {
        [HarmonyILManipulator]
        private static void GetNearestInteractableIL(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<TalkDoerLite>("PreventCoopInteraction")))
                return;
            cursor.Emit(OpCodes.Call, typeof(GetNearestInteractablePatch).GetMethod("CheckPreventCoopInteraction", BindingFlags.Static | BindingFlags.NonPublic));
        }

        private static bool CheckPreventCoopInteraction(bool oldValue)
        {
          return oldValue && _Gunfig.Disabled(COOP_FOYER_SEL);
        }
  }

  [HarmonyPatch(typeof(TalkDoerLite), nameof(TalkDoerLite.Interact))]
  private class FoyerCharacterCoopInteractPatch
  {
      static bool Prefix(TalkDoerLite __instance, PlayerController interactor)
      {
        if (interactor == GameManager.Instance.PrimaryPlayer)
          return true; // call the original method
        if (!GameManager.Instance.IsFoyer)
          return true; // call the original method
        if (_Gunfig.Disabled(COOP_FOYER_SEL))
          return true; // call the original method
        if (__instance.gameObject.GetComponent<FoyerCharacterSelectFlag>() is not FoyerCharacterSelectFlag f)
          return true; // call the original method
        if (!f.CanBeSelected())
          return true; // call the original method
        ReassignCoopPlayer(f);
        return false; // skip the original method
      }

      private static PlayerController ReassignCoopPlayer(FoyerCharacterSelectFlag f)
      {
        _ManualCoopPrefabOverride = f.CharacterPrefabPath;
        Vector2 poofPos = GameManager.Instance.SecondaryPlayer.CenterPosition;
        Vector3 spawnPos = GameManager.Instance.SecondaryPlayer.transform.position;
        GameManager.Instance.ClearSecondaryPlayer();
        GameManager.LastUsedCoopPlayerPrefab = (GameObject)BraveResources.Load(_ManualCoopPrefabOverride);
        GameObject gameObject = UnityEngine.Object.Instantiate(GameManager.LastUsedCoopPlayerPrefab, spawnPos, Quaternion.identity);
        gameObject.SetActive(true);
        PlayerController playerController = gameObject.GetComponent<PlayerController>();
        if (f && f.IsAlternateCostume)
          playerController.SwapToAlternateCostume();
        GameManager.Instance.SecondaryPlayer = playerController;
        playerController.PlayerIDX = 1;
        LootEngine.DoDefaultItemPoof(poofPos);
        return playerController;
      }
  }

  [HarmonyPatch(typeof(AmmonomiconController), nameof(AmmonomiconController.HandleOpenAmmonomicon), MethodType.Enumerator)]
  private class FastAmmonomiconPatch
  {
      [HarmonyILManipulator]
      private static void FastAmmonomiconIL(ILContext il, MethodBase original)
      {
          ILCursor cursor = new ILCursor(il);
          Type ot = original.DeclaringType; // get type of the IEnumerator itself
          if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchCall<AmmonomiconController>(nameof(AmmonomiconController.GetAnimationLength))))
              return;
          cursor.Emit(OpCodes.Ldarg_0); // load the IEnumerator instance
          cursor.Emit(OpCodes.Ldfld, AccessTools.GetDeclaredFields(ot).Find(f => f.Name == "isDeath")); // load actual isDeath field from the IEnumerator instance
          cursor.Emit(OpCodes.Call, typeof(FastAmmonomiconPatch).GetMethod(nameof(FastAmmonomiconPatch.AdjustAnimationSpeed), BindingFlags.Static | BindingFlags.NonPublic));
          return;
      }

      private static float AdjustAnimationSpeed(float oldValue, bool isDeath)
      {
          if (isDeath || _Gunfig.Disabled(FAST_POKEDEX))
            return oldValue;
          AkSoundEngine.StopAll(AmmonomiconController.Instance.gameObject);
          return 0f;
      }
  }

  [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.GetEquippedWith))]
  private class UnlimitedActiveItemsPatch
  {
      [HarmonyILManipulator]
      private static void UnlimitedActiveItemsIL(ILContext il)
      {
          ILCursor cursor = new ILCursor(il);
          if (!cursor.TryGotoNext(MoveType.Before, instr => instr.MatchStloc(6))) // V_6 == num3 == max active items we can carry
              return;

          cursor.Emit(OpCodes.Call, typeof(UnlimitedActiveItemsPatch).GetMethod(nameof(UnlimitedActiveItemsPatch.ActiveItemCapacity), BindingFlags.Static | BindingFlags.NonPublic));
      }

      private static int ActiveItemCapacity(int oldValue)
      {
        return _Gunfig.Enabled(ALL_THE_ITEMS) ? 9999 : oldValue;
      }
  }

  [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.UpdateInventoryMaxItems))]
  private class UpdateInventoryMaxItemsPatch
  {
      static bool Prefix(PlayerController __instance)
      {
          if (_Gunfig.Enabled(ALL_THE_ITEMS))
            return false; // skip the original method if we have unlimited item slots, since we never need to drop anything
          return true; // call the original method
      }
  }

  [HarmonyPatch(typeof(GameStatsManager), nameof(GameStatsManager.GetPlayerStatValue))]
  private class HaveInfiniteCreditsPatch
  {
      static bool Prefix(GameStatsManager __instance, TrackedStats stat, ref float __result)
      {
        if (stat != TrackedStats.META_CURRENCY || _Gunfig.Disabled(INFINITE_META))
          return true; // call the original method
        __result = 99999;
        return false; // skip the original method
      }
  }

  [HarmonyPatch(typeof(GameStatsManager), nameof(GameStatsManager.SetStat))]
  private class DontSetCreditsPatch
  {
      static bool Prefix(GameStatsManager __instance, TrackedStats stat, float value)
      {
          if (stat != TrackedStats.META_CURRENCY || _Gunfig.Disabled(INFINITE_META))
            return true;     // call the original method
          return false; // skip the original method
      }
  }

  [HarmonyPatch(typeof(GameStatsManager), nameof(GameStatsManager.RegisterStatChange))]
  private class DontRegisterCreditsPatch
  {
      static bool Prefix(GameStatsManager __instance, TrackedStats stat, float value)
      {
          if (stat != TrackedStats.META_CURRENCY || _Gunfig.Disabled(INFINITE_META))
            return true;     // call the original method
          return false; // skip the original method
      }
  }

  [HarmonyPatch(typeof(SimpleStatLabel), nameof(SimpleStatLabel.Update))]
  private class InfiniteCreditsOnHUDPatch
  {
      static bool Prefix(SimpleStatLabel __instance)
      {
        if (!__instance.m_label || !__instance.m_label.IsVisible || __instance.stat != TrackedStats.META_CURRENCY || _Gunfig.Disabled(INFINITE_META))
          return true; // call original method
        __instance.m_label.AutoHeight = true;
        __instance.m_label.ProcessMarkup = true;
        __instance.m_label.Text = "[sprite \"infinite-big\"]";
        return false; // skip origina method
      }
  }

  [HarmonyPatch(typeof(AmmonomiconDeathPageController), nameof(AmmonomiconDeathPageController.GetNumMetasToQuickRestart))]
  private class FreeQuickRestartPatch
  {
      static void Postfix(AmmonomiconDeathPageController __instance, QuickRestartOptions __result)
      {
        if (_Gunfig.Enabled(INFINITE_META))
          __result.NumMetas = 0;
      }
  }

  /// <summary>Prevent Cultist from disappearing from Breach when Breach co-op character select is enabled</summary>
  [HarmonyPatch(typeof(FoyerCharacterSelectFlag), nameof(FoyerCharacterSelectFlag.OnCoopChangedCallback))]
  private class FoyerOnCoopChangedCallbackPatch
  {
      static bool Prefix(FoyerCharacterSelectFlag __instance)
      {
          return _Gunfig.Disabled(COOP_FOYER_SEL); // call the original method iff we aren't allowing coop character selecting
      }
  }

  /* TODO:
      - fix scrolling not working until clicking somewhere on the page //NOTE: vanilla bug, keyboard takes control away from mouse
  */
  /// <summary>If the player is targeting an item, make opening the ammonomicon bring up the entry for that item.</summary>
  [HarmonyPatch]
  private class AmmonomiconControllerOpenAmmonomiconPatch
  {
    private static int _NextBookmark                      = 0;
    private static bool _DidAlexandriaScan                = false;
    private static Type _AlexandriaShopItemType           = null;
    private static FieldInfo _AlexandriaShopItemItemField = null;

    private static int AttemptToGetPickupIdFromAlexandriaShopItem(IPlayerInteractable targetInteractable, ref bool isGun)
    {
      if (!_DidAlexandriaScan)
      {
        _DidAlexandriaScan = true;
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
          if (!assembly.FullName.Contains("Alexandria"))
            continue;
          _AlexandriaShopItemType = assembly.GetType("Alexandria.NPCAPI.CustomShopItemController");
          if (_AlexandriaShopItemType == null)
            return -1;
          _AlexandriaShopItemItemField = _AlexandriaShopItemType.GetField("item", BindingFlags.Public | BindingFlags.Instance);
        }
      }
      if (_AlexandriaShopItemItemField == null)
        return -1; // Alexandria not found, so this can never be an Alexandria shop item
      if (!_AlexandriaShopItemType.IsInstanceOfType(targetInteractable))
        return -1; // Not a shop item
      object customItemPickup = _AlexandriaShopItemItemField.GetValue(targetInteractable);
      if (customItemPickup == null)
        return -1; // No associated pickup
      PickupObject pickup = (PickupObject)customItemPickup;
      isGun = pickup is Gun;
      return pickup.PickupObjectId;
    }

    [HarmonyPatch(typeof(AmmonomiconController), nameof(AmmonomiconController.OpenInternal))]
    [HarmonyILManipulator]
    private static void AmmonomiconControllerOpenInternalPatchIL(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);
        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<Material>("set_shader")))
          return;

        cursor.Emit(OpCodes.Ldarg_0); // AmmonomiconController instance
        cursor.Emit(OpCodes.Ldarg_1); // isDeath
        cursor.Emit(OpCodes.Ldarg_2); // isVictory
        cursor.Emit(OpCodes.Ldarga, 3); // ref targetTrackable
        cursor.CallPrivate(typeof(AmmonomiconControllerOpenAmmonomiconPatch), nameof(MaybeOpenToTargetedItem));
    }

    private static void MaybeOpenToTargetedItem(AmmonomiconController self, bool isDeath, bool isVictory, ref EncounterTrackable targetTrackable)
    {
        if (isDeath || isVictory || _Gunfig.Disabled(TARGET_POKEDEX))
          return;
        if (GameManager.Instance.PrimaryPlayer is not PlayerController player)
          return;
        if (player.m_lastInteractionTarget is not IPlayerInteractable targetInteractable)
          return;
        int targetPickupId = -1;
        bool isGun = false;

        if (targetInteractable is Gun gun)
        {
          isGun = true;
          targetPickupId = gun.PickupObjectId;
        }
        else if (targetInteractable is PlayerItem active)
          targetPickupId = active.PickupObjectId;
        else if (targetInteractable is PassiveItem passive)
          targetPickupId = passive.PickupObjectId;
        else if (targetInteractable is ShopItemController shopItem && shopItem.item is PickupObject shopPickup)
        {
          isGun = shopPickup is Gun;
          targetPickupId = shopPickup.PickupObjectId;
        }
        else
          targetPickupId = AttemptToGetPickupIdFromAlexandriaShopItem(targetInteractable, ref isGun);
        if (targetPickupId == -1)
          return;
        if (PickupObjectDatabase.GetById(targetPickupId) is not PickupObject pickup)
          return;
        if (pickup.gameObject.GetComponent<EncounterTrackable>() is not EncounterTrackable newTargetTrackable)
          return;
        if (newTargetTrackable.journalData != null && newTargetTrackable.journalData.SuppressInAmmonomicon)
          return;

        targetTrackable = newTargetTrackable;
        _NextBookmark = isGun ? 1 : 2;
        self.m_AmmonomiconInstance.CurrentlySelectedTabIndex = _NextBookmark;

        self.m_CurrentLeftPageManager.Disable();
        self.m_CurrentRightPageManager.Disable();

        self.m_CurrentLeftPageManager = self.LoadPageUIAtPath(self.m_AmmonomiconInstance.bookmarks[_NextBookmark].TargetNewPageLeft,
          isGun ? AmmonomiconPageRenderer.PageType.GUNS_LEFT : AmmonomiconPageRenderer.PageType.ITEMS_LEFT, false, false);
        self.m_CurrentRightPageManager = self.LoadPageUIAtPath(self.m_AmmonomiconInstance.bookmarks[_NextBookmark].TargetNewPageRight,
          isGun ? AmmonomiconPageRenderer.PageType.GUNS_RIGHT : AmmonomiconPageRenderer.PageType.ITEMS_RIGHT, false, false);
        self.m_CurrentLeftPageManager.ForceUpdateLanguageFonts();
        self.m_CurrentRightPageManager.ForceUpdateLanguageFonts();
    }

    [HarmonyPatch(typeof(AmmonomiconInstanceManager), nameof(AmmonomiconInstanceManager.Open))]
    [HarmonyPrefix]
    private static bool AmmonomiconInstanceManagerOpenPrefix(AmmonomiconInstanceManager __instance)
    {
      if (_NextBookmark == 0)
        return true;
      __instance.m_currentlySelectedBookmark = _NextBookmark;
      _NextBookmark = 0;
      __instance.StartCoroutine(__instance.HandleOpenAmmonomicon());
      return false;
    }
  }
}

// Scrapped for now:
// internal const string FREE_PARADOX    = "0-Credit Gunslinger / Paradox";
  // GetNumMetasToQuickRestart // the big one, need to modify this all over
  // CheckKeepModifiersQuickRestart
  // OnSelectedCharacterCallback -> prevent decrease on character select
  // CanBeSelected -> should always return true
  // SetGunGame blessed runs -> random guns

  // need to figure out how to disable initial creidts for challenge mode, boss rush, and blessed runs
