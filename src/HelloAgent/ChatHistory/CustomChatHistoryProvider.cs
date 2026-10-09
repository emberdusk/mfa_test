using System.Collections.Concurrent;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace HelloAgent.ChatHistory;

/// <summary>
/// 一个显式配置的聊天历史提供程序：把历史保存在提供程序自己的存储中（以会话为单位），
/// 而不是依赖框架默认的 <c>InMemoryChatHistoryProvider</c>。
/// </summary>
/// <remarks>
/// 它演示了 <see cref="ChatHistoryProvider"/> 的两个关键钩子：
/// 每次调用前由 <see cref="ProvideChatHistoryAsync"/> 取出该会话既有历史并拼进请求，
/// 调用后再由 <see cref="StoreChatHistoryAsync"/> 追加本轮消息。
/// 这里用进程内的字典作为存储；要获得真正的持久化，只需把字典替换为文件 / 数据库，
/// 存储契约不变。会话标识（<see cref="State.SessionId"/>）通过
/// <see cref="ProviderSessionState{T}"/> 写进 <see cref="AgentSession.StateBag"/>，
/// 因此可随会话序列化一起保存与恢复。
/// </remarks>
public sealed class CustomChatHistoryProvider : ChatHistoryProvider
{
    private readonly ConcurrentDictionary<string, List<ChatMessage>> _store = new();
    private readonly ProviderSessionState<State> _sessionState;
    private IReadOnlyList<string>? _stateKeys;

    /// <param name="stateKey">
    /// 写入会话状态包的键；默认为类型名，避免与其它提供程序冲突。
    /// </param>
    public CustomChatHistoryProvider(string? stateKey = null)
    {
        _sessionState = new ProviderSessionState<State>(
            _ => new State(Guid.NewGuid().ToString("N")),
            stateKey ?? nameof(CustomChatHistoryProvider));
    }

    /// <summary>
    /// 本提供程序占用的会话状态键，供框架做冲突校验。
    /// </summary>
    public override IReadOnlyList<string> StateKeys => _stateKeys ??= [_sessionState.StateKey];

    protected override ValueTask<IEnumerable<ChatMessage>> ProvideChatHistoryAsync(
        InvokingContext context,
        CancellationToken cancellationToken = default)
    {
        var state = _sessionState.GetOrInitializeState(context.Session);
        var history = _store.TryGetValue(state.SessionId, out var stored)
            ? stored.ToArray()
            : [];

        Console.WriteLine($"[历史] 载入会话 {Short(state.SessionId)} 的 {history.Length} 条历史消息");
        return ValueTask.FromResult<IEnumerable<ChatMessage>>(history);
    }

    protected override ValueTask StoreChatHistoryAsync(
        InvokedContext context,
        CancellationToken cancellationToken = default)
    {
        var state = _sessionState.GetOrInitializeState(context.Session);
        var stored = _store.GetOrAdd(state.SessionId, _ => []);

        stored.AddRange(context.RequestMessages);
        stored.AddRange(context.ResponseMessages ?? []);

        Console.WriteLine($"[历史] 保存会话 {Short(state.SessionId)} 的消息，现共 {stored.Count} 条");
        return ValueTask.CompletedTask;
    }

    private static string Short(string id) => id.Length > 8 ? id[..8] : id;

    /// <summary>
    /// 写入 <see cref="AgentSession.StateBag"/> 的每会话状态。
    /// </summary>
    public sealed class State
    {
        public State(string sessionId) => SessionId = sessionId;

        /// <summary>
        /// 用于在本提供程序存储中定位该会话历史的键。
        /// </summary>
        public string SessionId { get; }
    }
}
