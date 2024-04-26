using Lt.Base;
using Lt.GHComponent.Analysis;

namespace Lt.GHComponent.Osboletes.Osb1
{
    /// <summary>
    /// 过时组件：高程分析
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public sealed class LTTE : AOComponent
    {
        public LTTE() : base("高程分析", "LTTE", "分析", ComponentID.LTME_Osb, nameof(LTME), LTResource.山体高程分析)
        { }
        protected override void AddParameter(ParamManager pm)
        {
            pm.AddIP(ParT.Mesh, "地形", "Mt", "要进行坡度分析的山地地形网格");

            pm.AddOP(ParT.Mesh, "地形", "M", "已按海拔着色的地形网格");
            pm.AddOP(ParT.Interval, "海拔", "E", "海拔范围（两位小数）");
        }
    }
}
