using UnityEngine;
using System.Collections.Generic;
using System;
using KspNalCommon;
using UnityEngine.UI;
using static KspCraftOrganizer.RegisterToolbar;

namespace KspCraftOrganizer
{
    public class OrganizerWindow : BaseWindow, IGuiOverlayContainer
    {
        private static readonly string UP_ARROW_STRING = "\u02C4";
        private static readonly string DOWN_ARROW_STRING = "\u02C5";

        private static readonly string[] SPH_VAB = { "VAB", "SPH", "Subass" };
        private static readonly CraftType[] SPH_VAB_STATES = { CraftType.VAB, CraftType.SPH, CraftType.SUBASSEMBLY };

        private static readonly string TEXT_FILTER_CONTROL_NAME = "OrganizerWindow_TEXT_FILTER_CONTROL_NAME";

        public static readonly int FILTER_TOOLBAR_WIDTH = 220;
        public static readonly int MANAGE_TAGS_TOOLBAR_WIDTH = 240;
        public static readonly int NO_MANAGE_TAGS_TOOLBAR_WIDTH = 0;
        public static readonly int MIN_WINDOW_WIDTH = 870;

        private Vector2 tagScrollPosition;
        public bool showManageTagsToolbar { get; set; }
        public string selectedCraftName = "";
        public bool dirSelected = false;
        public bool selectAllFiltered;
        private int selectedGuiSkin = 1;
        private Rect skinToolbarRect;
        private bool extendedCancel;
        private bool displayQuestionAboutExtendedQuestion;

        private OrganizerController _modelLazy;
        private List<DrawOverlay> endOverlaysToDraw = new List<DrawOverlay>();
        private List<DrawOverlay> startOverlaysToDraw = new List<DrawOverlay>();
        private bool showSaveFileChoice = false;

        private DropDownList<string> chooseSaveName;
        private DropDownList<CraftSortData> sortingModeDropDown;

        public OrganizerController model
        {
            get
            {
                if (_modelLazy == null)
                {
                    _modelLazy = new OrganizerController();
                    if (lastCraftTypeSet)
                        model.craftType = lastCraftType;
                }
                return _modelLazy;
            }
        }

        public GUIStyle warningLabelStyle { get { return RegisterToolbar._warningLabelStyle; } }
        public GUIStyle toggleButtonStyleFalse { get { return RegisterToolbar._toggleButtonStyleFalse; } }
        public GUIStyle toggleButtonStyleTrue { get { return RegisterToolbar._toggleButtonStyleTrue; } }
        public GUIStyle settingsBtnStyle { get { return RegisterToolbar.settingsBtnStyle; } }

        private OrganizerWindowTagsManagementBar tagsManagementBar;
        private OrganizerWindowCraftList craftList;

        private EditorListenerService editorListenerService = EditorListenerService.instance;

        private ShouldCurrentCraftBeSavedQuestionWindow shouldCurrentCraftBeSavedWindow;
        private CraftAlreadyExistsQuestionWindow craftAlreadyExistsQuestionWindow;

        public OrganizerWindow(ShouldCurrentCraftBeSavedQuestionWindow shouldCurrentCraftBeSavedWindow, CraftAlreadyExistsQuestionWindow craftAlreadyExistsQuestionWindow) : base("Load craft")
        {
            tagsManagementBar = new OrganizerWindowTagsManagementBar(this);
            craftList = new OrganizerWindowCraftList(this);
            this.shouldCurrentCraftBeSavedWindow = shouldCurrentCraftBeSavedWindow;
            this.craftAlreadyExistsQuestionWindow = craftAlreadyExistsQuestionWindow;
            this.chooseSaveName = new DropDownList<string>(model.availableSaveNames, t => t);
            chooseSaveName.selectedItem = model.currentSave;

            sortingModeDropDown = new DropDownList<CraftSortData>(new CraftSortData[0], t =>
            {
                if (t.function.isSame(model.getCraftSortingFunction()))
                {
                    if (model.getCraftSortingFunction().isReversed)
                    {
                        return UP_ARROW_STRING + " " + t.name;
                    }
                    else
                    {
                        return DOWN_ARROW_STRING + " " + t.name;
                    }
                }
                else
                {
                    return t.name;
                }
            });
            sortingModeDropDown.selectedItemIndex = 0;
        }

