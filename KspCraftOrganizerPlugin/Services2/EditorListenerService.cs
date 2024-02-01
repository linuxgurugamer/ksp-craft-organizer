using System;
using System.IO;
using KSP.UI.Screens;
using System.Collections;
using UnityEngine;

using KspNalCommon;
using KSP.Localization;
using static KspCraftOrganizer.RegisterToolbar;

namespace KspCraftOrganizer
{
    /**
	 * This class can generate events related to Editor: new/load/save. It can also answer if ship has been modified.
	 * 
	 * About new/load/save events:
	 * 
	 * This class can generate new/load/save events for Editor facitily. Originally Save events are not generated, so this class
	 * is a huge hack to simulate it. Basically it checks if file was updated on disk when some new ship is going to be loaded to editor
	 * or if editor is exited or if new ship is going to be made etc. This is not very reliable, but I have not found any other solution
	 * 
	 * It would be good to have following events supported nativly by KSP:
	 *  - Save event fired every time ship is saved (including autosaves), with the path to the file where it is going to be saved
	 *  - "Is ship modified" query event or similar mechanism that would allow plugin to inform the KSP that ship is modified, so
	 * 		clicking on "open" button or "new" could show warning message "your ship is not saved" even if original ship is unmodified,
	 * 		but for example tags are modified
	 * 
	 * About checking if ship has been modified:
	 * 
	 * It does not take into account that user could undo some action. It seems to be a bug in KSP that Undo events are sent 
	 * in both cases: Undo and redo, but redo are never sent so we cannot detect it reliably.
	 * 
	 */
    public class EditorListenerService : MonoBehaviour
    {

        public static EditorListenerService instance = new EditorListenerService();

        //
        //the ship is autosaved on launch, so we should generate onSave events for save only during launch. Right now they are generated each time editor is exited.
        //
        //

        public EditorListenerService()
        {
        }


        private string _lastSavedShipName;
        private string _lastShipNameInEditor;
        private string originalShipRealFileOrNull;
        private bool newEditor;
        private DateTime lastSaveDate;
        private FileLocationService fileLocation = FileLocationService.instance;

        public delegate void OnShipSaved(string fileName, bool craftSavedToNewFile);
        public delegate void OnShipLoaded(string fileName);
        public delegate void OnEditorStarted();

        public OnShipSaved onShipSaved { get; set; }
        public OnShipLoaded onShipLoaded { get; set; }
        public OnEditorStarted onEditorStarted { get; set; }

        int btnId;
        public void start()
        {
            GameEvents.onEditorStarted.Add(this.processOnEditorStarted);
            GameEvents.onEditorLoad.Add(this.processOnEditorLoaded);
            EditorLogic.fetch.shipNameField.onValueChanged.AddListener(this.processOnShipNameChanged);

            GameEvents.onEditorShipModified.Add(this.onEditorShipModified);
            //GameEvents.onEditorUndo.Add(this.onEditorUndo);

            //            GameEvents.onEditorRestart.Add(OnEditorRestart);//fired when New Craft is pressed!

            ButtonManager.BtnManager.InitializeListener(EditorLogic.fetch.saveBtn, NullMethod, "CraftOrganizer");

            btnId = ButtonManager.BtnManager.AddListener(EditorLogic.fetch.saveBtn, OnSaveButtonInput, "CraftOrganizer", "CraftOrganizer");


            onShipSaved += delegate (string path, bool craftSavedToNewFile)
            {
                PluginLogger.logDebug("[Event spy]On Saved to " + path + ", is new file: " + craftSavedToNewFile);
            };

            onShipLoaded += delegate (string path)
            {
                PluginLogger.logDebug("[Event spy]On Loaded from " + path);
            };
            onEditorStarted += delegate ()
            {
                PluginLogger.logDebug("[Event spy]On Editor started");
            };

            if (EditorLogic.fetch.ship == null)
            {
                PluginLogger.logDebug("Ship in editor is null");
                _lastSavedShipName = "";
                _lastShipNameInEditor = "";
                originalShipRealFileOrNull = null;
                newEditor = true;
            }
            else
            {
                _lastSavedShipName = EditorLogic.fetch.ship.shipName;
                _lastShipNameInEditor = EditorLogic.fetch.ship.shipName;
                newEditor = EditorLogic.fetch.ship.Count == 0;
                string file = EditorDriver.filePathToLoad;//this path may be invalid when editor starts
                if (file != "" && Path.GetFullPath(fileLocation.getCraftSaveFilePathForShipName(_lastSavedShipName)) != Path.GetFullPath(file))
                {
                    PluginLogger.logDebug("Name of the ship is '" + _lastSavedShipName + "' but KSP claims it is loaded from '" + file + "' which is probably a bug. Assuming that file was loaded from autosave.");
                    file = fileLocation.getAutoSaveShipPath();//this path may be invalid when we use dynamic plugin reload but it is relevant only during development so lets stick to it
                }
                if (newEditor)
                {
                    file = null;
                }
                originalShipRealFileOrNull = file;
                PluginLogger.logDebug("Ship name in editor: " + _lastSavedShipName + ", number of parts: " + EditorLogic.fetch.ship.Count + ", file:" + originalShipRealFileOrNull);
                if (!newEditor && onShipLoaded != null)
                {
                    onShipLoaded(file);
                }
                updateLastSaveDate();
                //UpdateLastSaveTimeFromOriginalShipFile();

            }
        }

