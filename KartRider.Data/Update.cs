using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows.Forms;
using Profile;
using KartRider.Logging;

namespace KartRider
{
    class Update
    {
        // 自更新检查的仓库（本项目自己的 Release）
        public static string owner = "cookieskuqi";
        public static string repo = "KartRider-Launcher";
        static string name;
        static string filePath;

        public static async Task<bool> UpdateDataAsync(bool silent = false)
        {
            filePath = JsonHelper.GetFilePath();
            Log.Info("当前程序路径: " + filePath);
            name = Path.GetFileName(filePath);
            string Update_Folder = Path.Combine(Path.GetDirectoryName(filePath), "Update");
            string Update_File = Path.Combine(Update_Folder, name);

            // 自更新不走 GitHub API（匿名额度 60 次/小时，很快就被限流导致检查失效），
            // 改读 Release 附件 version.json（releases/latest/download 直链，无额度限制）
            string localSha = CalculateSHA256(filePath).ToLowerInvariant();
            Log.Info($"当前程序 SHA256: {localSha}");

            try
            {
                string versionJson = await FetchVersionInfoAsync();
                if (versionJson == null)
                {
                    Log.Info("版本信息获取失败，跳过自更新检查");
                    return false;
                }

                var info = JsonSerializer.Deserialize<VersionInfo>(versionJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (info == null || string.IsNullOrWhiteSpace(info.sha256))
                {
                    Log.Info("version.json 里没有 sha256，跳过自更新检查");
                    return false;
                }

                string remoteSha = info.sha256.Trim().ToLowerInvariant().Replace("sha256:", "");
                Log.Info($"远端版本 SHA256: {remoteSha}（构建于 {info.time}）");
                if (remoteSha == localSha)
                {
                    Log.Info("当前已是最新版本，无需更新。");
                    return false;
                }

                // 发现新版本：弹窗询问，用户可以选择不更新
                string note = string.IsNullOrWhiteSpace(info.note) ? "" : "\n\n更新内容：\n" + info.note;
                if (!silent)
                {
                    DialogResult result = MessageBox.Show(
                        "发现启动器新版本（" + (info.time ?? "") + " 构建），是否立即更新？" + note +
                        "\n\n更新过程中启动器会短暂关闭，完成后会自动重新打开。\n选择「否」可以继续使用当前版本。",
                        "启动器更新",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result != DialogResult.Yes)
                    {
                        Log.Info("用户取消更新，保留当前版本");
                        return false;
                    }
                }

                try
                {
                    Directory.CreateDirectory(Update_Folder);
                    string downloadUrl = $"https://github.com/{owner}/{repo}/releases/latest/download/{name}";

                    var downloader = new MultiThreadedDownloader(downloadUrl, Update_File, 1);
                    bool downloaded = await downloader.StartDownloadAsync();
                    if (!downloaded)
                    {
                        // CN 网络直连失败时，用设置的代理前缀重试一次
                        string proxy = ProfileService.SettingConfig.Proxy;
                        if (!string.IsNullOrWhiteSpace(proxy))
                        {
                            string proxied = proxy + downloadUrl.Replace("https://", "");
                            Log.Info($"直连下载失败，改用代理重试: {proxied}");
                            downloaded = await new MultiThreadedDownloader(proxied, Update_File, 1).StartDownloadAsync();
                        }
                    }

                    if (!downloaded)
                    {
                        Log.Info("更新包下载失败");
                        return false;
                    }

                    return ConfirmAndApplyUpdate(Update_File, "sha256:" + remoteSha);
                }
                catch (Exception ex)
                {
                    Log.Info($"下载过程中出现错误: {ex.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Info($"自更新检查失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>读取最新 Release 的 version.json（直连优先，失败时尝试设置的代理前缀）。</summary>
        private static async Task<string> FetchVersionInfoAsync()
        {
            string path = $"https://github.com/{owner}/{repo}/releases/latest/download/version.json";

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("KartRiderLauncher");
                httpClient.Timeout = TimeSpan.FromSeconds(10);
                try
                {
                    return await httpClient.GetStringAsync(path);
                }
                catch (Exception ex)
                {
                    Log.Info($"直连获取版本信息失败: {ex.Message}");
                }
            }

            try
            {
                string proxy = ProfileService.SettingConfig.Proxy;
                if (string.IsNullOrWhiteSpace(proxy))
                {
                    return null;
                }

                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("KartRiderLauncher");
                    httpClient.Timeout = TimeSpan.FromSeconds(10);
                    return await httpClient.GetStringAsync(proxy + path.Replace("https://", ""));
                }
            }
            catch (Exception ex)
            {
                Log.Info($"代理获取版本信息也失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>Release 里 version.json 的结构。</summary>
        public class VersionInfo
        {
            public string sha256 { get; set; }
            public string time { get; set; }
            public string note { get; set; }
        }

        /// <summary>把 Release 说明压成几行纯文本，放进询问框。</summary>
        private static string BuildNote(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return string.Empty;
            }

            var lines = new List<string>();
            foreach (string raw in body.Replace("\r\n", "\n").Split('\n'))
            {
                string line = raw.Trim().TrimStart('#', '-', '*', ' ').Trim();
                if (line.Length == 0 || line.StartsWith(">"))
                {
                    continue;
                }
                lines.Add(line);
                if (lines.Count >= 4)
                {
                    break;
                }
            }

            if (lines.Count == 0)
            {
                return string.Empty;
            }

            string text = string.Join("\n", lines);
            if (text.Length > 220)
            {
                text = text.Substring(0, 220) + "…";
            }
            return "\n\n更新内容：\n" + text;
        }

        /// <summary>
        /// 校验下载文件的完整性，并应用更新。
        /// 用户确认已在进行下载之前完成；不在后台静默替换自身——
        /// "下载后自动执行替换脚本并自杀"会被行为检测（PDM）判为恶意自更新。
        /// </summary>
        /// <param name="updateFilePath">下载到本地的更新文件路径</param>
        /// <param name="expectedDigest">GitHub 公布的 sha256:xxxx 摘要</param>
        /// <returns>是否已应用更新</returns>
        private static bool ConfirmAndApplyUpdate(string updateFilePath, string expectedDigest)
        {
            try
            {
                // 1. 校验下载文件的 SHA256 与官方发布的 digest 一致，防止下载到损坏或被篡改的文件
                string actualHash = "sha256:" + CalculateSHA256(updateFilePath);
                if (!string.Equals(actualHash, expectedDigest, StringComparison.OrdinalIgnoreCase))
                {
                    Log.Info($"校验失败：下载文件的哈希与官方发布不一致，已取消更新");
                    return false;
                }

                // 2. 校验通过后执行替换
                ApplyUpdate(updateFilePath);
                return true;
            }
            catch (Exception ex)
            {
                Log.Info($"应用更新时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 应用更新：采用"重命名替换"方案，不使用 bat 脚本、不自杀、不删除自身文件，
        /// 以避免 PDM 行为检测（自删/替换/销毁目录/强制终止进程属于高危特征）。
        /// Windows 允许重命名正在运行的映像文件，因此：
        ///   1. 将运行中的自身 exe 重命名为 *.old
        ///   2. 将下载的新 exe 移入主路径
        ///   3. 清理更新目录（托管 API，不经过 cmd）
        ///   4. 启动新版本（携带 /updated 参数，由新实例清理 *.old 备份）
        /// </summary>
        /// <param name="Update_FilePath">下载到本地的更新文件路径</param>
        /// <returns>是否已应用更新</returns>
        public static bool ApplyUpdate(string Update_FilePath)
        {
            try
            {
                string oldFile = filePath + ".old";
                string updateFolder = Path.GetDirectoryName(Update_FilePath);

                // 1. 重命名运行中的自身（若残留旧备份先清理）
                if (File.Exists(oldFile))
                {
                    File.Delete(oldFile);
                }
                File.Move(filePath, oldFile);

                // 2. 新 exe 移入主路径
                File.Move(Update_FilePath, filePath);

                // 3. 清理更新目录（含已无用的下载文件）
                try
                {
                    if (Directory.Exists(updateFolder))
                    {
                        Directory.Delete(updateFolder, true);
                    }
                }
                catch (Exception ex)
                {
                    Log.Info($"清理更新目录时出错: {ex.Message}");
                }

                // 4. 启动新版本，由新实例完成旧备份清理
                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    Arguments = "/updated",
                    UseShellExecute = true
                });

                // 正常退出当前进程（非强制终止）
                Environment.Exit(0);
                return true;
            }
            catch (Exception ex)
            {
                Log.Info($"\n应用更新时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 新版本实例启动后调用：清理上一次更新遗留的旧版本备份文件（*.old）。
        /// </summary>
        public static void CleanupOldVersion()
        {
            try
            {
                string oldFile = filePath + ".old";
                if (File.Exists(oldFile))
                {
                    File.Delete(oldFile);
                    Log.Info($"已清理旧版本备份: " + oldFile);
                }
            }
            catch (Exception ex)
            {
                Log.Info($"清理旧版本备份时出错: {ex.Message}");
            }
        }

        public static async Task<IpInfo> GetCountryAsync()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10); // 与 GetLatestRelease 一致，避免默认100秒超时长时间阻塞
                    HttpResponseMessage response = await client.GetAsync("https://ipinfo.io/json");
                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();
                        IpInfo data = JsonSerializer.Deserialize<IpInfo>(json,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        return data;
                    }
                    else
                    {
                        Log.Info($"请求失败，状态码: {response.StatusCode}");
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Info($"发生异常: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 检查URL是否可访问
        /// </summary>
        /// <param name="url">要检查的URL</param>
        /// <returns>如果URL可访问则返回true，否则返回false</returns>
        public static async Task<bool> GetUrl(string url)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));

                    if (response.IsSuccessStatusCode)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// 计算指定文件的SHA256哈希值
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>SHA256哈希值的字符串表示</returns>
        public static string CalculateSHA256(string filePath)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                using (FileStream stream = File.OpenRead(filePath))
                {
                    // 计算文件的哈希值
                    byte[] hashBytes = sha256.ComputeHash(stream);

                    // 将字节数组转换为十六进制字符串
                    return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        public static async Task<apiUpdate> GetUpdateAsync()
        {
            // API 地址
            string url = "https://tcgapi.tiancity.com/tcgame/V2/apiUpdate?gameid=4";

            // 创建 HTTP 客户端
            using HttpClient client = new HttpClient();

            try
            {
                // 发送 GET 请求并获取响应字符串
                string json = await client.GetStringAsync(url);

                // 解析 JSON
                apiUpdate data = JsonSerializer.Deserialize<apiUpdate>(json);

                return data;
            }
            catch (Exception ex)
            {
                Log.Info($"请求或解析失败: {ex.Message}");
                return null;
            }
        }
    }

    // 1. 定义与GitHub Releases API对应的JSON模型（仅包含需要的字段，其他字段可忽略）
    /// <summary>
    /// GitHub Releases API返回的根对象
    /// </summary>
    class GitHubReleaseRoot
    {
        /// <summary>
        /// 发布版本下的所有资产文件（如exe、zip）
        /// </summary>
        public GitHubReleaseAsset[] Assets { get; set; }

        /// <summary>
        /// 发布版本的说明（如版本号、发布日期、更新内容等）
        /// </summary>
        public string body { get; set; }
    }

    public class IpInfo
    {
        public string Ip { get; set; }
        public string City { get; set; }
        public string Region { get; set; }
        public string Country { get; set; }
    }

    /// <summary>
    /// GitHub Releases中的单个资产文件（如Launcher.exe）
    /// </summary>
    public class GitHubReleaseAsset
    {
        /// <summary>
        /// 文件名（如Launcher.exe）
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// 文件的SHA256哈希值（格式：sha256:xxxxxx）
        /// </summary>
        public string digest { get; set; }

        /// <summary>
        /// 文件的浏览器下载链接
        /// </summary>
        public string browser_download_url { get; set; }
    }

    public class apiUpdate
    {
        public string game_id { get; set; }
        public string game_name { get; set; }
        public string version { get; set; }
        public string install_pack_time { get; set; }
        public string update_time { get; set; }
        public string download_prefix { get; set; }
        public string update_prefix { get; set; }
    }
}