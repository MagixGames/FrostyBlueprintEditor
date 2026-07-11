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
using FrostySdk.IO;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Utilities
{
    /// <summary>
    /// A simple passthrough node with 1 input and 1 output, used to reroute connections on the graph.
    /// When placed on a wire, it splits the connection and allows the user to freely move the routing point.
    /// </summary>
    public class WranglerNode : BaseNode, ITransient
    {
        public ConnectionType ConnectionType { get; set; }

        /// <summary>
        /// The original connection's source port before splitting, used for rewiring on destruction.
        /// </summary>
        public IPort OriginalSource { get; set; }

        /// <summary>
        /// The original connection's target port before splitting, used for rewiring on destruction.
        /// </summary>
        public IPort OriginalTarget { get; set; }

        public override string Header
        {
            get => "Wrangler";
        }

        public WranglerNode(ConnectionType type, INodeWrangler wrangler) : base(wrangler)
        {
            ConnectionType = type;
            Size = new Size(100, 24);

            switch (type)
            {
                case ConnectionType.Event:
                {
                    Inputs.Add(new EventInput("In", this) { Realm = Realm.Any });
                    Outputs.Add(new EventOutput("Out", this) { Realm = Realm.Any });
                } break;
                case ConnectionType.Link:
                {
                    Inputs.Add(new LinkInput("In", this) { Realm = Realm.Any });
                    Outputs.Add(new LinkOutput("Out", this) { Realm = Realm.Any });
                } break;
                case ConnectionType.Property:
                {
                    Inputs.Add(new PropertyInput("In", this) { Realm = Realm.Any });
                    Outputs.Add(new PropertyOutput("Out", this) { Realm = Realm.Any });
                } break;
            }
        }

        public WranglerNode()
        {
        }

        /// <summary>
        /// Retarget any connection going into our input back to the original target,
        /// and remove the transient from our output to the original target.
        /// This effectively "heals" the wire when the wrangler node is deleted.
        /// </summary>
        public override void OnDestruction()
        {
            if (OriginalSource == null || OriginalTarget == null)
                return;

            // Find the connection that was redirected to our input (source -> our input)
            // and retarget it back to the original target
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

            // Remove the transient connection from our output to the original target
            List<IConnection> toRemove = new List<IConnection>();
            foreach (IConnection connection in NodeWrangler.GetConnections(Outputs[0]))
            {
                if (connection.Source == Outputs[0])
                {
                    toRemove.Add(connection);
                }
            }

            foreach (IConnection connection in toRemove)
            {
                NodeWrangler.RemoveConnection(connection);
            }
        }

        #region ITransient

        public bool Load(LayoutReader reader)
        {
            ConnectionType = (ConnectionType)reader.ReadInt();
            Location = reader.ReadPoint();

            // Recreate ports based on connection type
            switch (ConnectionType)
            {
                case ConnectionType.Event:
                {
                    Inputs.Add(new EventInput("In", this) { Realm = Realm.Any });
                    Outputs.Add(new EventOutput("Out", this) { Realm = Realm.Any });
                } break;
                case ConnectionType.Link:
                {
                    Inputs.Add(new LinkInput("In", this) { Realm = Realm.Any });
                    Outputs.Add(new LinkOutput("Out", this) { Realm = Realm.Any });
                } break;
                case ConnectionType.Property:
                {
                    Inputs.Add(new PropertyInput("In", this) { Realm = Realm.Any });
                    Outputs.Add(new PropertyOutput("Out", this) { Realm = Realm.Any });
                } break;
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
