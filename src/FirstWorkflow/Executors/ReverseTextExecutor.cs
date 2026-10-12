using Microsoft.Agents.AI.Workflows;

namespace FirstWorkflow.Executors;

/// <summary>
/// 第二个执行器：反转输入文本。因为未被抑制，其返回结果会作为本执行器的输出产出，
/// 并经工厂中的 <c>WithOutputFrom</c> 声明为整张工作流的输出。
/// </summary>
internal sealed class ReverseTextExecutor() : Executor<string, string>("ReverseTextExecutor")
{
    /// <summary>
    /// 反转输入文本。
    /// </summary>
    /// <param name="message">要反转的输入文本。</param>
    /// <param name="context">工作流上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>反转后的文本。</returns>
    public override ValueTask<string> HandleAsync(
        string message,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
        => ValueTask.FromResult(string.Concat(message.Reverse()));
}
