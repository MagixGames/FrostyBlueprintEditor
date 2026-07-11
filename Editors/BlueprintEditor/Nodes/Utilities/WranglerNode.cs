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
using FrostySdk.Ebx;
using FrostySdk.IO;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Utilities
{
    public class WranglerNode : BaseNode, ITransient, IRedirect
    {
        private const int FormatVersion = 100;

        public ConnectionType ConnectionType { get; set; }

        public IPort OriginalSource { get; set; }

        public IPort OriginalTarget { get; set; }

        public override string Header => "Wrangler";

        #region IRedirect

        public PortDirection Direction { get; set; }
        public IRedirect SourceRedirect { get; set; }
        public IRedirect TargetRedirect { get; set; }
        public IPort RedirectTarget { get; set; }

        #endregion

        private bool _loadedFromLayout;

        public WranglerNode(ConnectionType type, INodeWrangler wrangler) : base(wrangler)
        {
            ConnectionType = type;
            Size = new Size(16, 16);
            CreatePorts();
        }

        public WranglerNode(INodeWrangler wrangler) : base(wrangler) { }
        public WranglerNode() { }

        private void CreatePorts()
        {
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
        }

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

        public override void OnCreation()
        {
            if (!_loadedFromLayout)
                return;

            _loadedFromLayout = false;

            if (OriginalSource == null || OriginalTarget == null || Inputs.Count == 0 || Outputs.Count == 0)
                return;

            // Find the EBX-backed connection from OriginalSource to OriginalTarget
            IConnection existingConnection = null;
            foreach (IConnection conn in NodeWrangler.Connections)
            {
                if (conn.Source == OriginalSource && conn.Target == OriginalTarget)
                {
                    existingConnection = conn;
                    break;
                }
            }

            if (existingConnection == null)
                return;

            // Retarget it through the wrangler: OriginalSource -> wrangler.Inputs[0]
            existingConnection.Target = Inputs[0];

            // Add transient connection: wrangler.Outputs[0] -> OriginalTarget
            NodeWrangler.AddConnection(new TransientConnection(Outputs[0], OriginalTarget, ConnectionType));
        }

        #region ITransient

        public bool Load(LayoutReader reader)
        {
            int first = reader.ReadInt();

            // Detect old format: first int was the ConnectionType (0-2)
            if (first >= 0 && first <= 2)
            {
                ConnectionType = (ConnectionType)first;
                Location = reader.ReadPoint();
                Size = new Size(16, 16);
                CreatePorts();
                return true;
            }

            // New format
            int version = first;
            ConnectionType = (ConnectionType)reader.ReadInt();
            Location = reader.ReadPoint();
            Size = new Size(16, 16);
            CreatePorts();

            if (version >= FormatVersion)
            {
                // Read OriginalSource port ref
                if (reader.ReadBoolean())
                {
                    AssetClassGuid guid = reader.ReadAssetClassGuid();
                    string portName = reader.ReadNullTerminatedString();
                    ConnectionType portType = (ConnectionType)reader.ReadInt();
                    OriginalSource = FindPort(guid, portName, portType, PortDirection.Out);
                }

                // Read OriginalTarget port ref
                if (reader.ReadBoolean())
                {
                    AssetClassGuid guid = reader.ReadAssetClassGuid();
                    string portName = reader.ReadNullTerminatedString();
                    ConnectionType portType = (ConnectionType)reader.ReadInt();
                    OriginalTarget = FindPort(guid, portName, portType, PortDirection.In);
                }

                _loadedFromLayout = true;
            }

            return true;
        }

        public void Save(LayoutWriter writer)
        {
            writer.Write(FormatVersion);
            writer.Write((int)ConnectionType);
            writer.Write(Location);

            WritePortRef(writer, OriginalSource);
            WritePortRef(writer, OriginalTarget);
        }

        #endregion

        private void WritePortRef(LayoutWriter writer, IPort port)
        {
            if (port?.Node is IEntityNode entityNode)
            {
                writer.Write(true);
                writer.Write(entityNode.InternalGuid);
                writer.WriteNullTerminatedString(port.Name);
                writer.Write((int)((EntityPort)port).Type);
            }
            else
            {
                writer.Write(false);
            }
        }

        private IPort FindPort(AssetClassGuid guid, string portName, ConnectionType portType, PortDirection direction)
        {
            foreach (IVertex vertex in NodeWrangler.Vertices)
            {
                if (!(vertex is IEntityNode entityNode))
                    continue;

                if (!entityNode.InternalGuid.Equals(guid))
                    continue;

                if (direction == PortDirection.Out)
                    return entityNode.GetOutput(portName, portType);

                return entityNode.GetInput(portName, portType);
            }

            return null;
        }
    }
}
