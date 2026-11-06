# AGENTS.md — 动代码前必读（一页）

> LiteGameNext 客户端重写工程。架构/目录/Lua/迁移见 `Docs/设计文档/`（01~04），实际进度见 `Docs/进度/迁移进度.md`。
> 本文件只保留**能被一个脚本检查、或一次评审判定**的规则。想加规则先回答：**谁检查它**？答不出就不写。

## 一、机检红线（`python Tools/verify.py`）

| # | 规则 | 判据 |
|---|---|---|
| 1 | **预算** | 运行时 ≤ 25,000 行 / 220 文件；asmdef ≤ 5；interface ≤ 20；单文件 ≤ 400 行；单目录 ≤ 20 文件 |
| 2 | **依赖方向** | `Assets/Scripts/Core` 不出现 `LiteGame.Game/Ui/Boot/Features`；Core.asmdef 不引用 `LiteGame.Game`（AOT 边界，01 §15） |
| 3 | **结构** | 禁 `Abstractions/`、`Utils/`、`Helpers/`、`Common/` 目录；禁裸 `GameObject.Find(`；禁 public 可变字段（改 readonly 或构造注入） |
| 4 | **命名** | 主流 C#/.NET 规约（02 §3.1）：类型/方法 PascalCase、私有字段 `_camelCase`、常量 PascalCase、异步 `Async` 后缀、文件名=类型名；偏离只走例外登记（附 reason + approvedBy）；测试方法允许 `A_B_C` 分段；概念用词见表（02 §3.6，一个概念一个词） |
| 5 | **卫生** | 禁 `Debug.Log*`（Editor 与 `Log.cs` 除外）；catch 必须三选一（记日志/抛出/返回失败值）；禁裸 `.Forget()`；禁 `TODO/FIXME`；禁密钥字面量；Core 非 Host 不直读 `Time` |
| 6 | **注释** | XML 注释 ≤4 行；类型块注释 ≤3 行；成员 ≤2 行（生成物豁免） |
| 7 | **文档** | 相对链接有效；设计文档不写施工状态（`已交付/已实施/待提交/批次X`）；文档 ≤8 篇 / 2,000 行 |

## 二、评审红线（10 条，review 判定）

7. **禁硬编码**：魔法数字/字符串进常量或表；跨层同一常量 C# 单源；路径字面量禁中文。
8. **一个功能只在一侧**：C# 管引擎与性能路径，Lua 管页面与可热更数据；同一功能不得两侧各写一份（03 §1）。
9. **UI 单写者**：页面开关只走 `ScreenManager`；页面不得自己切状态；控件计时/动效参数收口。
10. **错误契约**：程序员错误 fail-fast；内容降级 `Log.Warning` + 显式默认值；禁吞异常静默继续（01 §10）。
11. **参数校验**：公共方法先校验引用参数与范围参数，用 `nameof` 抛 `ArgumentException`/`ArgumentOutOfRangeException`；只有边界层（网络/UI 回调）返回失败值。
12. **生命周期**：`IDisposable` 实现幂等、`Dispose` 不抛；未启动/已释放时访问核心成员抛 `InvalidOperationException`（同层统一一种）。
13. **异常边界**：catch 三选一——记日志 / 抛出 / 返回可判断失败值；禁静默吞掉。
14. **不可变优先**：禁 public 可变字段；集合暴露 `IReadOnlyList`/`IReadOnlyDictionary`；DTO 用只读属性。
15. **纯核心**：Core 非 Host 代码不碰 `Time`/`Random`/`File`/`PlayerPrefs` 的静态入口，一律经注入。
16. **异步纪律**：`CancellationToken` 贯穿到最内层；禁裸 `.Forget()`；异步方法 `Async` 结尾。

## 三、流程（三条，缺一不算收尾）

1. **动手前**：查 01 §11 预算与 §12 反清单。要突破预算，先写清"破了什么 / 为什么非破不可 / 怎么收回"。
2. **收尾**：编译 0 error → `python Tools/verify.py` 全绿 → 可交互面走**真实操作**验证（可点性自检 → 真实点击 → 副作用断言 → 截图），禁直调 API 冒充"操作过"。
3. **回写**：设计变更进 01~04；状态进 `Docs/进度/迁移进度.md`（设计文档不写"已完成/已交付"）。

## 四、预算速查（超限即停手）

| 项 | 上限 | 项 | 上限 |
|---|---|---|---|
| 运行时行数 | 25,000 | 单文件行数 | 400 |
| 运行时文件 | 220 | 单目录文件 | 20 |
| asmdef | 5 | interface | 20 |

## 五、反清单（不做的事）

不自研 DI 容器/模块系统/作用域；不做泛型 FSM/事件总线/对象池框架；不做 UI 绑定代码生成；不做租约/代次/事务化热更；不做 ECS/DOTS；不重写服务端与共享包。完整 16 条见 01 §12。

## 六、编辑器通道（必装 unity-pipeline）

- 包：`Packages/com.unity.pipeline/`（Unity 官方 0.7.0-exp.1 + 2022.3 兼容补丁），**不入库**；新克隆/换机跑 `python Tools/restore_pipeline.py`。
- CLI：`unity`（1.0.0-beta.2，已在 PATH）；`python Tools/verify.py --pipeline` 复查。
- 纪律：场景/Prefab/Material/SO/AnimationClip/AnimatorController/`.meta` 的创建修改，以及 `Assets/` 下文件/目录的移动改名删除，**一律走 CLI**；禁手改序列化文件与 GUID。CLI 不可达时只改纯文本，并在结论里写明"序列化资源未验证"（02 §5.1）。

## 七、共享包只读

`Packages/com.litegame.{sim,net,sync}` 迁移期以 `file:` 引用旧仓（`..\Test`），**不改、不复制**。需要改 → 回旧仓 + `toolbox.py build-hash`，再让新工程跟随（01 §15、02 §6）。

## 八、读文档顺序

01 架构（分层/启动/帧循环/预算）→ 02 目录与程序集（落点/命名/预算）→ 03 Lua 桥（改页面必读）→ 04 迁移（只改客户端时看处置判据）。
