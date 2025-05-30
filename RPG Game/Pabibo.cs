using System;

namespace RPG_Game
{
    /// <summary>
    /// Pabibo: An overachiever who loves to show their skills and abilities.
    /// Demonstrates Inheritance and Polymorphism.
    /// </summary>
    public class Pabibo : ClassFighter
    {
        private static Random rng = new Random();
        private readonly TimeSpan skillCooldown = TimeSpan.FromSeconds(8); // 8-second cooldown
        private const int skillManaCost = 50;

        public Pabibo(string name) : base(name, 100, 100) { } // 100 HP, 100 Mana

        /// <summary>
        /// Overrides Attack with a random damage between 5 and 10.
        /// </summary>
        public override int Attack()
        {
            return rng.Next(5, 11); // 5 to 10 inclusive
        }

        /// <summary>
        /// Pabibo's skill: Heals self and deals moderate damage while showing off.
        /// </summary>
        public override int UseSkill()
        {
            if (!IsSkillReady || Mana <= 0)
            {
                // Should not be called if not ready or no mana, but handle defensively
                return 0; 
            }
            int damage = rng.Next(25, 31); // Base skill damage: 25-30
            // Scale damage based on mana consumed
            damage = (int)(damage * (1 + (float)Mana / MaxMana)); // More mana = more damage
            // Scale healing based on mana consumed
            int healing = (int)(20 * (1 + (float)Mana / MaxMana)); // More mana = more healing
            ConsumeMana(Mana); // Drain all mana
            StartSkillCooldown(skillCooldown);
            RecoverMana(healing); // Pabibo heals based on mana consumed
            return damage;
        }
    }
} 