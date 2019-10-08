using System;
using System.Drawing;
using Grasshopper.GUI.Gradient;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Lt.Analysis
{/// <summary>
/// 电池界面 添加渐变范围
/// </summary>
    public class LtComponent : GH_Component
    {
        public LtComponent()
          : base("地形网格淹没分析", "地形网格淹没分析","分析被水淹没后的地形状态","Lt", "分析")
        {}
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("地形", "T", "要被淹没的山地地形网格", GH_ParamAccess.item);
            pManager.AddNumberParameter("高度", "H", "淹没地形的水平面高度", GH_ParamAccess.item);
            pManager.AddColourParameter("色彩", "C", "被水淹没区域的色彩", GH_ParamAccess.item,Color.FromArgb(52,58,107));
        }
        
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("地形", "M", "被水淹没后的地形网格", GH_ParamAccess.item);
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh t0= new Mesh();
            if (!DA.GetData(0, ref t0))
                return;
            float h0 = 0;
            if (!DA.GetData(1, ref h0))
                return;
            Color c0=new Color();
            if (!DA.GetData(2, ref c0))
                return;
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
            foreach (Point3f p in t0.Vertices)//判定修正后把点和色彩加入新网格
            {
                double z = p.Z;
                if (z > h0)
                {
                    t1.Vertices.Add(p);
                    t1.VertexColors.Add(gradient.ColourAt((z - min) / range));
                }
                else
                {
                    t1.Vertices.Add(new Point3f(p.X,p.Y,h0));
                    t1.VertexColors.Add(c0);
                }
            }
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
                Color.FromArgb(138,36,36),
            }
        );
        protected override Bitmap Icon => null;
        public override Guid ComponentGuid => new Guid("84474303-59cb-4248-9015-c5a02098fd99");
    }
}
