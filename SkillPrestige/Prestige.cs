using System;
using System.Collections.Generic;
using System.Linq;
using SkillPrestige.Framework;
using SkillPrestige.Logging;
using SkillPrestige.Professions;
using SkillPrestige.SkillTypes;
using StardewValley;

namespace SkillPrestige
{
    /// <summary>Represents a prestige for a skill.</summary>
    [Serializable]
    public class Prestige
    {
        /// <summary>The skill the prestige is for.</summary>
        public SkillType SkillType { get; set; }

        /// <summary>The total available prestige points, one is gained per skill reset.</summary>
        public int PrestigePoints { get; set; }

        /// <summary>Professions that have been chosen to be permanent using skill points.</summary>
        public IList<int> PrestigeProfessionsSelected { get; set; } = new List<int>();

        public Dictionary<string, int> CraftingRecipeAmountsToSave { get; set; } = new();

        public Dictionary<string, int> CookingRecipeAmountsToSave { get; set; } = new();

        public void FixDeserializedNulls()
        {
            this.CraftingRecipeAmountsToSave ??= new Dictionary<string, int>();
            this.CookingRecipeAmountsToSave ??= new Dictionary<string, int>();
        }

        /// <summary>Purchases a profession to be part of the prestige set.</summary>
        public static void AddPrestigeProfession(int professionId)
        {
            var skill = Skill.AllSkills.Single(x => x.Professions.Select(y => y.Id).Contains(professionId));
            var prestige = PrestigeSet.Instance.Prestiges.Single(x => x.SkillType == skill.Type);
            int originalPrestigePointsForSkill = prestige.PrestigePoints;
            if (skill.Professions.Where(x => x.LevelAvailableAt == 5).Select(x => x.Id).Contains(professionId))
            {
                prestige.PrestigePoints -= PerSaveOptions.Instance.CostOfTierOnePrestige;
                Logger.LogInformation($"Spent prestige point on {skill.Type.Name} skill.");
            }

            else if (skill.Professions.Where(x => x.LevelAvailableAt == 10).Select(x => x.Id).Contains(professionId))
            {
                prestige.PrestigePoints -= PerSaveOptions.Instance.CostOfTierTwoPrestige;
                Logger.LogInformation($"Spent 2 prestige points on {skill.Type.Name} skill.");
            }
            else
                Logger.LogError($"No skill found for selected profession: {professionId}");
            if (prestige.PrestigePoints < 0)
            {
                prestige.PrestigePoints = originalPrestigePointsForSkill;
                Logger.LogCritical($"Prestige amount for {skill.Type.Name} skill would have gone negative, unable to grant profession {professionId}. Prestige values reset.");
            }
            else
            {
                prestige.PrestigeProfessionsSelected.Add(professionId);
                Logger.LogInformation("Profession permanently added.");
                Profession.AddMissingProfessions();
            }
        }

        /// <summary>Prestiges a skill, resetting it to level 0, removing all recipes and effects of the skill at higher levels and grants one prestige point in that skill to the player.</summary>
        /// <param name="skill">the skill you wish to prestige.</param>
        public static void PrestigeSkill(Skill skill)
        {
            try
            {
                // previously painless prestige:
                Logger.LogInformation($"Prestiging skill {skill.Type.Name} via Painless Mode.");
                skill.SetSkillExperience(skill.GetSkillExperience() - PerSaveOptions.Instance.ExperienceNeededPerPainlessPrestige);
                Logger.LogInformation($"Removed {PerSaveOptions.Instance.ExperienceNeededPerPainlessPrestige} experience points from {skill.Type.Name} skill.");


                PrestigeSet.Instance.Prestiges.Single(x => x.SkillType == skill.Type).PrestigePoints += PerSaveOptions.Instance.PointsPerPrestige;
                Logger.LogInformation($"{PerSaveOptions.Instance.PointsPerPrestige} Prestige point(s) added to {skill.Type.Name} skill.");

            }
            catch (Exception exception)
            {
                Logger.LogError(exception.Message + Environment.NewLine + exception.StackTrace);
            }
        }

        /// <summary>Removes all cooking recipes granted by levelling a skill.</summary>
        /// <param name="skillType">the skill type to remove all cooking recipes from.</param>
        private static Dictionary<string, int> RemovePlayerCookingRecipesForSkill(SkillType skillType)
        {
            // if (skillType.Name.IsOneOf("Cooking", string.Empty))
            // {
            //     Logger.LogInformation($"Wiping skill cooking recipes for skill: {skillType.Name} could remove more than intended. Exiting skill cooking recipe wipe.");
            //     return null;
            // }
            Logger.LogInformation($"Removing {skillType.Name} cooking recipes.");
            var cookingAmountsToStore = new Dictionary<string, int>();
            foreach (
                var recipe in
                CraftingRecipe.cookingRecipes.Where(
                    x =>
                    {
                        string[] recipePieces = x.Value.Split('/');
                        return recipePieces.Length >=4
                               && recipePieces[3].Contains(skillType.Name)
                               && Game1.player.cookingRecipes.ContainsKey(x.Key);
                    }))
            {
                Logger.LogVerbose($"Removing {skillType.Name} cooking recipe {recipe.Key}");
                    cookingAmountsToStore.Add(recipe.Key, Game1.player.cookingRecipes[recipe.Key]);
                    Game1.player.cookingRecipes.Remove(recipe.Key);
            }
            Logger.LogInformation($"{skillType.Name} cooking recipes removed.");
            return cookingAmountsToStore;
        }
    }
}
