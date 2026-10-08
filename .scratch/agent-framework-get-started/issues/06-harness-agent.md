# 06: 工具包智能体（AsHarnessAgent）

**What to build:** 在 `HelloAgent` 中用 `AsHarnessAgent` 从聊天客户端创建工具包智能体，配合一个会话与交互式流式循环，使其能对多步请求制定计划并跟踪待办。

**Blocked by:** 04（内存与持久性（ChatHistoryProvider））

**Status:** ready-for-agent

- [ ] 对多步请求，agent 会给出计划/待办并推进执行
- [ ] 会话状态（计划、待办、历史）跨轮保留
- [ ] README 记录该步的运行方式
