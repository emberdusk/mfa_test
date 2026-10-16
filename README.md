# mfa_test

一个最小可运行的示例：用 C# 与 [Microsoft Agent Framework](https://learn.microsoft.com/agent-framework/) 创建 AI agent，模型后端接入 **OpenCode Go**（OpenAI 兼容接口）。

对应教程：

- [步骤 1：你的第一个智能体](https://learn.microsoft.com/zh-cn/agent-framework/get-started/your-first-agent?pivots=programming-language-csharp)（C# 分支）
- [步骤 2：添加工具](https://learn.microsoft.com/zh-cn/agent-framework/get-started/add-tools?pivots=programming-language-csharp)（C# 分支）
- [步骤 5：工作流](https://learn.microsoft.com/zh-cn/agent-framework/get-started/workflows?pivots=programming-language-csharp)（C# 分支）
- [步骤 6：智能体工具包](https://learn.microsoft.com/zh-cn/agent-framework/get-started/harness?pivots=programming-language-csharp)（C# 分支）
- [步骤 7：托管你的智能体](https://learn.microsoft.com/zh-cn/agent-framework/get-started/hosting?pivots=programming-language-csharp)（C# 分支）

> 与教程的差异：教程假设使用 Microsoft Foundry（`AIProjectClient` + `DefaultAzureCredential`），本示例改用 OpenCode Go 的 OpenAI 兼容端点，只靠一个 API Key 即可运行。见 [`docs/adr/0001-use-opencode-go-via-openai-compatible-chat-completions.md`](docs/adr/0001-use-opencode-go-via-openai-compatible-chat-completions.md)。
>
> 步骤 7 另有两处差异：教程片段里的 `builder.Services.AddA2AServer()`（无参）与 `app.MapA2AServer()` 在 1.24 源码中**不存在**，本示例改用真实 API `hostedAgent.AddA2AServer()` + `app.MapA2AJsonRpc(...)`（教程的 C# step-7 完整样例目录也已迁移）；托管包仅有 preview 版，本示例钉住与仓库同族的 `1.24.0-preview.261006.1`。协议选择见 [ADR-0003](docs/adr/0003-a2a-protocol-and-pinned-hosting-preview.md)。

## 前置条件

- [.NET SDK 10](https://dotnet.microsoft.com/download)（本项目目标框架为 `net10.0`）
- 一个 OpenCode Go API Key（在 [OpenCode Console](https://opencode.ai/auth) 订阅后获取）

## 配置

全部通过环境变量提供，API Key 不会写入仓库。

PowerShell：

```powershell
$env:LLM_API_KEY = "sk-..."
```

Bash：

```bash
export LLM_API_KEY="sk-..."
```

可选（不设置则使用下列默认值）：

| 环境变量 | 默认值 | 说明 |
| --- | --- | --- |
| `LLM_BASE_URL` | `https://opencode.ai/zen/go/v1` | OpenAI 兼容端点 |
| `LLM_CHAT_MODEL` | `mimo-v2.6-flash` | 模型 **ID**（必须小写，如 `mimo-v2.6-flash`、`deepseek-v4.1-flash`；不能用显示名 `MiMo-V2.6-Flash`，否则报 `Model is unavailable`） |
| `LLM_SESSION_ID` | 每次运行随机生成 | 作为 `x-opencode-session` 发送；固定它可复用提示缓存 |
| `A2A_AGENT_URL` | `http://localhost:5087` | 步骤 7：agent card 上公布的宿主地址（改端口时同步修改） |

## 运行

```powershell
dotnet run --project src/HelloAgent
```

预期输出（按教程步骤）：

1. **步骤 1**：回答「法国最大的城市是哪里？」，并以流式方式回答「用一句话讲个有趣的冷知识。」
2. **步骤 2**：回答「阿姆斯特丹的天气怎么样？」—— agent 会自动调用 `GetWeather` 工具，再据其返回作答
3. **步骤 3**：创建会话后先自我介绍（Alice / 徒步），再追问「你记得我什么？」，agent 能答出前文
4. **步骤 4**：用**显式配置**的 `CustomChatHistoryProvider` 再演示一次会话记忆，输出中可见每轮「载入 / 保存」历史的日志（见下文「内存与持久性」）

### 步骤 5：工作流（离线，独立项目）

工作流项目独立于 agent 与模型后端，**不需要 API Key、不联网**：

```powershell
dotnet run --project src/FirstWorkflow
```

预期输出：

```text
UppercaseExecutor: HELLO, WORLD!
ReverseTextExecutor: !DLROW ,OLLEH
```

回归断言（同样离线）：

```powershell
dotnet test
```

### 步骤 6：工具包智能体（交互式）

步骤 4 之后，程序并不退出，而是进入**交互式循环**（仍是同一条 `dotnet run --project src/HelloAgent`）：

```text
=== 步骤 6：工具包智能体（AsHarnessAgent） ===
输入一个多步请求试试，例如：帮我规划一次周末去阿姆斯特丹的两日行程，并给出天气建议
（空行跳过，输入 exit 退出；会话内待办与会话状态跨轮保留。）
>
```

输入一个多步请求，agent 会先列出计划与待办，再逐步推进（过程中会调用 `GetWeather`，并可能把计划写进记忆文件）。待办勾选数与是否切到 `execute` 模式取决于模型。每轮回复后打印一行状态：

```text
[mode] plan | 待办 2/4  [x] 查询阿姆斯特丹天气  [ ] 排两日行程  [ ] 确定交通  [ ] 汇总成行程单
```

待办与模式都随会话保留，因此第二轮可以只说「继续」。空行跳过，输入 `exit` 退出（从管道读取标准输入时，读到结尾同样退出）。

### 步骤 7：托管你的智能体（ASP.NET Core，独立项目）

`src/HostedAgent/` 把 agent 注册进 ASP.NET Core 的依赖注入容器，再经 **A2A 协议**的三种端点对外暴露。启动宿主（固定监听 `http://localhost:5087`，需 `LLM_API_KEY`）：

```powershell
dotnet run --project src/HostedAgent
```

另开一个终端，依次验证三个端点：

```bash
# 1. well-known agent card（服务发现名片：名称、技能、协议绑定地址）
curl http://localhost:5087/.well-known/agent-card.json

# 2. HTTP/JSON 绑定（POST /message:send）
curl -X POST http://localhost:5087/message:send \
  -H "Content-Type: application/json" \
  -d '{"message":{"kind":"message","messageId":"1","role":"ROLE_USER","parts":[{"kind":"text","text":"法国最大的城市是哪里？"}]}}'

# 3. JSON-RPC 绑定（POST /，A2A 方法名 SendMessage）
curl -X POST http://localhost:5087/ \
  -H "Content-Type: application/json" \
  -d '{"id":"1","jsonrpc":"2.0","method":"SendMessage","params":{"message":{"kind":"message","messageId":"2","role":"ROLE_USER","parts":[{"kind":"text","text":"阿姆斯特丹的天气怎么样？"}]}}}'
```

预期：三条均返回 HTTP 200；名片里 `name` 为 `hosted-agent`、含一条 `weather` 技能；后两条的 `parts` 中带 agent 的回答——第 3 条会经宿主调用 `GetWeather` 工具（`.WithAITool(...)` 挂载，与步骤 2 同一个工具），回答为「…多云，最高气温 15°C」一类的假数据。

请求格式注意两点（实测）：A2A 方法名是 **`SendMessage`**（`message/send` 会报 `-32601`），`role` 枚举是 **`ROLE_USER`**（小写 `user` 反序列化失败）。含中文的请求体若经 Windows 控制台发送可能被转成 GBK 而报 400（`invalid UTF-8`）——把 JSON 存成 UTF-8 文件后用 `curl --data-binary @文件` 发送即可。

会话相关：本步是**单轮**请求/响应（跨请求记忆见步骤 3/4）；宿主默认不持久化会话（`NoopAgentSessionStore`）。同一宿主进程内 `x-opencode-session` 沿用启动时的值（`LLM_SESSION_ID` 未设置则随机生成）。

## 工具

在 `Tools/WeatherTools.cs` 中，用 `[Description]` 标注的方法即一个工具；它通过 `AIFunctionFactory.Create(...)` 注册到 agent 的 `tools:` 列表：

```csharp
tools: [AIFunctionFactory.Create(WeatherTools.GetWeather)]
```

新增工具 = 新建一个 `XxxTools` 类，再往该列表追加一项。agent 会在需要时自动调用；所选模型需支持 tool calling（默认的 `mimo-v2.6-flash` 支持）。

## 内存与持久性

步骤 3 依赖框架默认的历史保存（Chat Completions 下为 `InMemoryChatHistoryProvider`）；步骤 4 则**显式**把一个聊天历史提供程序配置到 agent 上，用以理解历史如何被存取。

`src/HelloAgent/ChatHistory/CustomChatHistoryProvider.cs` 实现了一个自己的 `ChatHistoryProvider`，它把每个会话的历史存在提供程序自己的字典里，并在两个钩子处打印日志：

- `ProvideChatHistoryAsync`：每次调用**前**取出该会话的历史，拼进本轮请求；
- `StoreChatHistoryAsync`：调用**后**把本轮的请求与回复消息追加进存储。

会话标识经 `ProviderSessionState<T>` 写进 `AgentSession.StateBag`，因此可随会话序列化一起保存/恢复。要获得真正的持久化，只需把字典替换为文件或数据库——存储契约不变（这正是"内存与持久性"的接缝所在）。

装配仍集中在 `AgentFactory`：通过可选参数 `chatHistoryProvider` 传给 `ChatClientAgentOptions.ChatHistoryProvider`；不传时保持框架默认行为。

```csharp
var memoryAgent = AgentFactory.Create(
    apiKey, baseUrl, model, sessionId,
    chatHistoryProvider: new CustomChatHistoryProvider());
```

## 工作流

步骤 1–4 都是「**一个 agent**」的形态；工作流则是**图式编排**：把多个执行器（Executor）用边（Edge）连起来，数据沿边逐级传递。

`src/FirstWorkflow/` 中，`WorkflowFactory.Build()` 用 `WorkflowBuilder` 串起两个执行器并声明输出，程序入口只负责运行与打印事件：

- **UppercaseExecutor**：由 `Func<string,string>` 经 `BindAsExecutor` 绑定——一种定义执行器的方式；
- **ReverseTextExecutor**：`Executor<string,string>` 子类，覆写处理方法——另一种方式。

运行经 `InProcessExecution` 完成，遍历 `Run.NewEvents` 打印每个执行器的**完成事件**（`ExecutorCompletedEvent`，含执行器 ID 与其结果数据）。

构图之所以被抽到 `WorkflowFactory`，是为了给自动化测试留一个纯逻辑缝：`tests/FirstWorkflow.Tests/` 用它构建工作流、断言「`"Hello, World!"` → `"!DLROW ,OLLEH"`」，无需模型与网络。该决策见 [ADR-0002](docs/adr/0002-workflow-test-seam.md)。术语见根目录 [CONTEXT.md](CONTEXT.md)。

## 工具包智能体（步骤 6）

`AsHarnessAgent` 是「即拿即用」的形态：给一个聊天客户端，Agent Framework 就把多步任务所需的能力一次性装上——**计划/执行模式**、**待办跟踪**、**文件内存**、工具审批与 OpenTelemetry。本示例除一处外全部沿用默认：

- 显式关闭 **web 搜索**：默认会往工具列表注入 `HostedWebSearchTool`，而本仓库后端是 Chat Completions，hosted 工具不可用（见 [ADR-0001](docs/adr/0001-use-opencode-go-via-openai-compatible-chat-completions.md)）。
- 不设置上下文 token 上限，因此**上下文压缩**默认关闭——相关开关（`MaxContextWindowTokens`、`MaxOutputTokens`、`DisableCompaction`、`FileMemoryStore` 等）带 `MAAI001` 诊断，在本项目里是**错误级**，碰了直接编译失败。

**文件内存**默认写到 `{cwd}/agent-file-memory/{时间戳}_{guid}/`；`dotnet run --project src/HelloAgent` 的 cwd 是仓库根，因此该目录已加入 `.gitignore`。每次运行新建一个，属预期行为。

**状态从哪来**：`HarnessAgent` 不暴露计划与待办，但可经 `agent.GetService<TodoProvider>()` / `GetService<AgentModeProvider>()` 取到提供程序，再用 `GetAllTodosAsync(session)` / `GetModeAsync(session)` 读取——`Program.cs` 的状态行就由此而来；提供程序缺席时该行静默跳过。

harness 的工具面比前几步大得多（待办、模式、文件内存、技能…），对模型的工具调用质量更敏感：默认的 `mimo-v2.6-flash` 可能发出畸形工具调用，那一轮会打印 `[错误] 本轮失败：…` 后**继续**（循环不会退出）。遇到这种情况换更强的模型即可，例如 `LLM_CHAT_MODEL=deepseek-v4.1-flash`（实测全程正常，见 `.scratch/agent-framework-get-started/issues/06-harness-agent.md` 的 Comments）。

装配仍在 `AgentFactory`：`CreateChatClient` 是「同一后端、两种 agent」的接缝，步骤 1–4 的 `Create` 与步骤 6 的 `CreateHarness` 都基于它，会话头与端点只有一处。术语见根目录 [CONTEXT.md](CONTEXT.md)。

## 项目结构

```
MfaTest.slnx                          解决方案
src/HelloAgent/Program.cs             编排：读配置 → 建 agent → 调用（步骤 1–4 脚本 + 步骤 6 交互循环）
src/HelloAgent/AgentFactory.cs        装配：客户端 / 会话头 / agent（含工具包）/ 历史提供程序
src/HelloAgent/Tools/WeatherTools.cs  工具
src/HelloAgent/ChatHistory/CustomChatHistoryProvider.cs  聊天历史提供程序（步骤 4）
src/FirstWorkflow/Program.cs          工作流编排：取工作流 → 运行 → 打印事件（步骤 5）
src/FirstWorkflow/WorkflowFactory.cs  装配工作流：执行器与边（步骤 5 的测试缝）
src/FirstWorkflow/Executors/ReverseTextExecutor.cs       反转执行器（步骤 5）
src/HostedAgent/Program.cs           托管入口：注册聊天客户端 → AddAIAgent → 三种 A2A 端点（步骤 7）
src/HostedAgent/Properties/launchSettings.json          固定端口 5087
tests/FirstWorkflow.Tests/WorkflowTests.cs               工作流端到端测试（步骤 5）
CONTEXT.md                            领域术语表
docs/adr/                             架构决策记录
```

## 实现说明

- 使用 **Chat Completions** 客户端（`OpenAI.Chat`）。
- OpenCode Go 要求每个请求携带 `x-opencode-session` 头，否则返回 `400 MissingSessionID`。该头在 `AgentFactory` 中经 `HttpClient` 默认请求头注入。
- 工具由 Agent Framework 自动调用；`Program.cs` 的调用形态（`RunAsync` / `RunStreamingAsync`）与教程一致，后端差异被隔离在 `AgentFactory` 内。
- 多轮对话通过 `AgentSession` 实现：`CreateSessionAsync()` 建会话，之后多次 `RunAsync(..., session)` 复用同一会话，历史（`InMemoryChatHistoryProvider`）随会话保留。
- 内存与持久性（步骤 4）：历史提供程序经 `ChatClientAgentOptions.ChatHistoryProvider` 显式配置；自定义提供程序把历史存在自己的存储中，并在 `ProvideChatHistoryAsync` / `StoreChatHistoryAsync` 两个钩子处读写。替换存储后端即可持久化。
- 工作流（步骤 5）：**纯离线、不接模型**；两个执行器由一条边顺序连接，构图集中在 `WorkflowFactory`，运行经 `InProcessExecution`，事件经 `ExecutorCompletedEvent` 打印。`tests/FirstWorkflow.Tests` 对「输入 → 输出」做端到端断言（见 [ADR-0002](docs/adr/0002-workflow-test-seam.md)）。
- 工具包（步骤 6）：`CreateHarness` 经 `AsHarnessAgent` 一次装配出计划/执行模式、待办跟踪、文件内存与工具审批，仅显式关闭 web 搜索（后端限制）；交互循环全程复用同一个 `AgentSession`，计划与待办因此跨轮保留。装配接缝是 `CreateChatClient`（返回 `IChatClient`），harness 与 `AsAIAgent` 共用它。
- 托管（步骤 7）：`AgentFactory` 自本步起定位为**聊天客户端的唯一装配入口**——`CreateChatClient` 的结果以 keyed 单例（`chat-model`）注册进 DI，`AddAIAgent` 用教程的链式形态在容器里构造 agent，指令取共享常量 `AgentFactory.DefaultInstructions`，工具经 `.WithAITool(...)` 挂载。`HostedAgent` 以项目引用复用 `HelloAgent`，会话头与模型 ID 不出现第二份实现。A2A 暴露三个端点（JSON-RPC、HTTP/JSON、well-known 名片），协议选择与版本钉住见 [ADR-0003](docs/adr/0003-a2a-protocol-and-pinned-hosting-preview.md)，术语见 [CONTEXT.md](CONTEXT.md) 的「托管」「协议端点」。
