using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

namespace SelfMovingCart.Patches
{
    class CartSelfMovementManager : MonoBehaviour
    {
        Rigidbody rb;
        public CartTargetSync cartTargetSync;
        Transform inCart;

        // Cart mass.
        float totalCartMass = 0f;
        float calculateObjectsEvery = 1f;

        // Pathfinding and nav
        int cornerInd = 0;
        NavMeshPath cartNavMeshPath;
        List<Vector3> pathCorners = new List<Vector3> ();
        bool isCartFollowingPath = false;

        float cornerMinDistance = 1f; // The distance at which the cart considers itself to have reached the corner.
        float cartSpeed = 15f;
        float cartRotationSpeed = 2f;
        float cartDriveRotationSpeed = 7f;

        // Extraction related variables.
        int extractionPointInd = -1;
        bool isExtracting = false;

        public bool isCartBeingPulled = false;
        Vector3 lastCartPosition = Vector3.zero;

        // Remote control
        public bool remoteControlForward = false;
        public bool remoteControlBack = false;
        public bool remoteControlRight = false;
        public bool remoteControlLeft = false;

        float pathRecalculationTimer = 5f; // The path will always be recalculated after 5 seconds.
        float checkDistanceFromPathTimer = 0.5f;
        Vector3 finalDestination;

        // Visualization.
        List<GameObject> cornerSpheres; // For path corners.
        GameObject cartHeightGuide; // Appears in front of the cart to check the height.
        bool cartGuideForward = true; // Should cart guide be forward or backwards.

        bool toVisualize = false;

        void Start()
        {
            // Setting up the cart.
            rb = GetComponent<Rigidbody>();
            cartTargetSync = gameObject.AddComponent<CartTargetSync>();
            inCart = base.transform.Find("In Cart");

            cartNavMeshPath = new NavMeshPath();
            isCartFollowingPath = false;

            isCartBeingPulled = false;

            remoteControlForward = false;
            remoteControlBack = false;
            remoteControlRight = false;
            remoteControlLeft = false;

            cornerSpheres = new List<GameObject>();

            cartHeightGuide = new GameObject("CartHeightGuide");
            cartHeightGuide.transform.localScale = Vector3.one / 4f;
            cartGuideForward = true;

            if (toVisualize)
                MiscHelper.AddSphereToGameObject(cartHeightGuide, 0.1f, Color.yellow);
        }

        void FixedUpdate()
        {
            GetNextCartStepHeight();
            HandleCollidedDoors();

            /*************************************/
            /**************CART MASS**************/
            /*************************************/
            calculateObjectsEvery -= Time.fixedDeltaTime;
            if (calculateObjectsEvery < 0f)
            {
                CalculateCartObjectsMass();
                calculateObjectsEvery = 0.5f;
                //SelfMovingCartBase.mls.LogInfo($"Cart mass: {totalCartMass}");
            }

            /******************************************/
            /**************REMOTE CONTROL**************/
            /******************************************/
            if ((remoteControlForward || remoteControlBack || remoteControlRight || remoteControlLeft) && !isCartBeingPulled)
            {
                StopPathfinding();

                ApplyUpwardsForceToCart();
                if(remoteControlForward || remoteControlBack) // Only change cart speed if it's moving, not just turning.
                    HandleCartSpeed();

                if (remoteControlForward)
                    MoveCartForward();
                if (remoteControlBack)
                    MoveCartBackward();
                if (remoteControlRight)
                    TurnCartRight();
                if (remoteControlLeft)
                    TurnCartLeft();
            }

            /***************************************/
            /**************PATHFINDING**************/
            /***************************************/
            // Disable pathfinding if player grabs cart.
            if (isCartBeingPulled && isCartFollowingPath) StopPathfinding();

            // If cart not pathfinding, return.
            if (!isCartFollowingPath) return;

            ApplyUpwardsForceToCart(); // This will make the cart always weigh the same while pathfinding so it doesn't get stuck at stairs.
            HandleCartSpeed();
            FollowPath();

            HandlePathRecalculation();
        }

