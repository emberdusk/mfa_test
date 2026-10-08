# mfa_test

一个最小可运行的示例：用 C# 与 [Microsoft Agent Framework](https://learn.microsoft.com/agent-framework/) 创建 AI agent，模型后端接入 **OpenCode Go**（OpenAI 兼容接口）。

对应教程：[步骤 1：你的第一个智能体](https://learn.microsoft.com/zh-cn/agent-framework/get-started/your-first-agent?pivots=programming-language-csharp)（C# 分支）。

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

预期输出：先回答「法国最大的城市是哪里？」，再以流式方式回答「用一句话讲个有趣的冷知识。」。

## 项目结构

```
MfaTest.slnx                     解决方案
src/HelloAgent/Program.cs        agent 创建与调用
docs/adr/                        架构决策记录
```

## 实现说明

- 使用 **Chat Completions** 客户端（`OpenAI.Chat`）。
- OpenCode Go 要求每个请求携带 `x-opencode-session` 头，否则返回 `400 MissingSessionID`。本示例通过在 `HttpClient` 默认请求头上注入该头来满足要求。
- `AIAgent` 的调用形态（`RunAsync` / `RunStreamingAsync`）与教程一致，后端差异被隔离在客户端构造处。
