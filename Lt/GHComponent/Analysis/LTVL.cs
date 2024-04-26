using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel;
using Lt.Majas;
using Rhino.Display;
using Rhino.Geometry.Intersect;
using Rhino.Geometry;
using System.Windows.Forms;
using Lt.Base;
using Lt.Base.Component;
using Lt.Base.Extensions;

namespace Lt.GHComponent.Analysis
{
    /// <summary>
    /// 视线分析
    /// Terrain Grade
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public sealed class LTVL : AComponent
    {
        public LTVL() : base("视线分析", "LTVL",
            "分析在山地某处的可见范围,cpu线程数大于1时自动调用多核计算",
            "分析",
            ID.LTVL, 4, LTResource.视线分析)
        {
            ColorO = new MColorMenuItem(this, Color.Red, "观察点色彩(&C)");
            SizeO = new MDoubleMenuItem(this, 10, "观察点尺寸(&S)");
            ColorV = new MColorMenuItem(this, Color.FromArgb(0, 207, 182), "可见点色彩(&C)");
            SizeV = new MDoubleMenuItem(this, 4, "可见点尺寸(&S)");
            EyeHight = new MDoubleMenuItem(this, 1.5, "眼高（单位米）(&E)", true);
            Ov = new MBooleanMenuItem(this, true, "障碍物(&O)", mf: m => m.Def ? "显示障碍物" : "");
            Oc = new MColorMenuItem(this, Color.FromArgb(64, 0, 0, 0), "障碍物色彩");
            SolutionExpired += (s, e) => Mesh0.Clear();
            Mesh0 = new Update<Mesh>(() =>
            {
                Mesh0.Value = Const.Paral
                    ? GetOutByItem<GH_Point>(0).AsParallel() //获取可见点数据
                        .Aggregate(new Mesh(), SphereAppend)
                    : GetOutByItem<GH_Point>(0) //获取可见点数据
                        .Aggregate(new Mesh(), SphereAppend);
            });
        }
        protected override void AddParameter(ParamManager pm)
        {
            pm.AddIP(ParT.Mesh, "地形", "Mt", "要进行坡度分析的山地地形网格");
            pm.AddIP(ParT.Brep, "障碍物", "O", "（可选）阻挡视线的障碍物体，", ParamTrait.List | ParamTrait.Optional);
            pm.AddIP(ParT.Point, "观察点", "P", "观察者所在的点位置（可不在网格上），支持多点观察", ParamTrait.List);
            pm.AddIP(ParT.Integer, "精度", "A", "分析精度(单位：米)，即分析点阵内的间距");

            pm.AddOP(ParT.Point, "观察点", "O", "观察者视点位置", ParamTrait.List);
            pm.AddOP(ParT.Point, "可见点", "V", "被看见的点", ParamTrait.List);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (!DA.OutDataC(0, out Mesh tm) && !tm.IsValid
                || !DA.OutDataList(2, out List<Point3d> pl)
                || !DA.OutData(3, out int a))
                return;
            DA.OutDataList(1, out List<Brep> o);
            var om = o.Select(t =>
                Mesh.CreateFromBrep(t)
                    .Aggregate(new Mesh(), (c, t0) => c.AppendMesh(t0)))
                .ToArray();
            Point3d[] pt, grid;

            //制作网格上用于测量的点阵
            BoundingBox mb = tm.Vertices.Aggregate(new BoundingBox(), (s, i) => s.UnionPoint(i));

            int rx = (int)Math.Round((mb.Max.X - mb.Min.X) / a);
            int ry = (int)Math.Round((mb.Max.Y - mb.Min.Y) / a);
            //获取平面上点阵
            Point3d[] grid0 = new Point3d[rx * ry];
            for (int ix = 0; ix < rx; ix++)
                for (int iy = 0; iy < ry; iy++)
                    grid0[ix * ry + iy] = new Point3d(mb.Min.X + ix * a, mb.Min.Y + iy * a, mb.Min.Z);

            var eh = new Vector3d(0, 0, EyeHight.Def);//眼高向量
            if (Const.Paral)
            {
                pt = pl.AsParallel()
                    .Select(ProjectZ)//将观测点投影到地形网格上
                    .Where(t => t != Point3d.Unset)
                    .Select(t => t + eh)//增加眼高
                    .ToArray();

                //将栅格点投影到地形网格上
                grid = grid0.AsParallel()
                    .Select(ProjectZ)//投影到网格上
                    .Where(t => t != Point3d.Unset)//剔除不在网格上的点
                    .Where(t => pt.Any(t0 => Intersection.MeshLine(tm, new Line(t0, t), out _).Length == 1)) //获取无遮挡时能被观察到的点
                    .Where(t => pt.Any(t0 => om.All(t1 => OLineOverlap(t1, t0, t))))//剔除被障碍物遮挡
                    .ToArray();
            }
            else
            {
                pt = pl.Select(ProjectZ)//将观测点投影到地形网格上
                    .Where(t => t != Point3d.Unset)
                    .Select(t => t + eh)//增加眼高
                    .ToArray();

                //将栅格点投影到地形网格上
                grid = grid0.Select(ProjectZ)//投影到网格上
                    .Where(t => t != Point3d.Unset)//剔除不在网格上的点
                    .Where(t =>
                        pt.Any(t0 => Intersection.MeshLine(tm, new Line(t0, t), out _).Length == 1))//获取无遮挡时能被观察到的点
                    .Where(t => pt.Any(t0 => om.All(t1 => OLineOverlap(t1, t0, t)))) //剔除被障碍物遮挡
                    .ToArray();
            }

            DA.SetDataList(0, pt);
            DA.SetDataList(1, grid);
            return;

            //将点z向投影到网格上
            Point3d ProjectZ(Point3d t)
            {
                var r = new Ray3d(new Point3d(t.X, t.Y, mb.Min.Z), Vector3d.ZAxis); //转换射线
                double d = Intersection.MeshRay(tm, r); //求交点参数
                return d < 0 ? Point3d.Unset : r.PointAt(d); //返回点
            }
        }
        /// <summary>
        /// 障碍物与直线的重叠情况
        /// </summary>
        /// <param name="o">障碍物</param>
        /// <param name="s">直线起点</param>
        /// <param name="t">直线终点</param>
        /// <returns>true为重叠，否则不重叠，（重叠为点则不重叠）</returns>
        private static bool OLineOverlap(Mesh o, Point3d s, Point3d t)
        {
            var ml = Intersection.MeshLine(o, new Line(s, t), out _);
            switch (ml.Length)
            {
                case 0: return true;
                case 1: return ml[0] == t;
                default: return false;
            }
        }
        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            Menu_Color(menu, ref ColorO);
            Menu_Double(menu, ref SizeO, icon: LTResource.PointStyle_20x20);
            Menu_Color(menu, ref ColorV);
            Menu_Double(menu, ref SizeV, icon: LTResource.PointStyle_20x20);
            Menu_Double(menu, ref EyeHight, "人眼高度", icon: LTResource.EyeHight_20x20);
            Menu_Boolean(menu, ref Ov, "控制是否显示障碍物");
            Menu_BColor(Ov, ref Oc);
        }
        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            if (Hidden || !IsPreviewCapable || Locked) return; //电池隐藏或不可预览时跳过

