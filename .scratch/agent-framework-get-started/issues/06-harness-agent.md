# 06: 工具包智能体（AsHarnessAgent）

**What to build:** 在 `HelloAgent` 中用 `AsHarnessAgent` 从聊天客户端创建工具包智能体，配合一个会话与交互式流式循环，使其能对多步请求制定计划并跟踪待办。

**Blocked by:** 04（内存与持久性（ChatHistoryProvider））

**Status:** done

- [x] 对多步请求，agent 会给出计划/待办并推进执行
- [x] 会话状态（计划、待办、历史）跨轮保留
- [x] 每轮回复后打印状态行（当前模式 + 待办清单），并随执行推进而变化
- [x] README 记录该步的运行方式

## 决策

### 1. 形态：步骤 1–4 之后进入交互式 REPL

步骤 1–4 保持现有脚本式演示（`dotnet run` 的自动段仍是可重复回归），随后打印一段提示并进入 `while + Console.ReadLine` 循环：空行跳过，`exit`（忽略大小写）退出，每轮用 `RunStreamingAsync(input, session)` 边生成边打印。这与教程步骤 6 的形态一致，也是本步唯一的验收方式。

每轮调用包一层 `try/catch`：模型偶尔会发出畸形工具调用（实测见下方 Comments），框架此时直接抛异常，单轮失败不该带走整个会话。

工具沿用现有的 `WeatherTools.GetWeather`，好让计划里出现真实的工具步骤（而不只是文字计划）。

### 2. 装配：`AgentFactory` 抽出 `CreateChatClient`

`AsHarnessAgent` 是 `IChatClient` 上的扩展，而当前 `AgentFactory.Create` 在一条链式调用里就把 `IChatClient` 消耗掉了。改为：

- 新增 `AgentFactory.CreateChatClient(apiKey, baseUrl, model, sessionId) → IChatClient`——端点、`x-opencode-session` 头、模型 ID 只此一处；
- 现有 `Create(...)` 改为基于它 + `AsAIAgent(agentOptions)`，对外行为不变；
- 新增 `AgentFactory.CreateHarness(...) → AIAgent`，用 `HarnessAgentOptions` 装配 harness。

理由：符合 spec「`AgentFactory` 仍是 agent 构造的唯一入口」；若在 `Program.cs` 里就地重建客户端，会话头与端点会出现第二份实现。

### 3. 状态可视化：每轮结束打印状态行

harness 的计划与待办存在会话里，但 `HarnessAgent` 没有公开属性暴露它们；经 `AIAgent.GetService<T>()` 可以拿到提供程序：

- 模式：`agent.GetService<AgentModeProvider>()` → `GetModeAsync(session)`（内置取值 `plan` / `execute`）；
- 待办：`agent.GetService<TodoProvider>()` → `GetAllTodosAsync(session)`（每项含 `Id` / `Title` / `Description` / `IsComplete`）。

在每轮回复后打印一行，例如：

```text
[mode] plan | 待办 1/3  [x] 查询阿姆斯特丹天气  [ ] 排两日行程  [ ] 汇总成行程单
```

用 ASCII 的 `[x]` / `[ ]` 做完成标记，不用 emoji（Windows 控制台编码易出豆腐块）。某个提供程序缺席时只跳过它那一段（另一段照常打印；两个都缺则整行不打印），不报错。

不做 `/todos`、`/mode` 斜杠命令：官方教程里的 `HarnessConsole` 是**示例**而非框架组件，不为它引入额外依赖。

### 4. harness 默认项的取舍

保留默认——待办跟踪、计划/执行模式、文件内存、技能发现、OpenTelemetry、工具自动审批。只显式关闭一项：

- `DisableWebSearch = true`：默认会往 `ChatOptions.Tools` 注入 `HostedWebSearchTool`，而本仓库后端是 Chat Completions，hosted 工具不可用（[ADR-0001](../../../docs/adr/0001-use-opencode-go-via-openai-compatible-chat-completions.md) 已记录该后果）。

不设置 `MaxContextWindowTokens` / `MaxOutputTokens`，因此上下文压缩默认关闭（这些开关是实验性 API，见「实现备注」）。

指令采用「英文骨架 + 中文人格」：`HarnessInstructions` 留 `null`（用框架默认的 `HarnessAgent.DefaultInstructions`），`ChatOptions.Instructions` 写一条**只属于本步**的规划导向中文指令：

> 你是一个善于规划的助手：处理多步请求时先列出计划与待办，再逐项推进，并说明每一步的依据。

步骤 1–4 的旧指令不动。「回答请保持简洁」不适用于本步——它会把计划正文压掉，让「给出计划」这一步在终端上不可见。

### 5. 包与版本

