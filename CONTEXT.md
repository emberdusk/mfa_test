# mfa_test

用 C# 与 Microsoft Agent Framework 构建 AI 能力的示例集合，模型后端接入 OpenCode Go；每一步能力都可独立运行演示。

## Language

**Executor（执行器）**:
工作流中的一个处理单元：接收一条输入消息，处理并（可选）产出输出消息。
_Avoid_: 步骤、节点、processor

**Edge（边）**:
连接两个执行器的数据流路径，决定一个执行器的输出流向哪个执行器。
_Avoid_: 连线、链接、transition

**Workflow（工作流）**:
由执行器与边构成的图；数据沿边从上游执行器流向下游执行器。
_Avoid_: 管线、流程、pipeline

**Run（运行）**:
工作流的一次执行实例，承载本次运行产生的事件与输出。
_Avoid_: 执行、execution、session（会话是 agent 侧的概念，勿混用）

**WorkflowEvent（工作流事件）**:
运行过程中发布的事件；本示例关注完成事件——某个执行器结束，携其标识与结果数据。
_Avoid_: 消息、通知
