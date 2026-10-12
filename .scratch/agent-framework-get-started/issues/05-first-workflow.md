# 05: 首个工作流（Executor + Edge）

**What to build:** 新增一个独立项目，演示两个执行器串成一张图（文本转大写 → 反转字符串），运行工作流并打印每个执行器的完成事件。

**Blocked by:** None (can start immediately)

**Status:** done

- [x] 运行该项目时，输出包含两个执行器各自的完成事件与结果
- [x] 不依赖模型或网络即可运行
- [x] 解决方案 `MfaTest.slnx` 收录该项目
- [x] `dotnet test` 通过（工作流端到端断言）

## Comments

- 已实现：新增 `src/FirstWorkflow`（`WorkflowFactory` 构图 + `Executors/ReverseTextExecutor` + `Program` 编排）。运行输出 `UppercaseExecutor: HELLO, WORLD!` 与 `ReverseTextExecutor: !DLROW ,OLLEH`；不需要密钥、不联网。
- 测试缝：构图抽到 `WorkflowFactory.Build()`，新增 `tests/FirstWorkflow.Tests` 对「输入 → 输出」做离线端到端断言（`dotnet test` 绿）。决策见 ADR-0002。
- 记录与收尾：新增 `docs/adr/0002-workflow-test-seam.md` 与根 `CONTEXT.md`（术语表）；README 补本步运行/测试方式与结构树；伞形 spec 的 Testing Decisions 已指向新决策。
- 细分切片与决策详见 `.scratch/first-workflow/`（spec 与 issues 01–03）。
