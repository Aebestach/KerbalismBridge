# Kerbalism Bridge：Process 层 / Native 层

**Kerbalism Bridge** 是 Kerbalism 与第三方模组之间的整合桥：**资源走 Kerbalism**；**热量**在需要时走 SystemHeat 回路，或由模组原生逻辑自行处理。

整合分 **两层**（文档中仍可用旧称 **Layer A = Process 层**、**Layer B = Native 层**）。按部件**原本用什么模块**来选，同一部件不要混用。

---

## 工程与安装包

三个 DLL 构成主桥；其余为可选卫星。**SterlingSystemsKerbalism 不在本仓库**，由 Sterling Systems 自行维护。

```
KerbalismBridge/                         ← 仓库 / 解决方案
│
├── 【主桥 · 三个 DLL】
│   GameData/zKerbalismBridge/           ← 运行时基础（Harmony、背景热、编辑器仿真）
│   GameData/zKerbalismProcess/          ← Process 层（ProcessControllerSystemHeat 等）
│   GameData/zKerbalismNative/           ← Native 层（*KerbalismUpdater、各 mod Harmony）
│
└── 【卫星 · 可选】
    GameData/zKerbalismFFT/              ← FFT 配方 / 工业 Process 补丁 / Fusion MM
    GameData/zKerbalismDynamicRadiation/ ← 独立 DLL：关堆后辐射衰减
    GameData/zKerbalismCryo/             ← CryoTanks + SH 低温罐 Layer B
    GameData/zKerbalismNFE/              ← NFE 电容 / 回收机 Layer B
    GameData/zKerbalismSpaceDust/        ← SpaceDust 采集器 Layer B

【外部 · 玩家另装，不合入本仓库】
    SterlingSystems                      ← 部件本体
    SterlingSystemsKerbalism             ← Sterling 的 Kerbalism Profile + Process 前半段 cfg
```

| 包 | DLL | 职责 |
|----|-----|------|
| **zKerbalismBridge** | 有 | 共用运行时；不含 Process / Updater 模块 |
| **zKerbalismProcess** | 有 | Kerbalism **替代式**整合；`:NEEDS[zKerbalismBridge]`；热相关 patch 另 `:NEEDS[SystemHeat]` |
| **zKerbalismNative** | 有 | **Layer B 核心**：通用 SystemHeat Updater + 裂变堆/机；`:NEEDS[zKerbalismBridge,SystemHeat]` |
| **zKerbalismNFE** | 有 | NFE 电容、核回收机（Layer B 卫星） |
| **zKerbalismSpaceDust** | 有 | SpaceDust 采集器（Layer B 卫星） |
| **zKerbalismCryo** | 有 | CryoTanks / SH 低温罐（Layer B 卫星） |
| **zKerbalismFFT** | 有 | Profile、工业厂 Layer A cfg、Fusion 挂 Updater 的 MM |
| **zKerbalismDynamicRadiation** | 有 | 可选玩法；软依赖已整合的堆/发动机 |
| **SterlingSystemsKerbalism** | 无 | Sterling 维护；本仓库仅 **`ModsSupport/SterlingSystems.cfg`** 做 FINAL 热桥 |

依赖关系：

```
Kerbalism
    └── zKerbalismBridge
            ├── zKerbalismProcess  ←── SystemHeat（可选，回路热）
            └── zKerbalismNative   ←── SystemHeat / FFT / NFE …（按 patch）
```

**NFE、SpaceDust、Cryo** 等第三方 Layer B 整合在**卫星 DLL**（`zKerbalismNFE`、`zKerbalismSpaceDust`、`zKerbalismCryo`）。**zKerbalismNative** 仅保留 SystemHeat 通用核心。FFT 工业厂、Sterling ISRU / **Fuel Cell** 等仍属 **Process 层** cfg。

---

## 一句话对比

| | **Process 层**（Layer A） | **Native 层**（Layer B） |
|---|---------------------------|---------------------------|
| **适用** | Stock / Kerbalism 可替代的转换器、采集、**燃料电** | Mod **自定义原生** C# 模块 |
| **做法** | 换成 Kerbalism `ProcessController` / `Harvester`；可选升为 `*SystemHeat` | **保留**原生模块，旁挂 `*KerbalismUpdater` |
| **资源** | Kerbalism 流程 + Broker | Harmony 拦截原生 IO，Kerbalism 记账 |
| **热量** | 可选：`ProcessControllerSystemHeat` + `ModuleSystemHeat` 回路 | 有 SH 时原生模块产热；无 SH 时由 mod 自管（如 FFT 聚变） |
| **配方** | 需要 Kerbalism Profile + Configure | 一般**不需要**再写 ISRU Profile |
| **SystemHeat** | **可选**（要回路废热才装 Process + SH patch） | **可选**（有 SH 原生模块则接 Updater；无 SH 仍可 Native 整合） |

