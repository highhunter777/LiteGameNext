# Tools

机检与工程脚本。除 `build_ui_prefabs.cs` / `click_back.cs` / `play_resume.cs` 外都用宿主 Python 直接跑。

| 文件 | 用途 | 何时跑 |
|---|---|---|
| `verify.py` | 机检（预算/依赖方向/结构/命名/卫生/注释/文档），`--build` 连 dotnet、`--pipeline` 查 Unity 通道 | 每次收尾 |
| `restore_pipeline.py` | 还原 `Packages/com.unity.pipeline`（不入库） | 新克隆/换机 |
| `restore_thirdparty.py` | 从旧仓复制 UniTask / xLua / `Plugins/x86_64`（不入库） | 新克隆/换机 |
| `sync_shared.py` | 同步旧仓 Lua 表（`--config` 连配置表 C#），写 `sync-manifest.json`；`--check` 只比对 | 旧仓重新生成后 |
| `build_lobby_prefab.cs` | 增量建 `Page_Lobby.prefab` 并追加 UIRoot 目录条目（不改 UIRoot GUID） | 改 Lobby 页骨架时 |
| `build_ui_prefabs.cs` | 生成 UIRoot / LoadingScreen / ErrorScreen 预制体并接 Boot 场景；会先删后建，属资产可复现来源 | 改 UI 骨架时，经 Unity 内 `run_script` 执行 |
| `naming-exceptions.json` | 命名例外登记（`verify.py` 读取） | 需要命名偏离时 |

**开源参考仓库**：`ThirdParty/`（不入库；Unity 不导入，因为它们不在 `Assets/` 下）。开工时断网，用本机代理拉：

```powershell
git -c http.proxy=http://127.0.0.1:7890 clone --depth 1 https://github.com/Haruma-K/UnityScreenNavigator ThirdParty/UnityScreenNavigator
git -c http.proxy=http://127.0.0.1:7890 clone --depth 1 https://github.com/hadashiA/VContainer ThirdParty/VContainer
git -c http.proxy=http://127.0.0.1:7890 clone --depth 1 https://github.com/mattak/Unidux ThirdParty/Unidux
git -c http.proxy=http://127.0.0.1:7890 clone --depth 1 https://github.com/Cysharp/MessagePipe ThirdParty/MessagePipe
```

两个 Play 验证脚手架（编辑器内 `run_script` 调用，不参与构建）：

- `play_resume.cs`：编辑器失焦时置 `runInBackground` 并排一帧，避免 Play 循环冻结。
- `click_back.cs`：用 UGUI 事件管线派发 Back 按钮的 pointerClick（legacy input 无注入 API 时的替代）。
