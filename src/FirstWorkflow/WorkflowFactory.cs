using FirstWorkflow.Executors;
using Microsoft.Agents.AI.Workflows;

namespace FirstWorkflow;

/// <summary>
/// 装配工作流：把执行器与边集中在这里，程序入口只负责运行与打印。
/// 之所以把构图抽成一个对外的工厂，是为了给自动化测试留一个「输入→输出」的纯逻辑缝。
/// </summary>
public static class WorkflowFactory
{
    /// <summary>
    /// 构建本示例的工作流：一个执行器把文本转大写，另一个把结果反转，两者由一条边顺序连接。
    /// </summary>
    /// <returns>已连边并声明了输出的工作流。</returns>
    public static Workflow Build()
    {
        // 第一个执行器：把 string→string 的函数绑定为执行器（另一种定义执行器的方式）。
        Func<string, string> uppercaseFunc = s => s.ToUpperInvariant();
        var uppercase = uppercaseFunc.BindAsExecutor("UppercaseExecutor");

        // 第二个执行器：执行器子类。
        ReverseTextExecutor reverse = new();

        // 用一条边把它们顺序连接，并声明反转执行器的输出为工作流的输出。
        WorkflowBuilder builder = new(uppercase);
        builder.AddEdge(uppercase, reverse).WithOutputFrom(reverse);
        return builder.Build();
    }
}
