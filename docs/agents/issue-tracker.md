# 问题跟踪器：本地 Markdown

本仓库的 issue 与 spec 以 markdown 文件形式存放在 `.scratch/` 下。

## 约定

- 每个功能一个目录：`.scratch/<feature-slug>/`
- spec 文件为 `.scratch/<feature-slug>/spec.md`
- 实现类 issue 每个工单一文件，位于 `.scratch/<feature-slug>/issues/<NN>-<slug>.md`，从 `01` 开始编号，绝不合并成单个 tickets 文件
- 分诊状态以 `Status:` 行的形式记录在每个 issue 文件顶部附近（角色字符串见 `triage-labels.md`）
- 评论与对话历史追加到文件底部，置于 `## Comments` 标题之下

## 当某个技能说「发布到问题跟踪器」

在 `.scratch/<feature-slug>/` 下新建文件（必要时创建目录）。

## 当某个技能说「获取相关工单」

读取所引用路径处的文件。用户通常会直接给出路径或工单编号。

## 寻路（Wayfinding）操作

由 `/wayfinder` 使用。**map** 是一个文件，每个工单对应一个 **child** 文件。

- **Map**：`.scratch/<effort>/map.md`（承载 Notes / Decisions-so-far / Fog 的正文）。
- **Child 工单**：`.scratch/<effort>/issues/NN-<slug>.md`，从 `01` 开始编号，正文中写明问题。`Type:` 行记录工单类型（`research`/`prototype`/`grilling`/`task`）；`Status:` 行记录 `claimed`/`resolved`。
- **阻塞**：顶部附近的 `Blocked by: NN, NN` 行。当它列出的每个文件都变为 `resolved` 时，该工单即解除阻塞。
- **Frontier**：扫描 `.scratch/<effort>/issues/`，找出处于打开、未阻塞且未认领状态的文件；编号最小者优先。
- **认领**：动手前先设置 `Status: claimed` 并保存。
- **解决**：在 `## Answer` 标题下追加答案，设置 `Status: resolved`，然后把一条上下文指针（要点 + 链接）追加到 `map.md` 中 map 的 Decisions-so-far 下。
