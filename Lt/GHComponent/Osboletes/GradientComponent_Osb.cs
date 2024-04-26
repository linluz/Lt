using System.Drawing;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.GUI.Gradient;
using Lt.Majas;
using Lt.Majas.MenuItemClass;

namespace Lt.GHComponent.Osboletes
{
    /// <summary>
    /// 过时组件：渐变基类
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public abstract class GradientComponent_Osb : AOComponent
    {
        protected GradientComponent_Osb(string name, string nickname, string subCategory, string id, string nname,
            Bitmap icon = null) :
            base(name, nickname, subCategory, id, nname, icon)
        { Gra = new MGradientMenuItem(this, GH_Gradient.GreyScale(), "渐变(&G)", rw: false); }

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
}
