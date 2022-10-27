using GH_IO.Serialization;
using Grasshopper.GUI.Gradient;
using Lt.Majas;
using System.Drawing;
using System.Windows.Forms;
using Lt.Analysis;

namespace Lt.Osbolete
{
    #region Osb1
    public abstract class AOComponent : MOComponent
    {
        protected AOComponent(string name, string nickname, string subCategory, string id, string nname, Bitmap icon = null) :
            base(name, nickname, "", "LT", subCategory, id, nname, icon)
        { }
    }
    // ReSharper disable once UnusedMember.Global
    public class LTFT : AOComponent
    {
        public LTFT() :
            base("地形网格淹没分析", "LTFT", "分析", ID.LTMF_Osb, nameof(LTMF), LTResource.山体淹没分析)
        { }

        protected override void AddParameter(ParamManager pm)
        {
            pm.AddIP(ParT.Mesh, "地形", "Mt", "要被淹没的山地地形网格");
            pm.AddIP(ParT.Number, "高度", "E", "淹没地形的水平面高度");
            pm.AddIP(ParT.Colour, "色彩", "C", "被水淹没区域的色彩", def: Color.FromArgb(52, 58, 107));

            pm.AddOP(ParT.Mesh, "地形", "Mf", "被水淹没后的地形网格");
        }
    }

    // ReSharper disable once UnusedMember.Global
    public class LTTG : AOComponent
    {
        public LTTG() : base("坡度分析", "LTTG", "分析", ID.LTMG_Osb, nameof(LTMG), LTResource.山体坡度分析)
        { }
        protected override void AddParameter(ParamManager pm)
        {
            pm.AddIP(ParT.Mesh, "地形", "Mt", "要进行坡度分析的山地地形网格");

            pm.AddOP(ParT.Mesh, "地形", "M", "已按角度着色的地形网格");
            pm.AddOP(ParT.Interval, "角度", "A", "坡度范围（度）");
        }
    }

    // ReSharper disable once UnusedMember.Global
    public class LTTE : AOComponent
    {
        public LTTE() : base("高程分析", "LTTE", "分析", ID.LTME_Osb, nameof(LTME), LTResource.山体高程分析)
        { }
        protected override void AddParameter(ParamManager pm)
        {
            pm.AddIP(ParT.Mesh, "地形", "Mt", "要进行坡度分析的山地地形网格");

            pm.AddOP(ParT.Mesh, "地形", "M", "已按海拔着色的地形网格");
            pm.AddOP(ParT.Interval, "海拔", "E", "海拔范围（两位小数）");
        }
    }

    // ReSharper disable once UnusedMember.Global
    public class LTVL_Osb : AOComponent
    {
        public LTVL_Osb() : base("视线分析", "LTVL", "分析", ID.LTVL_Osb, nameof(LTVL), LTResource.视线分析)
        { }
        protected override void AddParameter(ParamManager pm)
        {
            pm.AddIP(ParT.Mesh, "地形", "Mt", "要进行坡度分析的山地地形网格,仅支持单项数据");
            pm.AddIP(ParT.Mesh, "障碍物", "O", "（可选）阻挡视线的障碍物体，,仅支持单列数据", ParamTrait.List | ParamTrait.Optional);
            pm.AddIP(ParT.Point, "观察点", "P", "观察者所在的点位置（不一定在网格上），支持多点观察,仅支持单列数据", ParamTrait.List);
            pm.AddIP(ParT.Integer, "精度", "A", "分析精度，即分析点阵内的间距,仅支持单项数据");

            pm.AddOP(ParT.Point, "观察点", "O", "观察者视点位置（眼高1m5）", ParamTrait.List);
            pm.AddOP(ParT.Point, "可见点", "V", "被看见的点", ParamTrait.List);
        }
    }

    // ReSharper disable once UnusedMember.Global
    public class LTCE_Osb : AOComponent
    {
        public LTCE_Osb() : base("等高线高程分析", "LTCE", "分析", ID.LTCE_Osb, nameof(LTCE), LTResource.等高线高程分析)
        { }
        protected override void AddParameter(ParamManager pm)
        {
            pm.AddIP(ParT.Curve, "等高线", "C", "待分析的等高线，请自行确保输入的都是水平曲线", ParamTrait.List);

            pm.AddOP(ParT.Colour, "色彩", "C", "输入曲线高程的映射色彩", ParamTrait.List);
            pm.AddOP(ParT.Interval, "范围", "R", "输入高程线的高程范围");
        }
    }

