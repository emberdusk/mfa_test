using HelloAgent;
using HelloAgent.ChatHistory;
using HelloAgent.Tools;
using Microsoft.Agents.AI;
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

// 步骤 6：工具包智能体——AsHarnessAgent 一次装上多步任务所需的能力（计划/执行模式、
// 待办跟踪、文件内存、工具审批），状态（计划、待办、历史）随会话保留。
// 与步骤 1–4 的脚本式演示不同，这一步是交互式的：输入多步请求，观察它计划与推进。
Console.WriteLine();
Console.WriteLine("=== 步骤 6：工具包智能体（AsHarnessAgent） ===");
var harnessAgent = AgentFactory.CreateHarness(
    apiKey,
    baseUrl,
    model,
    sessionId,
    tools: [AIFunctionFactory.Create(WeatherTools.GetWeather)]);
var harnessSession = await harnessAgent.CreateSessionAsync();

// 计划与待办存在会话里，agent 本身不暴露它们；取到提供程序即可读出，用于打印状态行。
// 1.24.0 上取不到时为 null（降级：该行不打印）。
var modeProvider = harnessAgent.GetService<AgentModeProvider>();
var todoProvider = harnessAgent.GetService<TodoProvider>();

Console.WriteLine("输入一个多步请求试试，例如：帮我规划一次周末去阿姆斯特丹的两日行程，并给出天气建议");
Console.WriteLine("（空行跳过，输入 exit 退出；会话内待办与会话状态跨轮保留。）");

while (true)
{
    Console.Write("> ");
    var input = Console.ReadLine();

    // 标准输入读到结尾（管道、重定向）与输入 exit 同样退出；空行只是跳过。
    if (input is null || input.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }
    if (input.Trim().Length == 0)
    {
        continue;
    }

    try
    {
        await foreach (var update in harnessAgent.RunStreamingAsync(input, harnessSession))
        {
            Console.Write(update);
        }
        Console.WriteLine();
    }
    catch (Exception ex)
    {
        // 模型偶尔会发出畸形工具调用（例如缺必填参数），框架此时会直接抛异常。
        // 单轮失败不该带走整个会话：打印一行后继续，会话状态与待办都还在。
        Console.WriteLine();
        Console.WriteLine($"[错误] 本轮失败：{ex.Message}");
        continue;
    }

    try
    {
        await PrintHarnessStateAsync(modeProvider, todoProvider, harnessSession);
    }
    catch (Exception ex)
    {
        // 状态行只是「看得见」的辅助：读不出来不该牵连本轮已经成功的回复。
        Console.WriteLine($"[错误] 状态读取失败：{ex.Message}");
    }
}

// 打印一行状态：当前模式与待办清单（如 `[mode] plan | 待办 1/3  [x] 查天气  [ ] 排行程`）。
static async Task PrintHarnessStateAsync(
    AgentModeProvider? modeProvider,
    TodoProvider? todoProvider,
    AgentSession session)
{
    var parts = new List<string>();

    if (modeProvider is not null)
    {
        parts.Add($"[mode] {await modeProvider.GetModeAsync(session)}");
    }

    if (todoProvider is not null)
    {
        var todos = await todoProvider.GetAllTodosAsync(session);
        var items = string.Join("  ", todos.Select(t => $"{(t.IsComplete ? "[x]" : "[ ]")} {t.Title}"));
        parts.Add($"待办 {todos.Count(t => t.IsComplete)}/{todos.Count}  {items}");
    }

    if (parts.Count > 0)
    {
        Console.WriteLine(string.Join(" | ", parts));
    }
}
