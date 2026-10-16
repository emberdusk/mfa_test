using A2A;
using A2A.AspNetCore;
using HelloAgent;
using HelloAgent.Tools;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;

// 配置全部来自环境变量，避免把密钥写入仓库（与 HelloAgent 同一套变量）。
var apiKey = Environment.GetEnvironmentVariable("LLM_API_KEY")
    ?? throw new InvalidOperationException("请设置 LLM_API_KEY 环境变量。");
var baseUrl = Environment.GetEnvironmentVariable("LLM_BASE_URL") ?? "https://opencode.ai/zen/go/v1";
// 注意：这里必须是模型 ID（小写，如 mimo-v2.6-flash），不是显示名（MiMo-V2.6-Flash）。
var model = Environment.GetEnvironmentVariable("LLM_CHAT_MODEL") ?? "mimo-v2.6-flash";
// OpenCode Go 要求每个会话带上 x-opencode-session，用于请求路由与提示缓存。
var sessionId = Environment.GetEnvironmentVariable("LLM_SESSION_ID") ?? Guid.NewGuid().ToString("N");
// agent card 上公布的自身地址；默认与 launchSettings.json 的固定端口一致。
var agentUrl = Environment.GetEnvironmentVariable("A2A_AGENT_URL") ?? "http://localhost:5087";

const string agentName = "hosted-agent";
const string chatClientKey = "chat-model";

var builder = WebApplication.CreateBuilder(args);

// 聊天客户端注册为 keyed 单例：x-opencode-session 头、端点与模型 ID 仍只来自 AgentFactory。
builder.Services.AddKeyedSingleton<IChatClient>(
    chatClientKey,
    AgentFactory.CreateChatClient(apiKey, baseUrl, model, sessionId));

// AddAIAgent 在依赖注入中构造 agent；指令取 AgentFactory 的共享常量，工具经 WithAITool 挂上。
var hostedAgent = builder.AddAIAgent(
        agentName,
        AgentFactory.DefaultInstructions,
        description: "托管在 ASP.NET Core 中的示例智能体，经 A2A 协议暴露。",
        chatClientServiceKey: chatClientKey)
    .WithAITool(AIFunctionFactory.Create(WeatherTools.GetWeather));

// A2A 服务注册须在映射端点之前：两个 Map* 按 agent 名从容器惰性解析。
hostedAgent.AddA2AServer();

var app = builder.Build();

// 名片发布在 well-known 发现端点：客户端先取它，再按其中的地址与协议绑定发消息。
AgentCard agentCard = new()
{
    Name = agentName,
    Description = "托管在 ASP.NET Core 中的示例智能体，经 A2A 协议暴露。",
    Version = "1.0.0",
    DefaultInputModes = ["text/plain"],
    DefaultOutputModes = ["text/plain"],
    Capabilities = new()
    {
        Streaming = false,
        PushNotifications = false,
    },
    Skills =
    [
        new()
        {
            Id = "weather",
            Name = "天气查询",
            Description = "查询指定地点的天气（示例为只读假数据）。",
            Tags = ["weather"],
            Examples = ["阿姆斯特丹的天气怎么样？"],
        },
    ],
    SupportedInterfaces =
    [
        new() { Url = agentUrl, ProtocolBinding = ProtocolBindingNames.JsonRpc, ProtocolVersion = "1.0" },
        new() { Url = agentUrl, ProtocolBinding = ProtocolBindingNames.HttpJson, ProtocolVersion = "1.0" },
    ],
};

// 同一个 agent 暴露为两种 A2A 绑定：JSON-RPC（POST /）与 HTTP/JSON（POST /message:send）。
app.MapA2AJsonRpc(hostedAgent, "/");
app.MapA2AHttpJson(hostedAgent, "/");
app.MapWellKnownAgentCard(agentCard);

app.Run();
