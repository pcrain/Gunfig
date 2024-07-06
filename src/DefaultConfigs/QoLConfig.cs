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
  internal const string STATIC_CAMERA   = "Static Camera While Aiming";
  internal const string MENU_SOUNDS     = "Better Menu Sounds";
  internal const string HEROBRINE       = "Disable Herobrine";
  internal const string HEALTH_BARS     = "Show Enemy Health Bars";
  internal const string DAMAGE_NUMS     = "Show Damage Numbers";
  internal const string CHEATS_LABEL    = "Cheats / Debug Stuff";
  internal const string SPAWN_ITEMS     = "Spawn Items from Ammonomicon";

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
    _Gunfig.AddToggle(key: FINGER_SAVER);
    _Gunfig.AddToggle(key: STATIC_CAMERA);
    _Gunfig.AddToggle(key: HEALTH_BARS);
    _Gunfig.AddToggle(key: DAMAGE_NUMS);

    // Add a toggle that goes into effect immediately without awaiting confirmation from the player.
    _Gunfig.AddToggle(key: MENU_SOUNDS, updateType: Gunfig.Update.Immediate);

    // Add a colorized label
    _Gunfig.AddLabel(CHEATS_LABEL.Red());

    // Add a colorized toggle
    _Gunfig.AddToggle(key: SPAWN_ITEMS, label: SPAWN_ITEMS.Magenta());

    // Add a button with a custom callback when processed. Buttons always trigger their callbacks immediately when pressed.
    // Note that we have to explicitly specify a label to color the button text Red, as we cannot add colors to the key.
    _Gunfig.AddButton(key: HEROBRINE, label: HEROBRINE.Red(),
      callback: (optionKey, optionValue) => ETGModConsole.Log($"Clicked the {optionKey} button...but you can't disable Herobrine :/"));

    // Do some extra setup once all mods are loaded in
    Gunfig.OnAllModsLoaded += LateInit;

    // See the hooks and functions throughout the remainder of this file to see examples of how configuration options are used in practice.
    InitQoLHooks();
  }

  private static void LateInit()
  {
    // Dynamically update our PLAYER_TWO_CHAR scroll box with modded characters
    GunfigDebug.Log($"doing late init for QoL config");
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

  private static PlayerController OnGenerateCoopPlayer(Func<HutongGames.PlayMaker.Actions.ChangeCoopMode, PlayerController> orig, HutongGames.PlayMaker.Actions.ChangeCoopMode coop)
  {
    coop.PlayerPrefabPath = $"Player{_PLAYER_MAP[_Gunfig.Value(PLAYER_TWO_CHAR)]}";
    return orig(coop);
  }

  private static void OnMainMenuUpdate(Action<MainMenuFoyerController> orig, MainMenuFoyerController menu)
  {
    orig(menu);

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
    GameObject instantiatedCoopPlayer = UnityEngine.Object.Instantiate((GameObject)BraveResources.Load($"Player{_PLAYER_MAP[_Gunfig.Value(PLAYER_TWO_CHAR)]}"), Vector3.zero, Quaternion.identity);
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

  private static GameObject VFXHealthBar = null;
  private static readonly int ScouterId = 821;
  private static void DoHealthEffects(float damageAmount, bool fatal, HealthHaver target)
  {
    if (GameManager.Instance.PrimaryPlayer.HasPassiveItem(ScouterId))
      return;
    if (GameManager.Instance.CurrentGameType == GameManager.GameType.COOP_2_PLAYER && GameManager.Instance.SecondaryPlayer.HasPassiveItem(ScouterId))
      return;

    VFXHealthBar ??= (PickupObjectDatabase.GetById(ScouterId) as RatchetScouterItem).VFXHealthBar;

    Vector3 worldPosition = target.transform.position;
    float heightOffGround = 1f;

    if (target.GetComponent<SpeculativeRigidbody>() is SpeculativeRigidbody body)
    {
      worldPosition = body.UnitCenter.ToVector3ZisY();
      heightOffGround = worldPosition.y - body.UnitBottomCenter.y;
      if (_Gunfig.Enabled(HEALTH_BARS) && (bool)body.healthHaver && !body.healthHaver.HasHealthBar && !body.healthHaver.HasRatchetHealthBar && !body.healthHaver.IsBoss)
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

    if (_Gunfig.Enabled(DAMAGE_NUMS))
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
}

// Scrapped for now:
// internal const string FREE_PARADOX    = "0-Credit Gunslinger / Paradox";
  // GetNumMetasToQuickRestart // the big one, need to modify this all over
  // CheckKeepModifiersQuickRestart
  // OnSelectedCharacterCallback -> prevent decrease on character select
  // CanBeSelected -> should always return true
  // SetGunGame blessed runs -> random guns

  // need to figure out how to disable initial creidts for challenge mode, boss rush, and blessed runs