---

## Process 层 — Kerbalism 替代路径

**典型部件：** `ModuleResourceConverter`、`ModuleResourceHarvester`（Kerbalism 化学厂 / 钻机 / 泵、Sterling 圆形精炼厂、**金属燃料电**、FFT 工业厂等）。

**流程：**

1. MM 将部件改为 Kerbalism **`ProcessController`** / **`Harvester`**（+ Profile、Configure）
2. 若装 SystemHeat 且需要回路热：`zKerbalismProcess` 将 `ProcessController` → **`ProcessControllerSystemHeat`**（采集 → **`HarvesterSystemHeat`**），并补 `ModuleSystemHeat`
3. 资源走 Kerbalism；回路温度影响效率（启用 SH 时）

**例子：**

- Kerbalism 默认化学厂 / 钻机
- Sterling **`ConvertersMode0`** + **`Profile.cfg`**（SterlingSystemsKerbalism）；本仓库 **`SterlingSystems.cfg`** 负责 FINAL 热参数
- Sterling **`SystemHeatFuelCells.cfg`**：`Configure title = Fuel Cell` → **Process 层**，不是 Native 层
- FFT 工业厂：`FFTIndustrialConverters.cfg` + `FarFutureTechnologies.cfg`（Process + SH）

**Fuel Cell：** 与化学厂同族——Kerbalism `ProcessController` + Fuel Cell 式 `Configure`；Sterling MAEC 走 Process 层，**不是**保留 `ModuleSystemHeatConverter` 的 Native 路径。

---

## Native 层 — 原生模块旁路

**典型部件：** Mod 自带 C# 模块——`ModuleSystemHeatConverter`、`FusionReactor`、`DischargeCapacitor`、`ModuleSpaceDustHarvester` 等。

**流程：**

1. **不替换**原生 C# 模块（UI、曲线、mod 逻辑照旧）
2. 增加 **`*KerbalismUpdater`** 旁路
3. Harmony **屏蔽**原生模块对资源的直接读写
4. 热量：有 SystemHeat 时仍走原生 `UpdateFlux()` 等；无 SH 时留在 mod 内（FFT 聚变、NFE 电容等）

**例子：**

- NFE 核回收机（`ModuleSystemHeatConverter` + Updater）
- FFT 聚变堆 / 聚变发动机（`Fusion*KerbalismUpdater`，DLL 在 **zKerbalismNative** / **zKerbalismFFT**）
- NFE 电容（`NFECapacitorKerbalismUpdater`，**zKerbalismNFE**）
- SystemHeat 裂变堆 / 发动机（**zKerbalismNative**）
- SpaceDust 采集（**zKerbalismSpaceDust**）

---

## 怎么判断用哪一层？

```
部件上是 ModuleResourceConverter / ModuleResourceHarvester
（或已被 Kerbalism / mod 适配包换成 ProcessController）
  → Process 层

部件上是 mod 自定义原生模块（ModuleSystemHeat*、FusionReactor、DischargeCapacitor …）
  → Native 层（加 Updater，不要整段换成 ProcessControllerSystemHeat）
```

---

## 和旧版 0.5 的区别

旧版 **`SystemHeatConverterKerbalism`** 把许多本应 Native 层的部件也整段替换掉。

**Bridge 架构：** Process 层用 `ProcessControllerSystemHeat`（可选 SH）；Native 层用 Updater——**行为跟模组作者，资源跟 Kerbalism**。

---

## C# 命名空间

| 程序集 | 命名空间 | 职责 |
|--------|----------|------|
| zKerbalismBridge | `KerbalismBridge` | 运行时、背景热、编辑器仿真 |
| zKerbalismProcess | `KerbalismProcess` | ProcessControllerSystemHeat、HarvesterSystemHeat |
| zKerbalismNative | `KerbalismNative` | *KerbalismUpdater、NFE 电容、裂变 Harmony |

仓库与解决方案：**`KerbalismBridge`**（`src/KerbalismBridge.sln`）。