    // ReSharper disable once UnusedMember.Global
    public class LTCF_Osb : AOComponent
    {
        public LTCF_Osb() : base("等高线淹没分析", "LTCE", "分析", ID.LTCF_Osb, nameof(LTCF), LTResource.等高线淹没分析) { }
        protected override void AddParameter(ParamManager pm)
        {
            pm.AddIP(ParT.Curve, "等高线", "C", "要进行淹没分析的等高线", ParamTrait.List);
            pm.AddIP(ParT.Integer, "高程", "E", "水面的高程");
            pm.AddIP(ParT.Boolean, "摊平", "F", "是否要将水下等高线摊平到水平面，默认为false", def: false);
            pm.AddIP(ParT.Colour, "水上色", "Cu", "未淹没区等高线的色彩", def: Color.White);
            pm.AddIP(ParT.Colour, "水下色", "Cd", "被淹没区等高线的色彩", def: Color.FromArgb(59, 104, 156));

            pm.AddOP(ParT.Curve, "水上线", "Cu", "未淹没区的等高线");
            pm.AddOP(ParT.Curve, "水下线", "Cd", "被淹没区的等高线");
        }
    }
    #endregion
    #region Osb2
    public abstract class GradientComponent_Osb : AOComponent
    {
        protected GradientComponent_Osb(string name, string nickname, string subCategory, string id, string nname,
             Bitmap icon = null) :
            base(name, nickname,  subCategory, id, nname, icon)
        { Gra = new MGradientMenuItem(this, GH_Gradient.GreyScale(), "渐变(&G)",rw:false); }

        public override bool Read(GH_IReader reader)
        {
            Gra.Def = reader.GetGradient("渐变");
            Gra.Rev = reader.GetBoolean("反转渐变");
            Gra.ReCom = reader.GetBoolean("重算否");
            return base.Read(reader);
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.SetGradient("渐变", Gra.Def);
            writer.SetBoolean("反转渐变", Gra.Rev);
            writer.SetBoolean("重算否", Gra.ReCom);
            return base.Write(writer);
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            Menu_Gradient(menu, ref Gra,
                "左小右大，\r\n若修改后预览无变化，请重计算本电池,并告知开发者修复\r\n要新增预设，请依靠【渐变】电池制作渐变并使用其右键菜单项\r\n【添加当前渐变Add Current Gradient】");
        }
        
