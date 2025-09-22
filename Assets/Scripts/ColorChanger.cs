using UnityEngine;
using UnityEngine.UI;

namespace EaseDev.Systems
{
    /// <summary>
    /// Controls cube color changes between red and blue
    /// </summary>
    public class ColorChanger : MonoBehaviour
    {
        [Header("Materials")]
        [SerializeField] private Material redMaterial;
        [SerializeField] private Material blueMaterial;

        [Header("Target Objects")]
        [SerializeField] private GameObject targetCube;
        [SerializeField] private Button changeColorButton;

        private Renderer cubeRenderer;
        private bool isRed = true;

        private void Awake()
        {
            // Find cube if not assigned
            if (targetCube == null)
            {
                targetCube = GameObject.Find("TestCube");
            }

            // Find button if not assigned
            if (changeColorButton == null)
            {
                changeColorButton = FindObjectOfType<Button>();
            }

            // Get cube renderer
            if (targetCube != null)
            {
                cubeRenderer = targetCube.GetComponent<Renderer>();
            }

            // Create materials if they don't exist
            CreateMaterialsIfNeeded();

            // Setup initial state
            SetupInitialState();
        }

        private void Start()
        {
            // Connect button click event
            if (changeColorButton != null)
            {
                changeColorButton.onClick.AddListener(ChangeColor);
            }
        }

        /// <summary>
        /// Changes cube color between red and blue
        /// </summary>
        public void ChangeColor()
        {
            if (cubeRenderer == null) return;

            if (isRed)
            {
                // Change to blue
                cubeRenderer.material = blueMaterial;
                isRed = false;
                Debug.Log("[ColorChanger] Changed cube color to blue");
            }
            else
            {
                // Change to red
                cubeRenderer.material = redMaterial;
                isRed = true;
                Debug.Log("[ColorChanger] Changed cube color to red");
            }
        }

        private void CreateMaterialsIfNeeded()
        {
            // Create red material
            if (redMaterial == null)
            {
                redMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                redMaterial.name = "RedMaterial";
                redMaterial.color = Color.red;
            }

            // Create blue material
            if (blueMaterial == null)
            {
                blueMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                blueMaterial.name = "BlueMaterial";
                blueMaterial.color = Color.blue;
            }
        }

        private void SetupInitialState()
        {
            // Set initial red color
            if (cubeRenderer != null && redMaterial != null)
            {
                cubeRenderer.material = redMaterial;
                isRed = true;
                Debug.Log("[ColorChanger] Set initial cube color to red");
            }
        }

        private void OnValidate()
        {
            // Auto-find objects in editor
            if (targetCube == null)
            {
                targetCube = GameObject.Find("TestCube");
            }

            if (changeColorButton == null)
            {
                changeColorButton = FindObjectOfType<Button>();
            }
        }
    }
}