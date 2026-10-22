using UnityEngine;

namespace WindingTale.Scenes.GameFreeFieldScene
{
    /// <summary>
    /// Lays a creature model down on its side and stands it back up -- Yuni, found fainted on
    /// Map 34. Lying, the model keeps its face to the camera with its head to the left of the
    /// screen (turned a quarter about the axis pointing at the camera), stays centred on its
    /// tile, and is sunk a little so part of the body sinks into the grass. Standing up puts
    /// it back exactly where and how it stood, facing front.
    /// </summary>
    public class FreeFieldPose : MonoBehaviour
    {
        /// <summary>How much of the lying body's height goes under the ground.</summary>
        private const float SinkShare = 0.3f;

        private Vector3 standingPosition;
        private Quaternion standingRotation;

        public bool IsLying { get; private set; }

        public void LieDown()
        {
            if (IsLying)
            {
                return;
            }

            standingPosition = transform.position;
            standingRotation = transform.rotation;
            Bounds standing = MeshBounds();

            // The camera looks at the board from the south (yaw 180), so the screen's left is
            // world +X; a -90 degree turn about Z takes the head (+Y) there.
            transform.rotation = Quaternion.Euler(0f, 0f, -90f);
            Bounds lying = MeshBounds();

            transform.position += new Vector3(
                standing.center.x - lying.center.x,
                standing.min.y - lying.min.y - lying.size.y * SinkShare,
                standing.center.z - lying.center.z);

            IsLying = true;
        }

        public void StandUp()
        {
            if (!IsLying)
            {
                return;
            }

            transform.SetPositionAndRotation(standingPosition, standingRotation);
            FreeFieldWalker walker = GetComponent<FreeFieldWalker>();
            if (walker != null)
            {
                walker.Face(FreeFieldFacing.Down);
            }

            IsLying = false;
        }

        /// <summary>
        /// The world box around every mesh of the model, hidden animation frames included
        /// (the Animator switches their renderers, and a disabled renderer has no bounds).
        /// </summary>
        private Bounds MeshBounds()
        {
            bool any = false;
            Bounds bounds = new Bounds(transform.position, Vector3.zero);
            foreach (MeshFilter filter in GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh mesh = filter.sharedMesh;
                if (mesh == null)
                {
                    continue;
                }

                Bounds local = mesh.bounds;
                Matrix4x4 toWorld = filter.transform.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = new Vector3(
                        (i & 1) == 0 ? local.min.x : local.max.x,
                        (i & 2) == 0 ? local.min.y : local.max.y,
                        (i & 4) == 0 ? local.min.z : local.max.z);
                    Vector3 world = toWorld.MultiplyPoint3x4(corner);
                    if (!any)
                    {
                        bounds = new Bounds(world, Vector3.zero);
                        any = true;
                    }
                    else
                    {
                        bounds.Encapsulate(world);
                    }
                }
            }

            return bounds;
        }
    }
}
