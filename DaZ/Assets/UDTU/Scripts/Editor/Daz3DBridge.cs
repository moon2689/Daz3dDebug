﻿using UnityEngine;
using UnityEditor;
using System;




namespace Daz3D
{

    using record = Daz3DDTUImporter.ImportEventRecord;

    /// <summary>
    /// An editor window where unity user can monitor the progress and history and details of DTU import activity 
    /// </summary>
    public class Daz3DBridge : EditorWindow
    {

        private Vector2 _scrollPos;
        //Tuple<UnityEngine.Object, Texture> thumbnail = null;
        public static readonly Color ThemedColor = new Color(.7f, 1f, .8f);

        Texture masthead = null;
        public enum ToolbarMode
        {
            History,
            ReadMe,
            Options,
            Commands
        }
        public static ToolbarMode CurrentToolbarMode = ToolbarMode.ReadMe;
        private bool _needsRepaint = false;

        Vector2 readMePos = Vector2.zero;
        private static float _progress;
        public static float Progress
        {
            get { return _progress; }
            set { _progress = value; _instance?.Repaint(); }
        }

        private static Daz3DBridge _instance;
        [MenuItem("Daz3D/Open DazToUnity Bridge window", false, 0)]
        public static void ShowWindow()
        {
            ObtainInstance();
        }
        private static void ObtainInstance() 
        {  
            _instance = (Daz3DBridge)GetWindow(typeof(Daz3DBridge));
#if USING_HDRP
            _instance.titleContent = new GUIContent("uDTU: HDRP");
#elif USING_URP
            _instance.titleContent = new GUIContent("uDTU: URP");
#elif USING_BUILTIN
            _instance.titleContent = new GUIContent("uDTU: Built-In Rendering");
#else
            _instance.titleContent = new GUIContent("uDTU: RenderPipeline Not Detected");
            CurrentToolbarMode = ToolbarMode.Options;            
#endif
        }

        public static void AddDiffusionProfilePrompt()
        {
#if USING_HDRP
            if (System.IO.File.Exists("Assets/Daz3D/do_once.cfg"))
            {
                return;
            }
            byte[] buffer = { 0 };
            System.IO.File.WriteAllBytes("Assets/Daz3D/do_once.cfg", buffer);


            string diffusionProfileSettingsPath = "Project/HDRP Default Settings";
            if (Application.unityVersion.Contains("2019"))
            {
                diffusionProfileSettingsPath = "Project/Quality/HDRP";
            }

            EditorWindow settingspanel = UnityEditor.SettingsService.OpenProjectSettings(diffusionProfileSettingsPath);

            string diffusionProfileInstructions = "In order to use the HDRP Daz skin shaders, " +
                "you must manually add the IrayUberSkinDiffusionProfile to the Default Diffusion Profile Assets list.\n\n" +
                "This list is found at the bottom of the HDRP Default Settings panel in the Project Settings dialog.\n\n" +
                "Until this is done, materials using the HDRP Daz skin shader will have a Green Tint.";

            if (Application.unityVersion.Contains("2019"))
            {
                diffusionProfileInstructions = "In order to use the HDRP Daz skin shaders, " +
                "you must manually add the IrayUberSkinDiffusionProfile to the Diffusion Profile List.\n\n" +
                "For Unity 2019, this list is found in the Material section of each HD RenderPipeline Asset, " +
                "which can be found in the Quality->HDRP panel of the Project Settings dialog.\n\n" +
                "Until this is done, materials using the HDRP Daz skin shader will have a Green Tint.";
            }

            UnityEditor.EditorUtility.DisplayDialog("Required User Action: Add Iray Uber Diffusion Profile",
                diffusionProfileInstructions, "OK");
#endif
        }

        void OnEnable()
        {
            // If "Assets/Daz3D" folder does not exist, create it...
			// to prevent uDTU Daz plugin from installing Unity Files
            if (System.IO.Directory.Exists("Assets/Daz3D") == false)
            {
                System.IO.Directory.CreateDirectory("Assets/Daz3D");
            }

#if USING_HDRP || USING_URP || USING_BUILTIN

            // check for to_load file
            if (System.IO.File.Exists("Assets/Daz3D/dtu_toload.txt"))
            {
                byte[] byteBuffer = System.IO.File.ReadAllBytes("Assets/Daz3D/dtu_toload.txt");
                if (byteBuffer != null || byteBuffer.Length > 0)
                {
                    string dtuPath = System.Text.Encoding.UTF8.GetString(byteBuffer);

                    System.IO.File.Delete("Assets/Daz3D/dtu_toload.txt");
                    System.IO.File.Delete("Assets/Daz3D/dtu_toload.txt.meta");

                    if (System.IO.File.Exists(dtuPath))
                    {
                        //Debug.LogError("Found file: [" + dtuPath + "] " + dtuPath.Length);
                        if (dtuPath.Contains(".dtu"))
                        {
                            var fbxPath = dtuPath.Replace(".dtu", ".fbx");
                            Daz3DDTUImporter.Import(dtuPath, fbxPath);
                        }
                    }
                    else
                    {
                        //Debug.LogError("File NOT found: [" + dtuPath + "] " + dtuPath.Length);
                    }
                }

            }

#endif
        }

