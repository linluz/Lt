using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace Lt.GHComponent.Analysis;

/// <summary>
/// 山路坡度分析
/// Contour Flood Analysis
/// </summary>
// ReSharper disable once UnusedMember.Global
public sealed class LTRA : GradientComponent, IMCom_DoubleClick
{
    public LTRA() : base("山路坡度分析", "LTRA",
        "分析山路坡度并按角度赋予其对应色彩。\r\n双击：【坡度】【角度范围】切换为角度/弧度。\r\n烘焙：已按列成组已着色直线段",
        "分析",
        ComponentID.LTRA, 3, Resources.山路坡度分析)
    {
        Gra.Def = Const.Gradient0.Duplicate();
        Gra.ReCom = true;
        GI = new MBooleanMenuItem(this, true, "自适应角度(&A)", mf: m => m.Def ? "自适应" : "0-90º");
        C = new Update<Color[][]>(UpdateC);
        PreviewExpired += (_, _) => C.Clear();
        UD = new MBooleanMenuItem(this, true, "使用角度(&)", true, mf: m => m.Def ? "角度" : "弧度");
    }
    protected override void AddParameter(ParamManager pm)
    {
        pm.AddIP(ParT.Curve, "山路", "C", "要分析的山路中线（确保已投影在地形上）", ParamTrait.List);
        pm.AddIP(ParT.Integer, "精度", "E", "山路中线的细分重建密度(单位米)");

        pm.AddOP(ParT.Line, "路线", "L", "重建后用于分析的直线路线", ParamTrait.List);
        pm.AddOP(ParT.Number, "坡度", "A", "对应直线段的坡度(度)", ParamTrait.List);
        pm.AddOP(ParT.Text, "坡度范围", "Rs", "山路直线的坡度范围（即坡高/坡长）");
        pm.AddOP(ParT.Interval, "角度范围", "Ra", "山路直线与水平面所呈角度（度）的范围");
    }
    protected override void SolveInstance(IGH_DataAccess DA)
    {
        if (DA.Iteration == 0)
        {
            Lll = new List<List<Line>>(5);
            Dll = new List<List<double>>(5);
        }
        #region 输入输出变量初始化
        if (!DA.OutDataList(0, out List<Curve> cl) 
            || cl.Count == 0
            || !DA.OutData(1, out int e))
            return;
        #endregion

        Line[][] la = cl.Select(t =>
                new Polyline(t.DivideByCount(
                            // (int)Math.Ceiling(t.GetLength() / e), false//获取细分数
                            (int)Math.Round(t.GetLength() / e), true
                        )//获取细分点t值
                        .Select(t.PointAt)//t值转成点
                ).GetSegments()//将点转成多段线后，再提取全部线段
        ).ToArray();

        List<Line> ll = la.SelectMany(t => t).ToList(); //全部的线段都摊平到一个列表里

        var d = UD.Def ? ConvertConst.R2A : 1;
            
        //获取方向向量，计算角度,并保证是正的
        var a = ll.Select(t => t.Direction)
            .Select(t=>t.ToUnitize()) //向量单元化
            .Select(t => t.VectorToSlope() * d)
            .ToArray();

        DA.SetDataList(0, ll);
        DA.SetDataList(1, a);
        var ai = a.ToInterval();//获取角度区间
        double s = Math.Max(Math.Tan(ai.Max), 0.01);//避免分母太大，计算坡度较大值
        string rs = "0 to " + (s > 1 ? $"1/{Math.Round(s, 2)}" : $"{1 / s}/1");//格式化坡度范围
        DA.SetData(2, rs);
        DA.SetData(3, new Interval(Math.Round(ai.T0, UD.Def ? 2 : 4), Math.Round(ai.T1, UD.Def ? 2 : 4)));//格式化角度范围，弧度则不用格式化
        Lll.Add(ll);
        Dll.Add(a.ToList());
    }

    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
        base.AppendAdditionalComponentMenuItems(menu);
        Menu_Boolean(menu, ref GI, "默认启用，此时渐变色彩范围对应实际的角度范围。\r\n不启用时，范围对应0-90º", click: (_, _) => UpdateC());
        Menu_Boolean(menu, ref UD, "勾选时,【坡度】【角度范围】输出端输出度,否则为弧度", Resources.Degrees_16,
            (_, _) => UD.ReplacePDesc(true, "（弧度）", "（度）", 1, 3));
    }

    public override void DrawViewportWires(IGH_PreviewArgs args)
    {
        if (Locked || args.Document.PreviewMode == GH_PreviewMode.Disabled) return; //跳过锁定或非线框模式
        for (int i = 0; i < Lll.Count; i++)
        for (int j = 0; j < Lll[i].Count; j++)
            args.Display.DrawLine(Lll[i][j], Attributes.GetTopLevel.Selected ? args.WireColour_Selected : C.Value[i][j]);
    }
    public override void BakeGeometry(RhinoDoc doc, ObjectAttributes att, List<Guid> obj_ids)
    {
        if (Locked) return;
        for (int i = 0; i < Lll.Count; i++)
        {
            ObjectAttributes oa = att.Duplicate();
            oa.ColorSource = ObjectColorSource.ColorFromObject;
            int groupIndex = doc.Groups.Add();
            oa.AddToGroup(groupIndex);
            for (int j = 0; j < Lll[i].Count; j++)
            {
                ObjectAttributes oaj = oa.Duplicate();
                oaj.ObjectColor = C.Value[i][j];
                var id = Guid.Empty;
                new GH_Line(Lll[i][j]).BakeGeometry(doc, oaj, ref id);
                obj_ids.Add(id);
            }
        }
    }
    public override void CreateAttributes()
        => m_attributes = new DoubleClick_Attributes(this);

    private void UpdateC()
    {
        if (Locked) return;
        var itl = GetOutByItem<GH_Interval>(3);

        if (Dll.Count != itl.Count)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "输出的坡度的列表数量与区间数量不一致，请联系开发者修复bug！");
            C.Value = Dll.Select(t => t.Select(_ => Color.Black).ToArray()).ToArray();
            return;
        }

        C.Value = new Color[Dll.Count][];
        for (int i = 0; i < Dll.Count; i++)
        {
            Interval interval = GI.Def ? itl[i].Value : Const.A0(UD.Def);
            C.Value[i] = Dll[i].Select(t => Dou2Col(interval, t)).ToArray();
        }
    }

    private readonly Update<Color[][]> C;
    private List<List<Line>> Lll = new(0);
    private List<List<double>> Dll = new(0);
    /// <summary>
    /// 渐变是否自适应角度范围，否则为0-90度
    /// </summary>
    private MBooleanMenuItem GI;
    /// <summary>
    /// 使用角度
    /// </summary>
    private MBooleanMenuItem UD;
    public MBooleanMenuItem DoubleClick => UD;
}