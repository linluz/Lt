using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Grasshopper.Kernel;

namespace Lt.Majas.MenuItemClass
{
    /// <summary>
    /// 整数菜单项
    /// </summary>
    public sealed class MIntegerMenuItem : MMenuItem<int>
    {
        public MIntegerMenuItem(MComponent c, int def, string text,
            bool recom = false, bool rw = true, Func<MIntegerMenuItem, string> mf = null)
            : base(c, text, def, recom, rw)
        {
            SetMessage(mf);
            TextBox = null;
            ReadL.Add(r =>
            {
#if DEBUG
                if (!ItemExist(r, nameof(Def))) return false;
#endif
                Def = r.GetInt32(nameof(Def));
                return true;

            });
            WriteL.Add(w =>
            {
#if DEBUG
                if (!ItemNoExist(w, nameof(Def))) return false;
#endif 
                w.SetInt32(nameof(Def), Def);
                return true;
            });
        }

        public MIntegerMenuItem SetMenuItem(ToolStrip menu, string tooltip = null, Image icon = null)
        {
            Item = GH_DocumentObject.Menu_AppendItem(menu, Name, null, icon);
            if (!string.IsNullOrWhiteSpace(tooltip))
                Item.ToolTipText = tooltip;

            TextBox = GH_DocumentObject.Menu_AppendTextItem(Item.DropDown, Def.ToString(CultureInfo.InvariantCulture),
                (s, e) =>
                {
                    switch (e.KeyData)
                    {
                        case Keys.Enter:
                            SetInteger();//输入回车进行运算
                            break;
                        case Keys.Space:
                            s.CloseEntireMenuStructure();//输入空格和回车时关闭菜单栏
                            break;
                    }
                },
                (sender, s) =>
                    sender.TextBoxItem.ForeColor = double.TryParse(s, out double _) ? SystemColors.WindowText : Color.Red
                , false);
            TextBox.VisibleChanged += (s, e) => SetInteger();
            TextBox.ToolTipText = "按下回车确定输入并计算，\r\n按下空格关闭输入框";
            return this;
        }

        /// <summary>
        /// 将输入的数值赋给def
        /// </summary>
        /// <param name="text">对象的显示文本</param>
        /// <param name="recom">是否重计算</param>
        private void SetInteger()
        {
            if (!IsVaild) return;
            if (int.TryParse(TextBox.Text, out int d))
            {
                if (Def == d) return;
                Component.RecordUndoEvent($"设置{NameNoKey}");
                Def = d;
                Component.Expire(ReCom);
            }
            else
                TextBox.Text = Def.ToString(CultureInfo.InvariantCulture);
        }
        public ToolStripTextBox TextBox;
        public bool IsVaild => IsVaild0 && TextBox != null;
    }
}