        void HandlePathRecalculation()
        {
            checkDistanceFromPathTimer -= Time.fixedDeltaTime;
            pathRecalculationTimer -= Time.fixedDeltaTime;

            // Recalculate path if the cart deviated from the original path.
            if (checkDistanceFromPathTimer < 0f)
            {
                Vector3 closestPointOnPath = GetClosestPointOnPath();
                float distanceFromPath = Vector3.Distance(transform.position, closestPointOnPath);
                if (distanceFromPath > 0.5f) // If we're more than 2 units away from our path
                {
                    // Perform recalculation.
                    //SelfMovingCartBase.mls.LogInfo($"Recalculating path (far: {distanceFromPath})...");
                    CalculatePath(finalDestination);
                    checkDistanceFromPathTimer = 0.5f;
                    pathRecalculationTimer = 5f;
                }
            }

            // Recalculate path if 5 seconds passed since last calculation.
            if (pathRecalculationTimer < 0f)
            {
                // Perform recalculation.
                //SelfMovingCartBase.mls.LogInfo("Recalculating path (5s)...");
                CalculatePath(finalDestination);
                checkDistanceFromPathTimer = 0.5f;
                pathRecalculationTimer = 5f;
            }
        }

        void CalculateCartObjectsMass()
        {
            List <PhysGrabObject> itemsInCart = new List <PhysGrabObject>();
            float mass = 0f;
            Collider[] array = Physics.OverlapBox(inCart.position, inCart.localScale / 2f, inCart.rotation);
            foreach (Collider collider in array)
            {
                // Only items and the player count.
                bool isItem = collider.gameObject.layer == LayerMask.NameToLayer("PhysGrabObject");
                bool isPlayer = collider.gameObject.layer == LayerMask.NameToLayer("Player");
                if (!isItem && !isPlayer) continue;

                // Making sure the object has a rigidbody.
                Rigidbody itemRB = collider.gameObject.GetComponentInParent<Rigidbody>();
                if (itemRB == null) continue;

                if (isItem)
                {
                    PhysGrabObject item = collider.GetComponentInParent<PhysGrabObject>();
                    if (!itemsInCart.Contains(item)) // Only count item if it's not duplicate.
                    {
                        itemsInCart.Add(item);
                        mass += itemRB.mass;
                    }
                } else
                {
                    mass += itemRB.mass;
                }
            }

            totalCartMass = mass;
        }

        void MoveCartForward()
        {
            // Make cart guide ahead of the cart.
            if (!cartGuideForward) cartGuideForward = true;
            
            // Get forward direction of the cart (ignoring Y)
            Vector3 forwardDirection = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;

            // Calculate target velocity
            float yVelocity = rb.velocity.y; // Preserve vertical velocity for gravity
            Vector3 targetVelocity = forwardDirection * cartSpeed;
            targetVelocity.y = yVelocity;

            // Apply velocity to the cart
            rb.velocity = Vector3.Lerp(rb.velocity, targetVelocity, Time.fixedDeltaTime * 2f);
        }

        void MoveCartBackward()
        {
            // Make cart guide behind the cart.
            if (cartGuideForward) cartGuideForward = false;

            // Get backward direction of the cart (ignoring Y)
            Vector3 backwardDirection = -new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;

            // Calculate target velocity (slower when going backward)
            float yVelocity = rb.velocity.y; // Preserve vertical velocity for gravity
            Vector3 targetVelocity = backwardDirection * (cartSpeed * 0.7f); // Reduce speed for reverse
            targetVelocity.y = yVelocity;

            // Apply velocity to the cart
            rb.velocity = Vector3.Lerp(rb.velocity, targetVelocity, Time.fixedDeltaTime * 2f);
        }

