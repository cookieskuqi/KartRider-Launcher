using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using KartRider.Logging;
using Profile;

namespace KartRider
{
    /// <summary>一条公告。</summary>
    public class NoticeItem
    {
        public string Date { get; set; } = "";

        public string Title { get; set; } = "";

        public string Body { get; set; } = "";
    }

    /// <summary>
    /// 公告数据源：可以是一个 http(s) 网址，也可以是本地 txt 文件。
    /// 拉取失败时回退到本地缓存（Profile\notice.cache），保证离线也能看到上次的内容。
    /// </summary>
    public static class NoticeService
    {
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        private static readonly Regex DatePrefix = new Regex(
            @"^\s*(\d{4}[-/]\d{1,2}[-/]\d{1,2}|\d{1,2}[-/]\d{1,2})\s*", RegexOptions.Compiled);

        /// <summary>最近一次成功读取的原文。</summary>
        public static string RawText { get; private set; } = string.Empty;

        /// <summary>最近一次读取是否来自缓存（网络失败时）。</summary>
        public static bool FromCache { get; private set; }

        public static string CachePath
        {
            get { return Path.Combine(FileName.ProfileDir, "notice.cache"); }
        }

        /// <summary>默认的本地公告文件，配置留空时使用（存在才算）。</summary>
        public static string DefaultLocalFile
        {
            get { return Path.Combine(FileName.ProfileDir, "notice.txt"); }
        }

        /// <summary>首次使用时生成一份示例公告，方便直接编辑（已存在则不覆盖）。</summary>
        public static void EnsureTemplate()
        {
            try
            {
                Directory.CreateDirectory(FileName.ProfileDir);
                if (!File.Exists(DefaultLocalFile))
                {
                    File.WriteAllText(DefaultLocalFile, TemplateText, new UTF8Encoding(true));
                    Log.Info($"[Notice] 已生成示例公告: {DefaultLocalFile}");
                }
            }
            catch (Exception ex)
            {
                Log.Info($"[Notice] 生成示例公告失败: {ex.Message}");
            }
        }

        private const string TemplateText =
            "2026-11-01 公告栏说明\n" +
            "这里是 Profile\\notice.txt，用记事本直接改就行。\n" +
            "每条公告之间用单独一行 --- 分隔；第一行是标题（前面可以带日期），其余行是正文。\n" +
            "---\n" +
            "点右上角「编辑」可以改成网址\n" +
            "把公告文件放到自己的服务器或 GitHub Pages 上，填网址即可让所有玩家看到同一份公告。\n";

        /// <summary>按配置解析出实际要用的来源。</summary>
        public static string ResolveSource(string configured)
        {
            if (!string.IsNullOrWhiteSpace(configured))
            {
                return configured.Trim();
            }
            return File.Exists(DefaultLocalFile) ? DefaultLocalFile : string.Empty;
        }

        /// <summary>
        /// 读取公告。网络操作放在线程池，调用方负责回到 UI 线程。
        /// </summary>
        public static async Task<bool> LoadAsync(string source)
        {
            source = ResolveSource(source);
            if (string.IsNullOrWhiteSpace(source))
            {
                // 没配来源也没本地文件：清空成占位状态
                RawText = string.Empty;
                FromCache = false;
                return false;
            }

            try
            {
                string text;
                if (source.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                    || source.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    text = await Http.GetStringAsync(source).ConfigureAwait(false);
                }
                else
                {
                    text = File.ReadAllText(source, Encoding.UTF8);
                }

                RawText = text ?? string.Empty;
                FromCache = false;
                TryWriteCache(RawText);
                Log.Info($"[Notice] 已读取公告: {source}（{RawText.Length} 字）");
                return true;
            }
            catch (Exception ex)
            {
                Log.Info($"[Notice] 读取失败，尝试使用缓存: {ex.Message}");
                string cached = TryReadCache();
                if (cached != null)
                {
                    RawText = cached;
                    FromCache = true;
                    return true;
                }
                RawText = string.Empty;
                FromCache = false;
                return false;
            }
        }

        private static void TryWriteCache(string text)
        {
            try
            {
                Directory.CreateDirectory(FileName.ProfileDir);
                File.WriteAllText(CachePath, text, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Log.Info($"[Notice] 写缓存失败: {ex.Message}");
            }
        }

        private static string TryReadCache()
        {
            try
            {
                return File.Exists(CachePath) ? File.ReadAllText(CachePath, Encoding.UTF8) : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 解析公告文本。用单独一行 “---” 分隔每一条；
        /// 每条的第一行是标题（可带 yyyy-MM-dd 日期前缀），其余行是正文。
        /// </summary>
        public static List<NoticeItem> Parse(string text)
        {
            var items = new List<NoticeItem>();
            if (string.IsNullOrWhiteSpace(text))
            {
                return items;
            }

            var block = new List<string>();
            foreach (string rawLine in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
            {
                if (rawLine.Trim() == "---")
                {
                    AddBlock(items, block);
                    block = new List<string>();
                    continue;
                }
                block.Add(rawLine);
            }
            AddBlock(items, block);
            return items;
        }

        private static void AddBlock(List<NoticeItem> items, List<string> block)
        {
            while (block.Count > 0 && string.IsNullOrWhiteSpace(block[0]))
            {
                block.RemoveAt(0);
            }
            while (block.Count > 0 && string.IsNullOrWhiteSpace(block[block.Count - 1]))
            {
                block.RemoveAt(block.Count - 1);
            }
            if (block.Count == 0)
            {
                return;
            }

            string header = block[0].Trim();
            string date = string.Empty;
            Match match = DatePrefix.Match(header);
            if (match.Success)
            {
                date = match.Groups[1].Value;
                header = header.Substring(match.Length).Trim();
            }

            var body = new StringBuilder();
            for (int i = 1; i < block.Count; i++)
            {
                if (body.Length > 0)
                {
                    body.Append('\n');
                }
                body.Append(block[i].TrimEnd());
            }

            items.Add(new NoticeItem
            {
                Date = date,
                Title = header,
                Body = body.ToString()
            });
        }
    }
}
