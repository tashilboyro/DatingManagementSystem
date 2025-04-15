using Microsoft.VisualStudio.TestTools.UnitTesting;
using DatingManagementSystem.Controllers;
using DatingManagementSystem.Models;
using System;

namespace DatingManagementSystem.Tests
{
    [TestClass]
    public class JaccardSimilarityTests
    {
        // Test for Jaccard Similarity
        [TestMethod]
        public void JaccardSimilarity_CommonInterests_ReturnsCorrectScore()
        {
            // Arrange
            var controller = new UsersController(null!, null!, null!);
            string interests1 = "music, travel, reading";
            string interests2 = "reading, cooking, music";

            // Act
            double result = InvokeJaccard(controller, interests1, interests2);

            // Assert
            double expected = 2.0 / 4.0; // reading + music = 2, total unique = 4
            Assert.AreEqual(expected, result, 0.0001);
        }

        [TestMethod]
        public void JaccardSimilarity_NoOverlap_ReturnsZero()
        {
            var controller = new UsersController(null!, null!, null!);
            string interests1 = "sports, hiking";
            string interests2 = "coding, painting";

            double result = InvokeJaccard(controller, interests1, interests2);

            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void JaccardSimilarity_EmptyInterests_ReturnsZero()
        {
            var controller = new UsersController(null!, null!, null!);

            double result = InvokeJaccard(controller, "", "reading");

            Assert.AreEqual(0, result);
        }

        private double InvokeJaccard(UsersController controller, string i1, string i2)
        {
            var method = typeof(UsersController).GetMethod("CalculateJaccardSimilarity",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            return (double)method!.Invoke(controller, new object[] { i1, i2 })!;
        }
    }
}