        protected override float getWindowWidthOnScreen(Rect pos)
        {
            return Math.Min(Math.Min(Math.Max(Screen.width * 9 / 10, MIN_WINDOW_WIDTH), Screen.height * 1.5f), 800 * guiRawScale);
        }

        override protected float getMinWindowInnerWidth(Rect pos)
        {
            return MIN_WINDOW_WIDTH;
        }

        override public void displayWindow()
        {
            base.displayWindow();

            EditorListenerService.instance.fireEventIfShipHasBeenSaved();
            selectedGuiSkin = Array.IndexOf(GuiStyleOption.SKIN_STATES, model.selectedGuiStyle);
            _modelLazy = null;
            PluginLogger.logDebug("Current save: " + model.currentSave);
            chooseSaveName.selectedItem = model.currentSave;
        }

        public void Reload() { reload = true; }
        bool reload = false;
        bool lastCraftTypeSet = false;
        CraftType lastCraftType;
        override public void update()
        {
            base.update();
            if (windowDisplayed)
            {
                if (reload)
                {
                    lastCraftType = model.craftType;
                    _modelLazy = null;
                    reload = false;
                    lastCraftTypeSet = true;
                }
                List<CraftSortData> sortingFields = new List<CraftSortData>(new CraftSortData[] {
                    new CraftSortData("name", CraftSortFunction.SORT_CRAFTS_BY_NAME),
                    new CraftSortData("parts", CraftSortFunction.SORT_CRAFTS_BY_PARTS_COUNT),
                    new CraftSortData("mass", CraftSortFunction.SORT_CRAFTS_BY_MASS),
                    new CraftSortData("stages", CraftSortFunction.SORT_CRAFTS_BY_STAGES),
                    new CraftSortData("cost", CraftSortFunction.SORT_CRAFTS_BY_COST),
                    new CraftSortData("modification time", CraftSortFunction.SORT_CRAFTS_BY_DATE)
                });
                foreach (FilterTagGroup tagGroup in model.filterTagsGrouper.groups)
                {
                    if (!tagGroup.isYesNoGroup)
                    {
                        sortingFields.Add(new CraftSortData("tag:" + tagGroup.displayName, CraftSortFunction.createByTagSorting(tagGroup.name)));
                    }
                }
                if (sortingModeDropDown.selectedItem != null && sortingModeDropDown.getAndClearItemChangedByUserFlag())
                {
                    model.onCraftSortingFunctionSelected(sortingModeDropDown.selectedItem.function);
                }
                sortingModeDropDown.items = sortingFields;


                ICraftSortFunction selectedSortingFunction = model.getCraftSortingFunction();

                for (int index = 0; index < sortingFields.Count; ++index)
                {
                    CraftSortData s = sortingFields[index];
                    if (s.function.isSame(selectedSortingFunction))
                    {
                        sortingModeDropDown.selectedItemIndex = index;
                    }
                }

                model.update(chooseSaveName.selectedItem, selectAllFiltered);
                this.selectAllFiltered = model.selectAllFiltered;

                if (model.primaryCraft == null && !dirSelected)
                {
                    this.selectedCraftName = "";
                }
                model.selectedGuiStyle = GuiStyleOption.SKIN_STATES[selectedGuiSkin];
                guiStyleOption = GuiStyleOption.SKIN_STATES[selectedGuiSkin];
            }
        }

