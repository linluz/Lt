using System.Drawing;
using System.Windows.Forms;
using GH_IO.Serialization;
using Lt.GHComponent.Analysis;
using Lt.Majas;

namespace Lt.GHComponent.Osboletes.Osb2
{
    /// <summary>
    /// 过时组件：淹没分析(等高线)
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public sealed class LTCF_Osb2 : AOComponent
    {
        public LTCF_Osb2() : base("淹没分析(等高线)", "LTCF",
            "分析",
            ID.LTCF_Osb2, nameof(LTCF), LTResource.等高线淹没分析)
        {
            UpColor = new MColorMenuItem(this, Color.White, "未淹色彩(&U)", rw: false);
            DownColor = new MColorMenuItem(this, Color.FromArgb(59, 104, 156), "淹没色彩(&F)", rw: false);
        }
        protected override void AddParameter(ParamManager pm)
        {
            pm.AddIP(ParT.Curve, "等高线", "C", "要进行淹没分析的等高线", ParamTrait.List);
            pm.AddIP(ParT.Integer, "高程", "E", "水面的高程");
            pm.AddIP(ParT.Boolean, "摊平", "F", "是否要将水下等高线摊平到水面，默认为false", def: false);

            pm.AddOP(ParT.Curve, "未淹线", "Cu", "未淹没区域的等高线", ParamTrait.List);
            pm.AddOP(ParT.Curve, "淹没线", "Cd", "被淹没区域的等高线", ParamTrait.List);
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            Menu_Color(menu, ref UpColor);
            Menu_Color(menu, ref DownColor);
        }

        public override bool Read(GH_IReader reader)
        {
            UpColor.Def = reader.GetDrawingColor("colorup");
            DownColor.Def = reader.GetDrawingColor("colordown");
            return base.Read(reader);
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.SetDrawingColor("colorup", UpColor.Def);
            writer.SetDrawingColor("colordown", DownColor.Def);
            return base.Write(writer);
        }

        private MColorMenuItem UpColor;
        private MColorMenuItem DownColor;
    }
}
