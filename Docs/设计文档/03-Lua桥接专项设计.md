# 03 Lua 桥接专项设计

> **版本**：v0.2（2026-11-06）
> **定位**：裁决 C#/Lua 的边界——运行期 5 件、API 面、页面契约、节点寻址、跨边界数据规则、热更装载、性能纪律、旧页面迁移兼容清单，以及与 HybridCLR 的分工。
> **不做的事**：不裁决 UI 布局与视觉（见各页面预制体）、不裁决 Lua 语言规范细节、不引入 Lua 侧框架（无中间件/无 Lua 端 DI）。
> **冲突裁决**：与 01 §7 UI 架构冲突时以 01 为准（UI 单写者归 C#）；与旧仓 `Bridge` 契约冲突时**以本文为准**，旧页面按 §9 适配。
> **关联**：[01 架构总设计 §7](01-客户端架构总设计.md) · [02 目录 §2](02-目录与程序集分层设计.md)

---

## 1. 为什么保留，以及边界在哪

保留 Lua 的唯一理由是**页面逻辑与文案/数值可热更**（不重装客户端）。因此边界只按"可热更性"划，不按"哪边写着舒服"划：

| 归 Lua（可热更） | 归 C#（不热更） |
|---|---|
| 页面业务逻辑：按钮流程、状态文案、列表投影、防重入、轮询节拍 | 页面生命周期与栈、预制体加载、控件类型与布局 |
| 表格数据的读取与展示（地图名、武器名、文案） | 表格装载与校验、`ConfigDigest`、协议 DTO |
| 交互到能力的调用（建房/进房/开局/设置） | 网络、判定、输入、相机、资源包 |
| 页面级文案与格式（`text.Format`） | 文本表装载、语言切换 |

**一个功能只在一侧实现**（01 R6）。旧工程同一逻辑两侧各写一份（如房间事实既在 C# 又在 `Bridge.room`），新工程禁止：`Bridge` 只做**转发**，不持有业务状态。

`LuaPanda.lua`（3,333 行调试器）不进包：需要时从 `Assets/ThirdParty` 临时接入。

---

## 2. 运行期结构（6 件，≤ 900 行）

| 件 | 位置 | 行数预算 | 职责 |
|---|---|---|---|
| `LuaHost` | `Core/Lua` | ≤ 250 | `LuaEnv` 生命周期：Start / Reload / Dispose；装载 Lua 根目录；`main.lua` 执行；全局错误处理 |
| `Bridge`（部分类） | `Game/Bridge` | ≤ 500（9 文件） | 把 C# 能力注册成 8 张 Lua 表；只做转发，不做缓存 |
| `LuaPageProxy`（部分类） | `Core/Lua` | ≤ 200 × 2 文件 | 注入到页面的 `self.ui`；按 key 找控件并调用；缺件降级（基类 / 控件各一文件） |
| `LuaPage` | `Core/Lua` | ≤ 120 | Lua 页实例：`require` + `new` + 生命周期转发 + 错误隔离 + 10 Hz 节拍 |
| `LuaCallback` | `Core/Lua` | ≤ 80 | C# → Lua 回调：主线程、pcall、页面 token 校验、异常日志一次 |
| `LuaScreen`（MonoBehaviour） | `Game/Ui` | ≤ 80 | 预制体上的 Lua 页宿主：建 `LuaPage`、转发 `OnShow/OnReveal/OnHide/OnUpdate` |

**没有**：三张注册表、`RegistryFiller`、`LuaRegistryRefillService`、`LuaBehaviourAdapter`（257 行 + 生成 54 行）、`UIApiShim` 生成物、静态门面 `Bind(...)` 链。旧这套的价值是"环境重建后按表补注册"；新工程整包重载（§7），不需要增删注册。

启动时序（`Boot` 态内，一次）：

