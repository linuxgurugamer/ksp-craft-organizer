using KSP.UI.Screens;
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using SpaceTuxUtility;
using ClickThroughFix;
using static KspCraftOrganizer.RegisterToolbar;

namespace KspCraftOrganizer
{
    [KSPAddon(KSPAddon.Startup.EditorAny, true)]
    public class DirectorySelect : MonoBehaviour
    {
        internal static DirectorySelect instance = null;

        const int WIDTH = 300;
        const int HEIGHT = 150;
        Rect win;
        int _windowId;
        private bool disabled = true;

        private OrganizerWindow parent;

        Vector2 w;
        string selected = "";

        void Start()
        {
            instance = this;
            disabled = true;
            win = new Rect((Screen.width - WIDTH) / 2, (Screen.height - HEIGHT) / 2, WIDTH, HEIGHT);
            _windowId = WindowHelper.NextWindowId("KCO_DirectorySelect");
        }

        public void SetParent(OrganizerWindow p)
        {
            parent = p;
        }

        internal bool SetEnable
        {
            set
            {
                selected = "";
                disabled = !value;
            }
        }
        internal bool IsEnabled { get { return !disabled; } }

        bool displayError = false;

        void OnGUI()
        {
            if (disabled)
                return;
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

            if (!displayError)
                win = ClickThruBlocker.GUILayoutWindow(_windowId, win, DoWin, "Destination Directory");
            else
                win = ClickThruBlocker.GUILayoutWindow(_windowId, win, DoErrorWin, "Error");
        }


        string errorTitle;
        string errorMessage;
        void SetError(string title, string message)
        {
            errorTitle = title;
            errorMessage = message;
            displayError = true;
        }

        void DoErrorWin(int id)
        {
            using (new GUILayout.VerticalScope())
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(errorTitle);
                    GUILayout.FlexibleSpace();
                }
                GUILayout.Space(10);
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.TextArea(errorMessage);
                    GUILayout.FlexibleSpace();
                }
                GUILayout.Space(15);
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Dismiss"))
                        displayError = false;
                    GUILayout.FlexibleSpace();
                }
            }
        }
        void DoWin(int id)
        {
            if (Event.current.type == EventType.Repaint)
                GUI.BringWindowToFront(_windowId);
            using (new GUILayout.VerticalScope())
            {
                w = GUILayout.BeginScrollView(w);
#if false
                using (new GUILayout.HorizontalScope())
                {
                    if (DirectoryServices.curDir != "" && DrawEntry(".."))
                        selected = "..";
                }
#endif
                foreach (OrganizerCraftEntity craft in parent.model.filteredCrafts)
                {
                    if (craft.IsDir && (craft.DirName != ".." || DirectoryServices.curDir != ""))
                    {
                        if (DrawEntry(craft.DirName))
                            selected = craft.DirName;
                    }
                }
                GUILayout.EndScrollView();

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Close"))
                    {
                        parent.model.unselectAllMoveCrafts();
                        parent.model.ClearCraftList();
                        disabled = true;
                    }
                    GUILayout.FlexibleSpace();
                    GUI.enabled = (selected != "" && Settings.instance == null);
                    if (GUILayout.Button("Move Craft Files"))
                    {
                        var oce = parent.model.GetCraftSelectedForMove();
                        bool destExists = false;
                        foreach (OrganizerCraftEntity craft in oce)
                        {
                            craft.CraftFileInfo(out string dir, out string file);
                            string path = Path.GetFileNameWithoutExtension(file);
                            string curDir = Path.GetDirectoryName(file);

                            string destdir = Path.GetDirectoryName(file) + Path.DirectorySeparatorChar + selected;
                            destExists = MoveFile(path, "craft", curDir, destdir) |
                                         MoveFile(path, "crmgr", curDir, destdir) |
                                         MoveFile(path, "loadmeta", curDir, destdir);

                        }
                        if (!destExists)
                        {
                            foreach (OrganizerCraftEntity craft in oce)
                            {
                                craft.CraftFileInfo(out string dir, out string file);
                                string path = Path.GetFileNameWithoutExtension(file);
                                string curDir = Path.GetDirectoryName(file);
                                string destdir = Path.GetDirectoryName(file) + Path.DirectorySeparatorChar + selected;
                                destExists |= MoveFile(path, "craft", curDir, destdir, true) |
                                             MoveFile(path, "crmgr", curDir, destdir, true) |
                                             MoveFile(path, "loadmeta", curDir, destdir, true);

                            }
                            if (!destExists)
                            {
                                parent.model.unselectAllMoveCrafts();
                                parent.model.ClearCraftList();
                                disabled = true;
                            }

                        }
                        else
                        {
                            //ScreenMessages.PostScreenMessage("Craft file(s) already exists in destination", 10, ScreenMessageStyle.UPPER_CENTER);
                            SetError("Error Moving Craft File", "Craft file(s) already exists in destination");
                        }
                    }
                    GUILayout.FlexibleSpace();
                }
            }
            //GUI.DragWindow();
        }

        bool MoveFile(string path, string suffix, string curDir, string destDir, bool doit = false)
        {
            bool destExists = false;
            string curFilePath = curDir + Path.DirectorySeparatorChar + path + "." + suffix;
            if (File.Exists(curFilePath))
            {
                string destFilePath = destDir + Path.DirectorySeparatorChar + Path.GetFileName(path) + "." + suffix;
                destExists |= File.Exists(destFilePath);
                if (!doit || destExists)
                    return destExists;
                // Move the files here
                Log.Info("Moving file: " + curFilePath + " to " + destFilePath);
                try
                {
                    File.Move(curFilePath, destFilePath);
                }
                catch (Exception ex)
                {
                    //ScreenMessages.PostScreenMessage("Error occurred moving file: " + path, 10, ScreenMessageStyle.UPPER_CENTER);
                    //ScreenMessages.PostScreenMessage(ex.Message, 10, ScreenMessageStyle.UPPER_CENTER);
                    SetError("Unknown Error Moving Craft File", ex.Message);
                }
            }
            return false;
        }
        bool DrawEntry(string str)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUIStyle thisCraftButtonStyle = (selected == str) ? parent.toggleButtonStyleTrue : parent.toggleButtonStyleFalse;

                if (GUILayout.Button(str, thisCraftButtonStyle))
                {
                    return true;
                }
            }
            return false;
        }

        //internal void OnDestroy()
        //{
        //disabled = true;
        //instance = null;
        //Destroy(this);
        // }
    }
}
