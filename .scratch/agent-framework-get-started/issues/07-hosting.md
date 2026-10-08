# 07: 托管你的智能体（ASP.NET Core）

**What to build:** 新增一个 ASP.NET Core 项目，用 `AddAIAgent` 在依赖注入中注册 agent 并经协议端点暴露；启动宿主后，客户端发送请求即可获得 agent 的响应。

**Blocked by:** 03（多轮次对话（AgentSession））

**Status:** ready-for-agent

- [ ] 启动宿主后，向暴露的端点发送请求能得到 agent 的响应
- [ ] agent 复用 OpenCode Go 的配置（会话头与模型 ID）
- [ ] README 记录该步的启动与调用方式
