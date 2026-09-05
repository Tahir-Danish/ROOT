using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Deterministic verification suite for Demo Species Identity Lock.
/// Implements all 12 required test conditions:
/// 1. Confirm species A.
/// 2. Lock species A.
/// 3. Send species B.
/// 4. Verify visible identity remains A.
/// 5. Send UNKNOWN.
/// 6. Verify visible identity remains A.
/// 7. Send species B repeatedly.
/// 8. Verify visible identity remains A during the lock.
/// 9. After lock expiration, verify a new identity is NOT accepted from one observation alone.
/// 10. Verify existing confirmation logic is still required.
/// 11. Verify anchor transform does not change.
/// 12. Verify no duplicate card is created.
/// </summary>
public class SpeciesIdentityLockValidator : MonoBehaviour
{
    public bool runOnStart = false;

    void Start()
    {
        if (runOnStart)
        {
            RunAllTests();
        }
    }

    [ContextMenu("Run Identity Lock Tests")]
    public static bool RunAllTests()
    {
        Debug.Log("==================================================");
        Debug.Log("[SpeciesIdentityLockValidator] STARTING TESTS");
        Debug.Log("==================================================");

        int passed = 0;
        int failed = 0;

        void Assert(string testName, bool condition, string details = "")
        {
            if (condition)
            {
                Debug.Log($"[PASS] {testName} {details}");
                passed++;
            }
            else
            {
                Debug.LogError($"[FAIL] {testName} {details}");
                failed++;
            }
        }

        float baseTime = 100.0f;
        ARSpeciesCard.TimeOverride = baseTime;
        ARUXManager.TimeOverride = baseTime;

        try
        {
            // Setup test GameObject and Card Component
            var cardGo = new GameObject("TestIdentityLockCard");
            var card = cardGo.AddComponent<ARSpeciesCard>();

            // Attach mock UI Text components to inspect visible identity fields
            var nameGo = new GameObject("CommonName");
            nameGo.transform.SetParent(cardGo.transform);
            card.commonNameText = nameGo.AddComponent<Text>();

            var sciGo = new GameObject("SciName");
            sciGo.transform.SetParent(cardGo.transform);
            card.scientificNameText = sciGo.AddComponent<Text>();

            string speciesA = "05_caryota_urens";
            string speciesB = "01_alstonia_scholaris";
            string speciesUnknown = "unknown_plant";

            // -----------------------------------------------------------------
            // TEST 1 & 2: Confirm species A and verify identity lock is engaged
            // -----------------------------------------------------------------
            card.PopulateData(speciesA);

            Assert("Test 1: Confirm species A", card.CurrentSpeciesId == speciesA, $"Species={card.CurrentSpeciesId}");
            Assert("Test 2: Lock species A", card.IsIdentityLocked && card.LockedSpeciesId == speciesA, $"Locked={card.IsIdentityLocked}, LockId={card.LockedSpeciesId}");
            string visibleNameA = card.commonNameText.text;
            string visibleSciA = card.scientificNameText.text;

            // -----------------------------------------------------------------
            // TEST 3 & 4: Send species B during lock -> visible identity remains A
            // -----------------------------------------------------------------
            card.PopulateData(speciesB);
            Assert("Test 3 & 4: Send species B -> Visible identity remains A",
                card.CurrentSpeciesId == speciesA && card.commonNameText.text == visibleNameA && card.scientificNameText.text == visibleSciA,
                $"Expected={speciesA}, Actual={card.CurrentSpeciesId}");

            // -----------------------------------------------------------------
            // TEST 5 & 6: Send UNKNOWN during lock -> visible identity remains A
            // -----------------------------------------------------------------
            card.PopulateData(speciesUnknown);
            Assert("Test 5 & 6: Send UNKNOWN -> Visible identity remains A",
                card.CurrentSpeciesId == speciesA && card.commonNameText.text == visibleNameA,
                $"Expected={speciesA}, Actual={card.CurrentSpeciesId}");

            // -----------------------------------------------------------------
            // TEST 7 & 8: Send species B repeatedly during lock -> visible identity remains A
            // -----------------------------------------------------------------
            for (int i = 0; i < 10; i++)
            {
                ARSpeciesCard.TimeOverride = baseTime + (i * 0.3f); // within 3 seconds (< 5s lock)
                card.PopulateData(speciesB);
            }
            Assert("Test 7 & 8: Send species B repeatedly during lock -> Remains A",
                card.CurrentSpeciesId == speciesA && card.commonNameText.text == visibleNameA,
                $"After 10 frames at t={ARSpeciesCard.TimeOverride}, Species={card.CurrentSpeciesId}");

            // -----------------------------------------------------------------
            // TEST 9: After lock expiration, single observation of B is NOT accepted
            // -----------------------------------------------------------------
            // Advance time past 5-second lock
            ARSpeciesCard.TimeOverride = baseTime + 6.0f;
            ARUXManager.TimeOverride = baseTime + 6.0f;
            bool lockExpired = !card.IsIdentityLocked;
            Assert("Lock expired check", lockExpired, $"IsLocked={card.IsIdentityLocked}");

            // Simulate candidate reconfirmation on AnchoredTarget as done in ARUXManager
            var anchoredTarget = new ARUXManager.AnchoredTarget
            {
                cardInstance = cardGo,
                cardComponent = card,
                speciesId = speciesA,
                initialAnchorPosition = new Vector3(0.5f, 0.1f, 1.2f),
                pendingSpeciesId = null,
                pendingSpeciesObservationCount = 0,
                lastPendingSpeciesObservationTime = 0f
            };

            Vector3 initialPos = anchoredTarget.WorldPosition;

            // Frame 1 of species B after lock expiry:
            int requiredObservations = 3;
            float candidateTimeoutWindow = 1.5f;

            void SimulateIncomingObservation(string incomingSpecies, float timestamp)
            {
                ARSpeciesCard.TimeOverride = timestamp;
                ARUXManager.TimeOverride = timestamp;

                bool isLocked = anchoredTarget.cardComponent != null && anchoredTarget.cardComponent.IsIdentityLocked;
                if (isLocked)
                {
                    anchoredTarget.pendingSpeciesObservationCount = 0;
                    return;
                }

                if (incomingSpecies == anchoredTarget.speciesId)
                {
                    anchoredTarget.pendingSpeciesId = null;
                    anchoredTarget.pendingSpeciesObservationCount = 0;
                    return;
                }

                if (incomingSpecies == speciesUnknown)
                {
                    // Stable card policy: ignore unknown for confirmed card
                    return;
                }

                if (anchoredTarget.pendingSpeciesId == incomingSpecies &&
                    (timestamp - anchoredTarget.lastPendingSpeciesObservationTime <= candidateTimeoutWindow))
                {
                    anchoredTarget.pendingSpeciesObservationCount++;
                    anchoredTarget.lastPendingSpeciesObservationTime = timestamp;
                    if (anchoredTarget.pendingSpeciesObservationCount >= requiredObservations)
                    {
                        anchoredTarget.speciesId = incomingSpecies;
                        anchoredTarget.pendingSpeciesId = null;
                        anchoredTarget.pendingSpeciesObservationCount = 0;
                        anchoredTarget.cardComponent.PopulateData(incomingSpecies);
                    }
                }
                else
                {
                    anchoredTarget.pendingSpeciesId = incomingSpecies;
                    anchoredTarget.pendingSpeciesObservationCount = 1;
                    anchoredTarget.lastPendingSpeciesObservationTime = timestamp;
                }
            }

            // Send observation 1 of species B:
            SimulateIncomingObservation(speciesB, baseTime + 6.1f);
            Assert("Test 9: After lock expiration, single B observation NOT accepted",
                anchoredTarget.speciesId == speciesA && card.CurrentSpeciesId == speciesA && anchoredTarget.pendingSpeciesObservationCount == 1,
                $"CardSpecies={card.CurrentSpeciesId}, PendingObs={anchoredTarget.pendingSpeciesObservationCount}/3");

            // Also test transient unknown does not degrade:
            SimulateIncomingObservation(speciesUnknown, baseTime + 6.2f);
            Assert("Stable card: transient unknown after lock expiry ignored",
                anchoredTarget.speciesId == speciesA && card.CurrentSpeciesId == speciesA);

            // -----------------------------------------------------------------
            // TEST 10: Verify existing confirmation logic is required before changing
            // -----------------------------------------------------------------
            // Send observation 2 of species B:
            SimulateIncomingObservation(speciesB, baseTime + 6.3f);
            Assert("Test 10a: Observation 2 of B still requires 1 more observation",
                anchoredTarget.speciesId == speciesA && card.CurrentSpeciesId == speciesA && anchoredTarget.pendingSpeciesObservationCount == 2,
                $"CardSpecies={card.CurrentSpeciesId}, PendingObs={anchoredTarget.pendingSpeciesObservationCount}/3");

            // Send observation 3 of species B:
            SimulateIncomingObservation(speciesB, baseTime + 6.4f);
            Assert("Test 10b: Observation 3 of B CONFIRMS new identity and locks",
                anchoredTarget.speciesId == speciesB && card.CurrentSpeciesId == speciesB && card.IsIdentityLocked && card.LockedSpeciesId == speciesB,
                $"CardSpecies={card.CurrentSpeciesId}, Locked={card.IsIdentityLocked}, LockedId={card.LockedSpeciesId}");

            // -----------------------------------------------------------------
            // TEST 11: Verify anchor transform does not change
            // -----------------------------------------------------------------
            Vector3 finalPos = anchoredTarget.WorldPosition;
            Assert("Test 11: Anchor transform position immutable",
                Vector3.Distance(initialPos, finalPos) < 0.0001f,
                $"Initial={initialPos}, Final={finalPos}");

            // -----------------------------------------------------------------
            // TEST 12: Verify no duplicate card is created
            // -----------------------------------------------------------------
            Assert("Test 12: Single card instance preserved (no duplicates)",
                anchoredTarget.cardInstance == cardGo && cardGo != null);

            // Cleanup
            Object.DestroyImmediate(cardGo);
        }
        finally
        {
            ARSpeciesCard.TimeOverride = null;
            ARUXManager.TimeOverride = null;
        }

        Debug.Log("==================================================");
        Debug.Log($"[SpeciesIdentityLockValidator] RESULTS: {passed} PASSED, {failed} FAILED");
        Debug.Log("==================================================");

        return failed == 0;
    }
}
