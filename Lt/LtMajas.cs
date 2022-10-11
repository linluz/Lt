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
            bool set = component.Attributes.GetTopLevel.Selected;
            GH_PreviewMeshArgs args2 = new GH_PreviewMeshArgs(args.Viewport, args.Display,
                (set ? args.ShadeMaterial_Selected : args.ShadeMaterial), args.MeshingParameters);
            if (lmesh.Count == 0) return; ///�������񲻴���
            Mesh mesh = lmesh[0];
            if (mesh.VertexColors.Count > 0 && !set) //��������ɫ��δ��ѡȡʱ
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
        /// ������
        /// </summary>
        /// <param name="doco">Ҫ��������ĵ��</param>
        /// <param name="menu">Ҫ����ӵĲ˵�</param>
        /// <param name="text">�˵�����</param>
        /// <param name="gra">����д洢������ֶ�</param>
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
            return reader.GetGradient("����", Gradient);
        }
        public override bool Write(GH_IWriter writer)
        {
            base.Write(writer);
            return writer.SetGradient("����", Gradient);
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
            => this.Menu_Gradient(menu, "����",
                "��С�Ҵ����޸ĺ�Ԥ���ޱ仯�����ؼ��㱾���,����֪�������޸�\r\nҪ����Ԥ�裬�����������䡿����������䲢ʹ�����Ҽ��˵���\r\n����ӵ�ǰ����Add Current Gradient��", Gradient);
        internal void Menu_GradientPresetClicked(object sender, MouseEventArgs e)
        {
            GH_GradientMenuItem gh_GradientMenuItem = (GH_GradientMenuItem)sender;
            RecordUndoEvent("���ý���");
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
        /// ��ӽ���˵���
        /// </summary>
        /// <param name="grac">��������ĵ��</param>
        /// <param name="gra">����ж�Ӧʹ�õĽ���</param>
        public LT_GradientMenuItem(GradientComponent grac, GH_Gradient gra)
        {
            Gradient = gra;
            DisplayStyle = ToolStripItemDisplayStyle.None;
            Text = "����Ԥ��";
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
                    $"{nameof(ParamTrait)}���뺬���������ݽṹ��־��ͬ{nameof(GH_ParamAccess)}");
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
                            throw new ArgumentOutOfRangeException(nameof(type), "ParT.SubD����rhino7����");
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
                    throw new ArgumentException($"{type}���Ͳ�֧�����Ĭ��ֵ����������{nameof(def)}��������ʹ��Ϊnull", nameof(type));
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
            /// ��������ΪQ�Ĳ����ˣ�����Ĭ��ֵ���ø��� �� debug
            /// </summary>
            /// <typeparam name="Q">����Ĳ����˾�������</typeparam>
            /// <typeparam name="T">������Ĭ�����ݵ�����</typeparam>
            /// <param name="t">����������ö�٣��ֹ�����t����ʹ�÷����Q���Ƶ�Ҳ�У�����̫��</param>
            /// <param name="def">Ĭ�����ݣ���������null</param>
            /// <returns>����ΪQ�Ĳ�����</returns>
            /// <exception cref="ArgDefException">QΪ�ǳ־�ʱ��def����Ϊnull</exception>
            /// <exception cref="ArgumentException">def�����ݵ����Ͳ���T�Ǳ���</exception>
            private static Q SetGHP<Q, T>(ParamCategory t, params object[] def) where Q : IGH_Param, new()
            {
                Q ip = new Q();
                //��Ĭ��ֱ�����
                if (def == null)
                    return ip;
                //�Ǳ��� ȴ��Ĭ�Ͼͱ���
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
                throw new ArgumentException($"��AddIParameter���в���{nameof(def)}��������ͱ���{typeof(T)}������");
            }
            /// <summary>
            /// ��������ΪQ�Ĳ����ˣ�����Ĭ��ֵ���ø��� �� 
            /// </summary>
            /// <typeparam name="Q">����Ĳ����˾�������</typeparam>
            /// <typeparam name="T">������Ĭ�����ݵ�����</typeparam>
            /// <param name="def">Ĭ�����ݣ���������null</param>
            /// <returns>����ΪQ�Ĳ�����</returns>
            /// <exception cref="ArgDefException">QΪ�ǳ־�ʱ��def����Ϊnull</exception>
            /// <exception cref="ArgumentException">def�����ݵ����Ͳ���T�Ǳ���</exception>
            private static Q SetGHP<Q, T>(IEnumerable<object> def) where Q : IGH_Param, new()
            {
                Q ip = new Q();
                //��Ĭ��ֱ�����
                if (def == null)
                    return ip;

                Type bt = ip.Type.BaseType;
                //�Ǳ��� ȴ��Ĭ�Ͼͱ���
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
                throw new ArgumentException($"��AddIParameter���в���{nameof(def)}��������ͱ���{typeof(T)}������");
            }
            public int IParamCount => this[false].Count;
            public int OParamCount => this[true].Count;
            /// <summary>
            /// 
            /// </summary>
            /// <param name="io">trueΪ���룬falseΪ����</param>
            /// <returns></returns>
            public List<IGH_Param> this[bool io]
                => io ? m_owner.Params.Output : m_owner.Params.Input;
            /// <summary>
            /// 
            /// </summary>
            /// <param name="io">trueΪ���룬falseΪ����</param>
            /// <param name="index"></param>
            /// <returns></returns>
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
            Item = 0,//ͬ GH_ParamAccess������ֻ����һ�����ִֻ����ֵС����
            List = 1 << 0,
            Tree = 1 << 1,
            //todo ��Ҫʵ��
            OnlyOne = 1 << 2,//���������� �����ܵ�һ����֧/���ĵ�һ�����
            Hidden = 1 << 3,//���ش˲����˵�Ԥ��
            Optional = 1 << 4,//�˲�����Ϊ��ѡ����ʱ�����벻�ᱻ����
            //NamedList = 1 << 5,//�˲��������������������������������ʱ��Ч
            IsAngle = 1 << 6,//�˲�����Ϊ�ǶȲ����ˣ�������Ϊ����ֵ��������ʱ��Ч
            Simplify = 1 << 7,//�򻯲���·��
            Reverse = 1 << 8,//��ת��������
            Flatten = 1 << 9,//̯ƽ����������ͬʱ���ڣ�����ִֻ��̯ƽ
            Graft = 1 << 10//��֦����������ͬʱ����
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