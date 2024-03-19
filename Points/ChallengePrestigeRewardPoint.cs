using Life.Network;
using Life.UI;
using SQLite;
using System.Threading.Tasks;
using ModKit.Helper;
using ModKit.Helper.PointHelper;
using mk = ModKit.Helper.TextFormattingHelper;
using System.Collections.Generic;
using System.Linq;
using ChallengePrestige.Entities;
using ModKit.Utils;
using System;
using Life;
using Life.DB;

namespace ChallengePrestige
{
    public class ChallengePrestigeRewardPoint : ModKit.ORM.ModEntity<ChallengePrestigeRewardPoint>, PatternData
    {
        [AutoIncrement][PrimaryKey] public int Id { get; set; }
        public string TypeName { get; set; }
        public string PatternName { get; set; }

        [Ignore] public ModKit.ModKit Context { get; set; }

        public ChallengePrestigeRewardPoint() { }
        public ChallengePrestigeRewardPoint(bool isCreated)
        {
            TypeName = nameof(ChallengePrestigeRewardPoint);
        }

        /// <summary>
        /// Applies the properties retrieved from the database during the generation of a point in the game using this model.
        /// </summary>
        /// <param name="patternId">The identifier of the pattern in the database.</param>
        public async Task SetProperties(int patternId)
        {
            var result = await Query(patternId);

            Id = patternId;
            TypeName = nameof(ChallengePrestigeRewardPoint);
            PatternName = result.PatternName;

            //Add your other properties here
        }

        /// <summary>
        /// Contains the action to perform when a player interacts with the point.
        /// </summary>
        /// <param name="player">The player interacting with the point.</param>
        public void OnPlayerTrigger(Player player)
        {
            PrestigePanel(player);
        }

        /// <summary>
        /// Triggers the function to begin creating a new model.
        /// </summary>
        /// <param name="player">The player initiating the creation of the new model.</param>
        public void SetPatternData(Player player)
        {
            //Set the function to be called when a player clicks on the “create new model” button
            SetName(player);
        }

        /// <summary>
        /// Displays all properties of the pattern specified as parameter.
        /// The user can select one of the properties to make modifications.
        /// </summary>
        /// <param name="player">The player requesting to edit the pattern.</param>
        /// <param name="patternId">The ID of the pattern to be edited.</param>
        public async void EditPattern(Player player, int patternId)
        {
            ChallengePrestigeRewardPoint pattern = new ChallengePrestigeRewardPoint(false);
            pattern.Context = Context;
            await pattern.SetProperties(patternId);

            Panel panel = Context.PanelHelper.Create($"Modifier un {pattern.TypeName}", UIPanel.PanelType.Tab, player, () => EditPattern(player, patternId));


            panel.AddTabLine($"{mk.Color("Nom:", mk.Colors.Info)} {pattern.PatternName}", _ => {
                pattern.SetName(player, true);
            });
            //Add tablines for your other properties here

            panel.NextButton("Sélectionner", () => panel.SelectTab());
            panel.PreviousButton();
            panel.CloseButton();

            panel.Display();
        }

