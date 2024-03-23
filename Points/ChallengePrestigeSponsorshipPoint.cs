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
using Life.DB;
using System;

namespace ChallengePrestige.Points
{
    public class ChallengePrestigeSponsorshipPoint : ModKit.ORM.ModEntity<ChallengePrestigeSponsorshipPoint>, PatternData
    {
        [AutoIncrement][PrimaryKey] public int Id { get; set; }
        public string TypeName { get; set; }
        public string PatternName { get; set; }

        //Declare your other properties here
        [Ignore] public int minPrestige { get; set; } = 3;
        [Ignore] public int reward { get; set; } = 2500;
        [Ignore] public ModKit.ModKit Context { get; set; }

        public ChallengePrestigeSponsorshipPoint() { }
        public ChallengePrestigeSponsorshipPoint(bool isCreated)
        {
            TypeName = nameof(ChallengePrestigeSponsorshipPoint);
        }

        /// <summary>
        /// Applies the properties retrieved from the database during the generation of a point in the game using this model.
        /// </summary>
        /// <param name="patternId">The identifier of the pattern in the database.</param>
        public async Task SetProperties(int patternId)
        {
            var result = await Query(patternId);

            Id = patternId;
            TypeName = nameof(ChallengePrestigeSponsorshipPoint);
            PatternName = result.PatternName;

            //Add your other properties here
        }

