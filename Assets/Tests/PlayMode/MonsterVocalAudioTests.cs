#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

public class MonsterVocalAudioTests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static Type Gameplay(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    static object State(string name) => Enum.Parse(Gameplay("MonsterState"), name);
    static object Game(string name) => Enum.Parse(Gameplay("GameState"), name);
    static void Set(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);
    static object Get(object target, string name) => target.GetType().GetField(name, Private).GetValue(target);
    static void Call(object target, string name, params object[] args) => target.GetType().GetMethod(name,
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(target, args);

    sealed class Fixture : IDisposable
    {
        public readonly GameObject Root = new GameObject("Monster vocal fixture");
        public readonly AudioClip Clip = AudioClip.Create("Vocal fixture", 441000, 1, 44100, false);
        public readonly Component Audio;
        public readonly AudioSource Alert, Chase;
        public Fixture()
        {
            Root.SetActive(false);
            Alert = Source("Alert");
            Chase = Source("Chase");
            Audio = Root.AddComponent(Gameplay("MonsterAudioManager"));
            Set(Audio, "AlertSoundsObject", Alert.gameObject);
            Set(Audio, "ChaseAudioSource", Chase);
            Root.SetActive(true);
        }
        AudioSource Source(string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(Root.transform);
            var source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.clip = Clip;
            return source;
        }
        public void Update(string state, string game = "Playing") => Call(Audio, "UpdateVocalState", State(state), Game(game));
        public void Dispose()
        {
            UnityEngine.Object.DestroyImmediate(Root);
            UnityEngine.Object.DestroyImmediate(Clip);
        }
    }

    [UnityTest]
    public IEnumerator ChaseLoopWaitsPausesResumesAndStopsWithoutRestarts()
    {
        using (var f = new Fixture())
        {
            Call(f.Audio, "PlayAlertSound");
            f.Update("Chasing");
            Assert.That(f.Alert.isPlaying, Is.True);
            Assert.That(f.Chase.isPlaying, Is.False, "Alert must finish before chase loop");
            f.Update("Chasing", "Paused");
            Assert.That(f.Alert.isPlaying, Is.False);
            Assert.That((bool)Get(f.Audio, "_alertPaused"), Is.True);
            f.Update("Chasing");
            Assert.That(f.Alert.isPlaying, Is.True);
            f.Alert.Stop(); // Simulate alert completion without waiting for the recording.
            f.Update("Chasing");
            Assert.That(f.Chase.isPlaying && f.Chase.loop, Is.True);
            f.Chase.timeSamples = 44100;
            for (int i = 0; i < 10; i++)
            {
                f.Update("Searching");
                Assert.That(f.Chase.isPlaying, Is.False);
                f.Update("Chasing");
                Assert.That(f.Chase.isPlaying, Is.True);
                Assert.That(f.Chase.timeSamples, Is.GreaterThanOrEqualTo(44100), "Reacquisition must not rewind");
            }
            f.Update("Attacking");
            Assert.That(f.Chase.isPlaying, Is.False);
            f.Update("Chasing");
            f.Update("Chasing", "Paused");
            Assert.That(f.Chase.isPlaying, Is.False);
            f.Update("Chasing");
            Assert.That(f.Chase.timeSamples, Is.GreaterThanOrEqualTo(44100));
            f.Update("Roaming");
            Assert.That(f.Chase.isPlaying, Is.False);
            Assert.That(f.Chase.timeSamples, Is.Zero);
            f.Update("Chasing");
            f.Update("Chasing", "GameOver");
            Assert.That(f.Chase.isPlaying, Is.False);
            Assert.That((bool)Get(f.Audio, "_chaseStarted"), Is.False);
            f.Update("Chasing");
            f.Root.SetActive(false);
            Assert.That(f.Chase.isPlaying || f.Alert.isPlaying, Is.False);
            Assert.That((bool)Get(f.Audio, "_chaseStarted"), Is.False);
            f.Root.SetActive(true);
            Set(f.Audio, "ChaseAudioSource", null);
            Set(f.Audio, "_alertSoundSources", new List<AudioSource>());
            Assert.DoesNotThrow(() => { Call(f.Audio, "PlayAlertSound"); f.Update("Chasing"); });
            yield return null;
        }
    }

    [UnityTest]
    public IEnumerator AlertEligibilitySurvivesSightChangesCooldownAndDisable()
    {
        var settings = NavMesh.GetSettingsByID(0);
        var sources = new List<NavMeshBuildSource> { new NavMeshBuildSource {
            shape = NavMeshBuildSourceShape.Box, size = new Vector3(30, 1, 30),
            transform = Matrix4x4.TRS(new Vector3(1500, -0.5f, 1500), Quaternion.identity, Vector3.one) } };
        var data = NavMeshBuilder.BuildNavMeshData(settings, sources,
            new Bounds(new Vector3(1500, 0, 1500), new Vector3(40, 10, 40)), Vector3.zero, Quaternion.identity);
        var handle = NavMesh.AddNavMeshData(data);
        using (var f = new Fixture())
        {
            GameObject monsterRoot = new GameObject("Alert transition fixture");
            GameObject managerRoot = new GameObject("Audio game state fixture");
            try
            {
                monsterRoot.transform.position = new Vector3(1500, 0, 1500);
                var monster = (Behaviour)monsterRoot.AddComponent(Gameplay("MonsterController"));
                monster.enabled = false;
                monsterRoot.GetComponent<NavMeshAgent>().Warp(monsterRoot.transform.position);
                var manager = managerRoot.AddComponent(Gameplay("GameManager"));
                monster.GetType().GetProperty("GameManager").SetValue(monster, manager);
                Set(monster, "MonsterAudioManager", f.Audio);
                void Change(string state) => Call(monster, "ChangeState", State(state));
                Change("Chasing");
                Assert.That(f.Alert.isPlaying, Is.True);
                Assert.That((float)Get(monster, "_alertCooldownRemaining"), Is.EqualTo(15f));
                f.Alert.Stop();
                Change("Searching"); Change("Chasing"); Change("Attacking"); Change("Chasing");
                Assert.That(f.Alert.isPlaying, Is.False);
                Change("Roaming"); Change("Chasing");
                Assert.That(f.Alert.isPlaying, Is.False, "New encounter inside cooldown stays silent");
                Set(monster, "_alertCooldownRemaining", 0f);
                Call(monster, "Update");
                Assert.That(f.Alert.isPlaying, Is.False, "Expiry must not play a delayed alert");
                Change("Roaming"); Change("Chasing");
                Assert.That(f.Alert.isPlaying, Is.True, "Later eligible encounter plays again");
                manager.GetType().GetField("currentState").SetValue(manager, Game("Paused"));
                float remaining = (float)Get(monster, "_alertCooldownRemaining");
                yield return null;
                Call(monster, "Update");
                Assert.That((float)Get(monster, "_alertCooldownRemaining"), Is.EqualTo(remaining));
                monsterRoot.SetActive(false); monsterRoot.SetActive(true);
                Assert.That((float)Get(monster, "_alertCooldownRemaining"), Is.EqualTo(remaining));
                Assert.That(f.Alert.isPlaying || f.Chase.isPlaying, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(monsterRoot);
                UnityEngine.Object.DestroyImmediate(managerRoot);
                handle.Remove();
                UnityEngine.Object.DestroyImmediate(data);
            }
        }
    }
}
#endif
