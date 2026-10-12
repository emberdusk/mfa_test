# mfa_test

一个最小可运行的示例：用 C# 与 [Microsoft Agent Framework](https://learn.microsoft.com/agent-framework/) 创建 AI agent，模型后端接入 **OpenCode Go**（OpenAI 兼容接口）。

对应教程：

- [步骤 1：你的第一个智能体](https://learn.microsoft.com/zh-cn/agent-framework/get-started/your-first-agent?pivots=programming-language-csharp)（C# 分支）
- [步骤 2：添加工具](https://learn.microsoft.com/zh-cn/agent-framework/get-started/add-tools?pivots=programming-language-csharp)（C# 分支）
- [步骤 5：工作流](https://learn.microsoft.com/zh-cn/agent-framework/get-started/workflows?pivots=programming-language-csharp)（C# 分支）

> 与教程的差异：教程假设使用 Microsoft Foundry（`AIProjectClient` + `DefaultAzureCredential`），本示例改用 OpenCode Go 的 OpenAI 兼容端点，只靠一个 API Key 即可运行。见 [`docs/adr/0001-use-opencode-go-via-openai-compatible-chat-completions.md`](docs/adr/0001-use-opencode-go-via-openai-compatible-chat-completions.md)。

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

## 项目结构

```
MfaTest.slnx                          解决方案
src/HelloAgent/Program.cs             编排：读配置 → 建 agent → 调用
src/HelloAgent/AgentFactory.cs        装配：客户端 / 会话头 / agent / 历史提供程序
src/HelloAgent/Tools/WeatherTools.cs  工具
src/HelloAgent/ChatHistory/CustomChatHistoryProvider.cs  聊天历史提供程序（步骤 4）
src/FirstWorkflow/Program.cs          工作流编排：取工作流 → 运行 → 打印事件（步骤 5）
src/FirstWorkflow/WorkflowFactory.cs  装配工作流：执行器与边（步骤 5 的测试缝）
src/FirstWorkflow/Executors/ReverseTextExecutor.cs       反转执行器（步骤 5）
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
