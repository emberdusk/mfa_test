# 通过 OpenAI 兼容的 Chat Completions 接入 OpenCode Go

本示例对应 Microsoft Learn 的「你的第一个智能体」（C#）教程，但该教程假设模型后端是 Microsoft Foundry（`AIProjectClient` + `DefaultAzureCredential`）。我们改用 OpenCode Go。

OpenCode Go 暴露 OpenAI 兼容的 Chat Completions API。因此我们借助 `Microsoft.Agents.AI.OpenAI`，把官方 OpenAI .NET 客户端（`OpenAIClient` + `ApiKeyCredential` + 自定义 `Endpoint`）包装一层，通过 `.GetChatClient(model).AsAIAgent(...)` 得到标准的 `AIAgent`。

## 考虑过的替代方案

- **Microsoft Foundry**：教程原方案，但需要 Foundry 项目与 Azure 凭据。
- **Azure OpenAI（`AzureOpenAIClient`）**：同样依赖 Azure 资源。
- **OpenAI Responses 客户端**：OpenCode Go 对不同模型分别提供 Chat Completions、Responses、Anthropic Messages 三种兼容面；本示例只取 Chat Completions，以覆盖最广的模型范围。
- **直连模型厂商官方端点（如 DeepSeek）**：也可行，但需要一个厂商账号与密钥；改用 OpenCode Go 后，一个密钥即可访问其目录中的多个模型。

## 后果

- agent 的公开调用形态（`RunAsync` / `RunStreamingAsync`）与教程一致，后端差异被隔离在客户端构造处。
- OpenCode Go 要求每个请求携带 `x-opencode-session` 头，否则返回 `400 MissingSessionID`；本示例在 `HttpClient` 默认请求头上注入该头。
- 模型参数必须是模型 **ID**（小写，如 `mimo-v2.6-flash`），不能用显示名（如 `MiMo-V2.6-Flash`），否则返回 `Model is unavailable`。
- 仅具备 Chat Completions 提供的能力（函数工具、本地 MCP 等），不支持 Responses 专属的 hosted tools（如 code interpreter、file search）。
- 该后端无法在离线或无密钥环境下运行，且本示例是无单元测试缝的顶层语句控制台程序；回归守卫依赖端到端 `dotnet run`。