```
AssetHost.Init → ConfigHost.Load → LuaHost.Start()
   └─ 装载 lua 根 → LuaEnv.AddLoader → run("main.lua")
   └─ main.lua: require("lib.class") / 预置 require 搜索路径 / 注册全局错误钩子
Game → Bridge.Install(lua, ui, account, lobby, room, data, text, settings, log)   // 注册 8 张表 + Bridge
UiHost → ScreenManager → 打开首屏 → LuaScreen: page = require(script):new(self.ui)
```

---

## 3. API 面（8 张表；旧页面的 `Bridge.meta` 拆成 `account` / `lobby`，见 §9）

命名沿用旧工程的 `Bridge` 与 `self.ui`，**旧页面可以直接跑**，这是刻意的（降低迁移风险，见 §9）。

### 3.1 `Bridge.ui` — 页面导航（唯一写者：C# `ScreenManager`）

```lua
Bridge.ui.Show(page, data)        -- 入栈并显示（page = "ui.Page_Lobby"）
Bridge.ui.Replace(page, data)     -- 替换栈顶
Bridge.ui.Back()                  -- 返回上一页（栈空 = 无操作）
Bridge.ui.Close(page)             -- 关指定页
Bridge.ui.ShowToast(text)         -- 短提示（自动消失）
Bridge.ui.ShowConfirm(text, onOk) -- 确认框；onOk 必需
Bridge.ui.IsOpen(page) -> bool
```

### 3.2 `Bridge.account` / `Bridge.lobby` — 账号与大厅（转发 `AccountApi` / `LobbyApi`）

```lua
-- 账号与票据
Bridge.account.IsConfigured() -> bool
Bridge.account.AccountId() -> string
Bridge.account.Login(account, password, cb)  -- cb(ok, errKey)
Bridge.account.Logout()
Bridge.account.Records(cb)                   -- cb(ok, rows) 战绩列表

-- 房间列表与进出
Bridge.lobby.ListRooms(cb)                   -- cb(ok, rows)  rows = {{roomId, players, expectedPlayers, mapName}}
Bridge.lobby.CreateRoom(mapId, players, cb)  -- cb(ok, roomId)
Bridge.lobby.QuickEnter(cb)                  -- cb(ok, roomId)
Bridge.lobby.EnterRoom(roomId, cb)           -- cb(ok, roomId)
```

### 3.3 `Bridge.room` — 本房事实（只读快照，不持有状态）

```lua
Bridge.room.RoomId() -> string
Bridge.room.PlayerId() -> number
Bridge.room.IsOwner() -> bool
Bridge.room.Seats() -> {{playerId, name, ready}, ...}
Bridge.room.StartMatch() -> bool       -- 房主专用；失败返回 false
Bridge.room.Leave()
```

### 3.4 `Bridge.data` — 表投影（只读，来自 `cfg.Tables`）

```lua
Bridge.data.ListMaps() -> {{Id, Name}}          -- 地图表全行
Bridge.data.WeaponName(id) -> string
Bridge.data.EntityName(id) -> string
Bridge.data.MatchRules() -> {respawnSeconds, killLimit, timeLimit}
```
**规则**：每次调用返回**新表**（避免 Lua 侧改到 C# 缓存），但内部拼接字符串要缓存（§8）。

### 3.5 `Bridge.text` — 文案表

```lua
Bridge.text.Get(key) -> string
Bridge.text.Format(key, arg0, arg1) -> string   -- 最多 2 参（旧页面实际用量）
```
缺键：返回 `key` 本身 + 日志一次（不抛错，不白屏）。

### 3.6 `Bridge.settings` / `Bridge.log`

```lua
Bridge.settings.Get(key) -> number|string|bool   -- masterVolume / sensitivity / language
Bridge.settings.Set(key, value)
Bridge.log.Info(msg) / Warn(msg) / Error(msg)
```

### 3.7 `self.ui`（页面代理，22 个方法，全部保留）

