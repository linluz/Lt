#define gaixie
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.GUI.Gradient;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Rhino.DocObjects;
using Rhino;
using System.Reflection;

// ReSharper disable UnusedMember.Global
namespace Lt.Majas
{
    /// <summary>
    /// 通用
    /// </summary>
    public static class Ty
    {
        /// <summary>
        /// 绘制预览网格，（仅未被选中时显示伪色），用于重写DrawViewportMeshes内
        /// </summary>
        /// <param name="a">输出端索引</param>
        /// <param name="component">电池本体，默认输入this</param>
        /// <param name="args">预览变量，默认输入 args</param>
        public static void Draw1Meshes(int a, IGH_Component component, IGH_PreviewArgs args)
        {
            var lmesh = component.Params.Output[a].VolatileData.AllData(true).Select(t => ((GH_Mesh)t).Value).ToList();
            if (lmesh.Count == 0) return; ///避免网格不存在
            bool set = component.Attributes.GetTopLevel.Selected;
            GH_PreviewMeshArgs args2 = new GH_PreviewMeshArgs(args.Viewport, args.Display,
                set ? args.ShadeMaterial_Selected : args.ShadeMaterial, args.MeshingParameters);
            foreach (Mesh mesh in lmesh)
                if (mesh.VertexColors.Count > 0)
                    args2.Pipeline.DrawMeshFalseColors(mesh);
                else
                    args2.Pipeline.DrawMeshShaded(mesh, args2.Material);
        }
        public static void BakeColorGroup<T>(this RhinoDoc doc, List<List<T>> l, Color co, string groupname, ObjectAttributes att, List<Guid> obj_ids)
            where T : IGH_Goo, IGH_BakeAwareData
        {
            foreach (var l0 in l)
            {
                if (l0.Count == 0 || co.IsEmpty) return;
                ObjectAttributes oa = att.Duplicate();
                int groupIndex = doc.Groups.Add(groupname);
                oa.AddToGroup(groupIndex);
                oa.ColorSource = ObjectColorSource.ColorFromObject;
                oa.ObjectColor = co;
                foreach (T c in l0.Where(c => c.IsValid))
                {
                    c.BakeGeometry(doc, oa, out Guid id);
                    obj_ids.Add(id);
                }
            }
        }

        public static GH_Gradient Gradient0 = new GH_Gradient(
            new[] { 0, 0.2, 0.4, 0.6, 0.8, 1 },
            new[]
            {
                Color.FromArgb(45, 51, 87),
                Color.FromArgb(75, 107, 169),
                Color.FromArgb(173, 203, 249),
                Color.FromArgb(254, 244, 84),
                Color.FromArgb(234, 126, 0),
                Color.FromArgb(237, 53, 17)
            });
    }

    public abstract class AComponent : MComponent
    {
        protected AComponent(string name, string nickname, string description, string subCategory, string id, int exposure = 1, Bitmap icon = null) :
            base(name, nickname, description, "Lt", subCategory, id, exposure, icon)
        {
        }
    }

    public abstract class AOComponent : MOComponent
    {
        protected AOComponent(string name, string nickname, string subCategory, string id, string nname, Bitmap icon = null) :
            base(name, nickname, "", "LT", subCategory, id, nname, icon)
        { }
    }

    public abstract class GradientComponent : AComponent
    {
        protected GradientComponent(string name, string nickname, string description, string subCategory, string id,
            int exposure = 1, Bitmap icon = null) :
            base(name, nickname, description, subCategory, id, exposure, icon)
        { }

        public override bool Read(GH_IReader reader)
        {
            Gradient = reader.GetGradient("渐变");
            Rev.Value = reader.GetBoolean("反转渐变");
            ReCom = reader.GetBoolean("重算否");
            return base.Read(reader);
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.SetGradient("渐变", Gradient);
            writer.SetBoolean("反转渐变", Rev.Value);
            writer.SetBoolean("重算否", ReCom);
            return base.Write(writer);
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            Menu_Gradient(menu, "渐变(&G)", Gradient, Rev,
                "左小右大，\r\n若修改后预览无变化，请重计算本电池,并告知开发者修复\r\n要新增预设，请依靠【渐变】电池制作渐变并使用其右键菜单项\r\n【添加当前渐变Add Current Gradient】",
                ReCom);
        }

        protected Color Dou2Col(Interval it, double v)
            => Gradient.Double2GraColor(it, v, Rev.Value);

        protected GH_Boolean Rev = new GH_Boolean(false);
        protected bool ReCom;
        protected GH_Gradient Gradient;
    }

