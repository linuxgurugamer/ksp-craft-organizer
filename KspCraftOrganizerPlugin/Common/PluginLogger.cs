﻿using System;
using UnityEngine;

using static KspCraftOrganizer.RegisterToolbar;

namespace KspNalCommon
{
    public static class PluginLogger
    {
        public static string pluginPrefix = "<undefined-plugin>";
        private static bool debug_ = false;
        private static bool whileReadingSettings = false;

        public static void logTrace(object toLog)
        {
            if (debug)
            {
                Debug.Log("[" + pluginPrefix + "]" + toLog);
            }
        }

        public static bool debug
        {
            get
            {
                if (!whileReadingSettings && PluginCommons.instance != null)
                {
                    try
                    {
                        whileReadingSettings = true;
                        pluginPrefix = PluginCommons.instance.getPluginLogName();
                        debug_ = PluginCommons.instance.isDebug();
                    }
                    finally
                    {
                        whileReadingSettings = false;
                    }
                }
                return debug_;
            }
        }

        public static void logDebug(object toLog)
        {
            try
            {
                if (debug)
                {
                    Debug.LogWarning("[" + pluginPrefix + "]" + toLog);
                }
                else
                {
                    Debug.Log("[" + pluginPrefix + "]" + toLog);
                }
            }
            catch (Exception ex)
            {
                Debug.Log("Exception while logging");
                Debug.LogException(ex);
            }
        }

        internal static void logError(string toLog)
        {
            Debug.LogError("[" + pluginPrefix + "]" + toLog);
        }



        internal static void logError(string toLog, Exception ex)
        {
            logError(toLog);
            Debug.LogException(ex);
        }
    }
}

