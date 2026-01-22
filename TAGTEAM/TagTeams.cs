using Deadpan.Enums.Engine.Components.Modding; // this allows us to make WildfrostMod's

using DuosMod;
using FMODUnity;
using FMODUnityResonance;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEditor;
using Rewired.Utils;
using UnityEngine.Events;
using Unity.Services.Analytics;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Tables;
using UnityEngine.UI;
using DeadExtensions;
using static DynamicTutorialSystem;
using Unity.Mathematics;
using System.Collections;
using UnityEngine.SceneManagement;
using NaughtyAttributes.Test;
using WildfrostHopeMod;

namespace TagTeam
{
    public class ScriptableBetterCardsInHand : ScriptableAmount
    {
        public int Multiplier = 1;

        public override int Get(Entity entity)
        {
            return References.Player.handContainer.Count * Multiplier;
        }
    }

    public class StatusEffectMeldXYZAllies : StatusEffectData
    {
        public string statusType1;

        public string statusType2;

        public string statusType3;

        public StatusEffectData effectToApply;

        public StatusEffectData effectToApply2;

        public StatusEffectData effectToApply3;

        public override void Init()
        {
            base.OnApplyStatus += Run;
        }

        private IEnumerator Run(StatusEffectApply apply)
        {
            if (apply.effectData.type == statusType1)
            {
                yield return StatusEffectSystem.Apply(apply.target, target, effectToApply2, apply.count);
                yield return StatusEffectSystem.Apply(apply.target, target, effectToApply3, apply.count);
            }
            if (apply.effectData.type == statusType2)
            {
                yield return StatusEffectSystem.Apply(apply.target, target, effectToApply3, apply.count);
                yield return StatusEffectSystem.Apply(apply.target, target, effectToApply, apply.count);
            }
            if (apply.effectData.type == statusType3)
            {
                yield return StatusEffectSystem.Apply(apply.target, target, effectToApply, apply.count);
                yield return StatusEffectSystem.Apply(apply.target, target, effectToApply2, apply.count);
            }
        }

        public override bool RunApplyStatusEvent(StatusEffectApply apply)
        {
            if (target != null && apply.applier != null && apply.effectData != null && statusType1 != null && statusType2 != null && statusType3 != null && effectToApply != null && effectToApply2 != null && effectToApply3 != null)
            {
                Entity entity = target;
                if ((object)entity != null)
                {
                    _ = entity.enabled;
                    if (true && apply.applier.owner == target.owner && (apply.effectData?.type == statusType1 || apply.effectData?.type == statusType2 || apply.effectData?.type == statusType3) && !(apply.effectData == effectToApply) && !(apply.effectData == effectToApply2) && !(apply.effectData == effectToApply3))
                    {
                        return apply.count > 0;
                    }
                }
            }
            return false;
        }
    }


    public class Tagteam : WildfrostMod
    {
        [HarmonyPatch(typeof(CombineCardSequence), "Run", new Type[]
        {
        typeof(CardData[]),
        typeof(CardData)
        })]
        public class BetterComineSystem
        {
            private static IEnumerator Yeet(CombineCardSequence __instance, CardData[] cards, CardData finalCard)
            {
                CinemaBarSystem.State cinemaBarState = new CinemaBarSystem.State();
                PauseMenu.Block();
                CinemaBarSystem.SetSortingLayer("UI2", 100);
                CinemaBarSystem.In();
                Entity[] entities = __instance.CreateEntities(cards);
                Entity finalEntity = __instance.CreateFinalEntity(finalCard);
                Routine.Clump clump = new Routine.Clump();
                Entity[] array = entities;
                foreach (Entity entity in array)
                {
                    clump.Add(entity.display.UpdateData());
                }
                clump.Add(finalEntity.display.UpdateData());
                clump.Add(Sequences.Wait(0.5f));
                yield return clump.WaitForEnd();
                Entity[] array2 = entities;
                for (int j = 0; j < array2.Length; j++)
                {
                    array2[j].transform.localScale = Vector3.one * 0.8f;
                }
                Entity[] array3 = entities;
                foreach (Entity entity2 in array3)
                {
                    foreach (CardUpgradeData upgrade in entity2._data.upgrades)
                    {
                        References.PlayerData.inventory.upgrades.Add(upgrade);
                    }
                    References.PlayerData.inventory.deck.Remove(entity2.data);
                }
                References.PlayerData.inventory.deck.Add(finalEntity.data);
                __instance.fader.In();
                Vector3 vector = Vector3.zero;
                Entity[] array4 = entities;
                foreach (Entity entity3 in array4)
                {
                    vector += entity3.transform.position;
                }
                vector /= (float)entities.Length;
                __instance.group.position = vector;
                Entity[] array5 = entities;
                foreach (Entity entity4 in array5)
                {
                    Transform transform = UnityEngine.Object.Instantiate(__instance.pointPrefab, entity4.transform.position, Quaternion.identity, __instance.group);
                    transform.gameObject.SetActive(value: true);
                    entity4.transform.SetParent(transform);
                    __instance.points.Add(transform);
                    LeanTween.alphaCanvas(((Card)entity4.display).canvasGroup, 1f, 0.4f).setEaseInQuad();
                }
                foreach (Transform transform2 in __instance.points)
                {
                    LeanTween.moveLocal(to: transform2.localPosition.normalized, gameObject: transform2.gameObject, time: 0.4f).setEaseInQuart();
                }
                yield return new WaitForSeconds(0.4f);
                __instance.Flash(0.5f, 0.15f);
                Events.InvokeScreenShake(1f, 0f);
                array2 = entities;
                for (int n = 0; n < array2.Length; n++)
                {
                    array2[n].wobbler.WobbleRandom();
                }
                __instance.hitPs.Play();
                foreach (Transform transform3 in __instance.points)
                {
                    LeanTween.moveLocal(to: transform3.localPosition.normalized * 3f, gameObject: transform3.gameObject, time: 1f).setEase(__instance.bounceCurve);
                }
                LeanTween.moveLocal(__instance.group.gameObject, new Vector3(0f, 0f, -2f), 1f).setEaseInOutQuad();
                LeanTween.rotateZ(__instance.group.gameObject, PettyRandom.Range(160f, 180f), 1f).setOnUpdateVector3(delegate
                {
                    foreach (Transform point in __instance.points)
                    {
                        point.transform.eulerAngles = Vector3.zero;
                    }
                }).setEaseInOutQuad();
                yield return new WaitForSeconds(1f);
                __instance.Flash(1f, 0.15f);
                Events.InvokeScreenShake(1f, 0f);
                if ((bool)__instance.ps)
                {
                    __instance.ps.Play();
                }
                __instance.combinedFx.SetActive(value: true);
                finalEntity.transform.position = Vector3.zero;
                array2 = entities;
                for (int num = 0; num < array2.Length; num++)
                {
                    CardManager.ReturnToPool(array2[num]);
                }
                __instance.group.transform.localRotation = Quaternion.identity;
                finalEntity.curveAnimator.Ping();
                finalEntity.wobbler.WobbleRandom();
                CinemaBarSystem.Top.SetScript(__instance.titleKey.GetLocalizedString());
                CinemaBarSystem.Bottom.SetPrompt(__instance.continueKey.GetLocalizedString(), "Select");
                while (!InputSystem.IsButtonPressed("Select"))
                {
                    yield return null;
                }
                cinemaBarState.Restore();
                CinemaBarSystem.SetSortingLayer("CinemaBars");
                __instance.fader.gameObject.Destroy();
                __instance.cardSelector.character = References.Player;
                __instance.cardSelector.MoveCardToDeck(finalEntity);
                PauseMenu.Unblock();
            }

            public static bool Prefix(ref IEnumerator __result, ref CombineCardSequence __instance, ref CardData[] cards, ref CardData finalCard)
            {
                __result = Yeet(__instance, cards, finalCard);
                return false;
            }
        }

        [HarmonyPatch(typeof(CampaignGenerator), "GetPresetLines")]
        public class PatchPostFixPresetLines
        {
            public static CampaignGenerator generator;

            [HarmonyPostfix]
            private static string[] myfunc(string[] __result)
            {
                string[] array = __result.Clone() as string[];
                string[] array2 = __result.Clone() as string[];
                string text = "";
                for (int i = 0; i < array[0].Length; i++)
                {
                    text = ((i == 2) ? (text + "c") : (text + array[0][i]));
                }
                array2[0] = text;
                return array2;
            }
        }

        [HarmonyPatch(typeof(CampaignPopulator.Tier), "PullReward")]
        public class PatchPostfixCampaignPopulatorTierStuff
        {
            [HarmonyPostfix]
            private static CampaignNodeType fixingfunction(CampaignNodeType __result, ref CampaignPopulator.Tier __instance)
            {
                if (__instance.number != 0 || __result.name != "CampaignNodeCompanion" || __instance.rewards.Count() == 0)
                {
                    return __result;
                }
                CampaignNodeType campaignNodeType = __instance.rewards.RandomItem();
                __instance.rewards.Remove(campaignNodeType);
                return campaignNodeType;
            }
        }

        private bool preLoaded = false;

        private List<StatusEffectDataBuilder> statusEffects;

        private List<CardDataBuilder> cards;

        private List<TraitDataBuilder> traits;

        private List<KeywordDataBuilder> keywords;

