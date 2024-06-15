﻿using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Shared;
using Verse;
using static Shared.CommonEnumerators;

namespace GameClient
{
    //Class that handles all the planet functions for the mod

    public static class PlanetManager
    {
        public static List<Settlement> playerSettlements = new List<Settlement>();

        public static List<Site> playerSites = new List<Site>();

        //Parses the required packet into a useful order

        public static void ParseSettlementPacket(Packet packet)
        {
            SettlementData settlementData = (SettlementData)Serializer.ConvertBytesToObject(packet.contents);

            switch (settlementData.settlementStepMode)
            {
                case SettlementStepMode.Add:
                    SpawnSingleSettlement(settlementData);
                    break;

                case SettlementStepMode.Remove:
                    RemoveSingleSettlement(settlementData);
                    break;
            }
        }

        //Regenerates the planet of player objects

        public static void BuildPlanet()
        {
            FactionValues.FindPlayerFactionsInWorld();
            PlanetManagerHelper.GetMapGenerators();

            RemoveOldSettlements();
            RemoveOldSites();

            SpawnPlayerSettlements();
            SpawnPlayerSites();
        }

        //Removes old player settlements

        private static void RemoveOldSettlements()
        {
            playerSettlements.Clear();

            DestroyedSettlement[] destroyedSettlements = Find.WorldObjects.DestroyedSettlements.ToArray();
            foreach (DestroyedSettlement destroyedSettlement in destroyedSettlements)
            {
                Find.WorldObjects.Remove(destroyedSettlement);
            }

            Settlement[] settlements = Find.WorldObjects.Settlements.ToArray();
            foreach (Settlement settlement in settlements)
            {
                if (settlement.Faction == FactionValues.allyPlayer)
                {
                    Find.WorldObjects.Remove(settlement);
                    continue;
                }

                if (settlement.Faction == FactionValues.neutralPlayer)
                {
                    Find.WorldObjects.Remove(settlement);
                    continue;
                }

                if (settlement.Faction == FactionValues.enemyPlayer)
                {
                    Find.WorldObjects.Remove(settlement);
                    continue;
                }

                if (settlement.Faction == FactionValues.yourOnlineFaction)
                {
                    Find.WorldObjects.Remove(settlement);
                    continue;
                }
            }
        }

        //Spawns player settlements

        private static void SpawnPlayerSettlements()
        {
            for (int i = 0; i < PlanetManagerHelper.tempSettlements.Count(); i++)
            {
                OnlineSettlementFile settlementFile = PlanetManagerHelper.tempSettlements[i];

                try
                {
                    Settlement settlement = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
                    settlement.Tile = settlementFile.tile;
                    settlement.Name = $"{settlementFile.owner}'s settlement";
                    settlement.SetFaction(PlanetManagerHelper.GetPlayerFaction(settlementFile.goodwill));

                    playerSettlements.Add(settlement);
                    Find.WorldObjects.Add(settlement);
                }
                catch (Exception e) { Logger.Error($"Failed to build settlement at {settlementFile.tile}. Reason: {e}"); }
            }
        }

        //Removes old player sites

        private static void RemoveOldSites()
        {
            playerSites.Clear();

            Site[] sites = Find.WorldObjects.Sites.ToArray();
            foreach (Site site in sites)
            {
                if (site.Faction == FactionValues.enemyPlayer)
                {
                    Find.WorldObjects.Remove(site);
                    continue;
                }

                if (site.Faction == FactionValues.neutralPlayer)
                {
                    Find.WorldObjects.Remove(site);
                    continue;
                }

                if (site.Faction == FactionValues.allyPlayer)
                {
                    Find.WorldObjects.Remove(site);
                    continue;
                }

                if (site.Faction == FactionValues.yourOnlineFaction)
                {
                    Find.WorldObjects.Remove(site);
                    continue;
                }

                if (site.Faction == Faction.OfPlayer)
                {
                    Find.WorldObjects.Remove(site);
                    continue;
                }
            }
        }

        //Spawns player sites

        private static void SpawnPlayerSites()
        {
            for (int i = 0; i < PlanetManagerHelper.tempSites.Count(); i++)
            {
                OnlineSiteFile siteFile = PlanetManagerHelper.tempSites[i];

                try
                {
                    SitePartDef siteDef = SiteManager.GetDefForNewSite(siteFile.type, siteFile.fromFaction);
                    Site site = SiteMaker.MakeSite(sitePart: siteDef,
                        tile: siteFile.tile,
                        threatPoints: 1000,
                        faction: PlanetManagerHelper.GetPlayerFaction(siteFile.goodwill));

                    playerSites.Add(site);
                    Find.WorldObjects.Add(site);
                }
                catch (Exception e) { Logger.Error($"Failed to spawn site at {siteFile.tile}. Reason: {e}"); }
            }
        }

        //Spawns a player settlement from a request

