using System.ComponentModel;

namespace HelloAgent.Tools;

/// <summary>天气相关工具。新增工具时按领域新建一个 XxxTools 类即可。</summary>
public static class WeatherTools
{
    /// <summary>返回指定位置的天气（示例为只读的假数据）。</summary>
    [Description("获取指定位置的天气。")]
    public static string GetWeather(
        [Description("要查询天气的地点。")] string location)
        => $"{location} 的天气为多云，最高气温 15°C。";
}
