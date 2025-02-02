﻿using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using KspNalCommon;

namespace KspCraftOrganizer
{

    public class ListOfCraftsInSave
    {

        private readonly string saveName;
        static private FileLocationService fileLocationService = FileLocationService.instance;

        private readonly OrganizerController parent;
        private readonly SettingsService settingsService = SettingsService.instance;
        private readonly IKspAl ksp = IKspAlProvider.instance;
        private readonly Dictionary<CraftType, List<OrganizerCraftEntity>> craftTypeToAvailableCraftsLazy = new Dictionary<CraftType, List<OrganizerCraftEntity>>();



        public ListOfCraftsInSave(OrganizerController parent, string saveName)
        {
            this.parent = parent;
            this.saveName = saveName;
        }

        public List<OrganizerCraftEntity> getCraftsOfType(CraftType type)
        {
            if (!craftTypeToAvailableCraftsLazy.ContainsKey(type))
            {
                craftTypeToAvailableCraftsLazy.Add(type, fetchAvailableCraftsAndDirs(type));
            }
            return craftTypeToAvailableCraftsLazy[type];
        }

        private List<OrganizerCraftEntity> fetchAvailableCraftsAndDirs(CraftType type)
        {
            PluginLogger.logDebug("fetching '" + type + "' crafts from disk");

            string craftDirectory = fileLocationService.getCraftDirectoryForCraftType(saveName, type);
            List<OrganizerCraftEntity> toRetList = new List<OrganizerCraftEntity>();

            if (!SettingsService.instance.getPluginSettings().onlyStockVessels)
                toRetList.AddRange(fetchAvailableCraftsAndDirectories(craftDirectory, type, false));

            if (ksp.isShowStockCrafts() && parent.thisIsPrimarySave && 
                HighLogic.CurrentGame.Parameters.CustomParams<KSPCO_Settings>().allowStockVessels)
            {
                for (int dirCnt = 0; dirCnt < ksp.StockDirs().Length; dirCnt++)
                {
                    craftDirectory = fileLocationService.getStockCraftDirectoryForCraftType(type, dirCnt);
                    if (SettingsService.instance.getPluginSettings().onlyStockVessels && craftDirectory.Contains("saves"))
                        continue;
                    toRetList.AddRange(fetchAvailableCraftsAndDirectories(craftDirectory, type, true));
                }
            }

            toRetList.Sort(delegate (OrganizerCraftEntity c1, OrganizerCraftEntity c2)
            {
                if (c1.IsDir && !c2.IsDir)
                    return -1;
                if (!c1.IsDir && c2.IsDir)
                    return 1;
                if (c1.IsDir && c2.IsDir)
                    return c1.DirName.CompareTo(c2.DirName);

                int craftComparisonResult = 0;
                try
                {
                    craftComparisonResult = -c1.isAutosaved.CompareTo(c2.isAutosaved);
                    if (craftComparisonResult == 0)
                    {
                        craftComparisonResult = c1.name.CompareTo(c2.name);
                    }
                }
                catch (Exception ex)
                {
                    PluginLogger.logError("Error during craft file loading - cannot compare '" + c1.craftFile + "' with '" + c2.craftFile + "'", ex);
                }
                return craftComparisonResult;
            });
            return toRetList;
        }


        private OrganizerCraftEntity[] fetchAvailableCraftsAndDirectories(String craftDirectory, CraftType type, bool isStock)
        {
            PluginLogger.logDebug("fetching '" + type + "' crafts from disk from " + craftDirectory);
            List<OrganizerCraftEntity> toRet = new List<OrganizerCraftEntity>();

            float startLoadingTime = Time.realtimeSinceStartup;
            string[] craftFiles = null;
            List<string> dirs = new List<string>();
            string dir = craftDirectory + DirectoryServices.curDir;


            if (Directory.Exists(dir) )
            {
                craftFiles = fileLocationService.getAllCraftFilesInDirectory(dir);
                dirs = fileLocationService.getAllDirectoriesInDirectory(dir);
            }



            if (DirectoryServices.dirStack.Count > 0 && !isStock)
            {
                OrganizerCraftEntity newDir = new OrganizerCraftEntity(parent, "..", true);
                toRet.Add(newDir);
            }
            foreach (var d in dirs)
            {
                OrganizerCraftEntity newDir = new OrganizerCraftEntity(parent, d, true);
                toRet.Add(newDir);
            }
            if (craftFiles != null)
            {
                foreach (string craftFile in craftFiles)
                {
                    try
                    {
                        OrganizerCraftEntity newCraft = new OrganizerCraftEntity(parent, craftFile);
                        newCraft.isAutosaved = ksp.getAutoSaveCraftName() == Path.GetFileNameWithoutExtension(craftFile);
                        newCraft.isStock = isStock;

                        CraftSettingsDto craftSettings = settingsService.readCraftSettingsForCraftFile(newCraft.craftFile);
                        foreach (string craftTag in craftSettings.tags)
                        {
                            newCraft.addTag(craftTag);
                        }
                        newCraft.nameFromSettingsFile = craftSettings.craftName;
                        newCraft.craftSettingsFileIsDirty = false;

                        newCraft.finishCreationMode();

                        toRet.Add(newCraft);
                    }
                    catch (Exception ex)
                    {
                        PluginLogger.logError("Error during craft file loading '" + craftFile + "'", ex);
                    }
                }
                float endLoadingTime = Time.realtimeSinceStartup;
                PluginLogger.logDebug("Finished fetching " + craftFiles.Length + " crafts, it took " + (endLoadingTime - startLoadingTime) + "s");
            }
            //else
            //    PluginLogger.logDebug("Directory not found");
            return toRet.ToArray();
        }