        void TurnCartLeft()
        {
            // Calculate current speed for rotation adjustment
            float currentSpeed = new Vector3(rb.velocity.x, 0, rb.velocity.z).magnitude;

            // Reduce rotation speed when moving faster
            float speedFactor = Mathf.Clamp01(1.0f - (currentSpeed / (cartSpeed * 1.2f)));
            float adjustedRotationSpeed = cartDriveRotationSpeed * Mathf.Lerp(0.5f, 1.0f, speedFactor);

            // Calculate target rotation
            float turnAngle = -90 * Time.fixedDeltaTime * adjustedRotationSpeed;

            // Convert to angular velocity (around y-axis)
            // Use MathF.PI / 180f to convert degrees to radians
            float turnValueY = Mathf.Clamp(Mathf.Abs(turnAngle) / 180f, 0.2f, 1f) * 15f;
            turnValueY = Mathf.Clamp(turnValueY, 0f, 4f);

            // Create angular velocity vector (rotation around Y axis)
            Vector3 angularVel = new Vector3(0, Mathf.Sign(turnAngle) * turnValueY, 0);

            // Apply angular velocity
            rb.angularVelocity = Vector3.MoveTowards(rb.angularVelocity, angularVel, turnValueY);
            rb.angularVelocity = Vector3.ClampMagnitude(rb.angularVelocity, 4f);

            // Slightly counter sideways velocity that might occur during turning
            Vector3 sideVelocity = Vector3.Project(rb.velocity, transform.right);
            rb.velocity -= sideVelocity * 0.1f * Time.fixedDeltaTime;
        }

        void TurnCartRight()
        {
            // Calculate current speed for rotation adjustment
            float currentSpeed = new Vector3(rb.velocity.x, 0, rb.velocity.z).magnitude;

            // Reduce rotation speed when moving faster
            float speedFactor = Mathf.Clamp01(1.0f - (currentSpeed / (cartSpeed * 1.2f)));
            float adjustedRotationSpeed = cartDriveRotationSpeed * Mathf.Lerp(0.5f, 1.0f, speedFactor);

            // Calculate target rotation
            float turnAngle = 90 * Time.fixedDeltaTime * adjustedRotationSpeed;

            // Convert to angular velocity (around y-axis)
            float turnValueY = Mathf.Clamp(Mathf.Abs(turnAngle) / 180f, 0.2f, 1f) * 15f;
            turnValueY = Mathf.Clamp(turnValueY, 0f, 4f);

            // Create angular velocity vector (rotation around Y axis)
            Vector3 angularVel = new Vector3(0, Mathf.Sign(turnAngle) * turnValueY, 0);

            // Apply angular velocity
            rb.angularVelocity = Vector3.MoveTowards(rb.angularVelocity, angularVel, turnValueY);
            rb.angularVelocity = Vector3.ClampMagnitude(rb.angularVelocity, 4f);

            // Slightly counter sideways velocity that might occur during turning
            Vector3 sideVelocity = Vector3.Project(rb.velocity, transform.right);
            rb.velocity -= sideVelocity * 0.1f * Time.fixedDeltaTime;
        }

        void ApplyUpwardsForceToCart()
        {
            // Calculating force from (mass+height of the next step).
            Vector3 forceAmount = Vector3.up * (totalCartMass+GetNextCartStepHeight()) * Physics.gravity.magnitude;
            rb.AddForce(forceAmount);
        }

        float GetNextCartStepHeight()
        {
            Vector3 frontPosition = transform.position + transform.forward * 1.5f;
            if(!cartGuideForward)
                frontPosition = transform.position + transform.forward * -1.5f;

            if (isCartFollowingPath)
                frontPosition = MiscHelper.GetPointTowardTarget(transform.position, pathCorners[cornerInd], 1.5f);
            frontPosition = GetNearestNavMeshPosition(frontPosition);
            cartHeightGuide.transform.position = frontPosition;

            float heightDifferenceToCart = frontPosition.y - transform.position.y;
            heightDifferenceToCart += 0.5f; // Usual cart height.

            if (heightDifferenceToCart > 0.3f)
                return 2.1f;
            else if (heightDifferenceToCart < -0.3f)
                return -2f;

            return 0f;
        }

