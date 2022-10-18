using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GH_IO.Serialization;
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

namespace Lt.Analysis
{
    //after 给渐变的电池 加色彩标尺

    /// <summary>
    /// 网格淹没分析
    /// Flooded Terrain
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public class LTMF : GradientComponent
    {
        [SuppressMessage("ReSharper", "PossibleLossOfFraction")]
        public LTMF() : base(
            "淹没分析(网格)", "LTMF",
            "分析被水淹没后的地形状态",
            "分析",
            ID.LTMF, 1, LTResource.山体淹没分析)
        {
            Gradient = new GH_Gradient(
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
            if (!DA.GetData(0, ref t0))
                return;
            double e0 = 0;
            if (!DA.GetData(1, ref e0))
                return;
            bool f = false;
            if (!DA.GetData(2, ref f))
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
                t0.VertexColors.Add(hb ? Gradient.ColourAt(ie.NormalizedParameterAt(z)) : DownColor.Value);
            }

            DA.SetData(0, t0);
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
            => Menu_Color(menu, "淹没色彩(&F)", DownColor, recom: true);

        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            if (args.Document.PreviewMode != GH_PreviewMode.Shaded || Hidden || !args.Display.SupportsShading)
                return; ///跳过非着色模式和，或参数不支持预览
            Ty.Draw1Meshes(0, this, args);
        }
        public override bool Read(GH_IReader reader)
        {
            DownColor.Value = reader.GetDrawingColor("colordown");
            return base.Read(reader);
        }
        public override bool Write(GH_IWriter writer)
        {
            writer.SetDrawingColor("colordown", DownColor.Value);
            return base.Write(writer);
        }
        /// <summary>
        /// 水下色彩
        /// </summary>
        private GH_Colour DownColor = new GH_Colour(Color.FromArgb(52, 58, 107));
    }
    /// <summary>
    /// 网格坡向分析
    /// Slope Direction Analysis
    /// </summary>
    // todo 标明 用户对象版 依赖网格炸开
    public class LTMD : AComponent
    {
        public LTMD()
            : base("坡向分析(网格)", "LTMD",
                "山坡地形朝向分析,X轴向为东,Y轴向为北" +
                "\r\n双击电池图标以切换着色模式," +
                "\r\n注意：顶点着色会焊接网格顶点，可能导致顶点减少，" +
                "\r\n面着色会把面的每个顶点都解离出来，可能会导致顶点增加",
                "分析",
                ID.LTMD, 1, LTResource.山体坡向分析)
        {
            Shade = true;
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

            Mesh t0 = new Mesh();
            if (!DA.GetData(0, ref t0))
                return;
            List<Color> c = new List<Color>(9);
            if (!DA.GetDataList(1, c))
                return;
            if (c.Count != 9)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "输入色彩数量不等于9！不足已补充为最后一项,多出已忽视。");
                if (c.Count < 9)
                    for (int i = c.Count; i < 9; i++)
                        c.Add(c[i - 1]);
            }

            #endregion
            if (Shade)
            {
                //数量不对时计算面法向
                if (t0.FaceNormals.Count != t0.Faces.Count)
                    t0.FaceNormals.ComputeFaceNormals();
                //计算色彩
                var cl = t0.FaceNormals.Select(t => c[DShade(t)]).ToArray();
                //获取顶点列表
                Point3f[] vl = t0.Vertices.ToArray();
                var fl = t0.Faces.ToArray();
                var nl = t0.FaceNormals.ToArray();
                //清理顶点及顶点色彩列表
                t0.VertexColors.Clear();
                t0.Vertices.Clear();
                t0.Faces.Clear();
                t0.Normals.Clear();
                for (var i = 0; i < fl.Length; i++)
                {
                    var tri = fl[i].IsTriangle;
                    for (int j = 0; j < 3; j++)
                    {
                        //添加顶点,并修改对应面索引
                        fl[i][j] = t0.Vertices.Add(vl[fl[i][j]]);
                        //添加色彩
                        t0.VertexColors.Add(cl[i]);
                        //添加法向
                        t0.Normals.Add(nl[i]);
                    }

                    if (tri)
                        fl[i][3] = fl[i][2];
                    else
                    {
                        fl[i][3] = t0.Vertices.Add(vl[fl[i][3]]);
                        t0.VertexColors.Add(cl[i]);
                        t0.Normals.Add(nl[i]);
                    }
                }
                t0.Faces.AddFaces(fl);
            }
            else
            {
                t0.VertexColors.Clear();//清理旧色彩
                t0.Weld(DocumentAngleTolerance());//焊接顶点
                t0.Normals.ComputeNormals();//计算顶点法向
                //根据顶点法向计算色彩并添加
                foreach (Vector3f t in t0.Normals)
                    t0.VertexColors.Add(c[DShade(t)]);
            }

