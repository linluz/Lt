using System.Drawing;
using Lt.GHComponent.Analysis;

namespace Lt.GHComponent.Osboletes.Osb1;

/// <summary>
/// 过时组件：地形网格淹没分析
/// </summary>
// ReSharper disable once UnusedMember.Global
public sealed class LTFT()
    : AOComponent("地形网格淹没分析", "LTFT", "分析", ComponentID.LTMF_Osb, nameof(LTMF), Resources.山体淹没分析)
{
    protected override void AddParameter(ParamManager pm)
    {
        pm.AddIP(ParT.Mesh, "地形", "Mt", "要被淹没的山地地形网格");
        pm.AddIP(ParT.Number, "高度", "E", "淹没地形的水平面高度");
        pm.AddIP(ParT.Colour, "色彩", "C", "被水淹没区域的色彩", def: Color.FromArgb(52, 58, 107));

        pm.AddOP(ParT.Mesh, "地形", "Mf", "被水淹没后的地形网格");
    }
}