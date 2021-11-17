using UnityEngine;
using System.Collections.Generic;
using KSP.UI.Screens;
using KspNalCommon;
using System;
using KSP.UI;
using ToolbarControl_NS;
using SpaceTuxUtility;

using static KspCraftOrganizer.RegisterToolbar;

namespace KspCraftOrganizer
{
    public class KspCraftOrganizerProperties : CommonPluginProperties
    {
        public bool canGetIsDebug()
        {
            return SettingsService.instance != null;
        }

        public int getInitialWindowId()
        {
            return WindowHelper.NextWindowId("KCO_Properties");
        }

        public string getPluginDirectory()
        {
            return FileLocationService.instance.getThisPluginDirectory();
        }

        public string getPluginLogName()
        {
            return "CraftOrganizer " + KspCraftOrganizerVersion.Version;
        }

        public bool isDebug()
        {
            return SettingsService.instance.getPluginSettings().debug;
        }

        public bool replaceEditorLoadButton()
        {
            return SettingsService.instance.getPluginSettings().replace_editor_load_button;
        }
        public GUISkin kspSkin()
        {
            return IKspAlProvider.instance.kspSkin();
        }
    }

    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class KspCraftOrganizerMain : MonoBehaviour2
    {
        private readonly List<BaseWindow> windows = new List<BaseWindow>();

        static internal OrganizerWindow craftOrganizerWindow;
        private CurrentCraftTagsWindow manageThisCraftWindow;

        //private List<ApplicationLauncherButton> appLauncherButtons = new List<ApplicationLauncherButton>();

        private bool alreadyAfterCleanup = false;

        public void Start()
        {
            PluginCommons.init(new KspCraftOrganizerProperties());

            PluginLogger.logDebug("Craft organizer plugin - start");

            IKspAlProvider.instance.start();

            CraftAlreadyExistsQuestionWindow craftAlreadyExistsQuestionWindow = addWindow(new CraftAlreadyExistsQuestionWindow());
            ShouldCurrentCraftBeSavedQuestionWindow shouldCraftBeSavedQuestionWindow = addWindow(new ShouldCurrentCraftBeSavedQuestionWindow());
            craftOrganizerWindow = addWindow(new OrganizerWindow(shouldCraftBeSavedQuestionWindow, craftAlreadyExistsQuestionWindow));
            manageThisCraftWindow = addWindow(new CurrentCraftTagsWindow());

            if (!PluginCommons.instance.replaceEditorLoadButton())
            {
                if (toolbarControl == null)
                {
                    toolbarControl = gameObject.AddComponent<ToolbarControl>();
                    toolbarControl.AddToAllToolbars(ToggleDisplayWindow, null,
                         ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH,
                        MODID,
                        "COButton",
                        "KspCraftOrganizer/PluginData/" + "manage.png",
                        "KspCraftOrganizer/PluginData/" + "manage.png",
                        MODNAME
                    );

                }
            }
            else
            {
                //override existing actions on stock load button and replace with call to toggle CM's UI.
                {
                    UnityEngine.UI.Button.ButtonClickedEvent c = new UnityEngine.UI.Button.ButtonClickedEvent();
                    c.AddListener(on_load_click);
                    EditorLogic.fetch.loadBtn.onClick = c;
                }

            }

            if (toolbarControlTagManager == null)
            {
                toolbarControlTagManager = gameObject.AddComponent<ToolbarControl>();

                toolbarControlTagManager.AddToAllToolbars(ToggleCraftTagWindow, null,
                     ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH,
                    MODID,
                    "COBTagutton",
                    "KspCraftOrganizer/PluginData/" + "tags.png",
                    "KspCraftOrganizer/PluginData/" + "tags.png",
                    MODTAGNAME
                );
            }

            foreach (BaseWindow window in windows)
            {
                window.start();
            }

            EditorListenerService.instance.start();

            GameEvents.onGameSceneLoadRequested.Add(OnSceneLoadRequested);

        }


        void ToggleDisplayWindow()
        {
            craftOrganizerWindow.displayWindow();
            toolbarControl.SetFalse();
        }
        void ToggleCraftTagWindow()
        {
            manageThisCraftWindow.displayWindow();
            toolbarControlTagManager.SetFalse();
        }
        //Replace the default load action
        private UnityEngine.Events.UnityAction on_load_click = new UnityEngine.Events.UnityAction(() =>
        {
            craftOrganizerWindow.displayWindow();
        });

        public void OnSceneLoadRequested(GameScenes gs)
        {
            PluginLogger.logDebug("OnSceneLoadRequested");
            CleanUp();
        }

        internal const string MODID = "CraftOrganizer";
        internal const string MODNAME = "CraftOrganizer";
        internal const string MODTAGNAME = "TagManager";
        static internal ToolbarControl toolbarControl = null;
        static internal ToolbarControl toolbarControlTagManager = null;

#if false
        private void addLauncherButtonInAllEditors(Globals.Procedure callback, string textureFile)
        {
            ApplicationLauncherButton button = null;

            Texture2D texture = UiUtils.loadIcon(textureFile);

            button = ApplicationLauncher.Instance.AddModApplication(
                delegate ()
                {
                    button.SetFalse(false);
                    callback();
                }, null, null, null, null, null,
                ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH, texture);
            appLauncherButtons.Add(button);

        }
#endif

        private T addWindow<T>(T newWindow) where T : BaseWindow
        {
            windows.Add(newWindow);
            return newWindow;
        }



        private void CleanUp()
        {
            PluginLogger.logDebug("Craft organizer plugin - CleanUp in " + EditorDriver.editorFacility);

            GameEvents.onGameSceneLoadRequested.Remove(OnSceneLoadRequested);
            EditorListenerService.instance.processOnEditorExit();
#if false
            foreach (ApplicationLauncherButton button in appLauncherButtons)
            {
                if (ApplicationLauncher.Instance != null)
                    ApplicationLauncher.Instance.RemoveModApplication(button);
            }
#endif
            if (toolbarControl != null)
            {
                toolbarControl.OnDestroy();
                toolbarControl = null;
            }
            toolbarControlTagManager.OnDestroy();
            toolbarControlTagManager = null;
            EditorListenerService.instance.destroy();
            IKspAlProvider.instance.destroy();

            alreadyAfterCleanup = true;

        }

        //
        //Making cleanup in OnDestroy in not a good idea since ksp 1.2 because some global data no longer exist 
        //when this event is fired, so we make cleanup in onGameSceneLoadRequested. We still need to handle OnDestroy
        //in case plugin is reloaded using Kramax Plugin Reload.
        //
        public void OnDestroy()
        {
            PluginLogger.logDebug("OnDestroy");
            if (!alreadyAfterCleanup)
            {
                CleanUp();
            }
        }

        public void Update()
        {
            foreach (BaseWindow window in windows)
            {
                window.update();
            }
        }

        public void OnGUI()
        {
            RegisterToolbar.UpdateStyles(craftOrganizerWindow.guiStyleOption.id);

            GUI.enabled = !DirectorySelect.instance.IsEnabled && Settings.instance == null;
            foreach (BaseWindow window in windows)
            {
                window.onGUI();
            }
        }

        public void OnDisable()
        {
            PluginLogger.logDebug("Craft organizer plugin - OnDisable in " + EditorDriver.editorFacility);
        }
    }
}

