using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace Lt.GHComponent.Basis;

/// <summary>
/// 偏移曲面
/// </summary>
// ReSharper disable once UnusedMember.Global
public class OffsetB() : AComponent("偏移曲面", "LTOffsetB",
    "偏移曲面成实体",
    "基础",
    ComponentID.LTOffsetB, 1, LTResource.曲面偏移实体)
{
    protected override void AddParameter(ParamManager pm)
    {
        pm.AddIP(ParT.Surface, "曲面", "S", "待偏移的曲面", ParamTrait.Item|ParamTrait.Graft | ParamTrait.Simplify);
        pm.AddIP(ParT.Number, "距离", "D", "偏移距离", def: 0);

        pm.AddOP(ParT.Brep, "Brep", "B", "偏移成的brep");
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        if (!DA.OutDataC(0, out GH_Surface s)
            || s is not { IsValid: true }
            || !DA.OutData(1, out double d))
            return;
        var tol = DocumentTolerance();

        DA.SetData(0, d == 0 || (d < 0 ? -d : d) < tol//距离为0或小于公差时直接返回原曲面
            ? s
            : Brep.CreateFromOffsetFace(s.Value.Faces[0].ToBrep().Faces[0], d, tol, false, true)
        );
    }
}