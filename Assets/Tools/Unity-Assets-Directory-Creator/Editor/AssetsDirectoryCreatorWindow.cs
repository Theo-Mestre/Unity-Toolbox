using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class AssetsDirectoryCreatorWindow : EditorWindow
{
    #region Settings
    private AssetsCreatorSettings settings = null;
    private string folderName = "NewFeature";

    private bool ReloadDomainOnSave = true;

    // Folder
    private Dictionary<string, bool> folderSelection = new();
    private readonly List<string> customFolders = new();

    private readonly List<string> materialsNames = new();

    // Scripts
    private class ScriptsData
    {
        public ScriptTemplate template;
        public List<string> names;
        public bool isSelected;
    }
    private readonly List<ScriptsData> scriptsDatas = new();

    // The Scroll Position of the window
    Vector2 scrollPosition = Vector2.zero;
    #endregion

    #region EditorWindow Methods
    [MenuItem("Tools/Assets Directory Creator")]
    public static void ShowWindow()
    {
        GetWindow<AssetsDirectoryCreatorWindow>("Assets Directory Creator");
    }

    private void CreateGUI()
    {
        LoadSettings();

        LoadScriptTemplates();
    }

    private void OnGUI()
    {
        GUILayout.Label("Assets Directory Creator", EditorStyles.boldLabel);

        scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true);

        // Enable drag and drop for settings and reload settings if changed     
        if (RenderSettingsObjectField())
        {
            GUILayout.EndScrollView();
            return;
        }

        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));

        // Input for feature name
        folderName = EditorGUILayout.TextField("Feature Name", folderName);
        GUILayout.Space(10);

        // Checkbox for each default folder template
        RenderLayout("Select Subfolders to Create", RenderFolderCreationMenu);

        // Option to add custom subfolders
        RenderLayout("Custom Subfolders", RenderCustomFolderCreationMenu);

        // Display script templates and allow user to customize class names
        if (folderSelection.ContainsKey("Scripts") && folderSelection["Scripts"])
        {
            RenderLayout("Script Templates", RenderScriptCreationMenu);
        }

        // Create Materials
        if (folderSelection.ContainsKey("Materials"))
        {
            RenderLayout("Materials", RenderMaterialCreation, folderSelection["Materials"]);
        }

        GUILayout.EndScrollView();

        ReloadDomainOnSave = EditorGUILayout.Toggle("Reload Domain on Save", ReloadDomainOnSave);

        // Button to create the directory structure and scripts
        if (GUILayout.Button("Create Folders and Scripts"))
        {
            CreateFeatureFoldersAndScripts();
        }
    }
    #endregion

    #region Loading Methods
    /// <summary>
    /// Load the default AssetsCreatorSettings from Resources/AssetsCreatorSettings/ folder
    /// </summary>
    void LoadSettings()
    {
        // Load default settings from Resources folder
        settings = Resources.Load<AssetsCreatorSettings>("AssetsCreatorSettings/AssetsCreatorSettings");

        if (settings == null)
        {
            Debug.LogError("AssetsCreatorSettings not found! Please create one in Resources/AssetsCreatorSettings folder!");
            return;
        }

        if (settings.DefaultFolder.Count == 0)
        {
            Debug.LogError("Default folders not found in AssetsCreatorSettings! Please add some default folders!");
            return;
        }

        if (settings.DefaultFolder.Count != folderSelection.Count)
        {
            folderSelection = new Dictionary<string, bool>();
            foreach (string folder in settings.DefaultFolder)
            {
                folderSelection.Add(folder, false);
            }
        }
    }

    /// <summary>
    /// Load all script templates from Resources/ScriptTemplates folder
    /// </summary>
    private void LoadScriptTemplates()
    {
        // Load all script templates from Resources folder
        var templates = Resources.LoadAll<ScriptTemplate>("ScriptTemplates").ToList();

        foreach (var template in templates)
        {
            scriptsDatas.Add(new ScriptsData
            {
                template = template,
                names = new List<string>(),
                isSelected = false
            });
        }
    }

    /// <summary>
    /// Reset all data to default values
    /// </summary>
    private void ResetData()
    {
        folderName = "NewFeature";

        foreach (string key in folderSelection.Keys.ToList())
        {
            folderSelection[key] = false;
        }

        foreach (var scriptData in scriptsDatas)
        {
            scriptData.isSelected = false;
            scriptData.names = new List<string>();
        }

        customFolders.Clear();
        materialsNames.Clear();
        scrollPosition = Vector2.zero;
    }
    #endregion

    #region Rendering Methods
    /// <summary>
    /// Add a label and call renderContent with a space of 10
    /// </summary>  
    void RenderLayout(string label, System.Action renderContent, bool isShow = true)
    {
        if (isShow == false) return;

        GUILayout.Label(label, EditorStyles.boldLabel);
        renderContent();
        GUILayout.Space(10);
    }

    bool RenderSettingsObjectField()
    {
        GUILayout.BeginHorizontal();

        GUILayoutOption[] options =
        {
            GUILayout.Width(150),
            GUILayout.ExpandHeight(true),
            GUILayout.MaxHeight(20)
        };

        if (GUILayout.Button("Reset", options)) ResetData();

        AssetsCreatorSettings tempSettings = EditorGUILayout.ObjectField("", settings, typeof(AssetsCreatorSettings), false, options) as AssetsCreatorSettings;

        if (tempSettings != settings)
        {
            settings = tempSettings;
            LoadSettings();
        }

        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        return settings == null;
    }

    void RenderFolderCreationMenu()
    {
        if (folderSelection == null)
        {
            Debug.LogError("Folder Selection is null!");
            return;
        }

        foreach (string key in folderSelection.Keys.ToList())
        {
            folderSelection[key] = EditorGUILayout.Toggle(key, folderSelection[key]);
        }
    }

    void RenderCustomFolderCreationMenu()
    {
        if (customFolders == null)
        {
            Debug.LogError("Custom Folders is null!");
            return;
        }

        if (GUILayout.Button("Add Custom Folder"))
        {
            // Add a new empty entry for custom folder
            customFolders.Add("");
        }

        int toRemove = -1;
        // Display fields for custom folders
        for (int i = 0; i < customFolders.Count; i++)
        {
            GUILayout.BeginHorizontal();
            {
                customFolders[i] = EditorGUILayout.TextField($"Custom Folder {i + 1}", customFolders[i]);

                if (GUILayout.Button("x")) toRemove = i;
            }
            GUILayout.EndHorizontal();
        }

        // Remove the custom folder at the specified index
        if (toRemove != -1 && customFolders.Count > 0)
        {
            customFolders.RemoveAt(toRemove);
        }
    }

    void RenderScriptCreationMenu()
    {
        if (scriptsDatas == null)
        {
            Debug.LogError("Scripts Data is null!");
            return;
        }

        GUILayout.Label("Scripts will be created inside the Scripts folder");

        GUILayoutOption[] options = { GUILayout.Width(250) };

        // Display fields for script templates
        foreach (var scriptData in scriptsDatas)
        {
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
            GUILayout.BeginHorizontal();
            {
                GUILayout.BeginVertical();
                GUILayout.Label(scriptData.template.templateName, options);
                GUILayout.Label(new GUIContent(scriptData.template.description), EditorStyles.helpBox, options);
                GUILayout.EndVertical();

                scriptData.isSelected = EditorGUILayout.Toggle("", scriptData.isSelected, options);
            }
            GUILayout.EndHorizontal();

            if (scriptData.isSelected == false) continue;

            int toRemove = -1;
            // Display fields for script names
            for (int i = 0; i < scriptData.names.Count; i++)
            {
                GUILayout.BeginHorizontal();
                {
                    scriptData.names[i] = EditorGUILayout.TextField(scriptData.names[i] + " ", scriptData.names[i], new GUILayoutOption[] { GUILayout.ExpandWidth(true) } );

                    if (GUILayout.Button("x", GUILayout.ExpandWidth(false))) toRemove = i;
                }
                GUILayout.EndHorizontal();
            }

            // Remove the custom folder at the specified index
            if (toRemove != -1 && scriptData.names.Count > 0)
            {
                scriptData.names.RemoveAt(toRemove);
            }

            if (GUILayout.Button($"Add a {scriptData.template.templateName} script"))
            {
                // Add a new empty entry for templates scripts
                scriptData.names.Add("");
            }
        }
    }

    void RenderMaterialCreation()
    {
        if (materialsNames == null)
        {
            Debug.LogError("Materials Names is null!");
            return;
        }

        if (GUILayout.Button("Add Material"))
        {
            // Add a new empty entry for custom folder
            materialsNames.Add("");
        }

        int toRemove = -1;
        for (int i = 0; i < materialsNames.Count; i++)
        {
            GUILayout.BeginHorizontal();
            {
                materialsNames[i] = EditorGUILayout.TextField($"Material {i + 1} Name:", materialsNames[i]);

                if (GUILayout.Button("x")) toRemove = i;
            }
            GUILayout.EndHorizontal();
        }

        // Remove the custom folder at the specified index
        if (toRemove != -1 && materialsNames.Count > 0)
        {
            materialsNames.RemoveAt(toRemove);
        }
    }
    #endregion

    #region Folder and Script Creation
    private void CreateFeatureFoldersAndScripts()
    {
        if (string.IsNullOrEmpty(folderName))
        {
            Debug.LogError("Feature Name cannot be empty!");
            return;
        }

        // Create the main feature folder
        string featurePath = $"Assets/{settings.AssetsPath}/{folderName}";
        if (!AssetDatabase.IsValidFolder(featurePath))
        {
            Directory.CreateDirectory(featurePath);
        }

        // Create selected default subfolders
        foreach (var folder in folderSelection)
        {
            if (folder.Value == false) continue;

            CreateSubfolder(featurePath, folder.Key);
        }

        // Create custom subfolders
        foreach (string customFolders in customFolders)
        {
            if (string.IsNullOrEmpty(customFolders)) continue;

            CreateSubfolder(featurePath, customFolders);
        }

        // Create selected scripts
        foreach (var data in scriptsDatas)
        {
            if (!data.isSelected) continue;

            foreach (string name in data.names)
            {
                if (string.IsNullOrEmpty(name)) continue;

                CreateScript(featurePath, data.template, name);
            }
        }

        // Create Materials
        foreach (string name in materialsNames)
        {
            if (string.IsNullOrEmpty(name)) continue;

            CreateMaterial(featurePath, name);
        }

        if (ReloadDomainOnSave)
        {
            AssetDatabase.Refresh();
        }
        Debug.Log($"Created folders and scripts for {folderName}");

        ResetData();
    }

    private void CreateSubfolder(string rootPath, string folderName)
    {
        string fullPath = rootPath + $"/{folderName}";
        if (AssetDatabase.IsValidFolder(fullPath)) return;

        Directory.CreateDirectory(fullPath);
    }

    private void CreateScript(string folderPath, ScriptTemplate template, string className)
    {
        folderPath += "/Scripts";
        string scriptPath = folderPath + $"/{className}.cs";

        // Ensure Scripts folder exists
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string scriptContent = template.templateContent.Replace("{ClassName}", className);

        File.WriteAllText(scriptPath, scriptContent);
        Debug.Log($"Script {className}.cs created at {folderPath}");
    }

    private void CreateMaterial(string rootPath, string materialName)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        Material material = new(shader);
        AssetDatabase.CreateAsset(material, $"{rootPath}/Materials/{materialName}.mat");
        Debug.Log($"Material {materialName}.mat created at {rootPath}/Materials");
    }
    #endregion
}