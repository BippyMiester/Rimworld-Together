using Fleck;

namespace GameServer
{
    public class RconCommand
    {
        public string prefix;

        public string description;

        public int parameters;

        public Action<IWebSocketConnection> commandAction;

        public RconCommand(string prefix, int parameters, string description, Action<IWebSocketConnection> commandAction)
        {
            this.prefix = prefix;
            this.parameters = parameters;
            this.description = description;
            this.commandAction = commandAction;
        }
    }
}