        void HandleCartSpeed()
        {
            float cartPosDiff = GetDistanceWithoutY(transform.position, lastCartPosition);
            if (cartPosDiff < 0.075f && cartSpeed < 30f)
            {
                cartSpeed *= 1.003f;
                //SelfMovingCartBase.mls.LogInfo($"Cart speed increased to: {cartSpeed}");
            }
            else if (cartPosDiff > 0.08f)
            {
                cartSpeed /= 1.01f;
                //SelfMovingCartBase.mls.LogInfo($"Cart speed decreased to: {cartSpeed}");
            }
            //SelfMovingCartBase.mls.LogInfo($"Cart difference: {cartPosDiff} units.");
            lastCartPosition = transform.position;
        }

        // This function is sketchy.
        void FollowPath()
        {
            // Get target position
            Vector3 targetPosition = pathCorners[cornerInd];
            // Get cart position
            Vector3 cartPosition = GetNearestNavMeshPosition(transform.position);
            // Calculate direction to target
            Vector3 directionToTarget = (targetPosition - cartPosition).normalized;
            // Calculate distance to target
            float distanceToTarget = GetDistanceWithoutY(cartPosition, targetPosition);
            // Get target direction.
            Vector3 targetDirection = new Vector3(directionToTarget.x, 0f, directionToTarget.z).normalized;

            // Get forward angle.
            Vector3 cartForward = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
            float forwardTurnAngle = Vector3.Angle(cartForward, targetDirection);

            // Get backwards angle.
            Vector3 transformBackwards = transform.forward * -1f;
            Vector3 cartBackwards = new Vector3(transformBackwards.x, 0f, transformBackwards.z).normalized;
            float backwardTurnAngle = Vector3.Angle(cartBackwards, targetDirection);

            // Turning parameters
            float turnAngle;
            if(forwardTurnAngle <= backwardTurnAngle) // If it's faster to turn forward, we turn forward and move forward.
            {
                turnAngle = forwardTurnAngle;
                cartGuideForward = true;
            } else // Else, we turn and move backwards.
            {
                turnAngle = backwardTurnAngle;
                cartGuideForward = false;
            }

            // Check for upcoming corner to slow down in advance
            bool approachingCorner = false;
            float cornerSlowdownDistance = 3.0f; // Distance to start slowing down before a corner
            float cornerAngleThreshold = 45.0f; // Angle threshold to consider a turn as a corner

            // Look ahead to see if next corner requires a significant turn
            if (cornerInd < pathCorners.Count - 1 && distanceToTarget < cornerSlowdownDistance)
            {
                Vector3 nextCornerDirection = (pathCorners[cornerInd + 1] - targetPosition).normalized;
                float cornerTurnAngle = Vector3.Angle(directionToTarget, nextCornerDirection);

                if (cornerTurnAngle > cornerAngleThreshold)
                {
                    approachingCorner = true;
                    // Slow down more as we get closer to the corner
                    float cornerProximityFactor = 1.0f - (distanceToTarget / cornerSlowdownDistance);
                    // More aggressive slowdown for sharper turns
                    float cornerAngleFactor = Mathf.Clamp01(cornerTurnAngle / 90.0f);
                }
            }

            // Moving the cart
            if (distanceToTarget > cornerMinDistance)
            {
                // Calculate speed based on turn angle - slower for sharper turns
                float turnAngleSpeedFactor = Mathf.Lerp(0.3f, 1.0f, Mathf.Clamp01(1.0f - (turnAngle / 90f)));

                // Add additional speed reduction when approaching corners
                float cornerSpeedFactor = 1.0f;
                if (approachingCorner)
                {
                    // Calculate how much to slow down based on proximity to corner and corner angle
                    float cornerProximityFactor = 1.0f - (distanceToTarget / cornerSlowdownDistance);
                    float cornerAngleFactor = Mathf.Clamp01((Vector3.Angle(directionToTarget,
                                             (pathCorners[cornerInd + 1] - targetPosition).normalized)) / 90.0f);

                    // Apply stronger slowdown effect for sharp turns
                    cornerSpeedFactor = Mathf.Lerp(0.8f, 0.4f, cornerProximityFactor * cornerAngleFactor);
                }

                // Final speed multiplier combines current turn angle and upcoming corner factors
                float speedMultiplier = turnAngleSpeedFactor * cornerSpeedFactor;

                // Set cart velocity towards target
                Vector3 targetVelocity = directionToTarget * cartSpeed * speedMultiplier;
                targetVelocity.y = rb.velocity.y;

                // Smoothly transition to the target velocity
                rb.velocity = Vector3.Lerp(rb.velocity, targetVelocity, Time.fixedDeltaTime * 2f);

                // Rotate the cart to face the direction it's moving using angular velocity
                Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
                if (horizontalVelocity.magnitude > 0.1f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(horizontalVelocity, Vector3.up);

                    // Getting the cart rotation (based on whether it's moving backwards or forward)
                    float yEulerAngle = transform.rotation.eulerAngles.y;
                    if (!cartGuideForward) yEulerAngle = (transform.rotation.eulerAngles.y + 180f) % 360f;
                    Quaternion currentRotation = Quaternion.Euler(0f, yEulerAngle, 0f);

                    // Convert to angle axis
                    (targetRotation * Quaternion.Inverse(currentRotation)).ToAngleAxis(out float angle, out Vector3 axis);

                    // Adjust angle if needed
                    if (angle > 180f)
                    {
                        angle -= 360f;
                    }

                    // Apply rotation speed control here
                    float baseRotationFactor = 12f;
                    float rotationFactor = baseRotationFactor * cartRotationSpeed;

                    // Adjust turn based on angle
                    float adjustedTurnValue = Mathf.Clamp(Mathf.Abs(angle) / 180f, 0.2f, 1f) * rotationFactor;
                    adjustedTurnValue = Mathf.Clamp(adjustedTurnValue, 2f, 3f * cartRotationSpeed); // Minimum turn speed is 2, maximum is 3*2
                    adjustedTurnValue *= (1.0f - (Mathf.Clamp01(turnAngle / 180f) * 0.7f)); // Reduce for sharper turns

                    // Calculate and apply angular velocity
                    Vector3 angularVel = Mathf.PI / 180f * angle * axis.normalized * adjustedTurnValue;
                    angularVel = Vector3.ClampMagnitude(angularVel, 4f * cartRotationSpeed);
                    rb.angularVelocity = Vector3.MoveTowards(rb.angularVelocity, angularVel, adjustedTurnValue);
                    rb.angularVelocity = Vector3.ClampMagnitude(rb.angularVelocity, 4f * cartRotationSpeed);
                }
            }
            else if (cornerInd == pathCorners.Count - 1) // We've reached the target.
            {
                // Activating extraction (if it's idle)
                if (extractionPointInd != -1) if (ExtractionPointPatch.extractionStates[extractionPointInd] == ExtractionPoint.State.Idle) ExtractionPointPatch.extractionPoints[extractionPointInd].GetComponent<ExtractionPoint>().OnClick();

                // If the goal is to get inside the extraction, start a new pathfinding.
                if (isExtracting) {
                    SetPathfindingTarget(ExtractionPointPatch.extractionPoints[extractionPointInd].position, -1, false);
                    cornerMinDistance = 0.4f; // We need the cart to be exactly in the center of the extraction so it doesn't get hit.
                }
                else // Otherwise, stop the cart.
                {
                    // Gradually stop the cart
                    rb.velocity = Vector3.Lerp(rb.velocity, new Vector3(0f, 0f, 0f), Time.fixedDeltaTime * 3f);
                    rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, Vector3.zero, Time.fixedDeltaTime * 5f);

                    if (rb.velocity.magnitude < 0.1f)
                    {
                        rb.velocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                        StopPathfinding();
                        SelfMovingCartBase.mls.LogInfo($"Ending cart navigation.");
                    }
                }
            }
            else
            {
                ChangeCornerInd(cornerInd + 1);
            }
        }

