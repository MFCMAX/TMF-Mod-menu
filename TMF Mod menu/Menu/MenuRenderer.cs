using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StudioForge.Engine;

namespace TMFModMenu.Menu
{
    internal sealed class MenuRenderer
    {
        private static readonly Color ShadowColor = new(0, 0, 0, 145);
        private static readonly Color PanelColor = new(12, 12, 11, 222);
        private static readonly Color HeaderColor = new(66, 12, 10, 240);
        private static readonly Color AccentColor = new(151, 38, 23, 255);
        private static readonly Color BorderColor = new(91, 79, 65, 210);
        private static readonly Color SelectionColor = new(112, 29, 21, 225);
        private static readonly Color SelectedTextColor = new(255, 239, 207, 255);
        private static readonly Color TitleColor = new(241, 231, 204, 255);
        private static readonly Color RowColor = new(218, 211, 194, 255);
        private static readonly Color MutedColor = new(151, 143, 128, 255);
        private static readonly Color DisabledColor = new(100, 96, 88, 255);

        internal void Draw(
            SpriteBatchSafe spriteBatch,
            Texture2D blankTexture,
            SpriteFont titleFont,
            SpriteFont rowFont,
            SpriteFont smallFont,
            Viewport viewport,
            MenuSnapshot snapshot)
        {
            if (spriteBatch == null || blankTexture == null || titleFont == null ||
                rowFont == null || smallFont == null || snapshot == null)
                return;

            var layout = MenuLayout.Calculate(
                viewport.X,
                viewport.Y,
                viewport.Width,
                viewport.Height,
                snapshot.Rows.Count);
            var panel = new Rectangle(layout.X, layout.Y, layout.Width, layout.Height);
            var shadow = new Rectangle(panel.X + 5, panel.Y + 5, panel.Width, panel.Height);
            var header = new Rectangle(panel.X, panel.Y, panel.Width, 42);
            var accent = new Rectangle(panel.X, panel.Y, panel.Width, 6);

            bool ownsBatch = !spriteBatch.BeginCalled;
            if (ownsBatch)
                spriteBatch.Begin();

            // SpriteBatchSafe reports and swallows a failed Begin.
            if (!spriteBatch.BeginCalled)
                return;

            try
            {
                spriteBatch.Draw(blankTexture, shadow, ShadowColor);
                spriteBatch.Draw(blankTexture, panel, PanelColor);
                spriteBatch.Draw(blankTexture, header, HeaderColor);
                spriteBatch.Draw(blankTexture, accent, AccentColor);
                DrawBorder(spriteBatch, blankTexture, panel);

                spriteBatch.DrawString(
                    titleFont,
                    "TMF MOD MENU",
                    new Vector2(panel.X + 16, panel.Y + 12),
                    TitleColor);
                spriteBatch.DrawString(
                    smallFont,
                    snapshot.Breadcrumb,
                    new Vector2(panel.X + 17, panel.Y + 48),
                    MutedColor);

                string pageText = snapshot.PageIndicator;
                float pageWidth = smallFont.MeasureString(pageText).X;
                spriteBatch.DrawString(
                    smallFont,
                    pageText,
                    new Vector2(panel.Right - 17 - pageWidth, panel.Y + 48),
                    MutedColor);
                spriteBatch.Draw(
                    blankTexture,
                    new Rectangle(panel.X + 12, panel.Y + 69, panel.Width - 24, 1),
                    BorderColor);

                for (int i = 0; i < snapshot.Rows.Count; i++)
                {
                    var row = snapshot.Rows[i];
                    int rowY = layout.RowsY + (i * layout.RowHeight);
                    float textY = rowY + Math.Max(
                        0,
                        (layout.RowHeight - rowFont.LineSpacing) / 2f);
                    bool isSelected = row.IsSelected;
                    if (isSelected)
                    {
                        spriteBatch.Draw(
                            blankTexture,
                            new Rectangle(
                                panel.X + 9,
                                rowY,
                                panel.Width - 18,
                                layout.RowHeight - 1),
                            SelectionColor);
                        spriteBatch.DrawString(
                            rowFont,
                            ">",
                            new Vector2(panel.X + 15, textY),
                            SelectedTextColor);
                    }

                    Color textColor = !row.IsEnabled
                        ? DisabledColor
                        : isSelected ? SelectedTextColor : RowColor;
                    spriteBatch.DrawString(
                        rowFont,
                        row.Label,
                        new Vector2(panel.X + 39, textY),
                        textColor);

                    if (!string.IsNullOrEmpty(row.Value))
                    {
                        float valueWidth = rowFont.MeasureString(row.Value).X;
                        spriteBatch.DrawString(
                            rowFont,
                            row.Value,
                            new Vector2(panel.Right - 18 - valueWidth, textY),
                            textColor);
                    }
                }

                int footerY = Math.Min(layout.FooterY, panel.Bottom - 45);
                spriteBatch.Draw(
                    blankTexture,
                    new Rectangle(panel.X + 12, footerY, panel.Width - 24, 1),
                    BorderColor);
                spriteBatch.DrawString(
                    smallFont,
                    "UP/DOWN MOVE   RIGHT SELECT",
                    new Vector2(panel.X + 16, footerY + 8),
                    MutedColor);
                spriteBatch.DrawString(
                    smallFont,
                    "LEFT BACK   CTRL+L CLOSE",
                    new Vector2(panel.X + 16, footerY + 7 + smallFont.LineSpacing),
                    MutedColor);
            }
            finally
            {
                if (ownsBatch && spriteBatch.BeginCalled)
                    spriteBatch.End();
            }
        }

        private static void DrawBorder(
            SpriteBatchSafe spriteBatch,
            Texture2D blankTexture,
            Rectangle panel)
        {
            spriteBatch.Draw(blankTexture, new Rectangle(panel.X, panel.Y, panel.Width, 1), BorderColor);
            spriteBatch.Draw(blankTexture, new Rectangle(panel.X, panel.Bottom - 1, panel.Width, 1), BorderColor);
            spriteBatch.Draw(blankTexture, new Rectangle(panel.X, panel.Y, 1, panel.Height), BorderColor);
            spriteBatch.Draw(blankTexture, new Rectangle(panel.Right - 1, panel.Y, 1, panel.Height), BorderColor);
        }
    }
}
