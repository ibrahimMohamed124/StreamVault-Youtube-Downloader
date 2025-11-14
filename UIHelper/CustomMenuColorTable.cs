using System.Drawing;
using System.Windows.Forms;

namespace StreamVaultWinForms.UIHelper
{
    public class CustomMenuRenderer : ToolStripProfessionalRenderer
    {
        public CustomMenuRenderer() : base(new CustomMenuColorTable()) { }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            Color bgColor;

            if (e.Item.Selected)
                bgColor = Color.MediumSlateBlue;
            else
                bgColor = Color.FromArgb(40, 40, 55);

            using (var brush = new SolidBrush(bgColor))
            {
                e.Graphics.FillRectangle(brush, e.Item.ContentRectangle);
            }
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using (var brush = new SolidBrush(Color.FromArgb(40, 40, 55)))
            {
                e.Graphics.FillRectangle(brush, e.AffectedBounds);
            }
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = Color.White;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            using (var pen = new Pen(Color.FromArgb(70, 70, 90)))
            {
                e.Graphics.DrawLine(pen, 0, e.Item.Height / 2, e.Item.Width, e.Item.Height / 2);
            }
        }
    }

    public class CustomMenuColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => Color.FromArgb(40, 40, 55);
        public override Color MenuItemBorder => Color.FromArgb(40, 40, 55);
        public override Color MenuItemSelected => Color.MediumSlateBlue;
        public override Color MenuItemSelectedGradientBegin => Color.MediumSlateBlue;
        public override Color MenuItemSelectedGradientEnd => Color.MediumSlateBlue;
        public override Color MenuItemPressedGradientBegin => Color.MediumSlateBlue;
        public override Color MenuItemPressedGradientEnd => Color.MediumSlateBlue;
    }
}
