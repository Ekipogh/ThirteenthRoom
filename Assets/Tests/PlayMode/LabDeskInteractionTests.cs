#if UNITY_EDITOR
using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class LabDeskInteractionTests
{
    const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
    GameObject root;
    Component desk, player, score;
    ScriptableObject berry, potion;
    object inventory;
    Type deskType, inventoryType;

    void Set(string name, object value) => deskType.GetField(name, Fields).SetValue(desk, value);
    void Call(string name, params object[] args) => deskType.GetMethod(name).Invoke(desk, args);
    void AddBerry() => inventoryType.GetMethod("AddItem").Invoke(inventory, new object[] { berry });
    bool Has(ScriptableObject item) => (bool)inventoryType.GetMethod("HasItem").Invoke(inventory, new object[] { item });
    void Begin() => Call("BeginHoldInteract", player);
    void Hold(float seconds) => Call("HoldInteract", player, seconds);
    void End() => Call("EndHoldInteract", player);
    float Score => (float)score.GetType().GetProperty("CurrentScore").GetValue(score);

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("Brewing fixture");
        player = root.AddComponent(Type.GetType("PlayerInteractor, Assembly-CSharp", true));
        ((Behaviour)player).enabled = false;
        inventory = player.GetType().GetProperty("Inventory").GetValue(player);
        inventoryType = inventory.GetType();
        deskType = Type.GetType("LabDeskInteractable, Assembly-CSharp", true);
        desk = root.AddComponent(deskType);
        score = root.AddComponent(Type.GetType("ScoreManager, Assembly-CSharp", true));
        var itemType = Type.GetType("ItemDefinition, Assembly-CSharp", true);
        berry = ScriptableObject.CreateInstance(itemType);
        potion = ScriptableObject.CreateInstance(itemType);
        Set("berryDef", berry);
        Set("potionDef", potion);
        Call("BindScoreManager", score);
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(root);
        UnityEngine.Object.DestroyImmediate(berry);
        UnityEngine.Object.DestroyImmediate(potion);
    }

    [Test]
    public void CompletionConsumesOneBerryAndRequiresAnotherPress()
    {
        AddBerry(); AddBerry();
        Begin(); Hold(4.9f);
        Assert.That(Has(potion), Is.False);
        Hold(0.2f); Hold(20f); Begin(); Hold(20f);
        Assert.That(Has(potion), Is.True);
        Assert.That(Has(berry), Is.True);
        Assert.That(Score, Is.EqualTo(50f));
        End(); Begin(); Hold(5f);
        Assert.That(Has(berry), Is.False);
        Assert.That(Score, Is.EqualTo(100f));
    }

    [Test]
    public void ReleaseAndDisableDiscardPartialProgressWithoutConsumingBerry()
    {
        AddBerry(); Begin(); Hold(4f); End();
        Begin(); Hold(1f);
        Assert.That(Has(potion), Is.False);
        ((Behaviour)desk).enabled = false;
        ((Behaviour)desk).enabled = true;
        Hold(10f);
        Assert.That(Has(berry), Is.True);
        Assert.That(Score, Is.Zero);
        Begin(); Hold(1f);
        Assert.That(Has(potion), Is.False);
        Hold(4f);
        Assert.That(Score, Is.EqualTo(50f));
    }

    [Test]
    public void AssignedAudioStopsOnReleaseCompletionAndDisable()
    {
        var clip = AudioClip.Create("Brewing fixture audio", 44100, 1, 44100, false);
        try
        {
            var source = root.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.clip = clip;
            Set("PotionMakingAudio", source);
            AddBerry(); AddBerry();
            Begin();
            Assert.That(source.isPlaying, Is.True);
            End();
            Assert.That(source.isPlaying, Is.False);
            Begin(); Hold(5f);
            Assert.That(source.isPlaying, Is.False);
            End(); Begin();
            Assert.That(source.isPlaying, Is.True);
            ((Behaviour)desk).enabled = false;
            Assert.That(source.isPlaying, Is.False);
        }
        finally { UnityEngine.Object.DestroyImmediate(clip); }
    }

    [Test]
    public void LaboratoryContainsConfiguredBrewingDesks()
    {
        var lab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/Building/CR_Laboratory.prefab");
        var desks = lab.GetComponentsInChildren(deskType, true);
        Assert.That(desks.Length, Is.GreaterThan(0));
        foreach (var component in desks)
        {
            Assert.That(deskType.GetField("berryDef", Fields).GetValue(component), Is.Not.Null);
            Assert.That(deskType.GetField("potionDef", Fields).GetValue(component), Is.Not.Null);
            Assert.That(component.GetComponent<Collider>(), Is.Not.Null);
        }
    }

    [Test]
    public void MissingOrRemovedIngredientsAndMissingOutputCannotReward()
    {
        Begin(); Hold(10f);
        Assert.That(Score, Is.Zero);
        AddBerry(); Begin(); Hold(4f);
        inventoryType.GetMethod("RemoveItem").Invoke(inventory, new object[] { berry });
        Hold(1f);
        Assert.That(Has(potion), Is.False);
        AddBerry(); Set("potionDef", null);
        Begin(); Hold(10f);
        Assert.That(Has(berry), Is.True);
        Assert.That(Score, Is.Zero);
    }
}
#endif
