using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.GUI.Gradient;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace Lt.Analysis
{
    /// <summary>
    ///  电池界面 添加渐变范围
    ///
    ///视线分析 观测点的尺寸与屏幕有关 颜色
    ///淹没电池 检查颜色不对应的我那天
    /// </summary>
    public static class Ty
    {
        /// <summary>
        /// 绘制网格的预览外露边，用于重写DrawViewportWires内
        /// </summary>
        /// <param name="mesh">要绘制的网格</param>
        /// <param name="component">电池本体，默认输入this</param>
        /// <param name="args">预览变量，默认输入 args</param>
        public static void Draw1MeshE1(Mesh mesh, IGH_Component component, IGH_PreviewArgs args)
        {
            List<GH_Line> list = new List<GH_Line>();
            if(mesh==null)return;
            checked
            {///获取所有外露边
                for (int i = 0; i <= mesh.TopologyEdges.Count - 1; i++)
                {
                    int[] connectedFaces = mesh.TopologyEdges.GetConnectedFaces(i);
                    if (connectedFaces.Length == 1)
                        list.Add(new GH_Line(mesh.TopologyEdges.EdgeLine(i)));
                }
            }

            GH_PreviewWireArgs args2 = new GH_PreviewWireArgs(args.Viewport, args.Display,
                component.Attributes.GetTopLevel.Selected ? args.WireColour_Selected : args.WireColour,
                args.DefaultCurveThickness);//设置预览变量

            foreach (GH_Line t in list)//绘制线条
                t.DrawViewportWires(args2);
        }
        /// <summary>
        /// 绘制预览网格，（仅未被选中时显示伪色），用于重写DrawViewportMeshes内
        /// </summary>
        /// <param name="mesh">要绘制的网格</param>
        /// <param name="component">电池本体，默认输入this</param>
        /// <param name="args">预览变量，默认输入 args</param>
        public static void Draw1Meshes(Mesh mesh, IGH_Component component, IGH_PreviewArgs args)
        {
            bool set = component.Attributes.GetTopLevel.Selected;
            GH_PreviewMeshArgs args2 = new GH_PreviewMeshArgs(args.Viewport, args.Display, (set ? args.ShadeMaterial_Selected : args.ShadeMaterial), args.MeshingParameters);
            if (mesh == null) return;///避免网格不存在
            if (mesh.VertexColors.Count > 0 && !set)//仅存在着色且未被选取时
                args2.Pipeline.DrawMeshFalseColors(mesh);
            else
                args2.Pipeline.DrawMeshShaded(mesh, args2.Material);
        }
    }/// 通用
    public class LTFT : GH_Component
    {
        public LTFT()
          : base("地形网格淹没分析", "LTFT", "分析被水淹没后的地形状态", "Lt", "分析")
        { }//Flooded Terrain
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("地形", "Mt", "要被淹没的山地地形网格", GH_ParamAccess.item);
            pManager.AddNumberParameter("高度", "E", "淹没地形的水平面高度", GH_ParamAccess.item);
            pManager.AddColourParameter("色彩", "C", "被水淹没区域的色彩", GH_ParamAccess.item, Color.FromArgb(52, 58, 107));
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("地形", "Mf", "被水淹没后的地形网格", GH_ParamAccess.item);
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
            if (t0.Normals.Count == 0)//网格法向
                t0.Normals.ComputeNormals();
            Mesh cm = new Mesh();
            foreach (MeshFace f in t0.Faces)
                cm.Faces.AddFace(f);//把面加入新网格
            Interval ie =new Interval();
            foreach (Point3f p in t0.Vertices)
            {
                ie.Grow(p.Z);
            }//获取高度范围
            
            foreach (Point3f p in t0.Vertices)
            {
                double z = p.Z;
                if (z > e0)
                {
                    cm.Vertices.Add(p);
                    cm.VertexColors.Add(gradient.ColourAt(ie.NormalizedParameterAt(z)));
                }
                else
                {
                    cm.Vertices.Add(new Point3f(p.X, p.Y, Convert.ToSingle(e0)));
                    cm.VertexColors.Add(c0);
                }
            }//判定修正后把点和色彩加入新网格
            DA.SetData(0, cm);
            OutMesh = cm;
        }

        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            if (args.Document.PreviewMode != GH_PreviewMode.Shaded || !args.Display.SupportsShading) return;///跳过非着色模式和，或参数不支持预览
            Ty.Draw1Meshes(OutMesh, this, args);
        }
        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            if (Locked || args.Document.PreviewMode != GH_PreviewMode.Wireframe) return;//跳过锁定或非线框模式
            Ty.Draw1MeshE1(OutMesh, this, args);
        }
        private Mesh OutMesh = new Mesh();
        private GH_Gradient gradient = new GH_Gradient(
            new double[] { 0, 1 / 6, 1 / 3, 1 / 2, 2 / 3, 5 / 6, 1 },
            new[]
            {
                Color.FromArgb(45,51,87),
                Color.FromArgb(75 ,107,169),
                Color.FromArgb(173,203,249),
                Color.FromArgb(254,244,84),
                Color.FromArgb(234,126,0),
                Color.FromArgb(219,37,0),
                Color.FromArgb(138,36,36)
            }
        );
        protected override Bitmap Icon => LTResource.山体淹没分析;
        public override Guid ComponentGuid => new Guid("84474303-59cb-4248-9015-c5a02098fd99");
    }/// 地形网格淹没分析
    public class LTSD : GH_Component
    {
        public LTSD()
            : base("坡向分析", "LTSD", "山坡地形朝向分析,X轴向为东,Y轴向为北\r\n双击电池图标以切换着色模式", "Lt", "分析")
        {
            Num1 = Math.Tan(Math.PI / 8);
            Num2 = Math.Tan(Math.PI * 3 / 8);
            Shade = false;
            Message = "顶点着色";
        }
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("地形", "Mt", "要进行坡向分析的山地地形网格", GH_ParamAccess.item);
            pManager.AddColourParameter("色彩", "C", "各向的色彩，请按上、北、东北、东、东南、南、西南、西、西北的顺序连入9个色彩", GH_ParamAccess.list,
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
        }
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("地形", "Mc", "已根据坡向着色的地形网格", GH_ParamAccess.item);
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
                    }//添加顶点和顶点色
                    cm.Faces.AddFace(f.IsTriangle ? new MeshFace(fi[0], fi[1], fi[2]) : new MeshFace(fi[0], fi[1], fi[2], fi[3]));
                }
            }
            else
            {
                cm = t0.DuplicateMesh();
                foreach (Vector3f v in t0.Normals)
                    cm.VertexColors.Add(c[DShade(v)]);
            }
            DA.SetData(0, cm);
            OutMesh = cm;
        }
        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            if (args.Document.PreviewMode != GH_PreviewMode.Shaded || !args.Display.SupportsShading) return;///跳过非着色模式和，或参数不支持预览
            Ty.Draw1Meshes(OutMesh, this, args);
        }
        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            if (Locked || args.Document.PreviewMode != GH_PreviewMode.Wireframe) return;//跳过锁定或非线框模式
            Ty.Draw1MeshE1(OutMesh, this, args);
        }
        private Mesh OutMesh = new Mesh();
        public int DShade(Vector3f v)
        {
            if (v.X == 0 && v.Y == 0)//上方
                return 0;
            double t = Math.Abs(v.Y / v.X);
            bool x = v.X > 0;
            bool y = v.Y > 0;
            if (t < Num1)//东西
                return x ? 3 : 7;
            if (t > Num2)//北南
                return y ? 1 : 5;
            if (x)//东北、东南
                return y ? 2 : 4;
            return y ? 8 : 6;
            //西北、西南
        }
        public override void CreateAttributes()
        {
            m_attributes = new LTSD_Attributes(this);
        }
        public override bool Write(GH_IWriter writer)
        {
            writer.SetBoolean("面色否", Shade);
            return true;
        }
        public override bool Read(GH_IReader reader)
        {
            if (!reader.ItemExists("面色否")) return false;
            Shade = reader.GetBoolean("面色否");
            return true;
        }
        private double Num1;
        private double Num2;
        public bool Shade;
        protected override Bitmap Icon => LTResource.山体坡向分析;
        public override Guid ComponentGuid => new Guid("3d3ee5a9-c86e-4007-97c6-eb33aa365e27");
    }/// 坡向分析
    public class LTSD_Attributes : GH_ComponentAttributes
    {
        public LTSD_Attributes(IGH_Component component) : base(component)
        { }
        public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button == MouseButtons.Left && Bounds.Contains(e.CanvasLocation) && Owner is LTSD j)
            {
                j.Shade = !j.Shade;
                j.Message = j.Shade ? "面着色" : "顶点着色";
                j.ExpireSolution(true);
            }
            return GH_ObjectResponse.Handled;
        }
    }/// 坡向分析_属性
    public class LTTG : GH_Component
    {
        public LTTG() : base("坡度分析", "LTTG", "山地地形坡度分析", "Lt", "分析")
        { }//Terrain Grade
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("地形", "Mt", "要进行坡度分析的山地地形网格", GH_ParamAccess.item);
        }
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("地形", "M", "已按角度着色的地形网格", GH_ParamAccess.item);
            pManager.AddIntervalParameter("角度", "A", "坡度范围（度）", GH_ParamAccess.item);
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
                cm.VertexColors.Add(gradient.ColourAt(ia.NormalizedParameterAt(a)));
            DA.SetData(0, cm);
            DA.SetData(1, ia);
            OutMesh = cm;
        }
        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            if (args.Document.PreviewMode != GH_PreviewMode.Shaded || !args.Display.SupportsShading) return;///跳过非着色模式和，或参数不支持预览
            Ty.Draw1Meshes(OutMesh, this, args);
        }
        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            if (Locked || args.Document.PreviewMode != GH_PreviewMode.Wireframe) return;//跳过锁定或非线框模式
            Ty.Draw1MeshE1(OutMesh, this, args);
        }
        private Mesh OutMesh = new Mesh();
        public static GH_Gradient gradient = new GH_Gradient(
            new[] { 0, 0.2, 0.4, 0.6, 0.8, 1 },
            new[]
            {
             Color.FromArgb(45,51,87),
             Color.FromArgb(75 ,107,169),
             Color.FromArgb(173,203,249),
             Color.FromArgb(254,244,84),
             Color.FromArgb(234,126,0),
             Color.FromArgb(237,53,17)
            }
        );
        protected override Bitmap Icon => LTResource.山体坡度分析;
        public override Guid ComponentGuid => new Guid("6c33fb8b-9da6-4688-8a1b-d0363350d176");
    }/// 坡度分析
    public class LTTE : GH_Component
    {
        public LTTE() : base("高程分析", "LTTE", "山地地形高程分析", "Lt", "分析")
        { }//Terrain Grade
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("地形", "Mt", "要进行坡度分析的山地地形网格", GH_ParamAccess.item);
        }
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("地形", "M", "已按海拔着色的地形网格", GH_ParamAccess.item);
            pManager.AddIntervalParameter("海拔", "E", "海拔范围（两位小数）", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh tm = new Mesh();
            if (!DA.GetData(0, ref tm))
                return;
            List<double> re = new List<double>(3);
            Interval ie = new Interval();
            foreach (Point3f p in tm.Vertices)
            {
                double e = Math.Round(p.Z, 2);
                ie.Grow(e);
                re.Add(e);
            }
            Mesh cm = tm.DuplicateMesh();
            foreach (double e in re)
                cm.VertexColors.Add(LTTG.gradient.ColourAt(ie.NormalizedParameterAt(e)));
            DA.SetData(0, cm);
            DA.SetData(1, ie);
            OutMesh = cm;
        }
        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            if (args.Document.PreviewMode != GH_PreviewMode.Shaded || !args.Display.SupportsShading) return;///跳过非着色模式和，或参数不支持预览
            Ty.Draw1Meshes(OutMesh, this, args);
        }
        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            if (Locked || args.Document.PreviewMode != GH_PreviewMode.Wireframe) return;//跳过锁定或非线框模式
            Ty.Draw1MeshE1(OutMesh, this, args);
        }
        private Mesh OutMesh=new Mesh();
        protected override Bitmap Icon => LTResource.山体高程分析;
        public override Guid ComponentGuid => new Guid("1afc0549-5308-47ef-b91c-a2eade694250");
    }/// 高程分析
    public class LTVL : GH_Component
    {
        public LTVL() : base("视线分析", "LTVL", "分析在山地某处的可见范围,cpu线程数大于2时自动调用多核计算", "Lt", "分析")
        { }//Terrain Grade
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("地形", "Mt", "要进行坡度分析的山地地形网格,仅支持单项数据", GH_ParamAccess.item);
            pManager.AddMeshParameter("障碍物", "O", "阻挡视线的障碍物体，,仅支持单列数据", GH_ParamAccess.list);
            pManager.AddPointParameter("观察点", "P", "观察者所在的点位置（不一定在网格上），支持多点观察,仅支持单列数据", GH_ParamAccess.list);
            pManager.AddIntegerParameter("精度", "A", "分析精度，即分析点阵内的间距,仅支持单项数据", GH_ParamAccess.item);
        }
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("观察点", "O", "观察者视点位置（眼高1m5）", GH_ParamAccess.list);
            pManager.AddPointParameter("可见点", "V", "被看见的点", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (DA.Iteration > 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,"超出数据已被忽视");
                return;
            }
            Mesh tm = new Mesh();
            if (!DA.GetData(0, ref tm) && !tm.IsValid) return;

            List<Mesh> o = new List<Mesh>(3);
            if (!DA.GetDataList(1, o))return;

            List<Point3d> pl = new List<Point3d>();
            if (!DA.GetDataList(2, pl))return;

            int a = 0;
            if (!DA.GetData(3, ref a))return;

            #region 制作网格上用于测量的点阵

            BoundingBox mb = new BoundingBox();
            foreach (Point3f p3f in tm.Vertices)
                mb.Union(new Point3d(p3f));
            double px = mb.Min.X;
            double py = mb.Min.Y;
            int rx = (int) Math.Ceiling((mb.Max.X - px) / a);
            int ry = (int) Math.Ceiling((mb.Max.Y - py) / a);
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
            }//采样点
            #endregion
            foreach (Mesh m in o) //将障碍物附加入网格
                tm.Append(m);

            bool[] grib = new bool[grid.Count];//采样点布尔数组
            
            foreach (Point3d p in pt)
                if (Environment.ProcessorCount>2)
                {
                    Parallel.For(0, grid.Count, j =>//多线程处理每个采样点  
                    {
                        if (!grib[j]) //grib为否(尚未可见)时 判定当前是否为可见点，与网格上的点阵交点仅为1的即可见点
                            grib[j] = Intersection.MeshLine(tm, new Line(p, grid[j]), out _).Length == 1;
                    });
                }
                else
                {
                    for (int j=0;j< grid.Count;j++)
                    {
                        if (!grib[j]) //grib为否(尚未可见)时 判定当前是否为可见点，与网格上的点阵交点仅为1的即可见点
                            grib[j] = Intersection.MeshLine(tm, new Line(p, grid[j]), out _).Length == 1;
                    }
                }
                

            for (int i = grid.Count - 1; i >= 0; i--)//剔除不可见点
                if (!grib[i])
                    grid.RemoveAt(i);


            DA.SetDataList(0, pt);
            DA.SetDataList(1, grid);
        }
        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            IGH_Param igh_Param = Params.Output[1];//可见点
            IGH_PreviewObject igh_PreviewObject = (IGH_PreviewObject)igh_Param;
            if (igh_PreviewObject.Hidden || !igh_PreviewObject.IsPreviewCapable) return;//电池隐藏或不可预览时跳过
            igh_PreviewObject.DrawViewportWires(args);//可见点
            
            List<Point3d> pp = Params.Output[0].VolatileData.get_Branch(0) as List<Point3d>;
            int k =  ((GH_Structure<GH_Integer>) Params.Input[3].VolatileData).get_FirstItem(false).Value;
            args.Display.DrawPoints(pp, CentralSettings.PreviewPointStyle, (int)(k * 0.8), Color.CornflowerBlue);
            //观察点
            
        }
        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
        }
        protected override Bitmap Icon => LTResource.视线分析;
        public override Guid ComponentGuid => new Guid("f5b0968b-4de5-45a6-a82b-744f64787e85");
        public override GH_Exposure Exposure => GH_Exposure.quarternary;
    }/// 视线分析
}
