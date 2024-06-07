﻿using HarmonyLib;
using RimWorld.Planet;
using System.Linq;
using Verse;
using Verse.AI;

namespace GameClient
{
    [HarmonyPatch(typeof(SettlementDefeatUtility), "CheckDefeated")]
    public static class PatchSettlementJoin
    {
        [HarmonyPrefix]
        public static bool DoPre(Settlement factionBase)
        {
            if (Network.state == NetworkState.Disconnected) return true;

            if (FactionValues.playerFactions.Contains(factionBase.Faction)) return false;

            else return true;
        }
    }

    [HarmonyPatch(typeof(Pawn_JobTracker), "StartJob")]
    public static class PatchJobInformation
    {
        [HarmonyPrefix]
        public static bool DoPre(Job newJob, Pawn ___pawn)
        {
            if (Network.state == NetworkState.Connected)
            {
                if (ClientValues.isInVisit)
                {
                    if (OnlineVisitManager.nonFactionPawns.Contains(___pawn))
                    {
                        if (newJob.exitMapOnArrival) return false;
                    }
                }
            }

            return true;
        }
    }
}
