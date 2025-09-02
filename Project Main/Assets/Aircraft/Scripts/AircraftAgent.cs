using MLAgents;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Aircraft
{
    public class AircraftAgent : Agent
    {
        [SerializeField] protected float thrust = 100000f;
        [SerializeField] protected float pitchSpeed = 100f;
        [SerializeField] protected float yawSpeed = 100f;
        [SerializeField] protected float rollSpeed = 100f;
        [SerializeField] protected GameObject meshObject;
        [SerializeField] protected GameObject explosionEffect;
        [SerializeField] protected int stepTimeout = 1000;
        public int NextCheckpointIndex { get; set; }

        private AircraftArea area;
        new private Rigidbody rigidbody;
        private RayPerception3D rayPerception;
        private float nextStepTimeout;
        private bool frozen = false;

        private float pitchChange = 0f;
        private float smoothPitchChange = 0f;
        private float maxPitchAngle = 45f;
        private float yawChange = 0f;
        private float smoothYawChange = 0f;
        private float rollChange = 0f;
        private float smoothRollChange = 0f;
        private float maxRollAngle = 45f;
        //control

        public override void InitializeAgent()
        {
            base.InitializeAgent();
            area = GetComponentInParent<AircraftArea>();
            rigidbody = GetComponent<Rigidbody>();
            rayPerception = GetComponent<RayPerception3D>();  // assets/ml-agents/examples/sharedassets/scripts/rayperception2d.cs
            agentParameters.maxStep = area.trainingMode ? 10000 : 0; //max step if training
        }

        public override void AgentAction(float[] vectorAction, string textAction)
        {
            pitchChange = vectorAction[0]; //vertical index branch 1
            if (pitchChange == 2)
                pitchChange = -1f;
            yawChange = vectorAction[1]; //horizontal index branch 2
            if (yawChange == 2)
                yawChange = -1f;

            if (frozen)
                return;

            ProcessMovement();

            if (area.trainingMode)
            {
                AddReward(-1f / agentParameters.maxStep);
                if (GetStepCount() > nextStepTimeout)
                {
                    AddReward(-.5f);
                    Done();
                }

                Vector3 localCheckpointDir = VectorToNextCheckpoint();
                if (localCheckpointDir.magnitude < area.AircraftAcademy.resetParameters["checkpoint_radius"])
                {
                    GotCheckpoint();
                }
            }
        }

        public override void CollectObservations()
        {
            AddVectorObs(transform.InverseTransformDirection(rigidbody.velocity));  //check velocity vector3 = 3 values
            AddVectorObs(VectorToNextCheckpoint());  //check checkpoint vector3 = 3 values
            Vector3 nextCheckpointForward = area.Checkpoints[NextCheckpointIndex].transform.forward; //orient to next checkpoint vector3 = 3 values
            AddVectorObs(transform.InverseTransformDirection(nextCheckpointForward));

            string[] detectableObjects = { "Untagged", "checkpoint" };

            //forward and above
            //(2 tags + 1 hit/not + 1 distance to obj) * 3 ray angles = 12 values
            AddVectorObs(rayPerception.Perceive(
                rayDistance: 250f,
                rayAngles: new float[] { 60f, 90f, 120f },
                detectableObjects: detectableObjects,
                startOffset: 0f,
                endOffset: 75f
            ));
            //center and angles on horizon
            //(2 tags + 1 hit/not + 1 distance to obj) * 7 ray angles = 28 values
            AddVectorObs(rayPerception.Perceive(
                rayDistance: 250f,
                rayAngles: new float[] { 60f, 70f, 80f, 90f, 100f, 110f, 120f },
                detectableObjects: detectableObjects,
                startOffset: 0f,
                endOffset: 0f
            ));
            //forward and down
            //(2 tags + 1 hit/not + 1 distance to obj) * 3 ray angles = 12 values
            AddVectorObs(rayPerception.Perceive(
                rayDistance: 250f,
                rayAngles: new float[] { 60f, 90f, 120f },
                detectableObjects: detectableObjects,
                startOffset: 0f,
                endOffset: -75f
            ));

            // Total Observations = 3 + 3 + 3 + 12 + 28 + 12 = 61
        }

        public override void AgentReset()
        {
            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
            area.ResetAgentPosition(agent: this);

            if (area.trainingMode) nextStepTimeout = GetStepCount() + stepTimeout;
        }

        public void FreezeAgent()
        {
            frozen = true;
            rigidbody.Sleep();
        }

        public void ThawAgent()
        {
            frozen = false;
            rigidbody.WakeUp();
        }

        private void GotCheckpoint()
        {
            NextCheckpointIndex = (NextCheckpointIndex + 1) % area.Checkpoints.Count;

            if (area.trainingMode)
            {
                AddReward(.5f);
                nextStepTimeout = GetStepCount() + stepTimeout;
            }
        }

        private Vector3 VectorToNextCheckpoint()
        {
            Vector3 nextCheckpointDir = area.Checkpoints[NextCheckpointIndex].transform.position - transform.position;
            Vector3 localCheckpointDir = transform.InverseTransformDirection(nextCheckpointDir);
            return localCheckpointDir;
        }


        private void ProcessMovement()
        {
            rigidbody.AddForce(transform.forward * thrust, ForceMode.Force); //thrust
            Vector3 curRot = transform.rotation.eulerAngles; //current rot

            float rollAngle = curRot.z > 180f ? curRot.z - 360f : curRot.z;
            if (yawChange == 0f)
            {
                rollChange = -rollAngle / maxRollAngle;
            }
            else
            {
                rollChange = -yawChange;
            }


            smoothPitchChange = Mathf.MoveTowards(smoothPitchChange, pitchChange, 2f * Time.fixedDeltaTime);
            smoothYawChange = Mathf.MoveTowards(smoothYawChange, yawChange, 2f * Time.fixedDeltaTime);
            smoothRollChange = Mathf.MoveTowards(smoothRollChange, rollChange, 2f * Time.fixedDeltaTime);
            float pitch = ClampAngle(curRot.x + smoothPitchChange * Time.fixedDeltaTime * pitchSpeed,
                                        -maxPitchAngle,
                                        maxPitchAngle);
            float yaw = curRot.y + smoothYawChange * Time.fixedDeltaTime * yawSpeed;
            float roll = ClampAngle(curRot.z + smoothRollChange * Time.fixedDeltaTime * rollSpeed,
                                    -maxRollAngle,
                                    maxRollAngle);
            transform.rotation = Quaternion.Euler(pitch, yaw, roll);
        }

        private static float ClampAngle(float angle, float from, float to)
        {
            if (angle < 0f) angle = 360f + angle;
            if (angle > 180f) return Mathf.Max(angle, 360f + from);
            return Mathf.Min(angle, to);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.transform.CompareTag("checkpoint") &&
                other.gameObject == area.Checkpoints[NextCheckpointIndex])
            {
                GotCheckpoint();
            }
        }
        private void OnCollisionEnter(Collision collision)
        {
            if (!collision.transform.CompareTag("agent"))
            {

                if (area.trainingMode)
                {
                    AddReward(-1f);
                    Done();
                    return;
                }
                else
                {
                    StartCoroutine(ExplosionReset());
                }
            }
        }

        private IEnumerator ExplosionReset()
        {
            FreezeAgent();

            meshObject.SetActive(false);
            explosionEffect.SetActive(true);
            yield return new WaitForSeconds(2f);

            meshObject.SetActive(true);
            explosionEffect.SetActive(false);
            area.ResetAgentPosition(agent: this);
            yield return new WaitForSeconds(1f);

            ThawAgent();
        }
    }
}