            DA.SetData(0, t0);
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
            => Menu_AppendItem(menu, "使用面着色(&F)",
                delegate
                {
                    Shade = !Shade;
                    ExpireSolution(true);
                },
                null, true, Shade);

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

        public override void CreateAttributes()
        => m_attributes = new LTMD_Attributes(this);

        public override bool Write(GH_IWriter writer)
        {
            writer.SetBoolean("面色否", Shade);
            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            Shade = reader.GetBoolean("面色否");
            return base.Read(reader);
        }

        private static readonly double Num1 = Math.Tan(Math.PI / 8);
        private static readonly double Num2 = Math.Tan(Math.PI * 3 / 8);
        private bool _shade;

        internal bool Shade
        {
            get => _shade;
            set
            {
                Message = value ? "面着色" : "顶点着色";
                _shade = value;
            }
        }
    }
    /// <summary>
    /// 坡向分析_属性
    /// </summary>
    public class LTMD_Attributes : GH_ComponentAttributes
    {
        public LTMD_Attributes(IGH_Component component) : base(component) { }

        public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button == MouseButtons.Left && Bounds.Contains(e.CanvasLocation) && Owner is LTMD j)
            {
                j.Shade = !j.Shade;
                j.ExpireSolution(true);
            }

            return GH_ObjectResponse.Handled;
        }
    }
    /// <summary>
    /// 网格坡度分析
    /// Terrain Mesh Grade Analysis
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public class LTMG : GradientComponent
    {
        public LTMG() : base("坡度分析(网格)", "LTMG",
            "山地地形坡度分析",
            "分析",
            ID.LTMG, 1, LTResource.山体坡度分析)
        {
            Gradient = Ty.Gradient0.Duplicate();
            ReCom = true;
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
            var ra = tm.Normals.Select(t => Math.Round(Math.Acos(t.Z) * 57.29, 2)).ToArray();//法向转角度,保留两位小数

            //获取区间
            Interval ia = ra.ToInterval();

            //角度转换为色彩并给予网格
            tm.VertexColors.AppendColors(ra.Select(t => Dou2Col(ia, t)).ToArray());
            DA.SetData(0, tm);
            DA.SetData(1, ia);
        }
        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            if (args.Document.PreviewMode != GH_PreviewMode.Shaded || Hidden || !args.Display.SupportsShading)
                return; ///跳过非着色模式和，或参数不支持预览
            Ty.Draw1Meshes(0, this, args);
        }
    }
    /// <summary>
    /// 网格高程分析
    /// Terrain Mesh Elevation Analysis
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public class LTME : GradientComponent
    {
        public LTME() : base("高程分析(网格)", "LTME",
            "山地地形高程分析",
            "分析",
            ID.LTME, 1, icon: LTResource.山体高程分析)
        {
            Gradient = Ty.Gradient0.Duplicate();
            ReCom = true;
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
    public class LTVL : AComponent
    {
        public LTVL() : base("视线分析", "LTVL",
            "分析在山地某处的可见范围,cpu线程数大于2时自动调用多核计算",
            "分析",
            ID.LTVL, 4, LTResource.视线分析)
        {
           Paral = Environment.ProcessorCount > 1;
        }
        protected override void AddParameter(ParamManager pm)
        {
            pm.AddIP(ParT.Mesh, "地形", "Mt", "要进行坡度分析的山地地形网格,仅支持单项数据", ParamTrait.Item | ParamTrait.OnlyOne);
            pm.AddIP(ParT.Mesh, "障碍物", "O", "（可选）阻挡视线的障碍物体，,仅支持单列数据", ParamTrait.List | ParamTrait.OnlyOne | ParamTrait.Optional);
            pm.AddIP(ParT.Point, "观察点", "P", "观察者所在的点位置（可不在网格上），支持多点观察,仅支持单列数据", ParamTrait.List | ParamTrait.OnlyOne);
            pm.AddIP(ParT.Integer, "精度", "A", "分析精度(单位：米)，即分析点阵内的间距,仅支持单项数据", ParamTrait.Item | ParamTrait.OnlyOne);

            pm.AddOP(ParT.Point, "观察点", "O", "观察者视点位置", ParamTrait.List);
            pm.AddOP(ParT.Point, "可见点", "V", "被看见的点", ParamTrait.List);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (DA.Iteration == 0)
            {
                Pt.Clear();
                Grid.Clear();
            }

            if (DA.Iteration > 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "超出数据已被忽视");
                return;
            }

            Mesh tm = new Mesh();
            if (!DA.GetData(0, ref tm) && !tm.IsValid) return;
            List<Mesh> o = new List<Mesh>(3);
            DA.GetDataList(1, o);
            List<Point3d> pl = new List<Point3d>();
            if (!DA.GetDataList(2, pl)) return;

            int a = 0;
            if (!DA.GetData(3, ref a)) return;

            //制作网格上用于测量的点阵
            BoundingBox mb = new BoundingBox();
            foreach (Point3f p3f in tm.Vertices)
                mb.Union(new Point3d(p3f));

            double px = mb.Min.X;
            double py = mb.Min.Y;
            int rx = (int)Math.Round((mb.Max.X - px) / a);
            int ry = (int)Math.Round((mb.Max.Y - py) / a);
            //获取平面上点阵
            Point3d[] grid = new Point3d[rx * ry];
            for (int ix = 0; ix < rx; ix++)
                for (int iy = 0; iy < ry; iy++)
                    grid[ix * ry + iy] = new Point3d(mb.Min.X + ix * a, mb.Min.Y + iy * a, mb.Min.Z);
            //将点z向投影到网格上
            Point3d ProjectZ(Point3d t)
            {
                Ray3d r = new Ray3d(new Point3d(t.X, t.Y, mb.Min.Z), Vector3d.ZAxis); //转换射线
                double d = Intersection.MeshRay(tm, r); //求交点参数
                return d < 0 ? Point3d.Unset : r.PointAt(d); //返回点
            }


            //将栅格点投影到地形网格上
            grid = grid.Select(ProjectZ).Where(t => t != Point3d.Unset).ToArray();//剔除不在网格上的点

            //将观测点投影到地形网格上，并增加眼高
            var pt = pl.Select(ProjectZ).Where(t => t != Point3d.Unset).ToArray();
            for (int i = 0; i < pt.LongLength; i++)
                pt[i].Z += EyeHight.Value;//增加眼高

            //获取无遮挡时能被观察到的点
            var g0 = grid.Where(t =>
                pt.Aggregate(false, (c, t0) =>
                    c || Intersection.MeshLine(tm, new Line(t0, t), out _).Length == 1));
            //剔除被障碍物遮挡
            grid = g0.Where(t =>
                pt.Aggregate(false, (c0, t0) =>

                    c0 || o.Aggregate(true, (c1, t1) =>
                    {
                        if (!c1) return false;
                        var ml = Intersection.MeshLine(t1, new Line(t0, t), out _);
                        switch (ml.Length)
                        {
                            case 0: return true;
                            case 1: return ml[0] == t;
                            default: return false;
                        }
                    }
                    )
                )
            ).ToArray();

            DA.SetDataList(0, pt);
            Pt.AddRange(pt);
            DA.SetDataList(1, grid);
            Grid.AddRange(grid);
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            Menu_Color(menu, "观察点色彩(&C)", ColorO);
            Menu_Double(menu, "观察点尺寸(&S)", SizeO, icon: LTResource.PointStyle_20x20);
            Menu_Color(menu, "可见点色彩(&C)", ColorV);
            Menu_Double(menu, "可见点尺寸(&S)", SizeV, icon: LTResource.PointStyle_20x20);
            Menu_Double(menu, "眼高（单位米）(&E)", EyeHight, "人眼高度", icon: LTResource.EyeHight_20x20, recom: true);
        }
        public override bool Read(GH_IReader reader)
        {
            ColorO.Value = reader.GetDrawingColor("观色");
            SizeO.Value = reader.GetDouble("观寸");
            ColorV.Value = reader.GetDrawingColor("见色");
            SizeV.Value = reader.GetDouble("见寸");
            EyeHight.Value = reader.GetDouble("眼高");
            return base.Read(reader);
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.SetDrawingColor("观色", ColorO.Value);
            writer.SetDouble("观寸", SizeO.Value);
            writer.SetDrawingColor("见色", ColorV.Value);
            writer.SetDouble("见寸", SizeV.Value);
            writer.SetDouble("眼高", EyeHight.Value);
            return base.Write(writer);
        }
        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            if (Hidden || !IsPreviewCapable || Locked|| args.Document.PreviewMode==GH_PreviewMode.Shaded) return; //电池隐藏或不可预览时跳过
            args.Viewport.GetFrustumNearPlane(out Plane worldXY);
            foreach (Point3d t in Pt)
                args.Display.DrawCircle(new Circle(worldXY, t, SizeO.Value), ColorO.Value, args.DefaultCurveThickness);

            foreach (Point3d t in Grid)
                args.Display.DrawCircle(new Circle(worldXY, t, SizeV.Value), ColorV.Value, args.DefaultCurveThickness);
        }

        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            if (Hidden || !IsPreviewCapable || Locked || !args.Display.SupportsShading) return;
            MeshO = new Mesh();
            foreach (Mesh t in Pt.Select(t
                         => Mesh.CreateFromSphere(new Sphere(t, SizeO.Value), 60, 30)))
                MeshO.Append(t);
            if (MeshO.VertexColors.Count == 0 || MeshO.VertexColors[0] != ColorO.Value)
                MeshO.VertexColors.CreateMonotoneMesh(ColorO.Value);
            args.Display.DrawMeshFalseColors(MeshO);


            DisplayBitmapDrawList displayBitmapDrawList = new DisplayBitmapDrawList
            {
                MaximumCachedSortLists = 200
            };
            displayBitmapDrawList.SetPoints(Grid, Grid.Select(t => ColorV.Value));
            args.Display.DrawSprites(new DisplayBitmap(LTResource.FuzzySprite_64x64), displayBitmapDrawList,
                Convert.ToSingle(SizeV.Value), true);
        }


        /// <summary>
        /// 观察点
        /// </summary>
        private List<Point3d> Pt = new List<Point3d>();
        private Mesh MeshO = new Mesh();
        /// <summary>
        /// 观察点色彩
        /// </summary>
        private static GH_Colour ColorO = new GH_Colour(Color.Red);
        private static GH_Number SizeO = new GH_Number(10);
        /// <summary>
        /// 可见点
        /// </summary>
        private List<Point3d> Grid = new List<Point3d>();
        private Mesh MeshV = new Mesh();
        /// <summary>
        /// 可见点色彩
        /// </summary>
        private static GH_Colour ColorV = new GH_Colour(Color.FromArgb(0, 207, 182));
        private static GH_Number SizeV = new GH_Number(4);

        private static GH_Number EyeHight = new GH_Number(1.5);
        private readonly bool Paral;
    }
    /// <summary>
    /// 等高线高程分析
    /// Contour Line Elevation Analysis
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public class LTCE : GradientComponent
    {
        public LTCE() : base("高程分析(等高线)", "LTCE",
            "分析等高线的高程，并获得其可视化色彩。可直接烘焙出已着色曲线",
            "分析",
            ID.LTCE, 2, LTResource.等高线高程分析)
        {
            Gradient = Ty.Gradient0.Duplicate();
            ReCom = true;
        }
        protected override void AddParameter(ParamManager pm)
        {
            pm.AddIP(ParT.Curve, "等高线", "C", "待分析的等高线，请自行确保输入的都是水平曲线", ParamTrait.List);

            pm.AddOP(ParT.Colour, "色彩", "C", "输入曲线高程的映射色彩", ParamTrait.List);
            pm.AddOP(ParT.Interval, "范围", "R", "输入高程线的高程范围");
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (DA.Iteration == 0)
            {
                Col.Clear();
                Cur.Clear();
            }

            List<GH_Curve> g = new List<GH_Curve>(3);
            if (!DA.GetDataList(0, g)) return;
            var cd = g.Select(ci => ci.Value.PointAtEnd.Z).ToArray();
            Interval r = cd.ToInterval();

            DA.SetData(1, r);
            //cd的值转相对于r的标准参数，再获取对应位置色彩
            var col = cd.Select(d => Dou2Col(r, d)).ToList();
            DA.SetDataList(0, col);
            Col.AddRange(col);
            Cur.AddRange(g);
        }
        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            if (Locked || args.Document.PreviewMode == GH_PreviewMode.Disabled) return; //跳过锁定或非线框模式
            for (int i = 0; i < Col.Count; i++)
            {
                Curve cu = Cur[i].Value;
                if (cu.IsValid)
                    args.Display.DrawCurve(cu, Attributes.GetTopLevel.Selected ? args.WireColour_Selected : Col[i]);
            }
        }

        public override void BakeGeometry(RhinoDoc doc, ObjectAttributes att, List<Guid> obj_ids)
        {
            for (var i = 0; i < Cur.Count; i++)
            {
                GH_Curve c = Cur[i];
                if (!c.IsValid)
                    continue;
                ObjectAttributes oa = att.Duplicate();
                oa.ColorSource = ObjectColorSource.ColorFromObject;
                oa.ObjectColor = Col[i];
                Guid id = Guid.Empty;
                c.BakeGeometry(doc, oa, ref id);
                obj_ids.Add(id);
            }
        }

        private readonly List<Color> Col = new List<Color>();
        private readonly List<GH_Curve> Cur = new List<GH_Curve>();
    }
    /// <summary>
    /// 等高线淹没分析
    /// Contour Flood Analysis
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public class LTCF : AComponent
    {
        public LTCF() : base("淹没分析(等高线)", "LTCF",
            "通过等高线数据分析地形的淹没情况。可直接烘焙出已着色曲线",
            "分析",
            ID.LTCF, 2, LTResource.等高线淹没分析)
        { }
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
            if (DA.Iteration == 0)
            {
                Cu.Clear();
                Cd.Clear();
            }
            #region 输入输出变量初始化
            List<Curve> c = new List<Curve>(2);
            if (!DA.GetDataList(0, c)) return;
            int e = 0;
            if (!DA.GetData(1, ref e)) return;
            bool f = false;
            if (!DA.GetData(2, ref f)) return;
            List<Curve> lu = new List<Curve>(2);
            List<Curve> ld = new List<Curve>(2);
            #endregion
            Plane ep = new Plane(new Point3d(0, 0, e), new Vector3d(0, 0, 1));
            foreach (Curve c0 in c)
                if (c0.PointAtStart.Z > e)
                    lu.Add(c0);
                else
                    ld.Add(f ? Curve.ProjectToPlane(c0, ep) : c0);
            DA.SetDataList(0, lu);
            Cu.Add(lu.Select(t => new GH_Curve(t)).ToList());
            DA.SetDataList(1, ld);
            Cd.Add(ld.Select(t => new GH_Curve(t)).ToList());
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            Menu_Color(menu, "未淹色彩(&U)", UpColor);
            Menu_Color(menu, "淹没色彩(&F)", DownColor);
        }

        public override bool Read(GH_IReader reader)
        {
            UpColor.Value = reader.GetDrawingColor("colorup");
            DownColor.Value = reader.GetDrawingColor("colordown");
            return base.Read(reader);
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.SetDrawingColor("colorup", UpColor.Value);
            writer.SetDrawingColor("colordown", DownColor.Value);
            return base.Write(writer);
        }

        /// <summary>
        /// 水上色彩
        /// </summary>
        private GH_Colour UpColor = new GH_Colour(Color.White);
        /// <summary>
        /// 水下色彩
        /// </summary>
        private GH_Colour DownColor = new GH_Colour(Color.FromArgb(59, 104, 156));

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            if (Locked || args.Document.PreviewMode == GH_PreviewMode.Disabled) return; //跳过锁定或非线框模式
            if (!UpColor.Value.IsEmpty)
            {
                foreach (var l0 in Cu)
                    foreach (var l1 in l0)
                    {
                        bool set = Attributes.GetTopLevel.Selected;
                        if (l1.IsValid)
                            args.Display.DrawCurve(l1.Value, set ? args.WireColour_Selected : UpColor.Value);
                    }
            }
            if (DownColor.Value.IsEmpty) return;
            foreach (var l0 in Cd)
                foreach (var l1 in l0)
                {
                    bool set = Attributes.GetTopLevel.Selected;
                    if (l1.IsValid)
                        args.Display.DrawCurve(l1.Value, set ? args.WireColour_Selected : DownColor.Value);
                }
        }
        public override void BakeGeometry(RhinoDoc doc, ObjectAttributes att, List<Guid> obj_ids)
        {
            doc.BakeColorGroup(Cu, UpColor.Value, "UpWater", att, obj_ids);
            doc.BakeColorGroup(Cd, DownColor.Value, "DownWater", att, obj_ids);
        }

        private List<List<GH_Curve>> Cu = new List<List<GH_Curve>>(5);
        private List<List<GH_Curve>> Cd = new List<List<GH_Curve>>(5);
    }
    /// <summary>
    /// 山路坡度分析
    /// Contour Flood Analysis
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public class LTSA : GradientComponent
    {
        public LTSA() : base("山路坡度分析", "LTSA",
            "分析山路坡度并按角度赋予其对应色彩",
            "分析",
            ID.LTSA, 3, LTResource.山路坡度分析)
        {
            Gradient = Ty.Gradient0.Duplicate();
            ReCom = GI = true;
        }
        //todo 用户对象版 坡度范围输出端 输出错误 ，分子分母错位
        protected override void AddParameter(ParamManager pm)
        {
            pm.AddIP(ParT.Curve, "山路", "C", "要分析的山路中线（确保已投影在地形上）", ParamTrait.List);
            pm.AddIP(ParT.Integer, "精度", "E", "山路中线的细分重建密度(单位米)");

            pm.AddOP(ParT.Line, "路线", "L", "重建后用于分析的直线路线", ParamTrait.List);
            pm.AddOP(ParT.Number, "坡度", "A", "对应直线段的坡度(度)", ParamTrait.List | ParamTrait.IsAngle);
            pm.AddOP(ParT.Text, "坡度范围", "Rs", "山路直线的坡度范围（既坡高/坡长）");
            pm.AddOP(ParT.Interval, "角度范围", "Ra", "山路直线与水平面所呈角度的范围");
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            #region 输入输出变量初始化

            if (DA.Iteration == 0)
            {
                L.Clear();
                C.Clear();
            }

            List<Curve> cl = new List<Curve>(0);
            if (!DA.GetDataList(0, cl) || cl.Count == 0) return;
            int e = 0;
            if (!DA.GetData(1, ref e)) return;
            #endregion

            Line[][] la = cl.Select(t =>
                new Polyline(t.DivideByCount(
                           // (int)Math.Ceiling(t.GetLength() / e), false//获取细分数
                           (int)Math.Round(t.GetLength() / e), true
                        )//获取细分点t值
                        .Select(t.PointAt)//t值转成点
                ).GetSegments()//将点转成多段线后，再提取全部线段
            ).ToArray();

            List<Line> ll = new List<Line>(0);
            //全部的线段都摊平到一个列表里
            foreach (var t in la)
                ll.AddRange(t);

            L.Add(ll.Select(t => new GH_Line(t)).ToList());
            var v = ll.Select(t => t.Direction).ToArray();
            //向量单元化
            for (int i = 0; i < v.Length; i++)
                v[i].Unitize();
            //获取方向向量，计算角度,并保证是正的
            var a = v.Select(t => Math.Asin(t.Z < 0 ? -t.Z : t.Z) * 57.29).ToArray();

            DA.SetDataList(0, ll);
            DA.SetDataList(1, a);

            Interval ai = a.ToInterval();//获取角度区间
            double s = Math.Max(Math.Tan(ai.Max), 0.01);//避免分母太大，计算坡度较大值
            string rs = "0 to " + (s > 1 ? $"1/{Math.Round(s, 2)}" : $"{1 / s}/1");//格式化坡度范围
            DA.SetData(2, rs);
            DA.SetData(3, new Interval(Math.Round(ai.T0, 2), Math.Round(ai.T1, 2)));//格式化角度范围

            Interval interval = GI ? ai : A0;
            C.Add(a.Select(t => Dou2Col(interval, t)).ToList());
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            ToolStripMenuItem m = Menu_AppendItem(menu, "自适应角度(&A)",
                delegate
                {
                    GI = !GI;
                    ExpireSolution(true);
                }
                , null, true, GI);
            m.ToolTipText = "默认不启用，此时渐变色彩范围对应0-90º。" +
                            "\r\n启用时，范围对应实际的角度范围";
        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            if (Locked || args.Document.PreviewMode == GH_PreviewMode.Disabled) return; //跳过锁定或非线框模式
            for (int i = 0; i < L.Count; i++)
                for (int j = 0; j < L[i].Count; j++)
                    args.Display.DrawLine(L[i][j].Value, Attributes.GetTopLevel.Selected ? args.WireColour_Selected : C[i][j]);
        }
        public override void BakeGeometry(RhinoDoc doc, ObjectAttributes att, List<Guid> obj_ids)
        {
            for (int i = 0; i < L.Count; i++)
            {
                ObjectAttributes oa = att.Duplicate();
                oa.ColorSource = ObjectColorSource.ColorFromObject;
                int groupIndex = doc.Groups.Add();
                oa.AddToGroup(groupIndex);
                for (int j = 0; j < L[i].Count; j++)
                {
                    GH_Line l = L[i][j];
                    if (!l.IsValid) continue;
                    ObjectAttributes oaj = att.Duplicate();
                    oaj.ObjectColor = C[i][j];
                    Guid id = Guid.Empty;
                    l.BakeGeometry(doc, oaj, ref id);
                    obj_ids.Add(id);
                }
            }
        }
        public override void CreateAttributes()
            => m_attributes = new LTSA_Attributes(this);

        protected List<List<GH_Line>> L = new List<List<GH_Line>>(5);

        protected List<List<Color>> C = new List<List<Color>>(5);

        protected Interval A0 = new Interval(0, 90);
        /// <summary>
        /// 渐变是否自适应角度范围，否则为0-90度
        /// </summary>
        private bool _gi;
        internal bool GI
        {
            get => _gi;
            set
            {
                Message = value ? "自适应" : "0-90º";
                _gi = value;
            }
        }
    }
    /// <summary>
    /// 山路坡度分析_属性
    /// </summary>
    public class LTSA_Attributes : GH_ComponentAttributes
    {
        public LTSA_Attributes(IGH_Component component) : base(component) { }

        public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button == MouseButtons.Left && Bounds.Contains(e.CanvasLocation) && Owner is LTSA j)
            {
                j.GI = !j.GI;
                j.ExpireSolution(true);
            }

            return GH_ObjectResponse.Handled;
        }
    }
}