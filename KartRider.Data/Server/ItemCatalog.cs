using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using KartLibrary.File.Lpk;
using KartRider.Logging;
using Profile;

namespace KartRider
{
    /// <summary>
    /// 物品名称目录：从游戏数据包里的 zeta_/cn/shop/data/item.kml 解析
    /// 「类别 → 物品 ID → 中文名」，用于把玩家装扮的 ID 翻译成人类可读的名字。
    /// 解析一次后缓存到 Profile/ItemCatalog.json，之后启动直接读缓存。
    /// </summary>
    public static class ItemCatalog
    {
        private static readonly object Gate = new();
        private static Dictionary<string, Dictionary<int, string>> _catalog = new();
        private static bool _loaded;
        private static bool _building;

        private static string CachePath => Path.Combine(FileName.ProfileDir, "ItemCatalog.json");

        /// <summary>装扮槽位 → 物品类别（item.kml 的 itemCatId，已用真实数据核对）。</summary>
        public static readonly Dictionary<string, int> SlotCategories = new()
        {
            ["character"] = 1,   // 角色
            ["kart"] = 3,        // 卡丁车
            ["paint"] = 2,       // 喷漆（涂装）
            ["decal"] = 20,      // 贴花
            ["pet"] = 21,        // 宠物
            ["uniform"] = 18,    // 服装
            ["headband"] = 11,   // 头饰
            ["aura"] = 26,       // 炫光
            ["skidmark"] = 27    // 胎痕印迹
        };

