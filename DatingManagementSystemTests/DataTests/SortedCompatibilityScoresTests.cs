using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

namespace DatingManagementSystem.Tests
{
    [TestClass]
    public class SortedCompatibilityScoreTests
    {
        // Helper structure to simulate a compatibility score
        public class CompatibilityScore
        {
            public int User1Id { get; set; }
            public int User2Id { get; set; }
            public double Score { get; set; }
        }

        [TestMethod]
        public void InsertAndDeduplicateCompatibilityScores_WorksCorrectly()
        {
            int loggedInUserId = 1;
            var scores = new List<CompatibilityScore>
            {
                new CompatibilityScore { User1Id = 1, User2Id = 2, Score = 0.8 },
                new CompatibilityScore { User1Id = 2, User2Id = 1, Score = 0.8 }, // duplicate
                new CompatibilityScore { User1Id = 1, User2Id = 3, Score = 0.6 },
                new CompatibilityScore { User1Id = 1, User2Id = 4, Score = 0.0 }, // zero score
                new CompatibilityScore { User1Id = 5, User2Id = 1, Score = 0.7 },
            };

            Hashtable compatibilityScoresHashtable = new();
            HashSet<(int, int)> processedPairs = new();

            foreach (var score in scores)
            {
                int uid1 = score.User1Id;
                int uid2 = score.User2Id;
                var pairKey = uid1 < uid2 ? (uid1, uid2) : (uid2, uid1);

                if (!processedPairs.Contains(pairKey))
                {
                    int pairedUserId = uid1 == loggedInUserId ? uid2 : uid1;
                    compatibilityScoresHashtable[pairedUserId] = score.Score;
                    processedPairs.Add(pairKey);
                }
            }

            Assert.AreEqual(4, compatibilityScoresHashtable.Count); 
            Assert.AreEqual(0.8, compatibilityScoresHashtable[2]);
            Assert.AreEqual(0.6, compatibilityScoresHashtable[3]);
            Assert.AreEqual(0.7, compatibilityScoresHashtable[5]);
        }

        [TestMethod]
        public void MaxHeap_SortsScoresDescending()
        {
            var hashtable = new Hashtable
            {
                { 2, 0.8 },
                { 3, 0.6 },
                { 5, 0.7 },
                { 6, 0.01 } // below threshold
            };

            double threshold = 0.02;
            var maxHeap = new PriorityQueue<int, double>(Comparer<double>.Create((a, b) => b.CompareTo(a)));

            foreach (DictionaryEntry entry in hashtable)
            {
                int userId = (int)entry.Key;
                double score = entry.Value as double? ?? 0.0;

                if (score > threshold)
                {
                    maxHeap.Enqueue(userId, score);
                }
            }

            List<(int UserId, double Score)> sortedUsers = new();

            while (maxHeap.Count > 0)
            {
                maxHeap.TryDequeue(out int userId, out double score);
                sortedUsers.Add((userId, score));
            }

            var expectedOrder = new List<(int, double)>
            {
                (2, 0.8),
                (5, 0.7),
                (3, 0.6)
            };

            CollectionAssert.AreEqual(expectedOrder, sortedUsers);
        }
    }
}
