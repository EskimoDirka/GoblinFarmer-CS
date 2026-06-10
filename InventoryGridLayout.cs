using System.Drawing;
using DrawingPoint = System.Drawing.Point;

namespace GoblinFarmer
{
    internal static class InventoryGridLayout
    {
        public const int Columns = 10;
        public const int Rows = 6;

        public static readonly Rectangle ReferenceRectangle = new(1864, 725, 687, 423);

        public static Rectangle SlotRectangle(Bitmap inventoryGrid, int row, int column)
        {
            int slotWidth = inventoryGrid.Width / Columns;
            int slotHeight = inventoryGrid.Height / Rows;
            return new Rectangle(column * slotWidth, row * slotHeight, slotWidth, slotHeight);
        }

        public static DrawingPoint SlotScreenPoint(Rectangle screenGrid, Rectangle localSlot)
        {
            return new DrawingPoint(
                screenGrid.Left + localSlot.Left + (localSlot.Width / 2),
                screenGrid.Top + localSlot.Top + (localSlot.Height / 2));
        }
    }
}
