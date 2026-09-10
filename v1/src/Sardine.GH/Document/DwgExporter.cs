using System;
using System.Collections.Generic;
using System.IO;
using Rhino;

namespace Sardine.GH.Document
{
    /// <summary>
    /// Exports an explicit set of document objects to DWG through Rhino's scripted
    /// <c>-Export</c> command (proof-of-concept 10_Export behaviour). Only the
    /// supplied ObjectIds are selected for export; nothing else in the document is
    /// scanned or inferred.
    /// </summary>
    public static class DwgExporter
    {
        /// <summary>
        /// Selects <paramref name="objectIds"/> and exports them to <paramref name="dwgPath"/>.
        /// Returns true when the file exists afterwards. <paramref name="message"/> explains a failure.
        /// </summary>
        public static bool Export(RhinoDoc doc, IList<Guid> objectIds, string dwgPath, out int selected, out string message)
        {
            selected = 0;
            message = null;

            if (doc == null) { message = "No active Rhino document."; return false; }
            if (objectIds == null || objectIds.Count == 0) { message = "No ObjectIds supplied. Bake an option first."; return false; }
            if (string.IsNullOrWhiteSpace(dwgPath)) { message = "No DWG path supplied."; return false; }

            string directory = Path.GetDirectoryName(dwgPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            doc.Objects.UnselectAll();
            try
            {
                foreach (Guid id in objectIds)
                {
                    if (id == Guid.Empty) continue;
                    var obj = doc.Objects.FindId(id);
                    if (obj == null) continue;
                    if (obj.Select(true) > 0) selected++;
                }

                if (selected == 0)
                {
                    message = "None of the supplied ObjectIds exist in the document. Bake the option again before exporting.";
                    return false;
                }

                string script = string.Format("_-Export \"{0}\" _Enter", dwgPath);
                bool ran = RhinoApp.RunScript(script, false);

                if (!File.Exists(dwgPath))
                {
                    message = ran
                        ? "Rhino did not write the DWG file. Check the path is writable and the DWG export options."
                        : "The Rhino export command did not run. Check that no other command is active.";
                    return false;
                }

                return true;
            }
            finally
            {
                doc.Objects.UnselectAll();
                doc.Views.Redraw();
            }
        }

        /// <summary>
        /// Returns <paramref name="preferred"/> when it does not exist, otherwise the
        /// first <c>name_2.dwg</c>, <c>name_3.dwg</c>… that does not exist. Existing
        /// files are never overwritten.
        /// </summary>
        public static string UniquePath(string preferred)
        {
            if (!File.Exists(preferred)) return preferred;

            string directory = Path.GetDirectoryName(preferred) ?? string.Empty;
            string stem = Path.GetFileNameWithoutExtension(preferred);
            string ext = Path.GetExtension(preferred);

            for (int i = 2; i < 10000; i++)
            {
                string candidate = Path.Combine(directory, stem + "_" + i + ext);
                if (!File.Exists(candidate)) return candidate;
            }
            return Path.Combine(directory, stem + "_" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ext);
        }
    }
}
