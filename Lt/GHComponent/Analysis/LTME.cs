using System;
using Grasshopper.Kernel;
using Lt.Base;
using Lt.Base.Component;
using Lt.Base.Extensions;
using Lt.Majas;
using Rhino.Geometry;

namespace Lt.GHComponent.Analysis
{
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
            ID.LTME, icon: LTResource.山体高程分析)
        {
            Gra.Def = Const.Gradient0.Duplicate();
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
            var tm = new Mesh();
            if (!DA.GetData(0, ref tm))
                return;
            BoundingBox b = tm.GetBoundingBox(false);
            var ie = new Interval(b.Min.Z, b.Max.Z);
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
            args.Draw1Meshes(0, this);
        }
    }
}