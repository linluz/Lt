using System;
using System.Drawing;
using Grasshopper;
using Grasshopper.Kernel;

namespace Lt
{
    // ReSharper disable once UnusedMember.Global
    public class LtInfo : GH_AssemblyInfo
    {
        public override string Name => "Lt";

        public override Bitmap Icon => LTResource.ltlogo24;

        public override string Description => "一些景观方面的工具";

        public override Guid Id => new Guid("5c9c13be-588f-408e-9dc3-1922fcba732a");

        public override string AuthorName => "兰亭 & 林师兄";

        public override string AuthorContact => "1142060440@qq.com & 329978214@qq.com";
        public override string Version => "0.5";
    }
    // ReSharper disable once UnusedMember.Global
    public class AssemblyPriority : GH_AssemblyPriority
    {
        public override GH_LoadingInstruction PriorityLoad()
        {
            Instances.ComponentServer.AddCategoryIcon("Lt", LTResource.ltlogo16);
            return GH_LoadingInstruction.Proceed;
        }
    }

    public static class ID
    {
        internal const string LTMF_Osb = "84474303-59cb-4248-9015-c5a02098fd99";
        internal const string LTMG_Osb = "6c33fb8b-9da6-4688-8a1b-d0363350d176";
        internal const string LTME_Osb = "1afc0549-5308-47ef-b91c-a2eade694250";
        internal const string LTVL_Osb = "f5b0968b-4de5-45a6-a82b-744f64787e85";
        internal const string LTCE_Osb = "b8ddf076-9287-450e-833f-597f81bac1a4";
        internal const string LTCF_Osb = "8dd68005-d905-4990-a802-cfd30413f836";
        internal const string LTMF_Osb2 = "{F6F4BD0E-A8CE-4F5D-B089-54E1B0876480}";
        internal const string LTMD_Osb2 = "3d3ee5a9-c86e-4007-97c6-eb33aa365e27";
        internal const string LTMG_Osb2 = "{F8F7826B-92CA-4751-9ABF-F25C9E171288}";
        internal const string LTME_Osb2 = "{870471A6-7D3C-4E6C-AA65-03C3EC8BCD24}";
        internal const string LTVL_Osb2 = "{FD1F47F1-8395-49C2-BC34-740249A16535}";
        internal const string LTCE_Osb2 = "{1B1038DD-7DEF-4045-B48E-513F00A45E76}";
        internal const string LTCF_Osb2 = "{01AF0FEC-0FFC-4725-9643-F0E568CC6761}";
        internal const string LTSA = "525ba474-eb6b-43f9-a8c4-b9594f4b33cf";

        internal const string LTMF = "{2B6D31DD-61DD-422B-913F-70C9E02DF3A1}";
        internal const string LTMD = "{8A082285-6ED5-4B51-ABAA-FB4EFE5015FE}";
        internal const string LTMG = "{F138BCCB-E3C8-4924-AEBB-C7DFCAD50CE2}";
        internal const string LTME = "{644BA432-4045-4DEE-9163-CB05A95250B8}";
        internal const string LTVL = "{87FC75BC-FA14-46EF-AB4B-7CD96969CC6D}";
        internal const string LTCE = "{5C39B033-CFF7-4BD9-8C56-F5A5CCAC6C79}";
        internal const string LTCF = "{EBC2C889-0BDA-41BD-A42B-A40E7AE160AB}";
        internal const string LTRA = "{8D99052C-7D8C-4A44-8438-B2E7147A471D}";
        internal const string LTRW = "{830EE783-C3CB-4ED9-A7D2-F50503E617E7}";
        //internal const string 
    }
}