        public ICollection<List<OrganizerCraftEntity>> alreadyLoadedCrafts
        {
            get
            {
                return craftTypeToAvailableCraftsLazy.Values;
            }
        }
    }


    public class OrganizerControllerCraftList
    {
        public delegate bool CraftFilterPredicate(OrganizerCraftEntity craft, out bool shouldBeVisibleByDefault);

        private OrganizerCraftEntity[] cachedFilteredCrafts;
        private OrganizerCraftEntity _primaryCraft;
        private int cachedSelectedCraftsCount;
        private CraftType _craftType;
        private readonly Dictionary<string, ListOfCraftsInSave> saveToListOfCrafts = new Dictionary<string, ListOfCraftsInSave>();
        private readonly CraftSortingHelper sortingHelper;

        private readonly IKspAl ksp = IKspAlProvider.instance;
        private readonly FileLocationService fileLocationService = FileLocationService.instance;
        private readonly OrganizerController parent;

        public void ClearListOfCrafts()
        {
            cachedFilteredCrafts = null;
            saveToListOfCrafts.Clear();
        }
        private string currentSave { get { return parent.currentSave; } }

        public OrganizerControllerCraftList(OrganizerController parent)
        {
            this.parent = parent;
            this.sortingHelper = new CraftSortingHelper(parent.stateManager);
            _craftType = ksp.getCurrentEditorFacilityType();
        }

        public bool selectAllFiltered
        {
            get;
            private set;
        }

        public bool forceUncheckSelectAllFiltered
        {
            get;
            set;
        }

        public void update(bool selectAll, bool filterChanged)
        {
            if (this.cachedFilteredCrafts == null || filterChanged)
            {
                this.cachedFilteredCrafts = createFilteredCrafts(parent.craftFilterPredicate);
                parent.markFilterAsUpToDate();
            }
            updateSelectedCrafts(selectAll);
        }

        public void addCraftSortingFunction(CraftSortFunction function)
        {
            if (sortingHelper.addCraftSortingFunction(function))
            {
                cachedFilteredCrafts = null;
            }
        }

        public ICraftSortFunction getCraftSortingFunction()
        {
            return sortingHelper.getLastSortFunction();
        }

        public bool craftsAreFiltered { get; private set; }

        public OrganizerCraftEntity[] filteredCrafts
        {
            get
            {
                if (cachedFilteredCrafts == null)
                {
                    return new OrganizerCraftEntity[0];
                }
                return cachedFilteredCrafts;
            }
        }

