
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using ClickThroughFix;

//
// HighLogic.CurrentGame.Parameters.CustomParams<KSPCO_Settings>().???
//

namespace KspCraftOrganizer
{
    public class KSPCO_Settings : GameParameters.CustomParameterNode
    {
        public override string Title { get { return "General"; } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override string Section { get { return "Craft Organizer"; } }
        public override string DisplaySection { get { return "Craft Organizer"; } }
        public override int SectionOrder { get { return 1; } }
        public override bool HasPresets { get { return false; } }


        [GameParameters.CustomParameterUI("Debug",
            toolTip = "Enables debug mode")]
        public bool debug = false;

        [GameParameters.CustomParameterUI("Replace editor load button",
            toolTip = "Replace the load button with this mod")]
        public bool replaceEditorLoadButton = true;

        [GameParameters.CustomParameterUI("Show KSP version for craft file")]
        public bool showVersion = false;

        [GameParameters.CustomParameterUI("Allow stock vessels to be used")]
        public bool allowStockVessels = false;
        

        [GameParameters.CustomParameterUI("Do backup after every change",
            toolTip = "Save after every edit is made")]
        public bool saveBackupAfterChange = false;

        [GameParameters.CustomParameterUI("Do backup after every save",
            toolTip = "Make a backup version when the vessel is saved")]
        public bool saveBackupAfterSave = true;

        [GameParameters.CustomParameterUI("Make backup at regular timed intervals",
            toolTip = "Timed backups, will only make a backup if any changes have been made since the last one")]
        public bool timedBackups = true;

        [GameParameters.CustomFloatParameterUI("Timed backup intervals (minutes)", minValue = 1f, maxValue = 30.0f,
            toolTip = "How often a timed backup will be done")]
        public double backupInterval = 5f;

        [GameParameters.CustomIntParameterUI("Maximum number of backups", minValue =  5, maxValue = 1000, stepSize = 1,
            toolTip="This is for each vessel, there is no  limit on how many vessels you can have")]
        public int maxBackups = 10;

        public override void SetDifficultyPreset(GameParameters.Preset preset)
        {
        }

        public override bool Enabled(MemberInfo member, GameParameters parameters)
        {
            if (saveBackupAfterChange &&
                (member.Name == "timedBackups" || member.Name == "backupInterval" || member.Name== "saveBackupAfterSave"))
                return false;
            if (!timedBackups && member.Name == "backupInterval")
                return false;
            return true;
        }

        public override bool Interactible(MemberInfo member, GameParameters parameters)
        {
            return true;
        }
    }
}
