namespace Gunfiguration;

/// <summary>
/// Public portion of the Gunfig API. Basic usage:
///   1) Call <c>Gunfig.Get(modName)</c> to get a unique <paramref name="Gunfig"/> instance and configuration page for modName.
///   2) (Optional) Store the result of the above call to <c>Gunfig.Get()</c> in a static variable for use throughout your mod.
///   3) Add options to your configuration page using <c>AddToggle()</c>, <c>AddScrollBox()</c>, <c>AddButton()</c>, and <c>AddLabel()</c>.
///   4) Retrieve configured options anywhere in your code base using <c>Enabled()</c> or <c>Disabled()</c> for toggles and <c>Value()</c> for scrollboxes.
/// See the included <see cref="QoLConfig"/> class for usage examples.
/// </summary>
/// <remarks>
/// To use this API, make sure your Initialisation class registers the Gunfig BepIn dependency; i.e.:
///   [BepInDependency(Gunfiguration.C.MOD_GUID)]
/// </remarks>
public partial class Gunfig
{
  /// <summary>
  /// Gunfig.Update determines
  ///   1) when the new value for an option is actully set (i.e., when <c>Value()</c>, <c>Enabled()</c>, or <c>Disabled()</c> will return the new value),
  ///   2) when the callback (if any) associated with the option should be triggered, and
  ///   3) when the new value for the option should be written back to the configuration file.
  /// For all update types except <c>Immediate</c>, if the player backs out of the menu without confirming changes, none of the above events will occur.
  /// </summary>
  public enum Update {
    /// <summary>
    /// Immediately sets the option's new value, writes it to the gunfig file, and triggers any callbacks when the menu item is changed, without confirmation.
    /// </summary>
    Immediate,

    /// <summary>
    /// (Default) Sets the option's new value, writes it to the gunfig file, and triggers any callbacks when the options menu is closed with changes confirmed.
    /// Discards the change if the menu is closed without confirming changes.
    /// </summary>
    OnConfirm,

    /// <summary>
    /// Writes the new option's value to the gunfig file when the options menu is closed with changes confirmed.
    /// Does not set the option's value in memory or trigger any callbacks.
    /// Discards the change without writing to the gunfig file if the menu is closed without confirming changes.
    /// </summary>
    OnRestart,
  }

  /// <summary>Global event to be run once all mods dependent on MtG API are loaded in. Useful for dynamically populated scroll boxes.</summary>
  public static Action OnAllModsLoaded;

  /// <summary>
  /// Retrieves the unique Gunfig associated with the given <paramref name="modName"/>, creating it if it doesn't yet exist.
  /// </summary>
  /// <param name="modName">A name to uniquely identify your mod's configuration. The subpage in the MOD CONFIG menu will be set to this name. Can be colorized using <see cref="WithColor()"/>.</param>
  /// <returns>A unique <paramref name="Gunfig"/> associated with the given <paramref name="modName"/>. This can be safely stored in a variable and retrieved for later use.</returns>
  public static Gunfig Get(string modName)
  {
    string cleanModName = modName.ProcessColors(out Color _);
    for (int i = 0; i < _ActiveConfigs.Count; ++i)
    {
      Gunfig config = _ActiveConfigs[i];
      if (config._cleanModName != cleanModName)
        continue;
      if (_ConfigAssemblies[i] != Assembly.GetCallingAssembly().FullName)
        throw new AccessViolationException("Tried to access a Gunfig that doesn't belong to you!");
      return config;
    }

    GunfigDebug.Log($"Creating new Gunfig instance for {cleanModName}");
    Gunfig Gunfig     = new Gunfig();
    Gunfig._modName      = modName;  // need to keep colors intact here
    Gunfig._cleanModName = cleanModName;  // need to remove colors here
    Gunfig._configFile   = Path.Combine(SaveManager.SavePath, $"{cleanModName}.{GunfigMenu._GUNFIG_EXTENSION}");
    Gunfig.LoadFromDisk();
    _ActiveConfigs.Add(Gunfig);
    _ConfigAssemblies.Add(Assembly.GetCallingAssembly().FullName); // cache our assembly name to avoid illegal config accesses from other mods
    return Gunfig;
  }

