using System;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KitchenChaos.Tests.EditMode
{
    public sealed class CharacterControllerTests
    {
        private const string GameScenePath = "Assets/Scenes/2-GameScene.unity";

        [TestCase("Player")]
        [TestCase("AIPlayer")]
        public void PlayerObject_UsesOnlyCharacterControllerForCollision(string playerName)
        {
            Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            GameObject player = Array.Find(scene.GetRootGameObjects(), root => root.name == playerName);

            Assert.That(player, Is.Not.Null, $"Missing root object: {playerName}");
            Assert.That(player.GetComponent<CharacterController>(), Is.Not.Null);
            Assert.That(player.GetComponent<Rigidbody>(), Is.Null);
            Assert.That(player.GetComponent<CapsuleCollider>(), Is.Null);
        }
    }
}
