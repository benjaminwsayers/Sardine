namespace Sardine.Core.Models
{
    /// <summary>
    /// Classification of generated bay geometry.
    ///
    /// The integer value of each member is the first index of the public
    /// Grasshopper DataTree path convention (PRD section 19) and the Rhino layer
    /// classification used by Sardine.Bake (PRD section 21):
    ///
    ///   {0; edge; bay}   Perimeter standard bay
    ///   {1; row;  bay}   Central standard bay
    ///   {2; bank; bay}   Accessible geometry
    ///
    /// These values are stable for the whole v1.x release line.
    /// </summary>
    public enum BayKind
    {
        Perimeter = 0,
        Central = 1,
        Accessible = 2
    }
}