```lua
SetText(id, value)                 SetTextKey(id, key)         SetTextKeyArgs(id, key, a0, a1)
SetVisible(id, bool)               SetInteractable(id, bool)
OnButton(id, fn)                   Flash(id)                   Pulse(id)
SetToggle(id, bool)                OnToggleChanged(id, fn)
SetSlider(id, v)                   OnSliderChanged(id, fn)
SetDropdownOptions(id, list)       SetDropdown(id, index)      GetDropdown(id)
OnDropdownChanged(id, fn)
SetTab(id, index)                  OnTabChanged(id, fn)
SetList(id, rows)                  OnListClick(id, fn)
ShowToast(text)                    ShowConfirm(text, onOk)
```

`id` = **预制体子节点名**（唯一命名空间，与旧约定一致）。缺件行为：日志一次（含页面名 + id），调用变 no-op，页面继续活着（**fail-soft**；对照 01 §10 页面级降级）。

---

## 4. 页面契约

```lua
local M = class("Page_Lobby")

function M:ctor()            end   -- 只做字段初始化，不碰控件（控件在 OnShow 才可用）
function M:OnShow(data)      end   -- 每次展示：绑定控件、首查、重置节拍
function M:OnReveal()        end   -- 覆盖页关闭回到本页（不重跑 OnShow）
function M:OnUpdate(dt)      end   -- 仅在可见且前台时派发；默认 10 Hz 节拍由 C# 控制
function M:OnHide()          end   -- 关页：停轮询、清在途回调标记
```

| 时序 | 调用 |
|---|---|
| 打开 | `new` → `OnShow(data)` → 每帧 `OnUpdate(dt)`（节拍内） |
| 其上再开一页 | `OnPauseImplicit`（不新增方法，仅停止 `OnUpdate`） |
| 覆盖页关闭 | `OnReveal()` → 恢复 `OnUpdate` |
| 关页 | `OnHide()` → 引用释放（页面实例可复用，不销毁 Lua 环境） |

**错误隔离**：每个入口都用 `pcall` 包裹（`LuaCallback` 统一做）。异常处理：

1. 日志一次（页面名 + traceback + 当前方法名），**同一页面同一会话不重复刷**。
2. 该页标记 `broken`：后续 `OnUpdate` 停派发，按钮仍可点（允许用户返回），但不执行业务。
3. 不弹模态、不重启 Lua 环境（重启会让用户丢流程）。

---

## 5. 节点寻址与 `UiBinder`

```csharp
// 挂在页面预制体根上；Inspector 里手填，不生成代码、不扫命名约定
public sealed class UiBinder : MonoBehaviour
{
    [SerializeField] private Entry[] _entries;   // { string key; Component target; }
    public bool TryGet<T>(string key, out T comp) where T : Component;
}
```

| 规则 | 说明 |
|---|---|
| key 唯一 | 同一预制体内重复 → `Awake` 期报错并停用该页（编辑期还有一个菜单项做全量体检） |
| 不跨预制体 | 只查本页面板下的控件；弹窗/Toast 走 `Bridge.ui` |
| 类型无关 | `TryGet<T>` 按需强转；`Text`/`TMP_Text` 由代理内部处理 |
| 不缓存字符串 | 代理内部 `Dictionary<string, Component>`，构建一次 |

相比旧工程的 11,181 行工具链（绑定生成、契约清单、五透视、预览、工作台、校验 32 文件）：牺牲"批量校验的仪式感"，换取"零生成、零约定、改预制体不动代码"。全量体检保留为 `LiteGame.Editor` 里的一个菜单项（≤ 80 行）。

---

## 6. 跨边界数据规则

### 6.1 类型白名单

| 方向 | 允许 | 禁止 |
|---|---|---|
| Lua → C# | `nil`、`bool`、`number`、`string`、上述类型组成的表（≤ 3 层） | 函数（除回调位）、userdata、C# 对象引用、循环表 |
| C# → Lua | 同上 + `LuaPageProxy` | `LuaTable`（除注册用）、`Action` 传进 Lua 长期持有 |

### 6.2 回调

