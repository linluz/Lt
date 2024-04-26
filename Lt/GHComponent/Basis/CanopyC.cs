using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Lt.Base;
using Lt.Base.Component;
using Lt.Majas;
using Lt.Majas.MenuItemClass;
using Rhino.Geometry;

namespace Lt.GHComponent.Basis
{
    /// <summary>
    /// 林冠线
    /// Canopy Curve
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public class CanopyC : ADCComponent
    {
        //debug 待 及双击效果
        public CanopyC() : base(
            "林冠线", "CanopyC",
            "通过轮廓曲线来生成林冠线",
            "基础",
            ComponentID.CanopyC, 1, LTResource.林冠线)
        {
            Restrict = new MBooleanMenuItem(this, false, "轮廓限制(&B)", true, mf: m => m.Def ? "限制树心" : "限制树冠");
        }
        protected override void AddParameter(ParamManager pm)
        {
            pm.AddIP(ParT.Curve, "轮廓", "B", "拾取基础线框（树林边缘）");
            pm.AddIP(ParT.Number, "半径", "R", "每棵树的树冠半径");
            pm.AddIP(ParT.Number, "密度", "D", "每个树冠面积下所含的树量");
            pm.AddIP(ParT.Number, "剔除", "L", "小于此长度的碎线将被剔除");
            pm.AddIP(ParT.Integer, "种子", "S", "种树的随机种子");

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

            var TreeArea = r * r * Math.PI;
            Curve[] bs = { b };
            if (Restrict.Def)
            {
                b.TryGetPlane(out Plane plane);
                bs = b.Offset(plane, -r, DocumentTolerance(), CurveOffsetCornerStyle.Sharp)//偏移出树心限制线
                    .Where(t => t != null && t.IsValid).ToArray();//剔除为null或无效的值
            }

            var ba = bs.Select(t => new { V = t, A = Brep.CreatePlanarBreps(b)[0].GetArea() })
                .Where(t => t.A > TreeArea).ToArray();//剔除面积过小的

            var group = new GH_GeometryGroup();
            foreach (var ta in ba)
            {
                //树量=轮廓面积/(树冠面积/单冠密度)
                var count = (int)Math.Floor(ta.A / (TreeArea / d));
                b = ta.V;
                BoundingBox box = b.GetBoundingBox(true);
                int c0 = 0;
                var ran = new Random(s);
                List<Point3d> pl = new List<Point3d>(count);
                do
                {
                    var p = new Point3d(ran.NextNumber(box.Min.X, box.Max.X), ran.NextNumber(box.Min.Y, box.Max.Y), 0);
                    PointContainment pc = b.Contains(p);
                    if (pc == PointContainment.Unset || pc != PointContainment.Outside)
                        continue;
                    pl.Add(p);
                    c0++;
                } while (c0 == count);

                var ca = Curve.CreateBooleanUnion(pl.Select(t => new Circle(t, r).ToNurbsCurve()))
                    //转成圆组并求交集
                    .Where(t => t.GetLength() >= d)//剔除长度短于d
                    .Select(t => new GH_Curve(t)).ToArray();//转换成gh类型
                group.Objects.AddRange(ca);
            }

            DA.SetData(0, group);
        }
        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
            => Menu_Boolean(menu, ref Restrict, "未勾选时，轮廓线为树心范围，勾选后，为树冠范围");
        private static MBooleanMenuItem Restrict;
        public override MBooleanMenuItem DoubleClick => Restrict;
    }
}
