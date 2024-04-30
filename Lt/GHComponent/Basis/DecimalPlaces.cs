using System;
using Grasshopper.Kernel;

namespace Lt.GHComponent.Basis;
//debug 待测试效果
/// <summary>
/// 小数位数
/// </summary>
// ReSharper disable once UnusedMember.Global
public class DecimalPlaces() : AComponent("小数位数", "LTDecimalP",
    "控制保留的小数位数",
    "基础",
    ComponentID.LTDecimalP, 1, LTResource.小数点位数)
{
    protected override void AddParameter(ParamManager pm)
    {
        pm.AddIP(ParT.Number, "数值", "N", "要处理的数值");
        pm.AddIP(ParT.Integer, "位数", "D", "要保留的小数位数");

        pm.AddOP(ParT.Number, "数值", "N", "保留小数位数后的值");
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        if (
            !DA.OutData(0, out double n0)
            || n0 < 0
            || !DA.OutData(1, out int d)
        ) return;

        DA.SetData(0, Math.Round(n0, d));
    }
}