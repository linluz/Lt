using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace Lt.GHComponent.Analysis
{
    /// <summary>
    /// 实时山路坡度反馈
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public sealed class LTRW : GradientComponent
    {
        public LTRW() : base("实时山路坡度反馈", "LTRW",
            "实时反馈所绘制的山路坡度是否合理，\r\n不合理的区域用提示圆标注出来。" +
            "\r\n注意:绘制需要在top视图【road】图层内。\r\n双击：自动建立【road】图层并切换为当前图层。\r\n烘焙：已按输入线成组已着色直线段",
            "分析",
            ComponentID.LTRW, 3, LTResource.实时山路绘制反馈)
        {
            Gra.Def = Const.Gradient0.Duplicate();
            Gra.ReCom = false;
            GH = new MGradientMenuItem(this, GH_GradientControl.GradientPresets[1].Duplicate(), "高程渐变(&H)");
            GI = new MBooleanMenuItem(this, false, "自适应角度(&A)", mf: m => m.Def ? "自适应" : "0-上限");
            CV = new MBooleanMenuItem(this, true, "提示圆(&W)", mf: m => m.Def ? "显示提示圆" : "");
            MV = new MBooleanMenuItem(this, false, "地形网格(&T)", mf: m => m.Def ? "显示地形" : "");
            CR = new MDoubleMenuItem(this, 5, "提示圆半径(&R)");
            Ct = new Update<Color[][]>(UpdateC);
            Ma = new Update<Mesh[]>(UpdateM);
            PreviewExpired += (s, e) => Ct.Clear();
            PreviewExpired += (s, e) => Ma.Clear();
            HL = Math.PI;
        }
        protected override void AddParameter(ParamManager pm)
        {
            pm.AddIP(ParT.Mesh, "地形", "M", "山地地形网格,仅限一列", ParamTrait.List | ParamTrait.OneList);
            pm.AddIP(ParT.Integer, "重建精度", "E", "道路中线细分重建精度(米/一个点)", ParamTrait.OnlyOne, def: 2);
            pm.AddIP(ParT.Number, "坡度倒数", "P", "坡度倒数，用来筛选不合理坡度", ParamTrait.OnlyOne, def: 4);

            pm.AddOP(ParT.Curve, "山路", "R", "被分析的山路线段", ParamTrait.Tree);
            pm.AddOP(ParT.Angle, "坡度", "P", "山路线段对应的坡度(弧度)", ParamTrait.Tree | ParamTrait.UseDegrees);
            pm.AddOP(ParT.Integer, "不合理", "I", "坡度超过上限的线段的索引", ParamTrait.Tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (DA.Iteration > 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "本电池仅进行一次计算，多余数据已被忽视");
                return;
            }

            if (!DA.OutDataList(0, out List<Mesh> m)
                || m.Count == 0
                || !m.All(t => t.IsValid)
                || !DA.OutData(1, out int e)
                || e <= 0
                || !DA.OutData(2, out double p)
                || p <= 0)
                return;
            if (Const.RoadLayerIndex < 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "road图层不存在。可双击本电池图标来建立，并设为当前");
                return;
            }
            Settings.LayerIndexFilter = Const.RoadLayerIndex;

            HL = Math.Atan(1 / p);//计算坡度上限

            var ma = m.ToArray();

            var cl = RhinoDoc.ActiveDoc.Objects.GetObjectList(Settings)
                .Select(t => new ObjRef(t))
                .ToArray();

            KeyValuePair<Guid, uint>[] hashl = cl.Select(t => new KeyValuePair<Guid, uint>(t.ObjectId, t.RuntimeSerialNumber)).ToArray();
            Line[][] slaa = new Line[cl.Length][];
            double[][] sdaa = new double[cl.Length][];
            int[][] siaa = new int[cl.Length][];

            for (int i = 0; i < cl.Length; i++)
            {
                #region 查找有无对应，有则输出对应索引
                var ii = -1;
                for (int j = 0; j < HashA.Length; j++)
                {
                    if (HashA[j].Key != hashl[i].Key || HashA[j].Value != hashl[i].Value) continue;
                    ii = j;
                    break;
                }
                #endregion

                if (ii < 0)
                {
                    Curve t5 = cl[i].Curve().DuplicateCurve();//备份一份并转换成曲线
                    var t4 = t5.DivideByCount((int)Math.Round(t5.GetLength() / e), true) //曲线按精度细分出t值
                        .Select(t5.PointAt);//t值转点
                    var t3 = Intersection.ProjectPointsToMeshes(ma, t4, Vector3d.ZAxis, DocumentTolerance());
                    if (t3 == null)//剔除不在网格上的点
                        continue;
                    slaa[i] = new Polyline(t3).GetSegments();
                    if (Const.Paral)//尝试使用多核
                        sdaa[i] = slaa[i].AsParallel()
                            .Select(t => t.Direction//直线转对应向量
                                .ToUnitize()
                                .VectorToSlope())
                            .ToArray();
                    else
                        sdaa[i] = slaa[i].Select(t => t.Direction//直线转对应向量
                                .ToUnitize()
                                .VectorToSlope())
                            .ToArray();

                    List<int> ill = new List<int>(5);
                    for (int j = 0; j < sdaa[i].Length; j++)
                        if (sdaa[i][j] > HL)
                            ill.Add(j);
                    siaa[i] = ill.ToArray();

                }//上次的数据无符合的曲线
                else
                {
                    slaa[i] = Laa[ii];
                    sdaa[i] = Daa[ii];
                    siaa[i] = Iaa[ii];
                }//有对应的曲线
            }
            //储存计算结果，并转化数据
            HashA = hashl;
            Laa = slaa;
            Daa = sdaa;
            Iaa = siaa;

            DA.SetDataTree(0, Laa.Select(t => t.Select(t0 => new GH_Line(t0))).ToGhStructure());
            DA.SetDataTree(1, Daa.Select(t => t.Select(t0 => new GH_Number(t0))).ToGhStructure());
            DA.SetDataTree(2, Iaa.Select(t => t.Select(t0 => new GH_Integer(t0))).ToGhStructure());
            ExpirePreview(true);
        }
        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_Boolean(menu, ref GI, "默认启用，此时渐变色彩范围对应实际的角度范围。\r\n不启用时，范围对应0-角度上限º");
            Menu_Boolean(menu, ref MV, "控制是否显示地形网格");
            Menu_BGradient(MV, ref GH, "此渐变按高程着色输入网格");
            Menu_Boolean(menu, ref CV, "控制是否显示提示圆");
            Menu_BDouble(CV, ref CR);
        }

        public override void RegisterRemoteIDs(GH_GuidTable table)
        {
            base.RegisterRemoteIDs(table);
            foreach (var p in HashA)
                table.Add(p.Key, this);
        }

        public override void BakeGeometry(RhinoDoc doc, ObjectAttributes att, List<Guid> obj_ids)
        {
            for (var i = 0; i < Laa.Length; i++)
            {
                var t = Laa[i];
                ObjectAttributes oa = att.Duplicate();
                oa.ColorSource = ObjectColorSource.ColorFromObject;
                int groupIndex = doc.Groups.Add();
                oa.AddToGroup(groupIndex);
                for (var j = 0; j < t.Length; j++)
                {
                    ObjectAttributes oaj = oa.Duplicate();
                    oaj.ObjectColor = Ct.Value[i][j];
                    var id = Guid.Empty;
                    new GH_Line(Laa[i][j]).BakeGeometry(doc, oaj, ref id);
                    obj_ids.Add(id);
                }
            }
        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            if (Locked || Hidden) return;
            Color c0 = Attributes.Selected ? args.WireColour_Selected : args.WireColour;
            if (Laa.Length != 0)
            {
                args.Viewport.GetFrustumNearPlane(out Plane worldXY);
                for (int i = 0; i < Laa.Length; i++)
                {
                    for (int j = 0; j < Laa[i].Length; j++)
                        args.Display.DrawLine(Laa[i][j], Ct.Value[i][j]); //绘制山路线段
                    if (CV.Def)
                    {
                        //绘制提示圆
                        var i1 = i;
                        var ca = Iaa[i].Select(t =>
                        {
                            Line l0 = Laa[i1][t];//索引转换直线
                            return (l0.From + l0.To) / 2;//获取直线中点
                        })
                            .Select(t => new Circle(worldXY, t, CR.Def)).ToArray();
                        foreach (Circle c in ca)
                            args.Display.DrawCircle(c, c0);
                    }
                }
            }
            if (MV.Def && args.Document.PreviewMode == GH_PreviewMode.Wireframe)
                foreach (Mesh m in Ma.Value)//绘制地形网格
                    args.Display.DrawMeshWires(m, c0);
        }

        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            if (Locked || Hidden) return;
            if (MV.Def && args.Document.PreviewMode == GH_PreviewMode.Shaded)
                foreach (Mesh m in Ma.Value)//绘制地形网格
                    args.Display.DrawMeshFalseColors(m);
        }

        public override void CreateAttributes()
            => m_attributes = new LTRW_Attributes(this);
        /// <summary>
        /// 山路色彩
        /// </summary>
        private readonly Update<Color[][]> Ct;
        private void UpdateC()
        {
            Interval a1 = GI.Def ? Daa.SelectMany(t => t).ToInterval() : new Interval(0, HL);
            Ct.Value = Daa.Select(t => t.Select(t0 => Dou2Col(a1, t0)).ToArray()).ToArray();
        }
        /// <summary>
        /// 地形网格
        /// </summary>
        private readonly Update<Mesh[]> Ma;
        private void UpdateM()
        {
            Ma.Value = GetIntByItem<GH_Mesh>(0)
                .Select(t => t.Value)
                .Select(t =>
                {
                    t.VertexColors.Clear();
                    var i = t.Vertices.Select(t0 => (double)t0.Z).ToInterval();
                    t.VertexColors.AppendColors(
                        t.Vertices.Select(t0 => GH.Def.ColourAt(i.NormalizedParameterAt(t0.Z))).ToArray()
                    );
                    return t;
                }).ToArray();
        }
        /// <summary>
        /// 上次计算获得的曲线id及序列号
        /// </summary>
        private KeyValuePair<Guid, uint>[] HashA = new KeyValuePair<Guid, uint>[0];
        /// <summary>
        /// 上次计算获得的直线段
        /// </summary>
        private Line[][] Laa = new Line[0][];
        /// <summary>
        /// 上次计算获得的弧度
        /// </summary>
        private double[][] Daa = new double[0][];
        /// <summary>
        /// 上次计算获得的索引
        /// </summary>
        private int[][] Iaa = new int[0][];
        /// <summary>
        /// 显示地形
        /// </summary>
        private MBooleanMenuItem MV;
        /// <summary>
        /// 高程渐变
        /// </summary>
        private MGradientMenuItem GH;
        /// <summary>
        /// 角度上限
        /// </summary>
        private double HL;
        /// <summary>
        /// 渐变是否自适应角度范围，否则为0-90度
        /// </summary>
        private MBooleanMenuItem GI;
        /// <summary>
        /// 显示提示圆
        /// </summary>
        private MBooleanMenuItem CV;
        /// <summary>
        /// 提示圆尺寸
        /// </summary>
        private MDoubleMenuItem CR;
        /// <summary>
        /// 物件查找设定
        /// </summary>
        private static readonly ObjectEnumeratorSettings Settings = new ObjectEnumeratorSettings
        {
            ActiveObjects = true,
            LockedObjects = true,
            HiddenObjects = true,
            ObjectTypeFilter = ObjectType.Curve
        };
    }

    /// <summary>
    /// 山路坡度分析_属性
    /// </summary>
    public sealed class LTRW_Attributes : GH_ComponentAttributes
    {
        public LTRW_Attributes(IGH_Component component) : base(component) { }

        public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button == MouseButtons.Left && Bounds.Contains(e.CanvasLocation) && Owner is LTRW)
            {
                if (Const.RoadLayerIndex < 0)//不存在图层则创建
                    RhinoDoc.ActiveDoc.Layers.Add("road", Color.Black);
                RhinoDoc.ActiveDoc.Layers.SetCurrentLayerIndex(Const.RoadLayerIndex, true);//图层设为当前
            }

            return GH_ObjectResponse.Handled;
        }
    }
}
