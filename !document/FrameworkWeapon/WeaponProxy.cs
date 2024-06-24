//using Microsoft.Xna.Framework;
//using StardewValley;
//using StardewValley.GameData.Weapons;
//using StardewValley.Tools;

//namespace MarketTown
//{
//    public class WeaponProxy : Object
//    {
//        public MeleeWeapon Weapon { get; set; } = null;

//        public new int ParentSheetIndex
//        {
//            get { return Weapon?.ParentSheetIndex ?? 0; }
//            set
//            {
//                if (Weapon != null)
//                {
//                    Weapon.ParentSheetIndex = value;
//                }
//            }
//        }
//        public int WeaponName
//        {
//            get { return Weapon?.Name ?? ""; }
//        }

//        public int SalePrice
//        {
//            get { return Weapon?.salePrice() ?? 0; }
//        }

//        public WeaponProxy(MeleeWeapon weapon)
//        {
//            Weapon = weapon;
//        }
         
//        public WeaponProxy() { }

//        public Item GetWeaponOne()
//        {
//            Weapon.Name = Name;
//            return Weapon.getOne() as Object;
//        }

//        public override bool performDropDownAction(Farmer who)
//        {
//            return false;
//        }

//        public override void performRemoveAction()
//        {
//            // Do nothing.
//        }
//    }
//}