        override protected void windowGUI(int WindowID)
        {
            if (GUI.Button(new Rect(2, 2, 20, 20), RegisterToolbar.settingsTextureBtn, settingsBtnStyle))
            {
                GameObject go = new GameObject();
                var w = go.AddComponent<Settings>();
            }

            drawStartOverlays();

            using (new GUILayout.VerticalScope())
            {

                drawTopToolbar();

                GUILayout.Space(10);

                using (new GUILayout.HorizontalScope())
                {

                    drawFilterColumn();

                    using (new GUILayout.VerticalScope())
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            drawCraftsFilteredWarning();
                            GUILayout.FlexibleSpace();
                            if (!showManageTagsToolbar)
                            {
                                GUILayout.Button("Sort by:", skin.label, GUILayout.Width(60));
                            }
                            sortingModeDropDown.onGui(this, showManageTagsToolbar ? 100 : 200);
                        }
                        if (model.filteredCrafts.Length > 50)
                        {
                            GUILayout.Label(
                                "There are more than 50 crafts displayed at once (currently " + model.filteredCrafts.Length + "). Try to filter crafts to increase performance. You can also archive some crafts.",
                                warningLabelStyle
                            );
                        }
                        craftList.drawCraftsList();
                    }

                    GUILayout.Space(10);

                    if (showManageTagsToolbar)
                    {
                        tagsManagementBar.drawManageTagsColumn();
                    }
                }
                GUILayout.Space(10);

                drawBottomBar();
            }

            drawEndOverlays();

            drawCraftTooltip();

