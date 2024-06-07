﻿using Shared;
using static Shared.CommonEnumerators;
using System.Linq.Expressions;

namespace GameServer
{
    public static class SaveManager
    {
        //Variables

        public readonly static string fileExtension = ".mpsave";
        private readonly static string tempFileExtension = ".mpsavetemp";

        public static void ReceiveSavePartFromClient(ServerClient client, Packet packet)
        {
            string baseClientSavePath = Path.Combine(Master.savesPath, client.username + fileExtension);
            string tempClientSavePath = Path.Combine(Master.savesPath, client.username + tempFileExtension);

            FileTransferData fileTransferData = (FileTransferData)Serializer.ConvertBytesToObject(packet.contents);

            //if this is the first packet
            if (client.listener.downloadManager == null)
            {
                client.listener.downloadManager = new DownloadManager();
                client.listener.downloadManager.PrepareDownload(tempClientSavePath, fileTransferData.fileParts);
            }

            client.listener.downloadManager.WriteFilePart(fileTransferData.fileBytes);

            //if this is the last packet
            if (fileTransferData.isLastPart)
            {
                client.listener.downloadManager.FinishFileWrite();
                client.listener.downloadManager = null;

                byte[] completedSave = File.ReadAllBytes(tempClientSavePath);
                File.WriteAllBytes(baseClientSavePath, completedSave);
                File.Delete(tempClientSavePath);

                OnUserSave(client, fileTransferData);
            }

            else
            {
                Packet rPacket = Packet.CreatePacketFromJSON(nameof(PacketHandler.RequestSavePartPacket));
                client.listener.EnqueuePacket(rPacket);
            }
        }

        public static void SendSavePartToClient(ServerClient client)
        {
            string baseClientSavePath = Path.Combine(Master.savesPath, client.username + fileExtension);
            string tempClientSavePath = Path.Combine(Master.savesPath, client.username + tempFileExtension);

            //if this is the first packet
            if (client.listener.uploadManager == null)
            {
                Logger.WriteToConsole($"[Load save] > {client.username} | {client.SavedIP}");

                client.listener.uploadManager = new UploadManager();
                client.listener.uploadManager.PrepareUpload(baseClientSavePath);
            }

            FileTransferData fileTransferData = new FileTransferData();
            fileTransferData.fileSize = client.listener.uploadManager.fileSize;
            fileTransferData.fileParts = client.listener.uploadManager.fileParts;
            fileTransferData.fileBytes = client.listener.uploadManager.ReadFilePart();
            fileTransferData.isLastPart = client.listener.uploadManager.isLastPart;
            if(!Master.serverConfig.SyncLocalSave) fileTransferData.instructions = (int)SaveMode.Strict;

            Packet packet = Packet.CreatePacketFromJSON(nameof(PacketHandler.ReceiveSavePartPacket), fileTransferData);
            client.listener.EnqueuePacket(packet);

            //if this is the last packet
            if (client.listener.uploadManager.isLastPart)
                client.listener.uploadManager = null;
        }

        private static void OnUserSave(ServerClient client, FileTransferData fileTransferData)
        {
            if (fileTransferData.instructions == (int)SaveMode.Disconnect)
            {
                client.listener.disconnectFlag = true;
                Logger.WriteToConsole($"[Save game] > {client.username} > Disconnect");
            }
            else Logger.WriteToConsole($"[Save game] > {client.username} > Autosave");
        }

        public static bool CheckIfUserHasSave(ServerClient client)
        {
            string[] saves = Directory.GetFiles(Master.savesPath);
            foreach(string save in saves)
            {
                if (!save.EndsWith(fileExtension)) continue;
                if (Path.GetFileNameWithoutExtension(save) == client.username) return true;
            }

            return false;
        }

        public static byte[] GetUserSaveFromUsername(string username)
        {
            string[] saves = Directory.GetFiles(Master.savesPath);
            foreach (string save in saves)
            {
                if (!save.EndsWith(fileExtension)) continue;
                if (Path.GetFileNameWithoutExtension(save) == username) return File.ReadAllBytes(save);
            }

            return null;
        }