        #region CUSTOM
        public async void PrestigePanel(Player player)
        {
            var playerQuery = await ChallengePrestigePlayer.Query(cpp => cpp.PlayerId == player.account.id);

            Panel panel = Context.PanelHelper.Create($"Citoyens Prestigieux", UIPanel.PanelType.Text, player, () => PrestigePanel(player));


            if (playerQuery.Count > 0)
            {
                panel.TextLines.Add($"Bonjour {player.GetFullName()}");
                panel.TextLines.Add($"{mk.Color("Niveau de prestige", mk.Colors.Info)}: {playerQuery[0].Prestige}");
                panel.TextLines.Add($"");
                panel.TextLines.Add($"{mk.Align("Augmentez votre prestige", mk.Aligns.Left)}");
                panel.TextLines.Add($"{mk.Align("et débloquer diverses récompenses", mk.Aligns.Left)}");
                panel.TextLines.Add($"{mk.Align("en soutenant la commune !", mk.Aligns.Left)}");

                panel.NextButton("Récompenses", () =>RewardPanel(player));
                panel.NextButton("Classement", () =>LadderPanel(player));
            }
            else
            {
                panel.TextLines.Add($"Bienvenue {player.GetFullName()}");
                panel.TextLines.Add("Vous ne figurez pas dans notre registre.");
                panel.TextLines.Add("Devenez citoyen d'Amboise en prenant rendez-vous dès maintenant !");

                panel.AddButton("Prendre RDV", async ui => {
                    ChallengePrestigePlayer challengePrestigePlayer = new ChallengePrestigePlayer();
                    challengePrestigePlayer.PlayerId = player.account.id;
                    challengePrestigePlayer.CharacterFullName = player.GetFullName();
                    if (await challengePrestigePlayer.Save()) panel.Refresh();
                    else player.Notify("Oops !", "Nous ne parvenons pas à enregistrer votre rendez-vous.", Life.NotificationManager.Type.Error);
                });
            }

            panel.CloseButton();

            panel.Display();
        }

        public async void RewardPanel(Player player)
        {
            var rewardQuery = await ChallengePrestigeReward.QueryAll();
            var playerQuery = await ChallengePrestigePlayer.Query(cpp => cpp.PlayerId == player.account.id);

            Panel panel = Context.PanelHelper.Create($"Récompenses", UIPanel.PanelType.TabPrice, player, () => RewardPanel(player));


            if (playerQuery.Count > 0)
            {
                if (rewardQuery.Count > 0)
                {
                    foreach (var reward in rewardQuery)
                    {
                        int icon = reward.Money > 0 ? ItemUtils.getIconIdByItemId(1322) : reward.ItemId == 151 ? ItemUtils.getIconIdByItemId(1744) : ItemUtils.getIconIdByItemId(reward.ItemId);
                        string name = reward.Money > 0 ? $"Chèque cadeau {reward.Money}€" : $"{reward.ItemQuantity} {ItemUtils.GetItemById(reward.ItemId).itemName}";
                        var list = ListConverter.ReadJson(playerQuery[0].RewardsRecovered);

                        panel.AddTabLine(name, $"{(list.Contains(reward.Id) ? "CLAIM" : $"requis: {reward.PrestigeRequired} de prestige")}", icon, async ui => {
                            if (!list.Contains(reward.Id))
                            {
                                if (reward.Money == 0)
                                {
                                    var item = ItemUtils.GetItemById(reward.ItemId);
                                    if (InventoryUtils.AddItem(player, reward.ItemId, reward.ItemQuantity)) player.Notify("Erreur", "Vous n'avez pas suffisament de place dans votre inventaire.", NotificationManager.Type.Error);
                                    else player.Notify("Succès", $"Vous venez d'acquérir:\n{reward.ItemQuantity} {item.itemName}", NotificationManager.Type.Success);
                                }
                                else
                                {
                                    player.AddMoney(reward.Money, "prestige");
                                    player.Notify("Succès", $"Vous venez d'acquérir {reward.Money}€ !", NotificationManager.Type.Success);
                                }
                                list.Add(reward.Id);
                                playerQuery[0].RewardsRecovered = ListConverter.WriteJson(list);
                                await playerQuery[0].Save();
                                panel.Refresh();
                            }
                            else player.Notify("Refus", $"Vous avez déjà récupéré cette récompense", NotificationManager.Type.Error);
                        });
                    }

                    panel.AddButton("Récupérer", async => panel.SelectTab());
                }
                else player.Notify("Oops !", "Nous n'avons pas pu récupérer la liste des récompenses.", Life.NotificationManager.Type.Error);
            }
            else player.Notify("Oops !", "Nous avons rencontré un soucis lors de votre récupération dans le registre.", Life.NotificationManager.Type.Error);

            panel.PreviousButton();
            panel.CloseButton();

            panel.Display();
        }

