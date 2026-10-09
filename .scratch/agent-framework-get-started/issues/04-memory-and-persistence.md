# 04: 内存与持久性（ChatHistoryProvider）

**What to build:** 在 `HelloAgent` 中通过 `ChatClientAgentOptions` 显式配置聊天历史提供程序，并演示在一个会话内跨多次调用共享上下文（而非仅依赖默认行为）。

**Blocked by:** 03（多轮次对话（AgentSession））

**Status:** done

- [x] agent 使用显式配置的历史提供程序（而非仅默认的内存历史）
- [x] 会话内多次调用共享上下文，可回答依赖前文的问题
- [x] README 记录该步的运行方式

## Comments

实现完成（C#，参照 Microsoft Learn「步骤 4：内存和持久性」）：

- 新增 `src/HelloAgent/ChatHistory/CustomChatHistoryProvider.cs`：自定义 `ChatHistoryProvider`，把历史存在提供程序的字典中，会话标识经 `ProviderSessionState<T>` 写入 `AgentSession.StateBag`。
- `AgentFactory.Create` 增加可选参数 `chatHistoryProvider`，经 `ChatClientAgentOptions.ChatHistoryProvider` 显式装配（不传时保持框架默认）。
- `Program.cs` 增加步骤 4 演示：用同一会话先自我介绍再追问「你记得我什么？」；提供程序在读写两处打印日志，便于观察。
- `dotnet build src/HelloAgent` 通过（0 警告 0 错误）。
