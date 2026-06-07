# 开发者指南

仓库概览：[../README-CN.md](../README-CN.md) · English: [DEVELOPER.md](DEVELOPER.md)

---

## 解决方案与安装包

用 Visual Studio 打开 `src/KerbalismBridge.sln`。KSP 程序集引用位于 `../KSPDLL/`（与仓库同级的 `C#/` 目录下）。

| 项目 | 输出 DLL | GameData 目录 |
|------|----------|---------------|
| zKerbalismBridge | `zKerbalismBridge.dll` | `GameData/zKerbalismBridge/PluginData/` |
| zKerbalismProcess | `zKerbalismProcess.dll` | `GameData/zKerbalismProcess/PluginData/` |
| zKerbalismNative | `zKerbalismNative.dll` | `GameData/zKerbalismNative/PluginData/` |
| zKerbalismFFT | `zKerbalismFFT.dll` | `GameData/zKerbalismFFT/PluginData/` |
| zKerbalismDynamicRadiation | `zKerbalismDynamicRadiation.dll` | `GameData/zKerbalismDynamicRadiation/PluginData/` |
| zKerbalismCryo | `zKerbalismCryo.dll` | `GameData/zKerbalismCryo/PluginData/` |
| zKerbalismNFE | `zKerbalismNFE.dll` | `GameData/zKerbalismNFE/PluginData/` |
| zKerbalismSpaceDust | `zKerbalismSpaceDust.dll` | `GameData/zKerbalismSpaceDust/PluginData/` |

构建 **Release**。全新树请先编 **Bridge**，再编 Process / Native（解决方案已声明项目依赖）。中间文件在各项目 `obj/` 下；DLL 直接输出到 `GameData/.../PluginData/`。

```text
msbuild src\KerbalismBridge.sln /p:Configuration=Release
```

所有插件通过 [zKerbalismPluginHost](https://github.com/Aebestach/KerbalismPluginHost) 加载（`*.host.xml` + `PluginData/`）。**请勿**把 Bridge DLL 放进 `Plugins/`。

---

## 架构

整合分 **Layer A（Process 层）** 与 **Layer B（Native 层）**。模块命名、补丁布局与设计规则见：

| 文档 | 语言 |
|------|------|
| [architecture/KerbalismBridge-Architecture.md](architecture/KerbalismBridge-Architecture.md) | 中文 |
| [architecture/KerbalismBridge-Architecture-en.md](architecture/KerbalismBridge-Architecture-en.md) | English |

**SterlingSystemsKerbalism** 为外部维护；本仓库仅在 `GameData/zKerbalismProcess/Patches/ModsSupport/SterlingSystems.cfg` 提供 FINAL 热桥。

---

## 发布打包

脚本位于 `scripts/`。请在**仓库根目录**执行（脚本会根据自身路径定位仓库，但示例命令均假定当前目录为根目录）。

| 文件 | 用途 |
|------|------|
| `scripts/package-release.ps1` | PowerShell 入口 |
| `scripts/package-release.cmd` | CMD 入口（内部调用上述 `.ps1`） |

**注意：** CMD **不能**直接运行 `.ps1`；Windows 往往会用编辑器打开脚本，而不是执行。在 CMD 中请使用 `package-release.cmd`。

### PowerShell

```powershell
cd path\to\KerbalismBridge
.\scripts\package-release.ps1 -Version 1.0.0
.\scripts\package-release.ps1 -Version 1.0.0-beta.1 -SkipBuild
```

若提示无法运行脚本，可在当前窗口临时放开策略后再执行：

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
```

### CMD

```text
cd /d path\to\KerbalismBridge
scripts\package-release.cmd -Version 1.0.0
scripts\package-release.cmd -Version 1.0.0 -SkipBuild
```

### 参数

| 参数 | 必填 | 说明 |
|------|------|------|
| `-Version` | 是 | 发布版本号，如 `1.0.0`、`1.0.0-beta.1` 或 `v1.0.0` |
| `-SkipBuild` | 否 | 跳过 MSBuild；要求各包 DLL 已在 `GameData/.../PluginData/` |
| `-OutputDir` | 否 | 输出目录，默认 `dist` |

默认会先以 **Release** 配置编译 `src\KerbalismBridge.sln`（需已安装 Visual Studio 或 Build Tools 且能找到 MSBuild）。加 `-SkipBuild` 时则只打包现有 DLL。

### 输出

在 `dist/` 下生成八个 zip：**KerbalismBridge**、**KerbalismProcess**、**KerbalismNative**、**KerbalismFFT**、**KerbalismDynamicRadiation**、**KerbalismCryo**、**KerbalismNFE**、**KerbalismSpaceDust**（文件名形如 `KerbalismBridge.v1.0.0.zip`）。

每个 zip 含 `GameData/`、`LICENSE`、简要 `README.md`，以及从根目录 [CHANGELOG.md](../CHANGELOG.md) 截取的本包 `CHANGELOG.md`。

成功时终端会打印 `Created KerbalismBridge.v...zip` 与 `Done. Packages in: ...\dist`。

---

## 其他文档

| 路径 | 用途 |
|------|------|
| [../CHANGELOG.md](../CHANGELOG.md) | 版本历史 |
| [legal/ATTRIBUTION.md](legal/ATTRIBUTION.md) | Fork 与版权说明 |
| [../LICENSE](../LICENSE) | MIT 许可全文 |
