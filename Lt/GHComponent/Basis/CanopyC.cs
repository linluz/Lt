using System;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Majas.Utility.Population;
using Rhino.Geometry;

namespace Lt.GHComponent.Basis;

/// <summary>
/// 林冠线
/// Canopy Curve
/// </summary>
// ReSharper disable once UnusedMember.Global
public class CanopyC : ADCComponent
{
    //debug 待 及双击效果
    public CanopyC() : base(
        "林冠线", "LTCanopyC",
        "通过轮廓曲线来生成林冠线",
        "基础",
        ComponentID.LTCanopyC, 1, LTResource.林冠线)
    {
        Restrict = new MBooleanMenuItem(this, false, "轮廓限制(&B)", true, mf: m => m.Def ? "限制树心" : "限制树冠");
    }
    protected override void AddParameter(ParamManager pm)
    {
        pm.AddIP(ParT.Curve, "轮廓", "B", "拾取基础线框（树林边缘）");
        pm.AddIP(ParT.Number, "半径", "R", "每棵树的树冠半径");
        pm.AddIP(ParT.Number, "密度", "D", "每棵树的占地密度");
        pm.AddIP(ParT.Number, "剔除", "L", "小于此长度的碎线将被剔除");
        pm.AddIP(ParT.Integer, "种子", "S", "种树的随机种子,默认653", def: 653);

        pm.AddOP(ParT.Group, "林冠线", "C", "生成的成组的林冠线");
    }
    protected override void SolveInstance(IGH_DataAccess DA)
    {
        #region 初始化 获取输入
        if (!DA.OutData(0, out Curve b, new PolyCurve()) || RMNoValid(b, 0) || RMNoClosed(b, 0) || RNNoPlanar(b, 0) || //获取线框，并检测闭合与平面
            !DA.OutData(1, out double r) || RMSmaller(r, 0, 1, equal: true) ||
            !DA.OutData(2, out double d) || RMSmaller(d, 0, 2, equal: true) ||
            !DA.OutData(3, out double l) || RMSmaller(l, 0, 3, equal: true) ||
            !DA.OutData(4, out int s)) return;
        #endregion
        //bug 代码已正确 但运行效率是是原电池的7倍，待查
        var TreeArea = r * r * Math.PI;//树冠面积
        var TreeArea2 = TreeArea * d;//每颗树所占的树冠面积
        Curve[] bs = [b];
        if (Restrict.Def)
        {
            b.TryGetPlane(out Plane plane);
            bs = b.Offset(plane, -r, DocumentTolerance(), CurveOffsetCornerStyle.Sharp) //偏移出树心限制线
                .Where(t => t is { IsValid: true, IsClosed: true })//剔除为null或无效或不闭合的线
                .ToArray();
        }
        var group = new GH_GeometryGroup();
        var ra = bs.Where(c => c.GetLength() > l)//剔除过短的线
            .Select(c =>
            {
                Brep sur = Brep.CreatePlanarBreps(c)[0];
                var count = (int)(sur.GetArea() / TreeArea2);//树量=面积/(树冠面积/单冠密度)
                return new BrepPopulation(sur, s).Populate(count, null)//获取随机点
                    .Where(p => b.Contains(p) is PointContainment.Coincident or PointContainment.Inside) //保留内部或在边界上的点
                    .Select(p => new Circle(p, r).ToNurbsCurve()) //转换为圆
                    .ToArray();
            })
            .Select(t =>
                Curve.CreateBooleanUnion(t) //转成圆组并求交集
                    .Where(t0 => t0.GetLength() >= l)//剔除长度短于l
                    .Select(t0 => new GH_Curve(t0))
            )
            .SelectMany(t => t)
            .ToArray();

        group.Objects.AddRange(ra);

        DA.SetData(0, group);
    }
    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        => Menu_Boolean(menu, ref Restrict, "未勾选时，轮廓线为树心范围，勾选后，为树冠范围");
    private static MBooleanMenuItem Restrict;
    public override MBooleanMenuItem DoubleClick => Restrict;
}