  /// <summary>
  /// Appends a new togglable option to the current <paramref name="Gunfig"/>'s config page.
  /// </summary>
  /// <param name="key">The key for accessing the toggle's value through <c>GetBool()</c> and passed as the first parameter to the toggle's <paramref name="callback"/>. Must NOT be formatted.</param>
  /// <param name="enabled">Whether the toggle should be enabled by default if no prior configuration has been set.</param>
  /// <param name="label">The label displayed for the toggle on the config page. The toggle's <paramref name="key"/> will be displayed if no label is specified. Can be colorized using <see cref="WithColor()"/>.</param>
  /// <param name="callback">An optional Action to call when changes to the toggle are applied.
  /// The callback's first argument will be the toggle's <paramref name="key"/>.
  /// The callback's second argument will be the toggle's value ("1" if enabled, "0" if disabled).</param>
  /// <param name="updateType">Determines when changes to the option are applied. See <see cref="Gunfig.Update"/> documentation for descriptions of each option.</param>
  public void AddToggle(string key, bool enabled = false, string label = null, Action<string, string> callback = null, Gunfig.Update updateType = Gunfig.Update.OnConfirm)
  {
    RegisterOption(new Item(){
      _itemType   = ItemType.CheckBox,
      _updateType = updateType,
      _key        = key,
      _label      = label ?? key,
      _callback   = callback,
      _values     = enabled ? _CheckedBoxValues : _UncheckedBoxValues,
    });
  }

  /// <summary>
  /// Appends a new scrollbox option to the current <paramref name="Gunfig"/>'s config page.
  /// </summary>
  /// <param name="key">The key for accessing the scrollbox's value through <c>Value()</c> and passed as the first parameter to the scrollbox's <paramref name="callback"/>. Must NOT be formatted.</param>
  /// <param name="options">A list of strings determining the valid values for the scrollbox, displayed verbatim on the config page. Can be individually colorized using <see cref="WithColor()"/>.</param>
  /// <param name="label">The label displayed for the scrollbox on the config page. The scrollbox's <paramref name="key"/> will be displayed if no label is specified. Can be colorized using <see cref="WithColor()"/>.</param>
  /// <param name="callback">An optional Action to call when changes to the scrollbox are applied.
  /// The callback's first argument will be the scrollbox's <paramref name="key"/>.
  /// The callback's second argument will be the scrollbox's displayed value.</param>
  /// <param name="info">A list of strings determining informational text to be displayed alongside each value of the scrollbox. Must be null or match the length of <paramref name="options"/> exactly. Can be individually colorized using <see cref="WithColor()"/>.</param>
  /// <param name="updateType">Determines when changes to the option are applied. See <see cref="Gunfig.Update"/> documentation for descriptions of each option.</param>
  public void AddScrollBox(string key, List<string> options, string label = null, Action<string, string> callback = null, List<string> info = null, Gunfig.Update updateType = Gunfig.Update.OnConfirm)
  {
    RegisterOption(new Item(){
      _itemType   = ItemType.ArrowBox,
      _updateType = updateType,
      _key        = key,
      _label      = label ?? key,
      _callback   = callback,
      _values     = options,
      _info       = info,
    });
  }

  /// <summary>
  /// Appends a new option to the scrollbox corresponding to the given <paramref name="key"/> in the current <paramref name="Gunfig"/>'s config page. Updates do not take effect until the next time menus are rebuilt. Most useful when used in conjunction with the <see cref="OnAllModsLoaded"/> event.
  /// </summary>
  /// <param name="key">The key for accessing the scrollbox's value through <c>Value()</c> and passed as the first parameter to the scrollbox's <paramref name="callback"/>. Must NOT be formatted.</param>
  /// <param name="option">The new option value to add to the scrollbox. Can be colorized using <see cref="WithColor()"/>.</param>
  /// <param name="info">A description for the new option. Defaults to an empty string if not specified. Can be colorized using <see cref="WithColor()"/>. Ignored if the scrollbox doesn't contain info strings.</param>
  public void AddDynamicOptionToScrollBox(string key, string option, string info = null)
  {
    foreach (Item item in this._registeredOptions)
    {
      if (item._itemType != ItemType.ArrowBox)
        continue;
      if (item._key != key)
        continue;
      item._values.Add(option);
      if (item._info != null)
        item._info.Add(info ?? string.Empty);
      return;
    }
    ETGModConsole.Log($"WARNING: Tried to add dynamic option to non-existent scroll box {key}!");
  }

