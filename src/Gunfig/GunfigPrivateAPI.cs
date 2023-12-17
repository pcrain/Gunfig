namespace Gunfiguration;

// Internal portion of the Gunfig API. You should never need to use any of the functions in here directly.
public partial class Gunfig
{
  /*
     Eventual planned QoL improvements to make, from most to least important:
      - can't dynamically enable / disable options at runtime (must restart the game)
      - can't back out of one level of menus at a time (vanilla behavior; can maybe hook CloseAndMaybeApplyChangesWithPrompt?)
      - can't have first item of submenu be a label or it breaks focusing (vanilla ToggleToPanel() function assumes first control is selectable)

     Unimportant stuff I probably won't do:
      - modded config menu breaks when returning to the main menu from Breach (rare to run into, extremely hard to fix, and fixes itself starting a new run)
      - double menu sounds are played when navigating to new pages due to onfocus events for buttons playing sounds (tricky to fix and barely noticeable)
      - haven't implemented progress / fill bars (not particularly useful outside vanilla volume controls, so not in a hurry to implement this)
      - haven't implemented sprites for options (e.g. like vanilla crosshair selection) (very hard, requires modifying sprite atlas, and it is minimally useful)
  */

  private enum ItemType
  {
    Label,
    Button,
    CheckBox,
    ArrowBox,
  }

  private class Item
  {
    internal ItemType                _itemType   = ItemType.Label;
    internal Gunfig.Update           _updateType = Gunfig.Update.OnConfirm;
    internal string                  _key        = null;
    internal string                  _label      = null;
    internal List<string>            _values     = null;
    internal List<string>            _info       = null;
    internal Action<string, string>  _callback   = null;
  }

  internal static readonly List<string> _UncheckedBoxValues = new(){"0", "1"}; // options for default-unchecked checkboxes
  internal static readonly List<string> _CheckedBoxValues   = new(){"1", "0"}; // options for default-checked checkboxes
  internal static readonly List<string> _DefaultValues      = new(){"1"};      // dummy option for things like buttons

  internal static List<Gunfig> _ActiveConfigs    = new(); // list of all extant Gunfig instances
  internal static List<string> _ConfigAssemblies = new(); // list of assembly names associated with Gunfig instances, to avoid illegal accesses

  private Dictionary<string, string> _options = new(); // dictionary of mod options as key value pairs
  private List<Item> _registeredOptions       = new(); // list of options from which we can dynamically regenerate the options panel
  private bool _dirty                         = false; // whether we've been changed since last saving to disk
  private string _configFile                  = null;  // the file on disk to which we're writing
  internal string _modName                    = null;  // the name of our mod, including any formatting
  internal string _cleanModName               = null;  // the name of our mod, without formatting

  private Gunfig() { } // cannot construct Gunfig directly, must create / retrieve through GetConfigForMod()

  internal static void SaveActiveConfigsToDisk()
  {
    foreach (Gunfig config in _ActiveConfigs)
    {
      if (!config._dirty)
        continue;
      config.SaveToDisk();
      config._dirty = false;
    }
  }

  private void LoadFromDisk()
  {
    if (!File.Exists(this._configFile))
        return;
    try
    {
        string[] lines = File.ReadAllLines(this._configFile);
        foreach (string line in lines)
        {
          string[] tokens = line.Split('=');
          if (tokens.Length != 2 || tokens[0] == null || tokens[1] == null)
            continue;
          string key = tokens[0].Trim();
          string val = tokens[1].Trim();
          if (key.Length == 0 || val.Length == 0)
            continue;
          this._options[key] = val;
        }
    }
    catch (Exception e)
    {
      ETGModConsole.Log($"    error loading mod config file {this._configFile}: {e}");
    }
  }

  private void SaveToDisk()
  {
    try
    {
      using (StreamWriter file = File.CreateText(this._configFile))
      {
          foreach(string key in this._options.Keys)
          {
            if (string.IsNullOrEmpty(key))
              continue;
            file.WriteLine($"{key} = {this._options[key]}");
          }
      }
    }
    catch (Exception e)
    {
      ETGModConsole.Log($"    error saving mod config file {this._configFile}: {e}");
    }
  }

  internal dfScrollPanel RegenConfigPage()
  {
    dfScrollPanel subOptionsPanel = GunfigMenu.NewOptionsPanel($"{this._modName}");
    foreach (Item item in this._registeredOptions)
    {
      dfControl itemControl;
      switch (item._itemType)
      {
        default:
        case ItemType.Label:
          itemControl = subOptionsPanel.AddLabel(label: item._label);
          break;
        case ItemType.Button:
          itemControl = subOptionsPanel.AddButton(label: item._label);
          break;
        case ItemType.CheckBox:
          itemControl = subOptionsPanel.AddCheckBox(label: item._label);
          break;
        case ItemType.ArrowBox:
          itemControl = subOptionsPanel.AddArrowBox(label: item._label, options: item._values, info: item._info);
          break;
      }
      if (item._itemType != ItemType.Label) // pure labels don't need a GunfigOption and handle markup processing on site
        itemControl.gameObject.AddComponent<GunfigOption>().Setup(
          parentConfig: this, key: item._key, values: item._values, update: item._callback, updateType: item._updateType);
    }
    return subOptionsPanel;
  }

  // Set a config key to a value and return the value
  internal string Set(string key, string value)
  {
    if (string.IsNullOrEmpty(key))
      return null;
    this._options[key] = value;
    this._dirty = true;
    return value;
  }
}

// Private portion of GunfigHelpers
public static partial class GunfigHelpers
{
  internal const string MARKUP_DELIM = "@"; // "#" is used for localization strings, so we need something else

  // Helpers for processing colors on various dfControls
  internal static Color Dim(this Color c, bool dim) => Color.Lerp(dim ? Color.black : Color.white, c, 0.5f);
  internal static string ProcessColors(this string markupText, out Color color)
  {
    string processedText = markupText;
    color = Color.white;
    if (processedText.StartsWith(MARKUP_DELIM))
    {
      // convert "@" back to "#" for the purposes of color conversion
      if (ColorUtility.TryParseHtmlString($"#{processedText.Substring(1, 6)}", out color))
        processedText = processedText.Substring(7);
      else
        color = Color.white;
    }
    return processedText;
  }
}
