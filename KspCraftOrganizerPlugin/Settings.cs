using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SpaceTuxUtility;

namespace KspCraftOrganizer
{
    public class Settings : MonoBehaviour
    {
        internal static Settings instance = null;

        const int WIDTH = 300;
        const int HEIGHT = 100;
        Rect win;
        int _windowId;
        void Start()
        {
            instance = this;
            win = new Rect((Screen.width - WIDTH) / 2, (Screen.height - HEIGHT) / 2, WIDTH, HEIGHT);
            _windowId = WindowHelper.NextWindowId("KspCraftOrganizerSettings");
        }
        void OnGUI()
        {
            if (KspNalCommon.GuiStyleOption.SKIN_STATES[KspNalCommon.GuiStyleOption.lastSelected] == KspNalCommon.GuiStyleOption.Ksp)
            {
                GUI.skin = KspNalCommon.PluginCommons.instance.kspSkin();
                win.width = 250;
                win.height = 100;
            }
            else
            {
                win.width = 200;
                win.height = 100;
                GUI.skin = GUI.skin;
            }


            win = GUILayout.Window(12345, win, DoWin, "Settings");
        }

        void DoWin(int id)
        {
            GUILayout.BeginVertical();
            SettingsService.instance.getPluginSettings().debug =
                GUILayout.Toggle(SettingsService.instance.getPluginSettings().debug, "Debug");
            SettingsService.instance.getPluginSettings().replace_editor_load_button =
                GUILayout.Toggle(SettingsService.instance.getPluginSettings().replace_editor_load_button, "Replace editor load button");
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();


            if (GUILayout.Button("Close"))
            {
                SettingsService.instance.SavePluginSettings();
                OnDestroy();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        internal void OnDestroy()
        {
            instance = null;
            Destroy(this);
        }
    }
}
