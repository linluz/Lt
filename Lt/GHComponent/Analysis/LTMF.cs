using System;
using System.Drawing;
using System.Windows.Forms;
using Grasshopper.GUI.Gradient;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Lt.GHComponent.Analysis
{
    //首选项里 增加lt标签页 里面有默认渐变 默认色彩标尺 默认提示圆尺寸 等等
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
            ComponentID.LTMF, 1, LTResource.山体淹没分析)
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
            if (!DA.OutDataC(0, out Mesh  t0)
                || !DA.OutData(1, out double e0)
                || !DA.OutData(2, out bool f))
                return;
            #endregion

            BoundingBox b = t0.GetBoundingBox(false);
            var ie = new Interval(b.Min.Z, b.Max.Z); //获取高度范围
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
            args.Draw1Meshes(0, this);
        }
        /// <summary>
        /// 水下色彩
        /// </summary>
        private MColorMenuItem DownColor;
    }
}