        public static void SpawnSingleSettlement(SettlementData newSettlementJSON)
        {
            if (ClientValues.isReadyToPlay)
            {
                try
                {
                    Settlement settlement = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
                    settlement.Tile = newSettlementJSON.tile;
                    settlement.Name = $"{newSettlementJSON.owner}'s settlement";
                    settlement.SetFaction(PlanetManagerHelper.GetPlayerFaction(newSettlementJSON.goodwill));

                    playerSettlements.Add(settlement);
                    Find.WorldObjects.Add(settlement);
                }
                catch (Exception e) { Logger.Error($"Failed to spawn settlement at {newSettlementJSON.tile}. Reason: {e}"); }
            }
        }

        //Removes a player settlement from a request

        public static void RemoveSingleSettlement(SettlementData newSettlementJSON)
        {
            if (ClientValues.isReadyToPlay)
            {
                try
                {
                    Settlement toGet = playerSettlements.Find(x => x.Tile == newSettlementJSON.tile);

                    playerSettlements.Remove(toGet);
                    Find.WorldObjects.Remove(toGet);
                }
                catch (Exception e) { Logger.Error($"Failed to remove settlement at {newSettlementJSON.tile}. Reason: {e}"); }
            }
        }

        //Spawns a player site from a request

        public static void SpawnSingleSite(SiteData siteData)
        {
            if (ClientValues.isReadyToPlay)
            {
                try
                {
                    SitePartDef siteDef = SiteManager.GetDefForNewSite(siteData.type,siteData.isFromFaction);
                    Site site = SiteMaker.MakeSite(sitePart: siteDef,
                        tile: siteData.tile,
                        threatPoints: 1000,
                        faction: PlanetManagerHelper.GetPlayerFaction(siteData.goodwill));

                    playerSites.Add(site);
                    Find.WorldObjects.Add(site);
                }
                catch (Exception e) { Logger.Error($"Failed to spawn site at {siteData.tile}. Reason: {e}"); }
            }
        }

        //Removes a player site from a request

        public static void RemoveSingleSite(SiteData siteData)
        {
            if (ClientValues.isReadyToPlay)
            {
                try
                {
                    Site toGet = playerSites.Find(x => x.Tile == siteData.tile);

                    playerSites.Remove(toGet);
                    Find.WorldObjects.Remove(toGet);
                }
                catch (Exception e) { Logger.Message($"Failed to remove site at {siteData.tile}. Reason: {e}"); }
            }
        }
    }

    //Helper class for the PlanetManager class

    public static class PlanetManagerHelper
    {
        public static OnlineSettlementFile[] tempSettlements;
        public static OnlineSiteFile[] tempSites;

        public static MapGeneratorDef emptyGenerator;
        public static MapGeneratorDef defaultSettlementGenerator;
        public static MapGeneratorDef defaultSiteGenerator;

        public static void SetWorldFeatures(ServerGlobalData serverGlobalData)
        {
            tempSettlements = serverGlobalData.settlements;
            tempSites = serverGlobalData.sites;
        }

        //Returns an online faction depending on the value

        public static Faction GetPlayerFaction(Goodwill goodwill)
        {
            Faction factionToUse = null;

            switch (goodwill)
            {
                case Goodwill.Enemy:
                    factionToUse = FactionValues.enemyPlayer;
                    break;

                case Goodwill.Neutral:
                    factionToUse = FactionValues.neutralPlayer;
                    break;

                case Goodwill.Ally:
                    factionToUse = FactionValues.allyPlayer;
                    break;

                case Goodwill.Faction:
                    factionToUse = FactionValues.yourOnlineFaction;
                    break;

                case Goodwill.Personal:
                    factionToUse = Faction.OfPlayer;
                    break;
            }

            return factionToUse;
        }

        //Gets the default generator for the map builder

        public static void GetMapGenerators()
        {
            emptyGenerator = DefDatabase<MapGeneratorDef>.AllDefs.First(fetch => fetch.defName == "Empty");

            WorldObjectDef settlement = WorldObjectDefOf.Settlement;
            defaultSettlementGenerator = settlement.mapGenerator;

            WorldObjectDef site = WorldObjectDefOf.Site;
            defaultSiteGenerator = site.mapGenerator;
        }

        //Sets the default generator for the map builder

        public static void SetDefaultGenerators()
        {
            WorldObjectDef settlement = WorldObjectDefOf.Settlement;
            settlement.mapGenerator = defaultSettlementGenerator;

            WorldObjectDef site = WorldObjectDefOf.Site;
            site.mapGenerator = defaultSiteGenerator;
        }

        public static void SetOverrideGenerators()
        {
            WorldObjectDef settlement = WorldObjectDefOf.Settlement;
            settlement.mapGenerator = emptyGenerator;

            WorldObjectDef site = WorldObjectDefOf.Site;
            site.mapGenerator = emptyGenerator;
        }
    }
}
