# 01: 多轮次对话（AgentSession）

**What to build:** 在 `HelloAgent` 中演示会话记忆：先创建一次会话并自我介绍（姓名与爱好），再在同一会话里追问「你记得我什么？」，agent 能答出前一轮提供的信息。

**Blocked by:** None (can start immediately)

**Status:** done

- [x] 运行 `HelloAgent` 时，第二轮的答复包含第一轮给出的姓名/爱好
- [x] 两次调用复用同一个 `AgentSession`（经 `CreateSessionAsync` 获得）
- [x] README 记录该步的运行方式