        public async void LadderPanel(Player player)
        {
            var playerQuery = await ChallengePrestigePlayer.QueryAll();

            Panel panel = Context.PanelHelper.Create($"Classement", UIPanel.PanelType.TabPrice, player, () => LadderPanel(player));


            if (playerQuery.Count > 0)
            {
                var orderedPlayerQuery = playerQuery.OrderByDescending(instance => instance.Prestige).ToList();

                for (int i = 0; i < orderedPlayerQuery.Count; i++)
                {
                    panel.AddTabLine($"{mk.Color($"Prestige {orderedPlayerQuery[i].Prestige}", mk.Colors.Warning)} {mk.Pos(orderedPlayerQuery[i].CharacterFullName, 40)}", _ => { });
                }
            }
            else player.Notify("Oops !", "Nous avons rencontré un soucis lors de votre récupération dans le registre.", Life.NotificationManager.Type.Error);

            panel.PreviousButton();
            panel.CloseButton();

            panel.Display();
        }


        #endregion

        #region SETTERS
        /// <summary>
        /// Allows the player to set a name for the pattern, either during creation or modification.
        /// </summary>
        /// <param name="player">The player interacting with the panel.</param>
        /// <param name="inEdition">A flag indicating if the pattern is being edited.</param>
        public void SetName(Player player, bool isEditing = false)
        {
            Panel panel = Context.PanelHelper.Create($"{(!isEditing ? "Créer" : "Modifier")} un modèle de {TypeName}", UIPanel.PanelType.Input, player, () => SetName(player));

            panel.TextLines.Add("Donner un nom à votre modèle");
            panel.inputPlaceholder = "3 caractères minimum";

            if (!isEditing)
            {
                panel.NextButton("Suivant", async () =>
                {
                    if (panel.inputText.Length >= 3)
                    {
                        PatternName = panel.inputText;
                        await Save();
                        ConfirmGeneratePoint(player, this);
                    }
                    else
                    {
                        player.Notify("Attention", "Vous devez donner un nom à votre modèle (3 caractères minimum)", Life.NotificationManager.Type.Warning);
                        SetName(player);
                    }
                });
            }
            else
            {
                panel.PreviousButtonWithAction("Confirmer", async () =>
                {
                    if (panel.inputText.Length >= 3)
                    {
                        PatternName = panel.inputText;
                        if (await Save()) return true;
                        else
                        {
                            player.Notify("Erreur", "échec lors de la sauvegarde de vos changements", Life.NotificationManager.Type.Error);
                            return false;
                        }
                    }
                    else
                    {
                        player.Notify("Attention", "Vous devez donner un nom à votre modèle (3 caractères minimum)", Life.NotificationManager.Type.Warning);
                        return false;
                    }
                });
            }
            panel.PreviousButton();
            panel.CloseButton();

            panel.Display();
        }
        #endregion

        #region REPLACE YOUR CLASS/TYPE AS PARAMETER
        /// <summary>
        /// Displays a panel allowing the player to select a pattern from a list of patterns.
        /// </summary>
        /// <param name="player">The player selecting the pattern.</param>
        /// <param name="patterns">The list of patterns to choose from.</param>
        /// <param name="configuring">A flag indicating if the player is configuring.</param>
        public void SelectPattern(Player player, List<ChallengePrestigeRewardPoint> patterns, bool configuring)
        {
            Panel panel = Context.PanelHelper.Create("Choisir un modèle", UIPanel.PanelType.Tab, player, () => SelectPattern(player, patterns, configuring));

            foreach (var pattern in patterns)
            {
                panel.AddTabLine($"{pattern.PatternName}", _ => { });
            }
            if (patterns.Count == 0) panel.AddTabLine($"Vous n'avez aucun modèle de {TypeName}", _ => { });

            if (!configuring && patterns.Count != 0)
            {
                panel.CloseButtonWithAction("Confirmer", async () =>
                {
                    if (await Context.PointHelper.CreateNPoint(player, patterns[panel.selectedTab])) return true;
                    else return false;
                });
            }
            else
            {
                panel.NextButton("Modifier", () => {
                    EditPattern(player, patterns[panel.selectedTab].Id);
                });
                panel.NextButton("Supprimer", () => {
                    ConfirmDeletePattern(player, patterns[panel.selectedTab]);
                });
            }

            panel.PreviousButton();
            panel.CloseButton();

            panel.Display();
        }

