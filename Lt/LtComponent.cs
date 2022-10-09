using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Base;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using Lt.Majas;

namespace Lt.Analysis
{
    //todo 电池名和简称修改统一
    //todo 给渐变的电池 加色彩标尺


    /// <summary>
    /// 网格淹没分析
    /// Flooded Terrain
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public class LTMF : GradientComponent
    {
        public LTMF() : base(
            "网格淹没分析", "LTMF",
            "分析被水淹没后的地形状态",
            "分析",
            "84474303-59cb-4248-9015-c5a02098fd99", 1, LTResource.山体淹没分析)
        { Gradient = Ty.Gradient0; }

        protected override void AddParameter(ParamManager pm)
        {
            pm.AddIP(ParT.Mesh, "网格", "M", "要被淹没的山地地形网格");
            pm.AddIP(ParT.Number, "高度", "E", "淹没地形的水平面高度");
            pm.AddIP(ParT.Colour, "色彩", "C", "被水淹没区域的色彩", def: Color.FromArgb(52, 58, 107));

            pm.AddOP(ParT.Mesh, "网格", "M", "被水淹没后的地形网格");
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
            Color c0 = new Color();
            if (!DA.GetData(2, ref c0))
                return;

            #endregion

            if (t0.Normals.Count == 0) //网格法向
                t0.Normals.ComputeNormals();
            Mesh cm = new Mesh();
            foreach (MeshFace f in t0.Faces)
                cm.Faces.AddFace(f); //把面加入新网格
            BoundingBox b = t0.GetBoundingBox(false);
            Interval ie = new Interval(b.Min.Z, b.Max.Z); //获取高度范围
            foreach (Point3f p in t0.Vertices)
            {
                double z = p.Z;
                if (z > e0)
                {
                    cm.Vertices.Add(p);
                    cm.VertexColors.Add(Gradient.ColourAt(ie.NormalizedParameterAt(z)));
                }
                else
                {
                    cm.Vertices.Add(new Point3f(p.X, p.Y, Convert.ToSingle(e0)));
                    cm.VertexColors.Add(c0);
                }
            } //判定修正后把点和色彩加入新网格

            DA.SetData(0, cm);
        }
        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            if (args.Document.PreviewMode != GH_PreviewMode.Shaded || Hidden || !args.Display.SupportsShading)
                return; ///跳过非着色模式和，或参数不支持预览
            Ty.Draw1Meshes(0, this, args);
        }
    }
    /// <summary>
    /// 网格坡向分析
    /// Slope Direction Analysis
    /// </summary>
    public class LTMD : AComponent
    {
        public LTMD()
            : base("网格坡向分析", "LTMD",
                "山坡地形朝向分析,X轴向为东,Y轴向为北\r\n双击电池图标以切换着色模式",
                "分析",
                "3d3ee5a9-c86e-4007-97c6-eb33aa365e27", 1, LTResource.山体坡向分析)
        {
            Shade = true;
            Message = "面着色";
        }
        protected override void AddParameter(ParamManager pm)
        {
            pm.AddIP(ParT.Mesh, "网格", "M", "要进行坡向分析的山地地形网格");
            pm.AddIP(ParT.Colour, "色彩", "C",
                "各向的色彩，请按上、北、东北、东、东南、南、西南、西、西北的顺序连入9个色彩", ParamTrait.List,
                new List<Color>
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

            Mesh cm = new Mesh();
            if (Shade)
            {
                if (t0.FaceNormals.Count != t0.Faces.Count)
                    t0.FaceNormals.ComputeFaceNormals();
                int numi = 0;
                int[] fi = { 0, 0, 0, 0 };
                for (var i = 0; i < t0.Faces.Count; i++)
                {
                    MeshFace f = t0.Faces[i];
                    Color c1 = c[DShade(t0.FaceNormals[i])];
                    for (int j = 0; j < (f.IsTriangle ? 3 : 4); j++)
                    {
                        fi[j] = numi;
                        cm.Vertices.Add(t0.Vertices[f[j]]);
                        cm.VertexColors.Add(c1);
                        numi++;
                    } //添加顶点和顶点色

                    cm.Faces.AddFace(f.IsTriangle
                        ? new MeshFace(fi[0], fi[1], fi[2])
                        : new MeshFace(fi[0], fi[1], fi[2], fi[3]));
                }
            }
            else
            {
                cm = t0.DuplicateMesh();
                foreach (Vector3f v in t0.Normals)
                    cm.VertexColors.Add(c[DShade(v)]);
            }

            DA.SetData(0, cm);
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
            => Menu_AppendItem(menu, "使用面着色", Event, null, true, Shade);

        //debug
        public void Event(object sender, EventArgs e)
        {
            Shade = !Shade;
            ((ToolStripMenuItem)sender).Checked = Shade;
        }

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
        =>  m_attributes = new LTMD_Attributes(this);

        public override bool Write(GH_IWriter writer)
        {
            base.Write(writer);
            writer.SetBoolean("面色否", Shade);
            return true;
        }

        public override bool Read(GH_IReader reader)
        {
            base.Read(reader);
            if (!reader.ItemExists("面色否")) return false;
            Shade = reader.GetBoolean("面色否");
            return true;
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
        public LTMG() : base("网格坡度分析", "LTMG",
            "山地地形坡度分析",
            "分析",
            "6c33fb8b-9da6-4688-8a1b-d0363350d176", 1, LTResource.山体坡度分析)
        { Gradient = Ty.Gradient0; }
        protected override void AddParameter(ParamManager pm)
        {
            pm.AddIP(ParT.Mesh, "网格", "M", "要进行坡度分析的山地地形网格");

            pm.AddOP(ParT.Mesh, "网格", "M", "已按角度着色的地形网格");
            //debug 此处带验证 数据会不会被转换
            pm.AddOP(ParT.Angle, "角度", "A", "坡度范围（度）", ParamTrait.Item | ParamTrait.IsAngle);
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh tm = new Mesh();
            if (!DA.GetData(0, ref tm))
                return;
            List<double> ra = new List<double>(3);
            Interval ia = new Interval();
            foreach (Vector3f v in tm.Normals)
            {
                double a = Math.Acos(v.Z) * 180 / Math.PI;
                ia.Grow(a);
                ra.Add(a);
            }

            Mesh cm = tm.DuplicateMesh();
            foreach (double a in ra)
                cm.VertexColors.Add(Gradient.ColourAt(ia.NormalizedParameterAt(a)));
            DA.SetData(0, cm);
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
        public LTME() : base("高程分析", "LTME",
            "山地地形高程分析",
            "分析",
            "1afc0549-5308-47ef-b91c-a2eade694250", 1, icon: LTResource.山体高程分析)
        { Gradient = Ty.Gradient0; }
        protected override void AddParameter(ParamManager pm)
        {
            pm.AddIP(ParT.Mesh, "网格", "M", "要进行坡度分析的山地地形网格");

            pm.AddOP(ParT.Mesh, "网格", "M", "已按海拔着色的地形网格");
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
                cm.VertexColors.Add(Gradient.ColourAt(ie.NormalizedParameterAt(p.Z)));
            ie.T0 = Math.Round(ie.T0, 2);
            ie.T1 = Math.Round(ie.T1, 2);
            DA.SetData(0, cm);
            DA.SetData(1, ie);
        }
        public override bool Write(GH_IWriter writer)
        {
            base.Write(writer);
            return writer.SetGradient("渐变", Gradient);
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
    // bug 点与封包版本不一致
    public class LTVL : AComponent
    {
        public LTVL() : base("视线分析", "LTVL",
            "分析在山地某处的可见范围,cpu线程数大于2时自动调用多核计算",
            "分析",
            "f5b0968b-4de5-45a6-a82b-744f64787e85", 4, LTResource.视线分析)
        { }
        protected override void AddParameter(ParamManager pm)
        {
            pm.AddIP(ParT.Mesh, "地形", "Mt", "要进行坡度分析的山地地形网格,仅支持单项数据", ParamTrait.Item | ParamTrait.OnlyOne);
            pm.AddIP(ParT.Mesh, "障碍物", "O", "（可选）阻挡视线的障碍物体，,仅支持单列数据", ParamTrait.List | ParamTrait.OnlyOne | ParamTrait.Optional);
            pm.AddIP(ParT.Point, "观察点", "P", "观察者所在的点位置（不一定在网格上），支持多点观察,仅支持单列数据", ParamTrait.List | ParamTrait.OnlyOne);
            pm.AddIP(ParT.Integer, "精度", "A", "分析精度，即分析点阵内的间距,仅支持单项数据", ParamTrait.Item | ParamTrait.OnlyOne);

            pm.AddOP(ParT.Point, "观察点", "O", "观察者视点位置（眼高1m5）", ParamTrait.List);
            pm.AddOP(ParT.Point, "可见点", "V", "被看见的点", ParamTrait.List);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
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

            #region 制作网格上用于测量的点阵

            BoundingBox mb = new BoundingBox();
            foreach (Point3f p3f in tm.Vertices)
                mb.Union(new Point3d(p3f));
            double px = mb.Min.X;
            double py = mb.Min.Y;
            int rx = (int)Math.Ceiling((mb.Max.X - px) / a);
            int ry = (int)Math.Ceiling((mb.Max.Y - py) / a);
            List<Point3d> grid = new List<Point3d>(rx * ry);
            for (int ix = 0; ix < rx; ix++)
            {
                for (int iy = 0; iy < ry; iy++)
                {
                    Ray3d ray = new Ray3d(new Point3d(px, py, mb.Min.Z), Vector3d.ZAxis);
                    double pmd = Intersection.MeshRay(tm, ray);
                    if (RhinoMath.IsValidDouble(pmd) && pmd > 0.0)
                        grid.Add(ray.PointAt(pmd));
                    py += a;
                }

                px += a;
                py = mb.Min.Y;
            } //获取网格上点阵制作栅格点阵z射线,与网格相交，交点加入grid

            #endregion

            List<Point3d> pt = new List<Point3d>(pl.Count);

            #region 将观测点投影到地形网格上，并增加眼高

            foreach (Point3d p0 in pl)
            {
                Point3d p = p0;

                Ray3d rayp = new Ray3d(new Point3d(p.X, p.Y, mb.Min.Z), Vector3d.ZAxis);
                double pmdp = Intersection.MeshRay(tm, rayp);
                if (RhinoMath.IsValidDouble(pmdp) && pmdp > 0.0)
                    p = rayp.PointAt(pmdp);
                else
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"观测点{p}无法被Z向投影至地形网格上");
                    return;
                }

                p.Z += 1.5; //增加眼高
                pt.Add(p);
            } //采样点

            #endregion

            foreach (Mesh m in o) //将障碍物附加入网格
                tm.Append(m);

            bool[] grib = new bool[grid.Count]; //采样点布尔数组

            foreach (Point3d p in pt)
                if (Environment.ProcessorCount > 2)
                {
                    Parallel.For(0, grid.Count, j => //多线程处理每个采样点  
                    {
                        if (!grib[j]) //grib为否(尚未可见)时 判定当前是否为可见点，与网格上的点阵交点仅为1的即可见点
                            grib[j] = Intersection.MeshLine(tm, new Line(p, grid[j]), out _).Length == 1;
                    });
                }
                else
                {
                    for (int j = 0; j < grid.Count; j++)
                    {
                        if (!grib[j]) //grib为否(尚未可见)时 判定当前是否为可见点，与网格上的点阵交点仅为1的即可见点
                            grib[j] = Intersection.MeshLine(tm, new Line(p, grid[j]), out _).Length == 1;
                    }
                }


            for (int i = grid.Count - 1; i >= 0; i--) //剔除不可见点
                if (!grib[i])
                    grid.RemoveAt(i);


            DA.SetDataList(0, pt);
            DA.SetDataList(1, grid);
        }
        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            Menu_AppendColourPicker(Menu_AppendItem(menu, "观察点色彩").DropDown, ColorO, OChanged);
            Menu_AppendColourPicker(Menu_AppendItem(menu, "可见点色彩").DropDown, ColorV, VChanged);
        }
        public void OChanged(GH_ColourPicker sender, GH_ColourPickerEventArgs e)
            => ColorO = e.Colour;
        public void VChanged(GH_ColourPicker sender, GH_ColourPickerEventArgs e)
            => ColorV = e.Colour;

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            if (Hidden || !IsPreviewCapable) return; //电池隐藏或不可预览时跳过
            GH_Structure<GH_Integer> pk = (GH_Structure<GH_Integer>)Params.Input[3].VolatileData;
            int k = (pk).get_FirstItem(false).Value; //获取精度

            //观察点
            var Pt = Params.Output[0].VolatileData.AllData(true).Select(t => ((GH_Point)t).Value);
            args.Display.DrawPoints(Pt, CentralSettings.PreviewPointStyle, k,
                Attributes.GetTopLevel.Selected ? args.WireColour_Selected : ColorO);
            //可见点
            var Grid = Params.Output[1].VolatileData.AllData(true).Select(t => ((GH_Point)t).Value);
            args.Display.DrawPoints(Grid, CentralSettings.PreviewPointStyle, (int)(k * 0.4),
                Attributes.GetTopLevel.Selected ? args.WireColour_Selected : ColorV);

        }

        public override void DrawViewportMeshes(IGH_PreviewArgs args) { }


        private static Color ColorO = Color.Red;
        private static Color ColorV = Color.Aquamarine;
    }
    /// <summary>
    /// 等高线高程分析
    /// Contour Line Elevation Analysis
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    //todo 【等高线高程】增加 等高线高程 输出每条线的高度，色彩数据存入字段（运算前清除）、渲染和烘焙改写
    public class LTCE : GradientComponent
    {
        public LTCE() : base("等高线高程分析", "LTCE",
            "分析等高线的高程，并获得其可视化色彩。可直接烘焙出已着色曲线",
            "分析",
            "b8ddf076-9287-450e-833f-597f81bac1a4", 2, LTResource.等高线高程分析)
        { Gradient = Ty.Gradient0; }
        protected override void AddParameter(ParamManager pm)
        {
            pm.AddIP(ParT.Curve, "等高线", "C", "待分析的等高线，请自行确保输入的都是水平曲线", ParamTrait.List);

            pm.AddOP(ParT.Colour, "色彩", "C", "输入曲线高程的映射色彩", ParamTrait.List);
            pm.AddOP(ParT.Interval, "范围", "R", "输入高程线的高程范围");
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Curve> g = new List<Curve>(3);
            if (!DA.GetDataList(0, g)) return;
            List<Color> c = new List<Color>(g.Count);
            List<double> cd = new List<double>(g.Count);
            Interval r = new Interval();
            foreach (var cz in g.Select(ci => ci.PointAtEnd.Z))
            {
                r.Grow(cz);
                cd.Add(cz);
            }

            DA.SetData(1, r);
            c.AddRange(cd.Select(d => Gradient.ColourAt(r.NormalizedParameterAt(d))));
            DA.SetDataList(0, c);
        }
        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            if (Locked || args.Document.PreviewMode == GH_PreviewMode.Disabled) return; //跳过锁定或非线框模式
            Color[] colour = Params.Output[0].VolatileData.AllData(true).Select(t => ((GH_Colour)t).Value).ToArray();
            Curve[] curve = Params.Input[0].VolatileData.AllData(true).Select(t => ((GH_Curve)t).Value).ToArray();
            for (int i = 0; i < colour.Length; i++)
            {
                Curve cu = curve[i];
                if (cu.IsValid)
                    args.Display.DrawCurve(cu, Attributes.GetTopLevel.Selected ? args.WireColour_Selected : colour[i]);
            }
        }

        public override void BakeGeometry(RhinoDoc doc, ObjectAttributes att, List<Guid> obj_ids)
        {
            var colour = Params.Output[0].VolatileData.AllData(true).Select(t => ((GH_Colour)t).Value).ToArray();
            var curve = Params.Input[0].VolatileData.AllData(true).Select(t => ((GH_Curve)t)).ToArray();
            for (var i = 0; i < curve.Length; i++)
            {
                GH_Curve c = curve[i];
                if (!c.IsValid)
                    continue;
                ObjectAttributes oa = att.Duplicate();
                oa.ColorSource = ObjectColorSource.ColorFromObject;
                oa.ObjectColor = colour[i];
                Guid id = Guid.Empty;
                c.BakeGeometry(doc, oa, ref id);
                obj_ids.Add(id);
            }
        }
    }
    /// <summary>
    /// 等高线淹没分析
    /// Contour Flood Analysis
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    //todo 【等高线淹没分析】 右键选择设置 水上色 水下色， 烘焙按水上水下分组，
    public class LTCF : AComponent
    {
        public LTCF() : base("等高线淹没分析", "LTCF",
            "通过等高线数据分析地形的淹没情况。可直接烘焙出已着色曲线",
            "分析",
            "8dd68005-d905-4990-a802-cfd30413f836", 2, LTResource.等高线淹没分析)
        { }
        protected override void AddParameter(ParamManager pm)
        {
            pm.AddIP(ParT.Curve, "等高线", "C", "要进行淹没分析的等高线", ParamTrait.List);
            pm.AddIP(ParT.Integer, "高程", "E", "水面的高程");
            pm.AddIP(ParT.Boolean, "摊平", "F", "是否要将水下等高线摊平到水平面，默认为false", def: false);
            pm.AddIP(ParT.Colour, "水上色", "Cu", "未淹没区等高线的色彩", def: Color.White);
            pm.AddIP(ParT.Colour, "水下色", "Cd", "被淹没区等高线的色彩", def: Color.FromArgb(59, 104, 156));

            pm.AddOP(ParT.Curve, "水上线", "Cu", "未淹没区的等高线", ParamTrait.List);
            pm.AddOP(ParT.Curve, "水下线", "Cd", "被淹没区的等高线", ParamTrait.List);
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (DA.Iteration > 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "本电池仅运算一次，更多数据已被忽略");
                return;
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
            DA.SetDataList(1, ld);
        }
        public override void DrawViewportWires(IGH_PreviewArgs args)
        {//todo 这些循环是怎么回事 判断一下输入端是否应该只输入一种颜色
            if (Locked || args.Document.PreviewMode == GH_PreviewMode.Disabled) return; //跳过锁定或非线框模式
            var cu = Params.Input[3].VolatileData.AllData(true).Select(t => ((GH_Colour)t).Value).ToArray();
            var lu = Params.Output[0].VolatileData.AllData(true).Select(t => ((GH_Curve)t).Value).ToArray();
            var cd = Params.Input[4].VolatileData.AllData(true).Select(t => ((GH_Colour)t).Value).ToArray();
            var ld = Params.Output[1].VolatileData.AllData(true).Select(t => ((GH_Curve)t).Value).ToArray();
            if (lu.Length != 0 && cu.Length != 0)
            {
                foreach (Curve l in lu)
                {
                    bool set = Attributes.GetTopLevel.Selected;
                    if (l.IsValid)
                        args.Display.DrawCurve(l, set ? args.WireColour_Selected : cu[0]);
                }
            }
            if (ld.Length == 0 || cd.Length == 0) return;
            foreach (Curve l in ld)
            {
                bool set = Attributes.GetTopLevel.Selected;
                if (l.IsValid)
                    args.Display.DrawCurve(l, set ? args.WireColour_Selected : cd[0]);
            }
        }
        public override void DrawViewportMeshes(IGH_PreviewArgs args) { }
        public override void BakeGeometry(RhinoDoc doc, ObjectAttributes att, List<Guid> obj_ids)
        {
            List<GH_Colour> cu = ((GH_Structure<GH_Colour>)Params.Input[3].VolatileData).FlattenData();
            List<GH_Curve> lu = ((GH_Structure<GH_Curve>)Params.Output[0].VolatileData).FlattenData();
            List<GH_Colour> cd = ((GH_Structure<GH_Colour>)Params.Input[4].VolatileData).FlattenData();
            List<GH_Curve> ld = ((GH_Structure<GH_Curve>)Params.Output[1].VolatileData).FlattenData();
            if (lu.Count != 0 && cu.Count != 0)
            {
                ObjectAttributes oa = att.Duplicate();
                int groupIndex = doc.Groups.Add();
                oa.AddToGroup(groupIndex);
                oa.ColorSource = ObjectColorSource.ColorFromObject;
                oa.ObjectColor = cu[0].Value;
                foreach (GH_Curve c in lu)
                {
                    if (!c.IsValid)
                        continue;
                    Guid id = Guid.Empty;
                    c.BakeGeometry(doc, oa, ref id);
                    obj_ids.Add(id);
                }
            }

            if (ld.Count != 0 && cd.Count != 0)
            {
                ObjectAttributes oa = att.Duplicate();
                int groupIndex = doc.Groups.Add();
                oa.AddToGroup(groupIndex);
                oa.ColorSource = ObjectColorSource.ColorFromObject;
                oa.ObjectColor = cd[0].Value;
                foreach (GH_Curve c in ld)
                {
                    if (!c.IsValid)
                        continue;
                    Guid id = Guid.Empty;
                    c.BakeGeometry(doc, oa, ref id);
                    obj_ids.Add(id);
                }
            }
        }
    }
    /// <summary>
    /// 山路坡度分析
    /// Contour Flood Analysis
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    //debug 和写
    //bug 渐变颜色不对应， 角度范围 坡度范围都不对
    //todo 右键设置渐变
    //todo 右键设置渐变范围 最大值到最小值 或0-90度
    //todo 坡度输出端的 数据 还没改  请检查
    public class LTSA : GradientComponent
    {
        public LTSA() : base("山路坡度分析", "LTSA",
            "分析山路坡度并按角度赋予其对应色彩",
            "分析",
            "525ba474-eb6b-43f9-a8c4-b9594f4b33cf", 3, LTResource.山路坡度分析)
        { Gradient = Ty.Gradient0; }

        protected override void AddParameter(ParamManager pm)
        {
            pm.AddIP(ParT.Curve, "山路", "C", "要分析的山路中线（确保已投影在地形上）");
            pm.AddIP(ParT.Integer, "精度", "E", "山路中线的细分重建密度，米每点");

            pm.AddOP(ParT.Line, "路线", "L", "重建后用于分析的直线路线", ParamTrait.Tree);
            pm.AddOP(ParT.Number, "坡度", "A", "对应直线段的坡度", ParamTrait.Tree);
            pm.AddOP(ParT.Text, "坡度范围", "Rs", "山路直线的坡度范围（既坡高/坡长）");
            pm.AddOP(ParT.Interval, "角度范围", "Ra", "山路直线与水平面所呈角度的范围");
        }

        protected override void BeforeSolveInstance()
        {
            int c = Params.Input[0].VolatileDataCount;
            C = new List<List<Color>>(c);
            L = new List<List<Line>>(c);
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            #region 输入输出变量初始化
            int iter = DA.Iteration;
            L.Add(null);
            C.Add(null);
            Curve c = null;
            if (!DA.GetData(0, ref c) || c == null) return;
            int e = 0;
            if (!DA.GetData(1, ref e)) return;
            #endregion
            int count = (int)Math.Ceiling(c.GetLength() / e);
            Point3d[] p = c.DivideEquidistant(c.GetLength() / count);
            int pc = p.Length - 1;//段数
            List<GH_Line> l = new List<GH_Line>(pc);
            List<GH_Number> a = new List<GH_Number>(pc);
            L[iter] = new List<Line>(pc);
            C[iter] = new List<Color>(pc);
            for (int i = 0; i < pc; i++)
            {
                Line li = new Line(p[i], p[i + 1]);
                l.Add(new GH_Line(li));
                L[iter].Add(li);
                Vector3d v = p[i + 1] - p[i];
                double ai = Math.Acos(v.Z) * 180 / Math.PI;//计算角度
                a.Add(new GH_Number(ai));
                A1.Grow(ai);
                C[iter].Add(Gradient.ColourAt(A0.NormalizedParameterAt(ai))); //计算并添加色彩
            }
            DA.SetDataList(0, l);
            DA.SetDataList(1, a);
        }

        protected override void AfterSolveInstance()
        {
            double s = Math.Max(Math.Tan(A1.Max), 0.01);//避免分母太大
            string rs = $"0 to 1/{Math.Round((1 / s), 2)}";
            IGH_Param o2 = Params.Output[2];
            o2.ClearData();
            o2.AddVolatileData(new GH_Path(0), 0, rs);
            IGH_Param o3 = Params.Output[3];
            o3.ClearData();
            o3.AddVolatileData(new GH_Path(0), 0, A1);
        }
        public override bool Write(GH_IWriter writer)
        {
            base.Write(writer);
            return writer.SetGradient("渐变", Gradient);
        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            if (Locked || args.Document.PreviewMode == GH_PreviewMode.Disabled) return; //跳过锁定或非线框模式
            if (L == null || L.Count == 0) return;
            for (int i = 0; i < L.Count; i++)
            {
                List<Line> li = L[i];
                List<Color> ci = C[i];
                if (li.Count == 0) continue;
                for (int j = 0; j < li.Count; j++)
                    args.Display.DrawLine(li[j], Attributes.GetTopLevel.Selected ? args.WireColour_Selected : ci[j]);
            }
        }
        public override void BakeGeometry(RhinoDoc doc, ObjectAttributes att, List<Guid> obj_ids)
        {
            IList<List<GH_Line>> lt = ((GH_Structure<GH_Line>)Params.Output[0].VolatileData).Branches;
            if (lt.Count == 0) return;
            for (int i = 0; i < lt.Count; i++)
            {
                ObjectAttributes oa = att.Duplicate();
                oa.ColorSource = ObjectColorSource.ColorFromObject;
                int groupIndex = doc.Groups.Add();
                oa.AddToGroup(groupIndex);
                for (int j = 0; j < lt[i].Count; j++)
                {
                    GH_Line l = lt[i][j];
                    if (!l.IsValid) continue;
                    ObjectAttributes oaj = att.Duplicate();
                    oaj.ObjectColor = C[i][j];
                    Guid id = Guid.Empty;
                    l.BakeGeometry(doc, oaj, ref id);
                    obj_ids.Add(id);
                }
            }
        }

        protected List<List<Line>> L;
        protected List<List<Color>> C;
        protected Interval A0 = new Interval(0, 90);
        protected Interval A1;
    }
}
