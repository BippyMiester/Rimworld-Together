﻿using System.Linq;
using System;
using Shared;
using NAudio.Codecs;
using Verse;

namespace GameClient
{
    public static class DialogShortcuts
    {
        public static void ShowRegisteredDialog()
        {
            DialogManager.PopDialog();

            RT_Dialog_OK_Loop d1 = new RT_Dialog_OK_Loop(new string[] { "You have been successfully registered!",
                "You are now able to login using your new account"});

            DialogManager.PushNewDialog(d1);
        }

        public static void ShowLoginOrRegisterDialogs()
        {
            RT_Dialog_3Input a1 = new RT_Dialog_3Input(
                "New User",
                "Username",
                "Password",
                "Confirm Password",
                delegate { ParseConnectionDetails(true); },
                DialogManager.PopDialog ,
                false, true, true);

            RT_Dialog_2Input a2 = new RT_Dialog_2Input(
                "Existing User",
                "Username",
                "Password",
                delegate { ParseLoginUser(); },
                DialogManager.PopDialog,
                false, true);

            RT_Dialog_2Button d1 = new RT_Dialog_2Button(
                "Login Select",
                "Choose your login type",
                "New User",
                "Existing User",
                delegate { DialogManager.PushNewDialog(a1); },
                delegate {
                    DialogManager.PushNewDialog(a2);
                    PreferenceManager.LoadLoginDetails();
                },
                delegate { Network.listener.disconnectFlag = true; });

            DialogManager.PushNewDialog(d1);
        }

        public static void ShowWorldGenerationDialogs()
        {
            RT_Dialog_OK d3 = new RT_Dialog_OK("This feature is not implemented yet!",
                delegate { DialogManager.PushNewDialog(DialogManager.previousDialog); });

            RT_Dialog_2Button d2 = new RT_Dialog_2Button("Game Mode", "Choose the way you want to play",
                "Separate colony", "Together with other players (TBA)", null, delegate { DialogManager.PushNewDialog(d3); },
                delegate { DisconnectionManager.RestartGame(true); });

            RT_Dialog_OK_Loop d1 = new RT_Dialog_OK_Loop(new string[] { "Welcome to the world view!",
                        "Please choose the way you would like to play", "This mode can't be changed upon choosing!" },
                delegate { DialogManager.PushNewDialog(d2); });

            DialogManager.PushNewDialog(d1);
        }

        public static void ShowConnectDialogs()
        {
            RT_Dialog_ListingWithButton a1 = new RT_Dialog_ListingWithButton("Server Browser", "List of reachable servers",
                ClientValues.serverBrowserContainer,
                delegate { ParseConnectionDetails(true); },
                delegate { DialogManager.PushNewDialog(DialogManager.previousDialog); });

            RT_Dialog_2Input a2 = new RT_Dialog_2Input(
                "Connection Details",
                "IP",
                "Port",
                delegate { ParseConnectionDetails(false); },
                delegate { DialogManager.PushNewDialog(DialogManager.previousDialog); });

            RT_Dialog_2Button newDialog = new RT_Dialog_2Button(
                "Play Online",
                "Choose the connection type",
                "Server Browser",
                "Direct Connect",
                delegate { DialogManager.PushNewDialog(a1); },
                delegate {
                    DialogManager.PushNewDialog(a2);
                    PreferenceManager.LoadConnectionDetails();
                }, null);

            DialogManager.PushNewDialog(newDialog);
        }

        public static void ParseConnectionDetails(bool throughBrowser)
        {
            bool isInvalid = false;

            string[] answerSplit = null;
            if (throughBrowser)
            {
                answerSplit = ClientValues.serverBrowserContainer[(int)DialogManager.inputCache[0]].Split('|');

                if (string.IsNullOrWhiteSpace(answerSplit[0])) isInvalid = true;
                if (string.IsNullOrWhiteSpace(answerSplit[1])) isInvalid = true;
                if (answerSplit[1].Count() > 5) isInvalid = true;
                if (!answerSplit[1].All(Char.IsDigit)) isInvalid = true;
            }

            else
            {
                if (string.IsNullOrWhiteSpace((string)DialogManager.inputCache[0])) isInvalid = true;
                if (string.IsNullOrWhiteSpace((string)DialogManager.inputCache[1])) isInvalid = true;
                if (((string)DialogManager.inputCache[0]).Count() > 5) isInvalid = true;
                if (!((string)DialogManager.inputCache[1]).All(Char.IsDigit)) isInvalid = true;
            }

            if (!isInvalid)
            {
                if (throughBrowser)
                {
                    Network.ip = answerSplit[0];
                    Network.port = answerSplit[1];
                    PreferenceManager.SaveConnectionDetails(answerSplit[0], answerSplit[1]);
                }

                else
                {
                    Network.ip = ((string)DialogManager.inputCache[0]);
                    Network.port = ((string)DialogManager.inputCache[0]);
                    PreferenceManager.SaveConnectionDetails(((string)DialogManager.inputCache[0]), ((string)DialogManager.inputCache[0]));
                }

                DialogManager.PushNewDialog(new RT_Dialog_Wait("Trying to connect to server"));
                Network.StartConnection();
            }

            else
            {
                RT_Dialog_Error d1 = new RT_Dialog_Error("Server details are invalid! Please try again!");
                DialogManager.PushNewDialog(d1);
            }
        }