    public sealed class MGradientMenuItem : ToolStripMenuItem
    {
        /// <summary>
        /// 添加渐变菜单项
        /// </summary>
        /// <param name="grac">被添加至的电池</param>
        /// <param name="gra">电池中对应使用的渐变</param>
        public MGradientMenuItem(GH_Gradient gra, MouseEventHandler even)
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
                Pen pen = new Pen(Color.FromArgb(80, Color.Black));
                e.Graphics.DrawRectangle(pen, rectangle);
                pen.Dispose();
                rectangle.Offset(1, 1);
                Pen pen2 = new Pen(Color.FromArgb(150, Color.White));
                e.Graphics.DrawRectangle(pen2, rectangle);
                pen2.Dispose();
            }
            e.Graphics.DrawRectangle(Pens.Black, contentRectangle);
        }
    }

    public abstract class MComponent : GH_Component
    {
        protected MComponent(string name, string nickname, string description, string category, string subCategory,
            string id, int exposure = 1, Bitmap icon = null) : base(name, nickname, description, category, subCategory)
        {
            ComponentGuid = new Guid(id);
            Exposure = FindExposure(exposure);
            Icon = icon;
        }
#if gaixie
        /// <summary>
        /// 请勿重写此函数而改为重写【AddParameter()】函数
        /// </summary>
        /// <param name="pManager"></param>
        protected sealed override void RegisterInputParams(GH_InputParamManager pManager)
            => AddParameter(new ParamManager(this));

        /// <summary>
        /// 请勿重写此函数而改为重写【AddParameter()】函数
        /// </summary>
        /// <param name="pManager"></param>
        protected sealed override void RegisterOutputParams(GH_OutputParamManager pManager)
        { }
        /// <summary>
        /// 添加输入输出端，注意 简称不能有重复
        /// </summary>
        /// <param name="pm">参数管理器</param>
        protected virtual void AddParameter(ParamManager pm)
        {
            pm.AddIP<Param_Arc>("", "", "");
            pm.AddIP(ParT.Angle, "", "", "");
        }

#endif
        /// <summary>
        /// 报错！
        /// </summary>
        /// <param name="b">True则报错</param>
        /// <param name="s">报错信息</param>
        /// <param name="m">报错等级</param>
        /// <returns>b</returns>
        public bool RuntimeMes(bool b, string s, GH_RuntimeMessageLevel m)
        {
            if (b)
                AddRuntimeMessage(m, s);
            return b;
        }

        #region 输入端预处理
        /// <summary>
        /// 判定数值是否超过上限
        /// </summary>
        /// <param name="num">要判定的数值</param>
        /// <param name="max">上限</param>
        /// <param name="contain">true为可以等于上限</param>
        /// <returns>输出的报错信息</returns>
        public static string Maxpd(double num, double max, bool contain)
            => contain ? (num > max ? $"不能大于上限{max}" : "") : (num >= max ? $"不能大于等于上限{max}" : "");
        /// <summary>
        /// 判定数值是否超过下限
        /// </summary>
        /// <param name="num">要判定的数值</param>
        /// <param name="min">下限</param>
        /// <param name="contain">true为可以等于下限</param>
        /// <returns>输出的报错信息</returns>
        public static string Minpd(double num, double min, bool contain)
            => contain ? (num < min ? $"不能大于上限{min}" : "") : (num <= min ? $"不能大于等于上限{min}" : "");
        /// <summary>
        /// 判定数值是否超过上限,,若失败则输出false并自动报错
        /// </summary>
        /// <param name="paInt">输入端索引</param>
        /// <param name="max">上限</param>
        /// <param name="contain">true为可以等于上限</param>
        /// <returns>true为不通过</returns>
        public void PMaxpd(int paInt, double max, bool contain)
        {
            if (Params.Input.Count <= paInt)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"PMaxpd的输入端索引{paInt}超范围，请检查");
                throw new IndexOutOfRangeException("PMaxpd");
            }
            IGH_Param inp = Params.Input[paInt];
            if (inp is Param_Integer || inp is Param_Number)
            {
                IGH_StructureEnumerator d = inp.VolatileData.AllData(true);
                foreach (IGH_Goo g in d)
                {
                    double num = g is GH_Number number ? number.Value : ((GH_Integer)g).Value;
                    string str1 = Maxpd(num, max, contain);
                    if (str1.Length == 0) continue;
                    throw new ArgumentOutOfRangeException($"{PaInt(paInt)}输入端的值{str1}!", new Exception());
                }
                return;
            }
            throw new ArgumentException($"{PaInt(paInt)}必须为整数/数值参数端!请检查paInt参数。");
            //string str1 = Pdmax(num, max, contain);
            //if (str1.Length == 0) return true;
            //AddRuntimeMessage(GH_RuntimeMessageLevel.Error,  $"{PaInt(paInt)}输入端的值{str1}!" );
            //return false;
        }
        /// <summary>
        /// 判定数值是否超过下限,若失败则输出false并自动报错
        /// </summary>
        /// <param name="paInt">输入端索引</param>
        /// <param name="min">下限</param>
        /// <param name="contain">true为可以等于下限</param>
        /// <returns>true为不通过</returns>
        public void PMinpd(int paInt, double min, bool contain)
        {
            if (Params.Input.Count <= paInt)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"PMinpd的输入端索引{paInt}超范围，请检查");
                throw new IndexOutOfRangeException("PMinpd");
            }
            IGH_Param inp = Params.Input[paInt];
            if (inp is Param_Integer || inp is Param_Number)
            {
                IGH_StructureEnumerator d = inp.VolatileData.AllData(true);
                foreach (IGH_Goo g in d)
                {
                    double num = g is GH_Number number ? number.Value : ((GH_Integer)g).Value;
                    string str2 = Minpd(num, min, contain);
                    if (str2.Length == 0) continue;
                    throw new ArgumentOutOfRangeException($"{PaInt(paInt)}输入端的值{str2}!", new Exception());
                }
                return;
            }
            throw new ArgumentException($"{PaInt(paInt)}必须为整数/数值参数端!请检查paInt参数。");
        }
        /// <summary>
        /// 判定数值是否超过上下限,,若失败则输出false并自动报错
        /// </summary>
        /// <param name="paInt">输入端索引</param>
        /// <param name="min">下限</param>
        /// <param name="max">上限</param>
        /// <param name="containx">true为可以等于下限</param>
        /// <param name="containd">true为可以等于上限</param>
        /// <returns>true为不通过</returns>
        public void PMutpd(int paInt, double min, double max, bool containx, bool containd)
        {
            if (Params.Input.Count <= paInt)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"PMutpd的输入端索引{paInt}超范围，请检查");
                throw new IndexOutOfRangeException("PMutpd");
            }
            IGH_Param inp = Params.Input[paInt];
            if (inp is Param_Integer || inp is Param_Number)
            {
                IGH_StructureEnumerator d = inp.VolatileData.AllData(true);
                foreach (IGH_Goo g in d)
                {
                    double num = g is GH_Number number ? number.Value : ((GH_Integer)g).Value;
                    string str1 = Maxpd(num, max, containd);
                    string str2 = Minpd(num, min, containx);
                    if (str1.Length == 0 && str2.Length == 0) continue;
                    throw new ArgumentOutOfRangeException($"{PaInt(paInt)}输入端的值{str1},{str2}!", new Exception());
                }
                return;
            }
            throw new ArgumentException($"{PaInt(paInt)}必须为整数/数值参数端!请检查paInt参数。");
        }
        /// <summary>
        /// 输入端输入物单项判定,判定失败会自动报错
        /// </summary>
        /// <param name="paInt">要判定的输入端索引</param>
        public void PDxiangpd(int paInt)
        {
            if (Params.Input.Count <= paInt)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"PDxiangpd的输入端索引{paInt}超范围，请检查");
                throw new IndexOutOfRangeException("PDxiangpd");
            }
            IGH_Param inp = Params.Input[paInt];
            if (inp.VolatileDataCount < 2) return;
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"{PaInt(paInt)}端只允许运算一项数据，已仅使用其第一分枝第一项");
            GH_Structure<IGH_Goo> a = (GH_Structure<IGH_Goo>)inp.VolatileData;
            inp.ClearData();
            foreach (IGH_Goo t in a.Branches[0])
            {
                if (t == null) continue;
                inp.AddVolatileData(a.Paths[0], 0, t);
                return;
            }
        }
        /// <summary>
        /// 输入端输入物单列判定,判定失败会自动报错,并转换
        /// </summary>
        /// <param name="paInt">要判定的输入端索引</param>
        public void PDliepd(int paInt)
        {
            if (Params.Input.Count <= paInt)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"PDliepd的输入端索引{paInt}超范围，请检查");
                throw new IndexOutOfRangeException("PDliepd");
            }
            IGH_Param inp = Params.Input[paInt];
            if (inp.VolatileData.PathCount < 2) return;
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"{PaInt(paInt)}端只允许运算一列数据，已仅使用其第一分枝");
            GH_Structure<IGH_Goo> a = (GH_Structure<IGH_Goo>)inp.VolatileData;
            inp.ClearData();
            inp.AddVolatileDataList(a.Paths[0], a.Branches[0]);
        }
        /// <summary>
        /// 两个输入端输入物的总项数相等判定，判定失败会自动报错
        /// </summary>
        /// <param name="paInt1">要判定的输入端索引1</param>
        /// <param name="paInt2">要判定的输入端索引2</param>
        public void PMutixianggxpd(int paInt1, int paInt2)
        {
            if (Params.Input.Count <= paInt1 || Params.Input.Count <= paInt2)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"PMutixianggxpd的输入端索引{paInt1}和/或{paInt2}超范围，请检查");
                throw new IndexOutOfRangeException("PMutixianggxpd");
            }
            IGH_Param inp1 = Params.Input[paInt1];
            IGH_Param inp2 = Params.Input[paInt2];
            if (inp1.VolatileDataCount != inp2.VolatileDataCount)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    $"{PaInt(paInt1)}端与{PaInt(paInt2)}端的总项数不相等");
        }
        /// <summary>
        /// 两个输入端输入物的列数相等判，判定失败会自动报错
        /// </summary>
        /// <param name="paInt1">要判定的输入端索引1</param>
        /// <param name="paInt2">要判定的输入端索引2</param>
        public void PMutiliegxpd(int paInt1, int paInt2)
        {
            if (Params.Input.Count <= paInt1 || Params.Input.Count <= paInt2)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"PMutiliegxpd的输入端索引{paInt1}和/或{paInt2}超范围，请检查");
                throw new IndexOutOfRangeException("PMutiliegxpd");
            }
            IGH_Param inp1 = Params.Input[paInt1];
            IGH_Param inp2 = Params.Input[paInt2];
            if (inp1.VolatileData.PathCount != inp2.VolatileData.PathCount)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    $"{PaInt(paInt1)}端与{PaInt(paInt2)}端的列数不相等");
        }
        /// <summary>
        /// 获取参数端 名称(简称)
        /// </summary>
        /// <param name="index">输入参数端索引</param>
        /// <returns>参数端名(简称)</returns>
        public string PaInt(int index)
        {
            IGH_Param par = Params.Input[index];
            return $@"【{par.Name}({par.NickName})】";
        }
        #endregion

        internal void Anli(bool enabled, ToolStripDropDown menu)
        {
            if (!enabled) return;
            ToolStripMenuItem menu案例 = Menu_AppendItem(menu, "案例", Menu_案例Clicked, true);
            menu案例.ToolTipText = "点击本项以打开此电池的案例";
        }
        // ReSharper disable once PossibleNullReferenceException

        protected virtual void Menu_案例Clicked(object sender, EventArgs e) =>
            Instances.DocumentEditor.ScriptAccess_OpenDocument(
                $@"{Folders.AppDataFolder}Example\{Name}-{NickName}.gh");
        #region MenuItem
        /// <summary>
        /// 绘制设置整数菜单项
        /// </summary>
        /// <param name="menu">所在菜单</param>
        /// <param name="text">文本</param>
        /// <param name="def">预设值及要被修改的值</param>
        /// <param name="tooltip">提示</param>
        /// <param name="recom">重计算？否则仅过期预览</param>
        /// <returns>设置好的菜单项</returns>
        public ToolStripMenuItem Menu_Integer(ToolStripDropDown menu, string text, GH_Integer def, string tooltip = null, bool recom = false)
        {
            ToolStripMenuItem dou = Menu_AppendItem(menu, text);
            if (!string.IsNullOrWhiteSpace(tooltip))
                dou.ToolTipText = tooltip;
            Menu_AppendTextItem(dou.DropDown, def.Value.ToString(CultureInfo.InvariantCulture), null,
                (sender, s) =>
                {
                    if (int.TryParse(s, out int d))
                    {
                        RecordUndoEvent($"设置{text}");
                        def.Value = d;
                        if (recom)

                            ExpireSolution(false);
                        else
                            ExpirePreview(false);
                    }
                    else
                        MessageBox.Show("输入的不是整数，请检查");
                }, false);
            return dou;
        }
        /// <summary>
        /// 绘制设置数值菜单项
        /// </summary>
        /// <param name="menu">所在菜单</param>
        /// <param name="text">文本</param>
        /// <param name="def">预设值及要被修改的值</param>
        /// <param name="tooltip">提示</param>
        /// <param name="recom">重计算？否则仅过期预览</param>
        /// <returns>设置好的菜单项</returns>
        public ToolStripMenuItem Menu_Double(ToolStripDropDown menu, string text, GH_Number def, string tooltip = null,
            Image icon = null, bool recom = false)
        {
            ToolStripMenuItem dou = Menu_AppendItem(menu, text, null, icon);
            if (!string.IsNullOrWhiteSpace(tooltip))
                dou.ToolTipText = tooltip;
            Menu_AppendTextItem(dou.DropDown, def.Value.ToString(CultureInfo.InvariantCulture), null,
                (sender, s) =>
                {
                    if (double.TryParse(s, out double d))
                    {
                        RecordUndoEvent($"设置{text}");
                        def.Value = d;
                        if (recom)

                            ExpireSolution(false);
                        else
                            ExpirePreview(false);
                    }
                    else
                        MessageBox.Show("输入的不是数值，请检查");
                }, false);
            return dou;
        }
        /// <summary>
        /// 绘制设置整数菜单项
        /// </summary>
        /// <param name="menu">所在菜单</param>
        /// <param name="text">文本</param>
        /// <param name="def">预设值及要被修改的值</param>
        /// <param name="tooltip">提示</param>
        /// <param name="recom">重计算？否则仅过期预览</param>
        /// <returns>设置好的菜单项</returns>
        public ToolStripMenuItem Menu_Color(ToolStripDropDown menu, string text, GH_Colour def, string tooltip = null, bool recom = false)
        {
            Color c = def.Value;
            ToolStripMenuItem col = Menu_AppendItem(menu, text, null, c.ToSprite(20, 20));
            if (!string.IsNullOrWhiteSpace(tooltip))
                col.ToolTipText = tooltip;
            Menu_AppendColourPicker(col.DropDown, c,
                (sender, e) =>
                {
                    RecordUndoEvent($"设置{text}");
                    def.Value = e.Colour;
                    col.Image = e.Colour.ToSprite(20, 20);
                    if (recom)

                        ExpireSolution(false);
                    else
                        ExpirePreview(false);
                });
            return col;
        }
        /// <summary>
        /// 绘制设置渐变菜单项
        /// </summary>
        /// <param name="menu">所在菜单</param>
        /// <param name="text">文本</param>
        /// <param name="def">预设值及要被修改的值</param>
        /// <param name="tooltip">提示</param>
        /// <param name="recom">重计算？否则仅过期预览</param>
        /// <returns>设置好的菜单项</returns>
        public ToolStripMenuItem Menu_Gradient(ToolStripDropDown menu, string text, GH_Gradient def, GH_Boolean rev, string tooltip = null, bool recom = false)
        {
            ToolStripMenuItem gradient = Menu_AppendItem(menu, text);
            gradient.Image = LTResource.Gradient_20x20;
            if (!string.IsNullOrWhiteSpace(tooltip))
                gradient.ToolTipText = tooltip;
            if (gradient.DropDown is ToolStripDropDownMenu downMenu)
                downMenu.ShowImageMargin = false;
            List<GH_Gradient> gradientPresets = GH_GradientControl.GradientPresets.ToArray().ToList();
            //把默认值插入为第一个
            gradientPresets.Insert(0, def);
            void GradientPresetClicked(object s, MouseEventArgs e)
            {
                MGradientMenuItem GradientMenuItem = (MGradientMenuItem)s;
                RecordUndoEvent($"设置{text}");
                //删除旧的
                for (int i = def.GripCount - 1; i >= 0; i--)
                    def.RemoveGrip(i);
                //添加新的
                for (int i = 0; i < GradientMenuItem.Gradient.GripCount; i++)
                    def.AddGrip(GradientMenuItem.Gradient[i]);

                if (recom)
                    ExpireSolution(true);
                else
                    ExpirePreview(true);
            }//创建点击事件 本地方法
            //把渐变都添加到菜单中
            foreach (GH_Gradient t in gradientPresets)
                gradient.DropDownItems.Add(new MGradientMenuItem(t, GradientPresetClicked));
            //插入提示文本
            gradient.DropDownItems[0].ToolTipText = "当前渐变";

            #region 反转渐变
            //去除 渐变项文本中的(&)
            var t0 = text.IndexOf("(&", StringComparison.Ordinal);
            var t1 = t0 < 0 ? text : text.Substring(0, t0);
            ToolStripMenuItem m = Menu_AppendItem(menu, $"反转{t1}(&R)",
                delegate
                {
                    rev.Value = !rev.Value;
                    if (recom)
                        ExpireSolution(true);
                    else
                        ExpirePreview(true);
                });
            m.ToolTipText = "仅反转渐变的映射效果，不修改渐变数据";
            m.Checked = rev.Value;
            #endregion
            return gradient;
        }
        #endregion
        public sealed override bool Locked
        {
            get => base.Locked;
            set => base.Locked = value;
        }
        private static GH_Exposure FindExposure(int exposure)
        {
            switch (exposure)
            {
                case 0:
                    return GH_Exposure.hidden;
                case 1:
                    return GH_Exposure.primary;
                case 2:
                    return GH_Exposure.secondary;
                case 3:
                    return GH_Exposure.tertiary;
                case 4:
                    return GH_Exposure.quarternary;
                case 5:
                    return GH_Exposure.quinary;
                case 6:
                    return GH_Exposure.senary;
                case 7:
                    return GH_Exposure.septenary;
                default:
                    throw new ArgumentOutOfRangeException(nameof(exposure), "输入的参数超范围，请在0-7之间（皆含）");
            }
        }
        public sealed override Guid ComponentGuid { get; }
        protected sealed override Bitmap Icon { get; }
        public sealed override GH_Exposure Exposure { get; }
        public enum ParT
        {
            Angle,
            Arc,
            Boolean,
            Box,
            Brep,
            Circle,
            Colour,
            ComplexNumber,
            Culture,
            Curve,
            Field,
            GenericObject,
            Geometry,
            Group,
            Integer,
            Interval2D,
            Interval,
            Line,
            Matrix,
            MeshFace,
            Mesh,
            Number,
            Path,
            Plane,
            Point,
            Rectangle,
            ScriptVariable,
            SubD,
            Surface,
            Text,
            Time,
            Transform,
            Vector,
        }
