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

**Harness（工具包）**:
一种预装配的智能体形态：把规划、待办跟踪、文件内存、工具审批等能力一次性装上，使其能自行分多步完成任务，而不必由使用方逐件组装。
_Avoid_: 框架、脚手架、agent 框架

**Todo（待办）**:
工具包在一次多步任务中维护的工作项清单；每项带标题与完成标记，随会话保留，是计划被逐项推进的凭据。
_Avoid_: 任务列表、checklist、to-do list

**Mode（模式）**:
工具包的工作阶段，取值为 `plan`（先拆解请求并列成待办）与 `execute`（按待办逐项推进）。
_Avoid_: 状态、阶段、phase、stage

**托管（Hosting）**:
把智能体装进一个常驻进程、由依赖注入统一装配，并对外提供服务的形态。
_Avoid_: 部署、deployment、服务器

**协议端点（Protocol Endpoint）**:
宿主对外暴露智能体的通信入口；请求与响应的格式由所选协议定义，智能体实现本身与协议无关。
_Avoid_: API 接口、路由、route
