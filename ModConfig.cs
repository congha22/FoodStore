﻿using StardewModdingAPI;

namespace FoodStore
{
    public class ModConfig
    {
        public bool EnableMod { get; set; } = true;
        public bool EnablePrice { get; set; } = false;
        public bool EnableTip { get; set; } = false;
        public bool TipWhenNeaBy { get; set; } = true;
        public bool RushHour { get; set; } = true;

        public int MinutesToHungry { get; set; } = 600;
        public float MoveToFoodChance { get; set; } = 0.01f;
        public float MaxDistanceToFind{ get; set; } = 40;
        public float MaxDistanceToEat { get; set; } = 4f;



        public float LoveMultiplier { get; set; } = -1f;
        public float LikeMultiplier { get; set; } = -1f;
        public float NeutralMultiplier { get; set; } = -1f;
        public float DislikeMultiplier { get; set; } = -1f;
        public float HateMultiplier { get; set; } = -1f;

        public float TipLove { get; set; } = -1f;
        public float TipLike { get; set; } = -1f;
        public float TipNeutral { get; set; } = -1f;
        public float TipDislike { get; set; } = -1f;
        public float TipHate { get; set; } = -1f;


    }
}
