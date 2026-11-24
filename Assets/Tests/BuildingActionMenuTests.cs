using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ResortEmpire;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class BuildingActionMenuTests
{
    private Scene island;
    private GameObject actions;
    private GameObject staffScreen;
    private SimulationRunner runner;
    private int originalCoins;

    [UnitySetUp]
    public IEnumerator LoadIsland()
    {
        originalCoins = ResourceValues.getCoins();
        yield return new EnterPlayMode();
        const string path = "Assets/Scenes/IslandScene.unity";
        var loading = EditorSceneManager.LoadSceneAsyncInPlayMode(path,
            new LoadSceneParameters(LoadSceneMode.Single));
        while (!loading.isDone)
            yield return null;
        yield return null; // Allow Start (including the map camera fitter) to run.
        island = SceneManager.GetSceneByPath(path);
        var roots = island.GetRootGameObjects();
        runner = roots.SelectMany(root => root.GetComponentsInChildren<SimulationRunner>()).Single();
        var screen = roots.Single(root => root.GetComponent("WorkerAllocationScreen") != null);
        actions = screen.transform.Find("BuildingActions").gameObject;
        staffScreen = screen.transform.Find("Content").gameObject;
    }

    [UnityTearDown]
    public IEnumerator LeaveIsland()
    {
        yield return new ExitPlayMode();
        ResourceValues.addCoins(originalCoins - ResourceValues.getCoins());
    }

    private static void Tap(GameObject target)
    {
        var tap = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
        Assert.IsTrue(ExecuteEvents.Execute(target, tap, ExecuteEvents.pointerClickHandler));
    }

    private static void TapAt(GameObject target, Vector2 position)
    {
        var pointer = new PointerEventData(EventSystem.current) { position = position };
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, hits);
        Assert.IsNotEmpty(hits, target.name);
        var handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
        Assert.AreSame(target, handler, $"Tap on {target.name}. Hits in order: " +
            string.Join(", ", hits.Select(hit => hit.gameObject.name)));
        Tap(handler);
    }

    private void TapBuilding(string id)
    {
        var building = island.GetRootGameObjects().Single(root => root.name == id);
        Canvas.ForceUpdateCanvases();
        Physics2D.SyncTransforms();
        TapAt(building, Camera.main.WorldToScreenPoint(building.GetComponent<Collider2D>().bounds.center));
    }

    private static void TapButton(Button button)
    {
        Canvas.ForceUpdateCanvases();
        var rect = (RectTransform)button.transform;
        TapAt(button.gameObject, RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)));
    }

    private Button ActionButton(string name) =>
        actions.transform.Find("ActionSheet/" + name).GetComponent<Button>();

    [UnityTest]
    public IEnumerator BeforeWaveMenuOffersStaffForSelectedBuildingAndUpgradeIsDisabled()
    {
        TapBuilding("BeachBar");
        yield return null; // The new canvas graphics participate in raycasts after the frame is rendered.
        Assert.IsTrue(actions.activeInHierarchy);
        Assert.IsFalse(staffScreen.activeInHierarchy);
        var upgrade = ActionButton("Upgrade");
        Assert.IsTrue(upgrade.gameObject.activeInHierarchy);
        Assert.IsFalse(upgrade.IsInteractable());
        int coins = ResourceValues.getCoins();
        TapButton(upgrade);
        Assert.AreEqual(coins, ResourceValues.getCoins());
        Assert.IsTrue(actions.activeInHierarchy);

        TapButton(ActionButton("AssignStaff"));
        Assert.IsFalse(actions.activeInHierarchy);
        Assert.IsTrue(staffScreen.activeInHierarchy);
        // BeachBar is the empty destination. Select a staffed source through its real S3 card.
        var card = staffScreen.GetComponentsInChildren<Button>()
            .First(button => button.GetComponentsInParent<MonoBehaviour>().Any(c => c.GetType().Name == "BuildingCard"));
        Tap(card.gameObject);
        Assert.IsTrue(runner.HasWorker("BeachBar"));
        Assert.IsFalse(staffScreen.activeInHierarchy);
    }

    [UnityTest]
    public IEnumerator CloseDismissesOnlyTheMenuAndANewTapCanOpenItAgain()
    {
        TapBuilding("Reception");
        yield return null;
        TapButton(ActionButton("Close"));
        Assert.IsFalse(actions.activeInHierarchy);
        Assert.IsFalse(staffScreen.activeInHierarchy);
        Assert.IsTrue(runner.HasWorker("Reception"));
        TapBuilding("Restaurant");
        Assert.IsTrue(actions.activeInHierarchy);
    }

    [Test]
    public void StartingWaveClosesMenuAndInWaveTapOpensOnlyS3()
    {
        TapBuilding("Reception");
        runner.StartNextWave();
        Assert.AreEqual(GameState.WaveInProgress, runner.CurrentState);
        Assert.IsFalse(actions.activeInHierarchy);
        TapBuilding("Reception");
        Assert.IsTrue(staffScreen.activeInHierarchy);
        Assert.IsFalse(ActionButton("Upgrade").gameObject.activeInHierarchy);
    }

    [TestCase(852f, 393f)]
    [TestCase(480f, 320f)]
    public void MapTargetsAndMenuControlsMeetMinimumTouchSize(float widthDp, float heightDp)
    {
        var camera = Camera.main;
        camera.aspect = widthDp / heightDp;
        camera.GetComponent("IslandCameraFitter").SendMessage("Start");
        float dpPerWorldUnit = heightDp / (2f * camera.orthographicSize);
        foreach (string id in new[] { "Reception", "Restaurant", "BeachBar" })
        {
            var bounds = island.GetRootGameObjects().Single(root => root.name == id).GetComponent<Collider2D>().bounds;
            Assert.GreaterOrEqual(bounds.size.x * dpPerWorldUnit, 48f, id);
            Assert.GreaterOrEqual(bounds.size.y * dpPerWorldUnit, 48f, id);
        }
        var scaler = actions.GetComponentInParent<CanvasScaler>(true);
        var reference = scaler.referenceResolution;
        float dpPerCanvasUnit = Mathf.Pow(widthDp / reference.x, 1f - scaler.matchWidthOrHeight)
            * Mathf.Pow(heightDp / reference.y, scaler.matchWidthOrHeight);
        foreach (var button in actions.GetComponentsInChildren<Button>(true))
        {
            var rect = ((RectTransform)button.transform).rect;
            Assert.GreaterOrEqual(rect.width * dpPerCanvasUnit, 48f, button.name);
            Assert.GreaterOrEqual(rect.height * dpPerCanvasUnit, 48f, button.name);
        }
    }
}