        internal IEnumerator TimedCraftBackup()
        {
            while (true)
            {
                Log.Info("TimedCraftBackup loop, backuInterval: " + HighLogic.CurrentGame.Parameters.CustomParams<KSPCO_Settings>().backupInterval * 60f);
                if (isModifiedSinceSave)
                {
                    CreateCraftBackup(EditorLogic.fetch.ship);
                    ScreenMessages.PostScreenMessage("Vessel backup saved", 15, ScreenMessageStyle.UPPER_CENTER);
                }
                yield return new WaitForSecondsRealtime ((float)HighLogic.CurrentGame.Parameters.CustomParams<KSPCO_Settings>().backupInterval*60f);
            }
        }

        void NullMethod()
        {
        }

        public void OnSaveButtonInput()
        {
            IKspAl ksp = IKspAlProvider.instance;

            ksp.saveCurrentCraft();
            if (HighLogic.CurrentGame.Parameters.CustomParams<KSPCO_Settings>().saveBackupAfterSave && 
                !HighLogic.CurrentGame.Parameters.CustomParams<KSPCO_Settings>().saveBackupAfterChange)
                CreateCraftBackup(EditorLogic.fetch.ship);

            ButtonManager.BtnManager.InvokeNextDelegate(btnId, "CraftOrganizer");
        }
        public bool isModifiedSinceSave { get; private set; }

        private void onEditorShipModified(ShipConstruct s)
        {
            Log.Info("EditorListenerService.onEditorShipModified, autosaveOnChange: " + HighLogic.CurrentGame.Parameters.CustomParams<KSPCO_Settings>().saveBackupAfterChange);
            isModifiedSinceSave = true;
            if (HighLogic.CurrentGame.Parameters.CustomParams<KSPCO_Settings>().saveBackupAfterChange)
                CreateCraftBackup(EditorLogic.fetch.ship);
        }

#if false
        private void onEditorUndo(ShipConstruct s)
        {
            //we cannot track undo step - it seems they are buggy and this event is called in both Undo & Redo events. 
            //On the contrary, Redo events are never called :(
        }
#endif

        void CreateCraftBackup(ShipConstruct data)
        {
                var shipName = GetShipName();
                var currentTimeStamp = DateTime.Now.ToString("yy.MM.dd_HH.mm.ss.fff");
                string path = "";

                path = DirectoryServices.MakeNewHistoryDir(data.shipFacility);

                Log.Info("Creating backup at " + path);
                data.SaveShip().Save(Path.Combine(path, currentTimeStamp + ".craft"));
                CheckForMaxBackups(path);
        }

        void CheckForMaxBackups(string path)
        {
            Log.Info("CheckForMaxBackups, path: " + path);

            string[] files = Directory.GetFiles(path, "*.craft");
            Array.Sort(files);

            for (int i = 0; i < files.Length - HighLogic.CurrentGame.Parameters.CustomParams<KSPCO_Settings>().maxBackups; i++) 
            {
                File.Delete(files[i]);
                string s = files[i].Substring(0, files[i].Length - 6) + ".crmgr";
                File.Delete(s);
                Log.Info("Deleting old version file: " + files[i] + ", " + s);

            }
        }

        internal static string GetShipName()
        {
            return KSPUtil.SanitizeString(Localizer.Format(EditorLogic.fetch.ship.shipName), ' ', false);
        }

        public void destroy()
        {
            onShipSaved = null;
            onShipLoaded = null;
            onEditorStarted = null;

            GameEvents.onEditorStarted.Remove(this.processOnEditorStarted);
            GameEvents.onEditorLoad.Remove(this.processOnEditorLoaded);
            GameEvents.onEditorShipModified.Remove(this.onEditorShipModified);
            //GameEvents.onEditorUndo.Remove(this.onEditorUndo);

            EditorLogic.fetch.shipNameField.onValueChanged.RemoveListener(this.processOnShipNameChanged);

        }

        public void processOnEditorExit()
        {
            fireEventIfShipHasBeenSaved();
            if (onShipSaved != null)
            {
                onShipSaved(fileLocation.getAutoSaveShipPath(), true);
            }
        }


