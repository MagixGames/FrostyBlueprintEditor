using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;
using BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager.IO;
using BlueprintEditorPlugin.Editors.GraphEditor.NodeWrangler;
using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Models.Entities.Networking;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using BlueprintEditorPlugin.Models.Nodes.Utilities;
using FrostySdk.IO;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Utilities
{
    /// <summary>
    /// A single draggable dot placed on a wire that acts as a routing point.
    /// Wires pass through it visually. You cannot pull new wires from or to it.
    /// Implements IRedirect so BaseNodeWrangler skips ClearConnections on deletion,
    /// allowing OnDestruction to heal the wire.
    /// </summary>
    public class WranglerNode : BaseNode, ITransient, IRedirect
    {
        public ConnectionType ConnectionType { get; set; }

        /// <summary>The original connection's source port before splitting.</summary>
        public IPort OriginalSource { get; set; }

        /// <summary>The original connection's target port before splitting.</summary>
        public IPort OriginalTarget { get; set; }

        public override string Header => "Wrangler";

        #region IRedirect (minimal — prevents ClearConnections from wiping the wire)

        public PortDirection Direction { get; set; }
        public IRedirect SourceRedirect { get; set; }
        public IRedirect TargetRedirect { get; set; }
        public IPort RedirectTarget { get; set; }

        #endregion

        public WranglerNode(ConnectionType type, INodeWrangler wrangler) : base(wrangler)
        {
            ConnectionType = type;
            Size = new Size(16, 16);

            switch (type)
            {
                case ConnectionType.Event:
                    Inputs.Add(new EventInput("In", this) { Realm = Realm.Any });
                    Outputs.Add(new EventOutput("Out", this) { Realm = Realm.Any });
                    break;
                case ConnectionType.Link:
                    Inputs.Add(new LinkInput("In", this) { Realm = Realm.Any });
                    Outputs.Add(new LinkOutput("Out", this) { Realm = Realm.Any });
                    break;
                case ConnectionType.Property:
                    Inputs.Add(new PropertyInput("In", this) { Realm = Realm.Any });
                    Outputs.Add(new PropertyOutput("Out", this) { Realm = Realm.Any });
                    break;
            }
        }

        public WranglerNode(INodeWrangler wrangler) : base(wrangler) { }
        public WranglerNode() { }

        /// <summary>
        /// Heals the wire when deleted: retargets the real connection back to the
        /// original target, and removes the transient from our output.
        /// </summary>
        public override void OnDestruction()
        {
            if (OriginalSource == null || OriginalTarget == null)
                return;

            IConnection targetConnection = null;
            foreach (IConnection connection in NodeWrangler.GetConnections(Inputs[0]))
            {
                if (connection.Target == Inputs[0])
                {
                    targetConnection = connection;
                    break;
                }
            }

            if (targetConnection != null)
            {
                targetConnection.Target = OriginalTarget;
            }

            List<IConnection> toRemove = new List<IConnection>();
            foreach (IConnection connection in NodeWrangler.GetConnections(Outputs[0]))
            {
                if (connection.Source == Outputs[0])
                    toRemove.Add(connection);
            }

            foreach (IConnection connection in toRemove)
                NodeWrangler.RemoveConnection(connection);
        }

        #region ITransient

        public bool Load(LayoutReader reader)
        {
            ConnectionType = (ConnectionType)reader.ReadInt();
            Location = reader.ReadPoint();
            Size = new Size(16, 16);

            switch (ConnectionType)
            {
                case ConnectionType.Event:
                    Inputs.Add(new EventInput("In", this) { Realm = Realm.Any });
                    Outputs.Add(new EventOutput("Out", this) { Realm = Realm.Any });
                    break;
                case ConnectionType.Link:
                    Inputs.Add(new LinkInput("In", this) { Realm = Realm.Any });
                    Outputs.Add(new LinkOutput("Out", this) { Realm = Realm.Any });
                    break;
                case ConnectionType.Property:
                    Inputs.Add(new PropertyInput("In", this) { Realm = Realm.Any });
                    Outputs.Add(new PropertyOutput("Out", this) { Realm = Realm.Any });
                    break;
            }

            return true;
        }

        public void Save(LayoutWriter writer)
        {
            writer.Write((int)ConnectionType);
            writer.Write(Location);
        }

        #endregion
    }
}
