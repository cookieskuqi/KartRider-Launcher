using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using KartRider.Logging;
using Profile;

namespace KartRider
{
    /// <summary>
    /// 对局记录器：每局结算时把结果落盘成 JSON，供 LocalAdminApi 的 /api/* 查询，
    /// 玩家可以搭网页查自己的战绩（等价于当年韩服 KartRider Open API 的作用，
    /// 区别是数据来自我们自己的服务器，不需要向 Nexon 申请任何东西）。
    /// </summary>
    public static class MatchRecorder
    {
        private static readonly object Gate = new();
        private const int MaxMatches = 500;

        private static string StoreDir => Path.Combine(FileName.appDir, "Matches");
        private static string StorePath => Path.Combine(StoreDir, "matches.json");

        /// <summary>结算时调用：把一局的结果写进对局记录。</summary>
        public static void Record(GameRoom room, RoomMember[] members, Dictionary<int, uint> timeData)
        {
            try
            {
                var ranking = new Dictionary<int, int>(room.Ranking);
                var playerList = new List<JsonElement>();

                foreach (RoomMember member in members)
                {
                    // RoomMember 基类没有 ID，要到具体类型上取
                    string nickname;
                    bool ai;
                    byte team;
                    int id;

                    if (member is Player p)
                    {
                        id = p.ID;
                        nickname = string.IsNullOrWhiteSpace(p.Nickname) ? $"玩家{p.ID}" : p.Nickname;
                        ai = false;
                        team = p.Team;
                    }
                    else if (member is Ai a)
                    {
                        id = a.ID;
                        nickname = $"AI {a.ID}";
                        ai = true;
                        team = a.Team;
                    }
                    else
                    {
                        continue;
                    }

                    timeData.TryGetValue(id, out uint finishTick);
                    bool dnf = finishTick == uint.MaxValue;
                    ranking.TryGetValue(id, out int rank);

                    var entry = new Dictionary<string, object>
                    {
                        ["nickname"] = nickname,
                        ["id"] = id,
                        ["team"] = team,
                        ["rank"] = rank + 1,               // 0 基排名转成 1 基，第一名是 1
                        ["timeMs"] = dnf ? -1L : finishTick,
                        ["dnf"] = dnf,
                        ["ai"] = ai
                    };
                    playerList.Add(JsonSerializer.SerializeToElement(entry));
                }

                var match = new Dictionary<string, object>
                {
                    ["time"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    ["room"] = room.RoomName ?? string.Empty,
                    ["mode"] = room.ModeByte,
                    ["speed"] = room.SpeedType,
                    ["trackId"] = room.track,
                    ["track"] = RandomTrack.GetTrackName(room.track),
                    ["players"] = playerList
                };

                lock (Gate)
                {
                    Directory.CreateDirectory(StoreDir);
                    var list = LoadAll();
                    list.Insert(0, JsonSerializer.SerializeToElement(match));
                    while (list.Count > MaxMatches)
                    {
                        list.RemoveAt(list.Count - 1);
                    }
                    File.WriteAllText(StorePath, JsonSerializer.Serialize(list), new UTF8Encoding(false));
                }

                Log.Info($"[Match] 已记录对局: {match["track"]} / {playerList.Count} 人");
            }
            catch (Exception ex)
            {
                Log.Info($"[Match] 记录对局失败: {ex.Message}");
            }
        }

        private static List<JsonElement> LoadAll()
        {
            try
            {
                if (File.Exists(StorePath))
                {
                    return JsonSerializer.Deserialize<List<JsonElement>>(File.ReadAllText(StorePath)) ?? new List<JsonElement>();
                }
            }
            catch (Exception ex)
            {
                Log.Info($"[Match] 读取对局记录失败: {ex.Message}");
            }
            return new List<JsonElement>();
        }

        /// <summary>最近 n 场对局；nickname 非空时只返回该玩家参与的对局。</summary>
        public static string QueryMatches(int n, string nickname)
        {
            lock (Gate)
            {
                var all = LoadAll();
                IEnumerable<JsonElement> picked = all;
                if (!string.IsNullOrWhiteSpace(nickname))
                {
                    picked = all.Where(m =>
                    {
                        if (!m.TryGetProperty("players", out var players) || players.ValueKind != JsonValueKind.Array)
                        {
                            return false;
                        }
                        return players.EnumerateArray().Any(p =>
                            p.TryGetProperty("nickname", out var name) &&
                            string.Equals(name.GetString(), nickname, StringComparison.OrdinalIgnoreCase));
                    });
                }

                return JsonSerializer.Serialize(picked.Take(Math.Max(1, n)).ToList());
            }
        }

        /// <summary>全部出现过的玩家昵称（用于网页下拉框）。</summary>
        public static string UserList()
        {
            lock (Gate)
            {
                var names = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var match in LoadAll())
                {
                    if (match.TryGetProperty("players", out var players) && players.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var p in players.EnumerateArray())
                        {
                            if (p.TryGetProperty("nickname", out var name) &&
                                p.TryGetProperty("ai", out var ai) && ai.ValueKind == JsonValueKind.False &&
                                name.GetString() is string n && n.Length > 0)
                            {
                                names.Add(n);
                            }
                        }
                    }
                }
                return JsonSerializer.Serialize(names.ToList());
            }
        }

        /// <summary>某个玩家的生涯统计：场次、冠军、前三、DNF、最近对局。</summary>
        public static string UserStats(string nickname)
        {
            lock (Gate)
            {
                int games = 0, wins = 0, podium = 0, dnf = 0;
                var recent = new List<JsonElement>();
                string lastTime = null;

                foreach (var match in LoadAll())
                {
                    if (!match.TryGetProperty("players", out var players) || players.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    JsonElement me = default;
                    bool found = false;
                    foreach (var p in players.EnumerateArray())
                    {
                        if (p.TryGetProperty("nickname", out var name) &&
                            string.Equals(name.GetString(), nickname, StringComparison.OrdinalIgnoreCase))
                        {
                            me = p;
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        continue;
                    }

                    games++;
                    me.TryGetProperty("rank", out var rankEl);
                    int rank = rankEl.ValueKind == JsonValueKind.Number ? rankEl.GetInt32() : 99;
                    me.TryGetProperty("dnf", out var dnfEl);
                    bool isDnf = dnfEl.ValueKind == JsonValueKind.True;

                    if (rank == 1) wins++;
                    if (rank <= 3 && !isDnf) podium++;
                    if (isDnf) dnf++;

                    if (recent.Count < 10)
                    {
                        var copy = new Dictionary<string, object>
                        {
                            ["time"] = match.TryGetProperty("time", out var t) ? t.GetString() : "",
                            ["track"] = match.TryGetProperty("track", out var tr) ? tr.GetString() : "",
                            ["rank"] = rank,
                            ["dnf"] = isDnf
                        };
                        recent.Add(JsonSerializer.SerializeToElement(copy));
                    }

                    if (lastTime == null && match.TryGetProperty("time", out var timeEl))
                    {
                        lastTime = timeEl.GetString();
                    }
                }

                var result = new Dictionary<string, object>
                {
                    ["nickname"] = nickname,
                    ["games"] = games,
                    ["wins"] = wins,
                    ["podium"] = podium,
                    ["dnf"] = dnf,
                    ["lastTime"] = lastTime ?? "",
                    ["recent"] = recent
                };
                return JsonSerializer.Serialize(result);
            }
        }
    }
}
