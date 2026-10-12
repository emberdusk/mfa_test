# 02: 首个测试项目：工作流端到端断言

**What to build:** 仓库首个测试项目。经工厂缝构建工作流、跑 `"Hello, World!"`、断言输出 `!DLROW ,OLLEH`（并确认两个执行器都产出了完成事件）；`dotnet test` 通过、离线可跑；项目被解决方案收录。

**Blocked by:** 01（需要工厂缝与工作流项目）

**Status:** done

- [x] `dotnet test` 通过
- [x] 测试只断言外部可观察行为（输出/事件），不断言执行器内部实现
- [x] 不依赖模型、密钥或网络
- [x] 解决方案 `MfaTest.slnx` 收录测试项目

## Comments

- 已实现：`tests/FirstWorkflow.Tests`（xunit，SDK 模板默认版本）。测试经 `WorkflowFactory.Build()` 构建工作流，断言两个执行器的完成事件，并断言**声明的输出事件**（`WorkflowOutputEvent`）来自反转执行器、数据为 `!DLROW ,OLLEH`——因此 `WithOutputFrom` 被真实覆盖，而非只靠完成事件。
- `dotnet test`（仓库根）绿：1/1 通过。
