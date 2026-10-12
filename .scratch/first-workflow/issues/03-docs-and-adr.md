# 03: 记录与收尾：ADR-0002 + 文档 + 术语表 + 工单

**What to build:** 让本步的决策与用法可回溯：一篇 ADR 记录「工厂缝 + 首个测试项目」的决策；README 补运行/测试方式、结构树与工作流小节；根目录术语表（`CONTEXT.md`）收录本步引入的领域词汇；修正工单编号笔误并把伞形工单收尾。

**Blocked by:** 01、02（记录面向已定型的最终形态）

**Status:** done

- [x] ADR-0002 存在，含背景、决策与取舍理由
- [x] README 照做可复现本步的运行与测试
- [x] `CONTEXT.md` 收录 Executor / Edge / Workflow / Run / WorkflowEvent
- [x] 伞形工单 05 标题编号与文件名一致，且已置 `done` 并附一条实现记录
- [x] 伞形 spec 的 Testing Decisions 已指向本步的新决策

## Comments

- `docs/adr/0002-workflow-test-seam.md`：记录工厂缝与首个测试项目；并在「后果」中标注其与 ADR-0001「无单元测试缝」表述的关系。
- README：新增步骤 5 教程链接、离线运行与 `dotnet test` 方式、工作流小节、结构树条目与实现说明。
- `CONTEXT.md`：收录 Executor / Edge / Workflow / Run / WorkflowEvent（并标注 Workflow 的禁用同义词）。
- 工单：修正伞形 05 的编号笔误（`# 02` → `# 05`），置 `done` 并附实现记录；伞形 spec 的 Testing Decisions 与 Implementation Decisions（ADR 项）均已指向新决策。
