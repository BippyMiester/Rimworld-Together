using Fleck;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace GameServer
{
    public class Rcon
    {
        // List of connected and authenticated clients
        public static List<IWebSocketConnection> authenticatedClients = new List<IWebSocketConnection>();

        // Entry point for the Rcon class
        public static void StartServer(Dictionary<string, string> args)
        {
            Logger.Message("Gameserver.Rcon.StartServer");
            foreach (var kvp in args)
            {
                Console.WriteLine($"Key: {kvp.Key}, Value: {kvp.Value}");
            }
            Logger.Message($"Server RCON Password: {args["server.rconpassword"]}");
            Logger.Message($"Server RCON Port: {args["server.rconport"]}");
            // Gets the IPv4 address of the server
            string ipAddress = GetFirstIpAddress();
            if (ipAddress == "No IPv4 Address Found" || ipAddress.StartsWith("Error"))
            {
                Logger.Error("Failed to resolve a valid IP address. Check network interfaces.");
            }
            else
            {
                Logger.Debug($"Resolved IP Address: {ipAddress}");
            }
            
            // Check to see if the port is available
            if (!IsPortAvailable(int.Parse(args["server.rconport"])))
            {
                Logger.Error($"Port {args["server.rconport"]} is already in use.");
                return;
            }
            
            // The entire IP address and port
            string serverAddress = $"ws://{ipAddress}:{args["server.rconport"]}";
            Logger.Debug($"Attempting to bind WebSocketServer to {serverAddress}");

            // Start the WebSocket server
            var server = new WebSocketServer(serverAddress);
            server.RestartAfterListenError = true;
            server.Start(socket =>
            {
                socket.OnOpen = () => OnClientConnected(socket, args);
                socket.OnMessage = message => OnMessageReceived(socket, message);
                socket.OnClose = () => OnClientDisconnected(socket);
                socket.OnError = exception => OnSocketError(exception);
            });
        }
        
        private static bool IsPortAvailable(int port)
        {
            var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            var activeTcpListeners = ipGlobalProperties.GetActiveTcpListeners();
            return activeTcpListeners.All(endPoint => endPoint.Port != port);
        }
        
        public static string GetFirstIpAddress()
        {
            try
            {
                // Get all network interfaces
                var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up && 
                                 ni.NetworkInterfaceType != NetworkInterfaceType.Loopback);

                foreach (var ni in networkInterfaces)
                {
                    var ipProperties = ni.GetIPProperties();

                    // Get all unicast addresses and filter for IPv4 addresses
                    foreach (var unicastAddress in ipProperties.UnicastAddresses)
                    {
                        if (unicastAddress.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            return unicastAddress.Address.ToString();
                        }
                    }
                }
                return "No IPv4 Address Found";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        private static void OnClientConnected(IWebSocketConnection socket, Dictionary<string, string> args)
        {
            var path = socket.ConnectionInfo.Path; // Extract the path from the connection URL
            Console.WriteLine($"Client connected: {socket.ConnectionInfo.ClientIpAddress} with Path: {path}");

            // Authenticate using the password in the path
            if (path.Trim('/') == args["server.rconpassword"]) // Remove leading/trailing slashes
            {
                Console.WriteLine("Client authenticated successfully.");
                authenticatedClients.Add(socket);
                socket.Send("Authentication successful!");
            }
            else
            {
                Console.WriteLine("Client failed authentication. Closing connection...");
                socket.Send("Invalid password. Connection closed.");
                socket.Close();
            }
        }

        private static void OnMessageReceived(IWebSocketConnection socket, string message)
        {
            if (authenticatedClients.Contains(socket))
            {
                Console.WriteLine($"Message from {socket.ConnectionInfo.ClientIpAddress}: {message}");
                Logger.Outsider($"[RCON Command ({socket.ConnectionInfo.ClientIpAddress})] > {message}");
                CommandManager.ParseRconCommands(message, socket);
                socket.Send($"Echo: {message}");
            }
            else
            {
                socket.Send("You are not authenticated.");
                socket.Close();
            }
        }

        private static void OnClientDisconnected(IWebSocketConnection socket)
        {
            Console.WriteLine($"RCON Client disconnected: {socket.ConnectionInfo.ClientIpAddress}");
            authenticatedClients.Remove(socket);
        }

        private static void OnSocketError(Exception exception)
        {
            Console.WriteLine($"RCON Error: {exception.Message}");
        }
    }
}
