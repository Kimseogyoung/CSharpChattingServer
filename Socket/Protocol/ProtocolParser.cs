using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Protocol
{
    public class ProtocolParser
    {
        public static T Deserialize<T>(byte[] bytes)
        {
            var json = Encoding.UTF8.GetString(bytes);
            var obj = JsonSerializer.Deserialize<T>(json);
            return obj;
        }

        public static byte[] Serialize<T>(T obj)
        {
            var json = JsonSerializer.Serialize(obj);
            var bytes = Encoding.UTF8.GetBytes(json);
            return bytes;
        }
    }
}
