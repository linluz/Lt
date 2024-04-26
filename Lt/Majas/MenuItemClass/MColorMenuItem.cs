using System;
using System.Drawing;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.Kernel;

namespace Lt.Majas.MenuItemClass
{
    /// <summary>
    /// 色彩菜单项
    /// </summary>
    public sealed class MColorMenuItem : MMenuItem<Color>
    {
        public MColorMenuItem(MComponent c, Color def, string text,
            bool recom = false, bool rw = true, Func<MColorMenuItem, string> mf = null)
            : base(c, text, def, recom, rw)
        {
            SetMessage(mf);
            ColourPicker = null;
            ReadL.Add(r =>
            {
#if DEBUG
                if (!ItemExist(r, nameof(Def))) return false;
#endif
                Def = r.GetDrawingColor(nameof(Def));
                return true;

            });
            WriteL.Add(w =>
            {
#if DEBUG
                if (!ItemNoExist(w, nameof(Def))) return false;
#endif 
                w.SetDrawingColor(nameof(Def), Def);
                return true;
            });
        }

        internal MColorMenuItem SetMenuItem(ToolStrip menu, string tooltip = null, Image icon = null)
        {
            if (icon == null)
                icon = Def.ToSprite(20, 20);
            Item = GH_DocumentObject.Menu_AppendItem(menu, Name, null, icon);

            if (!string.IsNullOrWhiteSpace(tooltip))
                Item.ToolTipText = tooltip;

            ColourPicker = GH_DocumentObject.Menu_AppendColourPicker(Item.DropDown, Def,
                (sender, e) =>
                {
                    if (!IsVaild) return;
                    Component.RecordUndoEvent($"设置{NameNoKey}");
                    Def = e.Colour;
                    Item.Image = e.Colour.ToSprite(20, 20);
                    Component.Expire(ReCom);
                });
            return this;
        }
        public GH_ColourPicker ColourPicker;
        public bool IsVaild => IsVaild0 && ColourPicker != null;
    }
}
