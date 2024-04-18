using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Lt.Majas;


namespace Lt.Basis
{
    /// <summary>
    /// 云线
    /// Revcloud
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public class RCloud : AComponent
    {
        public RCloud() : base(
            "云线", "RCloud",
            "拾取基础线框自动生成云线,可一次拾取多根线框",
             "基础",
            ID.RCloud, 1, LTResource.云线) { }
        //AddIntegerParameter 和 item 都成变量

        protected override void AddParameter(ParamManager pm)
        {
            pm.AddIP(ParT.Curve, "基础线框", "B", "云线的基础线框,每个框线必须平面闭合");
            pm.AddIP(ParT.Number, "最小弧长", "Amin", "云线圆弧的最小长度,不能小于0.5，且不能大于线框的1/3");
            pm.AddIP(ParT.Number, "最大弧长", "Amax", "(可选)云线圆弧的最大长度，不输入则为固定弧长", ParamTrait.Item | ParamTrait.Optional);
            pm.AddIP(ParT.Boolean, "翻转内外", "R", "翻转云线的内外朝向，默认向外\r\ntrue为向外，false为向内", def: true);
            pm.AddIP(ParT.Integer, "随机种子", "S", "圆弧随机分布情况，默认为255，仅最大弧长有输入时有效", def: 225);

            pm.AddOP(ParT.Curve, "云线", "C", "生成的云线");
            pm.AddOP(ParT.Interval, "弧长区间", "I", "实际生成的弧长范围区间");
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            #region 初始化 获取输入
            Curve b = new PolyCurve();
            double max = 0;
            double min = 0;
            bool r = false;
            int s = 0;
            if (!DA.GetData(0, ref b) //获取线框，并检测闭合与平面
                || RMNoValid(b, 0)
                || RMNoClosed(b, 0)
                || RNNoPlanar(b, 0)
                || !DA.GetData(1, ref min)
                || RMSmaller(min, 0.5, 1, equal: true) //获取最小值，检测是否大于0.5
                || RMLarger(min * 3, b.GetLength(), 1, 0, tb: NumT.Length) //检测3倍最小值是否大于线长
                || !DA.GetData(3, ref r))
                return;
            var bmax = DA.GetData(2, ref max);
            if (bmax && RMSmaller(max, min, 2, 1) //能获取的时候检测最大值、最小值的关系，不对就报错不输出
                     && !DA.GetData(4, ref s)) //无法获取种子
                return;
            #endregion

            const double chordR = 1.2740056;//弧长转弦长系数（1/3）：arcsin(12/13)*13/12
            var rmax = max / chordR;//最小弧长转最小弦长
            var rmin = min / chordR;//最大弧长转最大弦长
            var l = b.GetLength();//线框长度

            double[] ra;
            int c;
            if (bmax)
            {
                c = (int)Math.Floor(l * 2 / (rmax + rmin));//除以r中值后 最接近的小的数量
                var r1 = l / c;//实际的平均值
                var c1 = c / 2;
                ra = new double[c]; //加头
                var c2 = c1;//与下一半的间隔
                var ran = new Random(s);
                if (c % 2 == 1)
                {
                    ra[c1] = r1; //给中间加值
                    c2++;//奇数时间隔加1
                }//若奇数，则给中间赋值，并调整第二段的起始位置

                for (int i = 0; i < c1; i++)
                {
                    double d0 = ran.NextNumber(rmin, rmax);
                    ra[i] = r1 + d0;
                    ra[i + c2] = r1 - d0;
                }//生成两段随机
            }
            else
            {
                c = (int)Math.Floor(l / rmin);
                ra = Enumerable.Repeat(l / c, c).ToArray();
            }
            for (int i = 1; i < c - 1; i++)
                ra[i] += ra[i - 1];//将自身长度变成叠加长度
            ra = new double[] { 0 }.Concat(ra).ToArray();//加头
            ra[c] = 0; //改尾

            b.TryGetPlane(out Plane plane);//获取所在平面
            var za = plane.ZAxis;//获取平面Z向
            const double angle = -Math.PI / 2;//旋转角度
            var arca = new Polyline(ra.Select(t => b.PointAtLength(t)))
                //按长度获取点，并转多段线
                .GetSegments()//获取全部线段
                .Select(t =>
                {
                    var v = t.Direction / 3;//获取1/3长度的向量
                    v.Rotate(r ? angle : -angle, za);//旋转方向,r真则反方向
                    return new Arc(t.From, (t.To + t.From) / 2 + v, t.To).ToNurbsCurve();
                }).ToArray();

            DA.SetData(0, Curve.JoinCurves(arca)[0]);
            DA.SetData(1, arca.Select(t => t.GetLength()).ToInterval());
        }
    }
    /// <summary>
    /// 林冠线
    /// Canopy Curve
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public class CanopyC : ADCComponent
    {//debug 待 及双击效果
        public CanopyC() : base(
            "林冠线", "CanopyC",
            "通过轮廓曲线来生成林冠线",
            "基础",
            ID.CanopyC, 1, LTResource.林冠线)
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
            Curve b = new PolyCurve();
            double r = 0;
            double d = 0;
            double l = 0;
            int s = 0;
            if (!DA.GetData(0, ref b) || RMNoValid(b, 0) || RMNoClosed(b, 0) || RNNoPlanar(b, 0) || //获取线框，并检测闭合与平面
                !DA.GetData(1, ref r) || RMSmaller(r, 0, 1, equal: true) ||
                !DA.GetData(2, ref d) || RMSmaller(d, 0, 2, equal: true) ||
                !DA.GetData(3, ref l) || RMSmaller(l, 0, 3, equal: true) ||
                !DA.GetData(4, ref s)) return;
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

            GH_GeometryGroup group = new GH_GeometryGroup();
            foreach (var ta in ba)
            {
                //树量=轮廓面积/(树冠面积/单冠密度)
                var count = (int)Math.Floor(ta.A / (TreeArea / d));
                b = ta.V;
                BoundingBox box = b.GetBoundingBox(true);
                int c0 = 0;
                Random ran = new Random(s);
                List<Point3d> pl = new List<Point3d>(count);
                do
                {
                    Point3d p = new Point3d(ran.NextNumber(box.Min.X, box.Max.X), ran.NextNumber(box.Min.Y, box.Max.Y), 0);
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