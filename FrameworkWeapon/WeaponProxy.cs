using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Tools;

namespace MarketTown
{
    public class WeaponProxy : Object
    {
        public MeleeWeapon Weapon { get; set; } = null;

        public new int ParentSheetIndex
        {
            get { return Weapon?.ParentSheetIndex ?? 0; }
            set
            {
                if (Weapon != null)
                {
                    Weapon.ParentSheetIndex = value;
                }
            }
        }
        public string WeaponName
        {
            get { return Weapon?.Name ?? ""; }
        }

        public int SalePrice
        {
            get { return Weapon?.salePrice() ?? 0; }
        }

        public WeaponProxy(MeleeWeapon weapon)
        {
            Weapon = weapon;
        }

        public WeaponProxy() { }

        public override Item getOne()
        {
            Weapon.Name = Name;
            return Weapon.getOne();
        }

        public override bool performDropDownAction(Farmer who)
        {
            return false;
        }

        public override void performRemoveAction(Vector2 tileLocation, GameLocation environment)
        {
            // Do nothing.
        }
    }
}