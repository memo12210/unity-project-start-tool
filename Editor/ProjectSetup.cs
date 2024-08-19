using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using static System.Environment;
using static System.IO.Path;
using static UnityEditor.AssetDatabase;

namespace Editor
{
    public static class ProjectSetup 
    {
        [MenuItem("Tools/Project Setup/Import Essential Assets")]
        public static void ImportEssentials() 
        {        
            Assets.ImportAsset("Odin Inspector 3.3.1.7.unitypackage", "Sirenix/Editor ExtensionSystem");
            Assets.ImportAsset("Graphy - Ultimate FPS Counter - Stats Monitor Debugger.unitypackage", "Tayx/ScriptingGUI");
            Assets.ImportAsset("Feel.unitypackage", "More Mountains/Editor ExtensionsEffects");
            Assets.ImportAsset("vFolders.unitypackage", "kubacho lab/Editor ExtensionsUtilities");
            Assets.ImportAsset("vHierarchy.unitypackage", "kubacho lab/Editor ExtensionsUtilities");
            Assets.ImportAsset("vRuler.unitypackage", "kubacho lab/Editor ExtensionsUtilities");
        }

        [MenuItem("Tools/Project Setup/Install Essential Packages")]
        public static void InstallPackages() 
        {
            Packages.InstallPackages(new[] 
            {
                "git+https://github.com/gustavopsantos/reflex.git?path=/Assets/Reflex/#8.5.2",
                "git+https://github.com/GlitchEnzo/NuGetForUnity.git?path=/src/NuGetForUnity",
                "git+https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask",
                "com.unity.inputsystem" // new input system should be installed at the end since it requires a restart
            });
        }

        [MenuItem("Tools/Project Setup/Create Folders")]
        public static void CreateFolders() 
        {
            Folders.Create("_Project", "Art", "Docs", "Prefabs", "Scripts");
            Folders.Create("_Project/Art", "Animations", "Audio", "Fonts", "Materials", "Models", "Shaders", "Sprites", "Textures");
            Folders.Create("_Project/Docs", "Design", "Guidelines", "References");
            Folders.Create("_Project/Prefabs", "Characters", "Environment", "Props", "UI");
            Folders.Create("_Project/Scripts", "Editor", "Gameplay", "Managers", "UI");           
            Refresh();
            
            Folders.Move("_Project", "Scenes");
            Folders.Move("_Project", "Settings");
            Refresh();

            MoveAsset("Assets/InputSystem_Actions.inputactions", "Assets/_Project/Settings/InputSystem_Actions.inputactions");
            DeleteAsset("Assets/Readme.asset");
            Refresh();
        }

        static class Assets 
        {
            public static void ImportAsset(string asset, string folder) 
            {
                string basePath;
                if (OSVersion.Platform is PlatformID.MacOSX or PlatformID.Unix) 
                {
                    string homeDirectory = GetFolderPath(SpecialFolder.Personal);
                    basePath = Combine(homeDirectory, "Library/Unity/Asset Store-5.x");
                } 
                else 
                {
                    string defaultPath = Combine(GetFolderPath(SpecialFolder.ApplicationData), "Unity");
                    basePath = Combine(EditorPrefs.GetString("AssetStoreCacheRootPath", defaultPath), "Asset Store-5.x");
                }

                asset = asset.EndsWith(".unitypackage") ? asset : asset + ".unitypackage";

                string fullPath = Combine(basePath, folder, asset);

                if (!File.Exists(fullPath)) 
                {
                    throw new FileNotFoundException($"The asset package was not found at the path: {fullPath}");
                }
                
                ImportPackage(fullPath, false);
            }
        }

        static class Packages 
        {
            static AddRequest _request;
            static Queue<string> _packagesToInstall = new Queue<string>();

            public static void InstallPackages(string[] packages) 
            {
                foreach (var package in packages) 
                {
                    _packagesToInstall.Enqueue(package);
                }

                if (_packagesToInstall.Count > 0) 
                {
                    StartNextPackageInstallation();
                }
            }

            static async void StartNextPackageInstallation() 
            {
                _request = Client.Add(_packagesToInstall.Dequeue());
            
                while (!_request.IsCompleted) await Task.Delay(10);
            
                if (_request.Status == StatusCode.Success) Debug.Log("Installed: " + _request.Result.packageId);
                else if (_request.Status >= StatusCode.Failure) Debug.LogError(_request.Error.message);

                if (_packagesToInstall.Count > 0) 
                {
                    await Task.Delay(1000);
                    StartNextPackageInstallation();
                }
            }
        }

        static class Folders 
        {
            public static void Create(string root, params string[] folders) 
            {
                var fullpath = Combine(Application.dataPath, root);
                if (!Directory.Exists(fullpath))
                {
                    Directory.CreateDirectory(fullpath);
                }

                foreach (var folder in folders) 
                {
                    CreateSubFolders(fullpath, folder);
                }
            }
        
            static void CreateSubFolders(string rootPath, string folderHierarchy) 
            {
                var folders = folderHierarchy.Split('/');
                var currentPath = rootPath;

                foreach (var folder in folders) 
                {
                    currentPath = Combine(currentPath, folder);
                    if (!Directory.Exists(currentPath)) 
                    {
                        Directory.CreateDirectory(currentPath);
                    }
                }
            }
        
            public static void Move(string newParent, string folderName) 
            {
                var sourcePath = $"Assets/{folderName}";
                if (IsValidFolder(sourcePath)) 
                {
                    var destinationPath = $"Assets/{newParent}/{folderName}";
                    var error = MoveAsset(sourcePath, destinationPath);

                    if (!string.IsNullOrEmpty(error)) 
                    {
                        Debug.LogError($"Failed to move {folderName}: {error}");
                    }
                }
            }
        
            public static void Delete(string folderName)
            {
                var pathToDelete = $"Assets/{folderName}";

                if (IsValidFolder(pathToDelete)) 
                {
                    DeleteAsset(pathToDelete);
                }
            }
        }
    }
}