`src/HelloAgent/HelloAgent.csproj` 增加 `Microsoft.Agents.AI.Harness` **1.24.0**，与仓库其余 Agent Framework 包（`Microsoft.Agents.AI.OpenAI`、`Microsoft.Agents.AI.Workflows`）对齐。`AsIChatClient()` 已由现有传递依赖 `Microsoft.Extensions.AI.OpenAI` 提供，无需新增包。

## 验收剧本（需密钥与网络）

1. `dotnet run --project src/HelloAgent`，等步骤 1–4 跑完，进入步骤 6 的循环（提示里给出建议输入）。
2. 输入多步请求：`帮我规划一次周末去阿姆斯特丹的两日行程，并给出天气建议`。
   期望：先出现计划与待办（状态行 `plan`，待办为若干 `[ ]` 项），推进过程中调用 `GetWeather`。
3. 输入 `继续`。
   期望：待办清单仍是第 2 步那一批（**跨轮保留**），且发生实际的推进动作（继续调用工具、把计划写进记忆文件等）。勾选数与是否切到 `execute` **取决于模型**：两次实跑分别是 6/6 与 0/6，模式两次都停在 `plan`，因此这两项不作判据。
4. 输入 `exit` 退出。

无法离线验证：与 ADR-0001 的后果一致，本步没有自动化测试缝，回归靠上面的端到端运行（ADR-0002 只就地放宽了「纯逻辑能力」，未要求 agent 类步骤自动化）。

## 实现备注（已核实的坑）

以下结论来自对 `Microsoft.Agents.AI.Harness` 1.24.0 实际下载与临时探针项目的实测编译（非文档推断）。

- **实验性 API 在本项目里是错误级**：`MaxContextWindowTokens`、`MaxOutputTokens`、`DisableCompaction`、`CompactionStrategy`、`FileMemoryStore`、`FileSystemAgentFileStore`、`FileAccessStore`、`FileAccessProviderOptions`、`LoopEvaluators` / `LoopAgentOptions`、`BackgroundAgents*` 都带 `MAAI001`，直接编译**失败**。要用就得 `#pragma warning disable MAAI001`——本步一律不用。
- **实测 0 警告可用**：`AsIChatClient()`、`IChatClient.AsHarnessAgent(HarnessAgentOptions)`、`CreateSessionAsync()`、`RunStreamingAsync(input, session)`、`GetService<TodoProvider>()` / `GetService<AgentModeProvider>()`、`GetAllTodosAsync` / `GetModeAsync`、`HarnessAgent.DefaultInstructions`，以及选项 `Name` / `Description` / `HarnessInstructions` / `ChatOptions` / `ChatHistoryProvider` / `AIContextProviders` / `MaximumIterationsPerRequest` / `DisableWebSearch` / `DisableFileMemory` / `DisableAgentSkillsProvider` / `DisableTodoProvider` / `DisableAgentModeProvider` / `DisableOpenTelemetry` / `OpenTelemetrySourceName` / `DisableApprovalNotRequiredFunctionBypassing` / `DisableApprovalResponseBinding`。
- **注意 1.24.0 与 main 分支的实验性标记不同**：`DisableCompaction` 在 main 的 PublicAPI 清单里不带标记，但 1.24.0 实测报 `MAAI001`。别照 main 的文档判断。
- **文件内存会写盘**：默认根目录是 `{cwd}/agent-file-memory/{时间戳}_{guid}/`。`dotnet run --project src/HelloAgent` 的 cwd 是仓库根，因此需在根 `.gitignore` 追加 `agent-file-memory/`（不加前导斜杠，任意深度都忽略）。每次运行新建一个目录，属预期行为。
- **技能发现**：默认从 cwd 做文件式发现；仓库根没有技能文件 → 空操作。日后若在仓库根放技能文件，行为会变，届时重新评估。
- **工具审批不会阻塞控制台**：默认审批只对 `ApprovalRequiredAIFunction` 工具生效，且默认开启「不需要审批的工具直接放行」；现有 `WeatherTools` 与 harness 自带工具都不需审批，因此不写审批处理器也能跑完。
- **OpenTelemetry** 默认开启但未配 exporter → 无输出，无需处理。
- **历史提供程序**：harness 自带「每次服务调用即持久化」的历史（默认 `InMemoryChatHistoryProvider`）；本步**不**接步骤 4 的 `CustomChatHistoryProvider`（步骤 4 已演示该缝，需要时可经 `HarnessAgentOptions.ChatHistoryProvider` 传入）。
- **`MaximumIterationsPerRequest` 不设**：用 `FunctionInvokingChatClient` 的默认值，少一个旋钮。

## 风险

