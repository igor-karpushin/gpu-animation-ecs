#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;

namespace SnivelerCode.GpuAnimation.Scripts.Editor
{
    public class AnimationGenerator : EditorWindow
    {
        static AnimationGenerator s_Window;

        List<GeneratorPrefabInstance> m_Prefabs;
        Texture2D m_AnimationTexture;
        Dictionary<int, int> m_PartialTextureIndex;
        Rect[] m_MTexturePackRects;
        Texture2D m_BaseTexture;
        string m_BatchName = string.Empty;

        [SerializeField]
        Shader shader;

        static readonly int[] s_AnimTextures =
        {
            Shader.PropertyToID("_SnivelerMainTextureFirst"),
            Shader.PropertyToID("_SnivelerMainTextureSecond"),
            Shader.PropertyToID("_SnivelerMainTextureThird")
        };

        static readonly int s_MainTexture = Shader.PropertyToID("_SnivelerMainTexture");
        static readonly int s_AlphaClip = Shader.PropertyToID("_AlphaClip");

        void OnEnable()
        {
            m_Prefabs = new List<GeneratorPrefabInstance>();
            EditorApplication.update += GenerateAnimation;
        }

        void OnDisable()
        {
            EditorApplication.update -= GenerateAnimation;
            m_BaseTexture = null;
            m_MTexturePackRects = null;
        }

        void GenerateAnimation() { }

        [MenuItem("Sniveler Code/Animation Generator", false)]
        static void MakeWindow()
        {
            s_Window = GetWindow(typeof(AnimationGenerator)) as AnimationGenerator;
            if (s_Window != null) s_Window.titleContent = new GUIContent("Generator");
        }

