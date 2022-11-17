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

namespace Lt.Analysis
{
    //after 给渐变的电池 加色彩标尺
    /// <summary>
    /// 网格淹没分析
    /// Flooded Terrain
    /// </summary>
    public sealed class LTMF : GradientComponent
    {
        public LTMF() : base(
            "淹没分析(网格)", "LTMF",
            "分析被水淹没后的地形状态",
            "分析",
            ID.LTMF, 1, LTResource.山体淹没分析)
        {
            DownColor = new MColorMenuItem(this, Color.FromArgb(52, 58, 107), "淹没色彩(&F)", true);
            Gra.Def = new GH_Gradient(
            new[] { 0, 0.16, 0.33, 0.5, 0.67, 0.84, 1 },
            new[]
            {
                Color.FromArgb(45, 51, 87),
                Color.FromArgb(75, 107, 169),
                Color.FromArgb(173, 203, 249),
                Color.FromArgb(254, 244, 84),
                Color.FromArgb(234, 126, 0),
                Color.FromArgb(219, 37, 0),
                Color.FromArgb(138, 36, 36)
            });
        }

        protected override void AddParameter(ParamManager pm)
        {
            pm.AddIP(ParT.Mesh, "地形", "M", "要被淹没的山地地形网格");
            pm.AddIP(ParT.Number, "高度", "E", "淹没地形的水平面高度");
            pm.AddIP(ParT.Boolean, "摊平", "F", "是否要将水下等高线摊平到水平面，默认为false", def: true);

            pm.AddOP(ParT.Mesh, "地形", "M", "被水淹没后的地形网格");
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            #region 初始化 获取输入
            Mesh t0 = new Mesh();
            double e0 = 0;
            bool f = false;
            if (!DA.GetData(0, ref t0)
                || !DA.GetData(1, ref e0)
                || !DA.GetData(2, ref f))
                return;
            #endregion

            BoundingBox b = t0.GetBoundingBox(false);
            Interval ie = new Interval(b.Min.Z, b.Max.Z); //获取高度范围
            t0.VertexColors.Clear();
            for (var i = 0; i < t0.Vertices.Count; i++)
            {
                Point3f p = t0.Vertices[i];
                double z = p.Z;
                var hb = z > e0;
                t0.Vertices[i] = !f || hb ? p : new Point3f(p.X, p.Y, Convert.ToSingle(e0));
                t0.VertexColors.Add(hb ? Gra.Def.ColourAt(ie.NormalizedParameterAt(z)) : DownColor.Def);
            }

            DA.SetData(0, t0);
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
            => Menu_Color(menu, ref DownColor);

        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            if (args.Document.PreviewMode != GH_PreviewMode.Shaded || Hidden || !args.Display.SupportsShading)
                return; ///跳过非着色模式和，或参数不支持预览
            Ty.Draw1Meshes(0, this, args);
        }
        /// <summary>
        /// 水下色彩
        /// </summary>
        private MColorMenuItem DownColor;
    }
    /// <summary>
    /// 网格坡向分析
    /// Slope Direction Analysis
    /// </summary>
    public sealed class LTMD : ADCComponent
    {
        public LTMD()
            : base("坡向分析(网格)", "LTMD",
                "山坡地形朝向分析,X轴向为东,Y轴向为北" +
                "\r\n双击：切换着色模式," +
                "\r\n注意：顶点着色会焊接网格顶点，可能导致顶点减少，" +
                "\r\n面着色会把面的每个顶点都解离出来，可能会导致顶点增加",
                "分析",
                ID.LTMD, 1, LTResource.山体坡向分析)
        {
            Shade = new MBooleanMenuItem(this, true, "使用面着色(&F)", true,
                mf: m => m.Def ? "面着色" : "顶点着色");
        }
        protected override void AddParameter(ParamManager pm)
        {
            pm.AddIP(ParT.Mesh, "地形", "M", "要进行坡向分析的山地地形网格");
            pm.AddIP(ParT.Colour, "色彩", "C",
                "各向的色彩，请按上、北、东北、东、东南、南、西南、西、西北的顺序连入9个色彩", ParamTrait.List,
                new[]
                {
                    Color.FromArgb(219, 219, 219),
                    Color.FromArgb(232, 77, 77),
                    Color.FromArgb(230, 168, 55),
                    Color.FromArgb(227, 227, 59),
                    Color.FromArgb(49, 222, 49),
                    Color.FromArgb(39, 219, 189),
                    Color.FromArgb(51, 162, 222),
                    Color.FromArgb(48, 48, 217),
                    Color.FromArgb(217, 46, 217)
                });

            pm.AddOP(ParT.Mesh, "网格", "M", "已根据坡向着色的地形网格");
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            #region 初始化 获取输入
            Mesh m = new Mesh();
            List<Color> c = new List<Color>(9);
            if (!DA.GetData(0, ref m)
                || !DA.GetDataList(1, c))
                return;
            if (c.Count != 9)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "输入色彩数量不等于9！不足已补充为最后一项,多出已忽视。");
                for (int i = c.Count; i < 9; i++)
                    c.Add(c[i - 1]);
            }
            #endregion
            if (Shade.Def)
            {
                //数量不对时计算面法向
                if (m.FaceNormals.Count != m.Faces.Count)
                    m.FaceNormals.ComputeFaceNormals();
                //计算色彩
                var cl = m.FaceNormals.Select(t => c[DShade(t)]).ToArray();
                //获取顶点列表
                Point3f[] vl = m.Vertices.ToArray();
                var fl = m.Faces.ToArray();
                var nl = m.FaceNormals.ToArray();
                //清理顶点及顶点色彩列表
                m.VertexColors.Clear();
                m.Vertices.Clear();
                m.Faces.Clear();
                m.Normals.Clear();
                for (var i = 0; i < fl.Length; i++)
                {
                    var tri = fl[i].IsTriangle;
                    for (int j = 0; j < 3; j++)
                    {
                        //添加顶点,并修改对应面索引
                        fl[i][j] = m.Vertices.Add(vl[fl[i][j]]);
                        //添加色彩
                        m.VertexColors.Add(cl[i]);
                        //添加法向
                        m.Normals.Add(nl[i]);
                    }

                    if (tri)
                        fl[i][3] = fl[i][2];
                    else
                    {
                        fl[i][3] = m.Vertices.Add(vl[fl[i][3]]);
                        m.VertexColors.Add(cl[i]);
                        m.Normals.Add(nl[i]);
                    }
                }
                m.Faces.AddFaces(fl);
            }
            else
            {
                m.VertexColors.Clear();//清理旧色彩
                m.Weld(DocumentAngleTolerance());//焊接顶点
                m.Normals.ComputeNormals();//计算顶点法向
                //根据顶点法向计算色彩并添加
                foreach (Vector3f t in m.Normals)
                    m.VertexColors.Add(c[DShade(t)]);
            }

