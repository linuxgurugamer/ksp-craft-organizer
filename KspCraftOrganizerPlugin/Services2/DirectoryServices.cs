using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using KspNalCommon;

using static KspCraftOrganizer.RegisterToolbar;

namespace KspCraftOrganizer
{
    public class DirectoryServices
    {
        internal static List<string> dirStack = new List<string>();
        static private FileLocationService fileLocationService = FileLocationService.instance;

        static public string curDir
        {
            get
            {
                string curDir = "";
                foreach (var d in dirStack)
                {
                    curDir = curDir + "/" + d;
                }
                return curDir;
            }
        }

        static public void PushDir(string dir)
        {
            dirStack.Add(dir);
        }
        static public void PopDir()
        {
            if (dirStack.Count > 0)
                dirStack.RemoveAt(dirStack.Count - 1);
        }
        static public void PopAll()
        {
            dirStack.Clear();
        }

        static internal string FixSeparators(string oldDir)
        {
            var dir = oldDir.Replace('\\', '/');
            if (Path.DirectorySeparatorChar != '/')
                dir = dir.Replace('/', Path.DirectorySeparatorChar);
            return dir;
        }

        static public void MakeNewDir(OrganizerController model, string newDirName)
        {
            string craftDirectory = fileLocationService.getCraftDirectoryForCraftType(model.currentSave, model.craftType);
            var dir = FixSeparators(craftDirectory + Path.DirectorySeparatorChar + curDir);

            if (Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir + Path.DirectorySeparatorChar + newDirName);
            }
        }

        internal const string historyDirectory = "_Backup";

#if false
        static public string MakeNewHistoryDir(OrganizerController model)
        {
            IKspAl ksp = IKspAlProvider.instance;
            string craftDirectory = fileLocationService.getCraftDirectoryForCraftType(ksp.getNameOfSaveFolder(), model.craftType);
            var dir = FixSeparators(craftDirectory + Path.DirectorySeparatorChar + curDir + historyDirectory);

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return dir;
        }
#endif

        static public string MakeNewHistoryDir(EditorFacility facility)
        {
            IKspAl ksp = IKspAlProvider.instance;
            CraftType type = CraftType.SUBASSEMBLY;

            switch (facility)
            {
                case EditorFacility.None: type = CraftType.SUBASSEMBLY; break;
                case EditorFacility.VAB: type = CraftType.VAB; break;
                case EditorFacility.SPH: type = CraftType.SPH; break;
            }

            string craftDirectory = fileLocationService.getCraftDirectoryForCraftType(ksp.getNameOfSaveFolder(), type);
            var dir = FixSeparators(craftDirectory + Path.DirectorySeparatorChar + curDir + EditorListenerService.GetShipName() + historyDirectory);

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return dir;
        }


        static public int GetCountOfFilesInDir(OrganizerController model, string dirName)
        {
            string craftDirectory = fileLocationService.getCraftDirectoryForCraftType(model.currentSave, model.craftType);
            string dir = FixSeparators(craftDirectory + Path.DirectorySeparatorChar + curDir + Path.DirectorySeparatorChar + dirName);
            if (Directory.Exists(dir))
            {
                int fileCount = Directory.GetFiles(dir, "*.craft", SearchOption.AllDirectories).Length;
                return fileCount;
            }
            return 0;
        }
        static public void DeleteDir(OrganizerController model, string dirName)
        {
            string craftDirectory = fileLocationService.getCraftDirectoryForCraftType(model.currentSave, model.craftType);
            string dir = FixSeparators(craftDirectory + Path.DirectorySeparatorChar + curDir + Path.DirectorySeparatorChar + dirName);

            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }
    }
}
