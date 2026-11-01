# 跑跑卡丁车 · 单机版启动器（定制版）

基于 [yanygm/Launcher_V2](https://github.com/yanygm/Launcher_V2) 的 **261031** 版本修改而来，在原版基础上补充了服务器管理与界面相关的功能。

> 原版发布页：https://github.com/yanygm/Launcher_V2/releases/tag/261031
> 本项目保留了原版的全部核心功能，改动集中在「服务器地址管理」和「界面 / 外观」两块。

## 下载

直接到 **[Releases](https://github.com/cookieskuqi/KartRider-Launcher/releases/latest)** 下载 `Launcher.exe`（单文件，需要 [.NET 8 桌面运行时](https://dotnet.microsoft.com/download/dotnet/8.0)）。

`latest` 这个 Release 会随每次推送到 `main` 自动更新；也可以自己 clone 下来编译，见下方「构建」。

## 相比原版新增的功能

### 服务器地址管理

- **IP 快捷选择**：设置里的 IP 输入框改为可编辑下拉框，历史地址一点即切
- **地址 + 备注**：「添加」按钮可以给每条服务器起名字（如「本机」「朋友服」），列表显示成 `备注 (IP)`
- **切换即生效**：选中即写入配置并在后台重启本地监听（IPv4 走 `RouterListener`，IPv6 走 `TinyMapper`），不再阻塞界面
- **旧配置自动兼容**：`SavedServers` 支持老版本的纯字符串数组，升级不会丢存档

### 界面

- 重新设计的界面：无边框自绘标题栏、左侧导航、圆角卡片、自绘按钮 / 下拉框 / 输入框 / 滑块
- **自定义背景图**：支持「右侧内容区」和「铺满整个窗口」两种范围，可调图片透明度与卡片透明度
- **深色模式**：整套色板可切换，设置里勾选即时生效
- **毛玻璃**：把背景图模糊后垫在侧栏、标题栏与卡片下方
- **公告栏**：主界面底部的公告卡片，支持本地 `notice.txt` 或 http(s) 网址，带离线缓存

### 其他

- **关闭启动器自更新**：自定义构建的 SHA256 与官方发布不一致，默认跳过版本检查（`Settings.json` 里 `SelfUpdate` 可改回）
- **修复控制台黑窗**：控制台改为按需创建，默认启动不再弹窗
- **修复 STAThread 失效**：`Main` 改为同步方法，UI 线程真正运行在 STA 上（此前是 MTA，会导致文件对话框异常）

## 使用方法

1. 下载或自行编译 `Launcher.exe`
2. 放到游戏目录（与 `KartRider.exe`、`KartRider.pin` 同级），覆盖原启动器
3. 运行后在「设置」里配置服务器地址，点「启动游戏」

配置保存在游戏目录的 `Profile\Settings.json`。

### 公告栏格式

`Profile\notice.txt`（首次运行会自动生成示例）：

```
2026-11-01 服务器维护通知
今晚 20:00 - 21:00 进行例行维护。

---
2026-10-30 新赛道与赛车
本次更新新增 3 张赛道、2 辆赛车。
```

- 每条公告之间用**单独一行 `---`** 分隔
- 每条第一行是标题，前面可以带日期（`2026-11-01` / `11-01` / `2026/11/01`）
- 其余行是正文，自动换行

也可以把这份文件放到 GitHub Pages 等静态托管上，在「公告 → 编辑」里填网址，所有玩家就能看到同一份公告。

## 构建

需要 .NET 8 SDK：

```bash
dotnet publish Launcher.csproj --runtime win-x64 -p:PublishSingleFile=true -p:AssemblyName=Launcher -o PublishedWinExe -c Release --no-self-contained
```

产物为单文件 `PublishedWinExe\Launcher.exe`（框架依赖，需安装 .NET 8 桌面运行时）。

## 目录结构

```
KartRider.Data/      启动器主体（窗体、服务器、数据解析）
  Forms/             主窗口、设置、公告设置等窗体
  Ui/                自绘控件与主题（Theme / FlatButton / CardPanel / ChromeForm 等）
  Notice/            公告读取与解析
  Profile/           配置与玩家档案
KartriderLibrary/    协议、加密、LPK 等底层库
```

## 致谢与许可

- 本项目基于 [yanygm/Launcher_V2](https://github.com/yanygm/Launcher_V2)（tag `261031`）修改
- 上游项目本身又基于 [Launcher.cn_3075](https://github.com/MyPuppy/Launcher.cn_3075) 等开源项目，详见 [CONTRIBUTING.md](CONTRIBUTING.md)
- 沿用上游的 **Academic Free License 3.0**，见 [LICENSE.md](LICENSE.md)

原版作者与相关开源项目的作者版权归其各自所有，本项目仅在其基础上做功能补充。
