namespace Gunfiguration;

public static class QoLConfig
{
  public static ModConfig Gunfig = null;

  public const string MENU_SOUNDS     = "Better Menu Sounds";
  public const string FINGER_SAVER    = "Auto-fire Semi-Automatic Weapons";
  public const string PLAYER_TWO_CHAR = "Co-op Character";
  public const string QUICKSTART      = "Quick Start Behavior";
  public const string AIM_CAMERA      = "Static Camera While Aiming";
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
    "Main Menu".Yellow(),
    "Co-op".Yellow(),
    "Main Menu + Co-op".Yellow(),
  };

  private static readonly List<string> _QUICKSTART_DESCRIPTIONS = new() {
    "Vanilla quickstart behavior".Green(),
    "Allows quickstarting on the main menu\nafter the title sequence".Green(),
    "Quick start will automatically start co-op\nif a second controller is plugged in".Green(),
    "Combines Main Menu and Co-op options".Green(),
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

    Gunfig.AddToggle(key: AIM_CAMERA);

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

    // Coop cultist preload fixer
    new Hook(
      typeof(GameManager).GetMethod("get_CoopPlayerPrefabForNewGame", BindingFlags.Static | BindingFlags.Public),
      typeof(QoLConfig).GetMethod("OnGetCoopPlayerPrefabForNewGame", BindingFlags.Static | BindingFlags.NonPublic)
      );
    new Hook(
      typeof(Dungeonator.Dungeon).GetMethod("GeneratePlayerIfNecessary", BindingFlags.Instance | BindingFlags.NonPublic),
      typeof(QoLConfig).GetMethod("OnGeneratePlayerIfNecessary", BindingFlags.Static | BindingFlags.NonPublic)
      );

  }

  private static void OnGeneratePlayerIfNecessary(Action<Dungeonator.Dungeon, MidGameSaveData> orig, Dungeonator.Dungeon dungeon, MidGameSaveData midgameSave)
  {
    if (midgameSave == null)
    {
      ETGModConsole.Log($"currently have {GameManager.Instance.AllPlayers.Length} players");
      dungeon.ForceRegenerationOfCharacters = false;
    }
    orig(dungeon, midgameSave);
  }

  private static GameObject OnGetCoopPlayerPrefabForNewGame(Func<GameObject> orig)
  {
    // Debug.Log($"hunh o.o");
    ETGModConsole.Log($"generating in OnGetCoopPlayerPrefabForNewGame");
    // ETGModConsole.Log($"valid 2nd player? {GameManager.Instance.SecondaryPlayer != null}");
    // if (GameManager.Instance.SecondaryPlayer != null)
    // {
    //   UnityEngine.Object.Destroy(GameManager.Instance.SecondaryPlayer);
    //   GameManager.Instance.SecondaryPlayer = null;
    // }
    // return (GameObject)BraveResources.Load($"PlayerCoopCultist");
    // return (GameObject)BraveResources.Load($"Player{_PLAYER_MAP[Gunfig.Get(PLAYER_TWO_CHAR)]}");
    return orig();
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

  internal class DummyMonoBehaviour : MonoBehaviour {}

  private static PlayerController OnGenerateCoopPlayer(Func<HutongGames.PlayMaker.Actions.ChangeCoopMode, PlayerController> orig, HutongGames.PlayMaker.Actions.ChangeCoopMode coop)
  {
    coop.PlayerPrefabPath = $"Player{_PLAYER_MAP[Gunfig.Get(PLAYER_TWO_CHAR)]}";
    // ETGModConsole.Log($"GENERATING COOP PLAYER {coop.PlayerPrefabPath}");
    ETGModConsole.Log($"generating in OnGenerateCoopPlayer");
    coop.Owner ??= new GameObject();
    coop.Fsm ??= new HutongGames.PlayMaker.Fsm();
    coop.Fsm.Owner ??= coop.Owner.AddComponent<DummyMonoBehaviour>();
    return orig(coop);
  }

  private static void OnMainMenuUpdate(Action<MainMenuFoyerController> orig, MainMenuFoyerController menu)
  {
    orig(menu);
    if (!(BraveInput.PlayerlessInstance?.ActiveActions?.Device?.Action4?.WasPressed ?? false) && !Input.GetKeyDown(KeyCode.Q))
      return;

    if (!Gunfig.Get(QUICKSTART).Contains("Menu"))  // catches both "Main Menu" and "Main Menu + Co-op"
      return;

    FinalIntroSequenceManager introManager = Foyer.Instance?.IntroDoer;
    if (introManager?.gameObject == null || introManager.m_isDoingQuickStart || !introManager.QuickStartAvailable())
      return;

    // logic yoinked from Foyer.Start() and FinalIntroSequenceManager.HandleBackgroundSkipChecks()
    menu.DisableMainMenu();
    UnityEngine.Object.FindObjectOfType<TitleDioramaController>()?.ForceHideFadeQuad();
    introManager.gameObject.transform.parent?.gameObject?.SetActive(true);
    introManager.m_skipCycle = true;
    introManager.m_isDoingQuickStart = true;

    ETGModConsole.Log($"doing quickstart");
    // SetupCoopQuickStart();
    GameManager.Instance.StartCoroutine(DoCoopQuickStart());
    // GameManager.Instance.StartCoroutine(CoopQuickStartCR(introManager));
    // introManager.StartCoroutine(introManager.DoQuickStart());
    // introManager.StartCoroutine(SpecialQuickStart(introManager));
  }

  private static IEnumerator DoCoopQuickStart()
  {
    // QuickStartObject.SetActive(false);
    // GameManager.Instance.StartCoroutine(FadeToBlack(0.1f, true, true));
    GameManager.PreventGameManagerExistence = false;
    GameManager.SKIP_FOYER = true;
    Foyer.DoMainMenu = false;
    // if (!m_inFoyer)
    {
      AkSoundEngine.LoadBank("SFX.bnk", -1, out uint out_bankID);
      GameManager.EnsureExistence();
    }
    AkSoundEngine.PostEvent("Play_UI_menu_confirm_01", GameManager.Instance.gameObject);
    // MidGameSaveData saveToContinue = null;
    // if (GameManager.VerifyAndLoadMidgameSave(out saveToContinue))
    // {
    //   yield return null;
    //   Dungeon.ShouldAttemptToLoadFromMidgameSave = true;
    //   GameManager.Instance.SetNextLevelIndex(GameManager.Instance.GetTargetLevelIndexFromSavedTileset(saveToContinue.levelSaved));
    //   GameManager.Instance.GeneratePlayersFromMidGameSave(saveToContinue);
    //   if (!m_inFoyer)
    //   {
    //     GameManager.Instance.FlushAudio();
    //   }
    //   GameManager.Instance.IsFoyer = false;
    //   Foyer.DoIntroSequence = false;
    //   Foyer.DoMainMenu = false;
    //   GameManager.Instance.IsSelectingCharacter = false;
    //   GameManager.Instance.DelayedLoadMidgameSave(0.25f, saveToContinue);
    //   yield break;
    // }
    GameManager.PlayerPrefabForNewGame = (GameObject)BraveResources.Load(CharacterSelectController.GetCharacterPathFromQuickStart());
    GameManager.Instance.GlobalInjectionData.PreprocessRun();
    yield return null;

    GameManager.Instance.CurrentGameType = GameManager.GameType.COOP_2_PLAYER;

    PlayerController playerController = GameManager.PlayerPrefabForNewGame.GetComponent<PlayerController>();
    GameStatsManager.Instance.BeginNewSession(playerController);
    GameObject instantiatedPlayer = UnityEngine.Object.Instantiate(GameManager.PlayerPrefabForNewGame, Vector3.zero, Quaternion.identity);
    GameManager.PlayerPrefabForNewGame = null;
    instantiatedPlayer.SetActive(true);
    PlayerController extantPlayer = instantiatedPlayer.GetComponent<PlayerController>();
    extantPlayer.PlayerIDX = 0;
    GameManager.Instance.PrimaryPlayer = extantPlayer;
    yield return null;

    GameObject instantiatedCoopPlayer = UnityEngine.Object.Instantiate((GameObject)BraveResources.Load($"Player{_PLAYER_MAP[Gunfig.Get(PLAYER_TWO_CHAR)]}"), Vector3.zero, Quaternion.identity);
    instantiatedCoopPlayer.SetActive(true);
    PlayerController extantCoopPlayer = instantiatedCoopPlayer.GetComponent<PlayerController>();
    extantCoopPlayer.PlayerIDX = 1;
    GameManager.Instance.SecondaryPlayer = extantCoopPlayer;
    yield return null;

    // if (!m_inFoyer)
    {
      GameManager.Instance.FlushAudio();
    }
    GameManager.Instance.FlushMusicAudio();
    GameManager.Instance.SetNextLevelIndex(1);
    GameManager.Instance.IsSelectingCharacter = false;
    GameManager.Instance.IsFoyer = false;
    GameManager.Instance.DelayedLoadNextLevel(0.5f);
    yield return null;
    yield return null;
    yield return null;
    Foyer.Instance.OnDepartedFoyer();
  }

  private static IEnumerator CoopQuickStartCR(FinalIntroSequenceManager introManager)
  {
    // GameManager.Instance.CurrentGameType = GameManager.GameType.COOP_2_PLAYER;
    yield return GameManager.Instance.StartCoroutine(HandleCharacterChangeNoFoyer());
    Foyer.Instance.OnDepartedFoyer();
    // GameManager.Instance.DelayedLoadNextLevel(0.05f);
    introManager.StartCoroutine(introManager.DoQuickStart());
  }

  // private static void SetupCoopQuickStart()
  // {
  //   GameManager.Instance.CurrentGameType = GameManager.GameType.COOP_2_PLAYER;
  //   if (GameManager.Instance.SecondaryPlayer != null)
  //   {
  //     ETGModConsole.Log($"secondary player already set up");
  //     return;
  //   }
  //     // UnityEngine.Object.DestroyImmediate(GameManager.Instance.SecondaryPlayer);
  //   GameManager.Instance.ClearSecondaryPlayer();
  //   GameManager.LastUsedCoopPlayerPrefab = (GameObject)BraveResources.Load($"Player{_PLAYER_MAP[Gunfig.Get(PLAYER_TWO_CHAR)]}");
  //   // GameManager.CoopPlayerPrefabForNewGame = GameManager.LastUsedCoopPlayerPrefab;
  //   // GameManager.Instance.RefreshAllPlayers();

  //   PlayerController playerController = UnityEngine.Object.Instantiate(GameManager.LastUsedCoopPlayerPrefab, Vector3.zero, Quaternion.identity).GetComponent<PlayerController>();
  //   playerController.gameObject.SetActive(true);
  //   playerController.PlayerIDX = 1;
  //   playerController.ActorName = "Player ID 1";

  //   GameManager.Instance.SecondaryPlayer = playerController;

  //   Foyer.Instance.OnCoopModeChanged();
  // }

  private static IEnumerator HandleCharacterChangeNoFoyer()
  {
    // InControl.InputDevice lastActiveDevice = GameManager.Instance.LastUsedInputDeviceForConversation ?? BraveInput.GetInstanceForPlayer(0).ActiveActions.Device;

    GameManager.Instance.CurrentGameType = GameManager.GameType.COOP_2_PLAYER;

    for (int i = 1; i < 2; ++i)
    {
      PlayerController newPlayer = GeneratePlayer(i == 1);
      yield return null;
      PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(newPlayer.specRigidbody);
    }

    GameManager.LastUsedCoopPlayerPrefab = null;
    GameManager.Instance.PrimaryPlayer?.ReinitializeMovementRestrictors();
    GameManager.Instance.MainCameraController.ClearPlayerCache();
    GameManager.Instance.RefreshAllPlayers();
    GameUIRoot.Instance.ConvertCoreUIToCoopMode();
    ETGModConsole.Log($"currently have {GameManager.Instance.AllPlayers.Length} players");
    ETGModConsole.Log($"  have primary player? {GameManager.Instance.PrimaryPlayer != null}");
    ETGModConsole.Log($"  have second player? {GameManager.Instance.SecondaryPlayer != null}");
    GameManager.Instance.m_players = new[]{
      GameManager.Instance.PrimaryPlayer,
      GameManager.Instance.SecondaryPlayer,
    };
    ETGModConsole.Log($"currently have {GameManager.Instance.AllPlayers.Length} players");
    // Foyer.Instance.ProcessPlayerEnteredFoyer(newPlayer);

    // BraveInput.ReassignAllControllers(lastActiveDevice);
    // if (Foyer.Instance.OnCoopModeChanged != null)
    //   Foyer.Instance.OnCoopModeChanged();

    yield break;
  }

  private static PlayerController GeneratePlayer(bool two)
  {
    if (two)
    {
      GameManager.Instance.ClearSecondaryPlayer();
      ETGModConsole.Log($"generating in GeneratePlayer");
      GameManager.LastUsedCoopPlayerPrefab = (GameObject)BraveResources.Load($"Player{_PLAYER_MAP[Gunfig.Get(PLAYER_TWO_CHAR)]}");
    }
    else
    {
      GameManager.Instance.ClearPrimaryPlayer();
      GameManager.LastUsedCoopPlayerPrefab = (GameObject)BraveResources.Load(CharacterSelectController.GetCharacterPathFromQuickStart());
    }

    GameObject gameObject = UnityEngine.Object.Instantiate(GameManager.LastUsedCoopPlayerPrefab, Vector3.zero, Quaternion.identity);
    gameObject.SetActive(true);
    PlayerController playerController = gameObject.GetComponent<PlayerController>();
    if (two)
    {
      if (GameManager.Instance.SecondaryPlayer != null)
        ETGModConsole.Log($"existing player {GameManager.Instance.SecondaryPlayer.name}");
      GameManager.Instance.SecondaryPlayer = playerController;
    }
    else
      GameManager.Instance.PrimaryPlayer = playerController;
    playerController.PlayerIDX = two ? 1 : 0;
    return playerController;
  }

  // private static IEnumerator SpecialQuickStart(FinalIntroSequenceManager introManager)
  // {
  //   ETGModConsole.Log($"creating player 2");
  //   // yield return introManager.StartCoroutine(new HutongGames.PlayMaker.Actions.ChangeCoopMode().HandleCharacterChange());

  //   FoyerCharacterSelectFlag[] foyerCharacters = UnityEngine.Object.FindObjectsOfType<FoyerCharacterSelectFlag>();
  //   foreach (FoyerCharacterSelectFlag foyerChar in foyerCharacters)
  //   {
  //     if (!foyerChar.IsCoopCharacter)
  //       continue;

  //     TalkDoerLite talkdoer = foyerChar.gameObject.GetComponent<TalkDoerLite>();
  //     if (talkdoer == null)
  //       break;

  //     ETGModConsole.Log($"found coop talkdoer");

  //     HutongGames.PlayMaker.Actions.ChangeCoopMode coopAction = null;
  //     for (int i = 0; i < talkdoer.playmakerFsms.Length; i++)
  //     {
  //       PlayMakerFSM playMakerFSM = talkdoer.playmakerFsms[i];
  //       if (playMakerFSM?.Fsm == null)
  //         continue;
  //       for (int j = 0; j < playMakerFSM.Fsm.States.Length; j++)
  //       {
  //         HutongGames.PlayMaker.FsmState fsmState = playMakerFSM.Fsm.States[j];
  //         for (int k = 0; k < fsmState.Actions.Length; k++)
  //         {
  //           HutongGames.PlayMaker.FsmStateAction fsmStateAction = fsmState.Actions[k];
  //           if (fsmStateAction is HutongGames.PlayMaker.Actions.ChangeCoopMode theCoopAction)
  //             coopAction = theCoopAction;
  //         }
  //       }
  //     }

  //     ETGModConsole.Log($"attempting to set up character 2");
  //     Foyer.Instance.SetUpCharacterCallbacks();
  //     GameManager.Instance.LastUsedInputDeviceForConversation = BraveInput.GetInstanceForPlayer(0).ActiveActions.Device;
  //     if (GameManager.Instance.LastUsedInputDeviceForConversation == null)
  //     {
  //       ETGModConsole.Log($"failed to get input device");
  //       yield break;
  //     }
  //     yield return talkdoer.StartCoroutine(coopAction.HandleCharacterChange());
  //     ETGModConsole.Log($"did it");
  //     // ETGModConsole.Log($"found foyer character {flag.CharacterPrefabPath}");
  //   }

  //   ETGModConsole.Log($"doing quick start");
  //   yield return introManager.StartCoroutine(introManager.DoQuickStart());
  //   ETGModConsole.Log($"done");
  // }
}

