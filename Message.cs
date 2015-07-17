using Codeplex.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace WebSocketServer2
{
    public class MessageParser
    {
        static public Message Parse(string msg)
        {
            var json = DynamicJson.Parse(msg);
            if (json.name == "joinRoom")
            {
                return new JoinRoom(json.name, json.socketId);
            }

            return null;
        }
    }

    public class Message
    {
        public Message(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
    }

    public class JoinRoom : Message
    {
        public JoinRoom(string name, string socketId)
            : base(name)
        {
            SocketId = socketId;
        }

        public string SocketId { get; set; }
    }
}
