﻿using Life.Network;
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

namespace ChallengePrestige
{
    public class ChallengePrestigeTasksPoint : ModKit.ORM.ModEntity<ChallengePrestigeTasksPoint>, PatternData
    {
        [AutoIncrement][PrimaryKey] public int Id { get; set; }
        public string TypeName { get; set; }
        public string PatternName { get; set; }

        [Ignore] public ModKit.ModKit Context { get; set; }

        public ChallengePrestigeTasksPoint() { }
        public ChallengePrestigeTasksPoint(bool isCreated)
        {
            TypeName = nameof(ChallengePrestigeTasksPoint);
        }

        /// <summary>
        /// Applies the properties retrieved from the database during the generation of a point in the game using this model.
        /// </summary>
        /// <param name="patternId">The identifier of the pattern in the database.</param>
        public async Task SetProperties(int patternId)
        {
            var result = await Query(patternId);

            Id = patternId;
            TypeName = nameof(ChallengePrestigeTasksPoint);
            PatternName = result.PatternName;

            //Add your other properties here
        }

        /// <summary>
        /// Contains the action to perform when a player interacts with the point.
        /// </summary>
        /// <param name="player">The player interacting with the point.</param>
        public void OnPlayerTrigger(Player player)
        {
            TaskPanel(player);
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
            ChallengePrestigeTasksPoint pattern = new ChallengePrestigeTasksPoint(false);
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
        public async void TaskPanel(Player player)
        {
            int today = DateUtils.GetNumericalDateOfTheDay();
            var taskQuery = await ChallengePrestigeTask.Query(cpt => cpt.Date == today);
            var playerQuery = await ChallengePrestigePlayer.Query(cpp => cpp.PlayerId == player.account.id);

            Panel panel = Context.PanelHelper.Create($"Offrande du jour", UIPanel.PanelType.Text, player, () => TaskPanel(player));

            if(playerQuery.Count > 0 && playerQuery != null)
            {
                if (taskQuery.Count > 0)
                {
                    if (playerQuery[0].LastTaskCompleted == taskQuery[0].Date)
                    {
                        panel.TextLines.Add("Merci !");
                        panel.TextLines.Add("Vous avez déjà soutenu votre");
                        panel.TextLines.Add("commune aujourd'hui.");
                        panel.TextLines.Add("Revenez demain.");
                    }
                    else
                    {
                        panel.TextLines.Add($"Soutenez votre commune !");
                        panel.TextLines.Add($"Offrir: {mk.Color(taskQuery[0].Quantity.ToString(), mk.Colors.Warning)} {mk.Color(ItemUtils.GetItemById(taskQuery[0].ItemId).itemName, mk.Colors.Warning)}");
                        panel.TextLines.Add($"avant la fin de la journée !");

                        panel.CloseButtonWithAction("Offrir", async () =>
                        {
                            var itemSelling = InventoryUtils.RemoveFromInventory(player, taskQuery[0].ItemId, taskQuery[0].Quantity);
                            if (itemSelling != default)
                            {
                                player.Notify("Succès", $"offrande quotidienne résolue\n{mk.Size(mk.Color("Gain de Prestige !", mk.Colors.Success), 20)}", Life.NotificationManager.Type.Success);
                                playerQuery[0].Prestige += 1;
                                playerQuery[0].LastTaskCompleted = taskQuery[0].Date;
                                taskQuery[0].Count++;
                                await taskQuery[0].Save();
                                return await playerQuery[0].Save();
                            }
                            else
                            {
                                player.Notify("Refus", "Vous n'avez pas les objets nécéssaire en nombre suffisant.");
                                return false;
                            }
                        });
                    }
                }
                else player.Notify("Oops !", "Nous n'avons pas pu récupérer la quête quotidienne.", Life.NotificationManager.Type.Error);
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
        public void SelectPattern(Player player, List<ChallengePrestigeTasksPoint> patterns, bool configuring)
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

            panel.AddButton("Retour", ui =>
            {
                AAMenu.AAMenu.menu.InteractionPanel(player, AAMenu.AAMenu.menu.InteractionTabLines);
            });
            panel.CloseButton();

            panel.Display();
        } //PARAM

        /// <summary>
        /// Confirms the generation of a point with a previously saved pattern.
        /// </summary>
        /// <param name="player">The player confirming the point generation.</param>
        /// <param name="pattern">The pattern to generate the point from.</param>
        public void ConfirmGeneratePoint(Player player, ChallengePrestigeTasksPoint pattern)
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
        } //PARAM
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
            var points = await NPoint.Query(e => e.TypeName == nameof(ChallengePrestigeTasksPoint));
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
            Panel panel = Context.PanelHelper.Create($"Points de type {nameof(ChallengePrestigeTasksPoint)}", UIPanel.PanelType.Tab, player, () => SelectNPoint(player, points));

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