  /// <summary>
  /// Appends a new button to the current <paramref name="Gunfig"/>'s config page.
  /// </summary>
  /// <param name="key">A unique key associated with the button, passed as the first parameter to the scrollbox's <paramref name="callback"/>. Must NOT be formatted.</param>
  /// <param name="label">The label displayed for the button on the config page. The button's <paramref name="key"/> will be displayed if no label is specified. Can be colorized using <see cref="WithColor()"/>.</param>
  /// <param name="callback">An optional Action to call when the button is pressed.
  /// The callback's first argument will be the button's <paramref name="key"/>.
  /// The callback's second argument will always be "1", and is only set for compatibility with other option callbacks.</param>
  public void AddButton(string key, string label = null, Action<string, string> callback = null)
  {
    RegisterOption(new Item(){
      _itemType   = ItemType.Button,
      _updateType = Gunfig.Update.Immediate,
      _key        = key,
      _label      = label ?? key,
      _callback   = callback,
      _values     = _DefaultValues,
    });
  }

  /// <summary>
  /// Appends a new label to the current <paramref name="Gunfig"/>'s config page.
  /// </summary>
  /// <param name="label">The text displayed for the label on the config page. Can be colorized using <see cref="WithColor()"/>.</param>
  public void AddLabel(string label)
  {
    RegisterOption(new Item(){
      _itemType   = ItemType.Label,
      _updateType = Gunfig.Update.Immediate,
      _key        = $"{label} label",
      _label      = label,
      _values     = _DefaultValues,
    });
  }

  /// <summary>
  /// Retrieves the effective current value (i.e., not including changes awaiting menu confirmation) of the option with key <paramref name="key"/> for the current <paramref name="Gunfig"/>.
  /// </summary>
  /// <param name="string">The key for the option we're interested in.</param>
  /// <returns>The value of the option with key <paramref name="key"/>, or <c>null</c> if no such option exists.</returns>
  public string Value(string key)
  {
    return this._options.TryGetValue(key, out string val) ? val : null;
  }

  /// <summary>
  /// Convenience function to retrieve the effective current enabled-ness (i.e., not including changes awaiting menu confirmation) of the toggle option with key <paramref name="key"/> for the current <paramref name="Gunfig"/>.
  /// </summary>
  /// <param name="string">The key for the option we're interested in.</param>
  /// <returns><c>true</c> if the boolean option with key <paramref name="key"/> is enabled, <c>false</c> if the boolean option is disabled or if no such boolean option exists.</returns>
  /// <remarks>Will always return false for options that aren't toggles.</remarks>
  public bool Enabled(string key)
  {
    return this._options.TryGetValue(key, out string val) && (val == "1");
  }

  /// <summary>
  /// Convenience function to retrieve the effective current disabled-ness (i.e., not including changes awaiting menu confirmation) of the toggle option with key <paramref name="key"/> for the current <paramref name="Gunfig"/>.
  /// </summary>
  /// <param name="string">The key for the option we're interested in.</param>
  /// <returns><c>true</c> if the boolean option with key <paramref name="key"/> is disabled, <c>false</c> if the boolean option is enabled or if no such boolean option exists.</returns>
  /// <remarks>Will always return false for options that aren't toggles.</remarks>
  public bool Disabled(string key)
  {
    return this._options.TryGetValue(key, out string val) && (val == "0");
  }
}

/// <summary>
/// Helper functions for adding formatting to menu items. Formatting can be applied to any string OTHER THAN OPTION KEYS, which must not have formatting.
/// </summary>
public static partial class GunfigHelpers
{
  // Add color formatting to a string for use within option menus
  public static string WithColor(this string s, Color c) => $"{MARKUP_DELIM}{ColorUtility.ToHtmlStringRGB(c)}{s}";
  // Convenience extensions for basic colors
  public static string Red(this string s)                => s.WithColor(Color.red);
  public static string Green(this string s)              => s.WithColor(Color.green);
  public static string Blue(this string s)               => s.WithColor(Color.blue);
  public static string Yellow(this string s)             => s.WithColor(Color.yellow);
  public static string Cyan(this string s)               => s.WithColor(Color.cyan);
  public static string Magenta(this string s)            => s.WithColor(Color.magenta);
  public static string Gray(this string s)               => s.WithColor(Color.gray);
  public static string White(this string s)              => s.WithColor(Color.white);
}
