using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel;
using Lt.Base;
using Lt.Base.Component;
using Lt.Majas;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino;

namespace Lt.GHComponent.Analysis
{
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
            Gra.Def = Const.Gradient0.Duplicate();
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
            if (!DA.OutDataList(0,out List<GH_Curve> g)) return;
            var cd = g.Select(ci => ci.Value.PointAtEnd.Z).ToArray();
            var r = cd.ToInterval();

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
                var id = Guid.Empty;
                Cur[i].BakeGeometry(doc, oa, ref id);
                obj_ids.Add(id);
            }
        }
    }
}