        /// <summary>
        /// Confirms the generation of a point with a previously saved pattern.
        /// </summary>
        /// <param name="player">The player confirming the point generation.</param>
        /// <param name="pattern">The pattern to generate the point from.</param>
        public void ConfirmGeneratePoint(Player player, ChallengePrestigeRewardPoint pattern)
        {
            Panel panel = Context.PanelHelper.Create($"Modèle \"{pattern.PatternName}\" enregistré !", UIPanel.PanelType.Text, player, () => ConfirmGeneratePoint(player, pattern));

            panel.TextLines.Add($"Voulez-vous générer un point sur votre position avec ce modèle \"{PatternName}\"");

            panel.CloseButtonWithAction("Générer", async () =>
            {
                if (await Context.PointHelper.CreateNPoint(player, pattern)) return true;
                else return false;
            });
            panel.PreviousButton();
            panel.CloseButton();

            panel.Display();
        }
        #endregion

        #region DO NOT EDIT
        /// <summary>
        /// Base panel allowing the user to choose between creating a pattern from scratch
        /// or generating a point from an existing pattern.
        /// </summary>
        /// <param name="player">The player initiating the creation or generation.</param>
        public void CreateOrGenerate(Player player)
        {
            Panel panel = Context.PanelHelper.Create($"Créer ou générer un {TypeName}", UIPanel.PanelType.Text, player, () => CreateOrGenerate(player));

            panel.TextLines.Add(mk.Pos($"{mk.Align($"{mk.Color("Générer", mk.Colors.Info)} utiliser un modèle existant. Les données sont partagés entre les points utilisant un même modèle.", mk.Aligns.Left)}", 5));
            panel.TextLines.Add("");
            panel.TextLines.Add($"{mk.Align($"{mk.Color("Créer:", mk.Colors.Info)} définir un nouveau modèle de A à Z.", mk.Aligns.Left)}");

            panel.NextButton("Créer", () =>
            {
                SetPatternData(player);
            });
            panel.NextButton("Générer", async () =>
            {
                await GetPatternData(player, false);
            });
            panel.AddButton("Retour", ui => AAMenu.AAMenu.menu.AdminPointsPanel(player));
            panel.CloseButton();

            panel.Display();
        }

        /// <summary>
        /// Retrieves all patterns before redirecting to a panel allowing the user various actions (CRUD).
        /// </summary>
        /// <param name="player">The player initiating the retrieval of pattern data.</param>
        /// <param name="configuring">A flag indicating if the user is configuring.</param>
        public async Task GetPatternData(Player player, bool configuring)
        {
            var patterns = await QueryAll();
            SelectPattern(player, patterns, configuring);
        }

