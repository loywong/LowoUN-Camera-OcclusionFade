using System.Collections.Generic;
using LowoUN.Util;
using Sirenix.OdinInspector;
using UnityEngine;

namespace LowoUN.Camera {
    enum ModelDeVICheckerLayerType : byte {
        NONE = 0,
        Wall,
        Ground,
        Obstacle,
        ModelDeVIChecker,
    }

    public class CameraOcclusionMgr : MonoBehaviour {
        [LabelText ("是否开启测试"), SerializeField] private bool isOpenTest;

        [SerializeField]
        Transform character;

        [SerializeField]
        float maxDistance = 100f;
        [SerializeField]
        LayerMask groundLayer;
        [SerializeField]
        LayerMask wallLayer;
        [SerializeField]
        LayerMask obstacleLayer;
        [SerializeField]
        LayerMask specLayer; // 专用层 抽象层

        RaycastHit hit_wall;
        RaycastHit hit_ground;
        RaycastHit hit_obstacle;
        // 可能交叉在一起，需要同时检测多个
        RaycastHit[] hit_specs; // = new RaycastHit[8]; // 预分配数组 
        int hit_specs_count;
        List<Renderer> meshRenderers_specs;

        [SerializeField, LabelText ("HDR虚化材质")]
        Material hdrMaterial;
        [SerializeField, LabelText ("低配虚化材质")]
        Material lowMaterial;

        Material targetMaterial;
        Dictionary<Renderer, Material[]> allwalls = new ();
        Dictionary<Renderer, Material[]> allgrounds = new ();
        Dictionary<Renderer, Material[]> allobstacles = new ();
        Dictionary<Renderer, Material[]> allSpecs = new ();

        void Awake () {
            hit_specs = new RaycastHit[8]; // 预分配数组
            meshRenderers_specs = new List<Renderer> ();

#if !UNITY_EDITOR
            isOpenTest = false;
#endif
        }

        void Start () {
            // if (Camera.main.allowHDR)
            //     targetMaterial = lowMaterial;
            // else
            targetMaterial = hdrMaterial;
        }

        // 起点偏移
        Vector3 rayStart;
        Vector3 raydir_specs;
        // readonly Vector3 roleHightHalf = new Vector3 (0, 0.8f, 0);

        void FixedUpdate () {
            if (character == null) {
                Debug.LogError ("no character");
                return;
            }

            // TEST 测量 角色到摄像机的距离
            // var maxdist = Vector3.Distance(transform.position,character.position);
            // UnityEngine.Debug.Log($"check maxDistance:{maxdist}");

            rayStart = character.position; // + roleHightHalf;
            raydir_specs = -transform.forward;

            if (Physics.Raycast (rayStart, raydir_specs, out hit_wall, maxDistance, wallLayer)) {
                if (isOpenTest) {
                    Debug.DrawRay (rayStart, raydir_specs * hit_wall.distance, Color.red, 0.1f);
                    Debug.Log ($"CameraObstacleChecker --> wall Layer 检测到遮挡 hit:{hit_wall.transform}");
                }
            }

            if (Physics.Raycast (rayStart, raydir_specs, out hit_ground, maxDistance, groundLayer)) {
                if (isOpenTest) {
                    Debug.DrawRay (rayStart, raydir_specs * hit_ground.distance, Color.red, 0.1f);
                    Debug.Log ($"CameraObstacleChecker --> ground Layer 检测到遮挡 hit:{hit_ground.transform}");
                }
            }

            if (Physics.Raycast (rayStart, raydir_specs, out hit_obstacle, maxDistance, obstacleLayer)) {
                if (isOpenTest) {
                    Debug.DrawRay (rayStart, raydir_specs * hit_obstacle.distance, Color.red, 0.1f);
                    Debug.Log ($"CameraObstacleChecker --> obstacle Layer 检测到遮挡 hit:{hit_obstacle.transform}");
                }
            }

            hit_specs_count = Physics.RaycastNonAlloc (
                rayStart,
                raydir_specs,
                hit_specs,
                maxDistance,
                specLayer
            );

#if UNITY_EDITOR
            if (isOpenTest) {
                for (int i = 0; i < hit_specs_count; i++) {
                    Debug.Log ($"CameraObstacleChecker --> specs Layer 检测到遮挡 index:{i} - {hit_specs[i].collider.name}");
                }
                Debug.Log ($"hit_wall:{hit_wall.transform},hit_ground:{hit_ground.transform},hit_obstacle:{hit_obstacle.transform}");
            }
#endif

            if (hit_wall.transform != null) {
                var obstacleRenderers = hit_wall.transform.GetComponent<CameraOcclusionObjs> ();
                if (obstacleRenderers != null) {
                    var meshRenderers = obstacleRenderers.BindRenderers;
                    if (meshRenderers.IsValid ()) {
                        ChangeObstacleTransparency (meshRenderers, ModelDeVICheckerLayerType.Wall);
                    } else Debug.Log ("遮挡对象未绑定渲染对象");
                } else { Debug.Log ("遮挡对象未添加绑定渲染对象的组件"); }
            } else RecoverAllWalls ();

            if (hit_ground.transform != null) {
                var obstacleRenderers = hit_ground.transform.GetComponent<CameraOcclusionObjs> ();
                if (obstacleRenderers != null) {
                    var meshRenderers = obstacleRenderers.BindRenderers;
                    if (meshRenderers.IsValid ()) {
                        ChangeObstacleTransparency (meshRenderers, ModelDeVICheckerLayerType.Ground);
                    } else Debug.Log ("遮挡对象未绑定渲染对象");
                } else { Debug.Log ("遮挡对象未添加绑定渲染对象的组件"); }
            } else RecoverAllGrounds ();

            if (hit_obstacle.transform != null) {
                var obstacleRenderers = hit_obstacle.transform.GetComponent<CameraOcclusionObjs> ();
                if (obstacleRenderers != null) {
                    var meshRenderers = obstacleRenderers.BindRenderers;
                    if (meshRenderers.IsValid ()) {
                        ChangeObstacleTransparency (meshRenderers, ModelDeVICheckerLayerType.Obstacle);
                    } else Debug.Log ("遮挡对象未绑定渲染对象");
                } else { Debug.Log ("遮挡对象未添加绑定渲染对象的组件"); }
            } else RecoverAllObss ();

            meshRenderers_specs.Clear ();
            RecoverAllSpecs ();
            for (int i = 0; i < hit_specs_count; i++) {
                var hit_spec = hit_specs[i];
                if (hit_spec.transform != null) {
                    var obstacleRenderers = hit_spec.transform.GetComponent<CameraOcclusionObjs> ();
                    if (obstacleRenderers != null) {
                        var meshRenderers = obstacleRenderers.BindRenderers;
                        if (meshRenderers.IsValid ()) {
                            meshRenderers_specs.AddRange (meshRenderers);
                            // ChangeObstacleTransparency (meshRenderers, ModelDeVICheckerLayerType.ModelDeVIChecker);
                        } else Debug.Log ("遮挡对象未绑定渲染对象");
                    } else { Debug.Log ("遮挡对象未添加绑定渲染对象的组件"); }
                }
            }

            if (meshRenderers_specs.IsValid ())
                ChangeObstacleTransparency (meshRenderers_specs, ModelDeVICheckerLayerType.ModelDeVIChecker);
        }

