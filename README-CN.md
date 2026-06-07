# Kerbalism Bridge

<div align="center">

<img src="https://imgur.com/ON8D2wc.jpg" alt="Banner"/>

</div>
[English](README.md) · **中文**

Kerbalism Bridge 让 **Kerbalism** 能管上各类先进动力与热管理部件——反应堆、转换器、采集器、电容、低温罐等。这些部件会出现在 **Kerbalism 规划器**里，**飞行中**从 Kerbalism 资源池取料，**未加载飞船**在时间加速时仍会背景模拟，而不是停在卸载那一刻的状态。

发布包拆成多个可选的 `GameData` 目录：**主桥**（SystemHeat 最低需求）与按需安装的**卫星** mod。

---

## 传承与改进

本项目继承自 [judicator/KerbalismSystemHeat](https://github.com/judicator/KerbalismSystemHeat) 与 [judicator/KerbalismFFT](https://github.com/judicator/KerbalismFFT)（原作者 Alexander Rogov），现由 [Aebestach/KerbalismBridge](https://github.com/Aebestach/KerbalismBridge) 维护，**并非** judicator 官方发布。

**相对上游 KerbalismSystemHeat**，Bridge 保留核心理念（SystemHeat 部件走 Kerbalism 资源、规划器支持、背景模拟），并新增：

- **在轨（已加载）飞船**的裂变堆 / 发动机 Kerbalism 资源整合（上游主要覆盖未加载飞船）。
- **未加载飞船的 SystemHeat 回路背景热模拟**，避免长时间时间加速后回路温度仍停在卸载时的数值。
- **Layer A / Layer B 双层整合**，不再对所有部件使用同一种方式（见下文）。
- **可选卫星包**：NFE 电容、SpaceDust 采集、CryoTanks、动态辐射衰减等。
- 通过 **zKerbalismPluginHost** 从 `PluginData/` 加载（**请勿**把 Bridge DLL 放进 `Plugins/`）。

**相对上游 KerbalismFFT**，本 fork 保留反物质储存、聚变堆 / 发动机规划器与背景行为、科学与可靠性补丁、FFT 工业厂等，并改进：

- **在轨聚变堆**的电力与推进剂也走 Kerbalism（不仅背景模拟）。
- 安装主桥时，聚变废热纳入 Bridge **背景热模拟**。
- **CryoTanks** 整合拆到独立卫星 **zKerbalismCryo**，便于维护。

版本历史见 [CHANGELOG.md](CHANGELOG.md)。

---

## Layer A 与 Layer B（简要）

Bridge **不会**对所有部件使用同一种整合方式。每个部件只选**一层**：

| | **Layer A — Process 层** | **Layer B — Native 层** |
|---|--------------------------|-------------------------|
| **对应包** | `zKerbalismProcess` | `zKerbalismNative` + 可选卫星 |
| **适用** | 转换器、采集器、燃料电等 | 模组原生模块（裂变、聚变、NFE 电容、SpaceDust、低温罐等） |
| **做法** | 部件运行 Kerbalism 流程；可选接入 SystemHeat 回路热 | 保留原生模块，旁挂整合器，资源经 Kerbalism 记账 |

**快速判断：** 已是 Kerbalism `ProcessController` / `Harvester` 的部件 → **Layer A**。带模组专用模块（SystemHeat 裂变、FFT 聚变、NFE 电容等）→ **Layer B**。同一部件不要混用两层。

---

## 安装包结构

### 主桥（SystemHeat 最低配置）

| GameData 文件夹 | 职责 |
|-----------------|------|
| `zKerbalismBridge` | 共用运行时 — 背景热模拟、编辑器仿真、设置 |
| `zKerbalismProcess` | Layer A — 转换器、采集器、散热器 |
| `zKerbalismNative` | Layer B 核心 — SystemHeat 裂变、通用 SH 转换器 / 采集器 |

### 卫星包（可选）

| GameData 文件夹 | 职责 |
|-----------------|------|
| `zKerbalismFFT` | Far Future Technologies — 反物质、聚变、科学、工业厂 |
| `zKerbalismDynamicRadiation` | 已整合裂变 / 聚变部件关堆后的辐射衰减 |
| `zKerbalismCryo` | CryoTanks + SystemHeat 低温罐 |
| `zKerbalismNFE` | Near Future Electrical — 放电电容 |
| `zKerbalismSpaceDust` | SpaceDust 采集器 |
| `zKerbalismSterlingSystems` | Sterling Systems — 裂变、转换器、发动机等（现由本仓库维护；感谢 [JadeOfMaar](https://github.com/JadeOfMaar)、[NerdyBoy](https://github.com/NerdyBoy709) 此前对 SterlingSystemsKerbalism 的编写） |

---

## 依赖

| 必需 | 说明 |
|------|------|
| [Kerbalism](https://github.com/Kerbalism/Kerbalism) 3.32+ | Bootstrap `*.kbin` 工作流 |
| [zKerbalismPluginHost](https://github.com/Aebestach/KerbalismPluginHost) | Bridge DLL 延迟加载 |
| [Module Manager](https://github.com/sarbian/ModuleManager) | 补丁 |

---

## 安装步骤

1. 安装 Kerbalism、Module Manager 与 **zKerbalismPluginHost**。
2. 删除旧版 `GameData/zKerbalismSystemHeat` 以及 `Plugins/` 里任何 Bridge DLL 副本。若从旧版 Bridge 升级，请安装新的 **`zKerbalismNFE`** 卫星（NFE 电容不再随 Native 内置）。
3. 将 **`zKerbalismBridge` + `zKerbalismProcess` + `zKerbalismNative`** 复制到 `GameData`（主桥最低配置）。
4. 按需安装卫星：`zKerbalismFFT`、`zKerbalismNFE`、`zKerbalismSpaceDust`、`zKerbalismCryo`、`zKerbalismDynamicRadiation`、`zKerbalismSterlingSystems` 等。
5. 删除 `ModuleManager.ConfigCache` 并重启 KSP。

---

## 设置

可调默认值在各包的 `Settings.cfg` 中：

| 文件 | 主要项 |
|------|--------|
| `zKerbalismBridge/Settings.cfg` | `BackgroundThermalSim`、`BackgroundRadiatorCoefficient` |
| `zKerbalismDynamicRadiation/Settings.cfg` | `Reactor_*` / `Engine_*` 最低辐射与衰减率 |
| `zKerbalismCryo/Settings.cfg` | `Enabled` |
| `zKerbalismFFT/Settings.cfg` | 反物质背景湮灭；FFT 辐射系数 |

---

## 文档

| 路径 | 用途 |
|------|------|
| [CHANGELOG.md](CHANGELOG.md) | 版本历史 |
| [docs/DEVELOPER-CN.md](docs/DEVELOPER-CN.md) | 编译、发布与架构（开发者） |
| [docs/legal/ATTRIBUTION.md](docs/legal/ATTRIBUTION.md) | Fork 与版权说明 |

---

## 许可

见 [LICENSE](LICENSE)。运行时依赖仍遵循各自许可，需自行获取。
