using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace Lt.Base.Extensions
{
    public static class GhExtensions
    {
        /// <summary>
        /// 绘制预览网格，（仅未被选中时显示伪色），用于重写DrawViewportMeshes内
        /// </summary>
        /// <param name="a">输出端索引</param>
        /// <param name="component">电池本体，默认输入this</param>
        /// <param name="args">预览变量，默认输入 args</param>
        public static void Draw1Meshes(this IGH_PreviewArgs args, int a, IGH_Component component)
        {
            var lmesh = component.Params.Output[a].VolatileData.AllData(true).Select(t => ((GH_Mesh)t).Value).ToList();
            if (lmesh.Count == 0) return; ///避免网格不存在
            var args2 = new GH_PreviewMeshArgs(args.Viewport, args.Display,
                component.Attributes.GetTopLevel.Selected ? args.ShadeMaterial_Selected : args.ShadeMaterial, args.MeshingParameters);
            foreach (Mesh mesh in lmesh)
                if (mesh.VertexColors.Count > 0)
                    args2.Pipeline.DrawMeshFalseColors(mesh);
                else
                    args2.Pipeline.DrawMeshShaded(mesh, args2.Material);
        }

        /// <summary>
        /// 按树状结构烘焙物体
        /// </summary>
        /// <typeparam name="T">物体类型</typeparam>
        /// <param name="doc">烘焙至的文档</param>
        /// <param name="l">要烘焙的树状结构物体</param>
        /// <param name="co">物体的色彩</param>
        /// <param name="groupname">群组名</param>
        /// <param name="att">物件属性</param>
        /// <param name="obj_ids">所在id列表</param>
        public static void BakeColorGroup<T>(this RhinoDoc doc, List<List<T>> l, Color co, string groupname, ObjectAttributes att, List<Guid> obj_ids)
            where T : IGH_Goo, IGH_BakeAwareData
        {
            foreach (var l0 in l)
            {
                if (l0.Count == 0 || co.IsEmpty) return;
                ObjectAttributes oa = att.Duplicate();
                int groupIndex = doc.Groups.Find(groupname, true);
                if (groupIndex == -1)
                    groupIndex = doc.Groups.Add(groupname);
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
    }
}
