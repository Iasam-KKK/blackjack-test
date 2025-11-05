using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace Map
{
    public class MapViewUI : MapView
    {
        [Header("UI Map Settings")]
        [Tooltip("ScrollRect that will be used for orientations: Left To Right, Right To Left")]
        [SerializeField] private ScrollRect scrollRectHorizontal;
        [Tooltip("ScrollRect that will be used for orientations: Top To Bottom, Bottom To Top")]
        [SerializeField] private ScrollRect scrollRectVertical;
        [Tooltip("Multiplier to compensate for larger distances in UI pixels on the canvas compared to distances in world units")]
        [SerializeField] private float unitsToPixelsMultiplier  = 10f;
        [Tooltip("Padding of the first and last rows of nodes from the sides of the scroll rect")]
        [SerializeField] private float padding;
        [Tooltip("Prefab of the UI line between the nodes (uses scripts from Unity UI Extensions)")]
        [SerializeField] private UILineRenderer uiLinePrefab;

        protected override void ClearMap()
        {
            scrollRectHorizontal.gameObject.SetActive(false);
            scrollRectVertical.gameObject.SetActive(false);

            foreach (ScrollRect scrollRect in new []{scrollRectHorizontal, scrollRectVertical})
            {
                // Only destroy runtime-generated map objects, preserve manually added backgrounds
                foreach (Transform t in scrollRect.content)
                {
                    if (t.gameObject.name == "OuterMapParent")
                    {
                        Destroy(t.gameObject);
                    }
                }
            }
            
            MapNodes.Clear();
            lineConnections.Clear();
        }

        private ScrollRect GetScrollRectForMap()
        {
            return orientation == MapOrientation.LeftToRight || orientation == MapOrientation.RightToLeft
                ? scrollRectHorizontal
                : scrollRectVertical;
        }

        protected override void CreateMapParent()
        {
            ScrollRect scrollRect = GetScrollRectForMap();
            scrollRect.gameObject.SetActive(true);
            
            // Set ScrollRect to Unrestricted movement type
            scrollRect.movementType = ScrollRect.MovementType.Unrestricted;
            
            // Enable scrolling in the appropriate direction based on orientation
            if (orientation == MapOrientation.LeftToRight || orientation == MapOrientation.RightToLeft)
            {
                scrollRect.horizontal = true;
                scrollRect.vertical = false;
            }
            else
            {
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
            }
            
            firstParent = new GameObject("OuterMapParent");
            firstParent.transform.SetParent(scrollRect.content);
            firstParent.transform.localScale = Vector3.one;
            RectTransform fprt = firstParent.AddComponent<RectTransform>();
            Stretch(fprt);
            
            mapParent = new GameObject("MapParentWithAScroll");
            mapParent.transform.SetParent(firstParent.transform);
            mapParent.transform.localScale = Vector3.one;
            RectTransform mprt = mapParent.AddComponent<RectTransform>();
            Stretch(mprt);
            
            SetMapLength();
            ScrollToOrigin();
        }

        private void SetMapLength()
        {
            RectTransform rt = GetScrollRectForMap().content;
            Vector2 sizeDelta = rt.sizeDelta;
            float length = padding + Map.DistanceBetweenFirstAndLastLayers() * unitsToPixelsMultiplier;
            if (orientation == MapOrientation.LeftToRight || orientation == MapOrientation.RightToLeft)
                sizeDelta.x = length;
            else
                sizeDelta.y = length;
            rt.sizeDelta = sizeDelta;
        }

        private void ScrollToOrigin()
        {
            switch (orientation)
            {
                case MapOrientation.BottomToTop:
                    scrollRectVertical.normalizedPosition = Vector2.zero;
                    break;
                case MapOrientation.TopToBottom:
                    scrollRectVertical.normalizedPosition = new Vector2(0, 1);
                    break;
                case MapOrientation.RightToLeft:
                    scrollRectHorizontal.normalizedPosition = new Vector2(1, 0);
                    break;
                case MapOrientation.LeftToRight:
                    scrollRectHorizontal.normalizedPosition = Vector2.zero;
                    break;
                default:
                    break;
            }
        }

        private static void Stretch(RectTransform tr)
        {
            tr.localPosition = Vector3.zero;
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.sizeDelta = Vector2.zero;
            tr.anchoredPosition = Vector2.zero;
        }

        protected override MapNode CreateMapNode(Node node)
        {
            GameObject mapNodeObject = Instantiate(nodePrefab, mapParent.transform);
            MapNode mapNode = mapNodeObject.GetComponent<MapNode>();
            NodeBlueprint blueprint = GetBlueprint(node.blueprintName);
            mapNode.SetUp(node, blueprint);
            mapNode.transform.localPosition = GetNodePosition(node);
            return mapNode;
        }

        private Vector2 GetNodePosition(Node node)
        {
            float length = padding + Map.DistanceBetweenFirstAndLastLayers() * unitsToPixelsMultiplier;
            
            switch (orientation)
            {
                case MapOrientation.BottomToTop:
                    return new Vector2(0f, (padding - length) / 2f) +
                           node.position * unitsToPixelsMultiplier;
                case MapOrientation.TopToBottom:
                    return new Vector2(0f, (length - padding) / 2f) -
                           node.position * unitsToPixelsMultiplier;
                case MapOrientation.RightToLeft:
                    return new Vector2((length - padding) / 2f, 0f) -
                           Flip(node.position) * unitsToPixelsMultiplier;
                case MapOrientation.LeftToRight:
                    return new Vector2((padding - length) / 2f, 0f) +
                           Flip(node.position) * unitsToPixelsMultiplier;
                default:
                    return Vector2.zero;
            }
        }

        private static Vector2 Flip(Vector2 other) => new Vector2(other.y, other.x);

        protected override void SetOrientation()
        {
            // do nothing here for UI:
        }

        protected override void CreateMapBackground(Map m)
        {
            // Background is manually controlled via Inspector
            // Add child GameObjects with Image components to the ScrollRect content
            // They will be preserved when the map regenerates
        }

        protected override void AddLineConnection(MapNode from, MapNode to)
        {
            if (uiLinePrefab == null) return;
            
            UILineRenderer lineRenderer = Instantiate(uiLinePrefab, mapParent.transform);
            lineRenderer.transform.SetAsFirstSibling();
            RectTransform fromRT = from.transform as RectTransform;
            RectTransform toRT = to.transform as RectTransform;
            Vector2 fromPoint = fromRT.anchoredPosition +
                                (toRT.anchoredPosition - fromRT.anchoredPosition).normalized * offsetFromNodes;

            Vector2 toPoint = toRT.anchoredPosition +
                              (fromRT.anchoredPosition - toRT.anchoredPosition).normalized * offsetFromNodes;

            // drawing lines in local space:
            lineRenderer.transform.position = from.transform.position +
                                              (Vector3) (toRT.anchoredPosition - fromRT.anchoredPosition).normalized *
                                              offsetFromNodes;

            // line renderer with 2 points only does not handle transparency properly:
            List<Vector2> list = new List<Vector2>();
            for (int i = 0; i < linePointsCount; i++)
            {
                list.Add(Vector3.Lerp(Vector3.zero, toPoint - fromPoint +
                                                    2 * (fromRT.anchoredPosition - toRT.anchoredPosition).normalized *
                                                    offsetFromNodes, (float) i / (linePointsCount - 1)));
            }
            
            Debug.Log("From: " + fromPoint + " to: " + toPoint + " last point: " + list[list.Count - 1]);

            lineRenderer.Points = list.ToArray();

            DottedLineRenderer dottedLine = lineRenderer.GetComponent<DottedLineRenderer>();
            if (dottedLine != null) dottedLine.ScaleMaterial();

            lineConnections.Add(new LineConnection(null, lineRenderer, from, to));
        }
    }
}