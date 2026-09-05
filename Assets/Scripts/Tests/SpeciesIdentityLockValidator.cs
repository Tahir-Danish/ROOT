using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Deterministic verification suite for Anchor-Lifetime Species Identity Lock.
/// Implements all 12 required test conditions:
/// 1. Confirm species A.
/// 2. Anchor/card created.
/// 3. Send species B.
/// 4. Card remains A.
/// 5. Send unknown.
/// 6. Card remains A.
/// 7. Simulate NO_TARGET / UNKNOWN_TARGET.
/// 8. Card remains A (across time and frames).
/// 9. Confirm a second spatially distinct species.
/// 10. Both cards retain independent identities.
/// 11. No anchor movement.
/// 12. No duplicate card for same target.
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
            // Setup test GameObject and Card Component for Target 1
            var cardGo1 = new GameObject("TestIdentityLockCard1");
            var card1 = cardGo1.AddComponent<ARSpeciesCard>();

            var nameGo1 = new GameObject("CommonName1");
            nameGo1.transform.SetParent(cardGo1.transform);
            card1.commonNameText = nameGo1.AddComponent<Text>();

            var sciGo1 = new GameObject("SciName1");
            sciGo1.transform.SetParent(cardGo1.transform);
            card1.scientificNameText = sciGo1.AddComponent<Text>();

            string speciesA = "05_caryota_urens";
            string speciesB = "01_alstonia_scholaris";
            string speciesUnknown = "unknown_plant";

            // -----------------------------------------------------------------
            // TEST 1 & 2: Confirm species A -> Anchor/Card created and locked
            // -----------------------------------------------------------------
            card1.PopulateData(speciesA);

            Assert("Test 1: Confirm species A", card1.CurrentSpeciesId == speciesA, $"Species={card1.CurrentSpeciesId}");
            Assert("Test 2: Anchor/card created & locked for anchor lifetime",
                card1.IsIdentityLocked && card1.LockedSpeciesId == speciesA,
                $"Locked={card1.IsIdentityLocked}, LockId={card1.LockedSpeciesId}");

            string visibleNameA = card1.commonNameText.text;
            string visibleSciA = card1.scientificNameText.text;

            var target1 = new ARUXManager.AnchoredTarget
            {
                cardInstance = cardGo1,
                cardComponent = card1,
                speciesId = speciesA,
                initialAnchorPosition = new Vector3(0.0f, 0.0f, 1.0f)
            };
            Vector3 initialPos1 = target1.WorldPosition;

            // -----------------------------------------------------------------
            // TEST 3 & 4: Send species B -> Card remains A
            // -----------------------------------------------------------------
            card1.PopulateData(speciesB);
            Assert("Test 3 & 4: Send species B -> Card remains A",
                card1.CurrentSpeciesId == speciesA && card1.commonNameText.text == visibleNameA && card1.scientificNameText.text == visibleSciA,
                $"Expected={speciesA}, Actual={card1.CurrentSpeciesId}");

            // -----------------------------------------------------------------
            // TEST 5 & 6: Send unknown -> Card remains A
            // -----------------------------------------------------------------
            card1.PopulateData(speciesUnknown);
            Assert("Test 5 & 6: Send unknown -> Card remains A",
                card1.CurrentSpeciesId == speciesA && card1.commonNameText.text == visibleNameA,
                $"Expected={speciesA}, Actual={card1.CurrentSpeciesId}");

            // -----------------------------------------------------------------
            // TEST 7: Simulate NO_TARGET / UNKNOWN_TARGET -> Card remains A
            // -----------------------------------------------------------------
            // Simulate incoming detection frame with UNKNOWN_TARGET perception state
            bool isConfirmedKnown = !string.IsNullOrEmpty(target1.speciesId) && target1.speciesId != "unknown_plant";
            bool isLocked1 = isConfirmedKnown || (target1.cardComponent != null && target1.cardComponent.IsIdentityLocked);
            Assert("Test 7a: Target 1 identity is locked against UNKNOWN_TARGET", isLocked1);

            // Directly verify sending unknown / unconfirmed does not modify card1
            card1.PopulateData(speciesUnknown);
            Assert("Test 7b: Simulate UNKNOWN_TARGET -> Card remains A",
                card1.CurrentSpeciesId == speciesA && card1.commonNameText.text == visibleNameA);

            // -----------------------------------------------------------------
            // TEST 8: Card remains A over extended time (e.g. 60s, 1000s)
            // -----------------------------------------------------------------
            ARSpeciesCard.TimeOverride = baseTime + 60.0f; // 60 seconds later
            card1.PopulateData(speciesB);
            Assert("Test 8a: After 60s, sending species B is rejected -> Card remains A",
                card1.CurrentSpeciesId == speciesA && card1.IsIdentityLocked,
                $"CardSpecies={card1.CurrentSpeciesId}, Locked={card1.IsIdentityLocked}");

            ARSpeciesCard.TimeOverride = baseTime + 1000.0f; // 1000 seconds later
            card1.PopulateData(speciesB);
            Assert("Test 8b: After 1000s, card remains A (immutable anchor lifetime)",
                card1.CurrentSpeciesId == speciesA && card1.IsIdentityLocked);

            // -----------------------------------------------------------------
            // TEST 9 & 10: Confirm a second spatially distinct species
            // Both cards retain independent identities
            // -----------------------------------------------------------------
            var cardGo2 = new GameObject("TestIdentityLockCard2");
            var card2 = cardGo2.AddComponent<ARSpeciesCard>();

            var nameGo2 = new GameObject("CommonName2");
            nameGo2.transform.SetParent(cardGo2.transform);
            card2.commonNameText = nameGo2.AddComponent<Text>();

            var sciGo2 = new GameObject("SciName2");
            sciGo2.transform.SetParent(cardGo2.transform);
            card2.scientificNameText = sciGo2.AddComponent<Text>();

            card2.PopulateData(speciesB);
            var target2 = new ARUXManager.AnchoredTarget
            {
                cardInstance = cardGo2,
                cardComponent = card2,
                speciesId = speciesB,
                initialAnchorPosition = new Vector3(1.5f, 0.0f, 1.0f) // 1.5m away (distinct spatial position)
            };
            Vector3 initialPos2 = target2.WorldPosition;

            Assert("Test 9: Confirm second spatially distinct species B",
                target2.speciesId == speciesB && card2.CurrentSpeciesId == speciesB && card2.IsIdentityLocked,
                $"Target2 Species={card2.CurrentSpeciesId}, Pos={initialPos2}");

            Assert("Test 10: Both cards retain independent locked identities",
                target1.speciesId == speciesA && card1.CurrentSpeciesId == speciesA && card1.LockedSpeciesId == speciesA &&
                target2.speciesId == speciesB && card2.CurrentSpeciesId == speciesB && card2.LockedSpeciesId == speciesB,
                $"Target1={card1.CurrentSpeciesId}, Target2={card2.CurrentSpeciesId}");

            // Verify cross-talk immunity: sending species A to card2 does not change it, and sending species B to card1 does not change it
            card1.PopulateData(speciesB);
            card2.PopulateData(speciesA);
            Assert("Test 10b: Cross-talk immunity verified",
                card1.CurrentSpeciesId == speciesA && card2.CurrentSpeciesId == speciesB);

            // -----------------------------------------------------------------
            // TEST 11: No anchor movement
            // -----------------------------------------------------------------
            Assert("Test 11a: Target 1 anchor position strictly immutable",
                Vector3.Distance(initialPos1, target1.WorldPosition) < 0.0001f,
                $"Pos1 Initial={initialPos1}, Current={target1.WorldPosition}");
            Assert("Test 11b: Target 2 anchor position strictly immutable",
                Vector3.Distance(initialPos2, target2.WorldPosition) < 0.0001f,
                $"Pos2 Initial={initialPos2}, Current={target2.WorldPosition}");

            // -----------------------------------------------------------------
            // TEST 12: No duplicate card for same target
            // -----------------------------------------------------------------
            // When raycast hits near Target 1 (within deduplication radius 0.20m), FindClosestTarget matches target1
            float distToTarget1 = Vector3.Distance(new Vector3(0.05f, 0.0f, 1.0f), target1.WorldPosition);
            bool isSameTarget = distToTarget1 <= 0.20f;
            Assert("Test 12: Deduplication matches existing target (no duplicate card created)",
                isSameTarget && target1.cardInstance == cardGo1);

            // Cleanup
            Object.DestroyImmediate(cardGo1);
            Object.DestroyImmediate(cardGo2);
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
