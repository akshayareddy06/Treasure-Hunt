using UnityEngine;

namespace TreasureHunt
{
    public enum TurnDirection
    {
        Left,
        Right
    }

    [RequireComponent(typeof(BoxCollider))]
    public class TurnTrigger : MonoBehaviour
    {
        [Header("Turn Properties")]
        public TurnDirection direction = TurnDirection.Left;
        public float targetYaw = 0f;
        public Vector3 cornerPosition = Vector3.zero;
        public bool hasTurned = false;

        private void Reset()
        {
            BoxCollider col = GetComponent<BoxCollider>();
            if (col != null)
            {
                col.isTrigger = true;
                col.size = new Vector3(8.0f, 4.0f, 14.0f);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasTurned) return;

            PlayerController pc = other.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.activeTurnTrigger = this;

                EndlessHUD hud = Object.FindAnyObjectByType<EndlessHUD>();
                if (hud != null)
                {
                    hud.ShowTurnPrompt(direction);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            PlayerController pc = other.GetComponent<PlayerController>();
            if (pc != null && pc.activeTurnTrigger == this)
            {
                pc.activeTurnTrigger = null;

                EndlessHUD hud = Object.FindAnyObjectByType<EndlessHUD>();
                if (hud != null)
                {
                    hud.HideTurnPrompt();
                }
            }
        }
    }
}
