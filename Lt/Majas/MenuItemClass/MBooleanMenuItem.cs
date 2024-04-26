using System;
using System.Drawing;
using System.Windows.Forms;
using Grasshopper.Kernel;

namespace Lt.Majas.MenuItemClass
{
    /// <summary>
    /// 布尔型菜单项
    /// </summary>
    public sealed class MBooleanMenuItem : MMenuItem<bool>
    {
        public MBooleanMenuItem(MComponent c, bool def, string text,
            bool recom = false, bool rw = true, Func<MBooleanMenuItem, string> mf = null)
            : base(c, text, def, recom, rw)
        {
            SetMessage(mf);
            ReadL.Add(r =>
            {
#if DEBUG
                if (!ItemExist(r, nameof(Def))) return false;
#endif
                Def = r.GetBoolean(nameof(Def));
                return true;

            });
            WriteL.Add(w =>
            {
#if DEBUG
                if (!ItemNoExist(w, nameof(Def))) return false;
#endif 
                w.SetBoolean(nameof(Def), Def);
                return true;
            });
        }

        public MBooleanMenuItem SetMenuItem(ToolStrip menu, string tooltip = null, Image icon = null, EventHandler click = null)
        {
            Item = GH_DocumentObject.Menu_AppendItem(menu, Name,
                delegate
                {
                    Def = !Def;
                    for (int i = 0; i < Item.DropDownItems.Count; i++)
                        Item.DropDownItems[i].Visible = Def;
                    Component.Expire(ReCom);
                }
                , icon, true, Def);
            if (!string.IsNullOrWhiteSpace(tooltip))
                Item.ToolTipText = tooltip;
            if (click != null)
                Item.Click += click;
            return this;
        }
        /// <summary>
        /// 设置事件，1点击后菜单项勾选状态与def同步，2打开时子菜单项可见性与勾选状态同步
        /// </summary>
        public void SetEvent()
        {
            if (SettedEvent) return;
            SettedEvent = true;
            Item.DropDownOpened += (s, e) => Item.DropDown.Visible = Item.Checked;
            Item.Click += (sender, args) => Item.Checked = Def;
        }
        /// <summary>
        /// 实践是否已设置
        /// </summary>
        private bool SettedEvent;
        /// <summary>
        /// 替换输入输出端的说明文本
        /// </summary>
        /// <param name="io">true为输出端，false为输入端</param>
        /// <param name="pi">索引序号</param>
        /// <param name="s0">按钮勾选时，被替换的文本，否则相反</param>
        /// <param name="s1">按钮勾选时，替换为的文本，否则相反</param>
        public void ReplacePDesc(bool io, string s0, string s1, params int[] pia)
        {
            foreach (var pi in pia)
            {
                var p = (io ? Component.Params.Output : Component.Params.Input)[pi];
                string s = p.Description;
                p.Description = Def
                    ? s.Replace(s0, s1)
                    : s.Replace(s1, s0);
            }
        }
        public bool IsVaild => IsVaild0;
    }
}
