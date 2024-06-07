using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;

namespace Shared
{
#pragma warning disable SYSLIB0011

    //Class that handles all of the mod's serialization functions

    public static class Serializer
    {
        //Overrider of the binary formatter settings to make it compatible with both framework versions

        private static BindOverrider bindOverrider = new BindOverrider();

        //Serialize from and to byte array

        public static byte[] ConvertObjectToBytes(object toConvert)
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            binaryFormatter.Binder = bindOverrider;

            MemoryStream memoryStream = new MemoryStream();
            binaryFormatter.Serialize(memoryStream, toConvert);

            return GZip.Compress(memoryStream.ToArray());
        }

        public static object ConvertBytesToObject(byte[] bytes)
        {
            bytes = GZip.Decompress(bytes);

            BinaryFormatter binaryFormatter = new BinaryFormatter();
            binaryFormatter.Binder = bindOverrider;

            MemoryStream memoryStream = new MemoryStream();
            memoryStream.Write(bytes, 0, bytes.Length);
            memoryStream.Seek(0, SeekOrigin.Begin);

            return binaryFormatter.Deserialize(memoryStream);
        }

        //Serialize from and to packets

        public static string SerializePacketToString(Packet packet) { return SerializeToString(packet); }

        public static Packet SerializeStringToPacket(string serializable) { return SerializeFromString<Packet>(serializable); }

        //Serialize from and to strings

        public static string SerializeToString(object serializable) { return JsonConvert.SerializeObject(serializable); }

        public static T SerializeFromString<T>(string serializable) { return JsonConvert.DeserializeObject<T>(serializable); }

        //Serialize from and to files

        public static void SerializeToFile(string path, object serializable)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(serializable, new JsonSerializerSettings
            {
                Formatting = Formatting.Indented
            }));
        }

        public static T SerializeFromFile<T>(string path)
        {
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
        }
    }
}