using System.ClientModel;
using System.ClientModel.Primitives;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;

namespace HelloAgent;

/// <summary>
/// 负责装配 <see cref="AIAgent"/>：客户端、会话头与工具都集中在这里，
/// 后续更换模型或增删工具只需改动本类。
/// </summary>
public static class AgentFactory
{
    /// <summary>按给定配置创建一个已挂载工具的 <see cref="AIAgent"/>。</summary>
    public static AIAgent Create(
        string apiKey,
        string baseUrl,
        string model,
        string sessionId,
        IList<AITool>? tools = null,
        string instructions = "你是一个友好的助手，回答请保持简洁。",
        string name = "HelloAgent")
    {
        // OpenCode Go 要求每个请求带上 x-opencode-session（路由/提示缓存要求）；
        // 缺失会被网关以 400 MissingSessionID 拒绝。这里在传输层统一注入。
        // 注意：HttpClient 需与进程同级存活，不能在此局部释放。
        var http = new HttpClient();
        http.DefaultRequestHeaders.Add("x-opencode-session", sessionId);
        http.DefaultRequestHeaders.UserAgent.ParseAdd("helloagent/1.0");

        // OpenAIClientOptions：把请求指向 OpenCode Go 的 OpenAI 兼容端点，而非官方 api.openai.com。
        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri(baseUrl),
            Transport = new HttpClientPipelineTransport(http),
        };

        // OpenAIClient：官方 OpenAI .NET SDK 的入口客户端，这里用 API Key 认证。
        // GetChatClient 选中 Chat Completions API 与指定模型 ID，再由 AsAIAgent
        // 包装成 Agent Framework 的 AIAgent（传入 tools 后会自动调用）。
        return new OpenAIClient(new ApiKeyCredential(apiKey), options)
            .GetChatClient(model)
            .AsAIAgent(
                instructions: instructions,
                name: name,
                tools: tools);
    }
}
