using System;

namespace RPG_Game
{
    /// <summary>
    /// Abstract base class representing a fighter in the classroom RPG.
    /// Demonstrates Abstraction and Encapsulation.
    /// </summary>
    public abstract class ClassFighter
    {
        // Encapsulated fields
        public string Name { get; set; }
        public int Health { get; protected set; }
        public int MaxHealth { get; protected set; }
        public int Mana { get; protected set; }
        public int MaxMana { get; protected set; }
        public bool IsSkillReady { get; protected set; }
        public DateTime SkillCooldownEnd { get; protected set; }

        public ClassFighter(string name, int maxHealth, int maxMana)
        {
            Name = name;
            MaxHealth = maxHealth;
            Health = maxHealth;
            MaxMana = maxMana;
            Mana = maxMana; // Start with full mana
            IsSkillReady = true;
            SkillCooldownEnd = DateTime.Now;
        }

        /// <summary>
        /// Abstract attack method (Abstraction & Polymorphism).
        /// </summary>
        /// <returns>Damage dealt as int.</returns>
        public abstract int Attack();

        /// <summary>
        /// Abstract skill method (Abstraction & Polymorphism).
        /// </summary>
        /// <returns>Damage or effect value as int.</returns>
        public abstract int UseSkill();

        /// <summary>
        /// Reduces health by the specified damage amount.
        /// </summary>
        /// <param name="damage">Amount of damage taken.</param>
        public void TakeDamage(int damage)
        {
            Health -= damage;
            if (Health < 0) Health = 0;
        }

        /// <summary>
        /// Increases mana by the specified amount, up to MaxMana.
        /// </summary>
        /// <param name="amount">Amount of mana to recover.</param>
        public void RecoverMana(int amount)
        {
            Mana += amount;
            if (Mana > MaxMana) Mana = MaxMana;
        }

        /// <summary>
        /// Consumes mana for using a skill.
        /// </summary>
        /// <param name="amount">Amount of mana to consume.</param>
        public void ConsumeMana(int amount)
        {
            Mana -= amount;
            if (Mana < 0) Mana = 0;
        }

        /// <summary>
        /// Sets skill to cooldown.
        /// </summary>
        /// <param name="cooldownDuration">Duration of the cooldown.</param>
        public void StartSkillCooldown(TimeSpan cooldownDuration)
        {
            IsSkillReady = false;
            SkillCooldownEnd = DateTime.Now + cooldownDuration;
        }

        /// <summary>
        /// Checks if skill is ready and updates state.
        /// </summary>
        public void UpdateSkillStatus()
        {
            if (!IsSkillReady && DateTime.Now >= SkillCooldownEnd)
            {
                IsSkillReady = true;
            }
        }
    }
} 