using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel;
using Lt.Majas;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino;
using System.Windows.Forms;
using Lt.Base.Component;
using Lt.Base.Extensions;

namespace Lt.GHComponent.Analysis
{
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
            if (!DA.OutDataList(0, out List<Curve> c)
                || !DA.OutData(1, out int e)
                || !DA.OutData(2, out bool f))
                return;
            #endregion
            var ep = new Plane(new Point3d(0, 0, e), new Vector3d(0, 0, 1));

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
            {
                Color col = set ? args.WireColour_Selected : UpColor.Def;
                foreach (GH_Curve l1 in GetOutByItem<GH_Curve>(0).Where(l1 => l1.IsValid))
                    args.Display.DrawCurve(l1.Value, col);
            }
            //bug 测试淹没线是否正常
            if (!DownColor.Def.IsEmpty)
            {
                Color col = set ? args.WireColour_Selected : DownColor.Def;
                foreach (GH_Curve l1 in GetOutByItem<GH_Curve>(1).Where(l1 => l1.IsValid))
                    args.Display.DrawCurve(l1.Value, col);
            }
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
}
