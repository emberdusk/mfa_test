using System.ClientModel;
using System.ClientModel.Primitives;
using Microsoft.Agents.AI;
using OpenAI;
using OpenAI.Chat;

// 配置全部来自环境变量，避免把密钥写入仓库。
var apiKey = Environment.GetEnvironmentVariable("LLM_API_KEY")
    ?? throw new InvalidOperationException("请设置 LLM_API_KEY 环境变量。");
var baseUrl = Environment.GetEnvironmentVariable("LLM_BASE_URL") ?? "https://opencode.ai/zen/go/v1";
// 注意：这里必须是模型 ID（小写，如 mimo-v2.6-flash），不是显示名（MiMo-V2.6-Flash）。
var model = Environment.GetEnvironmentVariable("LLM_CHAT_MODEL") ?? "mimo-v2.6-flash";
// OpenCode Go 要求每个会话带上 x-opencode-session，用于请求路由与提示缓存。
var sessionId = Environment.GetEnvironmentVariable("LLM_SESSION_ID") ?? Guid.NewGuid().ToString("N");

// OpenCode Go 提供 OpenAI 兼容的 Chat Completions 接口，因此把官方 OpenAI 客户端
// 指向它，并在传输层注入会话头，再用 AIAgent 包裹一层。
using var http = new HttpClient();
http.DefaultRequestHeaders.Add("x-opencode-session", sessionId);
http.DefaultRequestHeaders.UserAgent.ParseAdd("helloagent/1.0");

// OpenAIClientOptions：OpenAI 客户端的构造配置。
// - Endpoint：把请求指向 OpenCode Go 的 OpenAI 兼容端点，而非官方 api.openai.com。
// - Transport：改用带默认请求头的 HttpClient，使每个请求都自动带上 x-opencode-session
//   （OpenCode Go 的路由/提示缓存要求；缺失会被网关以 400 MissingSessionID 拒绝）。
var options = new OpenAIClientOptions
{
    Endpoint = new Uri(baseUrl),
    Transport = new HttpClientPipelineTransport(http),
};

// OpenAIClient：官方 OpenAI .NET SDK 的入口客户端，这里用 API Key 认证。
// GetChatClient 选中 Chat Completions API 与指定模型 ID，再由 AsAIAgent
// 包装成 Agent Framework 的 AIAgent。
AIAgent agent = new OpenAIClient(new ApiKeyCredential(apiKey), options)
    .GetChatClient(model)
    .AsAIAgent(
        instructions: "你是一个友好的助手，回答请保持简洁。",
        name: "HelloAgent");

// 非流式调用。
Console.WriteLine(await agent.RunAsync("法国最大的城市是哪里？"));

// 流式调用。
await foreach (var update in agent.RunStreamingAsync("用一句话讲个有趣的冷知识。"))
{
    Console.Write(update);
}
