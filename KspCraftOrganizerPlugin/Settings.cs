using UnityEngine;
using SpaceTuxUtility;
using ClickThroughFix;
using static KspCraftOrganizer.RegisterToolbar;

namespace KspCraftOrganizer
{
    public class Settings : MonoBehaviour
    {
        internal static Settings instance = null;

        const int WIDTH = 300;
        const int HEIGHT = 150;
        Rect win;
        int _windowId;
        void Start()
        {
            instance = this;
            win = new Rect((Screen.width - WIDTH) / 2, (Screen.height - HEIGHT) / 2, WIDTH, HEIGHT);
            _windowId = WindowHelper.NextWindowId("KCO_Settings");

        }
        void OnGUI()
        {
            if (KspNalCommon.GuiStyleOption.SKIN_STATES[KspNalCommon.GuiStyleOption.lastSelected] == KspNalCommon.GuiStyleOption.Ksp)
            {
                GUI.skin = KspNalCommon.PluginCommons.instance.kspSkin();
                win.width = 275;
                win.height = 160;
            }
            else
            {
                win.width = 225;
                win.height = 125;                
            }

            win = ClickThruBlocker.GUILayoutWindow(_windowId, win, DoWin, "Settings");
        }

        void DoWin(int id)
        {
            if (Event.current.type == EventType.Repaint)
                GUI.BringWindowToFront(_windowId);
            using (new GUILayout.VerticalScope())
            {
                SettingsService.instance.getPluginSettings().debug =
                    GUILayout.Toggle(SettingsService.instance.getPluginSettings().debug, "Debug");
                SettingsService.instance.getPluginSettings().replace_editor_load_button =
                    GUILayout.Toggle(SettingsService.instance.getPluginSettings().replace_editor_load_button, "Replace editor load button (must exit editor to apply)");
                SettingsService.instance.getPluginSettings().showVersion = GUILayout.Toggle(SettingsService.instance.getPluginSettings().showVersion, "Show KSP version for craft file");

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();


                    if (GUILayout.Button("Close"))
                    {
                        SettingsService.instance.SavePluginSettings();
                        OnDestroy();
                    }
                    GUILayout.FlexibleSpace();
                }
            }
            GUI.DragWindow();
        }

        internal void OnDestroy()
        {
            instance = null;
            Destroy(this);
        }
    }
}
