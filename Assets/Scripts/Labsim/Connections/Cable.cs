using UnityEngine;

namespace LabSim
{
    /// <summary>
    /// Visual + logical link between two connectors. Drawn as a cubic Bezier curve with a LineRenderer:
    /// P0 = A, P1 = A + k*dirA + sag, P2 = B + k*dirB + sag, P3 = B.
    /// Follows the connectors every frame, so instruments can be moved.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class Cable : MonoBehaviour
    {
        public int segments = 32;
        [Tooltip("Sag as a fraction of the distance between the plugs.")]
        public float sagFactor = 0.25f;
        [Tooltip("Straight length leaving each plug, in meters.")]
        public float minExitLength = 0.04f;

        public Connector A { get; private set; }
        public Connector B { get; private set; }

        LineRenderer line;
        Vector3[] points;

        public void Init(Connector a, Connector b, Material material, Color color, float width)
        {
            A = a;
            B = b;
            line = GetComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = segments + 1;
            line.widthMultiplier = width;
            line.numCapVertices = 4;
            line.numCornerVertices = 2;
            line.sharedMaterial = material;
            line.startColor = color;
            line.endColor = color;
            points = new Vector3[segments + 1];
            UpdateShape();
        }

        public Connector OtherEnd(Connector c)
        {
            if (c == A) return B;
            if (c == B) return A;
            return null;
        }

        void LateUpdate()
        {
            UpdateShape();
        }

        void UpdateShape()
        {
            if (A == null || B == null || line == null) return;
            BuildCurve(A.PlugPosition, A.PlugDirection, B.PlugPosition, B.PlugDirection,
                       sagFactor, minExitLength, points);
            line.SetPositions(points);
        }

        /// <summary>Fills 'buffer' with a sagging cubic Bezier between two plugs.</summary>
        public static void BuildCurve(Vector3 p0, Vector3 dir0, Vector3 p3, Vector3 dir1,
                                      float sagFactor, float minExit, Vector3[] buffer)
        {
            float dist = Vector3.Distance(p0, p3);
            float k = Mathf.Max(minExit, dist * 0.3f);
            Vector3 sag = Vector3.down * (dist * sagFactor);
            Vector3 p1 = p0 + dir0 * k + sag;
            Vector3 p2 = p3 + dir1 * k + sag;

            int n = buffer.Length - 1;
            for (int i = 0; i <= n; i++)
            {
                float u = (float)i / n;
                float v = 1f - u;
                buffer[i] = v * v * v * p0 + 3f * v * v * u * p1 + 3f * v * u * u * p2 + u * u * u * p3;
            }
        }

        /// <summary>Unplugs both ends and destroys the cable object.</summary>
        public void Detach()
        {
            if (A != null && A.Cable == this) A.Cable = null;
            if (B != null && B.Cable == this) B.Cable = null;
            A = null;
            B = null;
            Destroy(gameObject);
        }
    }
}
