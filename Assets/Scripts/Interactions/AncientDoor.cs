using System.Collections;
using UnityEngine;

namespace TreasureHunt
{
    public class AncientDoor : MonoBehaviour
    {
        public string requiredKeyID = "CaveKey_Level3";
        public Transform doorSlab;
        public float openHeight = 4.5f;
        public float openSpeed = 2.0f;
        public float interactionDistance = 3.5f;

        private bool isOpen = false;
        private bool isOpening = false;
        private Vector3 closedPos;
        private Vector3 openPos;
        private Transform playerTransform;

        private void Awake()
        {
            if (doorSlab == null) doorSlab = transform.Find("DoorSlab");
            if (doorSlab == null) doorSlab = transform;

            closedPos = doorSlab.localPosition;
            openPos = closedPos + new Vector3(0, openHeight, 0);
        }

        private void Update()
        {
            if (isOpen || isOpening) return;

            if (playerTransform == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) playerTransform = player.transform;
            }

            if (playerTransform != null)
            {
                float dist = Vector3.Distance(transform.position, playerTransform.position);
                if (dist <= interactionDistance)
                {
                    bool hasKey = GameManager.Instance != null && GameManager.Instance.HasKey(requiredKeyID);
                    if (hasKey)
                    {
                        if (UIManager.Instance != null)
                            UIManager.Instance.ShowPrompt("Press [E] to Unlock Door");

                        if (Input.GetKeyDown(KeyCode.E))
                        {
                            OpenDoor();
                        }
                    }
                    else
                    {
                        if (UIManager.Instance != null)
                            UIManager.Instance.ShowPrompt("Locked Door: Requires Ancient Key");
                    }
                }
            }
        }

        public void OpenDoor()
        {
            if (isOpen || isOpening) return;
            StartCoroutine(OpenRoutine());
        }

        private IEnumerator OpenRoutine()
        {
            isOpening = true;

            if (UIManager.Instance != null)
                UIManager.Instance.HidePrompt();

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFXAt(AudioManager.Instance.doorGrindClip, transform.position, 1.0f);

            // Shaking before rising
            float shakeElapsed = 0f;
            while (shakeElapsed < 0.4f)
            {
                shakeElapsed += Time.deltaTime;
                float shake = Mathf.Sin(shakeElapsed * 40f) * 0.04f;
                doorSlab.localPosition = closedPos + new Vector3(shake, 0, 0);
                yield return null;
            }

            // Slide upwards smoothly
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * openSpeed;
                doorSlab.localPosition = Vector3.Lerp(closedPos, openPos, Mathf.SmoothStep(0, 1, t));
                yield return null;
            }

            doorSlab.localPosition = openPos;
            isOpening = false;
            isOpen = true;
        }
    }
}
