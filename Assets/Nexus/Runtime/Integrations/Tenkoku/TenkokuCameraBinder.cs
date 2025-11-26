using UnityEngine;
using UnityEngine.SceneManagement;
using Tenkoku.Core;
using Mirror;

namespace Nexus
{
    /// <summary>
    /// Continuously ensures Tenkoku uses the correct active player camera on this client.
    /// Works offline and in multiplayer. Avoids menu/disabled cameras.
    /// </summary>
    public class TenkokuCameraBinder : MonoBehaviour
    {
        [SerializeField] private float checkInterval = 1.0f;
        private float nextCheck;
        private Camera lastAssigned;

        private void Start()
        {
            ForceBind();
        }

        private void Update()
        {
            if (Time.unscaledTime >= nextCheck)
            {
                nextCheck = Time.unscaledTime + checkInterval;
                ForceBind();
            }
        }

        private void ForceBind()
        {
            var cam = FindBestLocalCamera();
            if (cam == null) return;
            if (lastAssigned == cam && cam.enabled) return; // already bound

            var modules = Object.FindObjectsOfType<TenkokuModule>(true);
            TenkokuModule primary = null;
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (modules != null && modules.Length > 0)
            {
                for (int i = 0; i < modules.Length; i++)
                {
                    var ten = modules[i];
                    if (ten != null && ten.gameObject.scene == activeScene)
                    {
                        primary = ten;
                        break;
                    }
                }
                if (primary == null) primary = modules[0];

                if (primary != null)
                {
                    primary.cameraTypeIndex = 1;
                    primary.useAutoFX = true;
                    primary.enableFog = true;
                    primary.mainCamera = cam.transform;
                    primary.manualCamera = cam.transform;
                    primary.useCamera = cam.transform;
                    primary.useCameraCam = cam;
                }
            }
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.Skybox;

            // Ensure FX components exist so rain/fog effects render on this camera
            if (Application.isPlaying)
            {
                var fog = cam.GetComponent<Tenkoku.Effects.TenkokuSkyFog>();
                if (fog == null) cam.gameObject.AddComponent<Tenkoku.Effects.TenkokuSkyFog>();
                var shafts = cam.GetComponent<Tenkoku.Effects.TenkokuSunShafts>();
                if (shafts == null) cam.gameObject.AddComponent<Tenkoku.Effects.TenkokuSunShafts>();
            }
            lastAssigned = cam;
        }

        private static Camera FindBestLocalCamera()
        {
            // 1) Prefer camera owned by local NetworkPlayer (on this client)
            if (NetworkClient.active)
            {
                var players = Object.FindObjectsOfType<Nexus.Networking.NetworkPlayer>();
                foreach (var p in players)
                {
                    if (p != null && p.isLocalPlayer)
                    {
                        var c = p.GetComponentInChildren<Camera>(true);
                        if (c != null && c.enabled && c.gameObject.activeInHierarchy)
                            return c;
                    }
                }
            }

            // 2) Prefer any enabled camera tagged MainCamera
            var all = Object.FindObjectsOfType<Camera>();
            foreach (var c in all)
            {
                if (c != null && c.enabled && c.gameObject.activeInHierarchy && c.CompareTag("MainCamera"))
                    return c;
            }

            // 3) Fallback to Camera.main
            if (Camera.main != null && Camera.main.enabled)
                return Camera.main;

            // 4) Any enabled camera
            foreach (var c in all)
            {
                if (c != null && c.enabled && c.gameObject.activeInHierarchy)
                    return c;
            }

            return null;
        }
    }
}
