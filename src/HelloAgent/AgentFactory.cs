using System.ClientModel;
using System.ClientModel.Primitives;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;

namespace HelloAgent;

/// <summary>
/// 负责装配 <see cref="AIAgent"/>：客户端、会话头与工具都集中在这里，
/// 后续更换模型或增删工具只需改动本类。
/// </summary>
public static class AgentFactory
{
    /// <summary>
    /// 步骤 1–4 与步骤 7（托管）共用的默认指令；两处取同一措辞以免漂移。
    /// </summary>
    public const string DefaultInstructions = "你是一个友好的助手，回答请保持简洁。";

    /// <summary>
    /// 构造指向 OpenCode Go 的聊天客户端：端点、模型与会话头只在此处出现。
    /// </summary>
    /// <remarks>
    /// 这是「同一后端、两种 agent」的接缝——<see cref="Create"/> 与
    /// <see cref="CreateHarness"/> 都基于它，以免会话头出现第二份实现。
    /// </remarks>
    public static IChatClient CreateChatClient(
        string apiKey,
        string baseUrl,
        string model,
        string sessionId)
    {
        // OpenCode Go 要求每个请求带上 x-opencode-session（路由/提示缓存要求）；
        // 缺失会被网关以 400 MissingSessionID 拒绝。这里在传输层统一注入。
        // 注意：HttpClient 需与进程同级存活，不能在此局部释放。
        var http = new HttpClient();
        http.DefaultRequestHeaders.Add("x-opencode-session", sessionId);
        http.DefaultRequestHeaders.UserAgent.ParseAdd("helloagent/1.0");

        // OpenAIClientOptions：把请求指向 OpenCode Go 的 OpenAI 兼容端点，而非官方 api.openai.com。
        var clientOptions = new OpenAIClientOptions
        {
            Endpoint = new Uri(baseUrl),
            Transport = new HttpClientPipelineTransport(http),
        };

        // OpenAIClient：官方 OpenAI .NET SDK 的入口客户端，这里用 API Key 认证。
        // GetChatClient 选中 Chat Completions API 与指定模型 ID，再由 AsIChatClient
        // 适配成框架通用的 IChatClient。
        return new OpenAIClient(new ApiKeyCredential(apiKey), clientOptions)
            .GetChatClient(model)
            .AsIChatClient();
    }

    /// <summary>
    /// 按给定配置创建一个已挂载工具的 <see cref="AIAgent"/>。
    /// </summary>
    /// <param name="chatHistoryProvider">
    /// 显式配置的聊天历史提供程序；为 <see langword="null"/> 时由框架按服务能力自行选择
    /// （Chat Completions 会使用默认的 <c>InMemoryChatHistoryProvider</c>）。
    /// </param>
    public static AIAgent Create(
        string apiKey,
        string baseUrl,
        string model,
        string sessionId,
        IList<AITool>? tools = null,
        string instructions = DefaultInstructions,
        string name = "HelloAgent",
        ChatHistoryProvider? chatHistoryProvider = null)
    {
        // ChatClientAgentOptions：agent 级配置。指令与工具放在 ChatOptions 里；
        // 若传入 chatHistoryProvider，则显式启用该历史提供程序（而非依赖默认行为）。
        var agentOptions = new ChatClientAgentOptions
        {
            Name = name,
            ChatOptions = new ChatOptions
            {
                Instructions = instructions,
                Tools = tools,
            },
            ChatHistoryProvider = chatHistoryProvider,
        };

        return CreateChatClient(apiKey, baseUrl, model, sessionId).AsAIAgent(agentOptions);
    }

    /// <summary>
    /// 创建一个工具包智能体：由 Agent Framework 预装配计划/执行模式、待办跟踪、文件内存与工具审批，
    /// 使它能在一次会话里自行分多步完成任务。
    /// </summary>
    /// <remarks>
    /// 除 web 搜索外全部沿用框架默认。本仓库后端是 Chat Completions，不支持 hosted 工具
    /// （见 docs/adr/0001），而 harness 默认会注入 <c>HostedWebSearchTool</c>，故显式关闭。
    /// </remarks>
    public static AIAgent CreateHarness(
        string apiKey,
        string baseUrl,
        string model,
        string sessionId,
        IList<AITool>? tools = null,
        string instructions = "你是一个善于规划的助手：处理多步请求时先列出计划与待办，再逐项推进，并说明每一步的依据。",
        string name = "HelloAgent-Harness")
    {
        var harnessOptions = new HarnessAgentOptions
        {
            Name = name,
            ChatOptions = new ChatOptions
            {
                Instructions = instructions,
                Tools = tools,
            },
            DisableWebSearch = true,
        };

        return CreateChatClient(apiKey, baseUrl, model, sessionId).AsHarnessAgent(harnessOptions);
    }
}
