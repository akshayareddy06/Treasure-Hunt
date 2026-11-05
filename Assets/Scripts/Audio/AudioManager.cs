using System.Collections.Generic;
using UnityEngine;

namespace TreasureHunt
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource loopSource;

        [Header("Clips - Background Music")]
        public AudioClip caveAmbienceClip;
        public AudioClip dangerThemeClip;
        public AudioClip victoryThemeClip;

        [Header("Clips - Player")]
        public AudioClip footstepClip;
        public AudioClip jumpClip;
        public AudioClip landClip;
        public AudioClip playerDeathClip;

        [Header("Clips - Special Mechanics")]
        public AudioClip trolleyLoopClip;
        public AudioClip jetLoopClip;
        public AudioClip trolleyJumpClip;
        public AudioClip trolleyTurnClip;

        [Header("Clips - Cave Wildlife (Animal Sounds)")]
        public AudioClip batFlockClip;
        public AudioClip creatureGrowlClip;
        public AudioClip caveChirpClip;

        [Header("Obstacles & Traps")]
        public AudioClip bridgeCrackClip;
        public AudioClip rockCrashClip;
        public AudioClip plateClickClip;
        public AudioClip trapSpikeClip;
        public AudioClip stoneSwingClip;

        [Header("Clips - Collectibles & Environment")]
        public AudioClip coinPickupClip;
        public AudioClip crystalPickupClip;
        public AudioClip keyPickupClip;
        public AudioClip doorGrindClip;
        public AudioClip checkpointClip;

        [Header("Clips - UI")]
        public AudioClip buttonClickClip;
        public AudioClip buttonHoverClip;

        [Header("Volume Settings")]
        [Range(0f, 1f)] public float masterVolume = 1.0f;
        [Range(0f, 1f)] public float musicVolume = 0.8f;
        [Range(0f, 1f)] public float sfxVolume = 1.0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeAudioSources();
            LoadDefaultAudioClips();

            // Auto-start continuous cave music ambience
            if (caveAmbienceClip != null)
            {
                PlayMusic(caveAmbienceClip, true);
            }
        }

        private float animalTimer = 7.0f;

        private void Update()
        {
            // Periodic authentic cave animal sounds (bats, distant creature growl, cave crickets)
            animalTimer -= Time.deltaTime;
            if (animalTimer <= 0f)
            {
                animalTimer = Random.Range(8.5f, 15.0f);
                PlayRandomCaveAnimalSound();
            }
        }

        public void PlayRandomCaveAnimalSound()
        {
            AudioClip[] animals = new AudioClip[] { batFlockClip, creatureGrowlClip, caveChirpClip };
            int validCount = 0;
            foreach (var a in animals) if (a != null) validCount++;
            if (validCount == 0) return;

            int idx = Random.Range(0, animals.Length);
            if (animals[idx] != null)
            {
                PlaySFX(animals[idx], Random.Range(0.42f, 0.62f), 0.12f);
            }
        }

        private void InitializeAudioSources()
        {
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
            }

            if (loopSource == null)
            {
                loopSource = gameObject.AddComponent<AudioSource>();
                loopSource.loop = true;
                loopSource.playOnAwake = false;
            }

            UpdateVolumes();
        }

        public void LoadDefaultAudioClips()
        {
            // Silky smooth, atmospheric 16s cave ambient pad
            caveAmbienceClip = SynthesizeSmoothCaveMusic();
            dangerThemeClip = TryLoadClip("Audio/BackgroundMusic/DangerTheme", "DangerTheme", null);
            victoryThemeClip = TryLoadClip("Audio/BackgroundMusic/VictoryTheme", "VictoryTheme", null);

            footstepClip = TryLoadClip("Audio/Player/Footstep", "Footstep", SynthesizeFootstep);
            jumpClip = TryLoadClip("Audio/Player/Jump", "Jump", SynthesizeJump);
            landClip = TryLoadClip("Audio/Player/Land", "Land", SynthesizeLand);
            playerDeathClip = TryLoadClip("Audio/Player/PlayerDeath", "PlayerDeath", null);

            coinPickupClip = TryLoadClip("Audio/Obstacles/CoinPickup", "CoinPickup", SynthesizeCoinChime);
            crystalPickupClip = TryLoadClip("Audio/Obstacles/CrystalPickup", "CrystalPickup", SynthesizeCoinChime);
            keyPickupClip = TryLoadClip("Audio/Obstacles/KeyPickup", "KeyPickup", null);
            doorGrindClip = TryLoadClip("Audio/Obstacles/DoorGrind", "DoorGrind", null);
            bridgeCrackClip = TryLoadClip("Audio/Obstacles/BridgeCrack", "BridgeCrack", null);
            rockCrashClip = TryLoadClip("Audio/Obstacles/RockCrash", "RockCrash", null);
            plateClickClip = TryLoadClip("Audio/Obstacles/PlateClick", "PlateClick", null);
            trapSpikeClip = TryLoadClip("Audio/Obstacles/TrapSpike", "TrapSpike", null);
            stoneSwingClip = TryLoadClip("Audio/Obstacles/StoneSwing", "StoneSwing", null);
            checkpointClip = TryLoadClip("Audio/Environment/Checkpoint", "Checkpoint", SynthesizeCoinChime);

            buttonClickClip = TryLoadClip("Audio/UI/ButtonClick", "ButtonClick", SynthesizeFootstep);
            buttonHoverClip = TryLoadClip("Audio/UI/ButtonHover", "ButtonHover", null);

            // Special Mechanics: Trolley Track Clatter loop & Rocket Jetpack Thruster loop
            trolleyLoopClip = SynthesizeTrolleyLoop();
            jetLoopClip = SynthesizeJetLoop();
            trolleyJumpClip = SynthesizeTrolleyJump();
            trolleyTurnClip = SynthesizeTrolleyTurn();

            // Cave Wildlife (Animal Sounds)
            batFlockClip = SynthesizeBatFlock();
            creatureGrowlClip = SynthesizeCreatureGrowl();
            caveChirpClip = SynthesizeCaveChirp();
        }

        private AudioClip TryLoadClip(string relativePath, string clipName, System.Func<AudioClip> fallbackFactory)
        {
            // 1. Try Resources
            AudioClip clip = Resources.Load<AudioClip>(relativePath);
            if (clip != null) return clip;

            // 2. Direct WAV file read from disk
            string p1 = System.IO.Path.Combine(Application.dataPath, relativePath + ".wav");
            string p2 = System.IO.Path.Combine(Application.dataPath, "Audio", relativePath.Replace("Audio/", "") + ".wav");
            string targetPath = System.IO.File.Exists(p1) ? p1 : (System.IO.File.Exists(p2) ? p2 : null);

            if (targetPath != null)
            {
                try
                {
                    byte[] bytes = System.IO.File.ReadAllBytes(targetPath);
                    clip = DecodeWav(clipName, bytes);
                    if (clip != null) return clip;
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[AudioManager] Could not decode wav at {targetPath}: {ex.Message}");
                }
            }

            // 3. Fallback procedural synthesis
            if (fallbackFactory != null)
            {
                return fallbackFactory();
            }

            return null;
        }

        private static AudioClip DecodeWav(string clipName, byte[] wavData)
        {
            if (wavData == null || wavData.Length < 44) return null;
            if (wavData[0] != 'R' || wavData[1] != 'I' || wavData[2] != 'F' || wavData[3] != 'F') return null;

            int channels = System.BitConverter.ToInt16(wavData, 22);
            int sampleRate = System.BitConverter.ToInt32(wavData, 24);
            int bitsPerSample = System.BitConverter.ToInt16(wavData, 34);

            int pos = 12;
            while (pos + 8 < wavData.Length)
            {
                string chunkId = System.Text.Encoding.ASCII.GetString(wavData, pos, 4);
                int chunkSize = System.BitConverter.ToInt32(wavData, pos + 4);
                if (chunkId == "data")
                {
                    int bytesPerSample = bitsPerSample / 8;
                    int sampleCount = chunkSize / bytesPerSample / channels;
                    float[] samples = new float[sampleCount * channels];
                    int dataStart = pos + 8;

                    if (bitsPerSample == 16)
                    {
                        for (int i = 0; i < samples.Length; i++)
                        {
                            int byteIdx = dataStart + i * 2;
                            if (byteIdx + 1 < wavData.Length)
                            {
                                short val = System.BitConverter.ToInt16(wavData, byteIdx);
                                samples[i] = val / 32768.0f;
                            }
                        }
                    }

                    AudioClip clip = AudioClip.Create(clipName, sampleCount, channels, sampleRate, false);
                    clip.SetData(samples, 0);
                    return clip;
                }
                pos += 8 + chunkSize;
            }
            return null;
        }

        // ====================================================================
        // PROCEDURAL AUDIO SYNTHESIZERS (Zero external dependency)
        // ====================================================================
        private static AudioClip SynthesizeTrolleyLoop()
        {
            int sampleRate = 44100;
            float length = 1.25f;
            int totalSamples = Mathf.FloorToInt(sampleRate * length);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                // Low mechanical track rumble
                float rumble = Mathf.Sin(2f * Mathf.PI * 68f * t) * 0.28f + Mathf.Sin(2f * Mathf.PI * 136f * t) * 0.14f;
                // Periodic rhythmic wheel rail-joint clatters (at 4.5 Hz)
                float clickPhase = (t * 4.5f) % 1.0f;
                float click = Mathf.Exp(-clickPhase * 40f) * Mathf.Sin(2f * Mathf.PI * 480f * clickPhase) * 0.55f;
                // Wheel friction high-frequency texture
                float friction = (Mathf.PerlinNoise(t * 120f, 0f) - 0.5f) * 0.18f;

                samples[i] = Mathf.Clamp(rumble + click + friction, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("TrolleyLoop_Synth", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip SynthesizeJetLoop()
        {
            int sampleRate = 44100;
            float length = 1.0f;
            int totalSamples = Mathf.FloorToInt(sampleRate * length);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                // Deep roaring combustion resonance
                float flameRoar = Mathf.Sin(2f * Mathf.PI * 82f * t + Mathf.Sin(2f * Mathf.PI * 18f * t) * 1.5f) * 0.35f;
                // High-velocity exhaust whoosh noise
                float noise = (Mathf.PerlinNoise(t * 220f, 0.5f) - 0.5f) * 0.55f;

                samples[i] = Mathf.Clamp(flameRoar + noise, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("JetLoop_Synth", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip SynthesizeTrolleyJump()
        {
            int sampleRate = 44100;
            float length = 0.30f;
            int totalSamples = Mathf.FloorToInt(sampleRate * length);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float freq = Mathf.Lerp(140f, 360f, t / length);
                float env = Mathf.Exp(-t * 12f);
                float metal = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.6f + Mathf.Sin(2f * Mathf.PI * freq * 2.8f * t) * 0.3f;
                samples[i] = metal * env;
            }

            AudioClip clip = AudioClip.Create("TrolleyJump_Synth", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip SynthesizeJump()
        {
            int sampleRate = 44100;
            float length = 0.22f;
            int totalSamples = Mathf.FloorToInt(sampleRate * length);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float freq = Mathf.Lerp(210f, 540f, t / length);
                float env = Mathf.Exp(-t * 14f);
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.7f;
            }

            AudioClip clip = AudioClip.Create("Jump_Synth", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip SynthesizeLand()
        {
            int sampleRate = 44100;
            float length = 0.18f;
            int totalSamples = Mathf.FloorToInt(sampleRate * length);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 22f);
                float thud = Mathf.Sin(2f * Mathf.PI * 95f * t) * 0.7f;
                samples[i] = thud * env;
            }

            AudioClip clip = AudioClip.Create("Land_Synth", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip SynthesizeCoinChime()
        {
            int sampleRate = 44100;
            float length = 0.45f;
            int totalSamples = Mathf.FloorToInt(sampleRate * length);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 8.5f);
                float chime = Mathf.Sin(2f * Mathf.PI * 1320f * t) * 0.65f + Mathf.Sin(2f * Mathf.PI * 2640f * t) * 0.35f;
                samples[i] = chime * env;
            }

            AudioClip clip = AudioClip.Create("Coin_Synth", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip SynthesizeFootstep()
        {
            int sampleRate = 44100;
            float length = 0.08f;
            int totalSamples = Mathf.FloorToInt(sampleRate * length);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 45f);
                float click = Mathf.Sin(2f * Mathf.PI * 320f * t) * 0.5f;
                samples[i] = click * env;
            }

            AudioClip clip = AudioClip.Create("Footstep_Synth", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip SynthesizeSmoothCaveMusic()
        {
            int sampleRate = 44100;
            float length = 16.0f;
            int totalSamples = Mathf.FloorToInt(sampleRate * length);
            float[] samples = new float[totalSamples];

            // 4 warm ambient chords: Dm -> Bb -> C -> Am (4 seconds each)
            float[][] chords = new float[][]
            {
                new float[] { 73.42f, 110.00f, 174.61f, 220.00f, 293.66f }, // Dm
                new float[] { 58.27f,  87.31f, 146.83f, 174.61f, 233.08f }, // Bb
                new float[] { 65.41f,  97.99f, 164.81f, 196.00f, 261.63f }, // C
                new float[] { 55.00f,  82.41f, 130.81f, 164.81f, 220.00f }  // Am
            };

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float chordPos = (t / 4.0f) % 4.0f;
                int c0 = Mathf.FloorToInt(chordPos);
                int c1 = (c0 + 1) % 4;
                float frac = chordPos - c0;
                // Cosine smooth crossfade between chords
                float blend = 0.5f - 0.5f * Mathf.Cos(frac * Mathf.PI);

                float signal = 0f;

                // Voices in chord 0
                for (int v = 0; v < chords[c0].Length; v++)
                {
                    float f = chords[c0][v];
                    float vibrato = Mathf.Sin(2f * Mathf.PI * 0.15f * t + v) * 0.003f;
                    float wave = Mathf.Sin(2f * Mathf.PI * f * (1f + vibrato) * t) * 0.45f
                               + Mathf.Sin(4f * Mathf.PI * f * t) * 0.12f;
                    signal += wave * (1f - blend);
                }

                // Voices in chord 1
                for (int v = 0; v < chords[c1].Length; v++)
                {
                    float f = chords[c1][v];
                    float vibrato = Mathf.Sin(2f * Mathf.PI * 0.15f * t + v) * 0.003f;
                    float wave = Mathf.Sin(2f * Mathf.PI * f * (1f + vibrato) * t) * 0.45f
                               + Mathf.Sin(4f * Mathf.PI * f * t) * 0.12f;
                    signal += wave * blend;
                }

                signal *= 0.18f; // Soft ambient volume

                // Subtle ethereal crystal shimmer
                float shimmer = Mathf.Sin(2f * Mathf.PI * 587.33f * t) * 0.025f * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 0.25f * t));
                signal += shimmer;

                // Smooth loop-boundary crossfade (last 0.3s blends into start to guarantee 0% click)
                if (t >= length - 0.3f)
                {
                    float loopBlend = (length - t) / 0.3f;
                    signal *= loopBlend;
                }
                else if (t < 0.3f)
                {
                    float loopBlend = t / 0.3f;
                    signal *= loopBlend;
                }

                samples[i] = Mathf.Clamp(signal, -0.95f, 0.95f);
            }

            AudioClip clip = AudioClip.Create("SmoothCaveAmbience", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip SynthesizeBatFlock()
        {
            int sampleRate = 44100;
            float length = 1.35f;
            int totalSamples = Mathf.FloorToInt(sampleRate * length);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Sin(t / length * Mathf.PI);

                // Flapping wings rhythm (15 flaps per second)
                float wingFlap = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 15f * t);
                float wingNoise = (Mathf.PerlinNoise(t * 85f, 0f) - 0.5f) * wingFlap * 0.45f;

                // High-frequency ultrasonic bat echolocation chirps (at t=0.2, 0.6, 0.9, 1.15)
                float chirps = 0f;
                float[] chirpTimes = new float[] { 0.20f, 0.58f, 0.88f, 1.15f };
                foreach (float ct in chirpTimes)
                {
                    float dt = t - ct;
                    if (dt >= 0f && dt < 0.045f)
                    {
                        float chirpEnv = Mathf.Exp(-dt * 65f);
                        float chirpFreq = Mathf.Lerp(4800f, 3200f, dt / 0.045f);
                        chirps += Mathf.Sin(2f * Mathf.PI * chirpFreq * dt) * chirpEnv * 0.4f;
                    }
                }

                samples[i] = (wingNoise + chirps) * env;
            }

            AudioClip clip = AudioClip.Create("BatFlock_Synth", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip SynthesizeCreatureGrowl()
        {
            int sampleRate = 44100;
            float length = 2.2f;
            int totalSamples = Mathf.FloorToInt(sampleRate * length);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Sin(t / length * Mathf.PI);

                // Pitch envelope: low growl that swells and drops (72Hz -> 96Hz -> 58Hz)
                float pitch = 72f + Mathf.Sin(t / length * Mathf.PI) * 24f;
                // Throat roughness
                float throatMod = 1.0f + Mathf.Sin(2f * Mathf.PI * 26f * t) * 0.4f;

                float growl = Mathf.Sin(2f * Mathf.PI * pitch * throatMod * t) * 0.55f
                            + Mathf.Sin(4f * Mathf.PI * pitch * t) * 0.25f
                            + (Mathf.PerlinNoise(t * 40f, 0f) - 0.5f) * 0.25f;

                // Cavern echo tap at +0.22s
                float echo = (t > 0.22f) ? Mathf.Sin(2f * Mathf.PI * pitch * (t - 0.22f)) * 0.2f : 0f;

                samples[i] = (growl + echo) * env * 0.65f;
            }

            AudioClip clip = AudioClip.Create("CreatureGrowl_Synth", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip SynthesizeCaveChirp()
        {
            int sampleRate = 44100;
            float length = 0.85f;
            int totalSamples = Mathf.FloorToInt(sampleRate * length);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float clickSignal = 0f;
                float[] clickTimes = new float[] { 0.12f, 0.16f, 0.40f, 0.44f, 0.65f };
                foreach (float ct in clickTimes)
                {
                    float dt = t - ct;
                    if (dt >= 0f && dt < 0.025f)
                    {
                        float clickEnv = Mathf.Exp(-dt * 120f);
                        clickSignal += Mathf.Sin(2f * Mathf.PI * 2400f * dt) * clickEnv * 0.5f;
                    }
                }
                samples[i] = clickSignal;
            }

            AudioClip clip = AudioClip.Create("CaveChirp_Synth", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip SynthesizeTrolleyTurn()
        {
            int sampleRate = 44100;
            float length = 0.75f;
            int totalSamples = Mathf.FloorToInt(sampleRate * length);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Sin(t / length * Mathf.PI);
                float metal1 = Mathf.Sin(2f * Mathf.PI * 1850f * t + Mathf.Sin(2f * Mathf.PI * 35f * t) * 1.5f) * 0.45f;
                float metal2 = Mathf.Sin(2f * Mathf.PI * 2420f * t) * 0.25f;
                float noise = (Mathf.PerlinNoise(t * 300f, 0f) - 0.5f) * 0.35f;
                samples[i] = (metal1 + metal2 + noise) * env * 0.75f;
            }

            AudioClip clip = AudioClip.Create("TrolleyTurn_Synth", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        // ====================================================================
        // MOVEMENT AUDIO LOOPS (Trolley & Jetpack)
        // ====================================================================
        public void StartTrolleyLoop()
        {
            if (loopSource == null || trolleyLoopClip == null) return;
            if (loopSource.clip == trolleyLoopClip && loopSource.isPlaying) return;
            loopSource.clip = trolleyLoopClip;
            loopSource.loop = true;
            loopSource.volume = 0.85f * sfxVolume * masterVolume;
            loopSource.Play();
        }

        public void StopTrolleyLoop()
        {
            if (loopSource != null && loopSource.clip == trolleyLoopClip)
            {
                loopSource.Stop();
            }
        }

        public void StartJetLoop()
        {
            if (loopSource == null || jetLoopClip == null) return;
            if (loopSource.clip == jetLoopClip && loopSource.isPlaying) return;
            loopSource.clip = jetLoopClip;
            loopSource.loop = true;
            loopSource.volume = 0.90f * sfxVolume * masterVolume;
            loopSource.Play();
        }

        public void StopJetLoop()
        {
            if (loopSource != null && loopSource.clip == jetLoopClip)
            {
                loopSource.Stop();
            }
        }

        public void StopMovementLoops()
        {
            if (loopSource != null) loopSource.Stop();
        }

        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (clip == null) return;
            if (musicSource.clip == clip && musicSource.isPlaying) return;

            musicSource.clip = clip;
            musicSource.loop = loop;
            musicSource.volume = musicVolume * masterVolume;
            musicSource.Play();
        }

        public void StopMusic()
        {
            if (musicSource != null)
                musicSource.Stop();
        }

        public void PlaySFX(AudioClip clip, float volumeScale = 1.0f, float pitchVariation = 0.05f)
        {
            if (clip == null || sfxSource == null) return;

            float originalPitch = sfxSource.pitch;
            if (pitchVariation > 0f)
            {
                sfxSource.pitch = 1.0f + Random.Range(-pitchVariation, pitchVariation);
            }

            sfxSource.PlayOneShot(clip, volumeScale * sfxVolume * masterVolume);
            sfxSource.pitch = originalPitch;
        }

        public void PlaySFXAt(AudioClip clip, Vector3 position, float volumeScale = 1.0f, float maxDistance = 35f)
        {
            if (clip == null) return;
            GameObject temp = new GameObject("TempSFX_" + clip.name);
            temp.transform.position = position;
            AudioSource source = temp.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = volumeScale * sfxVolume * masterVolume;
            source.spatialBlend = 1.0f; // 3D sound
            source.minDistance = 2f;
            source.maxDistance = maxDistance;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.Play();
            Destroy(temp, clip.length + 0.2f);
        }

        public void SetMasterVolume(float vol)
        {
            masterVolume = Mathf.Clamp01(vol);
            UpdateVolumes();
        }

        public void SetMusicVolume(float vol)
        {
            musicVolume = Mathf.Clamp01(vol);
            UpdateVolumes();
        }

        public void SetSFXVolume(float vol)
        {
            sfxVolume = Mathf.Clamp01(vol);
            UpdateVolumes();
        }

        private void UpdateVolumes()
        {
            if (musicSource != null)
                musicSource.volume = musicVolume * masterVolume;
            if (sfxSource != null)
                sfxSource.volume = sfxVolume * masterVolume;
            if (loopSource != null)
                loopSource.volume = sfxVolume * masterVolume * 0.85f;
        }
    }
}
