using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KitchenChaos.Tests.EditMode
{
    public sealed class EventLifecycleTests
    {
        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ResetAllStaticEvents();
        }

        [TearDown]
        public void TearDown()
        {
            ResetAllStaticEvents();
        }

        [Test]
        public void SubsystemRegistration_ClearsAllStaticEventSubscribers()
        {
            EventHandler listener = (_, _) => { };
            AddStaticEventListener("KitchenObjectHolder", "OnDrop", listener);
            AddStaticEventListener("KitchenObjectHolder", "OnPickup", listener);
            AddStaticEventListener("CuttingCounter", "OnCut", listener);
            AddStaticEventListener("TrashCounter", "OnObjectTrashed", listener);

            ResetAllStaticEvents();

            Assert.That(GetSubscriberCount("KitchenObjectHolder", "OnDrop"), Is.Zero);
            Assert.That(GetSubscriberCount("KitchenObjectHolder", "OnPickup"), Is.Zero);
            Assert.That(GetSubscriberCount("CuttingCounter", "OnCut"), Is.Zero);
            Assert.That(GetSubscriberCount("TrashCounter", "OnObjectTrashed"), Is.Zero);
        }

        [Test]
        public void SoundManager_DisableAndEnable_DoesNotDuplicateStaticSubscriptions()
        {
            GameObject managerObject = new GameObject("SoundManager Test");
            Behaviour manager = (Behaviour)managerObject.AddComponent(GetRuntimeType("SoundManager"));

            // EditMode 下 AddComponent 不会完整模拟 Play Mode 生命周期，因此显式调用配对方法。
            InvokeInstanceLifecycle(manager, "OnEnable");
            Assert.That(GetSubscriberCount("CuttingCounter", "OnCut"), Is.EqualTo(1));

            InvokeInstanceLifecycle(manager, "OnDisable");
            Assert.That(GetSubscriberCount("CuttingCounter", "OnCut"), Is.Zero);

            InvokeInstanceLifecycle(manager, "OnEnable");
            Assert.That(GetSubscriberCount("CuttingCounter", "OnCut"), Is.EqualTo(1));

            InvokeInstanceLifecycle(manager, "OnDisable");
            UnityEngine.Object.DestroyImmediate(managerObject);
            Assert.That(GetSubscriberCount("CuttingCounter", "OnCut"), Is.Zero);
        }

        [Test]
        public void LegacyClearStaticData_NoLongerUsesStartOrdering()
        {
            MethodInfo startMethod = GetRuntimeType("ClearStaticData").GetMethod(
                "Start",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

            Assert.That(startMethod, Is.Null);
        }

        private static void ResetAllStaticEvents()
        {
            InvokeStaticReset("KitchenObjectHolder", "ResetStaticEvents");
            InvokeStaticReset("CuttingCounter", "ResetStaticEvent");
            InvokeStaticReset("TrashCounter", "ResetStaticEvent");
        }

        private static void InvokeStaticReset(string ownerTypeName, string methodName)
        {
            Type ownerType = GetRuntimeType(ownerTypeName);
            MethodInfo resetMethod = ownerType.GetMethod(
                methodName,
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(resetMethod, Is.Not.Null, $"{ownerType.Name}.{methodName} is missing.");
            resetMethod.Invoke(null, null);
        }

        private static void AddStaticEventListener(
            string ownerTypeName,
            string eventName,
            EventHandler listener)
        {
            Type ownerType = GetRuntimeType(ownerTypeName);
            EventInfo eventInfo = ownerType.GetEvent(
                eventName,
                BindingFlags.Static | BindingFlags.Public);
            Assert.That(eventInfo, Is.Not.Null, $"{ownerType.Name}.{eventName} is missing.");
            eventInfo.AddEventHandler(null, listener);
        }

        private static void InvokeInstanceLifecycle(Behaviour target, string methodName)
        {
            MethodInfo lifecycleMethod = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(lifecycleMethod, Is.Not.Null, $"{target.GetType().Name}.{methodName} is missing.");
            lifecycleMethod.Invoke(target, null);
        }

        private static int GetSubscriberCount(string ownerTypeName, string eventName)
        {
            Type ownerType = GetRuntimeType(ownerTypeName);
            FieldInfo eventField = ownerType.GetField(
                eventName,
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(eventField, Is.Not.Null, $"{ownerType.Name}.{eventName} backing field is missing.");

            Delegate subscribers = eventField.GetValue(null) as Delegate;
            return subscribers?.GetInvocationList().Length ?? 0;
        }

        private static Type GetRuntimeType(string typeName)
        {
            Type runtimeType = Type.GetType($"{typeName}, Assembly-CSharp");
            Assert.That(runtimeType, Is.Not.Null, $"Runtime type {typeName} is missing.");
            return runtimeType;
        }
    }
}
