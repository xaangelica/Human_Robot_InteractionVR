using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AvatarTrial
{
    public string avatarID;       // e.g., "Robot_15" or "Human_03"
    public string avatarType;     // "Robot" or "Human"
    public string resourcePath;   // "Avatars/Robot/Robot_15"
}

public class TaskManager : MonoBehaviour
{
[Header("UI Panels")]
    public GameObject participantIDPanel;        // Panel para ingresar ID
    public TMP_InputField participantInputField; // Campo de texto TMP
    public Button btnConfirmID;                  // Botón continuar
    public GameObject instructionsCanvas;        // Panel de instrucciones
    public GameObject questionnaireCanvas;       // Canvas de la escala VAS
    
    [Header("Avatar Spawning")]
    public Transform avatarSpawnPoint;

    [Header("Trial Timing Settings")]
    [Tooltip("Duración en milisegundos (ej: 4000 = 4s)")]
    public float avatarDisplayDurationMs = 4000f;
    
   [Header("VAS Controller")]
    public CustomVASController vasController;

    // Trial queue & state
    private List<AvatarTrial> trialList = new List<AvatarTrial>();
    private int currentTrialIndex = 0;
    private GameObject currentSpawnedAvatar;

   void Start()
    {
        InitializeTrialList();

        // 1. Mostrar únicamente la pantalla de ID al inicio
        if (participantIDPanel != null) participantIDPanel.SetActive(true);
        if (instructionsCanvas != null) instructionsCanvas.SetActive(false);
        if (questionnaireCanvas != null) questionnaireCanvas.SetActive(false);

        // 2. Conectar el botón de confirmación de ID
        if (btnConfirmID != null)
        {
            btnConfirmID.onClick.RemoveAllListeners();
            btnConfirmID.onClick.AddListener(OnConfirmParticipantID);
        }
    }

    public void OnConfirmParticipantID()
    {
        string idIngresado = (participantInputField != null && !string.IsNullOrEmpty(participantInputField.text)) 
                             ? participantInputField.text 
                             : "P01";

        // Inicializa el CSV con el nombre de este participante
        if (vasController != null)
        {
            vasController.InitializeParticipantSession(idIngresado);
        }

        // Apaga la pantalla de ID y muestra las instrucciones
        if (participantIDPanel != null) participantIDPanel.SetActive(false);
        if (instructionsCanvas != null) instructionsCanvas.SetActive(true);
    }

    void InitializeTrialList()
    {
        // MODO PRUEBA: Usamos tu unico robot para probar el flujo de 5 ensayos
        // Asegurate de tener un prefab en: Assets/Resources/TestAvatar.prefab
        for (int i = 1; i <= 5; i++)
        {
            trialList.Add(new AvatarTrial {
                avatarID = $"TestRobot_{i}",
                avatarType = "Robot",
                resourcePath = "TestAvatar" // Carga Assets/Resources/TestAvatar.prefab
            });
        }

        // Si prefieres no aleatorizar en pruebas, puedes comentar Shuffle:
        // Shuffle(trialList);
    }

   // {
        // Add 60 Robots
       // for (int i = 1; i <= 60; i++)
      //  {
           // trialList.Add(new AvatarTrial {
          //      avatarID = $"Robot_{i}",
          //      avatarType = "Robot",
           //     resourcePath = $"Avatars/Robot/Robot_{i}"
           // });
      //  }

        // Add 60 Humans
       // for (int i = 1; i <= 60; i++)
      //  {
           // trialList.Add(new AvatarTrial {
              //  avatarID = $"Human_{i}",
               // avatarType = "Human",
               // resourcePath = $"Avatars/Human/Human_{i}"
           // });
       // }

        // Optional: Shuffle the 120 trials so Robots and Humans appear in random order
      //  Shuffle(trialList);
   // }

    public void StartExperiment()
    {
        instructionsCanvas.SetActive(false);
        LoadNextTrial();
    }

    public void LoadNextTrial()

    {
        // 1. Si ya se completaron los ensayos
        if (currentTrialIndex >= trialList.Count)
        {
            Debug.Log("Experiment Complete! All trials finished.");
            if (questionnaireCanvas != null) questionnaireCanvas.SetActive(false);
            return;
        }

        // 2. Inicia la secuencia de avatar -> tiempo -> cuestionario
        StartCoroutine(TrialSequenceRoutine());
    }

  private IEnumerator TrialSequenceRoutine()
    {
        // A. Ocultar escala VAS mientras observa el avatar
        if (questionnaireCanvas != null)
        {
            questionnaireCanvas.SetActive(false);
        }

        // B. Destruir avatar previo y limpiar memoria
        if (currentSpawnedAvatar != null)
        {
            Destroy(currentSpawnedAvatar);
            Resources.UnloadUnusedAssets();
        }

        // C. Cargar y spawnear el nuevo avatar
        AvatarTrial currentTrial = trialList[currentTrialIndex];
        GameObject avatarPrefab = Resources.Load<GameObject>(currentTrial.resourcePath);

        if (avatarPrefab != null)
        {
            currentSpawnedAvatar = Instantiate(avatarPrefab, avatarSpawnPoint.position, avatarSpawnPoint.rotation);
        }
        else
        {
            Debug.LogError($"Avatar not found at path: {currentTrial.resourcePath}");
        }

        // D. ESPERA EN MILISEGUNDOS (ej. 4000ms = 4s)
        yield return new WaitForSeconds(avatarDisplayDurationMs / 1000f);

        // E. Destruir el avatar tras completarse el tiempo
        if (currentSpawnedAvatar != null)
        {
            Destroy(currentSpawnedAvatar);
        }

        // F. Encender la escala VAS
        if (questionnaireCanvas != null)
        {
            questionnaireCanvas.SetActive(true);
        }

        // G. Iniciar el cuestionario
        if (vasController != null)
        {
            vasController.StartVASSequence(currentTrial.avatarID, currentTrial.avatarType);
        }
    } // ⬅️ Llave que cierra TrialSequenceRoutine

    public void OnTwoVASQuestionsCompleted()
    {
        currentTrialIndex++;
        LoadNextTrial();
    }

    // Devuelve el ID del avatar actual para guardarlo en el CSV
    public string GetCurrentAvatarID()
    {
        if (currentTrialIndex < trialList.Count)
        {
            return trialList[currentTrialIndex].avatarID;
        }
        return "Unknown_Avatar";
    }

    private string currentAvatarID;
    private string currentAvatarType;

    public void SetCurrentAvatarInfo(string id, string type)
    {
        currentAvatarID = id;
        currentAvatarType = type;
    }

    // Fisher-Yates shuffle algorithm for randomized presentation
    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}