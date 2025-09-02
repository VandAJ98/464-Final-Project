using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Aircraft
{
    public class AircraftArea : MonoBehaviour
    {
        [SerializeField] private CinemachineSmoothPath racePath;
        [SerializeField] private GameObject checkpointPrefab;
        [SerializeField] private GameObject finishCheckpointPrefab;
        public bool trainingMode;
        public List<AircraftAgent> AircraftAgents { get; private set; }
        public List<GameObject> Checkpoints { get; private set; }
        public AircraftAcademy AircraftAcademy { get; private set; }

        private void Awake()
        {
            AircraftAgents = transform.GetComponentsInChildren<AircraftAgent>().ToList();

            AircraftAcademy = FindObjectOfType<AircraftAcademy>();
        }

        private void Start()
        {
            //create checkpoint loop
            Checkpoints = new List<GameObject>();
            int numCheckpoints = (int)racePath.MaxUnit(CinemachinePathBase.PositionUnits.PathUnits);
            for (int i = 0; i < numCheckpoints; i++)
            {

                GameObject checkpoint;
                if (i == numCheckpoints - 1) checkpoint = Instantiate<GameObject>(finishCheckpointPrefab);
                else checkpoint = Instantiate<GameObject>(checkpointPrefab);

                checkpoint.transform.SetParent(racePath.transform);
                checkpoint.transform.localPosition = racePath.m_Waypoints[i].position;
                checkpoint.transform.rotation = racePath.EvaluateOrientationAtUnit(i, CinemachinePathBase.PositionUnits.PathUnits);
                Checkpoints.Add(checkpoint);
            }
        }
        public void ResetAgentPosition(AircraftAgent agent)
        {
            int previousCheckpointIndex = agent.NextCheckpointIndex - 1;
            if (previousCheckpointIndex == -1)
                previousCheckpointIndex = Checkpoints.Count - 1;

            float startPosition = racePath.FromPathNativeUnits(previousCheckpointIndex, CinemachinePathBase.PositionUnits.PathUnits);
            Vector3 basePosition = racePath.EvaluatePosition(startPosition);

            Quaternion orientation = racePath.EvaluateOrientation(startPosition);

            Vector3 positionOffset = Vector3.right * (AircraftAgents.IndexOf(agent) - AircraftAgents.Count / 2f) * UnityEngine.Random.Range(9f, 10f);
            agent.transform.position = basePosition + orientation * positionOffset;
            agent.transform.rotation = orientation;
        }
    }
}
