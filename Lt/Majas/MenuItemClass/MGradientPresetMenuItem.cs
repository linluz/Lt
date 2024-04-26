using System.Drawing;
using System.Windows.Forms;
using Grasshopper.GUI.Gradient;

namespace Lt.Majas.MenuItemClass
{
    /// <summary>
    /// 渐变预设菜单项
    /// </summary>
    public sealed class MGradientPresetMenuItem : ToolStripMenuItem
    {
        /// <summary>
        /// 添加渐变菜单项
        /// </summary>
        /// <param name="grac">被添加至的电池</param>
        /// <param name="gra">电池中对应使用的渐变</param>
        public MGradientPresetMenuItem(GH_Gradient gra, MouseEventHandler even)
        {
            Gradient = gra;
            DisplayStyle = ToolStripItemDisplayStyle.None;
            Text = "渐变预设";
            Margin = new Padding(1);
            Paint += LT_GradientMenuItem_Paint;
            if (even != null)
                MouseDown += even;
        }

        public GH_Gradient Gradient { get; set; }
        private void LT_GradientMenuItem_Paint(object sender, PaintEventArgs e)
        {
            Rectangle contentRectangle = ContentRectangle;
            contentRectangle.X += 3;
            contentRectangle.Y++;
            contentRectangle.Width -= 25;
            contentRectangle.Height -= 3;
            e.Graphics.FillRectangle(Brushes.White, contentRectangle);
            if (Gradient != null)
            {
                Gradient.Render_Gradient(e.Graphics, contentRectangle);
                Rectangle rectangle = contentRectangle;
                rectangle.Width--;
                rectangle.Height--;
                var pen = new Pen(Color.FromArgb(80, Color.Black));
                e.Graphics.DrawRectangle(pen, rectangle);
                pen.Dispose();
                rectangle.Offset(1, 1);
                var pen2 = new Pen(Color.FromArgb(150, Color.White));
                e.Graphics.DrawRectangle(pen2, rectangle);
                pen2.Dispose();
            }
            e.Graphics.DrawRectangle(Pens.Black, contentRectangle);
        }
    }
}