        void StopPathfinding()
        {
            isCartFollowingPath = false;
            cartNavMeshPath = new NavMeshPath();
            pathCorners.Clear();
            cartSpeed = 15f;
            cornerMinDistance = 1f;

            extractionPointInd = -1;
            isExtracting = false;
        }

        public void SetPathfindingTarget(Vector3 target, int _extractionPointInd, bool _isExtracting) // Additional corners is used only in extraction.
        {
            /********** STOP LAST PATH **********/
            StopPathfinding();
            finalDestination = target;

            /********** HANDLE EXTRACTION **********/
            extractionPointInd = _extractionPointInd;
            isExtracting = _isExtracting;

            /********** CALCULATE NEW PATH **********/
            CalculatePath(target);

            // Run these after path has been calculated.
            cartSpeed = 15f;
            isCartFollowingPath = true;
            checkDistanceFromPathTimer = 0.5f;
            pathRecalculationTimer = 5f;
        }

        void CalculatePath(Vector3 target)
        {
            /********** CART NAVMESH POSITION **********/
            Vector3 cartPosition = GetNearestNavMeshPosition(transform.position);

            /********** TARGET NAVMESH POSITION **********/
            Vector3 targetPosition = GetNearestNavMeshPosition(target);

            /********** GETTING THE PATH **********/
            if (!NavMesh.CalculatePath(cartPosition, targetPosition, 1, cartNavMeshPath))
            {
                SelfMovingCartBase.mls.LogError($"Could not find path from {cartPosition} to {targetPosition}.");
                return;
            }

            // Adding the corners to the list.
            pathCorners.Clear();
            foreach (Vector3 corner in cartNavMeshPath.corners) pathCorners.Add(corner);

            ChangeCornerInd(0);
            VisualizeCorners();
        }