            if (args.Document.PreviewMode != GH_PreviewMode.Shaded)
            {
                args.Viewport.GetFrustumNearPlane(out Plane worldXY);

                GetOutByItem<GH_Point>(0).ForEach(t =>
                    args.Display.DrawCircle(new Circle(worldXY, t.Value, SizeO.Def), ColorO.Def, args.DefaultCurveThickness));

                Color ccc = Attributes.Selected ? args.WireColour_Selected : ColorV.Def;
                if (Const.Paral)
                {
                    Parallel.ForEach(GetOutByItem<GH_Point>(1), t =>
                        args.Display.DrawCircle(new Circle(worldXY, t.Value, SizeV.Def), ccc, args.DefaultCurveThickness));
                }
                else
                    GetOutByItem<GH_Point>(1).ForEach(t =>
                        args.Display.DrawCircle(new Circle(worldXY, t.Value, SizeV.Def), ccc, args.DefaultCurveThickness));
            }
            if (!Ov.Def) return;
            //绘制障碍物
            GH_PreviewWireArgs pwa = ToPreviewWireArgs(args, Color.FromArgb(Oc.Def.R, Oc.Def.B, Oc.Def.G));

            GetIntByItem<GH_Brep>(1).ForEach(t => t.DrawViewportWires(pwa));
        }

        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            if (Hidden || !IsPreviewCapable || Locked) return;
            if (Mesh0.Value.VertexColors.Count == 0 || Mesh0.Value.VertexColors[0] != ColorO.Def)
                Mesh0.Value.VertexColors.CreateMonotoneMesh(ColorO.Def);
            args.Display.DrawMeshFalseColors(Mesh0.Value);//绘制可见点

            if (Ov.Def)//绘制障碍物
            {
                GH_PreviewMeshArgs pma = ToPreviewMeshArgs(args, Oc.Def, false);
                GetIntByItem<GH_Brep>(1).ForEach(t => t.DrawViewportMeshes(pma));
            }

            var displayBitmapDrawList = new DisplayBitmapDrawList
            {
                MaximumCachedSortLists = 200
            };

            displayBitmapDrawList.SetPoints(GetOutByItem<GH_Point>(1).Select(t => t.Value), Attributes.Selected ? args.WireColour_Selected : ColorV.Def);
            args.Display.DrawSprites(new DisplayBitmap(LTResource.FuzzySprite_64x64), displayBitmapDrawList,
                Convert.ToSingle(SizeV.Def), true);
        }

        public Update<Mesh> Mesh0;

        /// <summary>
        /// 可见点转网格球并附加到一起
        /// </summary>
        /// <param name="m">要被附加的网格</param>
        /// <param name="p">点</param>
        /// <returns>已附加好的网格</returns>
        private Mesh SphereAppend(Mesh m, GH_Point p)
            => m.AppendMesh(Mesh.CreateFromSphere(new Sphere(p.Value, SizeO.Def), 60, 30));

        /// <summary>
        /// 观察点色彩
        /// </summary>
        private static MColorMenuItem ColorO;
        private static MDoubleMenuItem SizeO;
        /// <summary>
        /// 可见点色彩
        /// </summary>
        private static MColorMenuItem ColorV;
        private static MDoubleMenuItem SizeV;

        private static MDoubleMenuItem EyeHight;
        /// <summary>
        /// 是否显示障碍物
        /// </summary>
        private static MBooleanMenuItem Ov;
        /// <summary>
        /// 障碍物色彩
        /// </summary>
        private static MColorMenuItem Oc;
    }
}
