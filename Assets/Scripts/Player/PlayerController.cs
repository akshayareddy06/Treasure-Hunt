using UnityEngine;

namespace TreasureHunt
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Endless Auto-Runner Settings")]
        public bool autoRun = true;
        public float baseForwardSpeed = 10.0f;
        public float maxForwardSpeed = 16.0f;
        public float steeringSpeed = 11.5f;
        public float pathWidthLimit = 3.2f;

        [Header("Jumping & Gravity")]
        public float jumpHeight = 2.0f;
        public float gravity = 25.0f;

        [Header("Sliding (Duck under obstacles)")]
        public float slideDuration = 0.85f;
        private float slideTimer = 0f;
        private bool isSliding = false;

        [Header("Special Vehicles & Mechanics (L2 Jetpack & L3 Trolley)")]
        public bool isFlyingJetpack = false;
        public float jetpackTimer = 0f;
        public float targetFlightHeight = 5.2f;
        public bool isInTrolley = false;
        public TurnTrigger activeTurnTrigger = null;
        private Quaternion targetHeadingRotation = Quaternion.identity;

        [Header("References")]
        public Transform cameraTransform;
        public PlayerVisuals visuals;

        private CharacterController controller;
        private Vector3 verticalVelocity = Vector3.zero;
        private bool isGrounded;
        private bool wasGroundedLastFrame;
        private float stepTimer = 0f;
        private Vector3 knockbackVelocity = Vector3.zero;
        private bool isDead = false;
        private float currentSteerInput = 0f;
        private float distanceTraveled = 0f;
        private float currentForwardSpeed = 10.0f;

        // Mouse drag / swipe steering
        private bool isMouseDown = false;
        private Vector3 lastMousePos;

        public bool IsDead => isDead;
        public float DistanceTraveled => distanceTraveled;
        public float CurrentSpeed => currentForwardSpeed;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (visuals == null)
            {
                visuals = GetComponent<PlayerVisuals>();
                if (visuals == null)
                    visuals = gameObject.AddComponent<PlayerVisuals>();
            }

            controller.center = new Vector3(0, 0.95f, 0);
            controller.radius = 0.35f;
            controller.height = 1.85f;
        }

        private void Start()
        {
            currentForwardSpeed = baseForwardSpeed;
            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
        }

        private void Update()
        {
            if (isDead)
            {
                if (visuals != null)
                    visuals.Animate(0f, false, false, true, 0f, false);
                return;
            }

            // Fall-off chasm death check
            if (transform.position.y < -4.5f)
            {
                TriggerDeath();
                return;
            }

            HandleInput();
            HandleMovement();
            HandleAudioAndAnimation();
        }

        private void HandleInput()
        {
            // 0. Corner Turn Input: Turn Left / Turn Right at 90-degree junctions
            if (activeTurnTrigger != null && !activeTurnTrigger.hasTurned)
            {
                if (activeTurnTrigger.direction == TurnDirection.Left)
                {
                    if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow) || currentSteerInput < -0.4f)
                    {
                        ExecuteTurn(activeTurnTrigger);
                    }
                }
                else if (activeTurnTrigger.direction == TurnDirection.Right)
                {
                    if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow) || currentSteerInput > 0.4f)
                    {
                        ExecuteTurn(activeTurnTrigger);
                    }
                }
            }

            // 1. Keyboard Steering: A/D or Left/Right arrows
            float horizontal = Input.GetAxisRaw("Horizontal");

            // 2. Mouse Drag / Swipe Steering
            if (Input.GetMouseButtonDown(0))
            {
                isMouseDown = true;
                lastMousePos = Input.mousePosition;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                isMouseDown = false;
            }

            if (isMouseDown)
            {
                float deltaX = (Input.mousePosition.x - lastMousePos.x) / Screen.width;
                if (Mathf.Abs(deltaX) > 0.002f)
                {
                    horizontal += Mathf.Clamp(deltaX * 35.0f, -1f, 1f);

                    // Swipe turn support
                    if (activeTurnTrigger != null && !activeTurnTrigger.hasTurned)
                    {
                        if (deltaX < -0.03f && activeTurnTrigger.direction == TurnDirection.Left)
                            ExecuteTurn(activeTurnTrigger);
                        else if (deltaX > 0.03f && activeTurnTrigger.direction == TurnDirection.Right)
                            ExecuteTurn(activeTurnTrigger);
                    }
                }
                lastMousePos = Input.mousePosition;
            }

            currentSteerInput = Mathf.Lerp(currentSteerInput, horizontal, Time.deltaTime * 18.0f);

            // 3. Jump: Space, W, or Up Arrow
            if ((isGrounded || isFlyingJetpack) && (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)))
            {
                Jump();
            }

            // 4. Slide: S, Down Arrow, or Left Ctrl
            if (isGrounded && (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.LeftControl)))
            {
                StartSlide();
            }

            if (isSliding)
            {
                slideTimer -= Time.deltaTime;
                if (slideTimer <= 0f)
                {
                    EndSlide();
                }
            }
        }

        private void Jump()
        {
            verticalVelocity.y = Mathf.Sqrt(2f * jumpHeight * gravity);
            isSliding = false;
            controller.height = 1.85f;
            controller.center = new Vector3(0, 0.95f, 0);

            if (AudioManager.Instance != null)
            {
                if (isInTrolley && AudioManager.Instance.trolleyJumpClip != null)
                {
                    AudioManager.Instance.PlaySFX(AudioManager.Instance.trolleyJumpClip, 1.0f);
                }
                else if (AudioManager.Instance.jumpClip != null)
                {
                    AudioManager.Instance.PlaySFX(AudioManager.Instance.jumpClip, 0.9f);
                }
            }
        }

        public void ExecuteTurn(TurnTrigger trigger)
        {
            if (trigger == null || trigger.hasTurned) return;
            trigger.hasTurned = true;

            targetHeadingRotation = Quaternion.Euler(0, trigger.targetYaw, 0);
            transform.rotation = targetHeadingRotation;

            // Align along corner
            if (trigger.cornerPosition != Vector3.zero)
            {
                Vector3 p = transform.position;
                Vector3 toCorner = trigger.cornerPosition - p;
                p += Vector3.Project(toCorner, transform.right) * 0.7f;
                transform.position = p;
            }

            if (AudioManager.Instance != null)
            {
                if (isInTrolley && AudioManager.Instance.trolleyTurnClip != null)
                {
                    AudioManager.Instance.PlaySFX(AudioManager.Instance.trolleyTurnClip, 0.9f);
                }
                else if (AudioManager.Instance.footstepClip != null)
                {
                    AudioManager.Instance.PlaySFX(AudioManager.Instance.footstepClip, 0.9f);
                }
            }

            activeTurnTrigger = null;
            EndlessHUD hud = Object.FindAnyObjectByType<EndlessHUD>();
            if (hud != null) hud.HideTurnPrompt();

            Debug.Log($"[PlayerController] Executed 90° Turn to Yaw {trigger.targetYaw}°!");
        }

        public void ActivateJetpack(float duration = 7.5f)
        {
            isFlyingJetpack = true;
            jetpackTimer = duration;
            isInTrolley = false;

            if (visuals != null)
            {
                visuals.SetJetpackVisible(true);
                visuals.SetTrolleyVisible(false);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopTrolleyLoop();
                AudioManager.Instance.StartJetLoop();
            }

            Debug.Log($"[PlayerController] Rocket Jetpack ACTIVATED for {duration:F1}s!");
        }

        public void DeactivateJetpack()
        {
            if (!isFlyingJetpack) return;
            isFlyingJetpack = false;
            jetpackTimer = 0f;

            if (visuals != null)
            {
                visuals.SetJetpackVisible(false);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopJetLoop();
            }

            EndlessHUD hud = Object.FindAnyObjectByType<EndlessHUD>();
            if (hud != null)
            {
                hud.ShowLevelNotification("JETPACK DESCENT", "THRUSTERS OFF - RUNNING ON FOOT!", 2.8f);
            }

            Debug.Log("[PlayerController] Rocket Jetpack Deactivated - back on foot.");
        }

        public void ActivateTrolley()
        {
            isInTrolley = true;
            isFlyingJetpack = false;

            if (visuals != null)
            {
                visuals.SetTrolleyVisible(true);
                visuals.SetJetpackVisible(false);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopJetLoop();
                AudioManager.Instance.StartTrolleyLoop();
            }

            Debug.Log("[PlayerController] Mining Trolley ACTIVATED on railway tracks!");
        }

        public void DeactivateTrolley()
        {
            if (!isInTrolley) return;
            isInTrolley = false;

            if (visuals != null)
            {
                visuals.SetTrolleyVisible(false);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopTrolleyLoop();
            }

            Debug.Log("[PlayerController] Mining Trolley Deactivated.");
        }

        private void StartSlide()
        {
            isSliding = true;
            slideTimer = slideDuration;
            controller.height = 0.95f;
            controller.center = new Vector3(0, 0.48f, 0);

            if (AudioManager.Instance != null && AudioManager.Instance.footstepClip != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.footstepClip, 0.7f);
            }
        }

        private void EndSlide()
        {
            isSliding = false;
            controller.height = 1.85f;
            controller.center = new Vector3(0, 0.95f, 0);
        }

        private void HandleMovement()
        {
            // Smoothly rotate toward current heading
            transform.rotation = Quaternion.Slerp(transform.rotation, targetHeadingRotation, Time.deltaTime * 14.0f);

            isGrounded = controller.isGrounded;

            if (!wasGroundedLastFrame && isGrounded)
            {
                if (AudioManager.Instance != null && AudioManager.Instance.landClip != null)
                {
                    AudioManager.Instance.PlaySFX(AudioManager.Instance.landClip, 0.7f);
                }
            }
            wasGroundedLastFrame = isGrounded;

            float speedBonus = Mathf.Min((distanceTraveled / 120.0f) * 0.5f, maxForwardSpeed - baseForwardSpeed);
            currentForwardSpeed = autoRun ? (baseForwardSpeed + speedBonus) : 0f;

            Vector3 forwardMove = transform.forward * currentForwardSpeed;
            Vector3 steerMove = transform.right * (currentSteerInput * steeringSpeed);
            Vector3 horizontalMove = forwardMove + steerMove;

            // Handle Jetpack flight altitude
            if (isFlyingJetpack)
            {
                jetpackTimer -= Time.deltaTime;
                // Last 2.5s: smooth descent from sky flight height (5.2m) down to ground running height (0.5m)
                float targetY = (jetpackTimer < 2.5f) ? Mathf.Lerp(0.5f, targetFlightHeight, Mathf.Clamp01(jetpackTimer / 2.5f)) : targetFlightHeight;
                verticalVelocity.y = (targetY - transform.position.y) * 5.0f;

                if (jetpackTimer <= 0f && transform.position.y <= 0.85f)
                {
                    DeactivateJetpack();
                }
            }
            else
            {
                if (isGrounded && verticalVelocity.y < 0)
                {
                    verticalVelocity.y = -2.0f;
                }
                verticalVelocity.y -= gravity * Time.deltaTime;
            }

            if (knockbackVelocity.magnitude > 0.05f)
            {
                knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, Time.deltaTime * 6f);
            }

            Vector3 finalMove = (horizontalMove + knockbackVelocity) * Time.deltaTime + verticalVelocity * Time.deltaTime;
            controller.Move(finalMove);

            // Accumulate distance seamlessly across all headings!
            distanceTraveled += currentForwardSpeed * Time.deltaTime;
        }

        private void HandleAudioAndAnimation()
        {
            if (visuals != null)
            {
                visuals.Animate(currentForwardSpeed, isGrounded, currentForwardSpeed > 11.5f, isDead, currentSteerInput, isSliding, flying: isFlyingJetpack, inTrolley: isInTrolley);
            }

            if (isFlyingJetpack)
            {
                if (AudioManager.Instance != null) AudioManager.Instance.StartJetLoop();
            }
            else if (isInTrolley)
            {
                if (isGrounded && autoRun && AudioManager.Instance != null)
                {
                    AudioManager.Instance.StartTrolleyLoop();
                }
            }
            else if (isGrounded && autoRun && !isSliding)
            {
                stepTimer += Time.deltaTime * (currentForwardSpeed * 0.55f);
                if (stepTimer >= 1.0f)
                {
                    stepTimer = 0f;
                    PlayFootstepSound();
                }
            }
        }

        private void PlayFootstepSound()
        {
            if (AudioManager.Instance != null && AudioManager.Instance.footstepClip != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.footstepClip, 0.35f);
            }
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (isDead) return;

            string hitName = hit.gameObject.name.ToLower();
            string hitTag = hit.gameObject.tag.ToLower();

            // Ignore safe structural and environmental geometry:
            // Floors, walls, ceilings, arches, pillars, torches, crystals, tracks, signs, etc.
            if (hitName.Contains("floor") || hitName.Contains("wall") || hitName.Contains("ceiling") ||
                hitName.Contains("ground") || hitName.Contains("tile") || hitName.Contains("torch") ||
                hitName.Contains("pressureplate") || hitName.Contains("crystal") || hitName.Contains("runway") ||
                hitName.Contains("track") || hitName.Contains("rail") || hitName.Contains("sleeper") ||
                hitName.Contains("sign") || hitName.Contains("post") || hitName.Contains("arch") ||
                hitName.Contains("pillar") || hitName.Contains("beam") || hitName.Contains("stalactite") ||
                hitName.Contains("stalagmite") || hitName.Contains("rod"))
            {
                return;
            }

            // Only trigger death on genuine lethal hazards:
            // 1. Fallen boulder on ground ("RockBoulder")
            // 2. Swinging stone pendulum bob ("StoneBob")
            // 3. Extended trap spikes ("TrapSpike" only if extended!)
            // 4. Solid objects explicitly tagged "Hazard" or "Obstacle"
            bool isLethalSpike = hitName.Contains("spike") &&
                                 hit.gameObject.GetComponentInParent<TrapSpikes>() != null &&
                                 hit.gameObject.GetComponentInParent<TrapSpikes>().IsExtended;

            bool isLethalBoulder = hitName.Contains("boulder") || hitName == "rockboulder";
            bool isLethalStoneBob = hitName.Contains("stonebob");

            if (isLethalBoulder || isLethalStoneBob || isLethalSpike || hitTag == "hazard" || hitTag == "obstacle")
            {
                Debug.Log($"[PlayerController] Player dashed into lethal obstacle: {hit.gameObject.name}! Triggering instant death.");
                TriggerDeath();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isDead) return;

            string hitName = other.gameObject.name.ToLower();
            string hitTag = other.gameObject.tag.ToLower();

            // All helper triggers, sensors, coins, crystals, checkpoints, and turn zones MUST NEVER kill the player!
            if (other.isTrigger)
            {
                if (hitName.Contains("sensor") || hitName.Contains("trigger") || hitName.Contains("helper") ||
                    hitName.Contains("detector") || hitName.Contains("coin") || hitName.Contains("crystal") ||
                    hitName.Contains("turn") || hitName.Contains("checkpoint") ||
                    (hitName.Contains("zone") && !hitName.Contains("killzone")) ||
                    hitName.Contains("descent") || hitName.Contains("audio") || hitName.Contains("rock") ||
                    hitName.Contains("plate"))
                {
                    return;
                }
            }

            // Lethal trigger: KillZone (falling into abyss/pit)
            if (hitName.Contains("killzone") || hitTag == "killzone" || other.GetComponent<KillZone>() != null)
            {
                Debug.Log($"[PlayerController] Player triggered killzone: {other.gameObject.name}! Triggering death.");
                TriggerDeath();
            }
        }

        public void ApplyKnockback(Vector3 force)
        {
            knockbackVelocity = force;
        }

        public void SetDead(bool dead)
        {
            isDead = dead;
            if (isDead)
            {
                TriggerDeath();
            }
        }

        public void TeleportTo(Vector3 position, Quaternion rotation)
        {
            if (controller != null) controller.enabled = false;
            transform.position = position;
            transform.rotation = rotation;
            if (controller != null) controller.enabled = true;
            verticalVelocity = Vector3.zero;
            knockbackVelocity = Vector3.zero;
        }

        public void ResetPlayer()
        {
            isDead = false;
            distanceTraveled = 0f;
            currentForwardSpeed = baseForwardSpeed;
            verticalVelocity = Vector3.zero;
            knockbackVelocity = Vector3.zero;
            isSliding = false;

            DeactivateJetpack();
            DeactivateTrolley();
            if (AudioManager.Instance != null) AudioManager.Instance.StopMovementLoops();

            targetHeadingRotation = Quaternion.identity;
            activeTurnTrigger = null;

            TeleportTo(new Vector3(0, 0.5f, 0), Quaternion.identity);

            if (controller != null)
            {
                controller.height = 1.85f;
                controller.center = new Vector3(0, 0.95f, 0);
            }

            if (visuals != null)
            {
                visuals.ResetPose();
            }

            autoRun = true;
        }

        public void TriggerDeath()
        {
            if (isDead) return;
            isDead = true;

            int bestDist = PlayerPrefs.GetInt("BestRunDistance", 0);
            if ((int)distanceTraveled > bestDist)
            {
                PlayerPrefs.SetInt("BestRunDistance", (int)distanceTraveled);
                PlayerPrefs.Save();
            }

            if (AudioManager.Instance != null && AudioManager.Instance.playerDeathClip != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.playerDeathClip, 1.0f);
            }

            EndlessHUD hud = Object.FindAnyObjectByType<EndlessHUD>();
            if (hud != null)
            {
                hud.ShowGameOver((int)distanceTraveled);
            }
            else if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowGameOver();
            }
        }
    }
}
