using FirstWorkflow;
using Microsoft.Agents.AI.Workflows;

// 本示例纯离线：两个执行器由一条边串成（大写 → 反转），
// 不依赖模型、密钥或网络，可独立运行验证。
var workflow = WorkflowFactory.Build();

await using Run run = await InProcessExecution.RunAsync(workflow, "Hello, World!");
foreach (WorkflowEvent evt in run.NewEvents)
{
    if (evt is ExecutorCompletedEvent executorComplete)
    {
        Console.WriteLine($"{executorComplete.ExecutorId}: {executorComplete.Data}");
    }
}
