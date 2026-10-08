# 领域文档

工程类技能在探索代码库时，应如何阅读本仓库的领域文档。

## 探索之前，先读这些

- 仓库根目录下的 **`CONTEXT.md`**；或者
- 若存在 **`CONTEXT-MAP.md`**（仓库根目录）：它指向每个上下文各自的 `CONTEXT.md`。请阅读与当前主题相关的每一个。
- **`docs/adr/`**：阅读涉及你即将改动的区域的 ADR。在多上下文仓库中，还要查看 `src/<context>/docs/adr/` 里针对具体上下文的决策。

若其中任何文件不存在，**静默继续**。不要指出它们缺失，也不要主动建议创建。`/domain-modeling` 技能（经由 `/grill-with-docs` 与 `/improve-codebase-architecture` 触达）会在术语或决策真正定下来时按需懒加载创建它们。

## 文件结构

单上下文仓库（大多数仓库）：

```
/
├── CONTEXT.md
├── docs/adr/
│   ├── 0001-event-sourced-orders.md
│   └── 0002-postgres-for-write-model.md
└── src/
```

多上下文仓库（根目录存在 `CONTEXT-MAP.md`）：

```
/
├── CONTEXT-MAP.md
├── docs/adr/                          ← 系统级决策
└── src/
    ├── ordering/
    │   ├── CONTEXT.md
    │   └── docs/adr/                  ← 上下文专属决策
    └── billing/
        ├── CONTEXT.md
        └── docs/adr/
```

## 使用术语表中的词汇

当你的产出要指称某个领域概念时（无论是 issue 标题、重构提案、假设，还是测试名），请使用 `CONTEXT.md` 中定义的那个词。不要滑向术语表明确要避免的同义词。

若你需要的概念尚未收入术语表，这本身是个信号：要么你在创造项目并不使用的说法（请重新考虑），要么确实存在缺口（记录下来交给 `/domain-modeling`）。

## 标出 ADR 冲突

若你的产出与某个已有 ADR 相冲突，请明确指出来，而不是悄悄覆盖：

> _与 ADR-0007（事件溯源订单）冲突，但值得重新开启，因为……_