            DA.SetData(0, m);
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
            => Menu_Boolean(menu, ref Shade);

        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            if (args.Document.PreviewMode != GH_PreviewMode.Shaded || Hidden || !args.Display.SupportsShading)
                return; ///跳过非着色模式和，或参数不支持预览
            Ty.Draw1Meshes(0, this, args);
        }

        public int DShade(Vector3f v)
        {
            if (v.X == 0 && v.Y == 0) //上方
                return 0;
            double t = Math.Abs(v.Y / v.X);
            bool x = v.X > 0;
            bool y = v.Y > 0;
            if (t < Num1) //东西
                return x ? 3 : 7;
            if (t > Num2) //北南
                return y ? 1 : 5;
            if (x) //东北、东南
                return y ? 2 : 4;
            return y ? 8 : 6;
            //西北、西南
        }

        private static readonly double Num1 = Math.Tan(Math.PI / 8);
        private static readonly double Num2 = Math.Tan(Math.PI * 3 / 8);

        private MBooleanMenuItem Shade;
        public override MBooleanMenuItem DoubleClick => Shade;
    }
    /// <summary>
    /// 网格坡度分析
    /// Terrain Mesh Grade Analysis
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public sealed class LTMG : GradientComponent, IMCom_DoubleClick
    {
        public LTMG() : base("坡度分析(网格)", "LTMG",
            "山地地形坡度分析,\r\n双击：【坡度范围】切换为角度/弧度",
            "分析",
            ID.LTMG, 1, LTResource.山体坡度分析)
        {
            Gra.Def = Ty.Gradient0.Duplicate();
            Gra.ReCom = true;
            GI = new MBooleanMenuItem(this, true, "自适应范围(&A)", true, mf: m => m.Def ? "自适应" : "0-90°");
            UD = new MBooleanMenuItem(this, true, "使用角度(&U)", true, mf: m => m.Def ? "角度" : "弧度");
        }
        protected override void AddParameter(ParamManager pm)
        {
            pm.AddIP(ParT.Mesh, "地形", "M", "要进行坡度分析的山地地形网格");

            pm.AddOP(ParT.Mesh, "地形", "M", "已按角度着色的地形网格");
            pm.AddOP(ParT.Interval, "角度", "A", "坡度范围（度）");
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh tm = new Mesh();
            if (!DA.GetData(0, ref tm))
                return;
            if (tm.Normals.Count != tm.Vertices.Count)
                tm.Normals.ComputeNormals();//计算法向
            tm.VertexColors.Clear();//清除色彩

            double d = UD.Def ? Majas_Ex.R2A : 1;//输出弧度还是角度
            var ra = tm.Normals.Select(t => Math.Round(Math.Acos(t.Z) * d, 2)).ToArray();//法向转角度,保留两位小数


            Interval ia = ra.ToInterval();//获取角度范围
            Interval ib = GI.Def ? ia : Ty.A0(UD.Def);//着色范围
            //角度转换为色彩并给予网格
            tm.VertexColors.AppendColors(ra.Select(t => Dou2Col(ib, t)).ToArray());
            DA.SetData(0, tm);
            DA.SetData(1, ia);
        }
        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_Boolean(menu, ref GI, "默认启用，此时渐变色彩范围对应实际的角度范围。\r\n不启用时，范围对应0-90º");
            Menu_Boolean(menu, ref UD, "勾选时,【角度】输出端输出度,否则为弧度", LTResource.Degrees_16,
                (s, e) => UD.ReplacePDesc(true, "（弧度）", "（度）", 1));
        }

        public override void CreateAttributes()
            => m_attributes = new DoubleClick_Attributes(this);
        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            if (args.Document.PreviewMode != GH_PreviewMode.Shaded || Hidden || !args.Display.SupportsShading)
                return; ///跳过非着色模式和，或参数不支持预览
            Ty.Draw1Meshes(0, this, args);
        }

        private MBooleanMenuItem GI;
        private MBooleanMenuItem UD;
        public MBooleanMenuItem DoubleClick => UD;
    }
    /// <summary>
    /// 网格高程分析
    /// Terrain Mesh Elevation Analysis
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public sealed class LTME : GradientComponent
    {
        public LTME() : base("高程分析(网格)", "LTME",
            "山地地形高程分析",
            "分析",
            ID.LTME, 1, icon: LTResource.山体高程分析)
        {
            Gra.Def = Ty.Gradient0.Duplicate();
            Gra.ReCom = true;
        }
        protected override void AddParameter(ParamManager pm)
        {
            pm.AddIP(ParT.Mesh, "地形", "M", "要进行坡度分析的山地地形网格");

            pm.AddOP(ParT.Mesh, "地形", "M", "已按海拔着色的地形网格");
            pm.AddOP(ParT.Interval, "海拔", "E", "海拔范围（两位小数）");
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh tm = new Mesh();
            if (!DA.GetData(0, ref tm))
                return;
            BoundingBox b = tm.GetBoundingBox(false);
            Interval ie = new Interval(b.Min.Z, b.Max.Z);
            Mesh cm = tm.DuplicateMesh();
            foreach (Point3f p in tm.Vertices)
                cm.VertexColors.Add(Dou2Col(ie, p.Z));
            ie.T0 = Math.Round(ie.T0, 2);
            ie.T1 = Math.Round(ie.T1, 2);
            DA.SetData(0, cm);
            DA.SetData(1, ie);
        }
        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            if (args.Document.PreviewMode != GH_PreviewMode.Shaded || Hidden || !args.Display.SupportsShading)
                return; ///跳过非着色模式和，或参数不支持预览
            Ty.Draw1Meshes(0, this, args);
        }
    }
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
                if (Ty.Paral)
                    Mesh0.Value = GetOutByItem<GH_Point>(0).AsParallel() //获取可见点数据
                        .Aggregate(new Mesh(), SphereAppend);
                else
                    Mesh0.Value = GetOutByItem<GH_Point>(0) //获取可见点数据
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

            Mesh tm = new Mesh();
            List<Brep> o = new List<Brep>(3);
            List<Point3d> pl = new List<Point3d>();
            int a = 0;
            if (!DA.GetData(0, ref tm) && !tm.IsValid
                || !DA.GetDataList(2, pl)
                || !DA.GetData(3, ref a))
                return;
            DA.GetDataList(1, o);
            var om = o.Select
            (t =>
                Mesh.CreateFromBrep(t).Aggregate(new Mesh(), (c, t0) => c.AppendMesh(t0))
            ).ToArray();
            Point3d[] pt, grid;

            //制作网格上用于测量的点阵
            BoundingBox mb = new BoundingBox();
            foreach (Point3f p3f in tm.Vertices)
                mb.Union(new Point3d(p3f));

            double px = mb.Min.X;
            double py = mb.Min.Y;
            int rx = (int)Math.Round((mb.Max.X - px) / a);
            int ry = (int)Math.Round((mb.Max.Y - py) / a);
            //获取平面上点阵
            Point3d[] grid0 = new Point3d[rx * ry];
            for (int ix = 0; ix < rx; ix++)
                for (int iy = 0; iy < ry; iy++)
                    grid0[ix * ry + iy] = new Point3d(mb.Min.X + ix * a, mb.Min.Y + iy * a, mb.Min.Z);
            //将点z向投影到网格上
            Point3d ProjectZ(Point3d t)
            {
                Ray3d r = new Ray3d(new Point3d(t.X, t.Y, mb.Min.Z), Vector3d.ZAxis); //转换射线
                double d = Intersection.MeshRay(tm, r); //求交点参数
                return d < 0 ? Point3d.Unset : r.PointAt(d); //返回点
            }

            if (Ty.Paral)
            {
                //将栅格点投影到地形网格上
                var g0 = grid0.AsParallel().Select(ProjectZ).Where(t => t != Point3d.Unset).AsParallel();//剔除不在网格上的点

                //将观测点投影到地形网格上，并增加眼高
                pt = pl.AsParallel().Select(ProjectZ).Where(t => t != Point3d.Unset).ToArray();
                for (int i = 0; i < pt.LongLength; i++)
                    pt[i].Z += EyeHight.Def;//增加眼高

                //获取无遮挡时能被观察到的点
                g0 = g0.Where(t =>
                    pt.Any(t0 => Intersection.MeshLine(tm, new Line(t0, t), out _).Length == 1));
                //剔除被障碍物遮挡
                grid = g0.Where(t =>
                    pt.Any(t0 => om.All(t1 => OLineOverlap(t1, t0, t)))
                ).ToArray();
            }
            else
            {
                //将栅格点投影到地形网格上
                var g0 = grid0.Select(ProjectZ).Where(t => t != Point3d.Unset);//剔除不在网格上的点

                //将观测点投影到地形网格上，并增加眼高
                pt = pl.Select(ProjectZ).Where(t => t != Point3d.Unset).ToArray();
                for (int i = 0; i < pt.LongLength; i++)
                    pt[i].Z += EyeHight.Def;//增加眼高

                //获取无遮挡时能被观察到的点
                g0 = g0.Where(t =>
                    pt.Any(t0 => Intersection.MeshLine(tm, new Line(t0, t), out _).Length == 1));
                //剔除被障碍物遮挡
                grid = g0.Where(t =>
                    pt.Any(t0 => om.All(t1 => OLineOverlap(t1, t0, t)))
                ).ToArray();
            }

            DA.SetDataList(0, pt);
            DA.SetDataList(1, grid);
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
                foreach (var t in GetOutByItem<GH_Point>(0))
                    args.Display.DrawCircle(new Circle(worldXY, t.Value, SizeO.Def), ColorO.Def, args.DefaultCurveThickness);

                var ccc = Attributes.Selected ? args.WireColour_Selected : ColorV.Def;
                if (Ty.Paral)
                {
                    Parallel.ForEach(GetOutByItem<GH_Point>(1), t =>
                    {
                        args.Display.DrawCircle(new Circle(worldXY, t.Value, SizeV.Def),
                            ccc, args.DefaultCurveThickness);
                    });
                }
                else
                    foreach (var t in GetOutByItem<GH_Point>(1))
                        args.Display.DrawCircle(new Circle(worldXY, t.Value, SizeV.Def),
                            ccc, args.DefaultCurveThickness);
            }
            if (!Ov.Def) return;
            //绘制障碍物
            GH_PreviewWireArgs pwa = ToPreviewWireArgs(args, Color.FromArgb(Oc.Def.R, Oc.Def.B, Oc.Def.G));
            foreach (var o in GetIntByItem<GH_Brep>(1))
                o.DrawViewportWires(pwa);
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
                foreach (var o in GetIntByItem<GH_Brep>(1))
                    o.DrawViewportMeshes(pma);
            }

            DisplayBitmapDrawList displayBitmapDrawList = new DisplayBitmapDrawList
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
    /// <summary>
    /// 等高线高程分析
    /// Contour Line Elevation Analysis
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public sealed class LTCE : GradientComponent
    {
        public LTCE() : base("高程分析(等高线)", "LTCE",
            "分析等高线的高程，并获得其可视化色彩。\r\n烘焙：已着色等高线",
            "分析",
            ID.LTCE, 2, LTResource.等高线高程分析)
        {
            Gra.Def = Ty.Gradient0.Duplicate();
            Gra.ReCom = true;
        }
        protected override void AddParameter(ParamManager pm)
        {
            pm.AddIP(ParT.Curve, "等高线", "C", "待分析的等高线，请自行确保输入的都是水平曲线", ParamTrait.List);

            pm.AddOP(ParT.Colour, "色彩", "C", "输入曲线高程的映射色彩", ParamTrait.List);
            pm.AddOP(ParT.Interval, "范围", "R", "输入等高线的高程范围");
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<GH_Curve> g = new List<GH_Curve>(3);
            if (!DA.GetDataList(0, g)) return;
            var cd = g.Select(ci => ci.Value.PointAtEnd.Z).ToArray();
            Interval r = cd.ToInterval();

            DA.SetData(1, r);
            //cd的值转相对于r的标准参数，再获取对应位置色彩
            var col = cd.Select(d => Dou2Col(r, d)).ToList();
            DA.SetDataList(0, col);
        }
        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            if (Locked || args.Document.PreviewMode == GH_PreviewMode.Disabled) return; //跳过锁定或非线框模式
            var Col = GetOutByItem<GH_Colour>(0);
            var Cur = GetIntByItem<GH_Curve>(0);
            for (int i = 0; i < Col.Count; i++)
            {
                Curve cu = Cur[i].Value;
                if (cu.IsValid)
                    args.Display.DrawCurve(cu, Attributes.GetTopLevel.Selected ? args.WireColour_Selected : Col[i].Value);
            }
        }

        public override void BakeGeometry(RhinoDoc doc, ObjectAttributes att, List<Guid> obj_ids)
        {
            var Col = GetOutByItem<GH_Colour>(0);
            var Cur = GetIntByItem<GH_Curve>(0);
            for (var i = 0; i < Cur.Count; i++)
            {
                ObjectAttributes oa = att.Duplicate();
                oa.ColorSource = ObjectColorSource.ColorFromObject;
                oa.ObjectColor = Col[i].Value;
                Guid id = Guid.Empty;
                Cur[i].BakeGeometry(doc, oa, ref id);
                obj_ids.Add(id);
            }
        }
    }
    /// <summary>
    /// 等高线淹没分析
    /// Contour Flood Analysis
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public sealed class LTCF : AComponent
    {
        public LTCF() : base("淹没分析(等高线)", "LTCF",
            "通过等高线数据分析地形的淹没情况。\r\n烘焙：已着色已群组淹没/未淹曲线",
            "分析",
            ID.LTCF, 2, LTResource.等高线淹没分析)
        {
            UpColor = new MColorMenuItem(this, Color.White, "未淹色彩(&U)");
            DownColor = new MColorMenuItem(this, Color.FromArgb(59, 104, 156), "淹没色彩(&F)");
        }
        protected override void AddParameter(ParamManager pm)
        {
            pm.AddIP(ParT.Curve, "等高线", "C", "要进行淹没分析的等高线", ParamTrait.List);
            pm.AddIP(ParT.Integer, "高程", "E", "水面的高程");
            pm.AddIP(ParT.Boolean, "摊平", "F", "是否要将水下等高线摊平到水面，默认为false", def: false);

            pm.AddOP(ParT.Curve, "未淹线", "Cu", "未淹没区域的等高线", ParamTrait.List);
            pm.AddOP(ParT.Curve, "淹没线", "Cd", "被淹没区域的等高线", ParamTrait.List);
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            #region 输入输出变量初始化
            List<Curve> c = new List<Curve>(2);
            int e = 0;
            bool f = false;
            if (!DA.GetDataList(0, c)
                || !DA.GetData(1, ref e)
                || !DA.GetData(2, ref f))
                return;
            #endregion
            Plane ep = new Plane(new Point3d(0, 0, e), new Vector3d(0, 0, 1));

            var c0 = c.GroupBy(t => t.PointAtStart.Z > e)//分组
                .OrderBy(t => t.Key).ToArray();//排序，false在前
            List<Curve> ld = c0.First().Select(t => f ? Curve.ProjectToPlane(t, ep) : t).ToList();
            List<Curve> lu = c0.Last().ToList();
            DA.SetDataList(0, lu);
            DA.SetDataList(1, ld);
        }
        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            Menu_Color(menu, ref UpColor);
            Menu_Color(menu, ref DownColor);
        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            if (Locked || args.Document.PreviewMode == GH_PreviewMode.Disabled) return; //跳过锁定或非线框模式

            bool set = Attributes.GetTopLevel.Selected;
            if (!UpColor.Def.IsEmpty)
                foreach (GH_Curve l1 in GetOutByItem<GH_Curve>(0).Where(l1 => l1.IsValid))
                    args.Display.DrawCurve(l1.Value, set ? args.WireColour_Selected : UpColor.Def);

            if (DownColor.Def.IsEmpty)
                foreach (GH_Curve l1 in GetOutByItem<GH_Curve>(1).Where(l1 => l1.IsValid))
                    args.Display.DrawCurve(l1.Value, set ? args.WireColour_Selected : DownColor.Def);
        }
        public override void BakeGeometry(RhinoDoc doc, ObjectAttributes att, List<Guid> obj_ids)
        {
            doc.BakeColorGroup(GetOutByList<GH_Curve>(0), UpColor.Def, "UpWater", att, obj_ids);
            doc.BakeColorGroup(GetOutByList<GH_Curve>(1), DownColor.Def, "DownWater", att, obj_ids);
        }

        /// <summary>
        /// 水上色彩
        /// </summary>
        private MColorMenuItem UpColor;

        /// <summary>
        /// 水下色彩
        /// </summary>
        private MColorMenuItem DownColor;
    }
    /// <summary>
    /// 山路坡度分析
    /// Contour Flood Analysis
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public sealed class LTRA : GradientComponent, IMCom_DoubleClick
    {
        public LTRA() : base("山路坡度分析", "LTRA",
            "分析山路坡度并按角度赋予其对应色彩。\r\n双击：【坡度】【角度范围】切换为角度/弧度。\r\n烘焙：已按列成组已着色直线段",
            "分析",
            ID.LTRA, 3, LTResource.山路坡度分析)
        {
            Gra.Def = Ty.Gradient0.Duplicate();
            Gra.ReCom = true;
            GI = new MBooleanMenuItem(this, true, "自适应角度(&A)", mf: m => m.Def ? "自适应" : "0-90º");
            C = new Update<Color[][]>(UpdateC);
            PreviewExpired += (s, r) => C.Clear();
            UD = new MBooleanMenuItem(this, true, "使用角度(&)", true, mf: m => m.Def ? "角度" : "弧度");
        }
        protected override void AddParameter(ParamManager pm)
        {
            pm.AddIP(ParT.Curve, "山路", "C", "要分析的山路中线（确保已投影在地形上）", ParamTrait.List);
            pm.AddIP(ParT.Integer, "精度", "E", "山路中线的细分重建密度(单位米)");

            pm.AddOP(ParT.Line, "路线", "L", "重建后用于分析的直线路线", ParamTrait.List);
            pm.AddOP(ParT.Number, "坡度", "A", "对应直线段的坡度(度)", ParamTrait.List);
            pm.AddOP(ParT.Text, "坡度范围", "Rs", "山路直线的坡度范围（即坡高/坡长）");
            pm.AddOP(ParT.Interval, "角度范围", "Ra", "山路直线与水平面所呈角度（度）的范围");
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (DA.Iteration == 0)
            {
                Lll = new List<List<Line>>(5);
                Dll = new List<List<double>>(5);
            }
            #region 输入输出变量初始化
            List<Curve> cl = new List<Curve>(0);
            int e = 0;
            if (!DA.GetDataList(0, cl) || cl.Count == 0
                || !DA.GetData(1, ref e))
                return;
            #endregion

            Line[][] la = cl.Select(t =>
                new Polyline(t.DivideByCount(
                           // (int)Math.Ceiling(t.GetLength() / e), false//获取细分数
                           (int)Math.Round(t.GetLength() / e), true
                        )//获取细分点t值
                        .Select(t.PointAt)//t值转成点
                ).GetSegments()//将点转成多段线后，再提取全部线段
            ).ToArray();

            List<Line> ll = la.SelectMany(t => t).ToList(); //全部的线段都摊平到一个列表里

            var v = ll.Select(t => t.Direction).ToArray();
            //向量单元化
            for (int i = 0; i < v.Length; i++)
                v[i].Unitize();

            var d = UD.Def ? Majas_Ex.R2A : 1;
            //获取方向向量，计算角度,并保证是正的
            var a = v.Select(t => t.向量转坡度() * d).ToArray();

            DA.SetDataList(0, ll);
            DA.SetDataList(1, a);
            Interval ai = a.ToInterval();//获取角度区间
            double s = Math.Max(Math.Tan(ai.Max), 0.01);//避免分母太大，计算坡度较大值
            string rs = "0 to " + (s > 1 ? $"1/{Math.Round(s, 2)}" : $"{1 / s}/1");//格式化坡度范围
            DA.SetData(2, rs);
            DA.SetData(3, new Interval(Math.Round(ai.T0, UD.Def ? 2 : 4), Math.Round(ai.T1, UD.Def ? 2 : 4)));//格式化角度范围，弧度则不用格式化
            Lll.Add(ll);
            Dll.Add(a.ToList());
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_Boolean(menu, ref GI, "默认启用，此时渐变色彩范围对应实际的角度范围。\r\n不启用时，范围对应0-90º", click: (s, e) => UpdateC());
            Menu_Boolean(menu, ref UD, "勾选时,【坡度】【角度范围】输出端输出度,否则为弧度", LTResource.Degrees_16,
                (s, e) => UD.ReplacePDesc(true, "（弧度）", "（度）", 1, 3));
        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            if (Locked || args.Document.PreviewMode == GH_PreviewMode.Disabled) return; //跳过锁定或非线框模式
            for (int i = 0; i < Lll.Count; i++)
                for (int j = 0; j < Lll[i].Count; j++)
                    args.Display.DrawLine(Lll[i][j], Attributes.GetTopLevel.Selected ? args.WireColour_Selected : C.Value[i][j]);
        }
        public override void BakeGeometry(RhinoDoc doc, ObjectAttributes att, List<Guid> obj_ids)
        {
            if (Locked)return;
            for (int i = 0; i < Lll.Count; i++)
            {
                ObjectAttributes oa = att.Duplicate();
                oa.ColorSource = ObjectColorSource.ColorFromObject;
                int groupIndex = doc.Groups.Add();
                oa.AddToGroup(groupIndex);
                for (int j = 0; j < Lll[i].Count; j++)
                {
                    ObjectAttributes oaj = oa.Duplicate();
                    oaj.ObjectColor = C.Value[i][j];
                    Guid id = Guid.Empty;
                    new GH_Line(Lll[i][j]).BakeGeometry(doc, oaj, ref id);
                    obj_ids.Add(id);
                }
            }
        }
        public override void CreateAttributes()
            => m_attributes = new DoubleClick_Attributes(this);

        private void UpdateC()
        {
            if(Locked)return;
            var itl = GetOutByItem<GH_Interval>(3);

            if (Dll.Count != itl.Count)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "输出的坡度的列表数量与区间数量不一致，请联系开发者修复bug！");
                C.Value = Dll.Select(t => t.Select(t0 => Color.Black).ToArray()).ToArray();
                return;
            }

            C.Value = new Color[Dll.Count][];
            for (int i = 0; i < Dll.Count; i++)
            {
                Interval interval = GI.Def ? itl[i].Value : Ty.A0(UD.Def);
                C.Value[i] = Dll[i].Select(t => Dou2Col(interval, t)).ToArray();
            }
        }

        private readonly Update<Color[][]> C;
        private List<List<Line>> Lll = new List<List<Line>>(0);
        private List<List<double>> Dll = new List<List<double>>(0);
        /// <summary>
        /// 渐变是否自适应角度范围，否则为0-90度
        /// </summary>
        private MBooleanMenuItem GI;
        /// <summary>
        /// 使用角度
        /// </summary>
        private MBooleanMenuItem UD;
        public MBooleanMenuItem DoubleClick => UD;
    }
    /// <summary>
    /// 实时山路坡度反馈
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public sealed class LTRW : GradientComponent
    {
        public LTRW() : base("实时山路坡度反馈", "LTRW",
            "实时反馈所绘制的山路坡度是否合理，\r\n不合理的区域用提示圆标注出来。" +
            "\r\n注意:绘制需要在top视图【road】图层内。\r\n双击：自动建立【road】图层并切换为当前图层。\r\n烘焙：已按输入线成组已着色直线段",
            "分析",
            ID.LTRW, 3, LTResource.实时山路绘制反馈)
        {
            Gra.Def = Ty.Gradient0.Duplicate();
            Gra.ReCom = false;
            GH = new MGradientMenuItem(this, GH_GradientControl.GradientPresets[1].Duplicate(), "高程渐变(&H)");
            GI = new MBooleanMenuItem(this, false, "自适应角度(&A)", mf: m => m.Def ? "自适应" : "0-上限");
            CV = new MBooleanMenuItem(this, true, "提示圆(&W)", mf: m => m.Def ? "显示提示圆" : "");
            MV = new MBooleanMenuItem(this, false, "地形网格(&T)", mf: m => m.Def ? "显示地形" : "");
            CR = new MDoubleMenuItem(this, 5, "提示圆半径(&R)");
            Ct = new Update<Color[][]>(UpdateC);
            Ma = new Update<Mesh[]>(UpdateM);
            PreviewExpired += (s, e) => Ct.Clear();
            PreviewExpired += (s, e) => Ma.Clear();
            HL = Math.PI;
        }
        protected override void AddParameter(ParamManager pm)
        {
            pm.AddIP(ParT.Mesh, "地形", "M", "山地地形网格,仅限一列", ParamTrait.List | ParamTrait.OneList);
            pm.AddIP(ParT.Integer, "重建精度", "E", "道路中线细分重建精度(米/一个点)", ParamTrait.OnlyOne, def: 2);
            pm.AddIP(ParT.Number, "坡度倒数", "P", "坡度倒数，用来筛选不合理坡度", ParamTrait.OnlyOne, def: 4);

            pm.AddOP(ParT.Curve, "山路", "R", "被分析的山路线段", ParamTrait.Tree);
            pm.AddOP(ParT.Angle, "坡度", "P", "山路线段对应的坡度(弧度)", ParamTrait.Tree | ParamTrait.UseDegrees);
            pm.AddOP(ParT.Integer, "不合理", "I", "坡度超过上限的线段的索引", ParamTrait.Tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (DA.Iteration > 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "本电池仅进行一次计算，多余数据已被忽视");
                return;
            }

            List<Mesh> m = new List<Mesh>(0);
            int e = 0;
            double p = 0;
            if (!DA.GetDataList(0, m)
                || m.Count == 0
                || !m.All(t => t.IsValid)
                || !DA.GetData(1, ref e)
                || e <= 0
                || !DA.GetData(2, ref p)
                || p <= 0)
                return;
            if (Ty.L < 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "road图层不存在。可双击本电池图标来建立，并设为当前");
                return;
            }
            Settings.LayerIndexFilter = Ty.L;

            HL = Math.Atan(1 / p);//计算坡度上限

            var ma = m.ToArray();

            var cl = RhinoDoc.ActiveDoc.Objects.GetObjectList(Settings)
                .Select(t => new ObjRef(t))
                .ToArray();

            KeyValuePair<Guid, uint>[] hashl = cl.Select(t => new KeyValuePair<Guid, uint>(t.ObjectId, t.RuntimeSerialNumber)).ToArray();
            Line[][] slaa = new Line[cl.Length][];
            double[][] sdaa = new double[cl.Length][];
            int[][] siaa = new int[cl.Length][];

            for (int i = 0; i < cl.Length; i++)
            {
                #region 查找有无对应，有则输出对应索引
                var ii = -1;
                for (int j = 0; j < HashA.Length; j++)
                {
                    if (HashA[j].Key != hashl[i].Key || HashA[j].Value != hashl[i].Value) continue;
                    ii = j;
                    break;
                }
                #endregion

                if (ii < 0)
                {
                    var t5 = cl[i].Curve().DuplicateCurve();//备份一份并转换成曲线
                    var t4 = t5.DivideByCount((int)Math.Round(t5.GetLength() / e), true) //曲线按精度细分出t值
                        .Select(t5.PointAt);//t值转点
                    var t3 = Intersection.ProjectPointsToMeshes(ma, t4, Vector3d.ZAxis, DocumentTolerance());
                    if (t3 == null)//剔除不在网格上的点
                        continue;
                    slaa[i] = new Polyline(t3).GetSegments();
                    if (Ty.Paral)//尝试使用多核
                        sdaa[i] = slaa[i].AsParallel().Select(t =>
                        {
                            Vector3d v = t.Direction;//直线转对应向量
                            v.Unitize();
                            return v.向量转坡度();//向量转坡度(度)
                        }
                        ).ToArray();
                    else
                        sdaa[i] = slaa[i].Select(t =>
                        {
                            Vector3d v = t.Direction;//直线转对应向量
                            v.Unitize();
                            return v.向量转坡度();//向量转坡度(度)
                        }
                        ).ToArray();

                    List<int> ill = new List<int>(5);
                    for (int j = 0; j < sdaa[i].Length; j++)
                        if (sdaa[i][j] > HL)
                            ill.Add(j);
                    siaa[i] = ill.ToArray();

                }//上次的数据无符合的曲线
                else
                {
                    slaa[i] = Laa[ii];
                    sdaa[i] = Daa[ii];
                    siaa[i] = Iaa[ii];
                }//有对应的曲线
            }
            //储存计算结果，并转化数据
            HashA = hashl;
            Laa = slaa;
            Daa = sdaa;
            Iaa = siaa;

            DA.SetDataTree(0, Laa.Select(t => t.Select(t0 => new GH_Line(t0))).ToGhStructure());
            DA.SetDataTree(1, Daa.Select(t => t.Select(t0 => new GH_Number(t0))).ToGhStructure());
            DA.SetDataTree(2, Iaa.Select(t => t.Select(t0 => new GH_Integer(t0))).ToGhStructure());
            ExpirePreview(true);
        }
        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_Boolean(menu, ref GI, "默认启用，此时渐变色彩范围对应实际的角度范围。\r\n不启用时，范围对应0-角度上限º");
            Menu_Boolean(menu, ref MV, "控制是否显示地形网格");
            Menu_BGradient(MV, ref GH, "此渐变按高程着色输入网格");
            Menu_Boolean(menu, ref CV, "控制是否显示提示圆");
            Menu_BDouble(CV, ref CR);
        }

        public override void RegisterRemoteIDs(GH_GuidTable table)
        {
            base.RegisterRemoteIDs(table);
            foreach (var p in HashA)
                table.Add(p.Key, this);
        }

        public override void BakeGeometry(RhinoDoc doc, ObjectAttributes att, List<Guid> obj_ids)
        {
            for (var i = 0; i < Laa.Length; i++)
            {
                var t = Laa[i];
                ObjectAttributes oa = att.Duplicate();
                oa.ColorSource = ObjectColorSource.ColorFromObject;
                int groupIndex = doc.Groups.Add();
                oa.AddToGroup(groupIndex);
                for (var j = 0; j < t.Length; j++)
                {
                    ObjectAttributes oaj = oa.Duplicate();
                    oaj.ObjectColor = Ct.Value[i][j];
                    Guid id = Guid.Empty;
                    new GH_Line(Laa[i][j]).BakeGeometry(doc, oaj, ref id);
                    obj_ids.Add(id);
                }
            }
        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            if (Locked || Hidden) return;
            Color c0 = Attributes.Selected ? args.WireColour_Selected : args.WireColour;
            if (Laa.Length != 0)
            {
                args.Viewport.GetFrustumNearPlane(out Plane worldXY);
                for (int i = 0; i < Laa.Length; i++)
                {
                    for (int j = 0; j < Laa[i].Length; j++)
                        args.Display.DrawLine(Laa[i][j], Ct.Value[i][j]); //绘制山路线段
                    if (CV.Def)
                    {
                        //绘制提示圆
                        var i1 = i;
                        var ca = Iaa[i].Select(t =>
                        {
                            var l0 = Laa[i1][t];//索引转换直线
                            return (l0.From + l0.To) / 2;//获取直线中点
                        })
                            .Select(t => new Circle(worldXY, t, CR.Def)).ToArray();
                        foreach (var c in ca)
                            args.Display.DrawCircle(c, c0);
                    }
                }
            }
            if (MV.Def && args.Document.PreviewMode == GH_PreviewMode.Wireframe)
                foreach (var m in Ma.Value)//绘制地形网格
                    args.Display.DrawMeshWires(m, c0);
        }

        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            if (Locked || Hidden) return;
            if (MV.Def && args.Document.PreviewMode == GH_PreviewMode.Shaded)
                foreach (var m in Ma.Value)//绘制地形网格
                    args.Display.DrawMeshFalseColors(m);
        }

        public override void CreateAttributes()
            => m_attributes = new LTRW_Attributes(this);
        /// <summary>
        /// 山路色彩
        /// </summary>
        private readonly Update<Color[][]> Ct;
        private void UpdateC()
        {
            Interval a1 = GI.Def ? Daa.SelectMany(t => t).ToInterval() : new Interval(0, HL);
            Ct.Value = Daa.Select(t => t.Select(t0 => Dou2Col(a1, t0)).ToArray()).ToArray();
        }
        /// <summary>
        /// 地形网格
        /// </summary>
        private readonly Update<Mesh[]> Ma;
        private void UpdateM()
        {
            Ma.Value = GetIntByItem<GH_Mesh>(0)
                .Select(t => t.Value)
                .Select(t =>
                {
                    t.VertexColors.Clear();
                    var i = t.Vertices.Select(t0 => (double)t0.Z).ToInterval();
                    t.VertexColors.AppendColors(
                        t.Vertices.Select(t0 => GH.Def.ColourAt(i.NormalizedParameterAt(t0.Z))).ToArray()
                    );
                    return t;
                }).ToArray();
        }
        /// <summary>
        /// 上次计算获得的曲线id及序列号
        /// </summary>
        private KeyValuePair<Guid, uint>[] HashA = new KeyValuePair<Guid, uint>[0];
        /// <summary>
        /// 上次计算获得的直线段
        /// </summary>
        private Line[][] Laa = new Line[0][];
        /// <summary>
        /// 上次计算获得的弧度
        /// </summary>
        private double[][] Daa = new double[0][];
        /// <summary>
        /// 上次计算获得的索引
        /// </summary>
        private int[][] Iaa = new int[0][];
        /// <summary>
        /// 显示地形
        /// </summary>
        private MBooleanMenuItem MV;
        /// <summary>
        /// 高程渐变
        /// </summary>
        private MGradientMenuItem GH;
        /// <summary>
        /// 角度上限
        /// </summary>
        private double HL;
        /// <summary>
        /// 渐变是否自适应角度范围，否则为0-90度
        /// </summary>
        private MBooleanMenuItem GI;
        /// <summary>
        /// 显示提示圆
        /// </summary>
        private MBooleanMenuItem CV;
        /// <summary>
        /// 提示圆尺寸
        /// </summary>
        private MDoubleMenuItem CR;
        /// <summary>
        /// 物件查找设定
        /// </summary>
        private static readonly ObjectEnumeratorSettings Settings = new ObjectEnumeratorSettings
        {
            ActiveObjects = true,
            LockedObjects = true,
            HiddenObjects = true,
            ObjectTypeFilter = ObjectType.Curve
        };
    }
    /// <summary>
    /// 山路坡度分析_属性
    /// </summary>
    public sealed class LTRW_Attributes : GH_ComponentAttributes
    {
        public LTRW_Attributes(IGH_Component component) : base(component) { }

        public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button == MouseButtons.Left && Bounds.Contains(e.CanvasLocation) && Owner is LTRW)
            {
                if (Ty.L < 0)//不存在图层则创建
                    RhinoDoc.ActiveDoc.Layers.Add("road", Color.Black);
                RhinoDoc.ActiveDoc.Layers.SetCurrentLayerIndex(Ty.L, true);//图层设为当前
            }

            return GH_ObjectResponse.Handled;
        }
    }
}