        private void processOnEditorStarted()
        {
            PluginLogger.logDebug("processOnEditorStarted");
            fireEventIfShipHasBeenSaved();

            isModifiedSinceSave = false;
            newEditor = true;
            updateLastSaveDate();
            _lastSavedShipName = lastShipNameInEditor;

            if (onEditorStarted != null)
            {
                onEditorStarted();
            }

        }

        private void processOnEditorLoaded(ShipConstruct c, CraftBrowserDialog.LoadType lt)
        {
            if (lt == CraftBrowserDialog.LoadType.Normal)
            {
                PluginLogger.logDebug("processOnEditorLoaded, file: " + EditorDriver.filePathToLoad + ", c.shipName: " + c.shipName);
                string file = EditorDriver.filePathToLoad;
                if (!newEditor)
                {
                    fireEventIfShipHasBeenSaved();
                }

                _lastShipNameInEditor = c.shipName;

                _lastSavedShipName = c.shipName;
                newEditor = false;
                originalShipRealFileOrNull = file;
                isModifiedSinceSave = false;

                updateLastSaveDate();

                if (onShipLoaded != null)
                {
                    onShipLoaded(file);
                }
            }
        }

        private void updateLastSaveDate()
        {
            string fileSavePath = fileLocation.getCraftSaveFilePathForShipName(lastShipNameInEditor);
            if (File.Exists(fileSavePath))
            {
                lastSaveDate = File.GetLastWriteTime(fileSavePath);
            }
            else
            {
                PluginLogger.logDebug("Cannot update last save date because file does not exist " + fileSavePath);
                lastSaveDate = new DateTime(0);
            }
        }

        /**
         * This function must be called every time user has possibility to "see" that ship was saved. For example we 
         * use it when the user opens "load craft" window so we can detect if current ship was saved and if it was, the tags are
         * written to the disk as well.
         */
        public void fireEventIfShipHasBeenSaved()
        {

            string fileSavePath = fileLocation.getCraftSaveFilePathForShipName(lastShipNameInEditor);
            if (File.Exists(fileSavePath))
            {
                DateTime newWriteTime = File.GetLastWriteTime(fileSavePath);
                if (newWriteTime > lastSaveDate)
                {
                    PluginLogger.logDebug("Craft file for " + lastShipNameInEditor + " changed, previous save date: " + lastSaveDate + ", current save date: " + File.GetLastWriteTime(fileSavePath));
                    if (onShipSaved != null)
                    {
                        onShipSaved(fileSavePath,
                                    _lastSavedShipName != lastShipNameInEditor ||
                                    newEditor ||
                                    (originalShipRealFileOrNull != null && originalShipRealFileOrNull != fileSavePath));
                    }
                    else
                    {
                        PluginLogger.logDebug("onShipSaved will not be called because it is null");
                    }

                    lastSaveDate = File.GetLastWriteTime(fileSavePath);
                    _lastSavedShipName = lastShipNameInEditor;
                    originalShipRealFileOrNull = null;
                    newEditor = false;
                    isModifiedSinceSave = false;
                }
                else
                {
                    PluginLogger.logDebug("It was detected that file was not saved because it is not newer, new date: " + newWriteTime + ", old date: " + lastSaveDate + ", file: " + fileSavePath);
                }
            }
            else
            {
                PluginLogger.logDebug("It was detected that file was not saved because it does not exist: " + fileSavePath);
            }

        }


        private void processOnShipNameChanged(string newName)
        {
            if (lastShipNameInEditor != newName)
            {
                PluginLogger.logDebug("processOnShipNameChanged, old name: " + lastShipNameInEditor + ", new name: " + newName);

                fireEventIfShipHasBeenSaved();

                _lastShipNameInEditor = newName;
                updateLastSaveDate();
            }
        }

        public bool canAutoSaveSomethingToDisk()
        {
            return !newEditor && lastShipNameInEditor == _lastSavedShipName && File.Exists(originalShipFile) && fileLocation.getCraftSaveFilePathForCurrentShip() == currentShipFile;
        }

        public string lastShipNameInEditor { get { return _lastShipNameInEditor; } }
        public string lastSavedShipName { get { return _lastSavedShipName; } }


        public string originalShipFile
        {
            get
            {
                if (originalShipRealFileOrNull != null)
                {
                    return originalShipRealFileOrNull;
                }
                else
                {
                    return fileLocation.getCraftSaveFilePathForShipName(lastSavedShipName);
                }
            }
        }


        public string currentShipFile
        {
            get
            {
                if (originalShipRealFileOrNull != null && lastSavedShipName == lastShipNameInEditor)
                {
                    return originalShipRealFileOrNull;
                }
                else
                {
                    return fileLocation.getCraftSaveFilePathForShipName(lastShipNameInEditor);
                }
            }
        }

        public bool isNewEditor()
        {
            return newEditor;
        }
    }
}

