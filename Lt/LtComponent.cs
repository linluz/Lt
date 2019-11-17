using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.GUI.Gradient;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Rhino;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace Lt.Analysis
{/// <summary>
 ///  电池界面 添加渐变范围、关闭预览中的网格线部分
 /// </summary>
 public class LTFT : GH_Component
    {
        public LTFT()
          : base("地形网格淹没分析", "LTFT","分析被水淹没后的地形状态","Lt", "分析")
        {}//Flooded Terrain
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("地形", "Mt", "要被淹没的山地地形网格", GH_ParamAccess.item);
            pManager.AddNumberParameter("高度", "E", "淹没地形的水平面高度", GH_ParamAccess.item);
            pManager.AddColourParameter("色彩", "C", "被水淹没区域的色彩", GH_ParamAccess.item,Color.FromArgb(52,58,107));
        }
        
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("地形", "Mf", "被水淹没后的地形网格", GH_ParamAccess.item);
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            #region 初始化 获取输入
            Mesh t0= new Mesh();
            if (!DA.GetData(0, ref t0))
                return;
            double e0 = 0;
            if (!DA.GetData(1, ref e0))
                return;
            Color c0=new Color();
            if (!DA.GetData(2, ref c0))
                return;
            #endregion
            if (t0.Normals.Count == 0)//网格法向
                t0.Normals.ComputeNormals();
            Mesh t1 = new Mesh();
            foreach (MeshFace f in t0.Faces)
                t1.Faces.AddFace(f);//把面加入新网格
            double min = double.MaxValue;
            double max = double.MinValue;
            foreach (Point3f p in t0.Vertices)
            {
                double z = p.Z;
                min = Math.Min(min, z);
                max = Math.Max(max, z);
            }//获取高度范围
            double range = max - min;
            foreach (Point3f p in t0.Vertices)
            {
                double z = p.Z;
                if (z > e0)
                {
                    t1.Vertices.Add(p);
                    t1.VertexColors.Add(gradient.ColourAt((z - min) / range));
                }
                else
                {
                    t1.Vertices.Add(new Point3f(p.X,p.Y, Convert.ToSingle(e0)));
                    t1.VertexColors.Add(c0);
                }
            }//判定修正后把点和色彩加入新网格
            DA.SetData(0, t1);
        }
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
    }
 public class LTSD : GH_Component
{
    public LTSD()
        : base("坡向分析", "LTSD", "山坡地形朝向分析,X轴向为东,Y轴向为北\r\n双击电池图标以切换着色模式", "Lt", "分析")
    {
        Num1 = Math.Tan(Math.PI / 8);
        Num2 = Math.Tan(Math.PI*3 / 8);
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
            Mesh t1 = new Mesh();
            if (Shade)
            {
                if (t0.FaceNormals.Count != t0.Faces.Count)
                    t0.FaceNormals.ComputeFaceNormals();
                int numi = 0;
                int[] fi = {0, 0, 0, 0};
                for (var i = 0; i < t0.Faces.Count; i++)
                {
                    MeshFace f = t0.Faces[i];
                    Color c1 = c[DShade(t0.FaceNormals[i])];
                    for (int j = 0; j < (f.IsTriangle ? 3 : 4); j++)
                    {
                        fi[j] = numi;
                        t1.Vertices.Add(t0.Vertices[f[j]]);
                        t1.VertexColors.Add(c1);
                        numi++;
                    }//添加顶点和顶点色
                    t1.Faces.AddFace(f.IsTriangle ? new MeshFace(fi[0], fi[1], fi[2]) : new MeshFace(fi[0], fi[1], fi[2], fi[3]));
                }
            }
            else
            {
                t1 = t0.DuplicateMesh();
                foreach (Vector3f v in t0.Normals)
                    t1.VertexColors.Add(c[DShade(v)]);
            }
            DA.SetData(0, t1);
    }
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
    }
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
}
 public class LTTG : GH_Component
 {
     public LTTG() : base("坡度分析", "LTTG", "山地地形坡度分析", "Lt", "分析")
     {}//Terrain Grade
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
         Mesh tm=new Mesh();
         if (!DA.GetData(0,ref tm))
             return;
         List<double> ra = new List<double>(3);
         Interval ia = new Interval();
         foreach (Vector3f v in tm.Normals)
         {
             double a = Math.Acos(v.Z) * 180/Math.PI;
             ia.Grow(a);
             ra.Add(a);
         }
         Mesh cm = tm.DuplicateMesh();
         foreach (double a in ra)
             cm.VertexColors.Add(gradient.ColourAt(ia.NormalizedParameterAt(a)));
         DA.SetData(0, cm);
         DA.SetData(1, ia);
        }
     public static  GH_Gradient gradient = new GH_Gradient(
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
    }
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
             double e = Math.Round(p.Z,2);
             ie.Grow(e);
             re.Add(e);
         }
         Mesh cm = tm.DuplicateMesh();
         foreach (double e in re)
             cm.VertexColors.Add(LTTG.gradient.ColourAt(ie.NormalizedParameterAt(e)));
         DA.SetData(0, cm);
         DA.SetData(1, ie);
     }
     protected override Bitmap Icon => LTResource.山体高程分析;
     public override Guid ComponentGuid => new Guid("1afc0549-5308-47ef-b91c-a2eade694250");
 }
 public class LTVL : GH_Component
 {
     public LTVL() : base("视线分析", "LTVL", "分析在山地某处的可见范围", "Lt", "分析")
     { }//Terrain Grade
     protected override void RegisterInputParams(GH_InputParamManager pManager)
     {
         pManager.AddMeshParameter("地形", "Mt", "要进行坡度分析的山地地形网格", GH_ParamAccess.item);
         pManager.AddBrepParameter("障碍物", "O", "阻挡视线的障碍物体", GH_ParamAccess.list);
         pManager.AddPointParameter("观察点", "P", "观察者所在的点位置（不一定在网格上）", GH_ParamAccess.item);
         pManager.AddIntegerParameter("精度", "A", "分析精度，即分析点阵内的间距", GH_ParamAccess.item);
        }
     protected override void RegisterOutputParams(GH_OutputParamManager pManager)
     {
         pManager.AddPointParameter("观察点", "O", "观察者视点位置（眼高1m5）", GH_ParamAccess.item);
         pManager.AddPointParameter("可见点", "V", "被看见的点", GH_ParamAccess.list);
        }

     protected override void SolveInstance(IGH_DataAccess DA)
     {
         Mesh tm = new Mesh();
         if (!DA.GetData(0, ref tm))
             return;
         List<Mesh> o = new List<Mesh>(3);
         if(DA.GetDataList(1, o))
             return;
         Point3d p=new Point3d();
         if (!DA.GetData(2, ref p))
             return;
         int a =0;
         if (!DA.GetData(3, ref a))
             return;

         #region 将观测点投影到地形网格上
         Ray3d rayp = new Ray3d(new Point3d(p.X, p.Y, 0), Vector3d.ZAxis);
         double pmdp = Intersection.MeshRay(tm, rayp);
         if (RhinoMath.IsValidDouble(pmdp) && pmdp > 0.0)
             p = rayp.PointAt(pmdp);
         else
         {
             AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"观测点[{DA.Iteration}]无法被Z向投影至地形网格上");
             return;
         }
         #endregion

         p.Z += 1.5;
         DA.SetData(0, p);

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
                 Ray3d ray = new Ray3d(new Point3d(px, py, 0), Vector3d.ZAxis);
                 double pmd = Intersection.MeshRay(tm, ray);
                 if (RhinoMath.IsValidDouble(pmd) && pmd > 0.0)
                     grid.Add(ray.PointAt(pmd));
                 py += a;
             }
             px += a;
             py = mb.Min.Y;
         }//制作栅格点阵z射线,与网格相交，交点加入grid
         #endregion

         foreach (Mesh m in o)//将障碍物附加入网格
             tm.Append(m);
         for (var i = grid.Count - 1; i >= 0; i--)
         {
             Point3d t = grid[i];
             if (Intersection.MeshLine(tm, new Line(p, t), out _).Length != 1) //与网格上的点阵交点仅为1的即可见点
                 grid.RemoveAt(i);
         }
         DA.SetDataList(1, grid);
     }
        protected override Bitmap Icon => LTResource.视线分析;
     public override Guid ComponentGuid => new Guid("f5b0968b-4de5-45a6-a82b-744f64787e85");
     public override GH_Exposure Exposure => GH_Exposure.quarternary;
 }
}
