using Grasshopper;
using Grasshopper.Kernel;

namespace Lt;

// ReSharper disable once UnusedMember.Global
public class AssemblyPriority : GH_AssemblyPriority
{
    public override GH_LoadingInstruction PriorityLoad()
    {
        Instances.ComponentServer.AddCategoryIcon("Lt", Resources.ltlogo16);
        return GH_LoadingInstruction.Proceed;
    }
}