- C# → Lua 回调统一 `Action<bool, ...>` 形态（≤ 3 参），签名集中定义在 `XLuaGenConfig.cs`（Editor，**≤ 10 项**，稳定后不再动）。这是全工程唯一的"生成配置"，改它需评审。
- 回调**必须**在主线程触发；`LuaCallback` 负责 `pcall` + token 校验（页面已关 → 丢弃，不报错）。
- C# 侧发起异步操作时传入页面 token：`LobbyApi.ListRooms(pageToken, cb)`；`LuaCallback` 在页面关闭后静默丢弃。

### 6.3 对象生命周期

- Lua 不持有 Unity 对象。需要读写控件一律经 `self.ui` 的 key。
- 页面实例随页面关闭**不销毁**（池化复用），`OnHide` 必须清干净外部引用；内存基线：5 个页面常驻 ≤ 1 MB Lua 堆。
- 禁 `collectgarbage()` 手动调用。

---

## 7. 热更装载

### 7.1 Lua 根目录（三级回退）

| 优先级 | 位置 | 场景 |
|---|---|---|
| 1 | `Application.persistentDataPath/lua/<version>` | 真机热更后的候选/已激活内容 |
| 2 | `StreamingAssets/lua` | 首装内置内容（进包不压缩，YooAsset raw 收集） |
| 3 | `Assets/Scripts/Lua`（编辑器直读） | 编辑器与 CI，免打包 |

`LuaHost` 按上述顺序探测，第一个通过摘要校验的生效。

### 7.2 校验与切换

1. 读 `lua/manifest.json`（每文件 `sha256` + 总摘要）。
2. 逐文件比对；不符 → 丢弃该目录，回退下一级，日志一次（`Log.Warning`）。
3. 激活后记录 `ContentVersion`（设置页可见）。

### 7.3 整包替换

```
下载完成 → 校验通过 → LuaHost.Reload()
   ├─ 关闭所有 Lua 页面（ScreenManager.CloseAllAsync）
   ├─ LuaEnv.Dispose()（含 Lua 堆）
   ├─ 重建 LuaEnv → 重装 Bridge → run("main.lua")
   └─ 重开当前应显示的页（Lobby）→ 用户无感（≤ 1 帧黑场，由 Fade 遮）
失败 → 保留旧 LuaEnv（不 Dispose）→ 用户照常玩
```

**不做**：单页面热替换、按需重载、Lua/C# 双跑灰度。整包替换的代价是 1 次 200 ms 重载，换来"没有中间状态"。

---

## 8. 性能纪律

| 规则 | 量化 |
|---|---|
| `OnUpdate` 节拍 | 默认 10 Hz（100 ms）；需要更快的页面显式声明 `tickHz = 30`，审批为评审项 |
| 跨边界调用 | ≤ 200 次/帧（战斗内 ≤ 20）；Profile 里 `LuaEnv` 的 GC 不得出现在战斗稳态 |
| 字符串 | 不每帧拼接；列表文案只在数据指纹变化时重算（旧大厅页已验证的做法） |
| 列表 | `SetList` 整表下发；数据未变不下发（行 key 稳定，避免全表重绑） |
| 表分配 | 高频路径复用表（`_scratch` 字段），不每次 `{}` |
| Lua 堆 | 稳态 ≤ 1 MB / 5 页；§7 重载后必须回落到基线 |

**反模式（旧工程踩过）**：`OnShow` 只查一次列表导致整场定格；轮询密于服务端心跳；关页后仍写节点导致文案重挂。新工程把它们写进页面模板注释（3 行以内）。

---

## 9. 旧页面迁移兼容清单

