using Lt.Base;
using Lt.GHComponent.Analysis;

namespace Lt.GHComponent.Osboletes.Osb1
{
    /// <summary>
    /// 过时组件：视线分析
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public sealed class LTVL_Osb : AOComponent
    {
        public LTVL_Osb() : base("视线分析", "LTVL", "分析", ComponentID.LTVL_Osb, nameof(LTVL), LTResource.视线分析)
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
}
