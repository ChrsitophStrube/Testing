using System;
using System.Collections.Generic;
using System.Linq;

namespace HmiTesting.Core.Helpers
{
    /// <summary>
    /// Static helper for working with logical, URI‑like paths inside an arbitrary object tree.
    /// * Segments are joined with "/" when converted to string.
    /// * Supports appending new segments and the ".." operator to step back one level.
    /// * Immutable: every modification returns a new instance.
    /// </summary>
    public static class PathHandler
    {
        /// <summary>
        /// Immutable value object that represents a path.
        /// </summary>
        public readonly struct OpcPath
        {
            private readonly string[] _segments;

            /// <summary>Gets the individual segments of the path.</summary>
            public IReadOnlyList<string> Segments => _segments;

            private OpcPath(IEnumerable<string> segments)
            {
                _segments = segments.ToArray();
            }

            /// <summary>
            /// Creates a path from the given <paramref name="segments"/>.
            /// </summary>
            public static OpcPath From(params string[] segments) => new OpcPath(Normalize(segments));

            /// <summary>
            /// Returns a new <see cref="OpcPath"/> with <paramref name="segments"/> appended.
            /// The special segment ".." removes the last segment (similar to file‑system semantics).
            /// Empty strings and "." are ignored.
            /// </summary>
            public OpcPath Append(params string[] segments) => new OpcPath(Normalize(_segments.Concat(segments)));

            /// <summary>
            /// Returns the canonical string representation ("/"‑separated).
            /// </summary>
            public override string ToString() => string.Join("/", _segments);

            // Normalizes the segment list: handles "..", "." and trimming/empty removal.
            private static IEnumerable<string> Normalize(IEnumerable<string> segments)
            {
                var list = new List<string>();
                foreach (var raw in segments)
                {
                    var s = raw?.Trim();
                    if (string.IsNullOrEmpty(s) || s == ".")
                        continue;

                    if (s == "..")
                    {
                        if (list.Count == 0)
                            throw new InvalidOperationException("Cannot navigate above root ('..' at top‑level).");
                        list.RemoveAt(list.Count - 1);
                    }
                    else
                    {
                        list.Add(s);
                    }
                }
                return list;
            }
        }

        /// <summary>
        /// Convenience factory that forwards to <see cref="OpcPath.From"/>.
        /// </summary>
        public static OpcPath BuildPath(params string[] segments) => OpcPath.From(segments);

        /// <summary>
        /// Convenience wrapper to append <paramref name="segments"/> to an existing <paramref name="path"/>.
        /// </summary>
        public static OpcPath AppendPath(OpcPath path, params string[] segments) => path.Append(segments);
    }

}

/*
Example usage:

var path = UiTreePath.Build("UIRoot", "MainFrame", "ContentArea");
path = UiTreePath.Append(path, "Navigation");
// => "UIRoot/MainFrame/ContentArea/Navigation"

path = UiTreePath.Append(path, "..", "Sidebar", "List");
// => "UIRoot/MainFrame/ContentArea/Sidebar/List"

Console.WriteLine(path); // prints: UIRoot/MainFrame/ContentArea/Sidebar/List
*/