        private OrganizerCraftEntity[] createFilteredCrafts(CraftFilterPredicate craftFilterPredicate)
        {
            PluginLogger.logDebug("Creating filtered crafts");
            List<OrganizerCraftEntity> filtered = new List<OrganizerCraftEntity>();
            craftsAreFiltered = false;
            foreach (OrganizerCraftEntity craft in availableCrafts)
            {
                if (!craft.IsDir)
                {
                    bool shouldBeVisibleByDefault;
                    try
                    {
                        if (craftFilterPredicate(craft, out shouldBeVisibleByDefault))
                        {
                            filtered.Add(craft);
                            if (!shouldBeVisibleByDefault)
                            {
                                craftsAreFiltered = true;
                            }
                        }
                        else
                        {
                            if (shouldBeVisibleByDefault)
                            {
                                craftsAreFiltered = true;
                            }
                            if (craft.isSelectedPrimary)
                            {
                                primaryCraft = null;
                            }
                            craft.setSelectedInternal(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        PluginLogger.logError("Error while evaluating  if craft '" + craft.craftFile + "' is available in current filter", ex);
                    }
                }
                else
                    filtered.Add(craft);
            }
            sortCrafts(filtered);
            return filtered.ToArray();
        }


        private void sortCrafts(List<OrganizerCraftEntity> crafts)
        {
            sortingHelper.sortCrafts(crafts);
        }


        public OrganizerCraftEntity primaryCraft
        {
            set
            {
                if (_primaryCraft != null)
                {
                    _primaryCraft.setSelectedPrimaryInternal(false);
                }
                _primaryCraft = value;
                if (_primaryCraft != null)
                {
                    _primaryCraft.setSelectedPrimaryInternal(true);
                }
            }
            get
            {
                return _primaryCraft;
            }
        }

        public void renameCraft(OrganizerCraftEntity model, string newName)
        {
            string newFile = fileLocationService.renameCraft(model.craftFile, newName);
            model.renameCraft(newName, newFile);
        }

        public List<OrganizerCraftEntity> availableCrafts
        {
            get
            {
                return getCraftsForSave(currentSave).getCraftsOfType(craftType);
            }
        }

        public List<OrganizerCraftEntity> getCraftsOfType(CraftType type)
        {
            return craftsForCurrentSave.getCraftsOfType(type);
        }

        private ListOfCraftsInSave craftsForCurrentSave
        {
            get
            {
                return getCraftsForSave(currentSave);
            }

        }

        private ListOfCraftsInSave getCraftsForSave(string saveName)
        {
            if (!saveToListOfCrafts.ContainsKey(saveName))
            {
                saveToListOfCrafts.Add(saveName, new ListOfCraftsInSave(parent, saveName));
            }
            return saveToListOfCrafts[saveName];
        }

        internal void deleteCraft(OrganizerCraftEntity model)
        {
            fileLocationService.deleteCraft(model.craftFile);
            availableCrafts.Remove(model);
            clearCaches("craft deleted");
        }

        public int? selectedMoveCount = null;
        public int MoveSelectCount()
        {
            if (selectedMoveCount != null)
                return (int)selectedMoveCount;

            int i = 0;
            foreach (OrganizerCraftEntity craft in filteredCrafts)
                if (craft.isSelectedForMove)
                    i++;
            selectedMoveCount = i;
            return i;
        }
        public void unselectAllCrafts()
        {
            foreach (OrganizerCraftEntity craft in filteredCrafts)
            {
                craft.isSelected = false;
            }
        }

        public void unselectAllMoveCrafts()
        {
            foreach (OrganizerCraftEntity craft in filteredCrafts)
            {
                craft.isSelectedForMove = false;
            }
            selectedMoveCount = null;
        }

        public List<OrganizerCraftEntity> GetCraftSelectedForMove()
        {
            List<OrganizerCraftEntity> oce = new List<OrganizerCraftEntity>();
            foreach (OrganizerCraftEntity craft in filteredCrafts)
                if (craft.isSelectedForMove)
                    oce.Add(craft);
            return oce;
        }
        private void updateSelectedCrafts(bool selectAll)
        {
            if (forceUncheckSelectAllFiltered)
            {
                selectAllFiltered = false;
                forceUncheckSelectAllFiltered = false;
            }
            else
            {
                if (selectAllFiltered != selectAll || selectAll)
                {
                    foreach (OrganizerCraftEntity craft in filteredCrafts)
                    {
                        craft.isSelected = selectAll;
                    }
                    selectAllFiltered = selectAll;
                }
            }
            cachedSelectedCraftsCount = 0;
            foreach (OrganizerCraftEntity craft in filteredCrafts)
            {
                if (craft.isSelected)
                {
                    cachedSelectedCraftsCount += 1;
                }
            }
        }


        public ICollection<List<OrganizerCraftEntity>> alreadyLoadedCrafts
        {
            get
            {
                List<List<OrganizerCraftEntity>> loadedCrafts = new List<List<OrganizerCraftEntity>>();
                foreach (ListOfCraftsInSave listForSave in saveToListOfCrafts.Values)
                {
                    loadedCrafts.AddRange(listForSave.alreadyLoadedCrafts);
                }
                return loadedCrafts;

            }
        }

        public int selectedCraftsCount
        {
            get
            {
                return cachedSelectedCraftsCount;
            }
        }

        public CraftType craftType
        {
            get
            {
                return _craftType;
            }

            set
            {
                if (value != _craftType)
                {
                    _craftType = value;
                    clearCaches("New craft type: " + value);
                    if (this._primaryCraft != null)
                    {
                        this._primaryCraft.setSelectedPrimaryInternal(false);
                    }
                    this._primaryCraft = null;
                }
            }
        }

        public void clearCaches(string reason)
        {
            PluginLogger.logDebug("Clearing caches in OrganizerServiceCraftList, reason: " + reason);
            this.cachedFilteredCrafts = null;
            this.cachedSelectedCraftsCount = 0;
        }
    }
}

