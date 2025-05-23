using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SelfMovingCart.Patches
{
    // Helper class with useful functions
    public static class MiscHelper
    {
        private static Dictionary<PrimitiveType, Mesh> primitiveMeshes = new Dictionary<PrimitiveType, Mesh>();

        public static void AddSphereToGameObject(GameObject gameObject, float radius, Color color) // gameobject, 0.5f, Color.red
        {
            // Add visual components to make it visible
            // 1. Add a sphere collider
            SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
            sphere.radius = radius;
            sphere.isTrigger = true; // So it doesn't affect physics

            // 2. Add a mesh filter with a sphere mesh
            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.mesh = MiscHelper.GetPrimitiveMesh(PrimitiveType.Sphere);

            // 3. Add a mesh renderer with a material
            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = color;
        }

        public static void AddCubeToGameObject(GameObject gameObject, float width, float height, Color color)
        {
            // Add visual components to make it visible
            // 1. Add a box collider
            BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true; // So it doesn't affect physics
            gameObject.transform.localScale = new Vector3(width, height, width);

            // 2. Add a mesh filter with a cube mesh
            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.mesh = MiscHelper.GetPrimitiveMesh(PrimitiveType.Cube);

            // 3. Add a mesh renderer with a material
            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = color;
        }

        // Adds a mesh to object.
        public static Mesh GetPrimitiveMesh(PrimitiveType type)
        {
            if (!primitiveMeshes.ContainsKey(type))
            {
                GameObject go = GameObject.CreatePrimitive(type);
                Mesh mesh = go.GetComponent<MeshFilter>().sharedMesh;
                GameObject.Destroy(go);

                primitiveMeshes[type] = mesh;
            }

            return primitiveMeshes[type];
        }

        // Returns a point that is [distance] away from the [originPosition] towards a [targetPosition].
        public static Vector3 GetPointTowardTarget(Vector3 originPosition, Vector3 targetPosition, float distance)
        {
            // Calculate direction vector from origin to target.
            Vector3 directionToGoal = targetPosition - originPosition;

            // Calculate the distance between origin and target.
            float distanceToGoal = directionToGoal.magnitude;

            // Normalize the direction vector (make it length 1)
            Vector3 normalizedDirection = directionToGoal.normalized;

            // If goal is closer than our desired distance, return the goal position
            if (distanceToGoal <= distance)
            {
                return targetPosition;
            }
            // Otherwise, move 'distance' units toward the goal
            else
            {
                return originPosition + normalizedDirection * distance;
            }
        }
    }
}
