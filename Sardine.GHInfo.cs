using System;
using System.Drawing;
using Grasshopper;
using Grasshopper.Kernel;

namespace Sardine.GH
{
    public class Sardine_GHInfo : GH_AssemblyInfo
    {
        public override string Name => "Sardine.GH";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => null;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "";

        public override Guid Id => new Guid("9152e6f2-ab43-4e8d-992d-959c9a63b270");

        //Return a string identifying you or your company.
        public override string AuthorName => "";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "";

        //Return a string representing the version.  This returns the same version as the assembly.
        public override string AssemblyVersion => GetType().Assembly.GetName().Version.ToString();
    }
}