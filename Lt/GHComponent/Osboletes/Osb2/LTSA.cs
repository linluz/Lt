using System.Windows.Forms;
using Lt.GHComponent.Analysis;

namespace Lt.GHComponent.Osboletes.Osb2;

/// <summary>
/// 过时组件：山路坡度分析
/// </summary>
// ReSharper disable once UnusedMember.Global
public sealed class LTSA : GradientComponent_Osb
{
    public LTSA() : base("山路坡度分析", "LTSA",
        "分析",
        ComponentID.LTSA, nameof(LTRA), LTResource.山路坡度分析)
    {
        Gra.Def = Const.Gradient0.Duplicate();
        Gra.ReCom = true;
        GI = new MBooleanMenuItem(this, true, "自适应角度(&A)",rw:false);
    }
    protected override void AddParameter(ParamManager pm)
    {
        pm.AddIP(ParT.Curve, "山路", "C", "要分析的山路中线（确保已投影在地形上）", ParamTrait.List);
        pm.AddIP(ParT.Integer, "精度", "E", "山路中线的细分重建密度(单位米)");

        pm.AddOP(ParT.Line, "路线", "L", "重建后用于分析的直线路线", ParamTrait.List);
        pm.AddOP(ParT.Angle, "坡度", "A", "对应直线段的坡度(度)", ParamTrait.List);
        pm.AddOP(ParT.Text, "坡度范围", "Rs", "山路直线的坡度范围（既坡高/坡长）");
        pm.AddOP(ParT.Interval, "角度范围", "Ra", "山路直线与水平面所呈角度的范围");
    }

    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
        base.AppendAdditionalComponentMenuItems(menu);
        Menu_Boolean(menu, ref GI, "默认不启用，此时渐变色彩范围对应0-90º。\r\n启用时，范围对应实际的角度范围",
            click: (_, _) => Message = GI.Def ? "自适应" : "0-90º");
    }

    private MBooleanMenuItem GI;
}