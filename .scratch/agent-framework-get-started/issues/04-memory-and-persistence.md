# 04: 内存与持久性（ChatHistoryProvider）

**What to build:** 在 `HelloAgent` 中通过 `ChatClientAgentOptions` 显式配置聊天历史提供程序，并演示在一个会话内跨多次调用共享上下文（而非仅依赖默认行为）。

**Blocked by:** 03（多轮次对话（AgentSession））

**Status:** ready-for-agent

- [ ] agent 使用显式配置的历史提供程序（而非仅默认的内存历史）
- [ ] 会话内多次调用共享上下文，可回答依赖前文的问题
- [ ] README 记录该步的运行方式
