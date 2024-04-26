using Lt.Base;
using Lt.GHComponent.Analysis;
using Lt.Majas;

namespace Lt.GHComponent.Osboletes.Osb2
{
    /// <summary>
    /// 过时组件：高程分析(等高线)
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public sealed class LTCE_Osb2 : GradientComponent_Osb
    {
        public LTCE_Osb2() : base("高程分析(等高线)", "LTCE",
            "分析",
            ID.LTCE_Osb2, nameof(LTCE), LTResource.等高线高程分析)
        {
            Gra.Def = Const.Gradient0.Duplicate();
            Gra.ReCom = true;
        }
        protected override void AddParameter(ParamManager pm)
        {
            pm.AddIP(ParT.Curve, "等高线", "C", "待分析的等高线，请自行确保输入的都是水平曲线", ParamTrait.List);

            pm.AddOP(ParT.Colour, "色彩", "C", "输入曲线高程的映射色彩", ParamTrait.List);
            pm.AddOP(ParT.Interval, "范围", "R", "输入高程线的高程范围");
        }
    }
}
