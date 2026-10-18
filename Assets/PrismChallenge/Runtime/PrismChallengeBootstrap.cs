using UnityEngine;
using UnityEngine.SceneManagement;

namespace DimensionShifter.PrismChallenge
{
    // Additive installation: existing scenes, prefabs and player scripts stay untouched.
    public static class PrismChallengeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Subscribe()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void InitialScene() => Install(SceneManager.GetActiveScene());

        static void SceneLoaded(Scene scene, LoadSceneMode mode) => Install(scene);

        public static PrismChallengeRoom Install(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return null;
            DemoSession session = null;
            LevelGenerator generator = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                var existing = root.GetComponentInChildren<PrismChallengeRoom>(true);
                if (existing != null) return existing;
                if (session == null) session = root.GetComponentInChildren<DemoSession>(true);
                if (generator == null) generator = root.GetComponentInChildren<LevelGenerator>(true);
            }
            if (session == null || generator == null || !generator.authoredOnly) return null;
            var host = new GameObject("Prism Crucible • optional challenge");
            SceneManager.MoveGameObjectToScene(host, scene);
            var room = host.AddComponent<PrismChallengeRoom>();
            room.Initialize(session);
            return room;
        }
    }
}
