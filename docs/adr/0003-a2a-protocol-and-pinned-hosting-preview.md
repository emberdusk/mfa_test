# 步骤 7 经 A2A 协议暴露智能体，并钉住托管包的 1.24.0 preview 版本

步骤 7 需要把 agent 经「协议端点」对外暴露。我们选用 **A2A 协议**（`Microsoft.Agents.AI.Hosting.A2A*` 包，JSON-RPC 与 HTTP/JSON 双绑定 + well-known agent card），并把三个托管包全部钉在 **`1.24.0-preview.261006.1`**——托管库只有 preview 版，而这一版的依赖正是仓库其余项目在用的稳定版 `Microsoft.Agents.AI 1.24.0`。

## 考虑过的替代方案

- **minimal API 手写端点**（`app.MapPost(...)` 直接调 `RunAsync`）：实现最省事，但不是「协议」——讲不了教程想教的协议适配器架构，客户端也无法靠服务发现得知如何调用。
- **OpenAI 兼容端点**（`Microsoft.Agents.AI.Hosting.OpenAI`，路由 `POST /{agent.Name}/v1/chat/completions`）：与本仓库「一切 OpenAI 兼容」的气质一致，任何 OpenAI SDK 可直接调；但该包是 **alpha** 后缀（比 preview 更不稳定），且教程步骤 7 的教学主体是 A2A。留作日后候选。
- **`1.25.0-preview.261014.1`（更新的 preview）**：会把 `Microsoft.Agents.AI` 一并带离 1.24.0，与 `HelloAgent`/`FirstWorkflow` 产生跨版本差异，收益（更新）不抵代价（版本分裂）。

## 后果

- **教程片段与源码不符，按源码落地**：教程 C# 分支写的 `builder.Services.AddA2AServer()`（无参重载）与 `app.MapA2AServer()` 在 `dotnet-1.24.0` 与 `main` 源码中都不存在；教程的 C# step-7 完整样例目录也已迁走（只剩迁移说明）。实际 API 是 `hostedAgent.AddA2AServer()` + `app.MapA2AJsonRpc(agent, "/")` / `app.MapA2AHttpJson(agent, "/")` + `app.MapWellKnownAgentCard(card)`，依据为官方样例 `samples/05-end-to-end/A2AClientServer` 与包源码（README「与教程的差异」已注记）。
- **A2A 调用细节**：JSON-RPC 方法名为 `SendMessage`（`message/send` 返回 `-32601`），`role` 枚举为 `ROLE_USER`（小写 `user` 反序列化失败）；名片发布在 `/.well-known/agent-card.json`。
- **升级是显式动作**：日后要换 `1.25.x` 或等 stable，需同步改 `HostedAgent.csproj` 并回归三个端点；不得让其余项目被动升版。
- `HostedAgent` 经项目引用复用 `HelloAgent`（拿 `AgentFactory.CreateChatClient`），层次上是 Web 项目引用控制台项目——为保持「会话头与模型 ID 只有一处」而接受，见 spec 的装配入口决策。
