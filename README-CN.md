# Kerbalism Bridge

**Kerbalism Bridge** 将 Kerbalism 与 **SystemHeat**、**Near Future Electrical**、**Far Future Technologies** 以及可选的**动态辐射**整合在一起。本仓库一次构建多个可安装的 `GameData` 包：**主桥**（三个 DLL）与按需添加的**卫星** mod。

**版本：** 1.0.0（模组族首发）

所有 Bridge 插件在 Kerbalism 加载后，由 [zKerbalismPluginHost](https://github.com/Aebestach/KerbalismPluginHost) 从各 mod 的 `PluginData/` 加载。**请勿**把 Bridge DLL 放进 `Plugins/`。

---

## 整合方式：Layer A 与 Layer B

Bridge **不会**对所有部件使用同一种整合方式，而是分为两层（旧称 **Layer A**、**Layer B**）。按部件**原本使用的模块类型**选择一层，**同一部件不要混用两层**。

| | **Layer A — Process 层（Process layer）** | **Layer B — Native 层（Native layer）** |
|---|------------------------------------------|------------------------------------------|
| **对应包 / DLL** | `zKerbalismProcess` | `zKerbalismNative` |
| **适用** | 可被 Kerbalism 替代的转换器、采集器、**燃料电** 等 | Mod **自定义原生** C# 模块 |
| **做法** | MM 改为 Kerbalism `ProcessController` / `Harvester`；需要回路热时升级为 `ProcessControllerSystemHeat` / `HarvesterSystemHeat` | **保留** mod 原生模块，旁挂 `*KerbalismUpdater` |
| **资源** | Kerbalism 流程、Broker、背景模拟 | Harmony 拦截原生资源 IO，由 Kerbalism 记账 |
| **热量** | 可选：通过 `ModuleSystemHeat` 接入 SystemHeat 回路 | 仍由原生模块产热（如 `UpdateFlux()`）；视 mod 而定，可带或不带 SystemHeat |
| **配方** | 通常需要 Kerbalism Profile + Configure | 一般**不需要**额外 ISRU Profile |

**快速判断：**

```
部件上是 ModuleResourceConverter / ModuleResourceHarvester
（或适配包已换成 ProcessController）
  → Layer A（Process 层）

部件上是 mod 原生模块（ModuleSystemHeat*、FusionReactor、DischargeCapacitor …）
  → Layer B（Native 层）— 加 Updater，不要整段换成 ProcessControllerSystemHeat
```

**举例**

- **Layer A：** Kerbalism 化学厂 / 钻机；Sterling MAEC **燃料电**；FFT 工业冶炼厂（Process + 可选 SystemHeat）。
- **Layer B：** SystemHeat 裂变堆 / 发动机；NFE 电容与 SH 回收机；FFT 聚变堆 / 聚变发动机；SpaceDust 采集。

详细架构说明：[docs/architecture/KerbalismBridge-Architecture.md](docs/architecture/KerbalismBridge-Architecture.md)（English: [KerbalismBridge-Architecture-en.md](docs/architecture/KerbalismBridge-Architecture-en.md)）。

---

## 安装包结构

### 主桥（SystemHeat 整合的最低配置）

典型 SystemHeat + Kerbalism 环境请安装以下三个包。

| GameData 文件夹 | DLL | 职责 |
|-----------------|-----|------|
| `zKerbalismBridge` | `zKerbalismBridge.dll` | **共用运行时** — Harmony 引导、背景热模拟、编辑器仿真、设置。既非 Layer A 也非 Layer B，但 Process / Native 均依赖它。 |
| `zKerbalismProcess` | `zKerbalismProcess.dll` | **Layer A（Process 层）** — `ProcessControllerSystemHeat`、`HarvesterSystemHeat`、转换器 / 采集 / 散热器 MM |
| `zKerbalismNative` | `zKerbalismNative.dll` | **Layer B（Native 层）** — `*KerbalismUpdater`、裂变、NFE 电容 / 回收机、SpaceDust 等 |

加载顺序：**Bridge → Process / Native**（各 `*.host.xml` 通过 `RequireAssembly` 声明依赖 `zKerbalismBridge`）。

```
Kerbalism
    └── zKerbalismBridge          ← 运行时
            ├── zKerbalismProcess ← Layer A（patch 需要时再 :NEEDS[SystemHeat]）
            └── zKerbalismNative  ← Layer B（按 mod 分 :NEEDS[...]）
```

### 卫星包（可选）

| GameData 文件夹 | DLL | 职责 |
|-----------------|-----|------|
| `zKerbalismFFT` | `zKerbalismFFT.dll` | Far Future Technologies — 配方、工业厂 **Layer A** cfg、聚变 / 反物质 **Layer B** C# |
| `zKerbalismDynamicRadiation` | `zKerbalismDynamicRadiation.dll` | 已整合的裂变 / 聚变部件关堆后的辐射衰减 |
| `zKerbalismResourceAudit` | `zKerbalismResourceAudit.dll` | 静态扫描未走 Kerbalism/Bridge 的资源模块，报告写入 `Logs/` |

**NFE 电容** 已并入 **`zKerbalismNative`**（无独立 NFE 包）。**SterlingSystemsKerbalism** 由 Sterling Systems 维护；本仓库仅在 Process 层提供 `SterlingSystems.cfg` 作为 FINAL 热桥。

---

## 依赖

| 必需 | 说明 |
|------|------|
| [Kerbalism](https://github.com/Kerbalism/Kerbalism) 3.32+ | Bootstrap `*.kbin` 工作流 |
| [zKerbalismPluginHost](https://github.com/Aebestach/KerbalismPluginHost) | Bridge DLL 延迟加载 |
| [Module Manager](https://github.com/sarbian/ModuleManager) | 补丁 |

各包额外依赖（SystemHeat、FFT、NFE 等）：见 [docs/mods/](docs/mods/) — **面向玩家的说明，也会打进发布 zip**。

---

## 安装步骤

1. 安装 Kerbalism、Module Manager 与 **zKerbalismPluginHost**。
2. 删除旧版 `GameData/zKerbalismSystemHeat`、`GameData/zKerbalismNFE`，以及 `Plugins/` 里任何 Bridge DLL 副本。
3. 将 **`zKerbalismBridge` + `zKerbalismProcess` + `zKerbalismNative`** 复制到 `GameData`（主桥最低配置）。
4. 若使用对应 mod，再安装 `zKerbalismFFT` / `zKerbalismDynamicRadiation`。
5. 删除 `ModuleManager.ConfigCache` 并重启 KSP。

---

## 编译

用 Visual Studio 打开 `src/KerbalismBridge.sln`，构建 **Release**。KSP 引用路径：`../KSPDLL/`（与仓库同级的 `C#/` 下）。

```text
msbuild src\KerbalismBridge.sln /p:Configuration=Release
```

输出：

```text
GameData/zKerbalismBridge/PluginData/zKerbalismBridge.dll
GameData/zKerbalismProcess/PluginData/zKerbalismProcess.dll
GameData/zKerbalismNative/PluginData/zKerbalismNative.dll
GameData/zKerbalismFFT/PluginData/zKerbalismFFT.dll
GameData/zKerbalismDynamicRadiation/PluginData/zKerbalismDynamicRadiation.dll
```

全新树请先编 **Bridge**，再编 Process / Native（解决方案已声明项目依赖）。

---

## 发布打包

```powershell
.\scripts\package-release.ps1 -Version 1.0.0
```

生成五个 zip：**KerbalismBridge**、**KerbalismProcess**、**KerbalismNative**、**KerbalismFFT**、**KerbalismDynamicRadiation**。每个 zip 内含对应 `docs/mods/` 下的 README。

---

## 文档索引

| 路径 | 用途 |
|------|------|
| [README.md](README.md) | English repo overview |
| [docs/architecture/](docs/architecture/) | Process / Native（Layer A / B）架构说明 |
| [docs/mods/](docs/mods/) | 各可安装包的说明（亦随发布包分发） |
| [CHANGELOG.md](CHANGELOG.md) | 版本历史 |
| [docs/legal/ATTRIBUTION.md](docs/legal/ATTRIBUTION.md) | Fork 与版权说明 |

---

## 许可

见 [LICENSE](LICENSE)。运行时依赖仍遵循各自许可，需自行获取。
