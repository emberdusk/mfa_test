# 01: 工作流项目：Executor + Edge 可离线运行

**What to build:** 一个独立的工作流项目：两个执行器经一条边顺序连接（函数绑定式大写 → 执行器子类反转），构图逻辑收进一个工厂、程序入口只做「取工作流 → 运行 → 打印事件」。运行后打印两个执行器各自的完成事件，最终输出 `!DLROW ,OLLEH`；不依赖模型、不联网；项目被解决方案收录。

**Blocked by:** None (can start immediately)

**Status:** done

- [x] 运行该项目时，输出包含两个执行器各自的完成事件与结果
- [x] 最终输出为 `!DLROW ,OLLEH`
- [x] 不依赖模型、密钥或网络即可运行
- [x] 构图逻辑位于工厂内，程序入口只负责编排与打印
- [x] 解决方案 `MfaTest.slnx` 收录该项目

## Comments

- 已实现：`src/FirstWorkflow`（`WorkflowFactory` 构图 + `Executors/ReverseTextExecutor` + `Program` 编排）。`dotnet run --project src/FirstWorkflow` 输出 `UppercaseExecutor: HELLO, WORLD!` 与 `ReverseTextExecutor: !DLROW ,OLLEH`；不需密钥、不联网。
- 包版本固定 `Microsoft.Agents.AI.Workflows 1.24.0`，与仓库既有包一致。
