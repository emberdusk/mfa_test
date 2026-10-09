using HelloAgent;
using HelloAgent.ChatHistory;
using HelloAgent.Tools;
using Microsoft.Extensions.AI;

// 配置全部来自环境变量，避免把密钥写入仓库。
var apiKey = Environment.GetEnvironmentVariable("LLM_API_KEY")
    ?? throw new InvalidOperationException("请设置 LLM_API_KEY 环境变量。");
var baseUrl = Environment.GetEnvironmentVariable("LLM_BASE_URL") ?? "https://opencode.ai/zen/go/v1";
// 注意：这里必须是模型 ID（小写，如 mimo-v2.6-flash），不是显示名（MiMo-V2.6-Flash）。
var model = Environment.GetEnvironmentVariable("LLM_CHAT_MODEL") ?? "mimo-v2.6-flash";
// OpenCode Go 要求每个会话带上 x-opencode-session，用于请求路由与提示缓存。
var sessionId = Environment.GetEnvironmentVariable("LLM_SESSION_ID") ?? Guid.NewGuid().ToString("N");

// 装配 agent：客户端与会话头在 AgentFactory 内，工具在此登记。
// 新增工具 = 新建一个 XxxTools 类，再在下面的 tools 列表里追加一项。
var agent = AgentFactory.Create(
    apiKey,
    baseUrl,
    model,
    sessionId,
    tools: [AIFunctionFactory.Create(WeatherTools.GetWeather)]);

// 步骤 1：基本调用（非流式 + 流式）。
Console.WriteLine("=== 步骤 1：基本调用 ===");
Console.WriteLine(await agent.RunAsync("法国最大的城市是哪里？"));
await foreach (var update in agent.RunStreamingAsync("用一句话讲个有趣的冷知识。"))
{
    Console.Write(update);
}
Console.WriteLine();

// 步骤 2：工具调用——agent 会自动调用 GetWeather，再用其返回结果作答。
Console.WriteLine();
Console.WriteLine("=== 步骤 2：添加工具 ===");
Console.WriteLine(await agent.RunAsync("阿姆斯特丹的天气怎么样？"));

// 步骤 3：多轮对话——复用同一个会话，agent 能记住前文。
Console.WriteLine();
Console.WriteLine("=== 步骤 3：多轮对话 ===");
var session = await agent.CreateSessionAsync();
Console.WriteLine(await agent.RunAsync("我叫 Alice，喜欢徒步。", session));
Console.WriteLine(await agent.RunAsync("你记得我什么？", session));

// 步骤 4：内存与持久性——显式配置 ChatHistoryProvider（而非依赖默认行为）。
// CustomChatHistoryProvider 把历史存在自己的存储中，并打印每轮「载入 / 保存」日志，
// 便于观察历史如何被存取；会话内多次调用因此共享上下文。
Console.WriteLine();
Console.WriteLine("=== 步骤 4：内存与持久性 ===");
var memoryAgent = AgentFactory.Create(
    apiKey,
    baseUrl,
    model,
    sessionId,
    chatHistoryProvider: new CustomChatHistoryProvider());
var memorySession = await memoryAgent.CreateSessionAsync();
Console.WriteLine(await memoryAgent.RunAsync("你好！9的平方根是多少？", memorySession));
Console.WriteLine(await memoryAgent.RunAsync("我叫 Alice，喜欢徒步。", memorySession));
Console.WriteLine(await memoryAgent.RunAsync("你记得我什么？", memorySession));
Console.WriteLine(await memoryAgent.RunAsync("我总共问了几个问题？", memorySession));