        public static void ParseLoginUser()
        {
            bool isInvalid = false;
            if (string.IsNullOrWhiteSpace(((string)DialogManager.inputCache[0]))) isInvalid = true;
            if (((string)DialogManager.inputCache[0]).Any(Char.IsWhiteSpace)) isInvalid = true;
            if (string.IsNullOrWhiteSpace(((string)DialogManager.inputCache[0]))) isInvalid = true;

            if (!isInvalid)
            {
                JoinDetailsJSON loginDetails = new JoinDetailsJSON();
                loginDetails.username = (string)DialogManager.inputCache[0];
                loginDetails.password = Hasher.GetHashFromString((string)DialogManager.inputCache[1]);
                loginDetails.clientVersion = CommonValues.executableVersion;
                loginDetails.runningMods = ModManager.GetRunningModList().ToList();

                ChatManager.username = loginDetails.username;
                PreferenceManager.SaveLoginDetails(((string)DialogManager.inputCache[0]), ((string)DialogManager.inputCache[0]));

                Packet packet = Packet.CreatePacketFromJSON("LoginClientPacket", loginDetails);
                Network.listener.dataQueue.Enqueue(packet);

                DialogManager.PushNewDialog(new RT_Dialog_Wait("Waiting for login response"));
            }

            else
            {
                RT_Dialog_Error d1 = new RT_Dialog_Error("Login details are invalid! Please try again!",
                    delegate { DialogManager.PushNewDialog(DialogManager.previousDialog); });

                DialogManager.PushNewDialog(d1);
            }
        }

        public static void ParseRegisterUser()
        {
            bool isInvalid = false;
            if (string.IsNullOrWhiteSpace(((string)DialogManager.inputCache[0]))) isInvalid = true;
            if (((string)DialogManager.inputCache[0]).Any(Char.IsWhiteSpace)) isInvalid = true;
            if (string.IsNullOrWhiteSpace(((string)DialogManager.inputCache[1]))) isInvalid = true;
            if (string.IsNullOrWhiteSpace(((string)DialogManager.inputCache[2]))) isInvalid = true;
            if (((string)DialogManager.inputCache[1]) != ((string)DialogManager.inputCache[2])) isInvalid = true;

            if (!isInvalid)
            {
                JoinDetailsJSON registerDetails = new JoinDetailsJSON();
                registerDetails.username = (string)DialogManager.inputCache[0];
                registerDetails.password = Hasher.GetHashFromString((string)DialogManager.inputCache[1]);
                registerDetails.clientVersion = CommonValues.executableVersion;
                registerDetails.runningMods = ModManager.GetRunningModList().ToList();

                Packet packet = Packet.CreatePacketFromJSON("RegisterClientPacket", registerDetails);

                Logs.Message("attempting to send parse register data");
                Network.listener.dataQueue.Enqueue(packet);
                Logs.Message("sent parse register data");
                DialogManager.PushNewDialog(new RT_Dialog_Wait("Waiting for register response"));
            }

            else
            {
                RT_Dialog_Error d1 = new RT_Dialog_Error("Register details are invalid! Please try again!",
                    delegate { DialogManager.PushNewDialog(DialogManager.previousDialog); });

                DialogManager.PushNewDialog(d1);
            }
        }


        //changes in a textField are check based on string length, but if the contents of a text field are replaced,
        //i.e. 1234 -> 1255 where 34 are instantly replace with 55
        //we can't tell anything has changed on length. This function will change the characters that have been repalced
        public static string replaceNonCensoredSymbols(string recievingString, string giftingString, bool Censored, string censorSymbol)
        {
            string StringA = recievingString; string currCharA;
            string StringB = giftingString; string currCharB;
            string returnString = "";
            if (Censored)
            {
                for (int i = 0; i < giftingString.Length; i++)
                {
                    currCharA = StringA.Substring(0, 1);
                    currCharB = StringB.Substring(0, 1);
                    if (StringA.Length > 0) StringA = StringA.Substring(1, StringA.Length - 1);
                    if (StringB.Length > 0) StringB = StringB.Substring(1, StringB.Length - 1);
                    if (currCharB.ToString() == censorSymbol)
                        returnString += currCharA;
                    else
                        returnString += currCharB;
                }
            }
            else
            {
                for (int i = 0; i < giftingString.Length; i++)
                {
                    currCharA = StringA.Substring(0, 1);
                    currCharB = StringB.Substring(0, 1);
                    if (StringA.Length > 0) StringA = StringA.Substring(1, StringA.Length - 1);
                    if (StringB.Length > 0) StringB = StringB.Substring(1, StringB.Length - 1);
                    if (currCharA == currCharB)
                        returnString += currCharA;
                    else
                        returnString += currCharB;
                }
            }
            return returnString;
        }
    }
}
