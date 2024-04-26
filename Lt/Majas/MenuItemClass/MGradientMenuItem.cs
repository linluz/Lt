using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.GUI.Gradient;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;

namespace Lt.Majas.MenuItemClass
{
    /// <summary>
    /// 渐变菜单项
    /// </summary>
    public sealed class MGradientMenuItem : MMenuItem<GH_Gradient>
    {
        public MGradientMenuItem(MComponent c, GH_Gradient def, string text, bool rev = false,
            bool recom = false, bool rw = true, Func<MGradientMenuItem, string> mf = null)
            : base(c, text, def, recom, rw)
        {
            SetMessage(mf);
            RevBe = null;
            Rev = rev;
            ReadL.Add(r =>
            {
#if DEBUG
                if (!ChunkExist(r, nameof(Def))) return false;
                if (!ItemExist(r, nameof(Rev))) return false;
#endif
                Def = r.GetGradient(nameof(Def));
                Rev = r.GetBoolean(nameof(Rev));
                return true;

            });
            WriteL.Add(w =>
            {
#if DEBUG
                if (!ChunkNoExist(w, nameof(Def))) return false;
                if (!ItemNoExist(w, nameof(Rev))) return false;
#endif 
                w.SetGradient(nameof(Def), Def);
                w.SetBoolean(nameof(Rev), Rev);
                return true;
            });
        }

        internal MGradientMenuItem SetMenuItem(ToolStrip menu, string tooltip = null, Image icon = null)
        {
            if (icon == null) icon = LTResource.Gradient_20x20;
            Item = GH_DocumentObject.Menu_AppendItem(menu, Name, null, icon);
            if (!string.IsNullOrWhiteSpace(tooltip))
                Item.ToolTipText = tooltip;
            //if (gradient.DropDown is ToolStripDropDownMenu downMenu)
            //    downMenu.ShowImageMargin = false;

            List<GH_Gradient> gradientPresets = GH_GradientControl.GradientPresets.ToArray().ToList();
            //把默认值插入为第一个
            gradientPresets.Insert(0, Def);
            void GradientPresetClicked(object s, MouseEventArgs e)
            {
                var GradientMenuItem = (MGradientPresetMenuItem)s;
                Component.RecordUndoEvent($"设置{NameNoKey}");
                //删除旧的
                for (int i = Def.GripCount - 1; i >= 0; i--)
                    Def.RemoveGrip(i);
                //添加新的
                for (int i = 0; i < GradientMenuItem.Gradient.GripCount; i++)
                    Def.AddGrip(GradientMenuItem.Gradient[i]);

                Component.Expire(ReCom);
            }//创建点击事件 本地方法
            //把渐变都添加到菜单中
            foreach (GH_Gradient t in gradientPresets)
                Item.DropDownItems.Add(new MGradientPresetMenuItem(t, GradientPresetClicked));
            //插入提示文本
            Item.DropDownItems[0].ToolTipText = "当前渐变";
            #region 反转渐变
            RevBe = GH_DocumentObject.Menu_AppendItem(menu, $"反转{NameNoKey}(&R)",
                delegate
                {
                    if (!IsVaild) return;
                    Component.RecordUndoEvent($"反转{NameNoKey}");
                    Rev = !Rev;
                    Component.Expire(ReCom);
                });
            RevBe.ToolTipText = "仅反转渐变的映射效果，不修改渐变数据";
            RevBe.Checked = Rev;
            #endregion

            return this;
        }
        public bool Rev;
        public ToolStripMenuItem RevBe;
        public bool IsVaild => IsVaild0 && RevBe != null;
    }
}
