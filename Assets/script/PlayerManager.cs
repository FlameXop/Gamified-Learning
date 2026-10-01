using UnityEngine;

namespace FlamexStudios.Player
{
    [RequireComponent(typeof(PlayerInputHandler))]
    [RequireComponent(typeof(PlayerMovement))]
    public class PlayerManager : MonoBehaviour
    {
        public static PlayerManager Instance { get; private set; }

        public PlayerInputHandler Input { get; private set; }
        public PlayerMovement Movement { get; private set; }
        public Animator Animator { get; private set; }

        [Header("Visuals")]
        public GameObject characterModelRoot; // Drag "Ch07_nonPBR" here!
        private SkinnedMeshRenderer[] allMeshes;

        public void SetMeshVisibility(bool isVisible)
        {
            if (allMeshes == null) return;

            foreach (SkinnedMeshRenderer mesh in allMeshes)
            {
                if (mesh != null)
                {
                    mesh.enabled = isVisible;
                }
            }
        }

        private void Awake()
        {
            // Scalable Singleton Implementation
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Initialize dependencies
            Input = GetComponent<PlayerInputHandler>();
            Movement = GetComponent<PlayerMovement>();
            Animator = GetComponentInChildren<Animator>();

            // NEW: Added (true) so it finds absolutely every body part!
            if (characterModelRoot != null)
            {
                allMeshes = characterModelRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            }
        }
    }
}