| 旧文件 | 行数 | 处置 | 需要改动 |
|---|---|---|---|
| `Page_Login.lua` | 53 | 直接迁移 | `Bridge.meta.Login` → `Bridge.account.Login`（1 处） |
| `Page_Lobby.lua` | 177 | 直接迁移 | `Bridge.meta.*` → `Bridge.lobby.*`（4 处：ListRooms/CreateRoom/QuickEnter/EnterRoom）；`Bridge.data.ListMaps` / `SetList` / `OnListClick` 全保留 |
| `Page_Room.lua` | 104 | 直接迁移 | 无（`Bridge.room.*` 同名） |
| `Page_Result.lua` | 64 | 直接迁移 | 无 |
| `Page_Settings.lua` | 260 | 直接迁移 | `Bridge.settings` 的 key 名对齐 `SettingsService` 常量 |
| `Page_Records.lua` | 53 | 直接迁移 | `Bridge.meta.Records` → `Bridge.account.Records`（1 处） |
| `Page_ButtonDemo.lua` / `UITestPage.lua` | 28 / 20 | 废弃 | — |
| `lib/class.lua` | 18 | 直接迁移 | 无 |
| `Cfg/*.lua` | 439 | 生成物 | Luban 输出到 `Scripts/Lua/cfg` |
| `LuaPanda.lua` | 3,333 | 不进包 | 需要调试时从 `ThirdParty` 临时接入 |

结论：6 个业务页面 ≈ 700 行 Lua **基本零改动**迁移；工作量集中在"新门面补齐旧门面已有的 3 个方法"（`Records`、`StartMatch`、`Seats` 的字段名对齐）。

---

## 10. 关键决策与代价

| 决策 | 代价 | 为什么值得 |
|---|---|---|
| 保留 `Bridge` 命名与旧 API 面 | 继承了旧命名的历史包袱（`Bridge.ui.Show` 而非 `Ui.Show`） | 700 行页面零改动迁移，风险最低 |
| 手写 `XLuaGenConfig`（≤10 项）而不是生成适配层 | 回调签名改动需手工同步 | 避免"生成器补桥的洞"重演 |
| 节点 key = 子节点名 + 显式 `UiBinder` | 重命名子节点会断绑定（编辑期体检兜住） | 删掉 11,181 行工具链，且改动只碰预制体 |
| 整包替换而非单页热更 | 1 次 200 ms 重载 | 无中间状态，失败可回退 |
| 页面实例池化复用 | `OnHide` 必须清干净引用，否则状态残留 | 避免每次开页重建 Lua 对象 |

---

## 11. 与 HybridCLR 的关系（两种热更的分工）

HybridCLR 是"将来可选"（01 §15 只锁边界，本期不装）。两者不是替代关系，是分工：

| 维度 | Lua（本文） | HybridCLR（C# 热更） |
|---|---|---|
| 热更对象 | 页面逻辑、文案、表格数据 | C# 业务逻辑（性能路径、算法修补） |
| 生效方式 | 整包替换 Lua 环境（≈200 ms，免重启进程） | 装载 dll + 重进流程 |
| 上线成本 | 低（改文本重打包） | 中（编译 + 签名 + 装载 + 调试符号） |
| 跨边界契约 | `self.ui` 22 方法与 `Bridge` 8 张表 | `IGame` 与 Core 的 3 个端口接口 |

**三条规则**：

1. **同一功能不并存**：一个页面的逻辑件要么是 Lua 页、要么是 C# 页（01 §7.1 二选一），禁止两侧各写一份——这正是旧工程 3 张 Lua 注册表 + 桥接 1,300 行的由来。
2. **HybridCLR 落地不撤 Lua**：能"改文本就生效"的场景（文案、数值、列表投影、流程分支）留在 Lua，比编译 dll 便宜一个数量级；C# 热更只接"Lua 做不了的"。
3. **契约与逻辑侧无关**：`Bridge` 的 API 面是给"逻辑侧"的合同。将来把某页从 Lua 迁到 C#（例如设置页涉及大量计算），页面预制体、`UiBinder`、`ScreenId` 全不动，只换逻辑件——这是本文把"页面 = 预制体 + 逻辑件"拆开的原因。

**新增热更层需登记**：任何第三套热更手段（如资源内嵌 lua 之外的解释器）必须先写进 01 §15 并说明为何现有两层不可替代，否则不做。

