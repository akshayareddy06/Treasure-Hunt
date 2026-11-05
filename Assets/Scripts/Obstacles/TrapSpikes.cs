using System.Collections;
using UnityEngine;

namespace TreasureHunt
{
    public class TrapSpikes : MonoBehaviour
    {
        [Header("Spike Settings")]
        public Transform spikesTransform;
        public Vector3 extendedOffset = new Vector3(0, 1.2f, 0);
        public float extendSpeed = 16.0f;
        public float retractSpeed = 3.0f;
        public float holdDuration = 1.2f;
        public int damage = 1;

        private Vector3 retractedPos;
        private Vector3 extendedPos;
        private bool isExtended = false;
        public bool IsExtended => isExtended;
        private Collider spikeCollider;

        private void Awake()
        {
            if (spikesTransform == null) spikesTransform = transform.Find("Spikes");
            if (spikesTransform == null) spikesTransform = transform;

            retractedPos = spikesTransform.localPosition;
            extendedPos = retractedPos + extendedOffset;

            spikeCollider = GetComponentInChildren<Collider>();
            if (spikeCollider != null) spikeCollider.isTrigger = true;
        }

        public void FireTrap()
        {
            if (isExtended) return;
            StartCoroutine(TrapSequence());
        }

        private IEnumerator TrapSequence()
        {
            isExtended = true;

            // Trap fire SFX
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFXAt(AudioManager.Instance.trapSpikeClip, transform.position, 1.0f);
            }

            // Rapid extension
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * extendSpeed;
                spikesTransform.localPosition = Vector3.Lerp(retractedPos, extendedPos, t);
                yield return null;
            }
            spikesTransform.localPosition = extendedPos;

            // Check hit in area
            CheckDamagePlayer();

            yield return new WaitForSeconds(holdDuration);

            // Slower retraction
            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * retractSpeed;
                spikesTransform.localPosition = Vector3.Lerp(extendedPos, retractedPos, t);
                yield return null;
            }
            spikesTransform.localPosition = retractedPos;

            isExtended = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isExtended) return;

            PlayerController pc = other.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.TriggerDeath();
            }
            else
            {
                HealthManager health = other.GetComponent<HealthManager>();
                if (health != null) health.TakeDamage(999, Vector3.up * 10f);
            }
        }

        private void CheckDamagePlayer()
        {
            Collider[] hits = Physics.OverlapBox(spikesTransform.position, Vector3.one * 1.0f);
            foreach (Collider hit in hits)
            {
                PlayerController pc = hit.GetComponent<PlayerController>();
                if (pc != null)
                {
                    pc.TriggerDeath();
                }
                else
                {
                    HealthManager health = hit.GetComponent<HealthManager>();
                    if (health != null) health.TakeDamage(999, Vector3.up * 10f);
                }
            }
        }

        public void ResetTrap()
        {
            StopAllCoroutines();
            isExtended = false;
            if (spikesTransform != null) spikesTransform.localPosition = retractedPos;
        }
    }
}
