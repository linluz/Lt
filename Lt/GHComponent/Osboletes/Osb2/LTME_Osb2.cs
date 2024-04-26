using Lt.Base;
using Lt.GHComponent.Analysis;
using Lt.GHComponent.Osboletes;
using Lt.Majas;

namespace Lt.Osbolete.Osb2
{
    /// <summary>
    /// 过时组件：高程分析(网格)
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public sealed class LTME_Osb2 : GradientComponent_Osb
    {
        public LTME_Osb2() : base("高程分析(网格)", "LTME",
            "分析",
            ID.LTME_Osb2, nameof(LTME), icon: LTResource.山体高程分析)
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
    }
}
