using System.Drawing;
using System.Windows.Forms;
using GH_IO.Serialization;
using Lt.GHComponent.Analysis;

namespace Lt.GHComponent.Osboletes.Osb2
{
    /// <summary>
    /// 过时组件：视线分析
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public sealed class LTVL_Osb2 : AOComponent
    {
        public LTVL_Osb2() : base("视线分析", "LTVL",
            "分析",
            ComponentID.LTVL_Osb2, nameof(LTVL), LTResource.视线分析)
        {
            ColorO = new MColorMenuItem(this, Color.Red, "观察点色彩(&C)", rw: false);
            SizeO = new MDoubleMenuItem(this, 10, "观察点尺寸(&S)", rw: false);
            ColorV = new MColorMenuItem(this, Color.FromArgb(0, 207, 182), "可见点色彩(&C)", rw: false);
            SizeV = new MDoubleMenuItem(this, 4, "可见点尺寸(&S)", rw: false);
            EyeHight = new MDoubleMenuItem(this, 1.5, "眼高（单位米）(&E)", true, rw: false);
        }
        protected override void AddParameter(ParamManager pm)
        {
            pm.AddIP(ParT.Mesh, "地形", "Mt", "要进行坡度分析的山地地形网格");
            pm.AddIP(ParT.Mesh, "障碍物", "O", "（可选）阻挡视线的障碍物体，", ParamTrait.List | ParamTrait.Optional);
            pm.AddIP(ParT.Point, "观察点", "P", "观察者所在的点位置（可不在网格上），支持多点观察", ParamTrait.List);
            pm.AddIP(ParT.Integer, "精度", "A", "分析精度(单位：米)，即分析点阵内的间距");

            pm.AddOP(ParT.Point, "观察点", "O", "观察者视点位置", ParamTrait.List);
            pm.AddOP(ParT.Point, "可见点", "V", "被看见的点", ParamTrait.List);
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            Menu_Color(menu, ref ColorO);
            Menu_Double(menu, ref SizeO, icon: LTResource.PointStyle_20x20);
            Menu_Color(menu, ref ColorV);
            Menu_Double(menu, ref SizeV, icon: LTResource.PointStyle_20x20);
            Menu_Double(menu, ref EyeHight, "人眼高度", icon: LTResource.EyeHight_20x20);
        }
        public override bool Read(GH_IReader reader)
        {
            ColorO.Def = reader.GetDrawingColor("观色");
            SizeO.Def = reader.GetDouble("观寸");
            ColorV.Def = reader.GetDrawingColor("见色");
            SizeV.Def = reader.GetDouble("见寸");
            EyeHight.Def = reader.GetDouble("眼高");
            return base.Read(reader);
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.SetDrawingColor("观色", ColorO.Def);
            writer.SetDouble("观寸", SizeO.Def);
            writer.SetDrawingColor("见色", ColorV.Def);
            writer.SetDouble("见寸", SizeV.Def);
            writer.SetDouble("眼高", EyeHight.Def);
            return base.Write(writer);
        }

        private static MColorMenuItem ColorO;
        private static MDoubleMenuItem SizeO;
        private static MColorMenuItem ColorV;
        private static MDoubleMenuItem SizeV;
        private static MDoubleMenuItem EyeHight;
    }
}
