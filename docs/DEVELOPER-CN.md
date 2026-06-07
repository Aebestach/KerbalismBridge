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

```powershell
.\scripts\package-release.ps1 -Version 1.0.0
```

在 `dist/` 下生成八个 zip：**KerbalismBridge**、**KerbalismProcess**、**KerbalismNative**、**KerbalismFFT**、**KerbalismDynamicRadiation**、**KerbalismCryo**、**KerbalismNFE**、**KerbalismSpaceDust**。每个 zip 含 `GameData/`、`LICENSE`、简要 `README.md` 以及从根目录 [CHANGELOG.md](../CHANGELOG.md) 截取的本包 `CHANGELOG.md`。

DLL 已构建时可加 `-SkipBuild`。

---

## 其他文档

| 路径 | 用途 |
|------|------|
| [../CHANGELOG.md](../CHANGELOG.md) | 各包功能、依赖、设置与版本历史 |
| [legal/ATTRIBUTION.md](legal/ATTRIBUTION.md) | Fork 与版权说明 |
| [../LICENSE](../LICENSE) | MIT 许可全文 |
