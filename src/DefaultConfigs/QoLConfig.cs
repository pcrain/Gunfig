namespace Gunfiguration;

public static class QoLConfig
{
  public static ModConfig Gunfig = null;

  public const string MENU_SOUNDS     = "Better Menu Sounds";
  public const string FINGER_SAVER    = "Auto-fire Semi-Automatic Weapons";
  public const string PLAYER_TWO_CHAR = "Co-op Character";
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

  public static void Init()
  {
    Gunfig = ModConfig.GetConfigForMod("Quality of Life");

    Gunfig.AddToggle(key: MENU_SOUNDS, updateType: ModConfig.Update.Immediate);

    Gunfig.AddToggle(key: FINGER_SAVER);

    List<string> players = new();
    foreach (string player in _PLAYER_MAP.Keys)
      players.Add((player == "Cultist") ? player : player.Yellow());
    Gunfig.AddScrollBox(key: PLAYER_TWO_CHAR, options: players);

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
    return orig(coop);
  }
}
