namespace Gunfiguration;

// Internal class for managing individual menu items. You should never need to use any of the functions in here directly.
internal class GunfigOption : MonoBehaviour
{
  private static List<GunfigOption> _PendingUpdatesOnConfirm = new();

  private string _lookupKey                      = "";                        // key for looking up in our configuration file
  private string _defaultValue                   = "";                        // our default value if the key is not found in the configuration file
  private string _currentValue                   = "";                        // our current effective value, barring any pending changes
  private string _pendingValue                   = "";                        // our pending value after changes are applies
  private Gunfig.Update _updateType              = Gunfig.Update.OnConfirm;   // when to apply any pending changes
  private List<string> _validValues              = new();                     // valid values for the option (auto populated with "true" and "false" for toggles)
  private Action<string, string> _onApplyChanges = null;                      // event handler for execution
  private dfControl _control                     = null;                      // the dfControl to which we're attached
  private Gunfig _parent                         = null;                      // the Gunfig instance that's handling us
  private Color _labelColor                      = Color.white;
  private List<Color> _optionColors              = new();
  private List<Color> _infoColors                = new();

  [HarmonyPatch(typeof(FullOptionsMenuController), nameof(FullOptionsMenuController.CloseAndRevertChanges))]
  /// <summary>Resets Gunfig menu options when menus are closed without saving changes</summary>
  private class OnMenuCancelPatch
  {
      static void Prefix(FullOptionsMenuController __instance)
      {
        foreach (GunfigOption option in _PendingUpdatesOnConfirm)
          option.ResetMenuItemState();
        _PendingUpdatesOnConfirm.Clear();
      }
  }

  [HarmonyPatch(typeof(FullOptionsMenuController), nameof(FullOptionsMenuController.CloseAndApplyChanges))]
  /// <summary>Registers Gunfig changes after saving menu changes</summary>
  private class OnMenuConfirmPatch
  {
      static void Prefix(FullOptionsMenuController __instance)
      {
        foreach (GunfigOption option in _PendingUpdatesOnConfirm)
        {
          if (option._updateType == Gunfig.Update.OnConfirm)
            option.CommitPendingChanges();
          option._parent.Set(option._lookupKey, option._pendingValue);  // register change in the config handler even if the option's pending changes are deferred
        }
        Gunfig.SaveActiveConfigsToDisk();  // save all committed changes
        _PendingUpdatesOnConfirm.Clear();
      }
  }

  private static void OnGotFocus(Action<BraveOptionsMenuItem, dfControl, dfFocusEventArgs> orig, BraveOptionsMenuItem menuItem, dfControl control, dfFocusEventArgs args)
  {
    orig(menuItem, control, args);
    if (menuItem.GetComponent<GunfigOption>() is GunfigOption option)
      option.UpdateColors(menuItem, dim: false);
  }

  private static void OnSetUnselectedColors(Action<BraveOptionsMenuItem> orig, BraveOptionsMenuItem menuItem)
  {
    orig(menuItem);
    if (menuItem.GetComponent<GunfigOption>() is GunfigOption option)
      option.UpdateColors(menuItem, dim: true);
  }

  private void CommitPendingChanges()
  {
    if (this._pendingValue == this._currentValue)
      return;  // we didn't change, so we shouldn't do anything

    if (this._onApplyChanges != null)
      this._onApplyChanges(this._lookupKey, this._pendingValue);

    this._currentValue = this._pendingValue;  // set our current value to our pending value after applying changes in case we throw an exception and break the config

    if (this._updateType == Gunfig.Update.Immediate) // register and save immediate changes to disk TODO: maybe be more conservative with this?
    {
      this._parent.Set(this._lookupKey, this._pendingValue);
      Gunfig.SaveActiveConfigsToDisk();
    }
  }

  private void OnControlChanged(dfControl control, string stringValue)
  {
    this._pendingValue = stringValue;
    OnControlChanged();
  }

  private void OnControlChanged(dfControl control, bool toggleValue)
  {
    this._pendingValue = toggleValue ? "1" : "0";
    OnControlChanged();
  }

  private void OnButtonClicked(dfControl control)
  {
    if (this._onApplyChanges != null)
      this._onApplyChanges(this._lookupKey, this._pendingValue);
  }

  private void OnControlChanged()
  {
    if (this._updateType == Gunfig.Update.Immediate)
      CommitPendingChanges();
    else if (!_PendingUpdatesOnConfirm.Contains(this))
      _PendingUpdatesOnConfirm.Add(this);
    UpdateColors(base.GetComponent<BraveOptionsMenuItem>(), dim: false);  // we can probably safely assume we have focus
  }

