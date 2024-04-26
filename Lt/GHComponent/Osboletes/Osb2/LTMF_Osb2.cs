using System.Drawing;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.GUI.Gradient;
using Lt.GHComponent.Analysis;
using Lt.Majas.MenuItemClass;
using Lt.Base;


namespace Lt.GHComponent.Osboletes.Osb2
{
    /// <summary>
    /// 过时组件：淹没分析(网格)
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public sealed class LTMF_Osb2 : GradientComponent_Osb
    {
        public LTMF_Osb2() : base(
            "淹没分析(网格)", "LTMF",
            "分析",
            ComponentID.LTMF_Osb2, nameof(LTMF), LTResource.山体淹没分析)
        {
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
            DownColor = new MColorMenuItem(this, Color.FromArgb(52, 58, 107), "淹没色彩(&F)", true, rw: false);
        }

        protected override void AddParameter(ParamManager pm)
        {
            pm.AddIP(ParT.Mesh, "地形", "M", "要被淹没的山地地形网格");
            pm.AddIP(ParT.Number, "高度", "E", "淹没地形的水平面高度");
            pm.AddIP(ParT.Boolean, "摊平", "F", "是否要将水下等高线摊平到水平面，默认为false", def: true);

            pm.AddOP(ParT.Mesh, "地形", "M", "被水淹没后的地形网格");
        }


        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
            => Menu_Color(menu, ref DownColor);

        public override bool Read(GH_IReader reader)
        {
            DownColor.Def = reader.GetDrawingColor("colordown");
            return base.Read(reader);
        }
        public override bool Write(GH_IWriter writer)
        {
            writer.SetDrawingColor("colordown", DownColor.Def);
            return base.Write(writer);
        }
        /// <summary>
        /// 水下色彩
        /// </summary>
        private MColorMenuItem DownColor;
    }
}
