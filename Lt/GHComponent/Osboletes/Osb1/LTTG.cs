using Lt.GHComponent.Analysis;

namespace Lt.GHComponent.Osboletes.Osb1;

/// <summary>
/// 过时组件：坡度分析
/// </summary>
// ReSharper disable once UnusedMember.Global
public sealed class LTTG()
    : AOComponent("坡度分析", "LTTG", "分析", ComponentID.LTMG_Osb, nameof(LTMG), LTResource.山体坡度分析)
{
    protected override void AddParameter(ParamManager pm)
    {
        pm.AddIP(ParT.Mesh, "地形", "Mt", "要进行坡度分析的山地地形网格");

        pm.AddOP(ParT.Mesh, "地形", "M", "已按角度着色的地形网格");
        pm.AddOP(ParT.Interval, "角度", "A", "坡度范围（度）");
    }
}