        public bool once = false;

        [ConfigItem(true, "", "Tag Team Cards")]

        public bool nonspriteart;

        public override string GUID => "Potato.Wildfrost.TagTeamCards";

        public override string[] Depends => new string[0];

        public override string Title => "Tag Team Cards";

        public override string Description => "Adds some cards that are duos of companions, when you get both of the companions that make up a tag team, they join together! Charms/crowns on the cards when they form the tag team are given back to you :D\r\n(these are likely very unbalanced right now)\r\n\r\nNote: Companions in reserve will not form Tag teams, so you can use this to prevent yourself losing companions if you'd rather keep them that tag team them\r\n\r\nThis mod also makes it so that between fight 1 and fight 2, you will always see a path with 2 companion nodes available\r\n\r\nSmall recommendation:\r\nUsing the Variable Rewards Mod can be used to let you see more companions, it can be used to make it more likely to get duos!\r\n\r\nEvery Companion has at least 1 duo.\r\n\r\nNow has some fan made arts for some cards:\r\nKernel + firefist art by Megamarine\r\nBombom + Nova art by asterstruck\r\n\r\nThe fan made arts can always be disabled if you really prefer the vanilla sprite work and my poorly made mash ups of the sprites.\r\n\r\nExisting combos:\r\n\r\n-Neutral only\r\nDimona + Snobble\r\nNova + Bombom\r\nGojiber + Roibos\r\nJumbo + Blunky\r\n\r\n-Includes an SD unit\r\nKernel + Firefist\r\nPimento + Pyra\r\nFungun + Dimona\r\nPootie + tiny tyko\r\nWallop + Wort\r\nYuki + Snoffel\r\nLilBerry + Big Berry\r\nChompom + Shelly\r\nFulbert + Bonnie\r\n\r\n-Includes an SM unit\r\nMonch + Baker\r\nSpoof + Splinter\r\nTusk + berry sis\r\nShen + Dimona\r\nChicken + Egg\r\nDevicro + Groff\r\nVan Jun + Lupa\r\nVesta + Snoffel\r\nTaiga + Zula\r\n\r\n-Includes a CM unit\r\nMama Tinkerson + Alloy\r\nBiji + Dimona\r\nFolby + Knuckles\r\nNom & Stompy + Toaster\r\nMini Mika + Kreggo\r\nFoxee + Fizzle\r\nTinkerson Jr + Hazeblazer\r\nNeedle + Scaven\r\n\r\n-Gnome (both are the same effect, with different units so you can get it slightly more often :3)\r\nNaked Gnome + Lupa\r\nNaked Gnome + Snoffel\r\n\r\nMore to come likely in the future :D\r\n(when I feel like it and think of a fun idea)";

        private StatusEffectDataBuilder StatusCopy(string oldName, string newName)
        {
            StatusEffectData statusEffectData = Get<StatusEffectData>(oldName).InstantiateKeepName();
            statusEffectData.name = newName;
            StatusEffectDataBuilder statusEffectDataBuilder = statusEffectData.Edit<StatusEffectData, StatusEffectDataBuilder>();
            statusEffectDataBuilder.Mod = this;
            return statusEffectDataBuilder;
        }

        private CardDataBuilder CardCopy(string oldName, string newName)
        {
            CardData cardData = Get<CardData>(oldName).InstantiateKeepName();
            cardData.name = newName;
            CardDataBuilder cardDataBuilder = cardData.Edit<CardData, CardDataBuilder>();
            cardDataBuilder.Mod = this;
            return cardDataBuilder;
        }

        public Tagteam(string modDirectory)
            : base(modDirectory)
        {
        }

        private CardData.StatusEffectStacks SStack(string name, int amount)
        {
            return new CardData.StatusEffectStacks(Get<StatusEffectData>(name), amount);
        }

        public static CombineCardSystem.Combo newcombo(string name1, string name2, string output, string GUID)
        {
            CombineCardSystem.Combo result = default(CombineCardSystem.Combo);
            result.cardNames = new string[2] { name1, name2 };
            result.resultingCardName = GUID + "." + output;
            return result;
        }

        private void SceneLoaded(Scene scene)
        {
            if (!(scene.name != "Campaign"))
            {
                CombineCardSystem combineCardSystem = UnityEngine.Object.FindObjectOfType<CombineCardSystem>(includeInactive: true);
                combineCardSystem.enabled = true;
                combineCardSystem.combos = combineCardSystem.combos.Append(newcombo("NakedGnomeFriendly", "LuminCat", "NakedGnomeLupaDuo", this.GUID)).ToArray();
                combineCardSystem.combos = combineCardSystem.combos.Append(newcombo("NakedGnomeFriendly", "Snoffel", "NakedGnomeSnoffelDuo", this.GUID)).ToArray();
                combineCardSystem.combos = combineCardSystem.combos.Append(newcombo("Kernel", "Firefist", "KernelFirefistDuo", this.GUID)).ToArray();
                combineCardSystem.combos = combineCardSystem.combos.Append(newcombo("Monch", "TheBaker", "MonchBakerDuo", this.GUID)).ToArray();
                combineCardSystem.combos = combineCardSystem.combos.Append(newcombo("Ditto", "Spoof", "SpoofSplinterDuo", this.GUID)).ToArray();
                combineCardSystem.combos = combineCardSystem.combos.Append(newcombo("MamaTinkerson", "Turmeep", "MamaAlloyDuo", this.GUID)).ToArray();
                combineCardSystem.combos = combineCardSystem.combos.Append(newcombo("Pimento", "Pyra", "PimentoPyraDuo", this.GUID)).ToArray();
                combineCardSystem.combos = combineCardSystem.combos.Append(newcombo("Tusk", "BloodBoy", "TuskBerrySisDuo", this.GUID)).ToArray();
                combineCardSystem.combos = combineCardSystem.combos.Append(newcombo("Zoog", "Dimona", "ShenDimonaDuo", this.GUID)).ToArray();
                combineCardSystem.combos = combineCardSystem.combos.Append(newcombo("Fungoose", "Dimona", "FungunDimonaDuo", this.GUID)).ToArray();
                combineCardSystem.combos = combineCardSystem.combos.Append(newcombo("Witch", "Dimona", "BijiDimonaDuo", this.GUID)).ToArray();
                combineCardSystem.combos = combineCardSystem.combos.Append(newcombo("Snobble", "Dimona", "SnobbleDimonaDuo", this.GUID)).ToArray();
                combineCardSystem.combos = combineCardSystem.combos.Append(newcombo("Pootie", "TinyTyko", "TinyPootieDuo", this.GUID)).ToArray();
                combineCardSystem.combos = combineCardSystem.combos.Append(newcombo("Wallop", "Wort", "WallopWortDuo", this.GUID)).ToArray();
                combineCardSystem.combos = combineCardSystem.combos.Append(newcombo("Yuki", "Snoffel", "YukiSnoffelDuo", this.GUID)).ToArray();
                combineCardSystem.combos = combineCardSystem.combos.Append(newcombo("TailsFive", "Egg", "ChickenEggDuo", this.GUID)).ToArray();
                combineCardSystem.combos = combineCardSystem.combos.Append(newcombo("Blue", "Bombom", "NovaBomBomDuo", this.GUID)).ToArray();
                combineCardSystem.combos = combineCardSystem.combos.Append(newcombo("Reaper", "Boggler", "DevicroGroffDuo", this.GUID)).ToArray();
                combineCardSystem.combos = combineCardSystem.combos.Append(newcombo("LilBerry", "BigBerry", "LilBigBerryDuo", this.GUID)).ToArray();
                combineCardSystem.combos = combineCardSystem.combos.Append(newcombo("Gearhead", "Knuckles", "FolbyKnucklesDuo", this.GUID)).ToArray();
                combineCardSystem.combos = combineCardSystem.combos.Append(newcombo("GuardianGnome", "Havok", "NomStompyToasterQuad", this.GUID)).ToArray();
                combineCardSystem.combos = combineCardSystem.combos.Append(newcombo("Timmy", "Bunnight", "MiniMikaKreggoDuo", this.GUID)).ToArray();
                combineCardSystem.combos = combineCardSystem.combos.Append(newcombo("Foxee", "Gnomlings", "FoxeeFizzleDuo", this.GUID)).ToArray();
                combineCardSystem.combos = combineCardSystem.combos.Append(newcombo("Chompom", "Shelly", "ChompomShellyDuo", this.GUID)).ToArray();
                combineCardSystem.combos = combineCardSystem.combos.Append(newcombo("BoBo", "LuminCat", "VanJunLupaDuo", this.GUID)).ToArray();
                combineCardSystem.combos = combineCardSystem.combos.Append(newcombo("Jummo", "Voodoo", "TinkersonHazeDuo", this.GUID)).ToArray();
                combineCardSystem.combos = combineCardSystem.combos.Append(newcombo("MagmaBlacksmith", "Noggin", "GojiberRoisbosDuo", this.GUID)).ToArray();
                combineCardSystem.combos = combineCardSystem.combos.Append(newcombo("Snoffel", "Flash", "VestaSnoffelDuo", this.GUID)).ToArray();
                combineCardSystem.combos = combineCardSystem.combos.Append(newcombo("Ruckus", "Bear", "NeedleScavernDuo", this.GUID)).ToArray();
                combineCardSystem.combos = combineCardSystem.combos.Append(newcombo("Blunky", "Klutz", "JumboBlunkyDuo", this.GUID)).ToArray();
                combineCardSystem.combos = combineCardSystem.combos.Append(newcombo("Bonnie", "Fulbert", "FulbertBonnieDuo", this.GUID)).ToArray();
                combineCardSystem.combos = combineCardSystem.combos.Append(newcombo("Kokonut", "Zula", "TaigaZulaDuo", this.GUID)).ToArray();
            }
        }