        void Update()
        {

            if (_needsRepaint)
            {
                _needsRepaint = false;

                Repaint();
            }

            if (!_instance == null)
                ObtainInstance();
        }



        private void DrawProgressBar()
        {
            if (Progress > 0)
            {
                //float progress = Mathf.Abs(DateTime.Now.Millisecond * .001f);

                GUI.backgroundColor = new Color(1, .2f, .1f);
                GUILayout.Button("", GUILayout.Width(position.width * Progress), GUILayout.Height(12));
            }
        }


        void OnGUI()
        {
            DrawProgressBar();

            var temp = GUI.backgroundColor;

            GUI.backgroundColor = ThemedColor;

            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.BeginHorizontal();

            if (masthead == null)
                masthead = Resources.Load<Texture>("UnofficialDTU_Logo_TextOnly");

            GUILayout.FlexibleSpace();
            GUILayout.Label(masthead, GUILayout.Height(100));
            GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUIStyle myStyle = new GUIStyle(GUI.skin.label);
            myStyle.margin = new RectOffset(0, 0, 0, 0);

            var labels = new string[] { "History", "Read Me", "Options", "Commands" };
            CurrentToolbarMode = (ToolbarMode)GUILayout.Toolbar((int)CurrentToolbarMode, labels);

            switch (CurrentToolbarMode)
            {
                case ToolbarMode.History:
                    DrawHistory(myStyle);
                    break;
                case ToolbarMode.ReadMe:
                    DrawHelpReadMe();
                    break;
                case ToolbarMode.Options:
                    DrawOptions();
                    break;
                case ToolbarMode.Commands:
                    DrawCommands();
                    break;

            }

            GUI.backgroundColor = temp;

        }


        void OnSelectionChange()
        {
            if (Daz3DDTUImporter.ValidateDTUSelected())
                CurrentToolbarMode = ToolbarMode.Commands;
            _needsRepaint = true;
        }

        private void DrawCommands()
        {
            GUIStyle bigStyle = new GUIStyle(GUI.skin.button);
            bigStyle.fontSize = 14;
            bigStyle.normal.textColor = ThemedColor;
            bigStyle.padding = new RectOffset(24, 24, 12, 12);
            bigStyle.margin = new RectOffset(12, 12, 12, 12);

            if (CommandButton("Clear History", bigStyle))
                Daz3DDTUImporter.EmptyEventQueue();

            if (Daz3DDTUImporter.ValidateDTUSelected())
            {
                var dtu = Selection.activeObject;

                //the button
                if (CommandButton("Create Unity Prefab from '" + dtu.name + ".dtu'", bigStyle))
                {
                    var dtuPath = AssetDatabase.GetAssetPath(dtu);
                    var fbxPath = dtuPath.Replace(".dtu", ".fbx");
                    Daz3DDTUImporter.Import(dtuPath, fbxPath);
                }

                if (CommandButton("Extract Materials from '" + dtu.name + ".dtu'", bigStyle))
                    DTUConverter.MenuItemConvert();
            }
            else
            {
                GUILayout.Space(12);
                GUILayout.Label("Select a DTU file in project window for more commands...");
            }
        }


        private bool CommandButton(string label, GUIStyle style)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            var result = GUILayout.Button(label, style);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            return result;
        }