        /// <summary>确保目录已加载（缓存优先，没有就后台解析数据包）。</summary>
        public static void EnsureLoaded()
        {
            lock (Gate)
            {
                if (_loaded)
                {
                    return;
                }
                _loaded = true;

                try
                {
                    if (File.Exists(CachePath))
                    {
                        _catalog = JsonSerializer.Deserialize<Dictionary<string, Dictionary<int, string>>>(File.ReadAllText(CachePath)) ?? new();
                        int total = _catalog.Values.Sum(d => d.Count);
                        Log.Info($"[ItemCatalog] 已从缓存加载物品目录（{total} 条）");
                        if (total > 0)
                        {
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Info($"[ItemCatalog] 读取缓存失败，重新解析: {ex.Message}");
                }

                _building = true;
                _ = Task.Run(() =>
                {
                    try
                    {
                        BuildFromGamePack();
                    }
                    catch (Exception ex)
                    {
                        Log.Info($"[ItemCatalog] 解析物品目录失败: {ex.Message}");
                    }
                    finally
                    {
                        _building = false;
                    }
                });
            }
        }

        /// <summary>从游戏数据包解析 item.kml 并写缓存。</summary>
        private static void BuildFromGamePack()
        {
            string pack = FindGamePack();
            if (pack == null)
            {
                Log.Info("[ItemCatalog] 没有找到 Data\\aaa.pk，物品目录不可用");
                return;
            }

            Log.Info($"[ItemCatalog] 开始解析物品数据库: {pack}");
            var mgr = new LpkFolderManager();
            mgr.OpenDataFolder(pack);
            string kml = ExtractFile(mgr.GetRootFolder(), "zeta_/cn/shop/data/item.kml");
            if (string.IsNullOrEmpty(kml) || kml.StartsWith("(未找到)"))
            {
                Log.Info("[ItemCatalog] 数据包里没有 item.kml");
                return;
            }

            var doc = new XmlDocument();
            doc.LoadXml(kml.TrimStart('\uFEFF'));

            var catalog = new Dictionary<string, Dictionary<int, string>>(StringComparer.OrdinalIgnoreCase);
            foreach (XmlElement item in doc.DocumentElement.SelectNodes("item"))
            {
                string cat = item.GetAttribute("itemCatId");
                string idText = item.GetAttribute("itemId");
                string name = item.GetAttribute("itemName");
                if (cat.Length == 0 || !int.TryParse(idText, out int id) || name.Length == 0)
                {
                    continue;
                }
                if (!catalog.TryGetValue(cat, out var dict))
                {
                    dict = new Dictionary<int, string>();
                    catalog[cat] = dict;
                }
                dict[id] = name;
            }

            lock (Gate)
            {
                _catalog = catalog;
                Directory.CreateDirectory(Path.GetDirectoryName(CachePath));
                File.WriteAllText(CachePath, JsonSerializer.Serialize(catalog), new UTF8Encoding(false));
            }

            Log.Info($"[ItemCatalog] 物品目录解析完成（{catalog.Values.Sum(d => d.Count)} 条，{catalog.Count} 个类别）");
        }

        private static string FindGamePack()
        {
            string candidate = Path.Combine(FileName.appDir, "Data", "aaa.pk");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            // 启动器不在游戏目录时，从注册表兜底
            string gamePath = Microsoft.Win32.Registry.GetValue(
                "HKEY_CURRENT_USER\\Software\\TCGame\\kart", "gamepath", null) as string;
            if (!string.IsNullOrEmpty(gamePath))
            {
                candidate = Path.Combine(gamePath, "Data", "aaa.pk");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
            return null;
        }

        /// <summary>从 lpk 树里按 FullName 取文件内容。</summary>
        private static string ExtractFile(object root, string fullName)
        {
            var queue = new Queue<object>();
            queue.Enqueue(root);
            while (queue.Count > 0)
            {
                var folder = queue.Dequeue();
                var t = folder.GetType();
                var list = t.GetMethod("GetFilesInfo")?.Invoke(folder, null) as System.Collections.IEnumerable;
                if (list != null)
                {
                    foreach (var f in list)
                    {
                        var ft = f.GetType();
                        if (string.Equals(ft.GetProperty("FullName")?.GetValue(f)?.ToString(), fullName, StringComparison.OrdinalIgnoreCase))
                        {
                            var data = ft.GetMethod("GetData")?.Invoke(f, null) as byte[];
                            return data == null ? "" : Encoding.Unicode.GetString(data);
                        }
                    }
                }
                var subs = t.GetMethod("GetFoldersInfo")?.Invoke(folder, null) as System.Collections.IEnumerable;
                if (subs != null)
                {
                    foreach (var sub in subs) queue.Enqueue(sub);
                }
            }
            return null;
        }

        /// <summary>按槽位类别 + 物品 ID 查名称；目录没加载/查不到返回 null。</summary>
        public static string GetName(string slot, int id)
        {
            if (id <= 0 || !_catalog.TryGetValue(SlotCategories.TryGetValue(slot, out int cat) ? cat.ToString() : "", out var dict))
            {
                return null;
            }
            return dict.TryGetValue(id, out string name) ? name : null;
        }

        /// <summary>某个玩家的当前装扮（ID + 可解析的中文名）。</summary>
        public static string BuildAppearance(string nickname)
        {
            EnsureLoaded();
            ProfileService.Load(nickname);
            var rider = ProfileService.GetProfileConfig(nickname)?.RiderItem;
            if (rider == null)
            {
                return JsonSerializer.Serialize(new { nickname, found = false });
            }

            var slots = new (string Key, string Label, int Id)[]
            {
                ("character", "角色", rider.Set_Character),
                ("kart", "车辆", rider.Set_Kart),
                ("paint", "涂装", rider.Set_Paint),
                ("decal", "贴花", rider.Set_Decal),
                ("plate", "车牌", rider.Set_Plate),
                ("goggle", "眼镜", rider.Set_Goggle),
                ("headband", "头饰", rider.Set_HeadBand),
                ("headphone", "耳机", rider.Set_HeadPhone),
                ("balloon", "气球", rider.Set_Balloon),
                ("uniform", "服装", rider.Set_Uniform),
                ("pet", "宠物", rider.Set_Pet),
                ("flyingpet", "飞行宠", rider.Set_FlyingPet),
                ("aura", "炫光", rider.Set_Aura),
                ("skidmark", "胎痕", rider.Set_SkidMark)
            };

            var items = new List<JsonElement>();
            foreach (var (key, label, id) in slots)
            {
                if (id <= 0)
                {
                    continue;
                }
                string name = GetName(key, id);
                items.Add(JsonSerializer.SerializeToElement(new
                {
                    key,
                    label,
                    id,
                    name = name ?? ("#" + id)
                }));
            }

            var result = new Dictionary<string, object>
            {
                ["nickname"] = nickname,
                ["found"] = true,
                ["catalogReady"] = _catalog.Count > 0,
                ["items"] = items
            };
            return JsonSerializer.Serialize(result);
        }
    }
}
