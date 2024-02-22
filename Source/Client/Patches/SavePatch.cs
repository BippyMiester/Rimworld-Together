﻿using System;
using HarmonyLib;
using RimWorld;
using Verse;
using GameClient;

namespace GameClient
{
    [HarmonyPatch(typeof(GameDataSaveLoader), "SaveGame", typeof(string))]
    public static class SaveOnlineGame
    {
        [HarmonyPrefix]
        public static bool DoPre(ref string fileName, ref int ___lastSaveTick)
        {
            if (!Network.isConnectedToServer) return true;
            else
            {
                ClientValues.ForcePermadeath();
                ClientValues.ManageDevOptions();
                CustomDifficultyManager.EnforceCustomDifficulty();

                SaveManager.customSaveName = $"Server - {Network.ip} - {ChatManager.username}";
                fileName = SaveManager.customSaveName;

                try
                {
                    SafeSaver.Save(GenFilePaths.FilePathForSavedGame(fileName), "savegame", delegate
                    {
                        ScribeMetaHeaderUtility.WriteMetaHeader();
                        Game target = Current.Game;
                        Scribe_Deep.Look(ref target, "game", new object[0]);
                    }, Find.GameInfo.permadeathMode);
                    ___lastSaveTick = Find.TickManager.TicksGame;
                }
                catch (Exception ex) { Logs.Error("Exception while saving game: " + ex); }

                //send map data so players can offline visit/raid
                Logs.Message($"[Rimworld Together] > Sending maps to server");
                MapManager.SendMapsToServer();

                //send Local Save file to server
                Logs.Message($"[Rimworld Together] > Sending local save to server");
                SaveManager.SendSavePartToServer(fileName);

                return false;
            }
        }
    }

    [HarmonyPatch(typeof(Autosaver), "AutosaverTick")]
    public static class Autosave
    {
        [HarmonyPrefix]
        public static bool DoPre()
        {
            if (!Network.isConnectedToServer) return true;
            else
            {
                ClientValues.autosaveCurrentTicks++;
                if (ClientValues.autosaveCurrentTicks >= ClientValues.autosaveInternalTicks && !GameDataSaveLoader.SavingIsTemporarilyDisabled)
                {
                    SaveManager.ForceSave();

                    ClientValues.autosaveCurrentTicks = 0;
                }

                return false;
            }
        }
    }
}