        private void DrawOptions()
        {
            GUIStyle bigStyle = new GUIStyle(GUI.skin.toggle);
            bigStyle.fontSize = 14;

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUILayout.BeginVertical();
            GUILayout.Space(12);

            Daz3DDTUImporter.AutoImportDTUChanges = GUILayout.Toggle(Daz3DDTUImporter.AutoImportDTUChanges, "Automatically import when DTU changes are detected", bigStyle);
            Daz3DDTUImporter.GenerateUnityPrefab = GUILayout.Toggle(Daz3DDTUImporter.GenerateUnityPrefab, "Generate a Unity Prefab based on FBX and DTU", bigStyle);
            Daz3DDTUImporter.ReplaceSceneInstances = GUILayout.Toggle(Daz3DDTUImporter.ReplaceSceneInstances, "Replace instances of Unity Prefab in active scene(s)", bigStyle);
            Daz3DDTUImporter.AutomateMecanimAvatarMappings = GUILayout.Toggle(Daz3DDTUImporter.AutomateMecanimAvatarMappings, "Automatically setup the Mecanim Avatar", bigStyle);
            Daz3DDTUImporter.ReplaceMaterials = GUILayout.Toggle(Daz3DDTUImporter.ReplaceMaterials, "Replace FBX materials with high quality Daz-shader materials", bigStyle);

            GUILayout.Space(12);
            Daz3DDTUImporter.EnableDForceSupport = GUILayout.Toggle(Daz3DDTUImporter.EnableDForceSupport, "Enable dForce support (experimental)", bigStyle);
#if USING_HDRP || USING_URP
            Daz3DDTUImporter.UseNewShaders = GUILayout.Toggle(Daz3DDTUImporter.UseNewShaders, "Use New Shaders", bigStyle);
#else
            GUILayout.Label("New Shaders only available for HDRP and URP", bigStyle);
            Daz3DDTUImporter.UseNewShaders = false;            
#endif

            GUILayout.Space(12);
            if (GUILayout.Button("Reset All", GUILayout.Width(100)))
                Daz3DDTUImporter.ResetOptions();

            GUILayout.Space(24);
#if USING_HDRP
            GUILayout.TextArea("Unofficial DTU Configured for HDRP");
#elif USING_URP
            GUILayout.TextArea("Unofficial DTU Configured for URP");
#elif USING_BUILTIN
            GUILayout.TextArea("Unofficial DTU Configured for Built-In Rendering");
#else
            GUILayout.TextArea("No Renderpipeline configured.  Press Redetect RenderPipeline to configure now.");
#endif
            GUILayout.Space(12);
            if (GUILayout.Button("Redetect RenderPipeline", GUILayout.Width(200)))
                DetectRenderPipeline.RunOnce();

            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void DrawHistory(GUIStyle myStyle)
        {
            if (Daz3DDTUImporter.EventQueue.Count > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("*Click the ", myStyle);
                GUI.contentColor = Color.cyan;
                GUILayout.Label("links", myStyle);
                GUI.contentColor = Color.white;
                GUILayout.Label(" below to select those assets in Project window.", myStyle);

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            foreach (var record in Daz3DDTUImporter.EventQueue)
            {

                GUILayout.BeginVertical(GUI.skin.box);
                record.Unfold = EditorGUILayout.Foldout(record.Unfold, "Import Event: " + record.Timestamp);

                //GUILayout.Label("Import Event: " + record.Timestamp);

                if (record.Unfold)
                {
                    GUILayout.Space(4);//lead
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(16);//indent

                    //foreach (var word in words)
                    foreach (var token in record.Tokens)
                    {
                        if (token.Selectable)
                        {
                            GUI.contentColor = Color.cyan;

                            if (GUILayout.Button(token.Text, myStyle))
                                Selection.activeObject = token.Selectable;

                            GUI.contentColor = Color.white;

                        }
                        else
                        {
                            GUILayout.Label(token.Text, myStyle);
                        }

                        if (token.EndLine)
                        {
                            GUILayout.FlexibleSpace();
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(16);//indent
                        }

                    }

                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }

                GUILayout.EndVertical();

                //GUILayout.Space(8);

            }




            EditorGUILayout.EndScrollView();
        }

        private void DrawHelpReadMe()
        {
            var readMe = Resources.Load<TextAsset>("ReadMe");
            if (readMe)
            {
                readMePos = GUILayout.BeginScrollView(readMePos);
                GUILayout.TextArea(readMe.text);
                GUILayout.EndScrollView();
            }
            else
                GUILayout.Label("ReadMe text not loaded");

        }
    }


    [InitializeOnLoad]
    public static class OrnamenfftDTUFileInProjectWindow
    {

        static OrnamenfftDTUFileInProjectWindow()
        {
            EditorApplication.projectWindowItemOnGUI += DrawAssetDetails;

        }
        private static void DrawAssetDetails(string guid, Rect rect)
        {
            if (Application.isPlaying || Event.current.type != EventType.Repaint )
                return;

            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (!assetPath.ToLower().EndsWith(".dtu"))
                return;


            if (IsMainListAsset(rect))
                rect.x += 5;
            else
            {
                rect.x += 32;
                rect.y -= 12;
            }

            //rect.width = rect.height;


            GUI.color = new Color(.5f, 1, .5f);
            GUI.Label(rect, "■");

            rect.x -= 3;
            rect.y += 4;

            GUI.color = Color.yellow;
            GUI.Label(rect, "◢");

            rect.x -= 6;
            rect.y -= 4;

            GUI.color = new Color(1f, .3f, .2f);
            GUI.Label(rect, "◀");

            //rect.x --;
            //rect.y -= 2;
            GUI.color = Color.cyan;
            GUI.Label(rect, "◤");

            GUI.color = Daz3DBridge.ThemedColor * .7f;
            rect.x -= 30;
            GUI.Label(rect, "DTU");

            GUI.color = Color.white;
        }

        private static bool IsMainListAsset(Rect rect)
        {
            return rect.height <= 20;
        }
    }

}