#if gaixie
        protected class ParamManager
        {
            // Token: 0x06005141 RID: 20801 RVA: 0x001B1187 File Offset: 0x001AF387
            internal ParamManager(MComponent component)
            => m_owner = component;
            protected void FixUpParameter(IGH_Param param, string name, string nickName, string description)
            {
                if (param == null)
                    Tracing.Assert(new Guid("{976FD532-DAF5-4a39-8E8F-221583D6C9AF}"), "param is a null reference.");
                // ReSharper disable once PossibleNullReferenceException
                if (param.Attributes == null)
                    param.Attributes = new GH_LinkedParamAttributes(param, m_owner.Attributes);
                param.Name = string.IsNullOrEmpty(name) ? "No" : name;
                // ReSharper disable once PossibleNullReferenceException
                param.NickName = string.IsNullOrEmpty(nickName) ? name[0].ToString().ToUpper() : nickName;
                param.Description = description ?? "";
            }
            /// <summary>
            /// 添加输入端
            /// </summary>
            /// <param name="type">参数端类型</param>
            /// <param name="name">名称</param>
            /// <param name="nickname">简称</param>
            /// <param name="description">定义</param>
            /// <param name="trait">参数端状态</param>
            /// <param name="def">默认多个值的话必须使用[]而非List<>，否则会识别不了</param>
            /// <returns>输出设置好的参数端</returns>

            #region AddP
            /// <summary>
            /// 添加输入端
            /// </summary>
            /// <param name="type">参数端类型</param>
            /// <param name="name">名称</param>
            /// <param name="nickname">简称</param>
            /// <param name="description">定义</param>
            /// <param name="trait">参数端状态</param>
            /// <param name="def">默认多个值的话必须使用[]而非List<>，否则会识别不了</param>
            /// <returns>输出设置好的参数端</returns>
            public int AddIP(ParT type, string name, string nickname, string description
                , ParamTrait trait = ParamTrait.Item, params object[] def)
                => AddP(false, type, name, nickname, description, trait, def);
            /// <summary>
            /// 添加输入端
            /// </summary>
            /// <param name="ip">要设置并添加的参数端</param>
            /// <param name="name">名称</param>
            /// <param name="nickname">简称</param>
            /// <param name="description">定义</param>
            /// <param name="trait">参数端状态</param>
            /// <returns>输出设置好的参数端</returns>
            public int AddIP(IGH_Param ip, string name, string nickname, string description,
                ParamTrait trait = ParamTrait.Item)
                => AddP(false, ip, name, nickname, description, trait);
            /// <summary>
            /// 添加输入端
            /// </summary>
            /// <typeparam name="T">参数端的类型</typeparam>
            /// <param name="name">名称</param>
            /// <param name="nickname">简称</param>
            /// <param name="description">定义</param>
            /// <param name="trait">参数端状态</param>
            /// <returns>输出设置好的参数端</returns>
            public T AddIP<T>(string name, string nickname, string description,
                ParamTrait trait = ParamTrait.Item, params object[] def) where T : IGH_Param, new()
           => AddP<T>(false, name, nickname, description, trait, def);
            public int AddOP(ParT type, string name, string nickname, string description,
                ParamTrait trait = ParamTrait.Item)
                => AddP(true, type, name, nickname, description, trait);
            public int AddOP(IGH_Param ip, string name, string nickname, string description,
                ParamTrait trait = ParamTrait.Item)
                => AddP(true, ip, name, nickname, description, trait);
            public T AddOP<T>(string name, string nickname, string description,
                ParamTrait trait = ParamTrait.Item) where T : IGH_Param, new()
                => AddP<T>(true, name, nickname, description, trait);

            internal int AddP(bool io, ParT type, string name, string nickname, string description,
                ParamTrait trait = ParamTrait.Item, params object[] def)
                => AddP(io, PT2IParam(type, def), name, nickname, description, trait);

            internal int AddP(bool io, IGH_Param ip, string name, string nickname, string description,
                ParamTrait trait)
            {
                FixUpParameter(ip, name, nickname, description);
                this[io].Add(SetTrait(ip, trait));
                return this[io].Count - 1;
            }

            internal T AddP<T>(bool io, string name, string nickname, string description,
                ParamTrait trait, params object[] def) where T : IGH_Param, new()
            {
                T ip = SetGHP<T>(def);
                FixUpParameter(ip, name, nickname, description);

                this[io].Add(SetTrait(ip, trait));
                return ip;
            }

            #endregion

            #region ParamTrait
            private static IGH_Param SetTrait(IGH_Param ip, ParamTrait t)
            {
                ip.Access = Trait2Access(t);
                if (t.HasFlag(ParamTrait.Hidden) && ip is IGH_PreviewObject ipo)
                    ipo.Hidden = true;
                ip.Optional = t.HasFlag(ParamTrait.Optional);
                if (t.HasFlag(ParamTrait.IsAngle))
                    if (ip is Param_Number pn)
                        pn.AngleParameter = true;
                    else
                        throw new ArgumentOutOfRangeException(nameof(t), $"{nameof(ParamTrait.IsAngle)}只能用于【数值】参数端");
                ip.DataMapping = Trait2DataMap(t);
                ip.Simplify = t.HasFlag(ParamTrait.Simplify);
                ip.Reverse = t.HasFlag(ParamTrait.Reverse);
                return ip;
            }

            private static GH_ParamAccess Trait2Access(ParamTrait t)
            {
                if (t.HasFlag(ParamTrait.Item))
                    return GH_ParamAccess.item;
                if (t.HasFlag(ParamTrait.List))
                    return GH_ParamAccess.list;
                if (t.HasFlag(ParamTrait.Tree))
                    return GH_ParamAccess.tree;
                throw new ArgumentOutOfRangeException(nameof(t),
                    $"{nameof(ParamTrait)}必须含有输入数据结构标志，以便其设定{nameof(GH_ParamAccess)}");
            }

            private static GH_DataMapping Trait2DataMap(ParamTrait t)
            {
                if (t.HasFlag(ParamTrait.Flatten))
                    return GH_DataMapping.Flatten;
                return t.HasFlag(ParamTrait.Graft) ? GH_DataMapping.Graft : GH_DataMapping.None;
            }


            #endregion

            private IGH_Param PT2IParam(ParT type, params object[] def)
            {
                try
                {
                    switch (type)
                    {
                        case ParT.Angle:
                            Param_Number ip = SetGHP<Param_Number>(def);
                            ip.AngleParameter = true;
                            return ip;
                        case ParT.Arc:
                            return SetGHP<Param_Arc>(def);
                        case ParT.Boolean:
                            return SetGHP<Param_Boolean>(def);
                        case ParT.Box:
                            return SetGHP<Param_Box>(def);
                        case ParT.Brep:
                            return SetGHP<Param_Brep>(def);
                        case ParT.Circle:
                            return SetGHP<Param_Circle>(def);
                        case ParT.Colour:
                            return SetGHP<Param_Colour>(def);
                        case ParT.ComplexNumber:
                            return SetGHP<Param_Complex>(def);
                        case ParT.Culture:
                            return SetGHP<Param_Culture>(def);
                        case ParT.Curve:
                            return SetGHP<Param_Curve>(def);
                        case ParT.Field:
                            return SetGHP<Param_Field>(def);
                        case ParT.GenericObject:
                            return SetGHP<Param_GenericObject>(def);
                        case ParT.Geometry:
                            return SetGHP<Param_Geometry>(def);
                        case ParT.Group:
                            return SetGHP<Param_Group>(def);
                        case ParT.Integer:
                            return SetGHP<Param_Integer>(def);
                        case ParT.Interval2D:
                            return SetGHP<Param_Interval2D>(def);
                        case ParT.Interval:
                            return SetGHP<Param_Interval>(def);
                        case ParT.Line:
                            return SetGHP<Param_Line>(def);
                        case ParT.Matrix:
                            return SetGHP<Param_Matrix>(def);
                        case ParT.MeshFace:
                            return SetGHP<Param_MeshFace>(def);
                        case ParT.Mesh:
                            return SetGHP<Param_Mesh>(def);
                        case ParT.Number:
                            return SetGHP<Param_Number>(def);
                        case ParT.Path:
                            return SetGHP<Param_StructurePath>(def);
                        case ParT.Plane:
                            return SetGHP<Param_Plane>(def);
                        case ParT.Point:
                            return SetGHP<Param_Point>(def);
                        case ParT.Rectangle:
                            return SetGHP<Param_Rectangle>(def);
                        case ParT.ScriptVariable:
                            return SetGHP<Param_ScriptVariable>(def);
                        case ParT.SubD:
#if R7
                            return SetGHP<Param_SubD>(def);
#else
                            throw new ArgumentOutOfRangeException(nameof(type), "ParT.SubD仅在rhino7可用");
#endif
                        case ParT.Surface:
                            return SetGHP<Param_Surface>(def);
                        case ParT.Text:
                            return SetGHP<Param_String>(def);
                        case ParT.Time:
                            return SetGHP<Param_Time>(def);
                        case ParT.Transform:
                            return SetGHP<Param_Transform>(def);
                        case ParT.Vector:
                            return SetGHP<Param_Vector>(def);
                        default:
                            throw new ArgumentOutOfRangeException(nameof(type), type, null);
                    }
                }
                catch (ArgDefException)
                {
                    throw new ArgumentException($"{type}类型不支持添加默认值，请勿输入{nameof(def)}参数，或使其为null", nameof(type));
                }
            }

            /// <summary>
            /// 生成类型为Q的参数端，并将默认值设置给其
            /// </summary>
            /// <typeparam name="Q">输出的参数端具体类型</typeparam>
            /// <param name="def">默认数据，无则不输入,若其类型不被参数端所接受则会添加等量的默认类型的默认实例</param>
            /// <returns></returns>
            /// <exception cref="ArgumentException">Q非GH_PersistentParam，def却有值</exception>
            private static Q SetGHP<Q>(params object[] def) where Q : IGH_Param, new()
            {
                Q ip = new Q();
                //无默认直接输出
                if (def.Length == 0)
                    return ip;
                MethodInfo Method = ip.GetType().GetMethod("SetPersistentData", new[] { typeof(object[]) });
                if (Method == null)
                    throw new ArgumentException(
                        $"{nameof(Q)}不具有【SetPersistentData(params object[])】方法，\r\n可能不派生自【GH_PersistentParam<T>】,\r\n请不要给此参数端设置默认值");
                Method.Invoke(ip, new object[] { def });
                return ip;
            }
            public int IParamCount => this[false].Count;
            public int OParamCount => this[true].Count;
            /// <summary>
            /// 获取参数列表
            /// </summary>
            /// <param name="io">true为输入，false为输入</param>
            /// <returns>对应的输入或输出端列表</returns>
            public List<IGH_Param> this[bool io]
                => io ? m_owner.Params.Output : m_owner.Params.Input;
            /// <summary>
            /// 按索引获取参数端
            /// </summary>
            /// <param name="io">true为输入，false为输入</param>
            /// <param name="index"></param>
            /// <returns>对应的参数端</returns>
            public IGH_Param this[bool io, int index]
                => this[io][index];
            internal MComponent m_owner;
        }
        /// <summary>
        /// 参数端特性，数据组织类型、预览可见性，可选性、已命名列表、是否为角度、数据映射操作
        /// </summary>
        [Flags]
        public enum ParamTrait
        {
            Item = 1 << 0,//同 GH_ParamAccess，三项只能有一项，否则只执行数值小的项
            List = 1 << 1,
            Tree = 1 << 2,
            //todo 需要实现
            OnlyOne = 1 << 3,//配合上面可致 仅接受第一个分支/（的第一项）数据
            Hidden = 1 << 4,//隐藏此参数端的预览
            Optional = 1 << 5,//此参数端为可选，此时勿输入不会被报错
            //NamedList = 1 << 6,//此参数端有已命名项，仅当【整数】参数端时有效
            IsAngle = 1 << 7,//此参数端为角度参数端，仅当其为【数值】参数端时有效
            Simplify = 1 << 8,//简化参数路径
            Reverse = 1 << 9,//反转数据数序
            Flatten = 1 << 10,//摊平，不能与下同时存在，否则只执行摊平
            Graft = 1 << 11//升枝，不能与上同时存在
        }