        protected MGradientMenuItem Gra;
    }
    // ReSharper disable once UnusedMember.Global
    public class LTMF_Osb2 : GradientComponent_Osb
    {
        public LTMF_Osb2() : base(
            "淹没分析(网格)", "LTMF",
            "分析",
            ID.LTMF_Osb2, nameof(LTMF), LTResource.山体淹没分析)
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
            writer.SetDrawingColor("colordown",DownColor.Def);
            return base.Write(writer);
        }
        /// <summary>
        /// 水下色彩
        /// </summary>
        private MColorMenuItem DownColor;
    }
    // ReSharper disable once UnusedMember.Global
    public class LTMD_Osb2 : AOComponent
    {
        public LTMD_Osb2()
            : base("坡向分析(网格)", "LTMD",
                "分析",
                ID.LTMD_Osb2, nameof(LTMD), LTResource.山体坡向分析)
        {
            Shade = new MBooleanMenuItem(this, true, "使用面着色(&F)", true, rw: false);
        }
        protected override void AddParameter(ParamManager pm)
        {
            pm.AddIP(ParT.Mesh, "地形", "M", "要进行坡向分析的山地地形网格");
            pm.AddIP(ParT.Colour, "色彩", "C",
                "各向的色彩，请按上、北、东北、东、东南、南、西南、西、西北的顺序连入9个色彩", ParamTrait.List,
                new[]
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

            pm.AddOP(ParT.Mesh, "网格", "M", "已根据坡向着色的地形网格");
        }
        

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
            => Menu_Boolean(menu, ref Shade, click: (s, r) => Message = Shade.Def ? "面着色" : "顶点着色");



        public override bool Write(GH_IWriter writer)
        {
            writer.SetBoolean("面色否", Shade.Def);
            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            Shade.Def = reader.GetBoolean("面色否");
            return base.Read(reader);
        }

        private MBooleanMenuItem Shade;
    }
    // ReSharper disable once UnusedMember.Global
    public class LTMG_Osb2 : GradientComponent_Osb
    {
        public LTMG_Osb2() : base("坡度分析(网格)", "LTMG",
            "分析",
            ID.LTMG_Osb2, nameof(LTMG), LTResource.山体坡度分析)
        {
            Gra.Def = Ty.Gradient0.Duplicate();
            Gra.ReCom = true;
        }
        protected override void AddParameter(ParamManager pm)
        {
            pm.AddIP(ParT.Mesh, "地形", "M", "要进行坡度分析的山地地形网格");

            pm.AddOP(ParT.Mesh, "地形", "M", "已按角度着色的地形网格");
            pm.AddOP(ParT.Interval, "角度", "A", "坡度范围（度）");
        }
    }
    // ReSharper disable once UnusedMember.Global
    public class LTME_Osb2 : GradientComponent_Osb
    {
        public LTME_Osb2() : base("高程分析(网格)", "LTME",
            "分析",
            ID.LTME_Osb2, nameof(LTME), icon: LTResource.山体高程分析)
        {
            Gra.Def = Ty.Gradient0.Duplicate();
            Gra.ReCom = true;
        }
        protected override void AddParameter(ParamManager pm)
        {
            pm.AddIP(ParT.Mesh, "地形", "M", "要进行坡度分析的山地地形网格");

            pm.AddOP(ParT.Mesh, "地形", "M", "已按海拔着色的地形网格");
            pm.AddOP(ParT.Interval, "海拔", "E", "海拔范围（两位小数）");
        }
    }
    // ReSharper disable once UnusedMember.Global
    public class LTVL_Osb2 : AOComponent
    {
        public LTVL_Osb2() : base("视线分析", "LTVL",
            "分析",
            ID.LTVL_Osb2, nameof(LTVL), LTResource.视线分析)
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
        private static MDoubleMenuItem SizeV ;
        private static MDoubleMenuItem EyeHight ;
    }
    // ReSharper disable once UnusedMember.Global
    public class LTCE_Osb2 : GradientComponent_Osb
    {
        public LTCE_Osb2() : base("高程分析(等高线)", "LTCE",
            "分析",
            ID.LTCE_Osb2, nameof(LTCE), LTResource.等高线高程分析)
        {
            Gra.Def = Ty.Gradient0.Duplicate();
            Gra.ReCom = true;
        }
        protected override void AddParameter(ParamManager pm)
        {
            pm.AddIP(ParT.Curve, "等高线", "C", "待分析的等高线，请自行确保输入的都是水平曲线", ParamTrait.List);

            pm.AddOP(ParT.Colour, "色彩", "C", "输入曲线高程的映射色彩", ParamTrait.List);
            pm.AddOP(ParT.Interval, "范围", "R", "输入高程线的高程范围");
        }
    }
    // ReSharper disable once UnusedMember.Global
    public class LTCF_Osb2 : AOComponent
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
    // ReSharper disable once UnusedMember.Global
    public class LTSA : GradientComponent_Osb
    {
        public LTSA() : base("山路坡度分析", "LTSA",
            "分析",
            ID.LTSA, nameof(LTRA), LTResource.山路坡度分析)
        {
            Gra.Def = Ty.Gradient0.Duplicate();
            Gra.ReCom = true;
            GI = new MBooleanMenuItem(this, true, "自适应角度(&A)",rw:false);
        }
        protected override void AddParameter(ParamManager pm)
        {
            pm.AddIP(ParT.Curve, "山路", "C", "要分析的山路中线（确保已投影在地形上）", ParamTrait.List);
            pm.AddIP(ParT.Integer, "精度", "E", "山路中线的细分重建密度(单位米)");

            pm.AddOP(ParT.Line, "路线", "L", "重建后用于分析的直线路线", ParamTrait.List);
            pm.AddOP(ParT.Number, "坡度", "A", "对应直线段的坡度(度)", ParamTrait.List | ParamTrait.IsAngle);
            pm.AddOP(ParT.Text, "坡度范围", "Rs", "山路直线的坡度范围（既坡高/坡长）");
            pm.AddOP(ParT.Interval, "角度范围", "Ra", "山路直线与水平面所呈角度的范围");
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_Boolean(menu, ref GI, "默认不启用，此时渐变色彩范围对应0-90º。\r\n启用时，范围对应实际的角度范围",
                click: (s, r) => Message = GI.Def ? "自适应" : "0-90º");
        }

        private MBooleanMenuItem GI;

    }
    #endregion
}