        private void CardsPhoto(Scene scene)
        {
            MonoBehaviourSingleton<References>.instance.StartCoroutine(CardsPhoto2());
        }

        private static IEnumerator CardsPhoto2()
        {
            string[] everyGeneration = new string[29]
            {
            "KernelFirefistDuo", "MonchBakerDuo", "SpoofSplinterDuo", "MamaAlloyDuo", "SnobbleDimonaDuo", "PimentoPyraDuo", "TuskBerrySisDuo", "ShenDimonaDuo", "FungunDimonaDuo", "BijiDimonaDuo",
            "TinyPootieDuo", "YukiSnoffelDuo", "WallopWortDuo", "ChickenEggDuo", "NovaBomBomDuo", "DevicroGroffDuo", "LilBigBerryDuo", "FolbyKnucklesDuo", "NomStompyToasterQuad", "MiniMikaKreggoDuo",
            "ChompomShellyDuo", "TaigaZulaDuo", "VanJunLupaDuo", "TinkersonHazeDuo", "GojiberRoisbosDuo", "VestaSnoffelDuo", "NeedleScavernDuo", "JumboBlunkyDuo", "FulbertBonnieDuo"
            };
            yield return SceneManager.WaitUntilUnloaded("CardFramesUnlocked");
            yield return SceneManager.Load("CardFramesUnlocked", SceneType.Temporary);
            CardFramesUnlockedSequence sequence = UnityEngine.Object.FindObjectOfType<CardFramesUnlockedSequence>();
            TextMeshProUGUI titleObject = sequence.GetComponentInChildren<TextMeshProUGUI>(includeInactive: true);
            titleObject.text = "New Cards!";
            yield return sequence.StartCoroutine("CreateCards", everyGeneration.Select((string s) => "Potato.Wildfrost.TagTeamCards." + s).ToArray());
        }

        private void createmodassets()
        {
            statusEffects = new List<StatusEffectDataBuilder>();
            traits = new List<TraitDataBuilder>();
            cards = new List<CardDataBuilder>();
            keywords = new List<KeywordDataBuilder>();
            createKernelFirefist();
            createMonchBaker();
            createSpoofSplinter();
            createMamaAlloy();
            createDimonaSnobble();
            createPimentoPyra();
            createTuskBerrySis();
            createShenDimona();
            createFungunDimona();
            createBijiDimona();
            createPootiePinyTyko();
            createWallopWort();
            createYukiSnoffel();
            createChickenEgg();
            createNovaBomBom();
            createDevicroGroff();
            createLilBigBerry();
            createFolbyKnuckles();
            createNomStompyToaster();
            createaMiniMikaKreggo();
            createFoxeeFizzle();
            createChompomShelly();
            createZulaTaiga();
            createVanJunLupa();
            createTinkersonHazeblazer();
            createGojiberRoibos();
            createVestaSnoffel();
            createNeedleScavern();
            createJumboBlunky();
            createFulbertBonnie();
            createNakedGnomes();
            preLoaded = true;
        }