        /// <summary>
        /// Contains the action to perform when a player interacts with the point.
        /// </summary>
        /// <param name="player">The player interacting with the point.</param>
        public void OnPlayerTrigger(Player player)
        {
            SponsorPanel(player);
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
            ChallengePrestigeSponsorshipPoint pattern = new ChallengePrestigeSponsorshipPoint(false);
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

        /// <summary>
        /// Allows the player to set a name for the pattern, either during creation or modification.
        /// </summary>
        /// <param name="player">The player interacting with the panel.</param>
        /// <param name="inEdition">A flag indicating if the pattern is being edited.</param>
        public void SetName(Player player, bool isEditing = false)
        {
            Panel panel = Context.PanelHelper.Create($"{(!isEditing ? "Créer" : "Modifier")} un modèle de {TypeName}", UIPanel.PanelType.Input, player, () =>
            SetName(player));

            panel.TextLines.Add("Donner un nom à votre modèle");
            panel.inputPlaceholder = "3 caractères minimum";

            if (!isEditing)
            {
                panel.NextButton("Suivant", async () =>
                {
                    if (panel.inputText.Length >= 3)
                    {
                        PatternName = panel.inputText;
                        //function to call for the following property
                        // If you want to generate your point
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

        #region CUSTOM
        public async void SponsorPanel(Player player)
        {
            var playerQuery = await ChallengePrestigePlayer.Query(cpp => cpp.PlayerId == player.account.id);

            Panel panel = Context.PanelHelper.Create($"Offrande du jour", UIPanel.PanelType.Text, player, () => SponsorPanel(player));
            if (playerQuery.Count > 0)
            {
                panel.TextLines.Add("Parrainer un citoyen");
                panel.TextLines.Add("en lui partageant votre code");
                panel.TextLines.Add($"{mk.Color($"{Utils.GetCodeByPlayer(player.account)}", mk.Colors.Warning)}");

                panel.NextButton("Filleuls", () => ReferralPanel(player));
                panel.NextButton("Parrain", () => ReferrerPanel(player));
            }
            else panel.TextLines.Add("Pour accéder au système de parrainage, vous devez effectuer votre demande de recensement auprès de la mairie.");

            panel.CloseButton();
            panel.Display();
        }

        public async void ReferrerPanel(Player player)
        {
            var referrerList = await ChallengePrestigeSponsorship.Query(s => s.ReferralId == player.account.id);
            var playerQuery = await ChallengePrestigePlayer.Query(cpp => cpp.PlayerId == player.account.id);

            Panel panel = Context.PanelHelper.Create($"Parrain", referrerList.Count == 0 && playerQuery != null && playerQuery?[0].Prestige <= minPrestige ? UIPanel.PanelType.Input : UIPanel.PanelType.Text, player, () => ReferrerPanel(player));

            if (referrerList.Count == 0 && playerQuery?[0].Prestige > minPrestige)
            {
                panel.TextLines.Add("Vous n'avez jamais eu de parrain.");
            }
            else if(referrerList.Count == 0 && playerQuery?[0].Prestige <= minPrestige)
            {
                panel.TextLines.Add("Renseigner le code de votre parrain:");
                panel.AddButton("Confirmer", async ui =>
                {
                    if(ui.inputText.Length >= 6)
                    {
                        int accountId = Utils.GetPlayerIdFromCode(ui.inputText);
                        Account referrer = await LifeDB.FetchAccount(accountId);
                        var currentCode = Utils.GetCodeByPlayer(referrer);

                        if (referrer != null && referrer.id != player.account.id)
                        {
                            if (currentCode == ui.inputText)
                            {
                                ChallengePrestigeSponsorship challengePrestigeSponsorship = new ChallengePrestigeSponsorship();
                                challengePrestigeSponsorship.ReferralId = player.account.id;
                                challengePrestigeSponsorship.ReferrerId = referrer.id;
                                challengePrestigeSponsorship.Date = Utils.GetNumericalDateOfTheDay();
                                challengePrestigeSponsorship.isClaimedByReferral = false;
                                challengePrestigeSponsorship.isClaimedByReferrer = false;

                                if (await challengePrestigeSponsorship.Save())
                                {
                                    player.Notify("Succès", "Parrain enregistré !", Life.NotificationManager.Type.Success);
                                    panel.Refresh();
                                }
                                else player.Notify("Erreur", "Nous n'avons pas pu enregistrer votre parrain", Life.NotificationManager.Type.Error);
                            }
                            else player.Notify("Erreur", "Code invalide", Life.NotificationManager.Type.Error);
                        }
                        else player.Notify("Erreur", "Parrain introuvable", Life.NotificationManager.Type.Error);
                    }
                    else player.Notify("Erreur", "Format du code invalide", Life.NotificationManager.Type.Error);
                    
                });
            }
            else if(referrerList.Count > 0 && referrerList[0]?.ReferrerId != null)
            {
                var query = await LifeDB.FetchCharacters(referrerList[0].ReferrerId);
                panel.TextLines.Add("Votre parrain:");
                panel.TextLines.Add($"{query[0].firstname} {query[0].lastname}");
                panel.TextLines.Add($"");
                if (playerQuery?[0].Prestige > minPrestige)
                {
                    if (!referrerList[0].isClaimedByReferral)
                    {
                        panel.TextLines.Add("Récupérer votre récompense");
                        panel.AddButton("Récupérer", async ui =>
                        {
                            referrerList[0].isClaimedByReferral = true;
                            if (await referrerList[0].Save())
                            {
                                player.AddBankMoney(reward);
                                player.Notify("Succès", "Vous venez d'empocher 2000€ ! Merci pour votre parrainage");
                                panel.Refresh();
                            }
                        });
                    } else panel.TextLines.Add("Récompense récupérée");
                } else
                {
                    panel.TextLines.Add($"Atteignez le {$"{mk.Color($"Prestige {minPrestige + 1}", mk.Colors.Warning)}"}");
                    panel.TextLines.Add($"pour récupérer votre récompense !");
                }
            }

            panel.PreviousButton();
            panel.CloseButton();

            panel.Display();
        }

        public async void ReferralPanel(Player player)
        {
            var referralList = await ChallengePrestigeSponsorship.Query(s => s.ReferrerId == player.account.id);
            var playerList = await ChallengePrestigePlayer.QueryAll();

            Panel panel = Context.PanelHelper.Create($"Filleuls", UIPanel.PanelType.Tab, player, () => ReferralPanel(player));

            if(referralList.Count != 0)
            {
                foreach (var referral in referralList)
                {
                    var currentReferral = playerList.Where(p => p.PlayerId == referral.ReferralId).FirstOrDefault();

                    string state = null;
                    if (currentReferral.Prestige > minPrestige)
                        if (referral.isClaimedByReferrer) state = "CLAIM";
                        else state = "Prêt";
                    else state = "En attente";

                    panel.AddTabLine($"{mk.Color($"Prestige {currentReferral.Prestige}:", mk.Colors.Info)} {currentReferral.CharacterFullName} {$"{mk.Color(state, mk.Colors.Warning)}"}", async ui => {
                        if (state == "Prêt")
                        {
                            referral.isClaimedByReferrer = true;
                            if (await referral.Save())
                            {
                                player.AddBankMoney(reward);
                                player.Notify("Succès", "Vous venez d'empocher 2000€ !<br> Bonne continuation de notre commune", Life.NotificationManager.Type.Success);
                                panel.Refresh();
                            }
                            else player.Notify("Oops !", "Nous n'avons pas pu valider votre récompense", Life.NotificationManager.Type.Error);
                        }
                        else if (state == "CLAIM") player.Notify("Refus", "Vous avez déjà récupéré cette récompense", Life.NotificationManager.Type.Error); 
                        else player.Notify("Refus", $"Votre filleul doit être prestige {minPrestige + 1} minimum pour récupérer votre récompense", Life.NotificationManager.Type.Error);
                    });
                }
                panel.AddButton("Récupérer", ui => panel.SelectTab());
            } else panel.AddTabLine("Vous n'avez pas de filleul.", _ => { });
            
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
        public void SelectPattern(Player player, List<ChallengePrestigeSponsorshipPoint> patterns, bool configuring)
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
        }

        /// <summary>
        /// Confirms the generation of a point with a previously saved pattern.
        /// </summary>
        /// <param name="player">The player confirming the point generation.</param>
        /// <param name="pattern">The pattern to generate the point from.</param>
        public void ConfirmGeneratePoint(Player player, ChallengePrestigeSponsorshipPoint pattern)
        {
            Panel panel = Context.PanelHelper.Create($"Modèle \"{pattern.PatternName}\" enregistré !", UIPanel.PanelType.Text, player, () =>
            ConfirmGeneratePoint(player, pattern));

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
            panel.AddButton("Retour", ui =>
            {
                AAMenu.AAMenu.menu.AdminPointsPanel(player);
            });
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

            Panel panel = Context.PanelHelper.Create($"Supprimer un modèle de {pattern.TypeName}", UIPanel.PanelType.Text, player, () =>
            ConfirmDeletePattern(player, patternData));

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
            var points = await NPoint.Query(e => e.TypeName == nameof(ChallengePrestigeSponsorshipPoint));
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
            Panel panel = Context.PanelHelper.Create($"Points de type {nameof(ChallengePrestigeSponsorshipPoint)}", UIPanel.PanelType.Tab, player, () => SelectNPoint(player, points));

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
