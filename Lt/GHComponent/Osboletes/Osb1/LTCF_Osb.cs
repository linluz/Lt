using System.Drawing;
using Lt.Base;
using Lt.GHComponent.Analysis;

namespace Lt.GHComponent.Osboletes.Osb1
{
    /// <summary>
    /// 过时组件：等高线淹没分析
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public sealed class LTCF_Osb : AOComponent
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
}
