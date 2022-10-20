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
    /// ͨ��
    /// </summary>
    public static class Ty
    {
        /// <summary>
        /// ����Ԥ�����񣬣���δ��ѡ��ʱ��ʾαɫ����������дDrawViewportMeshes��
        /// </summary>
        /// <param name="a">���������</param>
        /// <param name="component">��ر��壬Ĭ������this</param>
        /// <param name="args">Ԥ��������Ĭ������ args</param>
        public static void Draw1Meshes(int a, IGH_Component component, IGH_PreviewArgs args)
        {
            var lmesh = component.Params.Output[a].VolatileData.AllData(true).Select(t => ((GH_Mesh)t).Value).ToList();
            if (lmesh.Count == 0) return; ///�������񲻴���
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
            Gradient = reader.GetGradient("����");
            Rev.Value = reader.GetBoolean("��ת����");
            ReCom = reader.GetBoolean("�����");
            return base.Read(reader);
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.SetGradient("����", Gradient);
            writer.SetBoolean("��ת����", Rev.Value);
            writer.SetBoolean("�����", ReCom);
            return base.Write(writer);
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            Menu_Gradient(menu, "����(&G)", Gradient, Rev,
                "��С�Ҵ�\r\n���޸ĺ�Ԥ���ޱ仯�����ؼ��㱾���,����֪�������޸�\r\nҪ����Ԥ�裬�����������䡿����������䲢ʹ�����Ҽ��˵���\r\n����ӵ�ǰ����Add Current Gradient��",
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
        /// ��ӽ���˵���
        /// </summary>
        /// <param name="grac">��������ĵ��</param>
        /// <param name="gra">����ж�Ӧʹ�õĽ���</param>
        public MGradientMenuItem(GH_Gradient gra, MouseEventHandler even)
        {
            Gradient = gra;
            DisplayStyle = ToolStripItemDisplayStyle.None;
            Text = "����Ԥ��";
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
        /// ������д�˺�������Ϊ��д��AddParameter()������
        /// </summary>
        /// <param name="pManager"></param>
        protected sealed override void RegisterInputParams(GH_InputParamManager pManager)
            => AddParameter(new ParamManager(this));

        /// <summary>
        /// ������д�˺�������Ϊ��д��AddParameter()������
        /// </summary>
        /// <param name="pManager"></param>
        protected sealed override void RegisterOutputParams(GH_OutputParamManager pManager)
        { }
        /// <summary>
        /// �����������ˣ�ע�� ��Ʋ������ظ�
        /// </summary>
        /// <param name="pm">����������</param>
        protected virtual void AddParameter(ParamManager pm)
        {
            pm.AddIP<Param_Arc>("", "", "");
            pm.AddIP(ParT.Angle, "", "", "");
        }

#endif
        /// <summary>
        /// ����
        /// </summary>
        /// <param name="b">True�򱨴�</param>
        /// <param name="s">������Ϣ</param>
        /// <param name="m">����ȼ�</param>
        /// <returns>b</returns>
        public bool RuntimeMes(bool b, string s, GH_RuntimeMessageLevel m)
        {
            if (b)
                AddRuntimeMessage(m, s);
            return b;
        }

        #region �����Ԥ����
        /// <summary>
        /// �ж���ֵ�Ƿ񳬹�����
        /// </summary>
        /// <param name="num">Ҫ�ж�����ֵ</param>
        /// <param name="max">����</param>
        /// <param name="contain">trueΪ���Ե�������</param>
        /// <returns>����ı�����Ϣ</returns>
        public static string Maxpd(double num, double max, bool contain)
            => contain ? (num > max ? $"���ܴ�������{max}" : "") : (num >= max ? $"���ܴ��ڵ�������{max}" : "");
        /// <summary>
        /// �ж���ֵ�Ƿ񳬹�����
        /// </summary>
        /// <param name="num">Ҫ�ж�����ֵ</param>
        /// <param name="min">����</param>
        /// <param name="contain">trueΪ���Ե�������</param>
        /// <returns>����ı�����Ϣ</returns>
        public static string Minpd(double num, double min, bool contain)
            => contain ? (num < min ? $"���ܴ�������{min}" : "") : (num <= min ? $"���ܴ��ڵ�������{min}" : "");
        /// <summary>
        /// �ж���ֵ�Ƿ񳬹�����,,��ʧ�������false���Զ�����
        /// </summary>
        /// <param name="paInt">���������</param>
        /// <param name="max">����</param>
        /// <param name="contain">trueΪ���Ե�������</param>
        /// <returns>trueΪ��ͨ��</returns>
        public void PMaxpd(int paInt, double max, bool contain)
        {
            if (Params.Input.Count <= paInt)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"PMaxpd�����������{paInt}����Χ������");
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
                    throw new ArgumentOutOfRangeException($"{PaInt(paInt)}����˵�ֵ{str1}!", new Exception());
                }
                return;
            }
            throw new ArgumentException($"{PaInt(paInt)}����Ϊ����/��ֵ������!����paInt������");
            //string str1 = Pdmax(num, max, contain);
            //if (str1.Length == 0) return true;
            //AddRuntimeMessage(GH_RuntimeMessageLevel.Error,  $"{PaInt(paInt)}����˵�ֵ{str1}!" );
            //return false;
        }
        /// <summary>
        /// �ж���ֵ�Ƿ񳬹�����,��ʧ�������false���Զ�����
        /// </summary>
        /// <param name="paInt">���������</param>
        /// <param name="min">����</param>
        /// <param name="contain">trueΪ���Ե�������</param>
        /// <returns>trueΪ��ͨ��</returns>
        public void PMinpd(int paInt, double min, bool contain)
        {
            if (Params.Input.Count <= paInt)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"PMinpd�����������{paInt}����Χ������");
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
                    throw new ArgumentOutOfRangeException($"{PaInt(paInt)}����˵�ֵ{str2}!", new Exception());
                }
                return;
            }
            throw new ArgumentException($"{PaInt(paInt)}����Ϊ����/��ֵ������!����paInt������");
        }
        /// <summary>
        /// �ж���ֵ�Ƿ񳬹�������,,��ʧ�������false���Զ�����
        /// </summary>
        /// <param name="paInt">���������</param>
        /// <param name="min">����</param>
        /// <param name="max">����</param>
        /// <param name="containx">trueΪ���Ե�������</param>
        /// <param name="containd">trueΪ���Ե�������</param>
        /// <returns>trueΪ��ͨ��</returns>
        public void PMutpd(int paInt, double min, double max, bool containx, bool containd)
        {
            if (Params.Input.Count <= paInt)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"PMutpd�����������{paInt}����Χ������");
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
                    throw new ArgumentOutOfRangeException($"{PaInt(paInt)}����˵�ֵ{str1},{str2}!", new Exception());
                }
                return;
            }
            throw new ArgumentException($"{PaInt(paInt)}����Ϊ����/��ֵ������!����paInt������");
        }
        /// <summary>
        /// ����������ﵥ���ж�,�ж�ʧ�ܻ��Զ�����
        /// </summary>
        /// <param name="paInt">Ҫ�ж������������</param>
        public void PDxiangpd(int paInt)
        {
            if (Params.Input.Count <= paInt)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"PDxiangpd�����������{paInt}����Χ������");
                throw new IndexOutOfRangeException("PDxiangpd");
            }
            IGH_Param inp = Params.Input[paInt];
            if (inp.VolatileDataCount < 2) return;
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"{PaInt(paInt)}��ֻ��������һ�����ݣ��ѽ�ʹ�����һ��֦��һ��");
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
        /// ����������ﵥ���ж�,�ж�ʧ�ܻ��Զ�����,��ת��
        /// </summary>
        /// <param name="paInt">Ҫ�ж������������</param>
        public void PDliepd(int paInt)
        {
            if (Params.Input.Count <= paInt)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"PDliepd�����������{paInt}����Χ������");
                throw new IndexOutOfRangeException("PDliepd");
            }
            IGH_Param inp = Params.Input[paInt];
            if (inp.VolatileData.PathCount < 2) return;
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"{PaInt(paInt)}��ֻ��������һ�����ݣ��ѽ�ʹ�����һ��֦");
            GH_Structure<IGH_Goo> a = (GH_Structure<IGH_Goo>)inp.VolatileData;
            inp.ClearData();
            inp.AddVolatileDataList(a.Paths[0], a.Branches[0]);
        }
        /// <summary>
        /// ��������������������������ж����ж�ʧ�ܻ��Զ�����
        /// </summary>
        /// <param name="paInt1">Ҫ�ж������������1</param>
        /// <param name="paInt2">Ҫ�ж������������2</param>
        public void PMutixianggxpd(int paInt1, int paInt2)
        {
            if (Params.Input.Count <= paInt1 || Params.Input.Count <= paInt2)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"PMutixianggxpd�����������{paInt1}��/��{paInt2}����Χ������");
                throw new IndexOutOfRangeException("PMutixianggxpd");
            }
            IGH_Param inp1 = Params.Input[paInt1];
            IGH_Param inp2 = Params.Input[paInt2];
            if (inp1.VolatileDataCount != inp2.VolatileDataCount)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    $"{PaInt(paInt1)}����{PaInt(paInt2)}�˵������������");
        }
        /// <summary>
        /// ������������������������У��ж�ʧ�ܻ��Զ�����
        /// </summary>
        /// <param name="paInt1">Ҫ�ж������������1</param>
        /// <param name="paInt2">Ҫ�ж������������2</param>
        public void PMutiliegxpd(int paInt1, int paInt2)
        {
            if (Params.Input.Count <= paInt1 || Params.Input.Count <= paInt2)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"PMutiliegxpd�����������{paInt1}��/��{paInt2}����Χ������");
                throw new IndexOutOfRangeException("PMutiliegxpd");
            }
            IGH_Param inp1 = Params.Input[paInt1];
            IGH_Param inp2 = Params.Input[paInt2];
            if (inp1.VolatileData.PathCount != inp2.VolatileData.PathCount)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    $"{PaInt(paInt1)}����{PaInt(paInt2)}�˵����������");
        }
        /// <summary>
        /// ��ȡ������ ����(���)
        /// </summary>
        /// <param name="index">�������������</param>
        /// <returns>��������(���)</returns>
        public string PaInt(int index)
        {
            IGH_Param par = Params.Input[index];
            return $@"��{par.Name}({par.NickName})��";
        }
        #endregion

        internal void Anli(bool enabled, ToolStripDropDown menu)
        {
            if (!enabled) return;
            ToolStripMenuItem menu���� = Menu_AppendItem(menu, "����", Menu_����Clicked, true);
            menu����.ToolTipText = "��������Դ򿪴˵�صİ���";
        }
        // ReSharper disable once PossibleNullReferenceException

        protected virtual void Menu_����Clicked(object sender, EventArgs e) =>
            Instances.DocumentEditor.ScriptAccess_OpenDocument(
                $@"{Folders.AppDataFolder}Example\{Name}-{NickName}.gh");
        #region MenuItem
        /// <summary>
        /// �������������˵���
        /// </summary>
        /// <param name="menu">���ڲ˵�</param>
        /// <param name="text">�ı�</param>
        /// <param name="def">Ԥ��ֵ��Ҫ���޸ĵ�ֵ</param>
        /// <param name="tooltip">��ʾ</param>
        /// <param name="recom">�ؼ��㣿���������Ԥ��</param>
        /// <returns>���úõĲ˵���</returns>
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
                        RecordUndoEvent($"����{text}");
                        def.Value = d;
                        if (recom)

                            ExpireSolution(false);
                        else
                            ExpirePreview(false);
                    }
                    else
                        MessageBox.Show("����Ĳ�������������");
                }, false);
            return dou;
        }
        /// <summary>
        /// ����������ֵ�˵���
        /// </summary>
        /// <param name="menu">���ڲ˵�</param>
        /// <param name="text">�ı�</param>
        /// <param name="def">Ԥ��ֵ��Ҫ���޸ĵ�ֵ</param>
        /// <param name="tooltip">��ʾ</param>
        /// <param name="recom">�ؼ��㣿���������Ԥ��</param>
        /// <returns>���úõĲ˵���</returns>
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
                        RecordUndoEvent($"����{text}");
                        def.Value = d;
                        if (recom)

                            ExpireSolution(false);
                        else
                            ExpirePreview(false);
                    }
                    else
                        MessageBox.Show("����Ĳ�����ֵ������");
                }, false);
            return dou;
        }
        /// <summary>
        /// �������������˵���
        /// </summary>
        /// <param name="menu">���ڲ˵�</param>
        /// <param name="text">�ı�</param>
        /// <param name="def">Ԥ��ֵ��Ҫ���޸ĵ�ֵ</param>
        /// <param name="tooltip">��ʾ</param>
        /// <param name="recom">�ؼ��㣿���������Ԥ��</param>
        /// <returns>���úõĲ˵���</returns>
        public ToolStripMenuItem Menu_Color(ToolStripDropDown menu, string text, GH_Colour def, string tooltip = null, bool recom = false)
        {
            Color c = def.Value;
            ToolStripMenuItem col = Menu_AppendItem(menu, text, null, c.ToSprite(20, 20));
            if (!string.IsNullOrWhiteSpace(tooltip))
                col.ToolTipText = tooltip;
            Menu_AppendColourPicker(col.DropDown, c,
                (sender, e) =>
                {
                    RecordUndoEvent($"����{text}");
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
        /// �������ý���˵���
        /// </summary>
        /// <param name="menu">���ڲ˵�</param>
        /// <param name="text">�ı�</param>
        /// <param name="def">Ԥ��ֵ��Ҫ���޸ĵ�ֵ</param>
        /// <param name="tooltip">��ʾ</param>
        /// <param name="recom">�ؼ��㣿���������Ԥ��</param>
        /// <returns>���úõĲ˵���</returns>
        public ToolStripMenuItem Menu_Gradient(ToolStripDropDown menu, string text, GH_Gradient def, GH_Boolean rev, string tooltip = null, bool recom = false)
        {
            ToolStripMenuItem gradient = Menu_AppendItem(menu, text);
            gradient.Image = LTResource.Gradient_20x20;
            if (!string.IsNullOrWhiteSpace(tooltip))
                gradient.ToolTipText = tooltip;
            if (gradient.DropDown is ToolStripDropDownMenu downMenu)
                downMenu.ShowImageMargin = false;
            List<GH_Gradient> gradientPresets = GH_GradientControl.GradientPresets.ToArray().ToList();
            //��Ĭ��ֵ����Ϊ��һ��
            gradientPresets.Insert(0, def);
            void GradientPresetClicked(object s, MouseEventArgs e)
            {
                MGradientMenuItem GradientMenuItem = (MGradientMenuItem)s;
                RecordUndoEvent($"����{text}");
                //ɾ���ɵ�
                for (int i = def.GripCount - 1; i >= 0; i--)
                    def.RemoveGrip(i);
                //����µ�
                for (int i = 0; i < GradientMenuItem.Gradient.GripCount; i++)
                    def.AddGrip(GradientMenuItem.Gradient[i]);

                if (recom)
                    ExpireSolution(true);
                else
                    ExpirePreview(true);
            }//��������¼� ���ط���
            //�ѽ��䶼��ӵ��˵���
            foreach (GH_Gradient t in gradientPresets)
                gradient.DropDownItems.Add(new MGradientMenuItem(t, GradientPresetClicked));
            //������ʾ�ı�
            gradient.DropDownItems[0].ToolTipText = "��ǰ����";

            #region ��ת����
            //ȥ�� �������ı��е�(&)
            var t0 = text.IndexOf("(&", StringComparison.Ordinal);
            var t1 = t0 < 0 ? text : text.Substring(0, t0);
            ToolStripMenuItem m = Menu_AppendItem(menu, $"��ת{t1}(&R)",
                delegate
                {
                    rev.Value = !rev.Value;
                    if (recom)
                        ExpireSolution(true);
                    else
                        ExpirePreview(true);
                });
            m.ToolTipText = "����ת�����ӳ��Ч�������޸Ľ�������";
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
                    throw new ArgumentOutOfRangeException(nameof(exposure), "����Ĳ�������Χ������0-7֮�䣨�Ժ���");
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
            /// ��������
            /// </summary>
            /// <param name="type">����������</param>
            /// <param name="name">����</param>
            /// <param name="nickname">���</param>
            /// <param name="description">����</param>
            /// <param name="trait">������״̬</param>
            /// <param name="def">Ĭ�϶��ֵ�Ļ�����ʹ��[]����List<>�������ʶ����</param>
            /// <returns>������úõĲ�����</returns>

            #region AddP
            /// <summary>
            /// ��������
            /// </summary>
            /// <param name="type">����������</param>
            /// <param name="name">����</param>
            /// <param name="nickname">���</param>
            /// <param name="description">����</param>
            /// <param name="trait">������״̬</param>
            /// <param name="def">Ĭ�϶��ֵ�Ļ�����ʹ��[]����List<>�������ʶ����</param>
            /// <returns>������úõĲ�����</returns>
            public int AddIP(ParT type, string name, string nickname, string description
                , ParamTrait trait = ParamTrait.Item, params object[] def)
                => AddP(false, type, name, nickname, description, trait, def);
            /// <summary>
            /// ��������
            /// </summary>
            /// <param name="ip">Ҫ���ò���ӵĲ�����</param>
            /// <param name="name">����</param>
            /// <param name="nickname">���</param>
            /// <param name="description">����</param>
            /// <param name="trait">������״̬</param>
            /// <returns>������úõĲ�����</returns>
            public int AddIP(IGH_Param ip, string name, string nickname, string description,
                ParamTrait trait = ParamTrait.Item)
                => AddP(false, ip, name, nickname, description, trait);
            /// <summary>
            /// ��������
            /// </summary>
            /// <typeparam name="T">�����˵�����</typeparam>
            /// <param name="name">����</param>
            /// <param name="nickname">���</param>
            /// <param name="description">����</param>
            /// <param name="trait">������״̬</param>
            /// <returns>������úõĲ�����</returns>
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
                        throw new ArgumentOutOfRangeException(nameof(t), $"{nameof(ParamTrait.IsAngle)}ֻ�����ڡ���ֵ��������");
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
                    $"{nameof(ParamTrait)}���뺬���������ݽṹ��־���Ա����趨{nameof(GH_ParamAccess)}");
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
                            throw new ArgumentOutOfRangeException(nameof(type), "ParT.SubD����rhino7����");
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
                    throw new ArgumentException($"{type}���Ͳ�֧�����Ĭ��ֵ����������{nameof(def)}��������ʹ��Ϊnull", nameof(type));
                }
            }

            /// <summary>
            /// ��������ΪQ�Ĳ����ˣ�����Ĭ��ֵ���ø���
            /// </summary>
            /// <typeparam name="Q">����Ĳ����˾�������</typeparam>
            /// <param name="def">Ĭ�����ݣ���������,�������Ͳ��������������������ӵ�����Ĭ�����͵�Ĭ��ʵ��</param>
            /// <returns></returns>
            /// <exception cref="ArgumentException">Q��GH_PersistentParam��defȴ��ֵ</exception>
            private static Q SetGHP<Q>(params object[] def) where Q : IGH_Param, new()
            {
                Q ip = new Q();
                //��Ĭ��ֱ�����
                if (def.Length == 0)
                    return ip;
                MethodInfo Method = ip.GetType().GetMethod("SetPersistentData", new[] { typeof(object[]) });
                if (Method == null)
                    throw new ArgumentException(
                        $"{nameof(Q)}�����С�SetPersistentData(params object[])��������\r\n���ܲ������ԡ�GH_PersistentParam<T>��,\r\n�벻Ҫ���˲���������Ĭ��ֵ");
                Method.Invoke(ip, new object[] { def });
                return ip;
            }
            public int IParamCount => this[false].Count;
            public int OParamCount => this[true].Count;
            /// <summary>
            /// ��ȡ�����б�
            /// </summary>
            /// <param name="io">trueΪ���룬falseΪ����</param>
            /// <returns>��Ӧ�������������б�</returns>
            public List<IGH_Param> this[bool io]
                => io ? m_owner.Params.Output : m_owner.Params.Input;
            /// <summary>
            /// ��������ȡ������
            /// </summary>
            /// <param name="io">trueΪ���룬falseΪ����</param>
            /// <param name="index"></param>
            /// <returns>��Ӧ�Ĳ�����</returns>
            public IGH_Param this[bool io, int index]
                => this[io][index];
            internal MComponent m_owner;
        }
        /// <summary>
        /// ���������ԣ�������֯���͡�Ԥ���ɼ��ԣ���ѡ�ԡ��������б��Ƿ�Ϊ�Ƕȡ�����ӳ�����
        /// </summary>
        [Flags]
        public enum ParamTrait
        {
            Item = 1 << 0,//ͬ GH_ParamAccess������ֻ����һ�����ִֻ����ֵС����
            List = 1 << 1,
            Tree = 1 << 2,
            //todo ��Ҫʵ��
            OnlyOne = 1 << 3,//���������� �����ܵ�һ����֧/���ĵ�һ�����
            Hidden = 1 << 4,//���ش˲����˵�Ԥ��
            Optional = 1 << 5,//�˲�����Ϊ��ѡ����ʱ�����벻�ᱻ����
            //NamedList = 1 << 6,//�˲��������������������������������ʱ��Ч
            IsAngle = 1 << 7,//�˲�����Ϊ�ǶȲ����ˣ�������Ϊ����ֵ��������ʱ��Ч
            Simplify = 1 << 8,//�򻯲���·��
            Reverse = 1 << 9,//��ת��������
            Flatten = 1 << 10,//̯ƽ����������ͬʱ���ڣ�����ִֻ��̯ƽ
            Graft = 1 << 11//��֦����������ͬʱ����
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
            => AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"�˵���ѹ��ڣ��������ֹ��滻Ϊ�µ�ء�{NewName}����");

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
        /// ���ݳߴ����ɴ�ɫͼƬ
        /// </summary>
        /// <param name="c">��ɫ</param>
        /// <param name="w">���</param>
        /// <param name="h">�߶�</param>
        /// <returns>���ɵĴ�ɫͼƬ</returns>
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
        /// ����ֵ������ת���ɽ���ɫ��
        /// </summary>
        /// <param name="gra">����</param>
        /// <param name="it">��ֵ�������䷶Χ</param>
        /// <param name="v">��ֵ</param>
        /// <param name="reverse">�Ƿ�ת����</param>
        /// <returns>��ֵ��Ӧ��ɫ��</returns>
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