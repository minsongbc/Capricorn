#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.Experimental.GraphView;

using Newtonsoft.Json;

namespace Dunward.Capricorn
{
    public class GraphView : UnityEditor.Experimental.GraphView.GraphView
    {
        private InputNode inputNode; // This node is the start point of the graph. It is not deletable and unique.
        private int lastNodeID = 0;

        public GraphView()
        {
            inputNode = new InputNode(this, -1, 100, 200);
            AddElement(inputNode);
            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            this.AddManipulator(new DragAndDropManipulator(data => Load(data)));
            this.AddManipulator(new ContextualMenuManipulator(evt => evt.menu.AppendAction("Add Node", OnClickAddNode, DropdownMenuAction.AlwaysEnabled)));
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();

            ports.ForEach(port =>
            {
                if (startPort == port) return;
                if (startPort.node == port.node) return;
                if (startPort.direction == port.direction) return;

                compatiblePorts.Add(port);
            });

            return compatiblePorts;
        }

        public string SerializeGraph()
        {
            var data = new GraphData();
            foreach (var node in nodes)
            {
                if (node is BaseNode capricornGraphNode)
                {
                    data.nodes.Add(capricornGraphNode.GetMainData());
                }
            }
            return JsonConvert.SerializeObject(data);
        }

        public void Load(string json)
        {
            ClearGraph();

            var data = JsonConvert.DeserializeObject<GraphData>(json);
            foreach (var nodeData in data.nodes)
            {
                switch (nodeData.nodeType)
                {
                    case NodeType.Input:
                        var inputNode = new InputNode(this, nodeData);
                        AddElement(inputNode);
                        break;
                    case NodeType.Output:
                        var outputNode = new OutputNode(this, nodeData);
                        AddElement(outputNode);
                        break;
                    case NodeType.Connector:
                    default:
                        var node = new ConnectorNode(this, nodeData);
                        AddElement(node);
                        break;
                }
            }

            ConnectDeserializeNodes();
            lastNodeID = data.nodes.Max(n => n.id);
        }
        
        private void ConnectDeserializeNodes()
        {
            foreach (var node in nodes)
            {
                var baseNode = node as BaseNode;
                for (int i = 0; i < baseNode.main.action.data.connections.Count; i++)
                {
                    var connection = baseNode.main.action.data.connections[i];
                    var outputPort = baseNode.outputContainer[i] as Port;
                    var inputPort = nodes.Where(n => n is BaseNode)
                                        .Cast<BaseNode>()
                                        .First(n => n.ID == connection).inputContainer[0] as Port;

                    var edge = outputPort.ConnectTo(inputPort);
                    AddElement(edge);
                }
            }
        }

        private void ClearGraph()
        {
            DeleteElements(nodes.ToList());
            DeleteElements(edges.ToList());
        }

        private void OnClickAddNode(DropdownMenuAction action)
        {
            var menu = ScriptableObject.CreateInstance<NodeSearchWindow>();
            menu.onSelectNode = (type) => 
            {
                // create node type
                var node = (BaseNode)System.Activator.CreateInstance(type, this, ++lastNodeID, action.eventInfo.mousePosition);
                AddElement(node);
            };

            SearchWindow.Open(new SearchWindowContext(action.eventInfo.mousePosition), menu);
        }

        private class DragAndDropManipulator : PointerManipulator
        {
            private Object item;
            private readonly System.Action<string> onLoadGraph;

            public DragAndDropManipulator(System.Action<string> onLoadGraph)
            {
                this.onLoadGraph = onLoadGraph;
            }

            protected override void RegisterCallbacksOnTarget()
            {
                target.RegisterCallback<DragEnterEvent>(OnDragEnter);
                target.RegisterCallback<DragLeaveEvent>(OnDragLeave);
                target.RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
                target.RegisterCallback<DragPerformEvent>(OnDragPerform);
            }

            protected override void UnregisterCallbacksFromTarget()
            {
                target.UnregisterCallback<DragEnterEvent>(OnDragEnter);
                target.UnregisterCallback<DragLeaveEvent>(OnDragLeave);
                target.UnregisterCallback<DragUpdatedEvent>(OnDragUpdated);
                target.UnregisterCallback<DragPerformEvent>(OnDragPerform);
            }

            private void OnDragEnter(DragEnterEvent _)
            {
                item = DragAndDrop.objectReferences[0];
            }

            private void OnDragLeave(DragLeaveEvent _)
            {
                item = null;
            }

            private void OnDragUpdated(DragUpdatedEvent _)
            {
                DragAndDrop.visualMode = IsSingleTextAsset() ? DragAndDropVisualMode.Link : DragAndDropVisualMode.Rejected;
            }

            private void OnDragPerform(DragPerformEvent _)
            {
                var textAsset = item as TextAsset;
                EditorUtility.DisplayProgressBar("Capricorn", "Load Graph...", 0.112f);
                onLoadGraph?.Invoke(textAsset.text);
                EditorUtility.ClearProgressBar();
            }

            private bool IsSingleTextAsset()
            {
                return item is TextAsset && DragAndDrop.objectReferences.Length == 1;
            }
        }
    }
}
#endif