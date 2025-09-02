using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Aircraft
{
    public class Rotate : MonoBehaviour
    {
        [SerializeField] private Vector3 rotateSpeed;
        [SerializeField] private bool randomize = false;

        private void Start()
        {
            if (randomize) transform.Rotate(rotateSpeed.normalized * UnityEngine.Random.Range(0f, 360f));
        }

        void Update()
        {
            transform.Rotate(rotateSpeed * Time.deltaTime, Space.Self);
        }
    }
}