- 计划/待办/模式能否真正「跑起来」取决于模型的 tool calling 质量。**实测已确认**：默认的 `mimo-v2.6-flash` 在 harness 的工具面（待办/模式/文件内存/技能/天气）下会发出畸形工具调用（缺必填参数），框架抛 `ArgumentException`；`deepseek-v4.1-flash` 下不再出现该错误（行为差异见 Comments）。用户侧只需 `LLM_CHAT_MODEL` 换模型，代码无需改动。
- ~~`GetService<TodoProvider>()` 需第一次实跑确认~~ **已解除**：1.24.0 运行时确实注册了这两个提供程序，状态行实测打印出模式与 6 项待办；降级分支仍保留。
- 请求偶发挂起（同一二进制、同一端点，一次跑在首次调用处卡住 >5 分钟无响应，端点探测正常）。属上游抖动，重跑即可；本示例没有为模型调用设置超时。

## Comments

- 2026-10-15 设计定稿（grill-with-docs 会话）。已敲定的取舍：形态用交互式 REPL（步骤 1–4 保持在前的自动段）、装配在 `AgentFactory` 抽出 `CreateChatClient`、每轮打印状态行、默认项只关 web 搜索、包版本 1.24.0、不为本步引入实验性 API。参考教程：<https://learn.microsoft.com/zh-cn/agent-framework/get-started/harness?pivots=programming-language-csharp>。
- 本 ticket 只做设计定稿，代码实现另行发起；`CONTEXT.md` 已随本次新增「工具包（Harness）」「待办（Todo）」「模式（Mode）」三个词。

### 实现完成（2026-10-15）

改动：`src/HelloAgent/HelloAgent.csproj`（+`Microsoft.Agents.AI.Harness` 1.24.0）、`AgentFactory.cs`（抽出 `CreateChatClient`，新增 `CreateHarness`）、`Program.cs`（步骤 6 交互循环 + 状态行 + 单轮容错）、`.gitignore`（`agent-file-memory/`）、README（步骤 6 运行方式与新章节）。

验证：

- `dotnet build src/HelloAgent`：0 警告 0 错误（未引入任何实验性 API）。
- `dotnet test tests/FirstWorkflow.Tests`：1/1 通过（步骤 5 回归未受影响）。
- 验收剧本（stdin 走管道喂入，需密钥与网络）：`LLM_CHAT_MODEL=deepseek-v4.1-flash` 下**验收清单四项全过**，实跑两次：
  - 第一次：第一轮「帮我规划一次周末去阿姆斯特丹的两日行程，并给出天气建议」→ 调用 `GetWeather`、建立 6 项待办、打印 `[mode] plan | 待办 0/6 …`；第二轮「继续」→ 沿用同一批待办并全部勾选完成（`待办 6/6 [x] …`）。
  - 第二次（容错改动之后）：同样两轮，待办始终是那 6 项（**跨轮保留稳定复现**），但第二轮选择「先请求批准再进 execute」，勾选数停在 0/6；实际推进动作照常发生（查天气、把行程写进记忆文件）。
  - 结论：「跨轮保留」是本步的稳定行为；「待办被勾选推进 / 模式切 `execute`」属模型行为，两次表现不同，不作判据（验收剧本第 3 条已据此改写）。
  - 文件内存两次都落盘：`agent-file-memory/{时间戳}_{guid}/amsterdam-weekend-plan.md`（含 `_description.md` 与 `memories.md`），且被 `.gitignore` 忽略；两次均以 `exit` 正常退出。
- 默认模型 `mimo-v2.6-flash` 的实跑（同一剧本）：第一轮计划与待办正常；第二轮模型发出缺 `items` 参数的畸形工具调用，框架抛 `ArgumentException: The arguments dictionary is missing a value for the required parameter 'items'`，未捕获时整个 REPL 被打挂。据此给每轮加了 `try/catch`（`[错误] 本轮失败：…` 只包住本轮回复，状态行另有一个 `catch`，见 code review 后的修正），并把「换模型」写进 README。这是本步唯一一处超出原设计的改动。
- 附带发现：`dotnet run` 之外的管道输入需注意编码——重定向时 .NET 按控制台代码页（zh-CN 为 GBK）读写，用 UTF-8 喂中文会变乱码；GBK 输入文件即正常。手动交互运行不受影响。
- code review（两轴）后的修正：状态读取异常不再被误报为「本轮失败」（拆成两个 catch）；README 与 `AgentFactory` 注释里指代框架处写明「Agent Framework」，避免与术语表给 Harness 列的 `_Avoid_`: 框架 混淆；`CONTEXT.md` 的 Mode 条改用 `plan` / `execute`，不再用与 Run 的 `_Avoid_`: 执行 相同的措辞。未采纳的两条判断性意见：`(apiKey, baseUrl, model, sessionId)` 数据簇（ticket 决策 2 已定签名）与 `Create`/`CreateHarness` 的轻微重复（两者选项类型不同，抽共享助手反而更绕）。
