using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Aircraft
{
    public class AircraftPlayer : AircraftAgent
    {
        [SerializeField] private InputAction pitchInput;
        [SerializeField] private InputAction yawInput;
        [SerializeField] private InputAction pauseInput;

        public override void InitializeAgent()
        {
            base.InitializeAgent();
            pitchInput.Enable();
            yawInput.Enable();
            pauseInput.Enable();
        }


        public override float[] Heuristic()
        {
            // Pitch: 1 == up, 0 == none, -1 == down
            float pitchValue = Mathf.Round(pitchInput.ReadValue<float>());

            // Yaw: 1 == turn right, 0 == none, -1 == turn left
            float yawValue = Mathf.Round(yawInput.ReadValue<float>());

            // convert -1 (down) to discrete value 2
            if (pitchValue == -1f) pitchValue = 2f;

            // convert -1 (turn left) to discrete value 2
            if (yawValue == -1f) yawValue = 2f;

            return new float[] { pitchValue, yawValue };
        }

        private void OnDestroy()
        {
            pitchInput.Disable();
            yawInput.Disable();
            pauseInput.Disable();
        }
    }
}
