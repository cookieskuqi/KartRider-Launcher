using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Profile
{
    /// <summary>
    /// 一条已保存的服务器地址。Note 是给玩家看的备注（如「本机」「朋友服」），可以为空。
    /// </summary>
    public class ServerEntry
    {
        /// <summary>服务器 IP（IPv4 或 IPv6 字面量）。</summary>
        public string IP { get; set; } = "";

        /// <summary>备注，留空则下拉框只显示 IP。</summary>
        public string Note { get; set; } = "";

        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(Note) ? IP : Note.Trim() + " (" + IP + ")";
        }
    }

    /// <summary>
    /// SavedServers 的读写兼容层。
    /// 早期版本把这一项存成 ["1.2.3.4", ...] 纯字符串数组；现在改为 [{IP,Note}]。
    /// 若不做兼容，旧配置反序列化会整体失败，玩家的昵称/版本/端口等设置会被默认值覆盖。
    /// </summary>
    public class ServerEntryListConverter : JsonConverter<List<ServerEntry>>
    {
        public override List<ServerEntry> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var list = new List<ServerEntry>();
            if (reader.TokenType == JsonTokenType.Null)
            {
                return list;
            }
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                reader.Skip();
                return list;
            }

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    break;
                }
                if (reader.TokenType == JsonTokenType.String)
                {
                    string ip = (reader.GetString() ?? "").Trim();
                    if (ip.Length > 0)
                    {
                        list.Add(new ServerEntry { IP = ip });
                    }
                }
                else if (reader.TokenType == JsonTokenType.StartObject)
                {
                    ServerEntry entry = JsonSerializer.Deserialize<ServerEntry>(ref reader, options);
                    if (entry != null && !string.IsNullOrWhiteSpace(entry.IP))
                    {
                        entry.IP = entry.IP.Trim();
                        entry.Note = (entry.Note ?? "").Trim();
                        list.Add(entry);
                    }
                }
                else
                {
                    reader.Skip();
                }
            }
            return list;
        }

        public override void Write(Utf8JsonWriter writer, List<ServerEntry> value, JsonSerializerOptions options)
        {
            // 逐项手写：这里若回调 JsonSerializer.Serialize 会重回本转换器，造成无限递归
            writer.WriteStartArray();
            foreach (ServerEntry entry in value ?? new List<ServerEntry>())
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.IP))
                {
                    continue;
                }
                writer.WriteStartObject();
                writer.WriteString(nameof(ServerEntry.IP), entry.IP);
                writer.WriteString(nameof(ServerEntry.Note), entry.Note ?? "");
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }
    }

    /// <summary>服务器地址校验：只接受 IP 字面量，主机名会被下游 IPAddress.Parse 拖到运行时才崩。</summary>
    public static class ServerAddress
    {
        public static bool IsValidIp(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip))
            {
                return false;
            }
            IPAddress address;
            if (!IPAddress.TryParse(ip.Trim(), out address))
            {
                return false;
            }
            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                // TryParse 会把 "1"、"192.168.1" 补全成完整地址，这里要求原样就是四段写法
                return address.ToString() == ip.Trim();
            }
            return address.AddressFamily == AddressFamily.InterNetworkV6;
        }
    }
}