        void OnGUI()
        {
            GUI.skin.label.richText = true;
            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();

            m_BatchName = EditorGUILayout.TextField("Batch Name:", m_BatchName);
            if (m_BatchName.Length < 5)
            {
                return;
            }

            EditorGUILayout.BeginVertical();
            for (var i = 0; i < m_Prefabs.Count; ++i)
            {
                EditorGUILayout.BeginHorizontal();

                var prefabInstance = m_Prefabs[i];
                var windowElement = (GameObject)EditorGUILayout.ObjectField(prefabInstance.Source, typeof(GameObject), true);
                if (windowElement)
                {
                    if (prefabInstance.Source == null || windowElement != prefabInstance.Source)
                    {
                        prefabInstance.Clear();
                        var renderer = windowElement.GetComponentInChildren<SkinnedMeshRenderer>();
                        if (renderer && renderer.sharedMesh)
                        {
                            prefabInstance.Source = windowElement;
                            prefabInstance.Extend = prefabInstance.Source != null;
                            
                            for (var k = 0; k < renderer.bones.Length; ++k)
                            {
                                prefabInstance.BoneMap.Add(renderer.bones[k].name, k);
                            }
                            prefabInstance.Lods.Add(new GeneratorLodInstance
                            {
                                Mesh = renderer.sharedMesh,
                                Percent = 60,
                                Locked = true
                            });
                            
                            prefabInstance.SubAlpha = new List<bool>();
                            foreach (var material in renderer.sharedMaterials)
                            {
                                var propertyAlpha = material.GetFloat(s_AlphaClip);
                                prefabInstance.SubAlpha.Add(math.abs(propertyAlpha - 1f) < 0.1f);
                            }
                            
                            var prefabAnimator = prefabInstance.Source.GetComponentInChildren<Animator>();
                            if (prefabAnimator)
                            {
                                prefabInstance.AnimatorSet((AnimatorController)prefabAnimator.runtimeAnimatorController);
                            }
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Prefab Error", "No SkinnedMeshRenderer", "ok");
                        }
                    }
                }

                GUI.enabled = prefabInstance.Source != null;
                var buttonStyle = new GUIStyle(GUI.skin.button);
                prefabInstance.Extend = GUILayout.Toggle(prefabInstance.Extend, "Config", buttonStyle);
                GUI.enabled = true;

                if (GUILayout.Button("Remove"))
                {
                    m_Prefabs.RemoveAt(i);
                }

                EditorGUILayout.EndHorizontal();

                if (prefabInstance.Extend)
                {
                    EditorGUILayout.BeginVertical(new GUIStyle { padding = new RectOffset(20, 0, 0, 0) });
                    var labelRight = new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        padding = new RectOffset(5, 5, 0, 0),
                        normal = new GUIStyleState
                        {
                            textColor = Color.yellow,
                            background = Texture2D.linearGrayTexture
                        }
                    };

                    // clips
                    GUILayout.Label("Animator Setup", labelRight);

                    EditorGUILayout.BeginHorizontal();
                    var windowAnimator = (AnimatorController)EditorGUILayout.ObjectField(prefabInstance.Animator, typeof(AnimatorController), true);
                    var prefabAnimator = prefabInstance.Source.GetComponentInChildren<Animator>();
                    if (prefabAnimator)
                    {
                        if (GUILayout.Button("From Prefab"))
                        {
                            var prefabAnimatorRuntime = (AnimatorController)prefabAnimator.runtimeAnimatorController;
                            if (prefabAnimatorRuntime)
                            {
                                windowAnimator = (AnimatorController)prefabAnimator.runtimeAnimatorController;
                            }
                        }
                    }

                    EditorGUILayout.EndHorizontal();

                    if (prefabInstance.Animator != windowAnimator)
                    {
                        prefabInstance.AnimatorSet(windowAnimator);
                    }

                    if (prefabInstance.Clips.Count > 0)
                    {
                        foreach (var clipName in prefabInstance.Clips.Keys)
                        {
                            var animInfo = prefabInstance.Clips[clipName];
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Space(10);
                            animInfo.Enable = GUILayout.Toggle(animInfo.Enable, $"{clipName}", new GUIStyle(GUI.skin.toggle)
                            {
                                alignment = TextAnchor.MiddleRight,
                                fixedWidth = 100
                            });

                            GUI.enabled = animInfo.Enable;
                            GUILayout.Space(20);
                            animInfo.Fps = (int)GUILayout.HorizontalSlider(animInfo.Fps, 15, 30, GUILayout.Width(70));
                            GUILayout.Label(animInfo.Fps + " Fps");

                            animInfo.Speed = GUILayout.HorizontalSlider(animInfo.Speed, 0.5f, 4f, GUILayout.Width(70));

                            var speedValue = GUILayout.TextField(animInfo.Speed.ToString("F2"), 4);
                            if (float.TryParse(speedValue, out var value))
                            {
                                animInfo.Speed = math.clamp(value, 0.5f, 4f);
                            }

                            GUILayout.Label("speed");
                            GUILayout.Space(40);
                            GUI.enabled = true;
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("No Animator Clips Find", MessageType.Warning);
                    }

                    // load setup
                    GUILayout.Label("Lods Setup", labelRight);
                    foreach (var lodInstance in prefabInstance.Lods)
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Label("Camera", GUILayout.Width(60));
                        lodInstance.Percent = (int)GUILayout.HorizontalSlider(lodInstance.Percent, 100, 0, GUILayout.Width(70));

                        EditorGUI.BeginDisabledGroup(lodInstance.Locked);
                        GUILayout.Label($"{lodInstance.Percent}%", GUILayout.Width(40));
                        
                        var buttonPressed = false;
                        var windowMesh = (Mesh)EditorGUILayout.ObjectField(lodInstance.Mesh, typeof(Mesh), true);
                        if (windowMesh)
                        {
                            var assetMeshPath = AssetDatabase.GetAssetPath(windowMesh);
                            var assetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetMeshPath);
                            
                            SkinnedMeshRenderer skinRenderer;
                            if (assetPrefab && (skinRenderer = assetPrefab.GetComponentInChildren<SkinnedMeshRenderer>()))
                            {
                                lodInstance.Mesh = windowMesh;
                                lodInstance.Skin = skinRenderer;
                            }
                            else
                            {
                                EditorUtility.DisplayDialog("Mesh Prefab Error", "No SkinnedMeshRenderer", "ok");
                            } 
                        }

                        if (!lodInstance.Locked)
                        {
                            buttonPressed = GUILayout.Button("remove", GUILayout.Width(60));
                        }

                        EditorGUI.EndDisabledGroup();
                        EditorGUILayout.EndHorizontal();
                        
                        if (buttonPressed)
                        {
                            prefabInstance.Lods.Remove(lodInstance);
                            break;
                        }
                    }

                    if (GUILayout.Button("add lod", GUILayout.Width(200)))
                    {
                        prefabInstance.Lods.Add(new GeneratorLodInstance
                        {
                            Percent = (int)(prefabInstance.Lods.Last().Percent * 0.5f)
                        });
                    }

                    EditorGUILayout.EndVertical();
                }
            }

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Add Prefab"))
            {
                var prefabInstance = new GeneratorPrefabInstance();
                prefabInstance.Clear();
                m_Prefabs.Add(prefabInstance);
            }

