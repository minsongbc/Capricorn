using UnityEngine;
using UnityEngine.UIElements;

namespace Dunward.Capricorn
{
    public class ConnectorNode : Node
    {
        public ConnectorNode(GraphView graphView, int id, float x, float y) : base(graphView, id, x, y)
        {
        }

        public ConnectorNode(GraphView graphView, int id, Vector2 mousePosition) : base(graphView, id, mousePosition)
        {
        }

        public ConnectorNode(GraphView graphView, NodeMainData mainData) : base(graphView, mainData)
        {
        }

        private void UpdateSubContainers(NodeMainData mainData)
        {
            main.action.DeserializeConnections(mainData.actionData);
        }

        protected override void SetupTitleContainer()
        {
            var title = new TextField() { value = $"{id}" };
            title.RegisterValueChangedCallback(evt =>
            {
                customTitle = evt.newValue;
            });
            title.RegisterCallback<FocusOutEvent>(evt =>
            {
                if (string.IsNullOrEmpty(title.value))
                {
                    customTitle = string.Empty;
                    title.value = $"{id}";
                }
            });

            titleContainer.Insert(0, title);
        }
    }
}