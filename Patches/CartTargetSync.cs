using Photon.Pun;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SelfMovingCart.Patches
{
    class CartTargetSync : MonoBehaviourPun
    {
        CartSelfMovementManager cart;

        void Start()
        {
            cart = GetComponent<CartSelfMovementManager>();
        }

        public void GoToTarget(int type, Vector3 playerPosition)
        {
            if (SemiFunc.IsMasterClientOrSingleplayer()) // Call function directly.
                GoToTargetRPC(type, playerPosition);
            else // Call RPC
                base.photonView.RPC("GoToTargetRPC", RpcTarget.MasterClient, type, playerPosition);
        }

        [PunRPC]
        void GoToTargetRPC(int type, Vector3 playerPosition) // type: 0: player, 1: ship, 2: near extraction, 3: inside extraction.
        {
            if (type == 0) // Player
                cart.SetPathfindingTarget(playerPosition, -1, false);
            else if (type == 1) // Ship
                cart.SetPathfindingTarget(TruckHealerPatch.truckPosition, -1, false);
            else // Extraction
            {
                Vector3 extractionFrontPosition = new Vector3();
                int nearestExtractionInd = 0;
                float nearestExtractionDist = Mathf.Infinity;
                bool isAnExtractionAvailable = false;

                for (int i = 0; i < ExtractionPointPatch.extractionPoints.Count; i++)
                {
                    Transform extractionPoint = ExtractionPointPatch.extractionPoints[i];
                    ExtractionPoint.State extractionState = ExtractionPointPatch.extractionStates[i];

                    SelfMovingCartBase.mls.LogInfo($"Extraction state: {extractionState}, rotation: {extractionPoint.rotation}");
                    if (extractionState == ExtractionPoint.State.Active)
                    {
                        // Getting the position in front of extraction.
                        extractionFrontPosition = GetExtractionFrontPosition(extractionPoint.position, extractionPoint.rotation);
                        cart.SetPathfindingTarget(extractionFrontPosition, i, type == 3);
                        return;
                    }

                    // Keeping record of the nearest extraction regardless of state in case none of the extractions are active.
                    if (extractionState != ExtractionPoint.State.Complete)
                    {
                        float dist = Vector3.Distance(extractionPoint.position, playerPosition);
                        if (dist < nearestExtractionDist)
                        {
                            nearestExtractionInd = i;
                            nearestExtractionDist = dist;
                            isAnExtractionAvailable = true;
                        }
                    }
                }

                // If none of the extractions are active, go to nearest one and activate it.
                if (isAnExtractionAvailable)
                {
                    Transform extractionPoint = ExtractionPointPatch.extractionPoints[nearestExtractionInd];
                    extractionFrontPosition = GetExtractionFrontPosition(extractionPoint.position, extractionPoint.rotation);
                    cart.SetPathfindingTarget(extractionFrontPosition, nearestExtractionInd, type == 3);
                }
            }
        }

        Vector3 GetExtractionFrontPosition(Vector3 position, Quaternion rotation)
        {
            Vector3 extractionFrontPosition = new Vector3(position.x, position.y, position.z);
            Vector3 positionAddition = Vector3.zero;
            float threshold = 0.1f;
            float distFromExtraction = 4f;

            if (Mathf.Abs(rotation.y - 0.7071f) < threshold)
            {
                if (Mathf.Abs(rotation.w - 0.7071f) < threshold) // (0.00000, 0.70711, 0.00000, 0.70711)
                {
                    positionAddition = new Vector3(distFromExtraction, 0f, 0f);
                }
                else // (0.00000, 0.70711, 0.00000, -0.70711)
                {
                    positionAddition = new Vector3(-distFromExtraction, 0f, 0f);
                }
            }
            else if (Mathf.Abs(rotation.y + 0.7071f) < threshold)
            {
                if (Mathf.Abs(rotation.w - 0.7071f) < threshold) // (0.00000, -0.70711, 0.00000, 0.70711)
                {
                    positionAddition = new Vector3(-distFromExtraction, 0f, 0f);
                }
                else // (0.00000, -0.70711, 0.00000, -0.70711)
                {
                    positionAddition = new Vector3(distFromExtraction, 0f, 0f);
                }
            }
            else if (Mathf.Abs(rotation.y - 1f) < threshold || Mathf.Abs(rotation.y + 1f) < threshold) // (0.00000, 1.00000, 0.00000, 0.00000) OR (0.00000, -1.00000, 0.00000, 0.00000)
            {
                positionAddition = new Vector3(0f, 0f, -distFromExtraction);
            }
            else if (Mathf.Abs(rotation.y) < threshold) // (0.00000, 0.00000, 0.00000, 1.00000) OR (0.00000, 0.00000, 0.00000, -1.00000)
            {
                positionAddition = new Vector3(0f, 0f, distFromExtraction);
            }

            return extractionFrontPosition + positionAddition;
        }

        public void MoveCart(bool moveForward, bool moveBack, bool turnRight, bool turnLeft)
        {
            if (SemiFunc.IsMasterClientOrSingleplayer()) // Call function directly.
                MoveCartRPC(moveForward, moveBack, turnRight, turnLeft);
            else // Call RPC
                base.photonView.RPC("MoveCartRPC", RpcTarget.MasterClient, moveForward, moveBack, turnRight, turnLeft);
        }

        [PunRPC]
        void MoveCartRPC(bool moveForward, bool moveBack, bool turnRight, bool turnLeft)
        {
            cart.remoteControlForward = moveForward;
            cart.remoteControlBack = moveBack;
            cart.remoteControlRight = turnRight;
            cart.remoteControlLeft = turnLeft;
        }
    }
}