        public static void ResetClientSave(ServerClient client)
        {
            if (!CheckIfUserHasSave(client))
            {
                ResponseShortcutManager.SendIllegalPacket(client, $"Player {client.username}'s save was attempted to be reset while the player doesn't have a save");
                return;
            }
            client.listener.disconnectFlag = true;

            //Locate and make sure there's no other backup save in the server
            string playerArchivedSavePath = Path.Combine(Master.archivedSavesPath, client.username);
            if (Directory.Exists(playerArchivedSavePath)) Directory.Delete(playerArchivedSavePath,true);
            Directory.CreateDirectory(playerArchivedSavePath);

            //Assign save paths to the backup files
            string mapsArchivePath = Path.Combine(playerArchivedSavePath, "Maps");
            string savesArchivePath = Path.Combine(playerArchivedSavePath, "Saves");
            string sitesArchivePath = Path.Combine(playerArchivedSavePath, "Sites");
            string settlementsArchivePath = Path.Combine(playerArchivedSavePath, "Settlements");

            //Create directories for the backup files
            Directory.CreateDirectory(mapsArchivePath);
            Directory.CreateDirectory(savesArchivePath);
            Directory.CreateDirectory(sitesArchivePath);
            Directory.CreateDirectory(settlementsArchivePath);

            //Copy save file to archive
            try { File.Copy(Path.Combine(Master.savesPath, client.username + fileExtension), Path.Combine(savesArchivePath , client.username + fileExtension)); }
            catch { Logger.Warning($"Failed to find {client.username}'s save"); }

            //Copy map files to archive
            MapFileData[] userMaps = MapManager.GetAllMapsFromUsername(client.username);
            foreach (MapFileData map in userMaps)
            {
                File.Copy(Path.Combine(Master.mapsPath, map.mapTile + MapManager.fileExtension), 
                    Path.Combine(mapsArchivePath, map.mapTile + MapManager.fileExtension));
            }

            //Copy site files to archive
            SiteFile[] playerSites = SiteManager.GetAllSitesFromUsername(client.username);
            foreach (SiteFile site in playerSites)
            {
                File.Copy(Path.Combine(Master.sitesPath, site.tile + SiteManager.fileExtension), 
                    Path.Combine(sitesArchivePath, site.tile + SiteManager.fileExtension));
            }

            //Copy settlement files to archive
            SettlementFile[] playerSettlements = SettlementManager.GetAllSettlementsFromUsername(client.username);
            foreach (SettlementFile settlementFile in playerSettlements)
            {
                File.Copy(Path.Combine(Master.settlementsPath, settlementFile.tile + SettlementManager.fileExtension), 
                    Path.Combine(settlementsArchivePath, settlementFile.tile + SettlementManager.fileExtension));
            }

            DeletePlayerData(client.username, client);
        }

        public static void DeletePlayerData(string username, ServerClient extraClientDetails = null)
        {
            ServerClient connectedUser = UserManager.GetConnectedClientFromUsername(username);
            if (connectedUser != null) connectedUser.listener.disconnectFlag = true;

            //Delete save file
            try { File.Delete(Path.Combine(Master.savesPath, username + fileExtension)); }
            catch { Logger.Warning($"Failed to find {username}'s save"); }

            //Delete map files
            MapFileData[] userMaps = MapManager.GetAllMapsFromUsername(username);
            foreach (MapFileData map in userMaps) MapManager.DeleteMap(map);

            //Delete site files
            SiteFile[] playerSites = SiteManager.GetAllSitesFromUsername(username);
            foreach (SiteFile site in playerSites) SiteManager.DestroySiteFromFile(site);

            //Delete settlement files
            SettlementFile[] playerSettlements = SettlementManager.GetAllSettlementsFromUsername(username);
            foreach (SettlementFile settlementFile in playerSettlements)
            {
                SettlementData settlementData = new SettlementData();
                settlementData.tile = settlementFile.tile;
                settlementData.owner = settlementFile.owner;

                SettlementManager.RemoveSettlement(extraClientDetails, settlementData, true);
            }

            Logger.WriteToConsole($"[Deleted player data] > {username}", LogMode.Warning);
        }
    }
}