  private void ProcessColors()
  {
    // Make sure we have a menu item
    BraveOptionsMenuItem menuItem = base.GetComponent<BraveOptionsMenuItem>();
    if (!menuItem)
    {
      ETGModConsole.Log($"  NULL BRAVE MENU ITEM");
      return;
    }

    // Set up color info from individual label texts
    if (menuItem.labelControl is dfLabel label)
      label.Text = label.Text.ProcessColors(out this._labelColor);
    if (menuItem.buttonControl is dfButton button)
      button.Text = button.Text.ProcessColors(out this._labelColor);
    if ((menuItem.selectedLabelControl is dfLabel settingLabel) && menuItem.labelOptions != null)
    {
      Color c;
      bool hasInfo = ((menuItem.infoControl != null) && (menuItem.infoOptions != null) && (menuItem.labelOptions.Length == menuItem.infoOptions.Length));
      for (int i = 0; i < menuItem.labelOptions.Length; ++i)
      {
        menuItem.labelOptions[i] = menuItem.labelOptions[i].ProcessColors(out c);
        this._optionColors.Add(c);
        if (hasInfo)
        {
          menuItem.infoOptions[i] = menuItem.infoOptions[i].ProcessColors(out c);
          this._infoColors.Add(c);
        }
      }
    }

    this._control.IsVisibleChanged += (_,_) => UpdateColors(menuItem, true);
  }

  private void ResetMenuItemState(bool addHandlers = false)
  {
    // Make sure we have a menu item
    BraveOptionsMenuItem menuItem = base.GetComponent<BraveOptionsMenuItem>();
    if (!menuItem)
    {
      ETGModConsole.Log($"  NULL BRAVE MENU ITEM");
      return;
    }

    // Set up the state of our menu item from our config
    if (menuItem.buttonControl is dfButton button)
    {
      if (addHandlers)
        this._control.gameObject.GetOrAddComponent<GunfigMenu.CustomButtonHandler>().onClicked += OnButtonClicked;
    }
    if (menuItem.checkboxChecked is dfControl checkBox)
    {
      bool isChecked = (this._currentValue.Trim() == "1");
      checkBox.IsVisible = isChecked;
      if (menuItem.checkboxUnchecked is dfControl checkBoxUnchecked)
        checkBoxUnchecked.IsVisible = true;
      menuItem.m_selectedIndex = isChecked ? 1 : 0;
      if (addHandlers)
        this._control.gameObject.GetOrAddComponent<GunfigMenu.CustomCheckboxHandler>().onChanged += OnControlChanged;
    }
    if ((menuItem.selectedLabelControl is dfLabel settingLabel) && menuItem.labelOptions != null)
    {
      bool hasInfo = ((menuItem.infoControl != null) && (menuItem.infoOptions != null) && (menuItem.labelOptions.Length == menuItem.infoOptions.Length));
      for (int i = 0; i < menuItem.labelOptions.Length; ++i)
      {
        if (menuItem.labelOptions[i] != this._currentValue)
          continue;
        settingLabel.Text = menuItem.labelOptions[i];
        if (hasInfo)
          menuItem.infoControl.Text = menuItem.infoOptions[i];
        menuItem.m_selectedIndex = i;
        break;
      }
      if (addHandlers)
        this._control.gameObject.GetOrAddComponent<GunfigMenu.CustomLeftRightArrowHandler>().onChanged += OnControlChanged;
    }

    UpdateColors(menuItem, dim: true);
  }

  internal void UpdateColors(BraveOptionsMenuItem menuItem, bool dim)
  {
    if (menuItem.labelControl != null)
      menuItem.labelControl.Color = this._labelColor.Dim(dim);
    if (menuItem.buttonControl != null)
      menuItem.buttonControl.TextColor = this._labelColor.Dim(dim);
    if (menuItem.selectedLabelControl != null)
      menuItem.selectedLabelControl.Color = this._optionColors[menuItem.m_selectedIndex % this._optionColors.Count].Dim(dim);
    if (menuItem.infoControl != null)
    {
      menuItem.infoControl.Text = menuItem.infoOptions[menuItem.m_selectedIndex % this._infoColors.Count];
      menuItem.infoControl.Color = this._infoColors[menuItem.m_selectedIndex % this._infoColors.Count].Dim(dim);
    }
  }

  internal static bool HasPendingChanges()
  {
    foreach (GunfigOption option in _PendingUpdatesOnConfirm)
      if (option._pendingValue != option._currentValue)
        return true;
    return false;
  }

  internal void Setup(Gunfig parentConfig, string key, List<string> values, Action<string, string> update, Gunfig.Update updateType = Gunfig.Update.OnConfirm)
  {
    this._control        = base.GetComponent<dfControl>();
    this._parent         = parentConfig;
    this._lookupKey      = key;
    this._validValues    = values;
    this._onApplyChanges = update;
    this._updateType     = updateType;

    // Load our default and current values from our config, or from the options passed to us
    if (!string.IsNullOrEmpty(key))  // null or empty key == pseudo-option used for its callback only
    {
      this._defaultValue = values[0].ProcessColors(out Color _);
      this._currentValue = this._parent.Value(this._lookupKey) ?? this._parent.Set(this._lookupKey, this._defaultValue);
      this._pendingValue = this._currentValue;
    }

    ProcessColors();
    ResetMenuItemState(addHandlers: true);
  }
}
