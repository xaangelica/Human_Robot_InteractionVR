using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CustomVASController : MonoBehaviour
{
    [Header("Referencias Visuales de tu Panel VAS")]
    public GameObject vasPanelRoot;             // 'spatial panel scroll (nueva vaz scale)'
    public TextMeshProUGUI questionTitleText;   
    public TextMeshProUGUI leftAnchorLabel;     
    public TextMeshProUGUI rightAnchorLabel;    
    public Button[] ratingButtons;              

    [Header("Conexión con TaskManager")]
    public TaskManager taskManager;

    [System.Serializable]
    public class VASQuestion
    {
        [TextArea] public string questionText;
        public string leftLabel;
        public string rightLabel;
    }

    [Header("Configuración de Preguntas")]
    public List<VASQuestion> trialQuestions = new List<VASQuestion>()
    {
        new VASQuestion { questionText = "¿Qué tan agradable es la cara?", leftLabel = "Muy desagradable", rightLabel = "Muy agradable" },
        new VASQuestion { questionText = "¿Qué tan robótica se ve esta cara?", leftLabel = "Nada robótica / Muy humana", rightLabel = "Totalmente robótica" }
    };

    private int currentQuestionStep = 0;
    private float questionStartTime;
    private string participantId = "P00";
    private string currentAvatarId = "Unknown";
    private string currentAvatarType = "Unknown";
    private int responseQ1 = -1;
    private float responseTimeQ1 = 0f;
    private int responseQ2 = -1;
    private float responseTimeQ2 = 0f;
    private bool isProcessingAnswer = false;

    private string csvFilePath;

    private void Start()
    {
        SetupButtonListeners();
    }

    /// <summary>
    /// Llamado desde TaskManager cuando se ingresa el ID del participante
    /// </summary>
    public void InitializeParticipantSession(string id)
    {
        participantId = string.IsNullOrEmpty(id) ? "Participante_Anonimo" : id.Trim();

        string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        string targetFolder = Path.Combine(desktopPath, "RespuestasVR");

        if (!Directory.Exists(targetFolder))
        {
            Directory.CreateDirectory(targetFolder);
        }

        string sessionTime = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        // El nombre del archivo ahora incluye el ID del participante
        csvFilePath = Path.Combine(targetFolder, $"{participantId}_Resultados_VAS_{sessionTime}.csv");

        Debug.Log($"<color=cyan>[VAS Setup] Archivo para {participantId} en: {csvFilePath}</color>");

        InitializeCSV();
    }

    private void InitializeCSV()
    {
        try
        {
            if (!File.Exists(csvFilePath))
            {
                using (var stream = new FileStream(csvFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                using (var writer = new StreamWriter(stream, Encoding.UTF8))
                {
                    writer.Write("ParticipantID,AvatarID,AvatarType,Q1_Agradable_Score,Q1_ReactionTime_s,Q2_Robotica_Score,Q2_ReactionTime_s,Timestamp\n");
                }
                Debug.Log("<color=green>[VAS Setup] Archivo CSV inicializado.</color>");
            }
        }
        catch (IOException ex)
        {
            Debug.LogWarning($"[VAS Advertencia] No se pudo inicializar CSV: {ex.Message}");
        }
    }

    private void SetupButtonListeners()
    {
        if (ratingButtons == null || ratingButtons.Length == 0) return;

        for (int i = 0; i < ratingButtons.Length; i++)
        {
            if (ratingButtons[i] != null)
            {
                int score = i + 1;
                ratingButtons[i].onClick.RemoveAllListeners();
                ratingButtons[i].onClick.AddListener(() => OnScoreSelected(score));
            }
        }
    }

    public void StartVASSequence(string avatarId, string avatarType)
    {
        currentAvatarId = avatarId;
        currentAvatarType = avatarType;
        currentQuestionStep = 0;
        isProcessingAnswer = false;

        if (vasPanelRoot != null) vasPanelRoot.SetActive(true);
        DisplayCurrentQuestion();
    }

    private void DisplayCurrentQuestion()
    {
        if (currentQuestionStep < trialQuestions.Count)
        {
            VASQuestion q = trialQuestions[currentQuestionStep];
            if (questionTitleText != null) questionTitleText.text = q.questionText;
            if (leftAnchorLabel != null) leftAnchorLabel.text = q.leftLabel;
            if (rightAnchorLabel != null) rightAnchorLabel.text = q.rightLabel;

            questionStartTime = Time.time;
        }
    }

    public void OnScoreSelected(int score)
    {
        if (isProcessingAnswer || currentQuestionStep > 1) return;

        isProcessingAnswer = true;
        float reactionTime = Time.time - questionStartTime;

        if (currentQuestionStep == 0)
        {
            responseQ1 = score;
            responseTimeQ1 = reactionTime;

            currentQuestionStep = 1;
            DisplayCurrentQuestion();
            isProcessingAnswer = false;
        }
        else if (currentQuestionStep == 1)
        {
            responseQ2 = score;
            responseTimeQ2 = reactionTime;
            currentQuestionStep = 2;

            if (vasPanelRoot != null) vasPanelRoot.SetActive(false);

            SaveTrialDataToCSV();

            if (taskManager != null)
            {
                taskManager.OnTwoVASQuestionsCompleted();
            }
        }
    }

    private void SaveTrialDataToCSV()
    {
        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        string row = $"{participantId},{currentAvatarId},{currentAvatarType},{responseQ1},{responseTimeQ1:F2},{responseQ2},{responseTimeQ2:F2},{timestamp}\n";

        try
        {
            using (var stream = new FileStream(csvFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
            {
                writer.Write(row);
            }
            Debug.Log($"<color=lime><b>[CSV Guardado]:</b> {row}</color>");
        }
        catch (IOException ex)
        {
            Debug.LogError($"[ERROR CSV]: {ex.Message}");
        }
    }
}