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

        float maxBackups;
        void Start()
        {
            instance = this;
            win = new Rect((Screen.width - WIDTH) / 2, (Screen.height - HEIGHT) / 2, WIDTH, HEIGHT);
            _windowId = WindowHelper.NextWindowId("KCO_Settings");
            maxBackups = HighLogic.CurrentGame.Parameters.CustomParams<KSPCO_Settings>().maxBackups;

        }
        void OnGUI()
        {
            if (KspNalCommon.GuiStyleOption.SKIN_STATES[KspNalCommon.GuiStyleOption.lastSelected] == KspNalCommon.GuiStyleOption.Ksp)
            {
                GUI.skin = KspNalCommon.PluginCommons.instance.kspSkin();
                win.width = 450;
                win.height = 330;
            }
            else
            {
                win.width = 500;
                win.height = 410;
            }

            win = ClickThruBlocker.GUILayoutWindow(_windowId, win, DoWin, "Settings");
        }

        void DoWin(int id)
        {
            if (Event.current.type == EventType.Repaint)
                GUI.BringWindowToFront(_windowId);
            using (new GUILayout.VerticalScope())
            {

                HighLogic.CurrentGame.Parameters.CustomParams<KSPCO_Settings>().debug =
                    GUILayout.Toggle(HighLogic.CurrentGame.Parameters.CustomParams<KSPCO_Settings>().debug, "Debug");

                HighLogic.CurrentGame.Parameters.CustomParams<KSPCO_Settings>().replaceEditorLoadButton =
                    GUILayout.Toggle(HighLogic.CurrentGame.Parameters.CustomParams<KSPCO_Settings>().replaceEditorLoadButton, "Replace editor load button (must exit editor to apply)");

                HighLogic.CurrentGame.Parameters.CustomParams<KSPCO_Settings>().showVersion =
                    GUILayout.Toggle(HighLogic.CurrentGame.Parameters.CustomParams<KSPCO_Settings>().showVersion, "Show KSP version for craft file");

                HighLogic.CurrentGame.Parameters.CustomParams<KSPCO_Settings>().saveBackupAfterChange =
                    GUILayout.Toggle(HighLogic.CurrentGame.Parameters.CustomParams<KSPCO_Settings>().saveBackupAfterChange, "Do backup after every change");

                HighLogic.CurrentGame.Parameters.CustomParams<KSPCO_Settings>().saveBackupAfterSave =
                    GUILayout.Toggle(HighLogic.CurrentGame.Parameters.CustomParams<KSPCO_Settings>().saveBackupAfterSave, "Do backup after every save ");


                HighLogic.CurrentGame.Parameters.CustomParams<KSPCO_Settings>().timedBackups =
                    GUILayout.Toggle(HighLogic.CurrentGame.Parameters.CustomParams<KSPCO_Settings>().timedBackups, "Make backup at regular timed intervals");

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Backup Interval (" + HighLogic.CurrentGame.Parameters.CustomParams<KSPCO_Settings>().backupInterval.ToString("F1") + " min): ");
                    HighLogic.CurrentGame.Parameters.CustomParams<KSPCO_Settings>().backupInterval =
                           GUILayout.HorizontalSlider((float)HighLogic.CurrentGame.Parameters.CustomParams<KSPCO_Settings>().backupInterval, 1f, 30f, GUILayout.Width(250));
                }
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Max Backups (" + HighLogic.CurrentGame.Parameters.CustomParams<KSPCO_Settings>().maxBackups + "): ");
                    maxBackups = GUILayout.HorizontalSlider(maxBackups, 1f, 1000f, GUILayout.Width(250));
                    HighLogic.CurrentGame.Parameters.CustomParams<KSPCO_Settings>().maxBackups = (int)maxBackups;
                }
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Note:  Changes here will take effect after exiting the editor");
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
