using Deadpan.Enums.Engine.Components.Modding; // this allows us to make WildfrostMod's

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
//using UnityEditor;
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
using WildfrostHopeMod.Utils; // Creates TMP_SpriteAsset
using WildfrostHopeMod.VFX;
using Extensions = Deadpan.Enums.Engine.Components.Modding.Extensions;
using System.Security.Cryptography;   // Declares StatusIconBuilder

namespace TopStar
{
    // Harmony Stuff
    [HarmonyPatch(typeof(References), nameof(References.Classes), MethodType.Getter)]
    static class FixClassesGetter
    {
        static void Postfix(ref ClassData[] __result) => __result = AddressableLoader.GetGroup<ClassData>("ClassData").ToArray();
    }

    [HarmonyPatch(typeof(TribeHutSequence), "SetupFlags")]
    class PatchTribeHut
    {
        static string TribeName = "CaretakerTribe";
        static void Postfix(TribeHutSequence __instance)
        {
            GameObject gameObject = GameObject.Instantiate(__instance.flags[0].gameObject);
            gameObject.transform.SetParent(__instance.flags[0].gameObject.transform.parent, false);
            TribeFlagDisplay flagDisplay = gameObject.GetComponent<TribeFlagDisplay>();
            ClassData tribe = TopStarMod.Instance.TryGet<ClassData>(TribeName);
            flagDisplay.flagSprite = tribe.flag;
            __instance.flags = __instance.flags.Append(flagDisplay).ToArray();
            flagDisplay.SetAvailable();
            flagDisplay.SetUnlocked();

            var sequence2 = Resources.FindObjectsOfTypeAll<TribeDisplaySequence>()
                .FirstOrDefault(s =>
                    s != null &&
                    s.gameObject.scene.IsValid() &&            // exclude assets/prefabs not in a scene
                    s.gameObject.hideFlags == HideFlags.None   // exclude hidden/editor objects
                );
            GameObject gameObject2 = GameObject.Instantiate(sequence2.displays[1].gameObject);
            gameObject2.transform.SetParent(sequence2.displays[2].gameObject.transform.parent, false);
            sequence2.tribeNames = sequence2.tribeNames.Append(TribeName).ToArray();
            sequence2.displays = sequence2.displays.Append(gameObject2).ToArray();

            Button button = flagDisplay.GetComponentInChildren<Button>();
            button.onClick.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.Off);
            button.onClick.AddListener(() => { sequence2.Run(TribeName); });

            //(SfxOneShot)
            gameObject2.GetComponent<SfxOneshot>().eventRef = FMODUnity.RuntimeManager.PathToEventReference("event:/sfx/card/draw_multi");

            //0: Flag (ImageSprite)
            gameObject2.transform.GetChild(0).GetComponent<ImageSprite>().SetSprite(tribe.flag);

            //1: Left (ImageSprite)
            List<string> sprname = new List<string> { "Starlight", "Solarflare", "Eclipse", "Stella", "Sui" };
            int spr = UnityEngine.Random.Range(0, sprname.Count);

            Sprite needle = TopStarMod.Instance.TryGet<CardData>(sprname[spr]).mainSprite;
            gameObject2.transform.GetChild(1).GetComponent<ImageSprite>().SetSprite(needle);

            //2: Right (ImageSprite)
            sprname = new List<string> { "Zerk", "Khris", "Cat", "Jijikan", "Kaido" };
            spr = UnityEngine.Random.Range(0, sprname.Count);

            Sprite muncher = TopStarMod.Instance.TryGet<CardData>(sprname[spr]).mainSprite;
            gameObject2.transform.GetChild(2).GetComponent<ImageSprite>().SetSprite(muncher);
            gameObject2.transform.GetChild(2).localScale *= 1.2f;

            //3: Textbox (Image)
            gameObject2.transform.GetChild(3).GetComponent<Image>().color = new Color(0.9f, 0.35f, 0.35f);

            //3-0: Text (LocalizedString)
            StringTable collection = LocalizationHelper.GetCollection("UI Text", SystemLanguage.English);
            gameObject2.transform.GetChild(3).GetChild(0).GetComponent<LocalizeStringEvent>().StringReference = collection.GetString(TopStarMod.Instance.TribeDescKey);

