using UnityEngine;

namespace TreasureHunt
{
    public class KillZone : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            PlayerController pc = other.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.TriggerDeath();
            }
            else
            {
                HealthManager health = other.GetComponent<HealthManager>();
                if (health != null) health.InstantKill();
            }
        }
    }
}