        void VisualizeCorners()
        {
            /********** DESTROY OLD SPHERES **********/
            foreach (GameObject cornerSphere in cornerSpheres)
            {
                Destroy( cornerSphere );
            }

            /********** CREATE NEW SPHERES FOR THE NEW PATH **********/
            cornerSpheres = new List<GameObject>();
            foreach (Vector3 corner in pathCorners)
            {
                GameObject cornerSphere = new GameObject("CornerSphere");
                cornerSphere.transform.position = corner;
                cornerSphere.transform.localScale = Vector3.one/4f;

                if (toVisualize) MiscHelper.AddSphereToGameObject(cornerSphere, 0.1f, Color.red);
                cornerSpheres.Add(cornerSphere);
            }
        }

        public Vector3 GetNearestNavMeshPosition(Vector3 origin)
        {
            float navMeshDist = 0.5f;
            float maxDist = 32f;

            NavMeshHit hit = new NavMeshHit();
            while (navMeshDist < maxDist)
            {
                if (!NavMesh.SamplePosition(origin, out hit, navMeshDist, 1))
                {
                    navMeshDist *= 2f;
                }
                else
                    break;
            }
            if (navMeshDist >= maxDist) // Finding target navmesh position failed.
            {
                //SelfMovingCartBase.mls.LogError($"Could not find navmesh position of {origin}");
                return new Vector3();
            }
            //SelfMovingCartBase.mls.LogInfo($"Found navmesh postion of {origin}");
            return hit.position;

        }

        static float GetDistanceWithoutY(Vector3 pos1, Vector3 pos2)
        {
            Vector3 pos1NoY = new Vector3(pos1.x, 0, pos1.z);
            Vector3 pos2NoY = new Vector3(pos2.x, 0, pos2.z);
            return Vector3.Distance(pos1NoY, pos2NoY);
        }

