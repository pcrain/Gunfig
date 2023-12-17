namespace Gunfiguration;

/*
   Known Issues:
    - none :D
*/

public static class QoLConfig
{
  public static ModConfig Gunfig = null;

  public const string FINGER_SAVER    = "Auto-fire Semi-Automatic Weapons";
  public const string PLAYER_TWO_CHAR = "Co-op Character";
  public const string QUICKSTART      = "Quick Start Behavior";
  public const string STATIC_CAMERA   = "Static Camera While Aiming";
  public const string MENU_SOUNDS     = "Better Menu Sounds";
  public const string HEROBRINE       = "Disable Herobrine";

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

  public static void Init()
  {
    Gunfig = ModConfig.GetConfigForMod("Quality of Life");

    Gunfig.AddToggle(key: MENU_SOUNDS, updateType: ModConfig.Update.Immediate);

    Gunfig.AddToggle(key: FINGER_SAVER);

    List<string> players = new();
    foreach (string player in _PLAYER_MAP.Keys)
      players.Add((player == "Cultist") ? player : player.Yellow());
    Gunfig.AddScrollBox(key: PLAYER_TWO_CHAR, options: players);

    Gunfig.AddScrollBox(key: QUICKSTART, options: _QUICKSTART_OPTIONS, info: _QUICKSTART_DESCRIPTIONS);

    Gunfig.AddToggle(key: STATIC_CAMERA);

    Gunfig.AddToggle(key: HEROBRINE, label: HEROBRINE.Red());

    InitQoLHooks();
  }

  private static void InitQoLHooks()
  {
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
    new ILHook(
      typeof(PlayerController).GetMethod("HandleGunFiringInternal", BindingFlags.Instance | BindingFlags.NonPublic),
      HandleGunFiringInternalIL
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

  private static void OnToggleCheckbox(Action<BraveOptionsMenuItem, dfControl, dfMouseEventArgs> orig, BraveOptionsMenuItem item, dfControl control, dfMouseEventArgs args)
  {
    orig(item, control, args);
    if (Gunfig.Enabled(MENU_SOUNDS))
      AkSoundEngine.PostEvent("Play_UI_menu_select_01", item.gameObject);
  }

  private static void OnHandleFillbarValueChanged(Action<BraveOptionsMenuItem> orig, BraveOptionsMenuItem item)
  {
    orig(item);
    if (Gunfig.Enabled(MENU_SOUNDS))
      AkSoundEngine.PostEvent("Play_UI_menu_select_01", item.gameObject);
  }

  // ILHook references
  // https://github.com/ThadHouse/quicnet/blob/522289d7f3206574d672c936b3129eedf415e735/src/Interop/ApiGenerator.cs#L78
  // https://github.com/StrawberryJam2021/StrawberryJam2021/blob/21079f1c2521aa704fc5ddc91f67ff3ebc95c317/Entities/ToggleSwapBlock.cs#L32
  private static void HandleGunFiringInternalIL(ILContext il)
  {
      ILCursor cursor = new ILCursor(il);
      // cursor.DumpIL("HandlePlayerPhasingInputIL");

      while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchAdd(), instr => instr.MatchStfld<PlayerController>("m_controllerSemiAutoTimer")))
      {
          /* the next four instructions after this point are as follows
              [keep   ] IL_0272: ldarg.0
              [keep   ] IL_0273: ldfld System.Single PlayerController::m_controllerSemiAutoTimer
              [replace] IL_0278: call System.Single BraveInput::get_ControllerFakeSemiAutoCooldown()
              [keep   ] 637 ... ble.un ... MonoMod.Cil.ILLabel
          */
          cursor.Index += 2; // skip the next two instructions so we still have m_controllerSemiAutoTimer on the stack
          cursor.Remove(); // remove the get_ControllerFakeSemiAutoCooldown() instruction
          cursor.Emit(OpCodes.Ldarg_0); // load the player instance as arg0
          cursor.Emit(OpCodes.Call, typeof(QoLConfig).GetMethod("OverrideSemiAutoCooldown", BindingFlags.Static | BindingFlags.NonPublic)); // replace with our own custom hook
          break; // we only care about the first occurrence of this pattern in the function
      }
  }

  private static float OverrideSemiAutoCooldown(PlayerController pc)
  {
    if (Gunfig.Enabled(FINGER_SAVER))
        return 0f; // replace the value we're checking against with 0f to completely remove semi-automatic fake cooldown
    return BraveInput.ControllerFakeSemiAutoCooldown; // return the original value
  }

  private static PlayerController OnGenerateCoopPlayer(Func<HutongGames.PlayMaker.Actions.ChangeCoopMode, PlayerController> orig, HutongGames.PlayMaker.Actions.ChangeCoopMode coop)
  {
    coop.PlayerPrefabPath = $"Player{_PLAYER_MAP[Gunfig.Get(PLAYER_TWO_CHAR)]}";
    ETGModConsole.Log($"generating in OnGenerateCoopPlayer");
    return orig(coop);
  }

  private static void OnMainMenuUpdate(Action<MainMenuFoyerController> orig, MainMenuFoyerController menu)
  {
    orig(menu);

    if (!Foyer.DoIntroSequence && !Foyer.DoMainMenu)
      return; // disallow extended quickstarting if we're actively in the Breach

    if (Gunfig.Get(QUICKSTART) == "Vanilla")
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

    if (InControl.InputManager.Devices.Count > 0 && Gunfig.Get(QUICKSTART).Contains("Co-op"))
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
    GameObject instantiatedCoopPlayer = UnityEngine.Object.Instantiate((GameObject)BraveResources.Load($"Player{_PLAYER_MAP[Gunfig.Get(PLAYER_TWO_CHAR)]}"), Vector3.zero, Quaternion.identity);
    instantiatedCoopPlayer.SetActive(true);
    PlayerController extantCoopPlayer = instantiatedCoopPlayer.GetComponent<PlayerController>();
    extantCoopPlayer.PlayerIDX = 1;
    GameManager.Instance.SecondaryPlayer = extantCoopPlayer;
  }

  private static Vector2 OnGetCoreOffset(Func<CameraController, Vector2, bool, bool, Vector2> orig, CameraController cam, Vector2 currentBasePosition, bool isUpdate, bool allowAimOffset)
  {
    if (Gunfig.Enabled(STATIC_CAMERA))
      return Vector2.zero;
    return orig(cam, currentBasePosition, isUpdate, allowAimOffset);
  }
}

