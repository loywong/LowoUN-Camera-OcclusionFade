using System.Collections.Generic;
using UnityEngine;

namespace LowoUN.Camera {
    public class CameraOcclusionObjs : MonoBehaviour {
        [SerializeField]
        List<Renderer> bindRenderers = new List<Renderer> ();
        public List<Renderer> BindRenderers => bindRenderers;
    }
}