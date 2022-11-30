using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using Lt.Majas;
using Grasshopper.GUI.Gradient;
using Rhino.Display;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Parameters;


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
            if (!DA.GetData(0, ref b) ||
                RMNoValid(b, 0) || RMNoClosed(b, 0) || RNNoPlanar(b, 0) || //获取线框，并检测闭合与平面
                !DA.GetData(1, ref min) || RMSmaller(min, 0.5, 1, equal: true) || //获取最小值，检测是否大于0.5
                RMLarger(min * 3, b.GetLength(), 1, 0, tb: NumT.Length) || //检测3倍最小值是否大于线长
                !DA.GetData(3, ref r))
                return;
            var bmax = DA.GetData(2, ref max);
            if (bmax && RMSmaller(max, min, 2, 1) //能获取的时候检测最大值、最小值的关系，不对就报错不输出
                     && !DA.GetData(4, ref s)) //无法获取种子
                return;
            #endregion

            const double chordR = 1.2740056;//弧长转弦长系数（1/3）：arcsin(12/13)*13/12
            var rmax = (max / chordR);//最小弧长转最小弦长
            var rmin = (min / chordR);//最大弧长转最大弦长
            var l = b.GetLength();//线框长度
           
            double[] ra;
            int c;
            if (bmax)
            {
                c = (int)Math.Floor(l *2/ (rmax + rmin));//除以r中值后 最接近的小的数量
                var r1 = l / c;//实际的平均值
                var c1 = c / 2;
                var rx = rmax - rmin;//差值
                ra = new double[c] ; //加头
                var c2 = c1;//与下一半的间隔
                Random ran = new Random(s);
                if (c % 2 == 1) 
                {
                    ra[c1] = r1; //给中间加值
                    c2++;//奇数时间隔加1
                }//若奇数，则给中间赋值，并调整第二段的起始位置

                for (int i = 0; i < c1; i++)
                {
                    double d0=(ran.NextDouble()-0.5) *rx;
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

}