using Lt.GHComponent.Analysis;

namespace Lt.GHComponent.Osboletes.Osb2
{
    /// <summary>
    /// 过时组件：坡度分析(网格)
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public sealed class LTMG_Osb2 : GradientComponent_Osb
    {
        public LTMG_Osb2() : base("坡度分析(网格)", "LTMG",
            "分析",
            ComponentID.LTMG_Osb2, nameof(LTMG), LTResource.山体坡度分析)
        {
            Gra.Def = Const.Gradient0.Duplicate();
            Gra.ReCom = true;
        }
        protected override void AddParameter(ParamManager pm)
        {
            pm.AddIP(ParT.Mesh, "地形", "M", "要进行坡度分析的山地地形网格");

            pm.AddOP(ParT.Mesh, "地形", "M", "已按角度着色的地形网格");
            pm.AddOP(ParT.Interval, "角度", "A", "坡度范围（度）");
        }
    }
}
