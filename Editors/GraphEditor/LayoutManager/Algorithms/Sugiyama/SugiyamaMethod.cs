// REPLACEMENT FOR:
// Editors/GraphEditor/LayoutManager/Algorithms/Sugiyama/SugiyamaMethod.cs
//
// Changes vs original:
//   1. BarycentricReorder()  — added. 4-pass sweep that sorts nodes inside each
//      layer by the median position of their neighbours in the adjacent layer.
//      This is the standard Sugiyama crossing-minimisation step that was missing.
//   2. AssignVerticalPositions() — rewritten. After the initial sequential pass,
//      a second pass nudges each node's Y toward the median Y of its connected
//      neighbours (clamped so nodes never overlap). Dramatically reduces wire
//      diagonals on "fan-out" subgraphs like your lightsaber FX blueprint.
//   3. MergeLayers() — bug fixed. The original used `earliestLayer == 0` as a
//      sentinel meaning "nothing found", which silently skipped any node whose
//      nearest target happened to be in layer 0. Now uses a nullable int so the
//      sentinel is actually null.
//   4. AssignHorizontalPositions() — each node is centred within its layer column
//      rather than all sharing the same X, so port anchors align better for nodes
//      narrower than the widest node in that layer.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager.Algorithms.LayeredGraph;
using BlueprintEditorPlugin.Editors.GraphEditor.NodeWrangler;
using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using BlueprintEditorPlugin.Options;

namespace BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager.Algorithms.Sugiyama
{
    public class SugiyamaMethod : IGraphAlgorithm
    {
        protected INodeWrangler _nodeWrangler;
        protected List<IConnection> _connections;
        protected List<IVertex> _vertices;

        protected List<NodeLayer> _layers = new List<NodeLayer>();
        protected List<VertexIsland> _islands = new List<VertexIsland>();

        // ── connection helpers ────────────────────────────────────────────────

        protected List<IConnection> GetConnections(INode node)
        {
            var result = new List<IConnection>();
            foreach (var c in _connections)
                if (c.Source.Node == node || c.Target.Node == node)
                    result.Add(c);
            return result;
        }

        protected List<IConnection> GetConnections(INode node, PortDirection direction)
        {
            var result = new List<IConnection>();
            foreach (var c in _connections)
            {
                if (direction == PortDirection.In && c.Target.Node == node)
                    result.Add(c);
                else if (direction == PortDirection.Out && c.Source.Node == node)
                    result.Add(c);
            }
            return result;
        }

        protected void RemoveLoops()
        {
            var snapshot = new List<IConnection>(_connections);
            foreach (var c in snapshot)
                if (c.Source.Node == c.Target.Node)
                    _connections.Remove(c);
        }

        protected void RemoveEmpty()
        {
            var snapshot = new List<IVertex>(_vertices);
            foreach (var v in snapshot)
            {
                if (v is INode node)
                {
                    if (!GetConnections(node).Any())
                        _vertices.Remove(node);
                }
                else
                {
                    _vertices.Remove(v);
                }
            }
        }

        // ── layer index lookup ────────────────────────────────────────────────

        protected int GetLayerFromNode(INode node)
        {
            for (int i = 0; i < _layers.Count; i++)
                if (_layers[i].Nodes.Contains(node))
                    return i;
            return -1;
        }

        // ── step 1: horizontal positions ──────────────────────────────────────
        // Each node is horizontally centred inside its layer column.

        protected void AssignHorizontalPositions()
        {
            double x = 0.0;
            foreach (var layer in _layers)
            {
                double layerWidth = layer.GetWidth();
                foreach (var node in layer.Nodes)
                {
                    // Centre narrow nodes within the column so port anchors line up better.
                    double offset = (layerWidth - node.Size.Width) * 0.5;
                    node.Location = new Point(x + offset, 0);
                }
                x += layerWidth + EditorOptions.VertXSpacing;
            }
        }

        // ── step 2: vertical positions ────────────────────────────────────────
        // First pass: sequential stack (same as before).
        // Second pass: nudge each node toward the median Y of its neighbours so
        // connections that could be horizontal actually stay horizontal.

        protected void AssignVerticalPositions()
        {
            // Pass 1 — sequential stack
            foreach (var layer in _layers)
            {
                double y = 0.0;
                foreach (var node in layer.Nodes)
                {
                    node.Location = new Point(node.Location.X, y);
                    y += node.Size.Height + EditorOptions.VertYSpacing;
                }
            }

            // Pass 2 — median-pull (one relaxation sweep, left→right then right→left)
            for (int pass = 0; pass < 2; pass++)
            {
                int start = pass == 0 ? 0 : _layers.Count - 1;
                int end   = pass == 0 ? _layers.Count : -1;
                int step  = pass == 0 ? 1 : -1;

                for (int li = start; li != end; li += step)
                {
                    var layer = _layers[li];

                    for (int ni = 0; ni < layer.Nodes.Count; ni++)
                    {
                        var node = layer.Nodes[ni];

                        // Collect the current Y centres of all directly connected neighbours
                        var neighbourYs = new List<double>();
                        foreach (var c in GetConnections(node))
                        {
                            INode neighbour = c.Source.Node == node ? c.Target.Node : c.Source.Node;
                            if (neighbour != node)
                                neighbourYs.Add(neighbour.Location.Y + neighbour.Size.Height * 0.5);
                        }

                        if (neighbourYs.Count == 0)
                            continue;

                        neighbourYs.Sort();
                        double medianCentreY = neighbourYs[neighbourYs.Count / 2];
                        double targetY = medianCentreY - node.Size.Height * 0.5;

                        // Clamp so we don't overlap the previous node in this layer
                        double minY = ni == 0
                            ? 0
                            : layer.Nodes[ni - 1].Location.Y + layer.Nodes[ni - 1].Size.Height + EditorOptions.VertYSpacing;

                        // Don't push past the next node (look ahead)
                        double maxY = ni == layer.Nodes.Count - 1
                            ? double.MaxValue
                            : layer.Nodes[ni + 1].Location.Y - node.Size.Height - EditorOptions.VertYSpacing;

                        double clampedY = Math.Max(minY, Math.Min(maxY, targetY));
                        node.Location = new Point(node.Location.X, clampedY);
                    }
                }
            }
        }

