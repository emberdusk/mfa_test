# 02: 添加工具

**What to build:** 定义带 `[Description]` 标注的函数工具（如 `GetWeather`）并注册到 agent，使其在需要时自动调用该工具再作答。

**Blocked by:** 01（你的第一个智能体）

**Status:** done（已实现）

- [x] 工具经 `AIFunctionFactory.Create(...)` 注册到 agent
- [x] 提问涉及天气时 agent 自动调用工具并据其返回作答
- [x] 工具按领域分文件，便于后续扩展