            if (GUILayout.Button("Generate"))
            {
                // directory
                BuildAnimationTexture();
            }

            EditorGUILayout.EndHorizontal();

            if (m_AnimationTexture != null)
            {
                GUILayout.Label($"Texture Size: {m_AnimationTexture.width}x{m_AnimationTexture.height}", new GUIStyle
                {
                    fixedWidth = 20,
                    padding = new RectOffset(5, 0, 5, 0),
                    normal = { textColor = Color.white }
                });
                var lastRect = GUILayoutUtility.GetLastRect();
                EditorGUI.DrawPreviewTexture(new Rect(5f, lastRect.y + lastRect.height + 5, m_AnimationTexture.width, m_AnimationTexture.height), m_AnimationTexture);
                GUILayout.Space(m_AnimationTexture.height + 5);
            }

            EditorGUILayout.EndVertical();
        }

        void ForceDirectory(string directory)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        void PrepareTexture(Texture texture)
        {
            var assetPath = AssetDatabase.GetAssetPath(texture);
            var tImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (tImporter != null)
            {
                if (!tImporter.isReadable)
                {
                    tImporter.isReadable = true;
                    AssetDatabase.ImportAsset(assetPath);
                }
            }
        }

        void BuildAnimationTexture()
        {
            var pixelIndex = 0;

            var writePixels = new List<GeneratorAnimationTexture>();
            for (var i = 0; i < s_AnimTextures.Length; ++i)
            {
                writePixels.Add(new GeneratorAnimationTexture(2048));
            }

            var resourcePath = Path.Combine("Assets", "Generated", m_BatchName);
            ForceDirectory(resourcePath);

            m_BaseTexture = new Texture2D(4096, 4096, TextureFormat.RGBA32, true);
            AssetDatabase.CreateAsset(m_BaseTexture, Path.Combine(resourcePath, "BatchTexture.asset"));

            var baseMaterial = new Material(shader) { name = "BatchMaterial", enableInstancing = true };
            AssetDatabase.CreateAsset(baseMaterial, Path.Combine(resourcePath, "BatchMaterial.mat"));

            var partialTextures = new Dictionary<int, Texture2D>();
            m_PartialTextureIndex = new Dictionary<int, int>();

            // create texture atlas
            foreach (var prefabInstance in m_Prefabs)
            {
                var renderer = prefabInstance.Source.GetComponentInChildren<SkinnedMeshRenderer>();
                var sharedMesh = renderer.sharedMesh;

                for (var i = 0; i < sharedMesh.subMeshCount; ++i)
                {
                    // todo: create normal specular other maps
                    /*var textureNames = renderer.sharedMaterials[i].GetTexturePropertyNames();
                    foreach (var textureName in textureNames)
                    {
                        var texture = renderer.sharedMaterials[i].GetTexture(textureName);
                        textures.Add(textureName);
                    }*/

                    var mainTexture = renderer.sharedMaterials[i].mainTexture;
                    var textureHash = mainTexture.GetHashCode();
                    if (!partialTextures.ContainsKey(textureHash))
                    {
                        PrepareTexture(mainTexture);
                        m_PartialTextureIndex.Add(textureHash, partialTextures.Count);
                        partialTextures.Add(textureHash, mainTexture as Texture2D);
                    }
                }
            }

            m_MTexturePackRects = m_BaseTexture.PackTextures(
                partialTextures.Values.ToArray(), 0, 4096);

            foreach (var prefabInstance in m_Prefabs)
            {
                var prefabTransform = prefabInstance.Source.transform;
                prefabTransform.position = float3.zero;
                prefabTransform.rotation = quaternion.identity;

                var prefabDirectory = Path.Combine(resourcePath, prefabInstance.Source.name);
                ForceDirectory(prefabDirectory);

                // build meshes
                var rootObject = BuildLodMeshProcess(prefabInstance, baseMaterial, mesh =>
                    AssetDatabase.CreateAsset(mesh, Path.Combine(prefabDirectory, $"{mesh.name}.asset")));

                var renderer = prefabInstance.Source.GetComponentInChildren<SkinnedMeshRenderer>();

                // first animation -> t pose
                var animations = new List<MaterialAnimation>
                {
                    new() { Frames = 1, Start = (ushort)pixelIndex, Speed = 1 }
                };
                BuildTPose(renderer, writePixels, ref pixelIndex);

                foreach (var clipName in prefabInstance.Clips.Keys)
                {
                    var clipInstance = prefabInstance.Clips[clipName];
                    if (clipInstance.Enable)
                    {
                        var frameCount = (ushort)(clipInstance.Source.length * clipInstance.Fps);
                        animations.Add(new MaterialAnimation
                        {
                            Fps = (byte)clipInstance.Fps,
                            Frames = frameCount,
                            Start = (ushort)pixelIndex,
                            Loop = clipInstance.Source.isLooping,
                            Speed = (byte)clipInstance.Speed
                        });

                        foreach (var frame in Enumerable.Range(0, frameCount))
                        {
                            clipInstance.Source.SampleAnimation(prefabInstance.Source, (float)frame / clipInstance.Fps);
                            WriteBoneMatrix(renderer, writePixels, ref pixelIndex);
                        }
                    }
                }

                var configComponent = rootObject.AddComponent<MaterialConfigSetupAuthoring>();
                configComponent.Setup(renderer.bones.Length, animations, prefabInstance.SubAlpha);

                PrefabUtility.SaveAsPrefabAsset(rootObject, Path.Combine(prefabDirectory, $"{rootObject.name}.prefab"));

                DestroyImmediate(rootObject);
            }

            if (pixelIndex == 0)
            {
                m_AnimationTexture = null;
                return;
            }

            var textureWidth = 1;
            var textureHeight = 1;

            while (textureWidth * textureHeight < pixelIndex)
            {
                if (textureWidth <= textureHeight)
                {
                    textureWidth *= 2;
                }
                else
                {
                    textureHeight *= 2;
                }
            }

            for (var i = 0; i < writePixels.Count; ++i)
            {
                var texturePixels = new Color[textureWidth * textureHeight];
                Array.Copy(writePixels[i].Pixels, texturePixels, pixelIndex);

                var animTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBAHalf, false, true);
                animTexture.SetPixels(texturePixels);
                animTexture.Apply();
                animTexture.filterMode = FilterMode.Point;
                baseMaterial.SetTexture(s_AnimTextures[i], animTexture);
                AssetDatabase.CreateAsset(animTexture, Path.Combine(resourcePath, $"AnimationTexture{i}.asset"));
            }

            baseMaterial.SetTexture(s_MainTexture, m_BaseTexture);
        }

        void BuildTPose(SkinnedMeshRenderer renderer, IReadOnlyList<GeneratorAnimationTexture> pixels, ref int index)
        {
            var mesh = renderer.sharedMesh;
            var assetPath = AssetDatabase.GetAssetPath(mesh);
            var originalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            var originalRenderer = originalPrefab.GetComponentInChildren<SkinnedMeshRenderer>();
            WriteBoneMatrix(originalRenderer, pixels, ref index);
        }

        void WriteBoneMatrix(SkinnedMeshRenderer renderer, IReadOnlyList<GeneratorAnimationTexture> pixels, ref int index)
        {
            foreach (var boneMatrix in renderer.bones.Select((b, idx) => b.localToWorldMatrix * renderer.sharedMesh.bindposes[idx]))
            {
                pixels[0].Write(index, new Color(boneMatrix.m00, boneMatrix.m01, boneMatrix.m02, boneMatrix.m03));
                pixels[1].Write(index, new Color(boneMatrix.m10, boneMatrix.m11, boneMatrix.m12, boneMatrix.m13));
                pixels[2].Write(index, new Color(boneMatrix.m20, boneMatrix.m21, boneMatrix.m22, boneMatrix.m23));
                index++;
            }
        }

        GameObject BuildLodMeshProcess(GeneratorPrefabInstance instance, Material baseMaterial, Action<Mesh> meshAction)
        {
            var rootObject = new GameObject(instance.Source.name);
            var lodGroupComponent = rootObject.AddComponent<LODGroup>();

            var lodsGroups = new List<LOD>();
            var renderer = instance.Source.GetComponentInChildren<SkinnedMeshRenderer>();
            for (var i = 0; i < instance.Lods.Count; ++i)
            {
                var lodInstance = instance.Lods[i];

                var lodMesh = lodInstance.Mesh;
                var mesh = Instantiate(lodMesh);
                mesh.name = "LodMesh" + lodInstance.Percent;

                var uvUpdate = new Vector2[mesh.uv.Length];
                for (var k = 0; k < mesh.subMeshCount; ++k)
                {
                    var mainTexture = renderer.sharedMaterials[k].mainTexture;

                    var textureHash = mainTexture.GetHashCode();
                    var rectIndex = m_PartialTextureIndex[textureHash];
                    var textureRect = m_MTexturePackRects[rectIndex];

                    var subMeshInfo = mesh.GetSubMesh(k);
                    for (var v = 0; v < subMeshInfo.vertexCount; ++v)
                    {
                        var uvVector = mesh.uv[subMeshInfo.firstVertex + v];
                        uvUpdate[subMeshInfo.firstVertex + v] = new Vector2
                        {
                            x = uvVector.x * textureRect.width + textureRect.x,
                            y = uvVector.y * textureRect.height + textureRect.y
                        };
                    }
                }

                mesh.SetUVs(0, uvUpdate);

                var boneIndexes = new List<Vector4>();
                var boneWeights = new List<Vector4>();
                var skinBones = lodInstance.Skin.bones;
                foreach (var boneWeight in mesh.boneWeights)
                {
                    var boneIndex0 = instance.BoneMap[skinBones[boneWeight.boneIndex0].name];
                    var boneIndex1 = instance.BoneMap[skinBones[boneWeight.boneIndex1].name];
                    var boneIndex2 = instance.BoneMap[skinBones[boneWeight.boneIndex2].name];
                    var boneIndex3 = instance.BoneMap[skinBones[boneWeight.boneIndex3].name];
                    boneIndexes.Add(new Vector4(boneIndex0, boneIndex1, boneIndex2, boneIndex3));
                    boneWeights.Add(new Vector4(boneWeight.weight0, boneWeight.weight1, boneWeight.weight2, boneWeight.weight3));
                }
                
                mesh.SetUVs(2, boneIndexes);
                mesh.SetUVs(3, boneWeights);

                meshAction?.Invoke(mesh);

                var lodObject = new GameObject("LodModel" + lodInstance.Percent);
                lodObject.transform.SetParent(rootObject.transform);

                var meshFilter = lodObject.AddComponent<MeshFilter>();
                meshFilter.mesh = mesh;

                var subMeshCount = mesh.subMeshCount;

                var meshRenderer = lodObject.AddComponent<MeshRenderer>();
                meshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
                meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                meshRenderer.lightProbeUsage = LightProbeUsage.Off;

                var sharedMaterials = new Material[subMeshCount];
                for (var k = 0; k < subMeshCount; ++k)
                {
                    sharedMaterials[k] = baseMaterial;
                }

                meshRenderer.sharedMaterials = sharedMaterials;

                lodsGroups.Add(new LOD
                {
                    screenRelativeTransitionHeight = lodInstance.Percent * 0.01f,
                    renderers = new Renderer[] { meshRenderer }
                });
            }

            lodGroupComponent.SetLODs(lodsGroups.ToArray());

            return rootObject;
        }
    }
}

#endif