        public float GetDistanceFrom(Vector3 position)
        {
            return GetDistanceWithoutY(transform.position, position);
        }

        // Helper method to find closest point on the current path
        Vector3 GetClosestPointOnPath()
        {
            Vector3 closestPoint = transform.position;
            float minDistance = float.MaxValue;

            // Check all path segments
            for (int i = 0; i < pathCorners.Count - 1; i++)
            {
                Vector3 startPoint = pathCorners[i];
                Vector3 endPoint = pathCorners[i + 1];

                // Find closest point on this segment
                Vector3 segment = endPoint - startPoint;
                float segmentLength = segment.magnitude;
                segment.Normalize();

                Vector3 cartToStart = transform.position - startPoint;
                float projectionLength = Vector3.Dot(cartToStart, segment);
                projectionLength = Mathf.Clamp(projectionLength, 0f, segmentLength);

                Vector3 pointOnSegment = startPoint + segment * projectionLength;
                float distance = Vector3.Distance(transform.position, pointOnSegment);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestPoint = pointOnSegment;
                }
            }

            return closestPoint;
        }

        void ChangeCornerInd(int newVal)
        {
            cornerInd = newVal;
        }


        List<PhysGrabHinge> collidedUnbrokenDoors = new List<PhysGrabHinge>();
        List<float> doorsCollisionTime = new List<float>();
        float destroyDoorAfter = 2f;

        void HandleCollidedDoors()
        {
            if (!isCartFollowingPath && !remoteControlForward && !remoteControlBack && !remoteControlRight && !remoteControlLeft)
            {
                if (collidedUnbrokenDoors.Count > 0)
                {
                    collidedUnbrokenDoors.Clear();
                    doorsCollisionTime.Clear();
                }
                return;
            }

            for (int i = 0; i < collidedUnbrokenDoors.Count; i++)
            {
                doorsCollisionTime[i] += Time.deltaTime;
                if (doorsCollisionTime[i] > destroyDoorAfter)
                {
                    // Destroy door.
                    if (collidedUnbrokenDoors[i] != null)
                        collidedUnbrokenDoors[i].DestroyHinge();
                    collidedUnbrokenDoors.RemoveAt(i);
                    doorsCollisionTime.RemoveAt(i);
                }
            }
        }

        void AddCollidedDoor(GameObject doorObject)
        {
            // Check that object is a door.
            PhysGrabHinge door = doorObject.GetComponent<PhysGrabHinge>();
            if (door == null) return;

            // Check that door is not already destroyed.
            DoorTracker doorTracker = doorObject.GetComponent<DoorTracker>();
            if (doorTracker.isDestroyed) return;

            // If door is broken, destroy right away.
            if (doorTracker.isBroken)
                door.DestroyHinge();
            else
            {
                if (!collidedUnbrokenDoors.Contains(door))
                {
                    collidedUnbrokenDoors.Add(door);
                    doorsCollisionTime.Add(0);
                }
            }
        }

        void RemoveCollidedDoor(GameObject doorObject)
        {
            // Check that object is a door.
            PhysGrabHinge door = doorObject.GetComponent<PhysGrabHinge>();
            if (door == null) return;

            int doorInd = collidedUnbrokenDoors.IndexOf(door);
            if (doorInd != -1)
            {
                collidedUnbrokenDoors.RemoveAt(doorInd);
                doorsCollisionTime.RemoveAt(doorInd);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if(isCartFollowingPath || remoteControlForward || remoteControlBack || remoteControlRight || remoteControlLeft)
                AddCollidedDoor(collision.gameObject);
        }

        // You can also add these if needed
        private void OnCollisionStay(Collision collision)
        {
            if (isCartFollowingPath || remoteControlForward || remoteControlBack || remoteControlRight || remoteControlLeft)
                AddCollidedDoor(collision.gameObject);
        }

        private void OnCollisionExit(Collision collision)
        {
            RemoveCollidedDoor(collision.gameObject);
        }
    }
}