            //4:Title Ribbon (Image)
            //4-0: Text (LocalizedString)
            gameObject2.transform.GetChild(4).GetChild(0).GetComponent<LocalizeStringEvent>().StringReference = collection.GetString(TopStarMod.Instance.TribeTitleKey);
        }
    }



    //Combination
    internal class StatusEffectInstantCombineCard : StatusEffectInstant
    {

        [Serializable]
        public struct Combo
        {
            public string[] cardNames;

            public string resultingCardName;

            public bool AllCardsInDeck(List<Entity> deck)
            {
                bool result = true;
                string[] array = cardNames;
                foreach (string cardName in array)
                {
                    if (!HasCard(cardName, deck))
                    {
                        result = false;
                        break;
                    }
                }

                return result;
            }

            public List<Entity> FindCards(List<Entity> deck)
            {
                List<Entity> tooFuse = new List<Entity>();
                string[] array = cardNames;
                foreach (string cardName in array)
                {
                    foreach (Entity item in deck)
                    {
                        if (item.data.name == cardName)
                        {
                            tooFuse.Add(item);
                            break;
                        }
                    }
                }

                return tooFuse;
            }

            public bool HasCard(string cardName, List<Entity> deck)
            {
                foreach (Entity item in deck)
                {
                    if (item.data.name == cardName)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        [SerializeField]
        public string combineSceneName = "CardCombine";

        public string[] cardNames;

        public string resultingCardName;

        public bool checkHand = true;
        public bool checkDeck = true;
        public bool checkBoard = true;

        public bool keepUpgrades = true;
        public List<CardUpgradeData> extraUpgrades;

        public bool spawnOnBoard = false;

        public bool changeDeck = false;

        public override IEnumerator Process()
        {
            Combo combo = new Combo()
            {
                cardNames = cardNames,
                resultingCardName = resultingCardName
            };

            List<Entity> fulldeck = new List<Entity>();
            if (checkHand)
            {
                fulldeck.AddRange(References.Player.handContainer.ToList());
            }
            if (checkDeck)
            {
                fulldeck.AddRange(References.Player.drawContainer.ToList());
                fulldeck.AddRange(References.Player.discardContainer.ToList());
            }
            if (checkBoard)
            {
                fulldeck.AddRange(Battle.GetCardsOnBoard(References.Player).ToList());
            }


            if (combo.AllCardsInDeck(fulldeck))
            {
                CombineAction action = new CombineAction(keepUpgrades, extraUpgrades, spawnOnBoard, target.containers[0]);
                action.combineSceneName = combineSceneName;
                action.tooFuse = combo.FindCards(fulldeck);
                action.combo = combo;

                if (changeDeck)
                {
                    EditDeck(combo.cardNames, combo.resultingCardName);
                }

                bool queueAction = true;
                foreach (PlayAction playAction in ActionQueue.instance.queue)
                {
                    if (playAction.GetType() == action.GetType())
                    {
                        queueAction = false;
                        break;
                    }
                }

                if (queueAction)
                {
                    ActionQueue.Stack(action);
                }

            }

            yield return base.Process();
        }

        public void EditDeck(string[] cardsToRemove, string cardToAdd)
        {
            List<CardData> oldCards = new List<CardData>();

            foreach (string name in cardsToRemove)
            {
                foreach (CardData card in References.Player.data.inventory.deck)
                {
                    if (card.name == name && !oldCards.Contains(card))
                    {
                        oldCards.Add(card);
                        break;
                    }
                }
            }

            if (oldCards.Count == cardsToRemove.Length)
            {
                List<CardUpgradeData> upgrades = new List<CardUpgradeData> { };

                foreach (CardData card in oldCards)
                {
                    if (keepUpgrades)
                    {
                        upgrades.AddRange(card.upgrades.Select(u => u.Clone()));
                    }

                    References.Player.data.inventory.deck.Remove(card);
                }

                CardData cardDataClone = AddressableLoader.GetCardDataClone(cardToAdd);

                upgrades.AddRange(extraUpgrades.Select(u => u.Clone()));

                foreach (CardUpgradeData upgrade in upgrades)
                {
                    upgrade.Assign(cardDataClone);
                }

                References.Player.data.inventory.deck.Add(cardDataClone);


            }


        }

        public class CombineAction : PlayAction
        {

            [SerializeField]
            public string combineSceneName;

            public Combo combo;

            public List<Entity> tooFuse;

            public bool keepUpgrades;

            public List<CardUpgradeData> extraUpgrades;

            public bool spawnOnBoard;

            public CardContainer row;

            public CombineAction(bool keepUpgrades, List<CardUpgradeData> extraUpgrades, bool spawnOnBoard, CardContainer row)
            {
                this.keepUpgrades = keepUpgrades;
                this.extraUpgrades = extraUpgrades;
                this.spawnOnBoard = spawnOnBoard;
                this.row = row;


            }

            public override IEnumerator Run()
            {
                return CombineSequence(combo, tooFuse);
            }

            public IEnumerator CombineSequence(Combo combo, List<Entity> tooFuse)
            {
                CombineCardSequence combineSequence = null;
                yield return SceneManager.Load(combineSceneName, SceneType.Temporary, delegate (Scene scene)
                {
                    combineSequence = scene.FindObjectOfType<CombineCardSequence>();
                });
                if ((bool)combineSequence)
                {
                    yield return combineSequence.Run2(tooFuse, combo.resultingCardName, keepUpgrades, extraUpgrades, spawnOnBoard, row);
                }

                yield return SceneManager.Unload(combineSceneName);
            }

        }

    }
    public static class CombineCardSequenceExtension
    {
        public static IEnumerator Run2(this CombineCardSequence seq, List<Entity> cardsToCombine, string resultingCard, bool keepUpgrades, List<CardUpgradeData> extraUpgrades, bool spawnOnBoard, CardContainer row)
        {
            CardData cardDataClone = AddressableLoader.GetCardDataClone(resultingCard);

            List<CardUpgradeData> upgrades = new List<CardUpgradeData> { };
            if (keepUpgrades)
            {
                foreach (Entity ent in cardsToCombine)
                {
                    upgrades.AddRange(ent.data.upgrades.Select(u => u.Clone()));
                }
            }
            upgrades.AddRange(extraUpgrades.Select(u => u.Clone()));

            foreach (CardUpgradeData upgrade in upgrades)
            {
                upgrade.Assign(cardDataClone);
            }


            yield return Run2(seq, cardsToCombine.ToArray(), cardDataClone, spawnOnBoard, row);
        }

        public static IEnumerator Run2(this CombineCardSequence seq, Entity[] entities, CardData finalCard, bool spawnOnBoard, CardContainer row)
        {
            Debug.Log("[TopStar] Combo.");//Debug
            PauseMenu.Block();
            Card card = CardManager.Get(finalCard, Battle.instance.playerCardController, References.Player, inPlay: false, isPlayerCard: true);
            card.transform.localScale = Vector3.one * 1f;
            card.transform.SetParent(seq.finalEntityParent);
            Entity finalEntity = card.entity;
            Routine.Clump clump = new Routine.Clump();
            Entity[] array = entities;
            foreach (Entity entity in array)
            {
                clump.Add(entity.display.UpdateData());
            }

            clump.Add(finalEntity.display.UpdateData());
            clump.Add(Sequences.Wait(0.5f));
            yield return clump.WaitForEnd();

            array = entities;
            foreach (Entity entity2 in array)
            {
                entity2.RemoveFromContainers();
            }

            array = entities;
            for (int i = 0; i < array.Length; i++)
            {
                array[i].transform.localScale = Vector3.one * 0.8f;
            }

            seq.fader.In();
            Vector3 zero = Vector3.zero;
            array = entities;
            foreach (Entity entity3 in array)
            {
                zero += entity3.transform.position;
            }

            zero /= (float)entities.Length;

            seq.group.position = zero;
            array = entities;
            foreach (Entity entity4 in array)
            {
                Transform transform = UnityEngine.Object.Instantiate(seq.pointPrefab, entity4.transform.position, Quaternion.identity, seq.group);
                transform.gameObject.SetActive(value: true);
                entity4.transform.SetParent(transform);
                entity4.flipper.FlipUp();
                seq.points.Add(transform);
                LeanTween.alphaCanvas(((Card)entity4.display).canvasGroup, 1f, 0.4f).setEaseInQuad();
            }

            foreach (Transform point in seq.points)
            {
                LeanTween.moveLocal(to: point.localPosition.normalized, gameObject: point.gameObject, time: 0.4f).setEaseInQuart();
            }

            yield return new WaitForSeconds(0.4f);

            Events.InvokeScreenShake(1f, 0f);
            array = entities;
            for (int i = 0; i < array.Length; i++)
            {
                array[i].wobbler.WobbleRandom();
            }

            foreach (Transform point2 in seq.points)
            {
                LeanTween.moveLocal(to: point2.localPosition.normalized * 3f, gameObject: point2.gameObject, time: 1f).setEase(seq.bounceCurve);
            }

            LeanTween.moveLocal(seq.group.gameObject, new Vector3(0f, 0f, -2f), 1f).setEaseInOutQuad();
            LeanTween.rotateZ(seq.group.gameObject, Dead.PettyRandom.Range(160f, 180f), 1f).setOnUpdateVector3(delegate
            {
                foreach (Transform point3 in seq.points)
                {
                    point3.transform.eulerAngles = Vector3.zero;
                }
            }).setEaseInOutQuad();
            yield return new WaitForSeconds(1f);

            Events.InvokeScreenShake(1f, 0f);
            if ((bool)seq.ps)
            {
                seq.ps.Play();
            }

            seq.combinedFx.SetActive(value: true);

            finalEntity.transform.position = Vector3.zero;
            array = entities;
            for (int i = 0; i < array.Length; i++)
            {
                CardManager.ReturnToPool(array[i]);
            }

            seq.group.transform.localRotation = Quaternion.identity;
            finalEntity.curveAnimator.Ping();
            finalEntity.wobbler.WobbleRandom();

            yield return new WaitForSeconds(1f);

            seq.fader.gameObject.Destroy();
            PauseMenu.Unblock();

            //
            bool flag = true;
            if (spawnOnBoard)
            {
                Debug.Log("[TopStar] Spawn on Board.");//Debug
                if (row.owner == References.Player && row.Count != 3)
                {
                    yield return Sequences.CardMove(finalEntity, new CardContainer[1] { row });
                    finalEntity.inPlay = true;
                    flag = false;
                }

                if (flag)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        row = Battle.instance.GetRow(References.Player, i);
                        if (row.Count != 3)
                        {

                            yield return Sequences.CardMove(finalEntity, new CardContainer[1] { row });
                            finalEntity.inPlay = true;
                            flag = false;

                            break;
                        }
                    }
                }



            }

            //
            if (flag)
            {
                yield return Sequences.CardMove(finalEntity, new CardContainer[1] { References.Player.handContainer });
                finalEntity.inPlay = true;
            }

            References.Player.handContainer.TweenChildPositions();
            ActionQueue.Add(new ActionReveal(finalEntity));

        }

    }



    //For example, getting to the player's deck would be:  References.PlayerData.inventory.deck.
    //So, adding a card, say Junk, to your deck would be:  References.PlayerData.inventory.deck.Add(Get<CardData>("Junk").Clone()).
    public class TopStarMod : WildfrostMod
    {
        // Our mod's constructor
        public TopStarMod(string modDirectory) : base(modDirectory)
        {
            Instance = this;
        }

        public override string GUID => "gionas361.wildfrost.topstarmod"; //[creator name].[game name].[mod name] is standard convention. LOWERCASE!
        public override string[] Depends => new string[] { }; //The GUIDs of other mods that must load before yours. Usually empty
        public override string Title => "Top Star";
        public override string Description => "Just making some OC's.";

        // Mod
        public static TopStarMod Instance; //Instead of Tutorial2, you should write the name of your class instead.
        public static List<object> assets = new List<object>();    //The list of builders that will build your CardData/StatusEffectData
        private bool preLoaded = false;                            //Used to prevent redundantly reconstructing our data. Not truly necessary.

        // TODO: This allows for icons in descriptions
        public override TMP_SpriteAsset SpriteAsset => spriteAsset;
        internal static TMP_SpriteAsset spriteAsset;

        // Tribe making
        private CardDataBuilder CardCopy(string oldName, string newName) => DataCopy<CardData, CardDataBuilder>(oldName, newName);
        private ClassDataBuilder TribeCopy(string oldName, string newName) => DataCopy<ClassData, ClassDataBuilder>(oldName, newName);
        private T DataCopy<Y, T>(string oldName, string newName) where Y : DataFile where T : DataFileBuilder<Y, T>, new()
        {
            Y data = Get<Y>(oldName).InstantiateKeepName();
            data.name = GUID + "." + newName;
            T builder = data.Edit<Y, T>();
            builder.Mod = this;
            return builder;
        }
        // Gets tribes
        private T[] DataList<T>(params string[] names) where T : DataFile => names.Select((s) => TryGet<T>(s)).ToArray();



        //Function for tribes
        public void createTribes()
        {
            //Code for tribes
            //Tribe 0: Caretakers
            assets.Add(TribeCopy("Magic", "CaretakerTribe")                   //Snowdweller = "Basic", Shadmancer = "Magic"
                .WithFlag("Images/DrawFlag.png")                    //Loads your DrawFlag.png in your Images subfolder of your mod folder
                .WithSelectSfxEvent(FMODUnity.RuntimeManager.PathToEventReference("event:/sfx/card/draw_multi"))    //Shuffling sound
                                                                                                                    //The above line may need one of the FMOD references
                .SubscribeToAfterAllBuildEvent(delegate (ClassData data)
                {
                    GameObject gameObject = ObjectExt.InstantiateKeepName<GameObject>(data.characterPrefab.gameObject);
                    UnityEngine.Object.DontDestroyOnLoad(gameObject);
                    gameObject.name = "Player (TopStar.CaretakerTribe)";
                    data.characterPrefab = gameObject.GetComponent<Character>();
                    //leaders
                    data.id = "TopStar.CaretakerTribe";
                    data.leaders = this.DataList<CardData>(new string[]
                    {
                        "Starlight",
                        "Solarflare",
                        "Eclipse"
                    });
                })
            );
        }
        //Function for effects
        public void createEffects()
        {
            //Code for status effects
            //Status 0: Summon Orygen
            //Base
            assets.Add(
                StatusCopy("Summon Fallow", "Summon Orygen")                        //Makes a copy of the Summon Fallow effect
                .SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)          //Changes the summoned card to Shade Snake, but not immediately. Once Shade Snake is properly loaded, the delegate is called.
                {
                    ((StatusEffectSummon)data).summonCard = TryGet<CardData>("gionas361.wildfrost.topstarmod.Orygen"); //Alternatively, I could've put TryGet<CardData>("mhcdc9.wildfrost.tutorial.shadeSnake") or TryGet<CardData>(Extensions.PrefixGUID("shadeSnake",this)) or the Get variants too
                                                                                                                       //This is because TryGet will try to prefix the name with your GUID. 
                                                                                                                       //If that fails, then it uses no GUID-prefixing.
                })
                );
            Debug.Log("[TopStar] Summon Orygen Added.");//Debug
            //Instant
            assets.Add(
                StatusCopy("Instant Summon Fallow", "Instant Summon Orygen") //Copying Instant Summon Fallow and changing the name.
                .SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)   //Replacing the targetSummon with our StatusEffectSummon, once the time is right. 
                {
                    ((StatusEffectInstantSummon)data).targetSummon = TryGet<StatusEffectSummon>("Summon Orygen");
                })
                );
            Debug.Log("[TopStar] Instant Summon Orygen Added."); //Debug
            //Destroyed
            assets.Add(
                StatusCopy("When Destroyed Summon Dregg", "When Destroyed Summon Orygen")                        //Makes a copy of the Summon Fallow effect
                .WithText("When destroyed, summon {0}")
                .WithTextInsert("<card=gionas361.wildfrost.topstarmod.Orygen>")
                .FreeModify<StatusEffectApplyX>(
                    delegate (StatusEffectApplyX data) {
                        data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                        data.targetMustBeAlive = false;
                    }
                )
                .SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)          //Changes the summoned card to Shade Snake, but not immediately. Once Shade Snake is properly loaded, the delegate is called.
                {
                    ((StatusEffectApplyXWhenDestroyed)data).effectToApply = TryGet<StatusEffectData>("Instant Summon Orygen");
                })
                );
            Debug.Log("[TopStar] Summon Orygen when destroyed Added."); //Debug
            
            //Status 1: Trigger on Kill
            //Keyword
            assets.Add(
                new KeywordDataBuilder(this)
                .Create("onrush")
                .WithTitle("On Rush")
                .WithTitleColour(new Color?(new Color(0.5686f, 0.0078f, 0.0f)))
                .WithShowName(true)
                .WithDescription("Trigger on Kill")//,\n<+1><keyword=attack>
                .WithNoteColour(new Color?(new Color(0.4f, 0.4f, 0.4f)))
                .WithBodyColour(new Color?(new Color(1.0f, 1.0f, 1.0f)))
                .WithCanStack(false)
                );
            //Base
            assets.Add(
                StatusCopy("On Kill Apply Attack To Self", "Trigger on Kill")
                .WithText("")
                .WithStackable(true)
                .SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
                {
                    ((StatusEffectApplyXOnKill)data).effectToApply = this.TryGet<StatusEffectData>("Trigger");
                })
                );
            //Trait
            assets.Add(
                new TraitDataBuilder(this)
                .Create("OnRush")
                .SubscribeToAfterAllBuildEvent(delegate (TraitData trait)
                {
                    trait.keyword = base.Get<KeywordData>(Extensions.PrefixGUID("onrush", this).ToLower());
                    trait.effects = new StatusEffectData[]
                    {
                        base.Get<StatusEffectData>("Trigger on Kill")
                    };
                })
                );
            
            //Status 2: Combination
            //Instant
            assets.Add(
                new StatusEffectDataBuilder(this)
                .Create<StatusEffectInstantCombineCard>("Combine S&L")
                .WithText("On Trigger, fuse the Companions: 'Senta' and 'Linda'")
                .SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
                {
                    ((StatusEffectInstantCombineCard)data).cardNames = new string[] { "gionas361.wildfrost.topstarmod.Senta", "gionas361.wildfrost.topstarmod.Linda" };
                    ((StatusEffectInstantCombineCard)data).resultingCardName = "gionas361.wildfrost.topstarmod.Senta&LindaDuo";
                    ((StatusEffectInstantCombineCard)data).spawnOnBoard = true;
                    ((StatusEffectInstantCombineCard)data).checkDeck = false;
                    ((StatusEffectInstantCombineCard)data).checkBoard = true;
                    ((StatusEffectInstantCombineCard)data).checkHand = true;
                    ((StatusEffectInstantCombineCard)data).changeDeck = true;
                    ((StatusEffectInstantCombineCard)data).keepUpgrades = true;
                    ((StatusEffectInstantCombineCard)data).extraUpgrades =
                    new List<CardUpgradeData>
                    {
                        TryGet<CardUpgradeData>("CardUpgradeInk")
                    };
                })
                );
            //Played
            assets.Add(
                new StatusEffectDataBuilder(this)
                .Create<StatusEffectApplyXOnCardPlayed>("Instant Combine Senta & Liza")
                .WithText("On Trigger, fuse the Companions: 'Senta' and 'Linda'")
                .WithStackable(false)
                .WithCanBeBoosted(false)
                .SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
                {
                    var realData = data as StatusEffectApplyXOnCardPlayed;

                    realData.eventPriority = 999999999;
                    realData.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                    realData.effectToApply = TryGet<StatusEffectData>("Combine S&L");

                    realData.targetConstraints = new[]
                    {
                        new TargetConstraintIsSpecificCard()
                        {
                            allowedCards = new CardData[]
                            {
                                Get<CardData>("Linda"),
                                Get<CardData>("Senta")
                            }
                        }
                    };
                })
                );

            //Status 3: Reduce counter when allies trigger
            assets.Add(
                new StatusEffectDataBuilder(this)
                .Create<StatusEffectApplyXWhenAlliesAttack>("Reduce Counter when X on Enemy")
                .WithText("When Attack or Effect lands succesfully on an Enemy, reduce <keyword=counter> by <{a}>")
                .WithStackable(false)
                .WithCanBeBoosted(true)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    var eff = (StatusEffectApplyXWhenAlliesAttack)data;
                    eff.targetMustBeAlive = false;
                    eff.effectToApply = this.TryGet<StatusEffectData>("Reduce Counter");
                    eff.allies = true;         // <-- we care about allies attacking
                    eff.enemies = false;       // <-- ignore enemy attacks
                })
                );
            Debug.Log("[TopStar] Reduce Counter by 1 (Sui)."); //Debug

            //Status 4: Gain Mana when Trigger
            assets.Add(
                new StatusEffectDataBuilder(this)
                .Create<StatusEffectGainManaOnSelfAttack>("Gain X Mana when Trigger")
                .WithText("Gain <{a}><keyword=gionas361.wildfrost.topstarmod.mana>")
                .WithStackable(false)
                .WithCanBeBoosted(true)
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    var eff = (StatusEffectGainManaOnSelfAttack)data;
                    eff.targetMustBeAlive = false;
                    eff.effectToApply = this.TryGet<StatusEffectData>("gionas361.wildfrost.topstarmod.mana"); // must match your mana status id
                })
                );
            Debug.Log("[TopStar] Gain X Mana (Boss)."); //Debug

            //Status 5: If Boss has X mana tranform
            //Check mana
            assets.Add(
                new StatusEffectDataBuilder(this)
                .Create<StatusEffectApplyXWhenYAppliedTo>("Check Boss Mana Once")
                .WithText("Summon a <card=gionas361.wildfrost.topstarmod.BlizzardSpell>, <card=gionas361.wildfrost.topstarmod.VulcanSpell> or <card=gionas361.wildfrost.topstarmod.StormSpell> when <keyword=gionas361.wildfrost.topstarmod.mana> reaches <{a}>, transform~!")
                .WithStackable(true) // amount matters to carry your 'variant' number
                .SubscribeToAfterAllBuildEvent(d =>
                {
                    var eff = (StatusEffectApplyXWhenYAppliedTo)d;
                    eff.effectToApply       = this.TryGet<StatusEffectData>("gionas361.wildfrost.topstarmod.mana"); /* getASpell() */
                    eff.applyToFlags        = StatusEffectApplyX.ApplyToFlags.Self;
                    eff.whenAppliedToFlags  = StatusEffectApplyX.ApplyToFlags.Self;
                    eff.whenAppliedTypes    = new[] { "gionas361.wildfrost.topstarmod.mana" };
                    eff.mustReachAmount     = true;
                    eff.eventPriority       = 2;
                    eff.applyEqualAmount    = false;

                    // Script Stuff
                    ScriptableFixedAmount script = ScriptableObject.CreateInstance<ScriptableFixedAmount>();
                    script.amount = 99;
                    ((StatusEffectApplyX)d).scriptableAmount = script;
                })
                );
            Debug.Log("[TopStar] Check Mana Amount (Boss)."); //Debug
            //Transform boss
            /*
            assets.Add(
                new StatusEffectDataBuilder(this)
                .Create<StatusEffectInstantCombineCard>("CB23K combo")
                .SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
                {
                    ((StatusEffectInstantCombineCard)data).cardNames = new string[]
                    {
                        "goobers.CB23O"
                    };
                    ((StatusEffectInstantCombineCard)data).resultingCardName = "goobers.CB23K";
                    ((StatusEffectInstantCombineCard)data).spawnOnBoard = true;
                    ((StatusEffectInstantCombineCard)data).changeDeck = true;
                    ((StatusEffectInstantCombineCard)data).keepUpgrades = true;
                })
                );
            */

            //Status 6: Add spells
            // Storm Spell
            assets.Add(
                StatusCopy("Summon Fallow", "Summon Storm Spell")
                .SubscribeToAfterAllBuildEvent(d =>
                {
                    ((StatusEffectSummon)d).summonCard = TryGet<CardData>("gionas361.wildfrost.topstarmod.StormSpell"); //= TryGet<CardData>("gionas361.wildfrost.topstarmod.StormSpell");
                })
                );
            assets.Add(
                StatusCopy("Instant Summon Fallow", "Instant Summon Storm Spell") //Copying Instant Summon Fallow and changing the name.
                .SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)   //Replacing the targetSummon with our StatusEffectSummon, once the time is right. 
                {
                    ((StatusEffectInstantSummon)data).targetSummon = TryGet<StatusEffectSummon>("Summon Storm Spell");
                })
                );
            assets.Add(
                new StatusEffectDataBuilder(this)
                .Create<StatusEffectApplyXInstant>("Summon Storm Spell when Triggered")                        //Makes a copy of the Summon Fallow effect
                .WithText("Summon Storm Spell")
                .WithTextInsert("<card=gionas361.wildfrost.topstarmod.StormSpell>")
                .FreeModify<StatusEffectApplyX>(
                    delegate (StatusEffectApplyX data) {
                        data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                        data.targetMustBeAlive = true;
                    }
                )
                .SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)          //Changes the summoned card to Shade Snake, but not immediately. Once Shade Snake is properly loaded, the delegate is called.
                {
                    ((StatusEffectApplyXInstant)data).effectToApply = TryGet<StatusEffectData>("Instant Summon Storm Spell");
                })
                );
            Debug.Log("[TopStar] Add Storm Spell (Boss)."); //Debug
            // Vulcan Spell
            assets.Add(
                StatusCopy("Summon Fallow", "Summon Vulcan Spell")
                .SubscribeToAfterAllBuildEvent(d =>
                {
                    ((StatusEffectSummon)d).summonCard = TryGet<CardData>("gionas361.wildfrost.topstarmod.VulcanSpell"); //= TryGet<CardData>("gionas361.wildfrost.topstarmod.StormSpell");
                })
                );
            assets.Add(
                StatusCopy("Instant Summon Fallow", "Instant Summon Vulcan Spell") //Copying Instant Summon Fallow and changing the name.
                .SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)   //Replacing the targetSummon with our StatusEffectSummon, once the time is right. 
                {
                    ((StatusEffectInstantSummon)data).targetSummon = TryGet<StatusEffectSummon>("Summon Vulcan Spell");
                })
                );
            assets.Add(
                new StatusEffectDataBuilder(this)
                .Create<StatusEffectApplyXInstant>("Summon Vulcan Spell when Triggered")                        //Makes a copy of the Summon Fallow effect
                .WithText("Summon Vulcan Spell")
                .WithTextInsert("<card=gionas361.wildfrost.topstarmod.VulcanSpell>")
                .FreeModify<StatusEffectApplyX>(
                    delegate (StatusEffectApplyX data) {
                        data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                        data.targetMustBeAlive = true;
                    }
                )
                .SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)          //Changes the summoned card to Shade Snake, but not immediately. Once Shade Snake is properly loaded, the delegate is called.
                {
                    ((StatusEffectApplyXInstant)data).effectToApply = TryGet<StatusEffectData>("Instant Summon Vulcan Spell");
                })
                );
            Debug.Log("[TopStar] Add Vulcan Spell (Boss)."); //Debug
            // Blizzard Spell
            assets.Add(
                StatusCopy("Summon Fallow", "Summon Blizzard Spell")
                .SubscribeToAfterAllBuildEvent(d =>
                {
                    ((StatusEffectSummon)d).summonCard = TryGet<CardData>("gionas361.wildfrost.topstarmod.BlizzardSpell"); //= TryGet<CardData>("gionas361.wildfrost.topstarmod.StormSpell");
                })
                );
            assets.Add(
                StatusCopy("Instant Summon Fallow", "Instant Summon Blizzard Spell") //Copying Instant Summon Fallow and changing the name.
                .SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)   //Replacing the targetSummon with our StatusEffectSummon, once the time is right. 
                {
                    ((StatusEffectInstantSummon)data).targetSummon = TryGet<StatusEffectSummon>("Summon Blizzard Spell");
                })
                );
            assets.Add(
                new StatusEffectDataBuilder(this)
                .Create<StatusEffectApplyXInstant>("Summon Blizzard Spell when Triggered")                        //Makes a copy of the Summon Fallow effect
                .WithText("Summon Blizzard Spell")
                .WithTextInsert("<card=gionas361.wildfrost.topstarmod.BlizzardSpell>")
                .FreeModify<StatusEffectApplyX>(
                    delegate (StatusEffectApplyX data) {
                        data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                        data.targetMustBeAlive = true;
                    }
                )
                .SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)          //Changes the summoned card to Shade Snake, but not immediately. Once Shade Snake is properly loaded, the delegate is called.
                {
                    ((StatusEffectApplyXInstant)data).effectToApply = TryGet<StatusEffectData>("Instant Summon Blizzard Spell");
                })
                );
            Debug.Log("[TopStar] Add Blizzard Spell (Boss)."); //Debug
        }
        //Function for Items
        public void createItems()
        {
            //Item 0: Storm Spell
            assets.Add(
                new CardDataBuilder(this)
                .CreateUnit("StormSpell", "Storm Spell")
                .SetSprites("StormSpell.png", "StormSpellBg.png")
                .WithCardType("Summoned")
                .SetStats(1, 5, 1)
                .SubscribeToAfterAllBuildEvent(card =>
                {
                    card.traits = new List<CardData.TraitStacks>
                    {
                        base.CreateTraitStack("Aimless", 1)
                    };
                    card.startWithEffects = new CardData.StatusEffectStacks[] //Manually set Shade Serpent's effects to the desired effect... when the time is right.
                    {
                        this.SStack("MultiHit", 4),
                    };
                })
                );
            Debug.Log("[TopStar] Storm Spell (Boss)."); //Debug

            //Item 1: Vulcan Spell
            assets.Add(
                new CardDataBuilder(this)
                .CreateUnit("VulcanSpell", "Vulcan Spell")
                .SetSprites("VulcanSpell.png", "VulcanSpellBg.png")
                .WithCardType("Summoned")
                .SetStats(1, 8, 3)
                .SubscribeToAfterAllBuildEvent(card =>
                {
                    card.traits = new List<CardData.TraitStacks>
                    {
                        base.CreateTraitStack("Barrage", 1)
                    };
                })
                );
            Debug.Log("[TopStar] Vulcan Spell (Boss)."); //Debug

            //Item 2: Blizzard Spell
            assets.Add(
                new CardDataBuilder(this)
                .CreateUnit("BlizzardSpell", "Blizzard Spell")
                .SetSprites("BlizzardSpell.png", "BlizzardSpellBg.png")
                .WithCardType("Summoned")
                .SetStats(1, 1, 2)
                .SubscribeToAfterAllBuildEvent(card =>
                {
                    card.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        SStack("Hit All Enemies", 1),
                        SStack("On Turn Apply Snow To Enemies", 2)
                    };
                })
                );
            Debug.Log("[TopStar] Blizzard Spell (Boss)."); //Debug
        }
        //Function for companions
        public void createCompanions()
        {
            //Code for cards
            //Card 0: Zerk
            assets.Add(
                new CardDataBuilder(this).CreateUnit("Zerk", "Zerk") //Internally the card's name will be "[GUID].shadeSerpent". In-game, it will be "Shade Serpent".
                .SetSprites("Zerk.png", "ZerkBg.png")                //See below.
                .SetStats(8, 3, 3)                                                      //Shade Serpent will have 8 health, 1 attack, and a 3-counter.
                .WithCardType("Friendly")                                             //All companions are "Friendly". Also, this line is not necessary since CreateUnit already sets the cardType to "Friendly".
                                                                                      //.WithFlavour("When Hit Add Frenzy To Self")                      // Due to next line it wont matter anymore
                .SetStartWithEffect(SStack("When Hit Add Frenzy To Self", 1)) //The only new line
                .AddPool("GeneralUnitPool")                                             //This puts Shade Serpent in the Shademancer pools. Other choices were "GeneralUnitPool", "SnowUnitPool", "BasicUnitPool", and "ClunkUnitPool".
                );
            //Card 1: Khris
            assets.Add(
                new CardDataBuilder(this).CreateUnit("Khris", "Khris") //Internally the card's name will be "[GUID].shadeSerpent". In-game, it will be "Shade Serpent".
                .SetSprites("Khris.png", "KhrisBg.png")                //See below.
                .SetStats(7, 10, 5)                                                      //Shade Serpent will have 8 health, 1 attack, and a 3-counter.
                .WithCardType("Friendly")                                             //All companions are "Friendly". Also, this line is not necessary since CreateUnit already sets the cardType to "Friendly".
                                                                                      //.WithFlavour("When Hit Apply Attack To Self")                      // Due to next line it wont matter anymore
                .AddPool("GeneralUnitPool")                                             //This puts Shade Serpent in the Shademancer pools. Other choices were "GeneralUnitPool", "SnowUnitPool", "BasicUnitPool", and "ClunkUnitPool".
                .SubscribeToAfterAllBuildEvent(delegate (CardData data)
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        SStack("When Deployed Apply Block To Self", 2),
                        SStack("When Block Lost, Damage Enemies", 1),
                        SStack("On Kill Apply Block To Self", 2)
                    };
                })
                );
            //Card 2: Cat
            assets.Add(
                new CardDataBuilder(this).CreateUnit("Cat", "Cat") //Internally the card's name will be "[GUID].shadeSerpent". In-game, it will be "Shade Serpent".
                .SetSprites("Cat.png", "CatBg.png")                //See below.
                .SetStats(9, 1, 2)                                                      //Shade Serpent will have 8 health, 1 attack, and a 3-counter.
                .WithCardType("Friendly")                                             //All companions are "Friendly". Also, this line is not necessary since CreateUnit already sets the cardType to "Friendly".
                                                                                      //.WithFlavour("When Hit Apply Attack To Self")                      // Due to next line it wont matter anymore
                .AddPool("GeneralUnitPool")                                             //This puts Shade Serpent in the Shademancer pools. Other choices were "GeneralUnitPool", "SnowUnitPool", "BasicUnitPool", and "ClunkUnitPool".
                .SubscribeToAfterAllBuildEvent(delegate (CardData data)        //New lines (replaces flavor text)
                {

                    data.startWithEffects = new CardData.StatusEffectStacks[] //Manually set Shade Serpent's effects to the desired effect... when the time is right.
                    {
                        SStack("When Destroyed Summon Orygen", 1),          //The effect we just made.
                        SStack("When Hit Heal Self", 2)
                    };
                })
                );
            //Card 3: Orygen
            assets.Add(
                new CardDataBuilder(this).CreateUnit("Orygen", "Orygen") //Internally the card's name will be "[GUID].shadeSerpent". In-game, it will be "Shade Serpent".
                .SetSprites("Orygen.png", "OrygenBg.png")                //See below.
                .SetStats(6, 6, 6)                                                      //Shade Serpent will have 8 health, 1 attack, and a 3-counter.
                .WithCardType("Summoned")                                             //All companions are "Friendly". Also, this line is not necessary since CreateUnit already sets the cardType to "Friendly".
                .SubscribeToAfterAllBuildEvent(delegate (CardData data)        //New lines (replaces flavor text)
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[] //Manually set Shade Serpent's effects to the desired effect... when the time is right.
                    {
                        SStack("Hit All Enemies", 1)
                    };
                })
                );
            //Card 4: Stella
            assets.Add(
                new CardDataBuilder(this).CreateUnit("Stella", "Stella") //Internally the card's name will be "[GUID].shadeSerpent". In-game, it will be "Shade Serpent".
                .SetSprites("Stella.png", "StellaBg.png")                //See below.
                .SetStats(9, 1, 3)                                                      //Shade Serpent will have 8 health, 1 attack, and a 3-counter.
                .WithCardType("Friendly")                                             //All companions are "Friendly". Also, this line is not necessary since CreateUnit already sets the cardType to "Friendly".
                .AddPool("GeneralUnitPool")
                .SubscribeToAfterAllBuildEvent(delegate (CardData data)        //New lines (replaces flavor text)
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[] //Manually set Shade Serpent's effects to the desired effect... when the time is right.
                    {
                        this.SStack("MultiHit", 3)
                    };
                    data.traits = new List<CardData.TraitStacks>
                    {
                        base.CreateTraitStack("Longshot", 1),
                        base.CreateTraitStack("Pull", 1)
                    };
                })
                );
            //Card 5: Jijikan
            assets.Add(
                new CardDataBuilder(this).CreateUnit("Jijikan", "Jijikan") //Internally the card's name will be "[GUID].shadeSerpent". In-game, it will be "Shade Serpent".
                .SetSprites("Jijikan.png", "JijikanBg.png")                //See below.
                .SetStats(9, 9, 4)                                                      //Shade Serpent will have 8 health, 1 attack, and a 3-counter.
                .WithCardType("Friendly")                                             //All companions are "Friendly". Also, this line is not necessary since CreateUnit already sets the cardType to "Friendly".
                .AddPool("GeneralUnitPool")
                .SubscribeToAfterAllBuildEvent(delegate (CardData data)        //New lines (replaces flavor text)
                {
                    data.traits = new List<CardData.TraitStacks>
                    {
                        base.CreateTraitStack("OnRush", 1)
                    };
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        this.SStack("On Kill Apply Attack To Self", 1)
                    };
                })
                );
            //Card 6: Senta & Linda
            assets.Add(
                new CardDataBuilder(this)
                .CreateUnit("Senta&LindaDuo", "Senta & Linda")
                .SetSprites("Senta&LindaDuo.png", "Senta&LindaDuoBg.png")
                .SetStats(15, 5, 3)
                .WithCardType("Friendly")
                .SubscribeToAfterAllBuildEvent(delegate (CardData card)
                {
                    card.traits = new List<CardData.TraitStacks>
                    {
                        base.CreateTraitStack("Barrage", 1)
                    };
                    card.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        new CardData.StatusEffectStacks(base.Get<StatusEffectData>("When Hit Apply Void To Attacker"), 2), //1 when linda only
                        new CardData.StatusEffectStacks(base.Get<StatusEffectData>("While Active Increase Effects To FrontAlly"), 3) //3 when senta only
                    };
                })
                );
            //Card 7: Senta
            assets.Add(
                new CardDataBuilder(this).CreateUnit("Senta", "Senta") //Internally the card's name will be "[GUID].shadeSerpent". In-game, it will be "Shade Serpent".
                .SetSprites("Senta.png", "SentaBg.png")                //See below.
                .SetStats(9, 1, 3)                                                      //Shade Serpent will have 8 health, 1 attack, and a 3-counter.
                .WithCardType("Friendly")                                             //All companions are "Friendly". Also, this line is not necessary since CreateUnit already sets the cardType to "Friendly".
                .AddPool("GeneralUnitPool")
                .SubscribeToAfterAllBuildEvent(delegate (CardData data)        //New lines (replaces flavor text)
                {
                    data.traits = new List<CardData.TraitStacks>
                    {
                        base.CreateTraitStack("Barrage", 1)
                    };
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        this.SStack("While Active Increase Effects To FrontAlly", 1),
                        this.SStack("Instant Combine Senta & Liza", 1)
                    };
                })
                );
            //Card 8: Linda
            assets.Add(
                new CardDataBuilder(this).CreateUnit("Linda", "Linda") //Internally the card's name will be "[GUID].shadeSerpent". In-game, it will be "Shade Serpent".
                .SetSprites("Linda.png", "LindaBg.png")                //See below.
                .SetStats(8, 3, 3)                                                      //Shade Serpent will have 8 health, 1 attack, and a 3-counter.
                .WithCardType("Friendly")                                             //All companions are "Friendly". Also, this line is not necessary since CreateUnit already sets the cardType to "Friendly".
                .AddPool("GeneralUnitPool")
                .SubscribeToAfterAllBuildEvent(delegate (CardData data)        //New lines (replaces flavor text)
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        SStack("When Hit Apply Void To Attacker", 1),
                        SStack("Instant Combine Senta & Liza", 1)
                    };
                })
                );
            //Card 8: Sui
            assets.Add(
                new CardDataBuilder(this).CreateUnit("Sui", "Sui")
                .SetSprites("Sui.png", "SuiBg.png")
                .SetStats(5, 50, 20)
                .WithCardType("Friendly")
                .SubscribeToAfterAllBuildEvent(delegate (CardData data)
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        SStack("Hit All Enemies", 1),
                        SStack("Reduce Counter when X on Enemy", 1)
                    };
                })
                .AddPool("GeneralUnitPool")
                );
            //Card 9: Kaido
            assets.Add(
                new CardDataBuilder(this).CreateUnit("Kaido", "Kaido")
                .SetSprites("Kaido.png", "KaidoBg.png")
                .SetStats(6, 6, 6)
                .WithCardType("Friendly")
                .SubscribeToAfterAllBuildEvent(delegate (CardData data)
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        SStack("On Turn Apply Spice To AllyBehind", 6),
                        SStack("On Turn Apply Spice To AllyInFrontOf", 6),
                        SStack("On Turn Apply Spice To Self", 6)
                    };
                    
                })
                .AddPool("GeneralUnitPool")
                );
            //Card 10: Boss
            assets.Add(
                new CardDataBuilder(this).CreateUnit("Boss", "Boss")
                .SetSprites("Boss.png", "BossBg.png")
                .SetStats(8, 0, 3)
                .WithCardType("Friendly")
                .SubscribeToAfterAllBuildEvent(delegate (CardData data)
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        SStack("Gain X Mana when Trigger", 3),
                        SStack("Check Boss Mana Once", 1)
                    };
                })
                .AddPool("GeneralUnitPool")
                );
        }
        //Function for leaders
        public void createLeaders()
        {
            //Code for leaders
            //Leader 0: Starlight
            assets.Add(
                new CardDataBuilder(this).CreateUnit("Starlight", "Starlight", "TargetModeBasic", "Blood Profile Normal", "SwayAnimationProfile") //Internally the card's name will be "[GUID].shadeSerpent". In-game, it will be "Shade Serpent".
                .SetSprites("Starlight.png", "StarlightBg.png")                //See below.
                .SetStats(new int?(7), new int?(4), 2)
                .WithCardType("Leader")
                .SubscribeToAfterAllBuildEvent(                               //Depending on what you are making, FreeModify might be replaced with SubscribeToAfterAllBuildEvent
                (data) =>                                  //data is assumed to be CardData
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        this.SStack("On Turn Apply Attack To Self", 1)
                    };
                    data.createScripts = new CardScript[]  //These scripts run when right before Events.OnCardDataCreated
                    {
                        GiveUpgrade("Crown"),                     //By our definition, no argument will give a crown
                        AddRandomHealth(-2,2),
                        AddRandomDamage(-1,1),
                        AddRandomCounter(-1,2)
                    };
                })
                );
            //Leader 1: Solarflare
            assets.Add(
                new CardDataBuilder(this).CreateUnit("Solarflare", "Solarflare", "TargetModeBasic", "Blood Profile Normal", "SwayAnimationProfile") //Internally the card's name will be "[GUID].shadeSerpent". In-game, it will be "Shade Serpent".
                .SetSprites("Solarflare.png", "SolarflareBg.png")                //See below.
                .SetStats(new int?(5), new int?(4), 4)
                .WithCardType("Leader")
                .SubscribeToAfterAllBuildEvent(                               //Depending on what you are making, FreeModify might be replaced with SubscribeToAfterAllBuildEvent
                (data) =>                                  //data is assumed to be CardData
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        this.SStack("Trigger When Ally Attacks", 2)
                    };
                    data.createScripts = new CardScript[]  //These scripts run when right before Events.OnCardDataCreated
                    {
                        GiveUpgrade("Crown"),                     //By our definition, no argument will give a crown
                        AddRandomHealth(-2,4),
                        AddRandomDamage(-3,2),
                        AddRandomCounter(-1,1)
                    };
                })
                );
            //Leader 2: Eclipse
            assets.Add(
                new CardDataBuilder(this).CreateUnit("Eclipse", "Eclipse", "TargetModeBasic", "Blood Profile Normal", "SwayAnimationProfile") //Internally the card's name will be "[GUID].shadeSerpent". In-game, it will be "Shade Serpent".
                .SetSprites("Eclipse.png", "EclipseBg.png")                //See below.
                .SetStats(new int?(10), new int?(10), 6)
                .WithCardType("Leader")
                .WithFlavour("Innately capable of equipping 6 charms.")
                .SubscribeToAfterAllBuildEvent(                               //Depending on what you are making, FreeModify might be replaced with SubscribeToAfterAllBuildEvent
                (data) =>                                  //data is assumed to be CardData
                {
                    data.charmSlots = 6;
                    data.startWithEffects = new CardData.StatusEffectStacks[]
                    {
                        this.SStack("Demonize", 1),
                        this.SStack("MultiHit", 1)

                    };
                    data.createScripts = new CardScript[]  //These scripts run when right before Events.OnCardDataCreated
                    {
                        GiveUpgrade("Crown"),                     //By our definition, no argument will give a crown
                        AddRandomHealth(-2,6),
                        AddRandomDamage(-3,2),
                        AddRandomCounter(-1,1)
                    };
                })
                );
        }
        //Function for charms
        public void createCharms()
        {
            //Code for Charm
            //Charm 0: giganttoken
            assets.Add(
                new CardUpgradeDataBuilder(this)
                .CreateCharm("CardUpgradeTokenOfGigants")                    //Internally named as CardUpgradeGlacial, sets its type to charm, and adds it to the general pool
                .WithType(CardUpgradeData.Type.Charm)                 //Not needed since we used CreateCharm (why did I put this here :/). If we do not want the charm in the general pool, you would have to use this method to make the upgrade a charm.
                .WithImage("TokenOfGigants.png")                        //Sets the image file path to "GlacialCharm.png". See below.
                .WithTitle("Token of Gigants")                           //Sets in-game name as Glacial Charm
                .WithText($"<-2><keyword=health>\n<+3><keyword=attack>") //Get allows me to skip the GUID. The Text class does not.
                                                                         //IMPORTANT: if you did not heed the advice from before, the keyword name must be lowercase, so use .ToLower() to fix that.
                                                                         //If you are having trouble, find your keyword via the Unity Explorer and verify its name. 
                .ChangeHP(-2)                                          //Affects cost in shops
                .ChangeDamage(3)
                );
            //Charm 1: flooroflanguage
            assets.Add(
                new CardUpgradeDataBuilder(this)
                .CreateCharm("CardUpgradeRedMist")                    //Internally named as CardUpgradeGlacial, sets its type to charm, and adds it to the general pool
                .WithType(CardUpgradeData.Type.Charm)                 //Not needed since we used CreateCharm (why did I put this here :/). If we do not want the charm in the general pool, you would have to use this method to make the upgrade a charm.
                .WithImage("FloorOfLanguage.png")                        //Sets the image file path to "GlacialCharm.png". See below.
                .WithTitle("IS THAT THE RED MIST?!?!")                           //Sets in-game name as Glacial Charm
                .WithText($"Gain <keyword={Extensions.PrefixGUID("onrush", this).ToLower()}>") //Get allows me to skip the GUID. The Text class does not.
                                                                                               //IMPORTANT: if you did not heed the advice from before, the keyword name must be lowercase, so use .ToLower() to fix that.
                                                                                               //If you are having trouble, find your keyword via the Unity Explorer and verify its name. 
                .SubscribeToAfterAllBuildEvent(delegate (CardUpgradeData data)
                {
                    data.giveTraits = new CardData.TraitStacks[] { new CardData.TraitStacks(Get<TraitData>("OnRush"), 1) };
                })
                );
        }



        // Initialize Assets
        private void CreateModAssets()
        {
            CreateManaIcon();
            createTribes();
            createEffects();
            createItems();
            createCompanions();
            createLeaders();
            createCharms();

            preLoaded = true;
        }



        // Tribe information
        public string TribeTitleKey => GUID + ".TribeTitle";
        public string TribeDescKey => GUID + ".TribeDesc";

        //Call this method in Load()
        private void CreateLocalizedStrings()
        {
            StringTable uiText = LocalizationHelper.GetCollection("UI Text", SystemLanguage.English);
            uiText.SetString(TribeTitleKey, "Caretakers");                                       //Create the title
            uiText.SetString(TribeDescKey, "A specie that came from the beyond the sky.\n" +
                "These beings, who manipulate the origin of energy;\n" +
                "With the help of residents from outside Stars. Now set as to " +
                "discover why they are not capable of leaving this Star.\n\n" +
                "This tribe is a bountyful bunch, " +
                "capable of whipping out out multiple enemies in quick succession.");                                  //Create the description.
        }



        // Leader methods
        internal CardScript GiveUpgrade(string name = "Crown") //Give a crown
        {
            CardScriptGiveUpgrade script = ScriptableObject.CreateInstance<CardScriptGiveUpgrade>(); //This is the standard way of creating a ScriptableObject
            script.name = $"Give {name}";                               //Name only appears in the Unity Inspector. It has no other relevance beyond that.
            script.upgradeData = TryGet<CardUpgradeData>(name);
            return script;
        }
        internal CardScript AddRandomHealth(int min, int max) //Boost health by a random amount
        {
            CardScriptAddRandomHealth health = ScriptableObject.CreateInstance<CardScriptAddRandomHealth>();
            health.name = "Random Health";
            health.healthRange = new Vector2Int(min, max);
            return health;
        }
        internal CardScript AddRandomDamage(int min, int max) //Boost damage by a ranom amount
        {
            CardScriptAddRandomDamage damage = ScriptableObject.CreateInstance<CardScriptAddRandomDamage>();
            damage.name = "Give Damage";
            damage.damageRange = new Vector2Int(min, max);
            return damage;
        }
        internal CardScript AddRandomCounter(int min, int max) //Increase counter by a random amount
        {
            CardScriptAddRandomCounter counter = ScriptableObject.CreateInstance<CardScriptAddRandomCounter>();
            counter.name = "Give Counter";
            counter.counterRange = new Vector2Int(min, max);
            return counter;
        }



        // Basic Status effect getter
        public T TryGet<T>(string name) where T : DataFile
        {
            T data;
            if (typeof(StatusEffectData).IsAssignableFrom(typeof(T)))
                data = base.Get<StatusEffectData>(name) as T;
            else if (typeof(KeywordData).IsAssignableFrom(typeof(T)))
                data = base.Get<KeywordData>(name.ToLower()) as T;
            else
                data = base.Get<T>(name);

            if (data == null)
                throw new Exception($"TryGet Error: Could not find a [{typeof(T).Name}] with the name [{name}] or [{Extensions.PrefixGUID(name, this)}]");

            return data;
        }
        public CardData.StatusEffectStacks SStack(string name, int amount) => new CardData.StatusEffectStacks(TryGet<StatusEffectData>(name), amount);
        private CardData.TraitStacks TStack(string name, int amount) => new CardData.TraitStacks(TryGet<TraitData>(name), amount);
        //See above
        //Note: you need to add the reference DeadExtensions.dll in order to use InstantiateKeepName(). 
        public StatusEffectDataBuilder StatusCopy(string oldName, string newName)
        {
            StatusEffectData data = TryGet<StatusEffectData>(oldName).InstantiateKeepName();
            data.name = GUID + "." + newName;
            data.targetConstraints = new TargetConstraint[0];
            StatusEffectDataBuilder builder = data.Edit<StatusEffectData, StatusEffectDataBuilder>();
            builder.Mod = this;
            return builder;
        }



        public void CreateManaIcon()
        {
            // everything Mana-related can go in here now
            assets.Add(
                new KeywordDataBuilder(this)
                .Create("mana")    // This cannot have spaces, all lowercase !!
                .WithTitle("Mana") // The name that shows up when hovering the icon
                .WithDescription("""
                        Source of Everything.
                        |The Energy that powers everything, from living to non-living. Do not waste it.
                        """)
                );

            assets.Add(
                new StatusEffectDataBuilder(this)
                .Create<StatusEffectMana>("mana") // Can be any StatusEffect class
                                                          // other code to make the effect
                .Subscribe_WithStatusIcon("mana") // TODO: Put whatever you want to name the icon builder
                );

            assets.Add(
                new StatusIconBuilder(this)
                .Create(name: "mana",     // Used in StatusEffectDataBuilder.Subscribe_WithStatusIcon()
                    statusType: "topstar.mana",   // Use the [creator name].[icon name]
                    ImagePath("Icons/topstar.mana.png"))  // I put the image I want to use in [my mod directory]/Images/Icons
                                                          // Ideally the filename == status type, but VFX mod will try to adjust it
                .WithIconGroupName(StatusIconBuilder.IconGroups.damage) // To show up under counter icons
                .WithApplyVFX(ImagePath("mana_vfx.gif"))

                // Icons without text can skip these two altogether
                .WithTextColour(new Color(1f, 1f, 1f))     
                .WithTextShadow(new Color(16f/255f, 65f/255f, 91f/255f))

                .WithTextboxSprite()                                    // This version reuses the main sprite for the textbox
                                                                        //.WithTextboxSprite(ImagePath("Icons/amber.png"))      // This version is slightly slower, but lets you use other (lower-res) textbox sprites

                .WithKeywords(iconKeywordOrNull: "mana") // the "icon keyword" will be adjusted to show the icon's textbox sprite
                );            
        }
        // A simple stack status that does NOT tick down each turn

        internal class StatusEffectMana : StatusEffectData
        {
            // If your API exposes any of these, keep them “off”.
            public override bool HasTurnEndRoutine => false;      // <- property in many builds

            // (Optional) keep other gates off too so it’s inert unless you modify it explicitly
            public override bool RunPostAttackEvent(Hit _) => false;
            public override bool RunPostHitEvent(Hit _) => false;
        }


        // Random Spell picker (helper method)
        public String getASpell()
        {
            System.Random rand = new System.Random();
            int num = rand.Next(1, 3);
            Debug.Log("[TopStar] Rolled Number: " + num);

            switch (num) {
                case 1:
                    Debug.Log("[TopStar] Returned: " + "Summon Storm Spell when Triggered");
                    return "Summon Storm Spell when Triggered";
                case 2:
                    Debug.Log("[TopStar] Returned: " + "Summon Vulcan Spell when Triggered");
                    return "Summon Vulcan Spell when Triggered";
                case 3:
                    Debug.Log("[TopStar] Returned: " + "Summon Blizzard Spell when Triggered");
                    return "Summon Blizzard Spell when Triggered";
            }

            Debug.Log("[TopStar] Defaulted to: " + "Summon Storm Spell when Triggered");
            return "Summon Storm Spell when Triggered";
        }



        // Lil Gazi
        private void LilGazy(CardData cardData) //cardData is the CardData that was created/duplicated
        {
            Debug.Log("[Tutorial1] New CardData Created: " + cardData.name); //If the method is unrecognized, try UnityEngine.Debug.Log instead.
            if (cardData.name == "BoostPet")     //Booshu's internal name is BerryPet 
            {
                cardData.forceTitle = "Lil' Nazi";
                Debug.Log("[Tutorial1] Lil NAZY!");
                //Alternatively, WriteLine("Booshu!"); works too. See below.
            }
        }



        //Loading and Unloading
        public override void Load()
        {
            Instance = this;

            //preLoaded makes sure that the builders are not made again on the 2nd load.
            if (!preLoaded)
            {
                // TODO: the spriteAsset has to be defined before any icons are made!
                spriteAsset = HopeUtils.CreateSpriteAsset(Title);

                CreateModAssets(); // <- where icons are made
                preLoaded = true;
            }
            // TODO: Let our sprites automatically show up for icon descriptions
            SpriteAsset.RegisterSpriteAsset();
            base.Load();                       //Actual loading


            Events.OnCardDataCreated += LilGazy;


            CreateLocalizedStrings();
            Events.OnEntityCreated += FixImage;
            GameMode gameMode = TryGet<GameMode>("GameModeNormal"); //GameModeNormal is the standard game mode. 
            gameMode.classes = gameMode.classes.Append(TryGet<ClassData>("CaretakerTribe")).ToArray();

            // Get the Status' Id
            Debug.Log("[TopStar] Mana id is: " + this.TryGet<StatusEffectData>("gionas361.wildfrost.topstarmod.mana")?.name);
        }
        public override void Unload()
        {
            // TODO: Prevent our icons from accidentally showing up in descriptions when not loaded
            SpriteAsset.UnRegisterSpriteAsset();
            base.Unload();

            
            Events.OnCardDataCreated -= LilGazy;


            Events.OnEntityCreated -= FixImage;
            GameMode gameMode = TryGet<GameMode>("GameModeNormal");
            gameMode.classes = RemoveNulls(gameMode.classes); //Without this, a non-restarted game would crash on tribe selection
            UnloadFromClasses();                               //This tutorial doesn't need it, but it doesn't hurt to clean the pools

        }
        
        
        
        //Call this method in Unload.
        public void UnloadFromClasses()
        {
            List<ClassData> tribes = AddressableLoader.GetGroup<ClassData>("ClassData");
            foreach (ClassData tribe in tribes)
            {
                if (tribe == null || tribe.rewardPools == null) { continue; } //This isn't even a tribe; skip it.

                foreach (RewardPool pool in tribe.rewardPools)
                {
                    if (pool == null) { continue; }; //This isn't even a reward pool; skip it.

                    pool.list.RemoveAllWhere((item) => item == null || item.ModAdded == this); //Find and remove everything that needs to be removed.
                }
            }


        }
        // Removes nulls
        internal T[] RemoveNulls<T>(T[] data) where T : DataFile
        {
            List<T> list = data.ToList();
            list.RemoveAll(x => x == null || x.ModAdded == this);
            return list.ToArray();
        }
        //Remember to hook this method onto Events.OnEntityCreated in the Load/Unload (see Tutorial 1 or the full code for more details).
        private void FixImage(Entity entity)
        {
            if (entity.display is Card card && !card.hasScriptableImage) //These cards should use the static image
            {
                card.mainImage.gameObject.SetActive(true);               //And this line turns them on
            }
        }



        //Credits to Hopeful for this AddAssets code.
        public override List<T> AddAssets<T, Y>()
        {
            if (assets.OfType<T>().Any()) Debug.LogWarning($"[{Title}] adding {typeof(Y).Name}s: {assets.OfType<T>()/*.Select(a => a._data.name).Join()*/}");
            return assets.OfType<T>().ToList();
        }
    }

    // Runs once right after THIS status is applied.
    // Uses this status's stack amount as your selector (1/2/3, etc.).
    internal class StatusEffectCheckBossManaOnce : StatusEffectData
    {
        public string bossInternalName = "gionas361.wildfrost.topstarmod.Boss"; // your Boss card internal name
        public string manaStatusId = "gionas361.wildfrost.topstarmod.mana"; // your mana status id
        public int requiredMana = 12;
        public override bool HasPostApplyStatusRoutine => true;

        // Only run when THIS status has just been applied to its holder
        public override bool RunPostApplyStatusEvent(StatusEffectApply apply)
        {
            Debug.Log("[TopStar] Target: " + apply.target);
            return apply != null && apply.target == target && apply.effectData == this;
        }

        public override IEnumerator PostApplyStatusRoutine(StatusEffectApply apply)
        {
            if (target == null || !target.enabled || !Battle.IsOnBoard(target))
                yield break;

            // Find allied Boss on board
            var boss = FindDeployedBossForOwner(target.owner, bossInternalName);
            if (boss == null)
            {
                Debug.Log("[TopStar] Boss check: Boss not deployed.");
                yield break;
            }

            // Read Mana on Boss
            int mana = GetStacksFromEntity(boss, manaStatusId);
            int needed = requiredMana;
            int variant = GetAmount(); // which item variant (1/2/3) based on this status stacks

            if (mana >= needed)
            {
                Debug.Log($"[TopStar] Boss check OK (mana={mana} ≥ {needed}). variant={variant}");
                // TODO: your merge logic here (use 'variant' and 'boss' as needed)
            }
            else
            {
                Debug.Log($"[TopStar] Boss check FAIL (mana={mana} < {needed}). variant={variant}");
            }
            yield break;
        }

        // ---- helpers ----

        private Entity FindDeployedBossForOwner(UnityEngine.Object ownerObj, string internalName)
        {
            var all = Resources.FindObjectsOfTypeAll<Entity>();
            for (int i = 0; i < all.Length; i++)
            {
                var e = all[i];
                if (!e) continue;
                if (e.owner != ownerObj) continue;
                if (!Battle.IsOnBoard(e)) continue;

                var cd = e.data as CardData;
                if (cd != null && cd.name == internalName)
                    return e;
            }
            return null;
        }

        // robust reflection to get stacks from e.statusEffects list (works across builds)
        private int GetStacksFromEntity(Entity e, string statusId)
        {
            if (e == null) return 0;

            var list = e.statusEffects as System.Collections.IEnumerable;
            if (list == null) return 0;

            foreach (var se in list)
            {
                if (se == null) continue;
                var t = se.GetType();

                object data = t.GetProperty("data", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(se)
                           ?? t.GetField("data", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(se);

                string id =
                    (data != null
                        ? (string)(
                            data.GetType().GetProperty("type", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(data) ??
                            data.GetType().GetField("type", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(data) ??
                            data.GetType().GetProperty("name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(data) ??
                            data.GetType().GetField("name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(data))
                        : (string)(
                            t.GetProperty("type", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(se) ??
                            t.GetField("type", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(se) ??
                            t.GetProperty("name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(se) ??
                            t.GetField("name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(se)));

                if (id != statusId) continue;

                var count =
                      ReadIntMember(se, "count")
                   ?? ReadIntMember(se, "stacks")
                   ?? ReadIntMember(se, "amount")
                   ?? 0;

                return count;
            }
            return 0;
        }

        private int? ReadIntMember(object obj, string name)
        {
            var t = obj.GetType();
            var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null && p.CanRead)
            {
                var v = p.GetValue(obj);
                if (v is int i) return i;
            }
            var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null)
            {
                var v = f.GetValue(obj);
                if (v is int i) return i;
            }
            return null;
        }
    }

    // Concrete effect: gain X of some other status (mana) when this unit triggers
    internal class StatusEffectGainManaOnSelfAttack : StatusEffectApplyX
    {
        public override void Init()
        {
            base.Init();
            // Subscribe to the attack event, not the status-apply event
            base.PostAttack += ApplyOnPostAttack;
        }

        public override bool RunPostAttackEvent(Hit hit)
        {
            if (target == null || !target.enabled) return false;
            if (!Battle.IsOnBoard(target)) return false;

            // If there's a Hit, require this unit to be the attacker.
            // If Hit is null, still treat it as "this unit attacked".
            return hit == null || hit.attacker == target;
        }

        private IEnumerator ApplyOnPostAttack(Hit hit)
        {
            // Apply the configured effect (Reduce Counter) to THIS effect’s holder
            // NOTE: StatusEffectApplyX.Run expects a list of targets and an amount.
            var amount = GetAmount(); // comes from .WithAmount(1) on the builder
            if (amount <= 0 || effectToApply == null)
                yield break;

            // Apply to the holder card only
            Debug.Log($"[TopStar] Applying {amount}x {effectToApply.name} to {target.name}");
            yield return Run(new List<Entity> { target }, amount);
            // If you don’t have ListPool, just do:
            // yield return Run(new List<Entity> { target }, amount);
        }
    }

    internal class StatusEffectApplyXWhenAlliesAttack : StatusEffectApplyX
    {
        // Configuration flags
        public bool allies = true;                // default to allies
        public bool enemies = false;              // ignore enemies

        // We’ll only apply to the holder, so we don’t need to keep a field list around.

        public override void Init()
        {
            base.Init();
            // Subscribe to the attack event, not the status-apply event
            base.PostAttack += ApplyOnPostAttack;
        }

        public override bool RunPostAttackEvent(Hit hit)
        {
            // Basic sanity checks
            if (!target || !target.enabled) return false;
            if (!Battle.IsOnBoard(target)) return false;

            // Filter by side
            if (hit?.attacker == null) return false;

            // If we only care about allies attacking:
            if (allies && hit.attacker.owner != target.owner) return false;

            // If we only care about enemies attacking:
            if (!allies && enemies && hit.attacker.owner == target.owner) return false;

            // Passed filters → trigger
            return true;
        }

        private IEnumerator ApplyOnPostAttack(Hit hit)
        {
            // Apply the configured effect (Reduce Counter) to THIS effect’s holder
            // NOTE: StatusEffectApplyX.Run expects a list of targets and an amount.
            var amount = 1; // comes from .WithAmount(1) on the builder
            if (amount <= 0 || effectToApply == null)
                yield break;

            // Apply to the holder card only
            yield return Run(new List<Entity> { target }, amount);
            // If you don’t have ListPool, just do:
            // yield return Run(new List<Entity> { target }, amount);
        }
    }
}