#define gaixie
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.GUI.Gradient;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Rhino.DocObjects;
using Rhino;

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
            bool set = component.Attributes.GetTopLevel.Selected;
            GH_PreviewMeshArgs args2 = new GH_PreviewMeshArgs(args.Viewport, args.Display,
                (set ? args.ShadeMaterial_Selected : args.ShadeMaterial), args.MeshingParameters);
            if (lmesh.Count == 0) return; ///避免网格不存在
            Mesh mesh = lmesh[0];
            if (mesh.VertexColors.Count > 0 && !set) //仅存在着色且未被选取时
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
        /// <summary>
        /// 渐变项
        /// </summary>
        /// <param name="doco">要被添加至的电池</param>
        /// <param name="menu">要被添加的菜单</param>
        /// <param name="text">菜单项名</param>
        /// <param name="gra">电池中存储渐变的字段</param>
        public static ToolStripMenuItem Menu_Gradient(this GradientComponent doco, ToolStripDropDown menu, string text, string tooltip, GH_Gradient gra)
        {
            ToolStripMenuItem gradient = GH_DocumentObject.Menu_AppendItem(menu, text);
            gradient.ToolTipText = tooltip;
            if (gradient.DropDown is ToolStripDropDownMenu downMenu)
                downMenu.ShowImageMargin = false;
            List<GH_Gradient> gradientPresets = GH_GradientControl.GradientPresets;
            for (int i = 0; i <= gradientPresets.Count - 1; i++)
            {
                LT_GradientMenuItem GraMenuItem = new LT_GradientMenuItem(doco, gra)
                {
                    Gradient = gradientPresets[i],
                    Index = i
                };
                gradient.DropDownItems.Add(GraMenuItem);
            }
            return gradient;
        }
        public static bool GetGradient(this GH_IReader reader, string item_name, GH_Gradient gra)
        {
            if (!(reader.FindChunk(item_name) is GH_IReader r)) return false;
            gra.Read(r);
            return true;
        }
        public static bool SetGradient(this GH_IWriter writer, string item_name, GH_Gradient gra)
        {
            GH_IWriter w = writer.CreateChunk(item_name);
            return gra.Write(w);
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

    public abstract class GradientComponent : AComponent
    {
        protected GradientComponent(string name, string nickname, string description, string subCategory, string id, int exposure = 1, Bitmap icon = null) :
            base(name, nickname, description, subCategory, id, exposure, icon)
        {
        }
        public override bool Read(GH_IReader reader)
        {
            base.Read(reader);
            return reader.GetGradient("渐变", Gradient);
        }
        public override bool Write(GH_IWriter writer)
        {
            base.Write(writer);
            return writer.SetGradient("渐变", Gradient);
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
            => this.Menu_Gradient(menu, "渐变",
                "左小右大，若修改后预览无变化，请重计算本电池,并告知开发者修复\r\n要新增预设，请依靠【渐变】电池制作渐变并使用其右键菜单项\r\n【添加当前渐变Add Current Gradient】", Gradient);
        internal void Menu_GradientPresetClicked(object sender, MouseEventArgs e)
        {
            GH_GradientMenuItem gh_GradientMenuItem = (GH_GradientMenuItem)sender;
            RecordUndoEvent("设置渐变");
            Gradient = gh_GradientMenuItem.Gradient;
            ExpirePreview(true);

        }

        private GH_Gradient _gradient;
        protected GH_Gradient Gradient
        {
            get => _gradient;
            set
            {
                _gradient = value;
                GradientChange?.Invoke(this, EventArgs.Empty);
            }
        }
        public event GradientEventHandler GradientChange;

        public delegate void GradientEventHandler(IGH_DocumentObject sender, EventArgs e);
    }

    public sealed class LT_GradientMenuItem : ToolStripMenuItem
    {
        /// <summary>
        /// 添加渐变菜单项
        /// </summary>
        /// <param name="grac">被添加至的电池</param>
        /// <param name="gra">电池中对应使用的渐变</param>
        public LT_GradientMenuItem(GradientComponent grac, GH_Gradient gra)
        {
            Gradient = gra;
            DisplayStyle = ToolStripItemDisplayStyle.None;
            Text = "渐变预设";
            Margin = new Padding(1);
            Paint += LT_GradientMenuItem_Paint;
            MouseDown += grac.Menu_GradientPresetClicked;
        }

        public GH_Gradient Gradient { get; set; }
        public int Index { get; set; }
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

            public int AddIP(ParT type, string name, string nickname, string description
                , ParamTrait trait = ParamTrait.Item, params object[] def)
                => AddP(false, type, name, nickname, description, trait, def);
            public int AddIP(IGH_Param ip, string name, string nickname, string description,
                ParamTrait trait = ParamTrait.Item)
                => AddP(false, ip, name, nickname, description, trait);

            public T AddIP<T>(string name, string nickname, string description,
                ParamTrait trait = ParamTrait.Item) where T : IGH_Param, new()
           => AddP(false, new T(), name, nickname, description, trait);
            public int AddOP(ParT type, string name, string nickname, string description,
                ParamTrait trait = ParamTrait.Item)
                => AddP(true, type, name, nickname, description, trait);
            public int AddOP(IGH_Param ip, string name, string nickname, string description,
                ParamTrait trait = ParamTrait.Item)
                => AddP(true, ip, name, nickname, description, trait);
            public T AddOP<T>(string name, string nickname, string description,
                ParamTrait trait = ParamTrait.Item) where T : IGH_Param, new()
                => AddP(true, new T(), name, nickname, description, trait);
            private int AddP(bool io, ParT type, string name, string nickname, string description,
                ParamTrait trait = ParamTrait.Item, params object[] def)
                => AddP(io, PT2IParam(type, def), name, nickname, description, trait);
            private int AddP(bool io, IGH_Param ip, string name, string nickname, string description,
                ParamTrait trait)
            {
                FixUpParameter(ip, name, nickname, description);
                this[io].Add(SetTrait(ip, trait));
                return this[io].Count - 1;
            }
            private T AddP<T>(bool io, T ip, string name, string nickname, string description,
                ParamTrait trait) where T : IGH_Param
            {
                FixUpParameter(ip, name, nickname, description);
                this[io].Add(SetTrait(ip, trait));
                return ip;
            }

            #region ParamTrait
            private IGH_Param SetTrait(IGH_Param ip, ParamTrait t)
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
                    $"{nameof(ParamTrait)}必须含有输入数据结构标志，同{nameof(GH_ParamAccess)}");
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
                            Param_Number ip = SetGHP<Param_Number, double>(ParamCategory.PeGo, def);
                            ip.AngleParameter = true;
                            return ip;
                        case ParT.Arc:
                            return SetGHP<Param_Arc, Arc>(ParamCategory.PeGe, def);
                        case ParT.Boolean:
                            return SetGHP<Param_Boolean, bool>(ParamCategory.PeGo, def);
                        case ParT.Box:
                            return SetGHP<Param_Box, Box>(ParamCategory.PeGe, def);
                        case ParT.Brep:
                            return SetGHP<Param_Brep, Brep>(ParamCategory.PeGe, def);
                        case ParT.Circle:
                            return SetGHP<Param_Circle, Circle>(ParamCategory.PeGe, def);
                        case ParT.Colour:
                            return SetGHP<Param_Colour, Color>(ParamCategory.PeGo, def);
                        case ParT.ComplexNumber:
                            return SetGHP<Param_Complex, Complex>(ParamCategory.PeGo, def);
                        case ParT.Culture:
                            return SetGHP<Param_Culture, CultureInfo>(ParamCategory.PeGo, def);
                        case ParT.Curve:
                            return SetGHP<Param_Curve, Curve>(ParamCategory.PeGe, def);
                        case ParT.Field:
                            return SetGHP<Param_Field, GH_Field>(ParamCategory.PaIG, def);
                        case ParT.GenericObject:
                            return SetGHP<Param_GenericObject, IGH_Goo>(ParamCategory.PeIO, def);
                        case ParT.Geometry:
                            return SetGHP<Param_Geometry, IGH_GeometricGoo>(ParamCategory.PeIG, def);
                        case ParT.Group:
                            return SetGHP<Param_Group, GH_GeometryGroup>(ParamCategory.PaIG, def);
                        case ParT.Integer:
                            return SetGHP<Param_Integer, int>(ParamCategory.PeGo, def);
                        case ParT.Interval2D:
                            return SetGHP<Param_Interval2D, UVInterval>(ParamCategory.PeGo, def);
                        case ParT.Interval:
                            return SetGHP<Param_Interval, Interval>(ParamCategory.PeGo, def);
                        case ParT.Line:
                            return SetGHP<Param_Line, Line>(ParamCategory.PeGe, def);
                        case ParT.Matrix:
                            return SetGHP<Param_Matrix, Matrix>(ParamCategory.PeGo, def);
                        case ParT.MeshFace:
                            return SetGHP<Param_MeshFace, MeshFace>(ParamCategory.PeGo, def);
                        case ParT.Mesh:
                            return SetGHP<Param_Mesh, Mesh>(ParamCategory.PeGe, def);
                        case ParT.Number:
                            return SetGHP<Param_Number, double>(ParamCategory.PeGo, def);
                        case ParT.Path:
                            return SetGHP<Param_StructurePath, GH_Path>(ParamCategory.PeGo, def);
                        case ParT.Plane:
                            return SetGHP<Param_Plane, Plane>(ParamCategory.PeGe, def);
                        case ParT.Point:
                            return SetGHP<Param_Point, Point3d>(ParamCategory.PeGe, def);
                        case ParT.Rectangle:
                            return SetGHP<Param_Rectangle, Rectangle3d>(ParamCategory.PeGe, def);
                        case ParT.ScriptVariable:
                            return SetGHP<Param_ScriptVariable, IGH_Goo>(ParamCategory.PeIO, def);
                        case ParT.SubD:
#if R7
                            return SetGHP<Param_SubD, SubD>(ParamCategory.PeGe, def);
#else
                            throw new ArgumentOutOfRangeException(nameof(type), "ParT.SubD仅在rhino7可用");
#endif
                        case ParT.Surface:
                            return SetGHP<Param_Surface, Surface>(ParamCategory.PeGe, def);
                        case ParT.Text:
                            return SetGHP<Param_String, string>(ParamCategory.PeGo, def);
                        case ParT.Time:
                            return SetGHP<Param_Time, DateTime>(ParamCategory.PeGo, def);
                        case ParT.Transform:
                            return SetGHP<Param_Transform, Transform>(ParamCategory.PeGo, def);
                        case ParT.Vector:
                            return SetGHP<Param_Vector, Vector3d>(ParamCategory.PeGo, def);
                        default:
                            throw new ArgumentOutOfRangeException(nameof(type), type, null);
                    }
                }
                catch (ArgDefException)
                {
                    throw new ArgumentException($"{type}类型不支持添加默认值，请勿输入{nameof(def)}参数，或使其为null", nameof(type));
                }
            }


            [Flags]
            private enum ParamCategory
            {
                Param,
                Persistent,
                IGoo,
                Goo,
                IGeometricGoo,
                GeometricGoo,
                PaIG = Param & IGoo,
                PeGe = Persistent & GeometricGoo,
                PeGo = Persistent & Goo,
                PeIO = Persistent & IGoo,
                PeIG = Persistent & IGeometricGoo
            }
            /// <summary>
            /// 生成类型为Q的参数端，并将默认值设置给其 待 debug
            /// </summary>
            /// <typeparam name="Q">输出的参数端具体类型</typeparam>
            /// <typeparam name="T">参数端默认数据的类型</typeparam>
            /// <param name="t">参数端类型枚举，手工设置t，若使用反射从Q上推导也行，但会太慢</param>
            /// <param name="def">默认数据，无则输入null</param>
            /// <returns>类型为Q的参数端</returns>
            /// <exception cref="ArgDefException">Q为非持久时，def必须为null</exception>
            /// <exception cref="ArgumentException">def中数据的类型不是T是报错</exception>
            private static Q SetGHP<Q, T>(ParamCategory t, params object[] def) where Q : IGH_Param, new()
            {
                Q ip = new Q();
                //无默认直接输出
                if (def == null)
                    return ip;
                //非保持 却有默认就报错
                if (!t.HasFlag(ParamCategory.Persistent))
                    throw new ArgDefException();

                if (t.HasFlag(ParamCategory.IGoo))
                {
                    if (ip is GH_PersistentParam<IGH_Goo> gg && def is IEnumerable<IGH_Goo> d)
                        gg.SetPersistentData(d);
                    else goto ArgErr;
                }
                if (t.HasFlag(ParamCategory.Goo))
                {
                    if (ip is GH_PersistentParam<GH_Goo<T>> gg && def is T[] d)
                        gg.SetPersistentData(d);
                    else goto ArgErr;
                }
                if (t.HasFlag(ParamCategory.IGeometricGoo))
                {
                    if (ip is GH_PersistentParam<IGH_GeometricGoo> gg && def is IEnumerable<IGH_GeometricGoo> d)
                        gg.SetPersistentData(d);
                    else goto ArgErr;
                }
                if (t.HasFlag(ParamCategory.GeometricGoo))
                {
                    if (ip is GH_PersistentParam<GH_GeometricGoo<T>> gg && def is T[] d)
                        gg.SetPersistentData(d);
                    else goto ArgErr;
                }

                return ip;
            ArgErr:
                throw new ArgumentException($"【AddIParameter】中参数{nameof(def)}中项的类型必须{typeof(T)}，请检查");
            }
            /// <summary>
            /// 生成类型为Q的参数端，并将默认值设置给其 待 
            /// </summary>
            /// <typeparam name="Q">输出的参数端具体类型</typeparam>
            /// <typeparam name="T">参数端默认数据的类型</typeparam>
            /// <param name="def">默认数据，无则输入null</param>
            /// <returns>类型为Q的参数端</returns>
            /// <exception cref="ArgDefException">Q为非持久时，def必须为null</exception>
            /// <exception cref="ArgumentException">def中数据的类型不是T是报错</exception>
            private static Q SetGHP<Q, T>(IEnumerable<object> def) where Q : IGH_Param, new()
            {
                Q ip = new Q();
                //无默认直接输出
                if (def == null)
                    return ip;

                Type bt = ip.Type.BaseType;
                //非保持 却有默认就报错
                Debug.Assert(bt != null, nameof(bt) + " != null");
                if (!bt.ToString().Contains("GH_PersistentParam") || !bt.ToString().Contains("GH_ExpressionParam"))
                    throw new ArgDefException();
                var bbt = bt.GenericTypeArguments[0].BaseType?.ToString();
                Debug.Assert(bbt != null, nameof(bbt) + " != null");
                if (bbt.Contains("GH_Goo"))
                {
                    if (ip is GH_PersistentParam<GH_Goo<T>> gg && def is IEnumerable<T> d)
                        gg.SetPersistentData(d);
                    else goto ArgErr;
                }
                if (bbt.Contains("IGH_GeometricGoo"))
                {
                    if (ip is GH_PersistentParam<IGH_GeometricGoo> gg && def is IEnumerable<IGH_GeometricGoo> d)
                        gg.SetPersistentData(d);
                    else goto ArgErr;
                }
                if (bbt.Contains("GH_GeometricGoo"))
                {
                    if (ip is GH_PersistentParam<GH_GeometricGoo<T>> gg && def is IEnumerable<T> d)
                        gg.SetPersistentData(d);
                    else goto ArgErr;
                }

                if (bbt.Contains("IGH_Goo"))
                {
                    if (ip is GH_PersistentParam<IGH_Goo> gg && def is IEnumerable<IGH_Goo> d)
                        gg.SetPersistentData(d);
                    else goto ArgErr;
                }

                return ip;
            ArgErr:
                throw new ArgumentException($"【AddIParameter】中参数{nameof(def)}中项的类型必须{typeof(T)}，请检查");
            }
            public int IParamCount => this[false].Count;
            public int OParamCount => this[true].Count;
            /// <summary>
            /// 
            /// </summary>
            /// <param name="io">true为输入，false为输入</param>
            /// <returns></returns>
            public List<IGH_Param> this[bool io]
                => io ? m_owner.Params.Output : m_owner.Params.Input;
            /// <summary>
            /// 
            /// </summary>
            /// <param name="io">true为输入，false为输入</param>
            /// <param name="index"></param>
            /// <returns></returns>
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
            Item = 0,//同 GH_ParamAccess，三项只能有一项，否则只执行数值小的项
            List = 1 << 0,
            Tree = 1 << 1,
            //todo 需要实现
            OnlyOne = 1 << 2,//配合上面可致 仅接受第一个分支/（的第一项）数据
            Hidden = 1 << 3,//隐藏此参数端的预览
            Optional = 1 << 4,//此参数端为可选，此时勿输入不会被报错
            //NamedList = 1 << 5,//此参数端有已命名项，仅当【整数】参数端时有效
            IsAngle = 1 << 6,//此参数端为角度参数端，仅当其为【数值】参数端时有效
            Simplify = 1 << 7,//简化参数路径
            Reverse = 1 << 8,//反转数据数序
            Flatten = 1 << 9,//摊平，不能与下同时存在，否则只执行摊平
            Graft = 1 << 10//升枝，不能与上同时存在
        }

#endif

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
    }
}