        void RecoverAllWalls () {
            foreach (var item in allwalls) {
                if (item.Key is MeshRenderer mr)
                    mr.materials = item.Value;
                else if (item.Key is SkinnedMeshRenderer smr)
                    smr.materials = item.Value;
            }
            allwalls.Clear ();
        }
        void RecoverAllGrounds () {
            // Debug.Log($"allgrounds:{allgrounds.Count}");
            foreach (var item in allgrounds) {
                if (item.Key is MeshRenderer mr)
                    mr.materials = item.Value;
                else if (item.Key is SkinnedMeshRenderer smr)
                    smr.materials = item.Value;
            }

            allgrounds.Clear ();
        }
        void RecoverAllObss () {
            foreach (var item in allobstacles) {
                if (item.Key is MeshRenderer mr)
                    mr.materials = item.Value;
                else if (item.Key is SkinnedMeshRenderer smr)
                    smr.materials = item.Value;
            }
            allobstacles.Clear ();
        }
        void RecoverAllSpecs () {
            foreach (var item in allSpecs) {
                if (item.Key is MeshRenderer mr)
                    mr.materials = item.Value;
                else if (item.Key is SkinnedMeshRenderer smr)
                    smr.materials = item.Value;
            }
            allSpecs.Clear ();
        }

        List<Material> itemMats = new ();
        void ChangeObstacleTransparency (List<Renderer> objs, ModelDeVICheckerLayerType layerType) {
            if (objs.IsNotValid ())
                return;

            foreach (var item in objs) {
                if (item == null || item.materials == null || item.materials.Length == 0)
                    continue;

                if (layerType == ModelDeVICheckerLayerType.Wall && allwalls.ContainsKey (item))
                    continue;
                if (layerType == ModelDeVICheckerLayerType.Ground && allgrounds.ContainsKey (item))
                    continue;
                if (layerType == ModelDeVICheckerLayerType.Obstacle && allobstacles.ContainsKey (item))
                    continue;
                if (layerType == ModelDeVICheckerLayerType.ModelDeVIChecker && allSpecs.ContainsKey (item))
                    continue;

                if (item is MeshRenderer or SkinnedMeshRenderer) {
                    if (layerType == ModelDeVICheckerLayerType.Wall) {
                        allwalls[item] = item.materials;
                    } else if (layerType == ModelDeVICheckerLayerType.Ground) {
                        allgrounds[item] = item.materials;
                    } else if (layerType == ModelDeVICheckerLayerType.Obstacle) {
                        allobstacles[item] = item.materials;
                    } else if (layerType == ModelDeVICheckerLayerType.ModelDeVIChecker) {
                        allSpecs[item] = item.materials;
                    }

                    if (item.materials != null && item.materials.Length > 0) {
                        itemMats.Clear ();
                        for (int i = 0; i < item.materials.Length; i++) {
                            itemMats.Add (targetMaterial);
                        }
                        item.materials = itemMats.ToArray ();
                    }
                }
            }
        }
    }
}