        private void createKernelFirefist()
        {
            cards.Add(new CardDataBuilder(this).CreateUnit("KernelFirefistDuo", "Kernel And Firefist").SetSprites("SpriteWork/KernelFirefist.png", "SpriteWork/KernelFirefist_BG.png").SetStats(12, 2, 4)
                .WithCardType()
                .SubscribeToAfterAllBuildEvent(delegate (CardData card)
                {
                    card.startWithEffects = new CardData.StatusEffectStacks[3]
                    {
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("When Health Lost Apply Equal Shell To Self"), 1),
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("When Hit Apply Spice To Self"), 4),
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("When Hit Apply Shell To Allies"), 1)
                    };
                }));
        }

        private void createMonchBaker()
        {
            statusEffects.Add(StatusCopy("Summon Beepop", "Summon_Monch").WithText("Summon <card=Monch>").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                ((StatusEffectSummon)data).summonCard = Get<CardData>("Monch").Clone();
            }));
            cards.Add(CardCopy("BeepopMask", "MonchMuffin").WithTitle("Monch Muffin").SetSprites("SpriteWork/MonchMuffin.png", "SpriteWork/MonchMuffin_BG.png").SetTraits(new CardData.TraitStacks(Get<TraitData>("Zoomlin"), 1), new CardData.TraitStacks(Get<TraitData>("Consume"), 1))
                .SubscribeToAfterAllBuildEvent(delegate (CardData card)
                {
                    card.startWithEffects = new CardData.StatusEffectStacks[1]
                    {
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("Summon_Monch"), 1)
                    };
                }));
            statusEffects.Add(StatusCopy("Summon SkullMuffin", "Summon MonchMuffin").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                (data as StatusEffectSummon).summonCard = Get<CardData>("MonchMuffin").Clone();
            }));
            statusEffects.Add(StatusCopy("Instant Summon SkullMuffin In Hand", "Instant Summon MonchMuffin In Hand").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                (data as StatusEffectInstantSummon).targetSummon = Get<StatusEffectData>("Summon MonchMuffin") as StatusEffectSummon;
            }));
            statusEffects.Add(StatusCopy("On Card Played Add SkullMuffin To Hand", "On Card Played Add MonchMuffin To Hand").WithText("Add <{a}> {0} to hand").WithTextInsert("<card=MonchMuffin>").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                (data as StatusEffectApplyXOnCardPlayed).effectToApply = Get<StatusEffectData>("Instant Summon MonchMuffin In Hand");
            }));
            cards.Add(new CardDataBuilder(this).CreateUnit("MonchBakerDuo", "Baker And Monch").SetSprites("SpriteWork/BakerMonch.png", "SpriteWork/BakerMonch_BG.png").SetStats(8, 1, 3)
                .WithCardType()
                .SetTraits(new CardData.TraitStacks(Get<TraitData>("Spark"), 1))
                .SubscribeToAfterAllBuildEvent(delegate (CardData card)
                {
                    card.startWithEffects = new CardData.StatusEffectStacks[1]
                    {
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("On Card Played Add MonchMuffin To Hand"), 1)
                    };
                }));
        }

        private void createSpoofSplinter()
        {
            statusEffects.Add(StatusCopy("Instant Summon Bootleg Copy At Appliers Position", "Instant Summon Copy At Appliers Position").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                (data as StatusEffectInstantSummon).withEffects = new StatusEffectData[0];
            }));
            statusEffects.Add(StatusCopy("On Turn Summon Bootleg Copy of RandomEnemy", "On Turn Summon Copy of RandomEnemy").WithText("Summon a copy of a random enemy").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                (data as StatusEffectApplyXOnTurn).effectToApply = Get<StatusEffectData>("Instant Summon Copy At Appliers Position");
            }));
            cards.Add(new CardDataBuilder(this).CreateUnit("SpoofSplinterDuo", "Splinter and Spoof").SetSprites("SpriteWork/SplinterSpoof.png", "SpriteWork/SplinterSpoof_BG.png").SetStats(2, null, 4)
                .WithCardType()
                .SetTraits(new CardData.TraitStacks(Get<TraitData>("Effigy"), 2))
                .SubscribeToAfterAllBuildEvent(delegate (CardData card)
                {
                    card.startWithEffects = new CardData.StatusEffectStacks[1]
                    {
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("On Turn Summon Copy of RandomEnemy"), 1)
                    };
                }));
        }

        private void createMamaAlloy()
        {
            statusEffects.Add(StatusCopy("On Card Played Add Scrap To Allies", "On Card Played Add Frenzy To Clunker Allies").WithText("Add <x{a}><keyword=frenzy> to all clunker allies").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                (data as StatusEffectApplyXOnCardPlayed).applyConstraints = new TargetConstraint[1]
                {
                new TargetConstraintIsCardType
                {
                    allowedTypes = new CardType[1] { Get<CardType>("Clunker") }
                }
                };
                (data as StatusEffectApplyXOnCardPlayed).effectToApply = Get<StatusEffectData>("MultiHit");
                ((StatusEffectApplyX)(data as StatusEffectApplyXOnCardPlayed)).noTargetType = NoTargetType.None;
            }));
            cards.Add(new CardDataBuilder(this).CreateUnit("MamaAlloyDuo", "Mama Tinkerson and Alloy").SetSprites("SpriteWork/MamaAlloy.png", "SpriteWork/MamaAlloy_BG.png").SetStats(9, 3, 4)
                .WithCardType()
                .SubscribeToAfterAllBuildEvent(delegate (CardData card)
                {
                    card.startWithEffects = new CardData.StatusEffectStacks[3]
                    {
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("Weakness"), 1),
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("On Card Played Add Scrap To Allies"), 1),
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("On Card Played Add Frenzy To Clunker Allies"), 1)
                    };
                }));
        }

        private void createDimonaSnobble()
        {
            statusEffects.Add(StatusCopy("When Enemy Is Hit By Item Apply Demonize To Them", "When Enemy Is Hit By Item Apply Snow To Them").WithText("When an enemy is hit with an <Item>, apply <{a}><keyword=snow> to them").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                (data as StatusEffectApplyXWhenUnitIsHit).effectToApply = Get<StatusEffectData>("Snow");
                ((StatusEffectApplyX)(data as StatusEffectApplyXWhenUnitIsHit)).noTargetType = NoTargetType.None;
            }));
            cards.Add(new CardDataBuilder(this).CreateUnit("SnobbleDimonaDuo", "Snobble and Dimona").SetSprites("SpriteWork/SnobbleDimona.png", "SpriteWork/SnobbleDimona_BG.png").SetStats(8, 2, 3)
                .WithCardType()
                .SetAttackEffect(new CardData.StatusEffectStacks(Get<StatusEffectData>("Demonize"), 3))
                .SubscribeToAfterAllBuildEvent(delegate (CardData card)
                {
                    card.startWithEffects = new CardData.StatusEffectStacks[1]
                    {
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("When Enemy Is Hit By Item Apply Snow To Them"), 3)
                    };
                }));
        }

        private void createPimentoPyra()
        {
            cards.Add(new CardDataBuilder(this).CreateUnit("PimentoPyraDuo", "Pimento and Pyra").SetSprites("SpriteWork/PimentoPyra.png", "SpriteWork/PimentoPyra_BG.png").SetStats(7, 1, 4)
                .WithCardType()
                .SubscribeToAfterAllBuildEvent(delegate (CardData card)
                {
                    card.startWithEffects = new CardData.StatusEffectStacks[3]
                    {
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("Halt Spice With Text"), 1),
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("On Turn Apply Spice To Self"), 3),
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("On Turn Apply Spice To Allies"), 2)
                    };
                }));
        }

        private void createTuskBerrySis()
        {
            statusEffects.Add(StatusCopy("When Hit Apply Spice To Allies", "When Hit Apply Teeth To Allies").WithText("When hit, Add <{a}><keyword=teeth> to all  allies").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                (data as StatusEffectApplyXWhenHit).applyConstraints = new TargetConstraint[1]
                {
                new TargetConstraintIsAlive()
                };
                (data as StatusEffectApplyXWhenHit).effectToApply = Get<StatusEffectData>("Teeth");
                ((StatusEffectApplyX)(data as StatusEffectApplyXWhenHit)).noTargetType = NoTargetType.None;
            }));
            statusEffects.Add(StatusCopy("When Hit Apply Spice To Allies", "When Hit Add Health To Allies").WithText("When hit, increase all allies <keyword=health> by <{a}>").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                (data as StatusEffectApplyXWhenHit).applyConstraints = new TargetConstraint[2]
                {
                new TargetConstraintIsAlive(),
                new TargetConstraintHasHealth()
                };
                (data as StatusEffectApplyXWhenHit).effectToApply = Get<StatusEffectData>("Increase Max Health");
                ((StatusEffectApplyX)(data as StatusEffectApplyXWhenHit)).noTargetType = NoTargetType.None;
            }));
            cards.Add(new CardDataBuilder(this).CreateUnit("TuskBerrySisDuo", "Tusk and Berry Sis").SetSprites("SpriteWork/TuskBerrySis.png", "SpriteWork/TuskBerrySis_BG.png").SetStats(11, 2, 4)
                .WithCardType()
                .SubscribeToAfterAllBuildEvent(delegate (CardData card)
                {
                    card.startWithEffects = new CardData.StatusEffectStacks[2]
                    {
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("When Hit Apply Teeth To Allies"), 1),
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("When Hit Add Health To Allies"), 2)
                    };
                }));
        }

        private void createShenDimona()
        {
            statusEffects.Add(StatusCopy("When Enemy Is Hit By Item Apply Demonize To Them", "When Enemy Is Hit By Item Apply overburn To Them").WithText("When an enemy is hit with an <Item>, apply <{a}><keyword=overload> to them").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                (data as StatusEffectApplyXWhenUnitIsHit).effectToApply = Get<StatusEffectData>("Overload");
                ((StatusEffectApplyX)(data as StatusEffectApplyXWhenUnitIsHit)).noTargetType = NoTargetType.None;
            }));
            cards.Add(new CardDataBuilder(this).CreateUnit("ShenDimonaDuo", "Shen and Dimona").SetSprites("SpriteWork/ShenDimona.png", "SpriteWork/ShenDimona_BG.png").SetStats(6, 3, 3)
                .WithCardType()
                .SetAttackEffect(new CardData.StatusEffectStacks(Get<StatusEffectData>("Demonize"), 3))
                .SubscribeToAfterAllBuildEvent(delegate (CardData card)
                {
                    card.startWithEffects = new CardData.StatusEffectStacks[1]
                    {
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("When Enemy Is Hit By Item Apply overburn To Them"), 4)
                    };
                }));
        }

        private void createFungunDimona()
        {
            statusEffects.Add(StatusCopy("When Enemy Is Hit By Item Apply Demonize To Them", "When Enemy Is Hit By Item Apply shroom To Them").WithText("When an enemy is hit with an <Item>, apply <{a}><keyword=shroom> to them").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                (data as StatusEffectApplyXWhenUnitIsHit).effectToApply = Get<StatusEffectData>("Shroom");
                ((StatusEffectApplyX)(data as StatusEffectApplyXWhenUnitIsHit)).noTargetType = NoTargetType.None;
            }));
            cards.Add(new CardDataBuilder(this).CreateUnit("FungunDimonaDuo", "Fungun and Dimona").SetSprites("SpriteWork/FungunDimona.png", "SpriteWork/FungunDimona_BG.png").SetStats(12, 4, 4)
                .WithCardType()
                .SetAttackEffect(new CardData.StatusEffectStacks(Get<StatusEffectData>("Demonize"), 3))
                .SubscribeToAfterAllBuildEvent(delegate (CardData card)
                {
                    card.startWithEffects = new CardData.StatusEffectStacks[1]
                    {
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("When Enemy Is Hit By Item Apply shroom To Them"), 5)
                    };
                }));
        }

        private void createBijiDimona()
        {
            statusEffects.Add(StatusCopy("When Enemy Is Hit By Item Apply Demonize To Them", "When Enemy Is Hit By Item Apply bom To Them").WithText("When an enemy is hit with an <Item>, apply <{a}><keyword=weakness> to them").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                (data as StatusEffectApplyXWhenUnitIsHit).effectToApply = Get<StatusEffectData>("Weakness");
                ((StatusEffectApplyX)(data as StatusEffectApplyXWhenUnitIsHit)).noTargetType = NoTargetType.None;
            }));
            cards.Add(new CardDataBuilder(this).CreateUnit("BijiDimonaDuo", "Biji and Dimona").SetSprites("SpriteWork/BijiDimona.png", "SpriteWork/BijiDimona_BG.png").SetStats(9, 2, 4)
                .WithCardType()
                .SetAttackEffect(new CardData.StatusEffectStacks(Get<StatusEffectData>("Demonize"), 2))
                .SubscribeToAfterAllBuildEvent(delegate (CardData card)
                {
                    card.startWithEffects = new CardData.StatusEffectStacks[1]
                    {
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("When Enemy Is Hit By Item Apply bom To Them"), 6)
                    };
                }));
        }

        private void createPootiePinyTyko()
        {
            statusEffects.Add(StatusCopy("When Hit Add Frenzy To Self", "When Hit Add Frenzy To Random Ally").WithText("When hit, apply <x{a}><keyword=frenzy> to a random ally").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                (data as StatusEffectApplyXWhenHit).applyToFlags = StatusEffectApplyX.ApplyToFlags.RandomAlly;
                ((StatusEffectApplyX)(data as StatusEffectApplyXWhenHit)).noTargetType = NoTargetType.None;
            }));
            cards.Add(new CardDataBuilder(this).CreateUnit("TinyPootieDuo", "Tiny Tyko and Pootie").SetSprites("SpriteWork/TinyPootie.png", "SpriteWork/TinyPootie_BG.png").SetStats(5, 1, 3)
                .WithCardType()
                .SubscribeToAfterAllBuildEvent(delegate (CardData card)
                {
                    card.startWithEffects = new CardData.StatusEffectStacks[2]
                    {
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("When Hit Add Frenzy To Random Ally"), 1),
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("MultiHit"), 1)
                    };
                }));
        }

        private void createWallopWort()
        {
            statusEffects.Add(StatusCopy("On Turn Apply Snow To Enemies", "On Turn Apply Shoom To Enemies in row").WithText("Apply <{a}><keyword=shroom> to enemies in row").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                (data as StatusEffectApplyXOnTurn).applyToFlags = StatusEffectApplyX.ApplyToFlags.EnemiesInRow;
                ((StatusEffectApplyX)(data as StatusEffectApplyXOnTurn)).noTargetTypeArgs = new string[1] { "<sprite name=shroom>" };
                (data as StatusEffectApplyXOnTurn).effectToApply = Get<StatusEffectData>("Shroom");
                ((StatusEffectApplyX)(data as StatusEffectApplyXOnTurn)).noTargetType = NoTargetType.None;
            }));
            cards.Add(new CardDataBuilder(this).CreateUnit("WallopWortDuo", "Wallop And Wort").SetSprites("SpriteWork/WallopWort.png", "SpriteWork/WallopWort_BG.png").SetStats(8, 1, 4)
                .WithCardType()
                .SetTraits(new CardData.TraitStacks(Get<TraitData>("Longshot"), 1))
                .SetAttackEffect(new CardData.StatusEffectStacks(Get<StatusEffectData>("Shroom"), 2))
                .SubscribeToAfterAllBuildEvent(delegate (CardData card)
                {
                    card.startWithEffects = new CardData.StatusEffectStacks[2]
                    {
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("MultiHit"), 1),
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("On Turn Apply Shoom To Enemies in row"), 2)
                    };
                }));
        }

        private void createYukiSnoffel()
        {
            statusEffects.Add(StatusCopy("Hit All Inkd Enemies", "Hit All snowd Enemies").WithText("Hits all <keyword=snow>'d enemies").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                ((data as StatusEffectChangeTargetMode).targetMode as TargetModeAll).constraints = new TargetConstraint[1]
                {
                new TargetConstraintHasStatus
                {
                    status = Get<StatusEffectData>("Snow")
                }
                };
                SubscribeToAfterAllBuildEvent2(this);
            }));
            cards.Add(new CardDataBuilder(this).CreateUnit("YukiSnoffelDuo", "Yuki And Snoffel").SetSprites("SpriteWork/YukiSnoffel.png", "SpriteWork/YukiSnoffel_BG.png").SetStats(6, 13, 4)
                .WithCardType()
                .SubscribeToAfterAllBuildEvent(delegate (CardData card)
                {
                    card.startWithEffects = new CardData.StatusEffectStacks[1]
                    {
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("Hit All snowd Enemies"), 1)
                    };
                }));
        }

        private void SubscribeToAfterAllBuildEvent2(WildfrostMod mod)
        {
            TraitData[] targetTraits = AddressableLoader.GetGroup<TraitData>("TraitData").ToArray().RemoveFromArray((TraitData item) => item.effects.FirstOrDefault((StatusEffectData effect) => effect is StatusEffectChangeTargetMode || effect is StatusEffectBombard) != null);
            StatusEffectData[] targetEffects = AddressableLoader.GetGroup<StatusEffectData>("StatusEffectData").ToArray().RemoveFromArray((StatusEffectData item) => item is StatusEffectChangeTargetMode || item is StatusEffectBombard);
            foreach (CardUpgradeData item in AddressableLoader.GetGroup<CardUpgradeData>("CardUpgradeData"))
            {
                if (item.giveTraits.FirstOrDefault((CardData.TraitStacks data) => targetTraits.FirstOrDefault((TraitData item2) => item2 == data.data) != null) != null || item.effects.FirstOrDefault((CardData.StatusEffectStacks data) => targetEffects.FirstOrDefault((StatusEffectData item2) => item2 == data.data) != null) != null)
                {
                    item.targetConstraints = item.targetConstraints.AddToArray(new TargetConstraintHasStatus
                    {
                        status = Get<StatusEffectData>("Hit All snowd Enemies"),
                        not = true,
                        name = "PotatoConstraint"
                    });
                }
            }
        }

        private void FixConstraints()
        {
            TraitData[] targetTraits = AddressableLoader.GetGroup<TraitData>("TraitData").ToArray().RemoveFromArray((TraitData item) => item.effects.FirstOrDefault((StatusEffectData effect) => effect is StatusEffectChangeTargetMode || effect is StatusEffectBombard) != null);
            StatusEffectData[] targetEffects = AddressableLoader.GetGroup<StatusEffectData>("StatusEffectData").ToArray().RemoveFromArray((StatusEffectData item) => item is StatusEffectChangeTargetMode || item is StatusEffectBombard);
            foreach (CardUpgradeData item in AddressableLoader.GetGroup<CardUpgradeData>("CardUpgradeData"))
            {
                if (item.giveTraits.FirstOrDefault((CardData.TraitStacks data) => targetTraits.FirstOrDefault((TraitData item2) => item2 == data.data) != null) != null || item.effects.FirstOrDefault((CardData.StatusEffectStacks data) => targetEffects.FirstOrDefault((StatusEffectData item2) => item2 == data.data) != null) != null)
                {
                    Debug.LogWarning(item.targetConstraints.Count());
                    item.targetConstraints = item.targetConstraints.Where((TargetConstraint x) => x.name != "PotatoConstraint").ToArray();
                    Debug.LogWarning(item.targetConstraints.Count());
                }
            }
        }

        private void createChickenEgg()
        {
            cards.Add(new CardDataBuilder(this).CreateUnit("Dregoru_unit", "Dregoru").SetSprites("SpriteWork/Dregoru.png", "SpriteWork/ChickenEgg_BG.png").SetStats(10, 10, 3)
                .WithCardType()
                .SubscribeToAfterAllBuildEvent(delegate (CardData card)
                {
                    card.startWithEffects = new CardData.StatusEffectStacks[1]
                    {
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("MultiHit"), 4)
                    };
                }));
            statusEffects.Add(StatusCopy("Summon Dregg", "Summon Dregoru").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                ((StatusEffectSummon)data).summonCard = Get<CardData>(GUID + ".Dregoru_unit");
            }));
            statusEffects.Add(StatusCopy("Instant Summon Dregg", "Instant Summon Dregoru").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                ((StatusEffectInstantSummon)data).targetSummon = Get<StatusEffectData>("Summon Dregoru") as StatusEffectSummon;
            }));
            statusEffects.Add(StatusCopy("When Destroyed Summon Dregg", "When Destroyed Summon Dregoru").WithText("When Destroyed, Summon {0}").WithTextInsert("<card=" + GUID + ".Dregoru_unit>").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                ((StatusEffectApplyXWhenDestroyed)data).effectToApply = Get<StatusEffectData>("Instant Summon Dregoru");
            }));
            cards.Add(new CardDataBuilder(this).CreateUnit("Dregashi_unit", "Dregashi").SetSprites("SpriteWork/Dregashi.png", "SpriteWork/ChickenEgg_BG.png").SetStats(8, 8, 3)
                .WithCardType()
                .SubscribeToAfterAllBuildEvent(delegate (CardData card)
                {
                    card.startWithEffects = new CardData.StatusEffectStacks[2]
                    {
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("When Destroyed Summon Dregoru"), 1),
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("MultiHit"), 3)
                    };
                }));
            statusEffects.Add(StatusCopy("Summon Dregg", "Summon Dregashi").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                ((StatusEffectSummon)data).summonCard = Get<CardData>(GUID + ".Dregashi_unit");
            }));
            statusEffects.Add(StatusCopy("Instant Summon Dregg", "Instant Summon Dregashi").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                ((StatusEffectInstantSummon)data).targetSummon = Get<StatusEffectData>("Summon Dregashi") as StatusEffectSummon;
            }));
            statusEffects.Add(StatusCopy("When Destroyed Summon Dregg", "When Destroyed Summon Dregashi").WithText("When Destroyed, Summon {0}").WithTextInsert("<card=" + GUID + ".Dregashi_unit>").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                ((StatusEffectApplyXWhenDestroyed)data).effectToApply = Get<StatusEffectData>("Instant Summon Dregashi");
            }));
            cards.Add(new CardDataBuilder(this).CreateUnit("Dregasan_unit", "Dregasan").SetSprites("SpriteWork/Dregasan.png", "CSpriteWork/hickenEgg_BG.png").SetStats(6, 6, 3)
                .WithCardType()
                .SubscribeToAfterAllBuildEvent(delegate (CardData card)
                {
                    card.startWithEffects = new CardData.StatusEffectStacks[2]
                    {
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("When Destroyed Summon Dregashi"), 1),
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("MultiHit"), 2)
                    };
                }));
            statusEffects.Add(StatusCopy("Summon Dregg", "Summon Dregasan").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                ((StatusEffectSummon)data).summonCard = Get<CardData>(GUID + ".Dregasan_unit");
            }));
            statusEffects.Add(StatusCopy("Instant Summon Dregg", "Instant Summon Dregasan").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                ((StatusEffectInstantSummon)data).targetSummon = Get<StatusEffectData>("Summon Dregasan") as StatusEffectSummon;
            }));
            statusEffects.Add(StatusCopy("When Destroyed Summon Dregg", "When Destroyed Summon Dregasan").WithText("When Destroyed, Summon {0}").WithTextInsert("<card=" + GUID + ".Dregasan_unit>").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                ((StatusEffectApplyXWhenDestroyed)data).effectToApply = Get<StatusEffectData>("Instant Summon Dregasan");
            }));
            cards.Add(new CardDataBuilder(this).CreateUnit("Dregani_unit", "Dregani").SetSprites("SpriteWork/Dregani.png", "SpriteWork/ChickenEgg_BG.png").SetStats(4, 4, 3)
                .WithCardType()
                .SubscribeToAfterAllBuildEvent(delegate (CardData card)
                {
                    card.startWithEffects = new CardData.StatusEffectStacks[2]
                    {
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("When Destroyed Summon Dregasan"), 1),
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("MultiHit"), 1)
                    };
                }));
            statusEffects.Add(StatusCopy("Summon Dregg", "Summon Dregani").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                ((StatusEffectSummon)data).summonCard = Get<CardData>(GUID + ".Dregani_unit");
            }));
            statusEffects.Add(StatusCopy("Instant Summon Dregg", "Instant Summon Dregani").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                ((StatusEffectInstantSummon)data).targetSummon = Get<StatusEffectData>("Summon Dregani") as StatusEffectSummon;
            }));
            statusEffects.Add(StatusCopy("When Destroyed Summon Dregg", "When Destroyed Summon Dregani").WithText("When Destroyed, Summon {0}").WithTextInsert("<card=" + GUID + ".Dregani_unit>").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                ((StatusEffectApplyXWhenDestroyed)data).effectToApply = Get<StatusEffectData>("Instant Summon Dregani");
            }));
            cards.Add(new CardDataBuilder(this).CreateUnit("ChickenEggDuo", "Chikichi And Egg").SetSprites("SpriteWork/ChickenEggDuo.png", "SpriteWork/ChickenEgg_BG.png").SetStats(2, 2, 3)
                .WithCardType()
                .SubscribeToAfterAllBuildEvent(delegate (CardData card)
                {
                    card.startWithEffects = new CardData.StatusEffectStacks[1]
                    {
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("When Destroyed Summon Dregani"), 1)
                    };
                }));
        }

        private void createNovaBomBom()
        {
            cards.Add(new CardDataBuilder(this).CreateUnit("NovaBomBomDuo", "Nova And Bombom").SetSprites("SpriteWork/NovaBombom.png", "SpriteWork/NovaBombom_BG.png").SetStats(6, null, 8)
                .WithCardType()
                .SubscribeToAfterAllBuildEvent(delegate (CardData card)
                {
                    card.startWithEffects = new CardData.StatusEffectStacks[3]
                    {
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("On Card Played Apply Block To Self"), 1),
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("When Block Lost, Damage Enemies"), 5),
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("Block"), 1)
                    };
                }));
        }

        private void createDevicroGroff()
        {
            statusEffects.Add(StatusCopy("When Ally Is Killed Apply Attack To Self", "When Ally Is Killed Apply Frenzy To Self").WithText("Gain <x{a}><keyword=frenzy> when an ally is killed").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                (data as StatusEffectApplyXWhenAllyIsKilled).effectToApply = Get<StatusEffectData>("MultiHit");
            }));
            cards.Add(new CardDataBuilder(this).CreateUnit("DevicroGroffDuo", "Devicro And Groff").SetSprites("SpriteWork/DevicroGroff.png", "SpriteWork/DevicroGroff_BG.png").SetStats(6, 5, 4)
                .WithCardType()
                .SubscribeToAfterAllBuildEvent(delegate (CardData card)
                {
                    card.startWithEffects = new CardData.StatusEffectStacks[1]
                    {
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("When Ally Is Killed Apply Frenzy To Self"), 1)
                    };
                }));
        }

        private void createLilBigBerry()
        {
            statusEffects.Add(StatusCopy("On Hit Equal Overload To Target", "On Hit Heal Equal to Allies").WithText("Restore <keyword=health> equal to damage dealt to all allies").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                (data as StatusEffectApplyXOnHit).applyToFlags = StatusEffectApplyX.ApplyToFlags.Allies;
                (data as StatusEffectApplyXOnHit).effectToApply = Get<StatusEffectData>("Heal");
                ((StatusEffectApplyX)(data as StatusEffectApplyXOnHit)).noTargetType = NoTargetType.None;
            }));
            statusEffects.Add(StatusCopy("When Ally Is Healed Apply Double Spice", "When Ally Is Healed Gain Attack").WithText("When an ally is healed, gain <+{a}><keyword=attack>").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                (data as StatusEffectApplyXWhenAllyHealed).applyEqualAmount = false;
                ((StatusEffectApplyX)(data as StatusEffectApplyXWhenAllyHealed)).equalAmountBonusMult = 1f;
                (data as StatusEffectApplyXWhenAllyHealed).applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                (data as StatusEffectApplyXWhenAllyHealed).effectToApply = Get<StatusEffectData>("Increase Attack");
            }));
            cards.Add(new CardDataBuilder(this).CreateUnit("LilBigBerryDuo", "Lil' Berry and Big Berry").SetSprites("SpriteWork/LilBigBerry.png", "SpriteWork/LilBigBerry_BG.png").SetStats(10, 2, 3)
                .WithCardType()
                .SubscribeToAfterAllBuildEvent(delegate (CardData card)
                {
                    card.startWithEffects = new CardData.StatusEffectStacks[2]
                    {
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("On Hit Heal Equal to Allies"), 1),
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("When Ally Is Healed Gain Attack"), 1)
                    };
                }));
        }

        private void createFolbyKnuckles()
        {
            statusEffects.Add(StatusCopy("When Hit Equal Damage To Attacker", "When Hit Damage To Attacker Equal cards Hand").WithText("When hit, deal damage back to the attacker equal to 3x cards in hand").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                (data as StatusEffectApplyX).applyEqualAmount = false;
                (data as StatusEffectApplyX).scriptableAmount = new ScriptableBetterCardsInHand
                {
                    Multiplier = 3
                };
                (data as StatusEffectApplyX).noTargetType = NoTargetType.None;
            }));
            cards.Add(new CardDataBuilder(this).CreateUnit("THECONE", "The Cones").SetSprites("SpriteWork/THECONE.png", "SpriteWork/THECONE_BG.png").SetStats()
                .WithCardType("Clunker")
                .SubscribeToAfterAllBuildEvent(delegate (CardData card)
                {
                    card.startWithEffects = new CardData.StatusEffectStacks[3]
                    {
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("Scrap"), 5),
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("When Hit Draw"), 2),
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("When Hit Damage To Attacker Equal cards Hand"), 1)
                    };
                }));
            statusEffects.Add(StatusCopy("Summon Beepop", "Summon THE CONE").WithText("Create <card=" + GUID + ".THECONE>").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                (data as StatusEffectSummon).summonCard = Get<CardData>(GUID + ".THECONE").Clone();
                (data as StatusEffectSummon).gainTrait = null;
                (data as StatusEffectSummon).setCardType = null;
            }));
            cards.Add(new CardDataBuilder(this).CreateUnit("FolbyKnucklesDuo", "Folby and Knuckles").SetSprites("SpriteWork/FolbyKnuckles.png", "SpriteWork/FolbyKnuckles_BG.png").SetStats(9, 4, 4)
                .WithCardType()
                .SubscribeToAfterAllBuildEvent(delegate (CardData card)
                {
                    card.startWithEffects = new CardData.StatusEffectStacks[1]
                    {
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("Summon THE CONE"), 1)
                    };
                    card.traits = new List<CardData.TraitStacks>
                    {
                    new CardData.TraitStacks(Get<TraitData>("Trash"), 2)
                    };
                }));
        }

        private void createNomStompyToaster()
        {
            statusEffects.Add(StatusCopy("On Card Played Trigger Against AllyBehind", "On Card Played Trigger Against Allies In Row").WithText("Also hits allies in row").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                (data as StatusEffectApplyXOnCardPlayed).applyToFlags = StatusEffectApplyX.ApplyToFlags.AlliesInRow;
            }));
            statusEffects.Add(StatusCopy("Sacrifice Ally", "KillWord").WithText("Kill Target"));
            cards.Add(new CardDataBuilder(this).CreateUnit("NomStompyToasterQuad", "Nom, Stompy and Toaster").SetSprites("SpriteWork/NomStompToast.png", "SpriteWork/NomStompToast_BG.png").SetStats(10, 1, 6)
                .WithCardType()
                .SubscribeToAfterAllBuildEvent(delegate (CardData card)
                {
                    card.attackEffects = new CardData.StatusEffectStacks[1]
                    {
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("KillWord"), 1)
                    };
                    card.startWithEffects = new CardData.StatusEffectStacks[1]
                    {
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("On Card Played Trigger Against Allies In Row"), 1)
                    };
                }));
        }

        private void createaMiniMikaKreggo()
        {
            statusEffects.Add(StatusCopy("When Card Destroyed, Gain Attack", "When Card Destroyed, Gain Frenzy").WithText("When a card is destroyed, gain <x{a}><keyword=frenzy>").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                (data as StatusEffectApplyXWhenCardDestroyed).effectToApply = Get<StatusEffectData>("MultiHit");
            }));
            statusEffects.Add(StatusCopy("When Hit With Junk Add Frenzy To Self", "When Hit With Junk Add Attack To Self").WithText("When hit with <card=Junk>, gain <+{a}><keyword=attack>").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                (data as StatusEffectApplyXWhenHit).effectToApply = Get<StatusEffectData>("Increase Attack");
            }));
            cards.Add(new CardDataBuilder(this).CreateUnit("MiniMikaKreggoDuo", "Mini Mika and Kreggo").SetSprites("SpriteWork/MiniMikaKreggo.png", "SpriteWork/MiniMikaKreggo_BG.png").SetStats(7, 1, 4)
                .WithCardType()
                .SubscribeToAfterAllBuildEvent(delegate (CardData card)
                {
                    card.startWithEffects = new CardData.StatusEffectStacks[2]
                    {
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("When Card Destroyed, Gain Frenzy"), 1),
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("When Hit With Junk Add Attack To Self"), 3)
                    };
                }));
        }

        private void createFoxeeFizzle()
        {
            statusEffects.Add(StatusCopy("On Turn Apply Spice To Self", "On Turn Apply Frenzy To Self").WithText("Gain <x{a}><keyword=frenzy>").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                (data as StatusEffectApplyXOnTurn).effectToApply = Get<StatusEffectData>("MultiHit");
            }));
            cards.Add(new CardDataBuilder(this).CreateUnit("FoxeeFizzleDuo", "Foxee and Fizzle").SetSprites("SpriteWork/FoxeeFizzle.png", "SpriteWork/FoxeeFizzleDuo.png").SetStats(5, 1, 4)
                .WithCardType()
                .SubscribeToAfterAllBuildEvent(delegate (CardData card)
                {
                    card.startWithEffects = new CardData.StatusEffectStacks[1]
                    {
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("On Turn Apply Frenzy To Self"), 1)
                    };
                }));
        }

        private void createChompomShelly()
        {
            statusEffects.Add(StatusCopy("While Active Frenzy To Allies", "While Active Chompom To Allies").WithText("While active, allies deal additional damage equal to <keyword=shell>").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                (data as StatusEffectWhileActiveX).effectToApply = Get<StatusEffectData>("Bonus Damage Equal To Shell");
            }));
            cards.Add(new CardDataBuilder(this).CreateUnit("ChompomShellyDuo", "Chompom and Shelly").SetSprites("SpriteWork/ChompomShellyDuo.png", "SpriteWork/ChompomShellyDuo_BG.png").SetStats(8, 2, 3)
                .WithCardType()
                .SubscribeToAfterAllBuildEvent(delegate (CardData card)
                {
                    card.startWithEffects = new CardData.StatusEffectStacks[2]
                    {
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("On Turn Apply Shell To Allies"), 2),
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("While Active Chompom To Allies"), 1)
                    };
                }));
        }

        private void createZulaTaiga()
        {
            statusEffects.Add(StatusCopy("When Hit Apply Shroom To Attacker", "When Hit Apply Overload To Attacker").WithText("When hit, apply <{a}><keyword=overload> to the attacker").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                (data as StatusEffectApplyXWhenHit).effectToApply = Get<StatusEffectData>("Overload");
            }));
            statusEffects.Add(StatusCopy("On Card Played Boost To Allies & Enemies", "On Card Played Boost To Self").WithText("Increase own effects by {a}").WithCanBeBoosted(value: false).SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                (data as StatusEffectApplyXOnCardPlayed).applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                (data as StatusEffectApplyXOnCardPlayed).canBeBoosted = false;
            }));
            cards.Add(new CardDataBuilder(this).CreateUnit("TaigaZulaDuo", "Taiga and Zula").SetSprites("SpriteWork/TaigaZulaDuo.png", "SpriteWork/TaigaZulaDuo_BG.png").SetStats(9, null, 4)
                .WithCardType()
                .SubscribeToAfterAllBuildEvent(delegate (CardData card)
                {
                    card.startWithEffects = new CardData.StatusEffectStacks[2]
                    {
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("When Hit Apply Overload To Attacker"), 2),
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("On Card Played Boost To Self"), 2)
                    };
                }));
        }

        private void createVanJunLupa()
        {
            statusEffects.Add(StatusCopy("When Hit Add Frenzy To Self", "When Hit Add AttackHealth to allies").WithText("When hit, add <+{a}><keyword=attack>/<+{a}><keyword=health> to allies").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                (data as StatusEffectApplyXWhenHit).effectToApply = Get<StatusEffectData>("Increase Attack & Health (No Constraints)");
                (data as StatusEffectApplyXWhenHit).applyToFlags = StatusEffectApplyX.ApplyToFlags.Allies;
                ((StatusEffectApplyX)(data as StatusEffectApplyXWhenHit)).noTargetType = NoTargetType.None;
            }));
            statusEffects.Add(StatusCopy("On Card Played Boost To Allies & Enemies", "On Card Played Boost To allies").WithText("Increase Effects on allies by <{a}>").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                (data as StatusEffectApplyXOnCardPlayed).applyToFlags = StatusEffectApplyX.ApplyToFlags.Allies;
                ((StatusEffectApplyX)(data as StatusEffectApplyXOnCardPlayed)).noTargetType = NoTargetType.None;
            }));
            cards.Add(new CardDataBuilder(this).CreateUnit("VanJunLupaDuo", "Lupa and Van Jun").SetSprites("SpriteWork/VanJunLupaDuo.png", "SpriteWork/VanJunLupaDuo_BG.png").SetStats(6, 2, 4)
                .WithCardType()
                .SubscribeToAfterAllBuildEvent(delegate (CardData card)
                {
                    card.startWithEffects = new CardData.StatusEffectStacks[2]
                    {
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("When Hit Add AttackHealth to allies"), 2),
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("On Card Played Boost To allies"), 2)
                    };
                }));
        }

        private void createTinkersonHazeblazer()
        {
            statusEffects.Add(StatusCopy("When Enemy Is Hit By Item Apply Demonize To Them", "When Enemy Is Hit By Junk Apply Haze To Them").WithText("When an enemy is hit with <card=Junk>, apply <{a}><keyword=haze> to them").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                (data as StatusEffectApplyXWhenUnitIsHit).attackerConstraints = new TargetConstraint[1]
                {
                new TargetConstraintIsSpecificCard
                {
                    allowedCards = new CardData[1] { Get<CardData>("Junk") }
                }
                };
                (data as StatusEffectApplyXWhenUnitIsHit).effectToApply = Get<StatusEffectData>("Haze");
                ((StatusEffectApplyX)(data as StatusEffectApplyXWhenUnitIsHit)).noTargetType = NoTargetType.None;
            }));
            cards.Add(new CardDataBuilder(this).CreateUnit("TinkersonHazeDuo", "Tinkerson Jr and Hazeblazer").SetSprites("SpriteWork/TinkersonHazeDuo.png", "SpriteWork/TinkersonHazeDuo_BG.png").SetStats(9, 4, 4)
                .WithCardType()
                .SetTraits(new CardData.TraitStacks(Get<TraitData>("Trash"), 1))
                .SubscribeToAfterAllBuildEvent(delegate (CardData card)
                {
                    card.startWithEffects = new CardData.StatusEffectStacks[1]
                    {
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("When Enemy Is Hit By Junk Apply Haze To Them"), 1)
                    };
                }));
        }

        private void createGojiberRoibos()
        {
            statusEffects.Add(StatusCopy("On Hit Equal Overload To Target", "On Hit attack Equal to health to Ally").WithText("Add <keyword=attack> equal to my <keyword=health> to a random ally").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                (data as StatusEffectApplyX).applyEqualAmount = true;
                ((StatusEffectApplyX)(data as StatusEffectApplyXOnHit)).scriptableAmount = new ScriptableCurrentHealth();
                (data as StatusEffectApplyXOnHit).applyToFlags = StatusEffectApplyX.ApplyToFlags.RandomAlly;
                (data as StatusEffectApplyXOnHit).effectToApply = Get<StatusEffectData>("Increase Attack");
                ((StatusEffectApplyX)(data as StatusEffectApplyXOnHit)).noTargetType = NoTargetType.None;
            }));
            cards.Add(new CardDataBuilder(this).CreateUnit("GojiberRoisbosDuo", "Gojiber and Roibos").SetSprites("SpriteWork/GojiberRoisbosDuo.png", "SpriteWork/GojiberRoibosDuo_BG.png").SetStats(4, 4, 4)
                .WithCardType()
                .SetTraits(new CardData.TraitStacks(Get<TraitData>("Heartburn"), 1))
                .SubscribeToAfterAllBuildEvent(delegate (CardData card)
                {
                    card.startWithEffects = new CardData.StatusEffectStacks[1]
                    {
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("On Hit attack Equal to health to Ally"), 1)
                    };
                }));
        }

        private void createVestaSnoffel()
        {
            statusEffects.Add(StatusCopy("On Turn Apply Snow To Enemies", "On Turn Apply Overload To Enemies").WithText("Apply <{a}><keyword=overload> to all enemies").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                ((StatusEffectApplyX)(data as StatusEffectApplyXOnTurn)).noTargetType = NoTargetType.None;
                (data as StatusEffectApplyXOnTurn).effectToApply = Get<StatusEffectData>("Overload");
            }));
            cards.Add(new CardDataBuilder(this).CreateUnit("VestaSnoffelDuo", "Vesta and Snoffel").SetSprites("SpriteWork/VestaSnoffelDuo.png", "SpriteWork/VestaSnoffelDuo_BG.png").SetStats(6, null, 4)
                .WithCardType()
                .SubscribeToAfterAllBuildEvent(delegate (CardData card)
                {
                    card.startWithEffects = new CardData.StatusEffectStacks[1]
                    {
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("On Turn Apply Overload To Enemies"), 3)
                    };
                }));
        }

        private void createNeedleScavern()
        {
            statusEffects.Add(StatusCopy("Instant Destroy Junk In Hand And Draw For Each", "Instant Destroy Items In Hand And Trash For Each").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                (data as StatusEffectInstantDestroyCardsInHandAndApplyXForEach).destroyConstraints = new TargetConstraint[2]
                {
                new TargetConstraintIsCardType
                {
                    allowedTypes = new CardType[1] { Get<CardType>("Item") }
                },
                new TargetConstraintIsSpecificCard
                {
                    allowedCards = new CardData[1] { Get<CardData>("Junk").Clone() },
                    not = true
                }
                };
                (data as StatusEffectInstantDestroyCardsInHandAndApplyXForEach).effectToApply = Get<StatusEffectData>("Instant Summon Junk In Hand");
            }));
            statusEffects.Add(StatusCopy("On Card Played Destroy All Junk In Hand And Draw For Each", "On Card Played Destroy All Items In Hand And Trash For Each").WithText("Destroy all\nnon-<card=Junk> <item> cards in hand and {0} for each").WithTextInsert("<keyword=trash> <{a}>").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                (data as StatusEffectApplyXOnCardPlayed).effectToApply = Get<StatusEffectData>("Instant Destroy Items In Hand And Trash For Each");
            }));
            cards.Add(new CardDataBuilder(this).CreateUnit("NeedleScavernDuo", "Needle and Scaven").SetSprites("SpriteWork/NeedleScavernDuo.png", "SpriteWork/NeedleScavernDuo_BG.png").SetStats(15, 4, 2)
                .WithCardType()
                .SubscribeToAfterAllBuildEvent(delegate (CardData card)
                {
                    card.startWithEffects = new CardData.StatusEffectStacks[2]
                    {
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("On Card Played Destroy All Items In Hand And Trash For Each"), 3),
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("MultiHit"), 1)
                    };
                }));
        }

        private void createJumboBlunky()
        {
            statusEffects.Add(StatusCopy("On Turn Apply Spice To AllyBehind", "On Turn Apply Block To Allies In Row").WithText("Apply <{a}><keyword=block> to allies in the row").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                ((StatusEffectApplyX)(data as StatusEffectApplyXOnTurn)).noTargetType = NoTargetType.None;
                (data as StatusEffectApplyXOnTurn).effectToApply = Get<StatusEffectData>("Block");
                (data as StatusEffectApplyXOnTurn).applyToFlags = StatusEffectApplyX.ApplyToFlags.AlliesInRow;
            }));
            cards.Add(new CardDataBuilder(this).CreateUnit("JumboBlunkyDuo", "Jumbo and Blunky").SetSprites("SpriteWork/JumboBlunkyDuo.png", "SpriteWork/JumboBlunkyDuo_BG.png").SetStats(10, 5, 3)
                .WithCardType()
                .SubscribeToAfterAllBuildEvent(delegate (CardData card)
                {
                    card.startWithEffects = new CardData.StatusEffectStacks[1]
                    {
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("On Turn Apply Block To Allies In Row"), 2)
                    };
                }));
        }

        private void createFulbertBonnie()
        {
            statusEffects.Add(new StatusEffectDataBuilder(this).Create<StatusEffectMeldXYZAllies>("SHELLSPICEHEALFUSION").WithCanBeBoosted(value: false).WithText("When an ally is <keyword=shell>'d,<keyword=spice>'d or <keyword=health> restored, apply equal of the other two")
                .WithType("")
                .FreeModify(delegate (StatusEffectMeldXYZAllies data)
                {
                    data.statusType1 = "shell";
                    data.statusType2 = "spice";
                    data.statusType3 = "heal";
                    data.effectToApply = Get<StatusEffectData>("Shell").InstantiateKeepName();
                    data.effectToApply2 = Get<StatusEffectData>("Spice").InstantiateKeepName();
                    data.effectToApply3 = Get<StatusEffectData>("Heal (No Ping)").InstantiateKeepName();
                    data.eventPriority = 1;
                }));
            cards.Add(new CardDataBuilder(this).CreateUnit("FulbertBonnieDuo", "Fulbert and Bonnie").SetSprites("SpriteWork/FulbertBonnieDuo.png", "SpriteWork/FulbertBonnieDuo_BG.png").SetStats(6, 0, 3)
                .WithCardType()
                .SetAttackEffect(new CardData.StatusEffectStacks(Get<StatusEffectData>("Shroom"), 3))
                .SubscribeToAfterAllBuildEvent(delegate (CardData card)
                {
                    card.startWithEffects = new CardData.StatusEffectStacks[1]
                    {
                    new CardData.StatusEffectStacks(Get<StatusEffectData>(GUID + ".SHELLSPICEHEALFUSION"), 1)
                    };
                }));
        }

        private void createNakedGnomes()
        {
            statusEffects.Add(StatusCopy("Temporary Summoned", "Temporary Hogheaded").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                ((StatusEffectTemporaryTrait)data).trait = Get<TraitData>("Pigheaded");
            }));
            statusEffects.Add(StatusCopy("Temporary Summoned", "Temporary Faith").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                ((StatusEffectTemporaryTrait)data).trait = Get<TraitData>("Effigy");
            }));
            statusEffects.Add(StatusCopy("Summon Beepop", "Summon_NakedGnome").WithText("Summon <card=NakedGnomeFriendly>").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                ((StatusEffectSummon)data).summonCard = Get<CardData>("NakedGnomeFriendly").Clone();
            }));
            statusEffects.Add(StatusCopy("On Card Played Apply Attack To Self", "On Card Played Apply Effigy To Self").WithText("Gain <keyword=effigy> <{a}>").SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
            {
                ((StatusEffectApplyXOnCardPlayed)data).effectToApply = Get<StatusEffectData>("Temporary Faith");
            }));
            cards.Add(new CardDataBuilder(this).CreateUnit("NakedGnomeLupaDuo", "Naked Gnome and Lupa").SetSprites("SpriteWork/NakedGnomeLupaDuo.png", "SpriteWork/NakedGnomeDuo_BG.png").SetStats(7, null, 3)
                .WithCardType()
                .SubscribeToAfterAllBuildEvent(delegate (CardData card)
                {
                    card.startWithEffects = new CardData.StatusEffectStacks[2]
                    {
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("On Card Played Apply Effigy To Self"), 4),
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("Summon_NakedGnome"), 1)
                    };
                }));
            cards.Add(new CardDataBuilder(this).CreateUnit("NakedGnomeSnoffelDuo", "Naked Gnome and Snoffel").SetSprites("SpriteWork/NakedGnomeSnoffelDuo.png", "SpriteWork/NakedGnomeDuo_BG.png").SetStats(7, null, 3)
                .WithCardType()
                .SubscribeToAfterAllBuildEvent(delegate (CardData card)
                {
                    card.startWithEffects = new CardData.StatusEffectStacks[2]
                    {
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("On Card Played Apply Effigy To Self"), 4),
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("Summon_NakedGnome"), 1)
                    };
                }));
        }

        private void updatecardart(CardData card)
        {
            if (nonspriteart)
            {
                if (card.name == GUID + ".KernelFirefistDuo")
                {
                    card.mainSprite = ImagePath("NewArt\\Kernel_And_Firefist_Just_Art_Card_Ready.png").ToSprite();
                    card.backgroundSprite = ImagePath("NewArt\\Kernel_And_Firefist_BG_Card_Ready.png").ToSprite();
                }
                else if (card.name == GUID + ".NovaBomBomDuo")
                {
                    card.mainSprite = ImagePath("NewArt/NovaBomBomFanArt.png").ToSprite();
                    card.backgroundSprite = ImagePath("SpriteWork/NovaBombom_BG.png").ToSprite();
                }
            }
            else if (card.name == GUID + ".KernelFirefistDuo")
            {
                card.mainSprite = ImagePath("SpriteWork\\KernelFirefist.png").ToSprite();
                card.backgroundSprite = ImagePath("SpriteWork\\KernelFirefist_BG.png").ToSprite();
            }
            else if (card.name == GUID + ".NovaBomBomDuo")
            {
                card.mainSprite = ImagePath("SpriteWork/NovaBombom.png").ToSprite();
                card.backgroundSprite = ImagePath("SpriteWork/NovaBombom_BG.png").ToSprite();
            }
        }

        public override void Load()
        {
            if (!preLoaded)
            {
                createmodassets();
            }
            Events.OnSceneLoaded += SceneLoaded;
            Events.OnCardDataCreated += updatecardart;
            Events.OnModLoaded += SubscribeToAfterAllBuildEvent2;
            base.Load();
        }

        public override void Unload()
        {
            Events.OnSceneLoaded -= SceneLoaded;
            Events.OnCardDataCreated -= updatecardart;
            Events.OnModLoaded -= SubscribeToAfterAllBuildEvent2;
            base.Unload();
            FixConstraints();
        }

        public override List<T> AddAssets<T, TY>()
        {
            string name = typeof(TY).Name;
            string a = name;
            List<T> result;
            if (!(a == "CardData"))
            {
                if (!(a == "StatusEffectData"))
                {
                    if (!(a == "KeywordData"))
                    {
                        if (!(a == "TraitData"))
                        {
                            result = null;
                        }
                        else
                        {
                            result = this.traits.Cast<T>().ToList<T>();
                        }
                    }
                    else
                    {
                        result = this.keywords.Cast<T>().ToList<T>();
                    }
                }
                else
                {
                    result = this.statusEffects.Cast<T>().ToList<T>();
                }
            }
            else
            {
                result = this.cards.Cast<T>().ToList<T>();
            }
            return result;
        }
    }

}
