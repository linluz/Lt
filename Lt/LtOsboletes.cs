using Lt.Analysis;
using Lt.Majas;
using System.Drawing;

namespace Lt.Osbolete
{
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
    public class LTCF_Osb : AOComponent
    {
        public LTCF_Osb() : base("等高线淹没分析", "LTCE", "分析",ID.LTCF_Osb, nameof(LTCF), LTResource.等高线淹没分析)
        { }
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
}