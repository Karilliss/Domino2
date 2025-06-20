using System;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using DominoGame;

namespace DominoGameUI
{
    public class DominoControl : UserControl
    {
        private const int TileWidth = 45;
        private const int TileHeight = 90;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DominoPiece Piece { get; private set; }

        public DominoControl(DominoPiece piece)
        {
            Piece = piece ?? throw new ArgumentNullException(nameof(piece));
            this.Size = new Size(TileWidth, TileHeight);
            this.BackColor = Color.White;
            this.DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            DrawDomino(e.Graphics, 0, 0, Piece.Value1, Piece.Value2);
        }

        private void DrawDomino(Graphics g, int x, int y, int top, int bottom)
        {
            Rectangle rect = new Rectangle(x, y, TileWidth, TileHeight);
            g.FillRectangle(Brushes.LightGray, rect);
            g.DrawRectangle(Pens.Black, rect);

            // Divider line
            g.DrawLine(Pens.Black, x, y + TileHeight / 2, x + TileWidth, y + TileHeight / 2);

            using (Font font = new Font("Arial", 14, FontStyle.Bold))
            using (StringFormat format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.DrawString(top.ToString(), font, Brushes.Black, new RectangleF(x, y, TileWidth, TileHeight / 2), format);
                g.DrawString(bottom.ToString(), font, Brushes.Black, new RectangleF(x, y + TileHeight / 2, TileWidth, TileHeight / 2), format);
            }
        }
    }
}