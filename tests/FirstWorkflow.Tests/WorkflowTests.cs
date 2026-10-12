using FirstWorkflow;
using Microsoft.Agents.AI.Workflows;

namespace FirstWorkflow.Tests;

/// <summary>
/// 工作流的端到端行为：输入 <c>"Hello, World!"</c>，先大写、再反转，输出 <c>"!DLROW ,OLLEH"</c>。
/// 只断言外部可观察行为（完成事件与声明的输出），不触碰执行器内部实现。
/// </summary>
public class WorkflowTests
{
    [Fact]
    public async Task Workflow_uppercases_then_reverses_input()
    {
        var workflow = WorkflowFactory.Build();

        await using Run run = await InProcessExecution.RunAsync(workflow, "Hello, World!");

        // NewEvents 返回「自上次访问以来」的事件，故只取一次快照再断言。
        var events = run.NewEvents.ToList();

        // 两个执行器都应产出完成事件。
        var completed = events.OfType<ExecutorCompletedEvent>().ToList();
        Assert.Equal(2, completed.Count);
        Assert.Contains(completed, e => e.ExecutorId == "UppercaseExecutor");
        Assert.Contains(completed, e => e.ExecutorId == "ReverseTextExecutor");

        // 反转执行器被 WithOutputFrom 声明为工作流输出，其输出事件即最终结果。
        var output = Assert.Single(events.OfType<WorkflowOutputEvent>());
        Assert.Equal("ReverseTextExecutor", output.ExecutorId);
        Assert.Equal("!DLROW ,OLLEH", Assert.IsType<string>(output.Data));
    }
}
