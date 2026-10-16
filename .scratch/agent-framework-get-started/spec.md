# Spec: Agent Framework 入门教程（步骤 1–7，OpenCode Go 后端）

## Problem Statement

仓库已实现官方教程的步骤 1（第一个智能体）与步骤 2（添加工具），运行在 OpenCode Go 之上。我们希望继续跟随 Microsoft Agent Framework 入门教程，把框架的核心能力——多轮对话、内存/持久性、工作流、工具包、托管——都做成**在 OpenCode Go 上可独立运行**的参考实现，从而不依赖 Azure/Foundry、只需一个 API Key 即可学习与验证。跟踪器中的 ticket 与教程的 7 个步骤一一对应。

## Solution

按教程步骤实现完整能力，每一步都可运行、可验证：

- **步骤 1–2（已完成）**：创建 agent、`RunAsync`/`RunStreamingAsync`、注册函数工具。
- **「agent 类」步骤（3 多轮对话、4 内存、6 工具包）** 扩展现有 `HelloAgent` 控制台项目；
- **形状不同的能力各开一个项目**：5 工作流（纯文本图，不调 LLM）、7 托管（ASP.NET Core Web）。

每一步完成后都能单独 `dotnet run`（或启动宿主）演示其行为。

## User Stories

1. 作为跟随教程的开发者，我想要 agent 在同一个会话里记住前文，这样我能在多轮里追问而不必重复背景。
2. 作为跟随教程的开发者，我想要显式配置聊天历史提供程序，这样我能理解并控制历史如何被存取。
3. 作为跟随教程的开发者，我想要把两个执行器串成工作流并看到每个执行器的完成事件，这样我能理解图式编排。
4. 作为跟随教程的开发者，我想要一个工具包智能体能规划并跟踪多步任务，这样我能处理较长的任务。
5. 作为跟随教程的开发者，我想要通过 HTTP 暴露 agent，这样其他客户端或 agent 能调用它。
6. 作为学习者，我想要每个能力都能单独运行演示，这样我学习时不被其它步骤干扰。
7. 作为学习者，我想要继续沿用 OpenCode Go（一个密钥、无需 Azure），这样我不必申请 Azure 资源。
8. 作为维护者，我想要新增工具只需加一个 XxxTools 类，这样扩展成本低。
9. 作为维护者，我想要 `x-opencode-session` 与会话/模型 ID 的约束被文档化，这样不会再次踩到 400。
10. 作为维护者，我想要工作流不依赖模型即可运行，这样我能离线验证。
11. 作为维护者，我想要托管端点有一个最小可用的请求/响应，这样我能端到端验证。
12. 作为维护者，我想要 README 记录每一步的运行方式，这样我能按需回看。
13. 作为学习者，我想要跟踪器按教程目录顺序（步骤 1→7）编号，这样我能对照官方文档逐步推进。

## Implementation Decisions

- **布局（B）**：`HelloAgent` 承载步骤 1–4、6；为步骤 5 新增「工作流」项目；为步骤 7 新增「托管」项目。`MfaTest.slnx` 收录全部项目。
- **后端**：保持 OpenCode Go；`x-opencode-session` 请求头与模型 ID 约束沿用 ADR-0001。
- **新增包**：步骤 5 用 `Microsoft.Agents.AI.Workflows`；步骤 6 用 `Microsoft.Agents.AI.Harness`；步骤 7 用 `Microsoft.Agents.AI.Hosting*`（preview）。
- **装配入口**：`AgentFactory` 是**聊天客户端**的唯一装配入口（端点、会话头、模型 ID 只此一处）。步骤 1–4、6 的 agent 构造仍在 `AgentFactory` 内完成；步骤 7 的 agent 由托管库 `AddAIAgent` 在 DI 中构造，复用 `AgentFactory.CreateChatClient` 注册进容器的聊天客户端（原「agent 构造的唯一入口」表述自此收窄）。
- **会话**：经 `agent.CreateSessionAsync()` 获得 `AgentSession`，在多次 `RunAsync` 之间复用以保留上下文。
- **内存**：通过 `ChatClientAgentOptions` 配置 `ChatHistoryProvider`，在会话内共享历史。
- **工作流**：用 `WorkflowBuilder` + `Executor` 构图，经 `InProcessExecution` 运行，监听 `ExecutorCompletedEvent`。
- **托管**：新增 `src/HostedAgent`（ASP.NET Core），用 `AddAIAgent` 在 DI 中注册 agent（挂 `GetWeather` 工具）并经 **A2A 协议端点**暴露；只做单轮请求/响应，不引入跨请求会话记忆。`HostedAgent` 经项目引用复用 `HelloAgent` 的 `AgentFactory`（客户端装配接缝），不抽独立类库、不复制客户端构造代码。固定端口便于 README 记录可复制的调用命令；验收以 README + curl 手动端到端为准，不新增测试项目。协议选择与 preview 版本钉住见 ADR-0003。
- **编号**：ticket 编号与教程目录顺序一致（01 your-first-agent … 07 hosting）。
- **ADR**：步骤 5 新增 ADR-0002（记录「工作流以工厂暴露纯逻辑缝并引入首个测试项目」），此后本节不再声称「无新增 ADR」——详见 [ADR-0002](../../docs/adr/0002-workflow-test-seam.md)。其余步骤仍为能力叠加，未产生难逆的架构决策。

## Testing Decisions

- 只测**外部行为**（会话是否记住、工作流输出、端点是否响应），不测实现细节。
- 自**步骤 5**起引入首个测试项目与测试缝：工作流的「输入→输出」以纯逻辑端到端断言，经工厂缝构建工作流、离线运行（见 [`.scratch/first-workflow/spec.md`](../first-workflow/spec.md) 与 ADR-0002）。这**推翻**了本 spec 早先「暂不引入测试项目或新测试缝」的结论。agent 类步骤（1–4、6–7）暂仍以端到端运行为准。
- 若日后需要 agent 侧自动化，可用缝：`AgentFactory` 注入确定性聊天客户端桩（验证 agent 行为，不碰网络）。

## Out of Scope

- Microsoft Foundry / Azure 相关路径与凭据。
- Python 与 Go 语言分支。
- 真实天气数据源（工具仍为只读假数据）。
- 生产级托管（鉴权、持久化会话存储、部署与扩缩容）。

## Further Notes

- OpenCode Go：缺少 `x-opencode-session` 会返回 `400 MissingSessionID`；模型参数必须是**模型 ID**（小写，如 `mimo-v2.6-flash`），显示名会报 `Model is unavailable`。
- 无网络或无密钥时，agent 类步骤（3/4/6/7）无法端到端验证；工作流步骤（5）可离线验证。
- ticket 01（your-first-agent）与 02（add-tools）为**已实现**的基线步骤，状态标记为 `done`。