#endif
    }

    public abstract class MOComponent : MComponent
    {
        protected MOComponent(string name, string nickname, string description, string category, string subCategory, string id, string nname, Bitmap icon = null) :
            base(name, nickname, description, category, subCategory, id, 0, icon)
        {
            NewName = nname;
        }

        protected override void BeforeSolveInstance()
            => AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"此电池已过期，请自行手工替换为新电池【{NewName}】！");

        protected override void SolveInstance(IGH_DataAccess DA)
        { }

        public override bool Obsolete => true;
        public string NewName;
    }

    [Serializable]
    public class ArgDefException : Exception { }
    public static class Majas_Ex
    {
        public static Param_Integer AddNamedValueL(this Param_Integer a, IEnumerable<string> b)
        {
            StringBuilder s = new StringBuilder();
            using (var be = b.GetEnumerator())
            {
                int i = 0;
                while (be.MoveNext())
                {
                    a.AddNamedValue(be.Current, i);
                    s.AppendLine($"{i}-{be.Current},");
                    i++;
                }
            }
            a.Description += s.ToString();
            return a;
        }
        public static Param_Integer AddNamedValueE<T>(this Param_Integer a) where T : Enum
        {
            StringBuilder s = new StringBuilder();
            Type t = typeof(T);
            foreach (var n0 in t.GetEnumNames())
            {
                int i = (int)Enum.Parse(t, n0);
                a.AddNamedValue(n0, i);
                s.AppendLine($"{i}-{n0},");
            }
            a.Description += s.ToString();
            return a;
        }
        public static bool GetDataM<T>(this IGH_DataAccess da, int index, out T destination)
        {
            T d = default;
            var b = da.GetData(index, ref d);
            destination = d;
            return b;
        }
        public static bool GetDataListM<T>(this IGH_DataAccess da, int index, out List<T> destination)
        {
            List<T> d = new List<T>();
            var b = da.GetDataList(index, d);
            destination = d;
            return b;
        }
        public static Interval ToInterval(this IEnumerable<double> l)
        {
            using (IEnumerator<double> e = l.GetEnumerator())
            {
                if (!e.MoveNext())
                    return Interval.Unset;
                var i0 = e.Current;
                var i1 = e.MoveNext() ? e.Current : i0;
                Interval r = new Interval(i0, i1);
                if (r.IsDecreasing) r.Swap();
                while (e.MoveNext())
                    r.Grow(e.Current);
                return r;
            }
        }
        /// <summary>
        /// 根据尺寸生成纯色图片
        /// </summary>
        /// <param name="c">颜色</param>
        /// <param name="w">宽度</param>
        /// <param name="h">高度</param>
        /// <returns>生成的纯色图片</returns>
        public static Bitmap ToBitmap(this Color c, int w, int h)
        {
            Bitmap bmp = new Bitmap(w, h);
            for (int i = 0; i < w; i++)
                for (int j = 0; j < h; j++)
                    bmp.SetPixel(i, j, c);
            return bmp;
        }

        public static Bitmap ToSprite(this Color c, int w, int h)
        {
            Bitmap b0 = new Bitmap(w, h);
            using (Bitmap b= LTResource.Sprite_20x20)
            {
                for (int i = 0; i < 20; i++)
                for (int j = 0; j < 20; j++)
                        b0.SetPixel(i, j, Color.FromArgb(b.GetPixel(i,j).A,c));
            }
            return b0;
        }

        #region Gradient
        public static GH_Gradient GetGradient(this GH_IReader reader, string item_name)
        {
            GH_Gradient gra = new GH_Gradient();
            gra.Read(reader.FindChunk(item_name));
            return gra;
        }

        public static void SetGradient(this GH_IWriter writer, string item_name, GH_Gradient gra)
            => gra.Write(writer.CreateChunk(item_name));
        /// <summary>
        /// 将数值按区间转换成渐变色彩
        /// </summary>
        /// <param name="gra">渐变</param>
        /// <param name="it">数值所在区间范围</param>
        /// <param name="v">数值</param>
        /// <param name="reverse">是否反转渐变</param>
        /// <returns>数值对应的色彩</returns>
        public static Color Double2GraColor(this GH_Gradient gra, Interval it, double v, bool reverse)
        {
            var n = it.NormalizedParameterAt(v);
            return gra.ColourAt(reverse ? 1 - n : n);
        }
        public static GH_Gradient CopyFrom(this GH_Gradient g0, GH_Gradient g1)
        {
            for (int i = g0.GripCount - 1; i >= 0; i--)
                g0.RemoveGrip(i);
            for (int i = 0; i < g1.GripCount; i++)
                g0.AddGrip(g1[i].Parameter, g1[i].ColourLeft, g1[i].ColourRight);
            g0.Linear = g1.Linear;
            g0.Locked = g1.Locked;

            return g0;
        }
        public static GH_Gradient Duplicate(this GH_Gradient g1)
        {
            GH_Gradient g0 = new GH_Gradient
            {
                Linear = g1.Linear,
                Locked = g1.Locked
            };
            for (int i = 0; i < g1.GripCount; i++)
                g0.AddGrip(g1[i].Parameter, g1[i].ColourLeft, g1[i].ColourRight);
            return g0;
        }
        #endregion
    }
}