using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace Sardine.GH
{
    /// <summary>
    /// Grasshopper assembly metadata for the Sardine plugin (.gha).
    /// The assembly Id is stable across v1.x releases.
    /// </summary>
    public class SardineInfo : GH_AssemblyInfo
    {
        public override string Name { get { return "Sardine"; } }

        public override Bitmap Icon { get { return null; } }

        public override string Description
        {
            get { return "Sardine car park layout generator for Rhino 8 / Grasshopper (WSP UK)."; }
        }

        public override Guid Id { get { return new Guid("9152e6f2-ab43-4e8d-992d-959c9a63b270"); } }

        public override string AuthorName { get { return "WSP UK"; } }

        public override string AuthorContact { get { return "Benj Sayers, WSP UK"; } }

        public override string AssemblyVersion
        {
            get { return GetType().Assembly.GetName().Version.ToString(); }
        }
    }

    /// <summary>Component tab and sub-category names shared by every Sardine component.</summary>
    public static class SardineCategory
    {
        public const string Tab = "Sardine";
        public const string Generate = "Generate";
        public const string Output = "Output";
    }
}