            GUI.DragWindow();
        }

        void drawStartOverlays()
        {
            foreach (DrawOverlay overlay in startOverlaysToDraw)
            {
                overlay();
            }
            startOverlaysToDraw.Clear();
        }

        public void addOverlayAtStart(DrawOverlay drawOverlay)
        {
            startOverlaysToDraw.Add(drawOverlay);
        }

        public void addOverlayAtEnd(DrawOverlay drawOverlay)
        {
            endOverlaysToDraw.Add(drawOverlay);
        }

        public float maxX
        {
            get
            {
                return windowWidth;
            }
        }

        private void drawEndOverlays()
        {
            foreach (DrawOverlay overlay in endOverlaysToDraw)
            {
                overlay();
            }
            endOverlaysToDraw.Clear();
        }

        private void drawTopToolbar()
        {
            using (new GUILayout.HorizontalScope())
            {
                int oldSphOrVab = Array.IndexOf(SPH_VAB_STATES, model.craftType);
                int sphOrVab = GUILayout.Toolbar(oldSphOrVab, SPH_VAB, GUILayout.MaxWidth(160), GUILayout.ExpandWidth(true));
                if (oldSphOrVab != sphOrVab)
                {
                    DirectoryServices.PopAll();
                }
                model.craftType = SPH_VAB_STATES[sphOrVab];

                if (showSaveFileChoice)
                {
                    if (GUILayout.Button("<<", GUILayout.ExpandWidth(false)))
                    {
                        showSaveFileChoice = false;
                    }
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Space(20);
                        GUILayout.Label("Import from save:", GUILayout.Width(150));
                        chooseSaveName.onGui(this, 200);
                    }
                }
                else
                {
                    if (GUILayout.Button(">>", GUILayout.ExpandWidth(false)))
                    {
                        showSaveFileChoice = true;
                    }
                    GUILayout.Space(10);
                }

                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Auto Tag"))
                {
                    DoAutoTag();
                }
                string toggleManageTagsButtonLabel = showManageTagsToolbar ? "Close Tags" : "Manage Tags";
                if (GUILayout.Button(toggleManageTagsButtonLabel, GUILayout.ExpandWidth(false)))
                {
                    showManageTagsToolbar = !showManageTagsToolbar;
                }
            }
        }

        void DoAutoTag()
        {
            foreach (var craft in model.availableCrafts)
            {
                if (!craft.IsDir)
                {
                    if (craft.isStock)
                        craft.addTag(@"Type\Stock");

                    if (craft.crewCapacity > 0)
                        craft.addTag(@"Type\Crewed");
                    else
                        craft.addTag(@"Type\Unmanned");
                }
            }
            ScreenMessages.PostScreenMessage("AutoTag completed", 5, ScreenMessageStyle.UPPER_CENTER);
        }
        void drawCraftsFilteredWarning()
        {
            bool displayCraftsFilteredWarning = model.craftsAreFiltered;
            if (!displayCraftsFilteredWarning)
            {
                GUI.BeginClip(Globals.ZERO_RECT);
            }

            GUILayout.Label("The filter modifies craft list", warningLabelStyle, GUILayout.Width(200));
            if (GUILayout.Button("Clear filter", GUILayout.ExpandWidth(false)))
            {
                clearFilters();
            }

            if (!displayCraftsFilteredWarning)
            {
                GUI.EndClip();
            }
        }

        private void drawFilterColumn()
        {
            using (new GUILayout.VerticalScope(GUILayout.Width(FILTER_TOOLBAR_WIDTH)))
            {
                using (new GUILayout.VerticalScope(GUILayout.ExpandWidth(false)))
                {
                    var width = FILTER_TOOLBAR_WIDTH - 10 - skin.verticalScrollbar.CalcScreenSize(skin.verticalScrollbar.CalcSize(new GUIContent(""))).x;
                    GUILayout.Label("Filter crafts by name:", GUILayout.Width(width));
                    GUI.SetNextControlName(TEXT_FILTER_CONTROL_NAME);
                    using (new GUILayout.HorizontalScope(GUILayout.ExpandWidth(true)))
                    {
                        model.craftNameFilter = GUILayout.TextField(model.craftNameFilter, GUILayout.Width(width));
                        if (justAfterWindowDisplay)
                        {
                            GUI.FocusControl(TEXT_FILTER_CONTROL_NAME);
                        }
                        if (model.craftNameFilter != "")
                        {
                            if (GUILayout.Button("x", originalSkin.button, GUILayout.ExpandWidth(false)))
                            {
                                model.craftNameFilter = "";
                            }
                        }
                    }
                }

                if (model.usedTags.Count > 0)
                {
                    FilterTagsGrouper filterTagsGrouper = model.filterTagsGrouper;
                    GUILayout.Space(15);
                    var width = FILTER_TOOLBAR_WIDTH - 10;
                    GUILayout.Label("Filter crafts by tag:", GUILayout.MaxWidth(width));
                    using (GUILayout.ScrollViewScope tagScrollScope = new GUILayout.ScrollViewScope(tagScrollPosition, false, false, GUILayout.MaxWidth(width)))
                    {
                        tagScrollPosition = tagScrollScope.scrollPosition;
                        using (new GUILayout.VerticalScope(GUILayout.ExpandWidth(false)))
                        {
                            foreach (FilterTagGroup tagGroup in filterTagsGrouper.groups)
                            {
                                bool collapsed = tagGroup.isCollapsedInFilterView;

                                if (!collapsed)
                                {
                                    using (new GUILayout.HorizontalScope(GUILayout.ExpandWidth(true)))
                                    {
                                        if (GUILayout.Button("- " + tagGroup.displayName + ":", this.skin.label))
                                        {
                                            tagGroup.isCollapsedInFilterView = !collapsed;
                                        }
                                        GUILayout.FlexibleSpace();
                                        drawClearTagGroupFilterButton(tagGroup);
                                    }
                                    if (tagGroup.isYesNoGroup)
                                    {
                                        OrganizerTagEntity tag = tagGroup.firstTag.originalTag;
                                        int thisYesNoTagState;
                                        if (tag.selectedForFiltering)
                                        {
                                            thisYesNoTagState = 0;
                                        }
                                        else if (filterTagsGrouper.hasGroupSelectedNoneFilter(tagGroup.name))
                                        {
                                            thisYesNoTagState = 1;
                                        }
                                        else
                                        {
                                            thisYesNoTagState = 2;
                                        }
                                        thisYesNoTagState = guiLayout_Radio_OrigSkin(thisYesNoTagState, 0, " " + tagGroup.displayName);
                                        thisYesNoTagState = guiLayout_Radio_OrigSkin(thisYesNoTagState, 1, " not " + tagGroup.displayName);
                                        thisYesNoTagState = guiLayout_Radio_OrigSkin(thisYesNoTagState, 2, " all");
                                        tag.selectedForFiltering = thisYesNoTagState == 0;
                                        tag.NOT = false;
                                        model.setGroupHasSelectedNoneFilter(tagGroup.name, thisYesNoTagState == 1);
                                    }
                                    else
                                    {
                                        foreach (TagInGroup<OrganizerTagEntity> tagInGroup in tagGroup.tags)
                                        {
                                            OrganizerTagEntity tag = tagInGroup.originalTag;
                                            GUILayout.BeginHorizontal();

                                            string s = tag.NOT ? "Not have" : "Have";
                                            if (GUILayout.Button(s, GUI.skin.label, GUILayout.Width(65)))
                                                tag.NOT = !tag.NOT;
                                            tag.selectedForFiltering = guiLayout_Toggle_OrigSkin(tag.selectedForFiltering, " " + tagInGroup.tagDisplayName);
                                            GUILayout.FlexibleSpace();
                                            GUILayout.EndHorizontal();
                                            GUILayout.Space(5);
                                        }
                                        bool noneSelected = guiLayout_Toggle_OrigSkin(filterTagsGrouper.hasGroupSelectedNoneFilter(tagGroup.name), " <with no tag assigned>");
                                        model.setGroupHasSelectedNoneFilter(tagGroup.name, noneSelected);

                                        bool requireAll = guiLayout_Toggle_OrigSkin(filterTagsGrouper.hasGroupRequireAllFilter(tagGroup.name), " <Require All>");
                                        model.setGroupHasRequireAllFilter(tagGroup.name, requireAll);

                                    }
                                }
                                else
                                {
                                    using (new GUILayout.HorizontalScope(GUILayout.ExpandWidth(true)))
                                    {
                                        if (GUILayout.Button("+ " + tagGroup.displayName, this.skin.label))
                                        {
                                            tagGroup.isCollapsedInFilterView = !collapsed;
                                        }
                                        GUILayout.FlexibleSpace();
                                        drawClearTagGroupFilterButton(tagGroup);
                                    }
                                }
                            }
                            if (filterTagsGrouper.restTags.Count > 0)
                            {
                                bool collapsed = model.restTagsInFilterCollapsed;
                                string labelString;
                                if (filterTagsGrouper.groups.Count > 0)
                                {
                                    labelString = "Rest tags";
                                }
                                else
                                {
                                    labelString = "Tags";
                                }
                                if (collapsed)
                                {
                                    if (GUILayout.Button("+ " + labelString, this.skin.label))
                                    {
                                        model.restTagsInFilterCollapsed = !collapsed;
                                        //model.markProfileSettingsAsDirty("rest tags group collapsed state changed");
                                    }
                                }
                                else
                                {
                                    if (GUILayout.Button("- " + labelString + ":", this.skin.label))
                                    {
                                        model.restTagsInFilterCollapsed = !collapsed;
                                        //model.markProfileSettingsAsDirty("rest tags group collapsed state changed");
                                    }
                                    foreach (OrganizerTagEntity tag in filterTagsGrouper.restTags)
                                    {
                                        GUILayout.BeginHorizontal();
                                        string s = tag.NOT ? "Not have" : "Have";
                                        if (GUILayout.Button(s, GUI.skin.label, GUILayout.Width(65)))
                                            tag.NOT = !tag.NOT;

                                        tag.selectedForFiltering = guiLayout_Toggle_OrigSkin(tag.selectedForFiltering, " " + tag.name);
                                        GUILayout.FlexibleSpace();
                                        GUILayout.EndHorizontal();
                                        GUILayout.Space(5);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        void drawClearTagGroupFilterButton(FilterTagGroup tagGroup)
        {
            FilterTagsGrouper filterTagsGrouper = model.filterTagsGrouper;
            bool shouldClearBeDisplayed = filterTagsGrouper.canGroupBeCleared(tagGroup.name);
            if (shouldClearBeDisplayed)
            {
                if (GUILayout.Button("x", originalSkin.button, GUILayout.ExpandWidth(false)))
                {
                    filterTagsGrouper.clearGroup(tagGroup.name);
                }
            }
        }

        public int guiLayout_Radio_OrigSkin(int state, int thisIndex, string name, params GUILayoutOption[] options)
        {
            bool oldIsSelected = state == thisIndex;
            bool newIsSelected = GUILayout.Toggle(oldIsSelected, name, originalSkin.toggle, options);
            if (newIsSelected || oldIsSelected)
            {
                return thisIndex;
            }
            return state;
        }
        public bool guiLayout_Toggle_OrigSkin(bool value, string name, params GUILayoutOption[] options)
        {
            return GUILayout.Toggle(value, name, originalSkin.toggle, options);
        }

        private void clearFilters()
        {
            model.clearFilters();
        }

        public bool isKspSkin()
        {
            return GuiStyleOption.SKIN_STATES[selectedGuiSkin] == GuiStyleOption.Ksp;
        }

        const int bottomButtonsWidth = 70;
        private void drawBottomBar()
        {
            using (new GUILayout.VerticalScope())
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Skin: ", GUILayout.Width(45));
                    selectedGuiSkin = GUILayout.Toolbar(selectedGuiSkin, GuiStyleOption.SKIN_DISPLAY_OPTIONS);
                    GuiStyleOption.lastSelected = selectedGuiSkin;
                    if (Event.current.type == EventType.Repaint)
                    {
                        skinToolbarRect = GUILayoutUtility.GetLastRect();
                    }
                    GUILayout.Space(Math.Max(FILTER_TOOLBAR_WIDTH - (skinToolbarRect.x + skinToolbarRect.width), 0));
                    if (model.primaryCraft != null)
                    {
                        if (!model.primaryCraft.isStock && !model.primaryCraft.isAutosaved)
                        {
                            if (model.primaryCraft.inRenameState)
                            {

                                selectedCraftName = GUILayout.TextField(selectedCraftName, GUILayout.Width(200));

                                if (GUILayout.Button("Rename"))
                                {
                                    string newName = selectedCraftName.Trim();
                                    if (newName != "")
                                    {
                                        model.renameCraft(model.primaryCraft, newName);
                                    }
                                    model.primaryCraft.inRenameState = false;
                                }
                                if (GUILayout.Button("Cancel"))
                                {
                                    model.primaryCraft.inRenameState = false;
                                }
                            }
                            else if (model.primaryCraft.inDeleteState)
                            {
                                GUILayout.Label("Delete " + model.primaryCraft.name + "?");
                                if (GUILayout.Button("Delete"))
                                {
                                    model.deleteCraft(model.primaryCraft);
                                    model.primaryCraft.inDeleteState = false;
                                }
                                if (GUILayout.Button("Cancel"))
                                {
                                    model.primaryCraft.inDeleteState = false;
                                }
                            }
                            else
                            {
                                if (GUILayout.Button("Rename", GUILayout.ExpandWidth(true), GUILayout.Width(bottomButtonsWidth)))
                                {
                                    model.primaryCraft.inRenameState = true;
                                    selectedCraftName = model.primaryCraft.name;
                                }
                                if (GUILayout.Button("Delete", GUILayout.ExpandWidth(true), GUILayout.Width(bottomButtonsWidth)))
                                {
                                    model.primaryCraft.inDeleteState = true;
                                    selectedCraftName = model.primaryCraft.name;
                                }
                            }
                        }

                        ShowFolderButtons();
                        if (!newFolder)
                        {
                            if (model.isCraftAlreadyLoadedInWorkspace())
                            {
                                if (GUILayout.Button("Merge", GUILayout.ExpandWidth(true), GUILayout.Width(bottomButtonsWidth)))
                                {
                                    model.mergeCraftToWorkspace(model.primaryCraft);
                                    hideWindow();
                                }
                            }
                            if (GUILayout.Button("Load", GUILayout.ExpandWidth(true), GUILayout.Width(bottomButtonsWidth)))
                            {
                                load();
                            }
                            drawCancelInBottomBar(bottomButtonsWidth);
                        }
                    }
                    else
                    {

                        ShowFolderButtons();

                        if (!newFolder)
                        {
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.FlexibleSpace();
                                drawCancelInBottomBar(bottomButtonsWidth);
                            }
                        }
                    }
                }
                if (extendedCancel)
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        if (displayQuestionAboutExtendedQuestion)
                        {
                            GUILayout.Label("Are you sure?");
                            if (GUILayout.Button("Discard"))
                            {
                                extendedCancel = false;
                                model.doNotWriteTagSettingsToDisk = true;
                                hideWindow();
                            }
                            if (GUILayout.Button("Cancel"))
                            {
                                displayQuestionAboutExtendedQuestion = false;
                                extendedCancel = false;
                            }
                        }
                        else
                        {
                            if (GUILayout.Button("Close & Discard all changes made in tags", GUILayout.ExpandWidth(false)))
                            {
                                displayQuestionAboutExtendedQuestion = true;
                            }
                        }
                    }
                }
            }
        }

        bool newFolder = false;
        string newFolderName;
        bool delFolder = false;
        int fileCnt;
        string delPrompt;
        void ShowFolderButtons()
        {
            GUILayout.FlexibleSpace();
            if (!delFolder)
            {
                if (!newFolder && GUILayout.Button("New Folder", GUILayout.Width(bottomButtonsWidth + 40)))
                {
                    newFolder = true;
                    newFolderName = "";
                }
            }
            if (newFolder)
            {
                GUILayout.Label("New Folder:", GUILayout.Width(bottomButtonsWidth + 40));
                newFolderName = GUILayout.TextField(newFolderName, GUILayout.Width(180));
                GUILayout.Space(5);
                if (GUILayout.Button("OK", RegisterToolbar.buttonStyleGreen, GUILayout.ExpandWidth(true), GUILayout.Width(bottomButtonsWidth)))
                {
                    DirectoryServices.MakeNewDir(model, newFolderName);
                    newFolder = false;
                    this.Reload();
                }
                GUILayout.Space(5);
                if (GUILayout.Button("Cancel", GUILayout.ExpandWidth(true), GUILayout.Width(bottomButtonsWidth)))
                {
                    newFolder = false;
                }
            }
            else
            {
                GUI.enabled = dirSelected;
                if (!delFolder)
                {
                    if (GUILayout.Button("Delete Folder", RegisterToolbar.buttonStyleOrange, GUILayout.Width(bottomButtonsWidth + 40)))
                    {
                        delFolder = true;
                        fileCnt = DirectoryServices.GetCountOfFilesInDir(model, selectedCraftName);

                        if (fileCnt == 0)
                            delPrompt = "OK to delete";
                        else
                            delPrompt = fileCnt.ToString() + " files in folder, OK to delete";
                    }
                }
                else
                {
                    if (GUILayout.Button("Cancel", GUILayout.ExpandWidth(true), GUILayout.Width(bottomButtonsWidth)))
                    {
                        delFolder = false;
                    }
                    GUILayout.Space(5);
                    if (GUILayout.Button(delPrompt, RegisterToolbar.buttonStyleOrange, GUILayout.ExpandWidth(true)))
                    {
                        DirectoryServices.DeleteDir(model, selectedCraftName);
                        delFolder = false;
                        this.Reload();
                    }

                }
                GUI.enabled = true;
            }
            GUILayout.FlexibleSpace();
        }
        public void load()
        {
            bool askSaveQuestion = editorListenerService.isModifiedSinceSave;
            bool askAlreadyExistsQuestion = model.isCraftAlreadyExists(model.primaryCraft);

            shouldCurrentCraftBeSavedWindow.okContinuation = () =>
            {

                craftAlreadyExistsQuestionWindow.craftName = model.primaryCraft.name;
                craftAlreadyExistsQuestionWindow.okContinuation = () =>
                {
                    model.loadCraftToWorkspace(model.primaryCraft);
                };

                if (askAlreadyExistsQuestion)
                {
                    craftAlreadyExistsQuestionWindow.displayWindow();
                }
                else
                {
                    craftAlreadyExistsQuestionWindow.okContinuation();
                }

            };

            if (askSaveQuestion)
            {
                shouldCurrentCraftBeSavedWindow.displayWindow();
            }
            else
            {
                shouldCurrentCraftBeSavedWindow.okContinuation();
            }
            hideWindow();
        }

        private void drawCancelInBottomBar(int bottomButtonsWidth)
        {
            if (GUILayout.Button("Cancel", GUILayout.ExpandWidth(true), GUILayout.Width(bottomButtonsWidth)))
            {
                hideWindow();
            }
            if (extendedCancel)
            {
                if (GUILayout.Button("<<", GUILayout.ExpandWidth(true)))
                {
                    extendedCancel = false;
                    displayQuestionAboutExtendedQuestion = false;
                }
            }
            else
            {
                if (GUILayout.Button(">>", GUILayout.ExpandWidth(true)))
                {
                    extendedCancel = true;
                    displayQuestionAboutExtendedQuestion = false;
                }
            }

        }

        override public void hideWindow()
        {
            base.hideWindow();
            model.writeAllDirtySettings();
            if (Settings.instance != null)
                Settings.instance.OnDestroy();
        }



        private void drawCraftTooltip()
        {
            if (GUI.tooltip != null && GUI.tooltip != "" && GUI.tooltip.StartsWith("!!craft"))
            {
                int craftIndex = int.Parse(GUI.tooltip.Substring("!!craft".Length));
                OrganizerCraftEntity tooltipCraft = model.filteredCrafts[craftIndex];

                //GUIStyle nameStyle = new GUIStyle(skin.label);
                //GUIStyle descStyle = new GUIStyle(skin.label);
                float tooltipTextWidth = 300;

                ParagraphBoxDrawer drawer = new ParagraphBoxDrawer(tooltipTextWidth);
                drawer.addParagraph(tooltipCraft.name, RegisterToolbar.nameStyle);
                string[] paragraphs = tooltipCraft.description.Split(new string[] { "\n", "¨¨" }, StringSplitOptions.None);
                foreach (string p in paragraphs)
                {
                    drawer.addParagraph(p.Substring(0, p.Length), RegisterToolbar.descStyle);
                }

#if false
                if (tooltipBackgroundStyle == null)
                {
                    tooltipBackgroundStyle = new GUIStyle(skin.box);
                    Texture2D backgroundTexture = new Texture2D(1, 1);
                    backgroundTexture.SetPixel(0, 0, new Color(0, 0, 0, 0.8f));
                    backgroundTexture.wrapMode = TextureWrapMode.Repeat;
                    backgroundTexture.Apply();
                    tooltipBackgroundStyle.normal.background = backgroundTexture;
                }
#endif
                float thumbSize = Math.Min(windowHeight / 3, 560);
                float thumbMargin = 10;
                Vector2 tooltipPos = Event.current.mousePosition + new Vector2(10, 10);
                Vector2 tooltipSize = new Vector2(tooltipTextWidth + thumbSize + thumbMargin * 2, Math.Max(drawer.contentSize.y, thumbSize + thumbMargin * 2));
                clampTooltipPosToWindow(ref tooltipPos, tooltipSize);

                if (new Rect(tooltipPos, tooltipSize).Contains(Event.current.mousePosition))
                {
                    tooltipPos = Event.current.mousePosition - tooltipSize - new Vector2(10, 10);
                    clampTooltipPosToWindow(ref tooltipPos, tooltipSize);
                }

                GUI.Box(new Rect(tooltipPos, tooltipSize), "", RegisterToolbar.tooltipBackgroundStyle);
                drawer.drawAt(tooltipPos);

                GUI.DrawTexture(new Rect(tooltipPos.x + tooltipTextWidth + thumbMargin, tooltipPos.y + thumbMargin, thumbSize, thumbSize), tooltipCraft.thumbTexture);
            }
        }

        private void clampTooltipPosToWindow(ref Vector2 tooltipPos, Vector2 tooltipSize)
        {
            if (tooltipPos.y + tooltipSize.y > windowHeight)
            {
                tooltipPos.y = windowHeight - tooltipSize.y - 10;
                if (tooltipPos.y < 10)
                {
                    tooltipPos.y = 10;
                }
            }
            if (tooltipPos.x + tooltipSize.x > windowWidth)
            {
                tooltipPos.x = windowWidth - tooltipSize.x - 10;
                if (tooltipPos.x < 10)
                {
                    tooltipPos.x = 10;
                }
            }
        }

    }
}

