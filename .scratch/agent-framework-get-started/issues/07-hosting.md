# 07: 托管你的智能体（ASP.NET Core）

**What to build:** 新增一个 ASP.NET Core 项目，用 `AddAIAgent` 在依赖注入中注册 agent 并经协议端点暴露；启动宿主后，客户端发送请求即可获得 agent 的响应。

**Blocked by:** 03（多轮次对话（AgentSession））

**Status:** done

- [x] 启动宿主后，向暴露的端点发送请求能得到 agent 的响应
- [x] agent 复用 OpenCode Go 的配置（会话头与模型 ID）
- [x] README 记录该步的启动与调用方式

## 决策（grill-with-docs 会话，2026-10-16 定稿）

设计树共三轮、11 问，全部收敛（用户逐轮「全部使用推荐」）。要点：

1. **协议 = A2A**（Q1）：对照过 minimal API 手写与 OpenAI 兼容端点（后者包为 alpha、路由带 agent 名前缀），教程教学主体是 A2A。协议选择与版本钉住入档 [ADR-0003](../../../docs/adr/0003-a2a-protocol-and-pinned-hosting-preview.md)。
2. **项目 = `src/HostedAgent`**（Q2），Web SDK、收进 `MfaTest.slnx`。
3. **装配分工（Q3，spec 已修订）**：`AgentFactory` 收窄为**聊天客户端的唯一装配入口**——`CreateChatClient` 注册为 keyed 单例（`chat-model`），agent 本体由 `AddAIAgent` 按教程链式形态构造。查证中发现 `AddAIAgent` 有工厂重载可两全，但它展示不出教程的 `instructions` 参数与 `.WithAITool(...)` 链，故不采用。
4. **只托管 agent**（Q4）：不做 `AddWorkflow`/`AddAsAIAgent`（`FirstWorkflow` 是离线纯文本图，执行器不是 agent，示范价值低）。
5. **验收 = README + curl 手动端到端**（Q5）：不新增测试项目，与 01–04、06 一致。
6. **代码共享 = 项目引用 `HelloAgent`**（Q6）：不抽类库（重构超范围）、不复制客户端构造（违反「只有一处」）。
7. **被暴露的 agent 挂 `GetWeather`**（Q7），指令取共享常量 `AgentFactory.DefaultInstructions`（本次从 `Create` 的默认参数抽出，避免两处漂移）。
8. **单轮请求/响应**（Q8）：不引入跨请求会话记忆；不加 `WithInMemorySessionStore`（A2A 默认 `NoopAgentSessionStore` 已够单轮用）。
9. **固定端口 5087**（Q9）：`launchSettings.json`，README 的 curl 复制即可用；名片公布的地址可经 `A2A_AGENT_URL` 覆盖。
10. **写 ADR-0003**（Q10）；**CONTEXT.md 新收「托管」「协议端点」两词条**（Q11，已录）。
11. **包版本（Q12）**：`Microsoft.Agents.AI.Hosting` / `.Hosting.A2A` / `.Hosting.A2A.AspNetCore` 全钉 `1.24.0-preview.261006.1`（依赖仓库现用的稳定版 `Microsoft.Agents.AI 1.24.0`，避免跨版本分裂）。
12. **A2A 暴露面 = 全量三个映射**（Q13）：`MapA2AJsonRpc` + `MapA2AHttpJson` + `MapWellKnownAgentCard`，各一行。
13. **教程过时差异入档**（Q14）：README 差异注记 + ADR-0003 后果节。

## 验收剧本（需密钥与网络）

1. `dotnet run --project src/HostedAgent`（监听 `http://localhost:5087`）。
2. `curl http://localhost:5087/.well-known/agent-card.json` → 200，`name` 为 `hosted-agent`，含 `weather` 技能与两个协议绑定地址。
3. `curl -X POST http://localhost:5087/message:send`（HTTP/JSON 绑定）→ 200，`parts` 带 agent 回答。
4. `curl -X POST http://localhost:5087/`，JSON-RPC `SendMessage` → 200，`result.message.parts` 带 agent 回答。
5. 第 3 或 4 条问天气 → 回答为 `WeatherTools` 假数据，证明工具经宿主生效。

## 实现备注（实测）

- **改动**：新增 `src/HostedAgent/`（`HostedAgent.csproj`、`Program.cs`、`Properties/launchSettings.json`）；`MfaTest.slnx` 收录；`AgentFactory` 抽出 `DefaultInstructions` 常量（`Create` 默认参数改指它，对外行为不变）；README（步骤 7 章节、差异注记、配置表、项目结构、实现说明）；本 ADR 与 spec/CONTEXT.md 同步。
- **编译**：`dotnet build src/HostedAgent` 0 警告 0 错误——A2A 端点方法虽带 `[Experimental]` 标记，但在本项目未触发错误级诊断（与 harness 的 `MAAI001` 不同），无需 `#pragma`。
- **教程与源码不符**：`AddA2AServer()` 无参重载与 `MapA2AServer()` 不存在；实际用 `hostedAgent.AddA2AServer()` + `MapA2AJsonRpc`/`MapA2AHttpJson`（源码核实的签名见 ADR-0003）。
- **A2A 调用细节（实测才确认）**：JSON-RPC 方法名是 `SendMessage`（`message/send` → `-32601 Invalid JSON-RPC request`）；`role` 必须是 `ROLE_USER`（小写 `user` → `A2A.Role` 反序列化异常）；名片路径是 `/.well-known/agent-card.json`（不是 `agent.json`）。
- **编码坑（与 06 相同根源）**：经控制台发中文请求体会被转成 GBK，服务端按 UTF-8 解码报 400 `invalid UTF-8`；把 JSON 存 UTF-8 文件后 `--data-binary @file` 即正常。README 已写明。
- **响应形状**：`parts` 常含一个空前文本段（`{"text":""}`）后再跟正文，属正常（历史拼接所致），非错误。
- **会话**：单轮验收，不涉及 `AgentSession`；宿主生命周期内 `x-opencode-session` 固定为启动时的值。

## 验收结果（2026-10-16）

- `dotnet build src/HostedAgent`：0 警告 0 错误。
- `dotnet test`：1/1 通过（步骤 5 回归未受影响）。
- 端到端五项全绿：agent card 200；HTTP/JSON 问「法国最大的城市」→「法国最大的城市是巴黎。」；JSON-RPC 问同题 → 「Paris is the largest city in France.」（模型自选语言）；问天气 → 「阿姆斯特丹目前多云，最高气温 15°C。」（`GetWeather` 经宿主生效）；UTF-8 中文请求体（文件方式）200。
- ticket 三条验收项全部满足。

## Comments

- 2026-10-16 grill-with-docs 会话完成设计（三轮 11 问）并同日实现、验收。CONTEXT.md 随本次新增「托管（Hosting）」「协议端点（Protocol Endpoint）」两词条；spec 的装配入口与托管两处决策已改写。
