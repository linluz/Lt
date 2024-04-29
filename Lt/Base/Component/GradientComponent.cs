using System.Drawing;
using System.Windows.Forms;
using Grasshopper.GUI.Gradient;
using Rhino.Geometry;

namespace Lt.Base.Component
{
    /// <summary>
    /// 渐变电池基类
    /// </summary>
    public abstract class GradientComponent : AComponent
    {
        protected GradientComponent(string name, string nickname, string description, string subCategory, string id,
            int exposure = 1, Bitmap icon = null) :
            base(name, nickname, description, subCategory, id, exposure, icon)
        {
            Gra = new MGradientMenuItem(this, GH_Gradient.GreyScale(), "渐变(&G)");
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            Menu_Gradient(menu, ref Gra,
                "左小右大，\r\n若修改后预览无变化，请重计算本电池,并告知开发者修复\r\n要新增预设，请依靠【渐变】电池制作渐变并使用其右键菜单项\r\n【添加当前渐变Add Current Gradient】");
        }

        protected Color Dou2Col(Interval it, double v)
            => Gra.Def.Double2GraColor(it, v, Gra.Rev);
        protected MGradientMenuItem Gra;
    }
}