        /// <summary>
        /// Confirms the deletion of the specified pattern.
        /// </summary>
        /// <param name="player">The player confirming the deletion.</param>
        /// <param name="patternData">The pattern data to be deleted.</param>
        public async void ConfirmDeletePattern(Player player, PatternData patternData)
        {
            var pattern = await Query(patternData.Id);

            Panel panel = Context.PanelHelper.Create($"Supprimer un modèle de {pattern.TypeName}", UIPanel.PanelType.Text, player, () => ConfirmDeletePattern(player, patternData));

            panel.TextLines.Add($"Cette suppression entrainera également celle des points.");
            panel.TextLines.Add($"Êtes-vous sûr de vouloir supprimer le modèle \"{pattern.PatternName}\" ?");

            panel.PreviousButtonWithAction("Confirmer", async () =>
            {
                if (await Context.PointHelper.DeleteNPointsByPattern(player, pattern))
                {
                    if (await pattern.Delete())
                    {
                        return true;
                    }
                    else
                    {
                        player.Notify("Erreur", $"Nous n'avons pas pu supprimer le modèle \"{PatternName}\"", Life.NotificationManager.Type.Error, 6);
                        return false;
                    }
                }
                else
                {
                    player.Notify("Erreur", "Certains points n'ont pas pu être supprimés.", Life.NotificationManager.Type.Error, 6);
                    return false;
                }
            });
            panel.PreviousButton();
            panel.CloseButton();

            panel.Display();
        }

        /// <summary>
        /// Retrieves all NPoints before redirecting to a panel allowing various actions by the user.
        /// </summary>
        /// <param name="player">The player retrieving the NPoints.</param>
        public async Task GetNPoints(Player player)
        {
            var points = await NPoint.Query(e => e.TypeName == nameof(ChallengePrestigeRewardPoint));
            SelectNPoint(player, points);
        }

        /// <summary>
        /// Lists the points using this pattern.
        /// </summary>
        /// <param name="player">The player selecting the points.</param>
        /// <param name="points">The list of points to choose from.</param>
        public async void SelectNPoint(Player player, List<NPoint> points)
        {
            var patterns = await QueryAll();
            Panel panel = Context.PanelHelper.Create($"Points de type {nameof(ChallengePrestigeRewardPoint)}", UIPanel.PanelType.Tab, player, () => SelectNPoint(player, points));

            if (points.Count > 0)
            {
                foreach (var point in points)
                {
                    var currentPattern = patterns.FirstOrDefault(p => p.Id == point.PatternId);
                    panel.AddTabLine($"point n° {point.Id}: {(currentPattern != default ? currentPattern.PatternName : "???")}", _ => { });
                }

                panel.NextButton("Voir", () =>
                {
                    DisplayNPoint(player, points[panel.selectedTab]);
                });
                panel.NextButton("Supprimer", async () =>
                {
                    await Context.PointHelper.DeleteNPoint(points[panel.selectedTab]);
                    await GetNPoints(player);
                });
            }
            else
            {
                panel.AddTabLine($"Aucun point de ce type", _ => { });
            }
            panel.AddButton("Retour", ui =>
            {
                AAMenu.AAMenu.menu.AdminPointsSettingPanel(player);
            });
            panel.CloseButton();

            panel.Display();
        }

        /// <summary>
        /// Displays the information of a point and allows the user to modify it.
        /// </summary>
        /// <param name="player">The player viewing the point information.</param>
        /// <param name="point">The point to display information for.</param>
        public async void DisplayNPoint(Player player, NPoint point)
        {
            var pattern = await Query(p => p.Id == point.PatternId);
            Panel panel = Context.PanelHelper.Create($"Point n° {point.Id}", UIPanel.PanelType.Tab, player, () => DisplayNPoint(player, point));

            panel.AddTabLine($"Type: {point.TypeName}", _ => { });
            panel.AddTabLine($"Modèle: {(pattern[0] != null ? pattern[0].PatternName : "???")}", _ => { });
            panel.AddTabLine($"", _ => { });
            panel.AddTabLine($"Position: {point.Position}", _ => { });


            panel.AddButton("TP", ui =>
            {
                Context.PointHelper.PlayerSetPositionToNPoint(player, point);
            });
            panel.AddButton("Définir pos.", async ui =>
            {
                await Context.PointHelper.SetNPointPosition(player, point);
                panel.Refresh();
            });
            panel.PreviousButton();
            panel.CloseButton();

            panel.Display();
        }
        #endregion
    }
}