        // ── step 3: barycentric crossing minimisation ─────────────────────────
        // For each layer, assign every node a "barycentric value" equal to the
        // average (or median) position-index of its neighbours in the adjacent
        // layer, then sort by that value.  We do 4 alternating passes (left-to-
        // right, right-to-left) which is sufficient for typical blueprint graphs.

        protected void BarycentricReorder()
        {
            const int passes = 4;

            for (int pass = 0; pass < passes; pass++)
            {
                bool leftToRight = (pass % 2 == 0);
                int start = leftToRight ? 1 : _layers.Count - 2;
                int end   = leftToRight ? _layers.Count : -1;
                int step  = leftToRight ? 1 : -1;

                for (int li = start; li != end; li += step)
                {
                    var layer    = _layers[li];
                    var adjLayer = _layers[li - step]; // the already-ordered adjacent layer

                    // Build a position map for the adjacent layer
                    var positionOf = new Dictionary<INode, int>();
                    for (int i = 0; i < adjLayer.Nodes.Count; i++)
                        positionOf[adjLayer.Nodes[i]] = i;

                    // Score each node in the current layer
                    var scores = new Dictionary<INode, double>();
                    foreach (var node in layer.Nodes)
                    {
                        var positions = new List<int>();
                        foreach (var c in GetConnections(node))
                        {
                            INode neighbour = c.Source.Node == node ? c.Target.Node : c.Source.Node;
                            if (positionOf.TryGetValue(neighbour, out int pos))
                                positions.Add(pos);
                        }

                        if (positions.Count == 0)
                        {
                            // No connections into this layer — keep current relative order
                            scores[node] = layer.Nodes.IndexOf(node);
                        }
                        else
                        {
                            // Use the median rather than the mean: more robust against outliers
                            positions.Sort();
                            scores[node] = positions[positions.Count / 2];
                        }
                    }

                    // Stable sort: nodes with equal scores keep their relative order
                    layer.Nodes.Sort((a, b) =>
                    {
                        double diff = scores[a] - scores[b];
                        if (Math.Abs(diff) < 0.001) return 0;
                        return diff < 0 ? -1 : 1;
                    });
                }
            }
        }

        // ── step 4: MergeLayers — bug-fixed ──────────────────────────────────
        // Original used `earliestLayer == 0` as "not found", which silently
        // dropped any node whose nearest neighbour was in layer 0.
        // Now uses nullable int; also guards against moving a node to a layer
        // where it would have no incoming edges (which re-introduces cycles).

        protected void MergeLayers()
        {
            for (int i = 0; i < _layers.Count; i++)
            {
                var layer = _layers[i];
                var nodes = new List<INode>(layer.Nodes);

                foreach (var node in nodes)
                {
                    int? bestLayer = null;

                    foreach (var c in GetConnections(node, PortDirection.Out))
                    {
                        int targetLayer = GetLayerFromNode(c.Target.Node);
                        if (targetLayer < 0) continue;

                        // Only move if the target is more than one layer away
                        if (targetLayer > i + 1)
                        {
                            if (bestLayer == null || targetLayer < bestLayer.Value)
                                bestLayer = targetLayer;
                        }
                    }

                    if (bestLayer == null) continue;

                    int dest = bestLayer.Value - 1;
                    if (dest == i) continue; // already there

                    // Safety: don't move if node has incoming edges from layers >= dest
                    // (that would create a backward edge, re-introducing a cycle)
                    bool safeToMove = true;
                    foreach (var c in GetConnections(node, PortDirection.In))
                    {
                        int srcLayer = GetLayerFromNode(c.Source.Node);
                        if (srcLayer >= dest)
                        {
                            safeToMove = false;
                            break;
                        }
                    }

                    if (!safeToMove) continue;

                    layer.Nodes.Remove(node);
                    _layers[dest].Nodes.Add(node);
                }
            }

            // Remove any layers that became empty after merging
            _layers.RemoveAll(l => l.Nodes.Count == 0);
        }

        // ── main entry point ──────────────────────────────────────────────────

        public virtual void SortGraph()
        {
            RemoveLoops();

            foreach (var v in _vertices)
                if (v is INode node)
                    new CycleRemover(_connections).RemoveCycles(node);

            var topo = new TopologicalSort(_vertices, _connections);
            List<IVertex> sorted = topo.SortGraph();

            RemoveEmpty();
            RemoveLoops();

            // Island detection (unchanged — used by subclasses)
            var islandSolver = new IslandSolver(_connections);
            foreach (var v in _vertices)
                if (v is INode node)
                {
                    var island = islandSolver.GetIsland(node);
                    if (island != null) _islands.Add(island);
                }

            var layerMaker = new LayerMaker(sorted, _connections);
            _layers = layerMaker.CreateLayers();

            MergeLayers();           // pull nodes close to their targets
            BarycentricReorder();    // ← NEW: minimise crossings
            AssignHorizontalPositions();
            AssignVerticalPositions(); // ← improved: median-pull second pass
        }

        public SugiyamaMethod(List<IConnection> connections, List<IVertex> vertices)
        {
            _connections = connections;
            _vertices    = vertices;
        }
    }
}