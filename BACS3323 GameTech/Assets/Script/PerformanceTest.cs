using System.Collections;
using UnityEngine;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class PerformanceTest : MonoBehaviour
{
    public DungeonController controller;

    public float interval = 5f;
    public float testDuration = 300f;

    float totalTime = 0f;

    int frameCount = 0;
    float fpsTimer = 0f;
    float currentFPS = 0f;


    int testCount = 0;

    float totalGenTime = 0;
    float totalFPS = 0;
    float totalMemory = 0;

    float minGenTime = float.MaxValue;
    float maxGenTime = 0;

    float minFPS = float.MaxValue;
    float maxFPS = 0;

    float minMemory = float.MaxValue;
    float maxMemory = 0;

    void Start()
    {
        StartCoroutine(RunTest());
    }

    IEnumerator RunTest()
    {
        Debug.Log("=== PERFORMANCE TEST START ===");

        while (totalTime < testDuration)
        {
            yield return new WaitForSeconds(interval);

            RunSingleTest();

            totalTime += interval;
        }

        PrintFinalResult();

        Debug.Log("=== PERFORMANCE TEST END ===");
    }

    void RunSingleTest()
    {
        // Testing parameters
        controller.seed = Random.Range(0, 999);
        controller.size = 12;
        controller.difficulty = 1.0f;
        controller.enableBranches = true;

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        controller.RunGeneration();

        stopwatch.Stop();
        float genTime = stopwatch.ElapsedMilliseconds;

        long memory = System.GC.GetTotalMemory(false) / (1024 * 1024);
        float fps = currentFPS;

        // =========================
        // update stats
        // =========================
        testCount++;

        totalGenTime += genTime;
        totalFPS += fps;
        totalMemory += memory;

        minGenTime = Mathf.Min(minGenTime, genTime);
        maxGenTime = Mathf.Max(maxGenTime, genTime);

        minFPS = Mathf.Min(minFPS, fps);
        maxFPS = Mathf.Max(maxFPS, fps);

        minMemory = Mathf.Min(minMemory, memory);
        maxMemory = Mathf.Max(maxMemory, memory);

        // stats log
        Debug.Log(
            $"[PERF] #{testCount} | " +
            $"GenTime:{genTime} ms | FPS:{fps:F1} | Memory:{memory} MB"
        );
    }

    void PrintFinalResult()
    {
        float avgGenTime = totalGenTime / testCount;
        float avgFPS = totalFPS / testCount;
        float avgMemory = totalMemory / testCount;

        Debug.Log("========= FINAL RESULT =========");

        Debug.Log($"Total Tests: {testCount}");
        Debug.Log($"Grid Size: {controller.size}");
        Debug.Log($"Difficulty: {controller.difficulty:F1}");
        Debug.Log($"Branches: {controller.enableBranches}");

        Debug.Log(
            $"GenTime -> Avg:{avgGenTime:F2} ms | Min:{minGenTime} | Max:{maxGenTime}"
        );

        Debug.Log(
            $"FPS     -> Avg:{avgFPS:F2} | Min:{minFPS:F1} | Max:{maxFPS:F1}"
        );

        Debug.Log(
            $"Memory  -> Avg:{avgMemory:F2} MB | Min:{minMemory} | Max:{maxMemory}"
        );
    }

    void Update()
    {
        frameCount++;
        fpsTimer += Time.deltaTime;

        if (fpsTimer >= 1f)
        {
            currentFPS = frameCount / fpsTimer;
            frameCount = 0;
            fpsTimer = 0f;
        }
    }
}