using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Space_Snake
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        // Textury
        private Texture2D snakeSegmentTexture;
        private Texture2D obstacleTexture;
        private Texture2D portalTexture;
        private Texture2D starTexture;

        // Had
        private List<Vector2> snakeSegments = new List<Vector2>();
        private int segmentCount = 3;          // přesně 3 segmenty
        private float segmentSpacing = 20f;    // vzdálenost mezi segmenty
        private Vector2 snakeHeadPos;

        // Překážky
        private List<Vector2> obstacles = new List<Vector2>();
        private List<Vector2> obstacleSizes = new List<Vector2>();
        private Random rand = new Random();

        // Portál
        private Vector2 portalPosition;
        private int portalSize = 50;

        // Scrolling / level
        private float scrollSpeed = 4f;        // můžeš zvyšovat (rychlejší)
        private float levelLength = 0f;        // celková délka levelu (px)

        // Hvězdy pro efekt
        private List<Vector2> stars = new List<Vector2>();
        private int starCount = 150;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _graphics.PreferredBackBufferWidth = 800;
            _graphics.PreferredBackBufferHeight = 600;
            _graphics.ApplyChanges();

            // start pozice hlavy
            snakeHeadPos = new Vector2(100, 300);

            // vytvoříme 3 segmenty těsně za hlavou
            snakeSegments.Clear();
            for (int i = 0; i < segmentCount; i++)
                snakeSegments.Add(snakeHeadPos - new Vector2(i * segmentSpacing, 0));

            // hvězdy
            stars.Clear();
            for (int i = 0; i < starCount; i++)
                stars.Add(new Vector2(rand.Next(0, 800), rand.Next(0, 600)));

            // Generujeme dlouhou řadu překážek (level delší)
            GenerateObstacles(25, 300, 150, 300); // 25 překážek, startOffset 300, minSpacing 150, maxSpacing 300

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            snakeSegmentTexture = new Texture2D(GraphicsDevice, 1, 1);
            snakeSegmentTexture.SetData(new[] { Color.LimeGreen });

            obstacleTexture = new Texture2D(GraphicsDevice, 1, 1);
            obstacleTexture.SetData(new[] { Color.OrangeRed });

            portalTexture = new Texture2D(GraphicsDevice, 1, 1);
            portalTexture.SetData(new[] { Color.CornflowerBlue }); // modrý portál

            starTexture = new Texture2D(GraphicsDevice, 1, 1);
            starTexture.SetData(new[] { Color.White });
        }

        // Vytvoří překážky a nastaví portalPosition za poslední překážku
        private void GenerateObstacles(int count, float startOffset, int minSpacing, int maxSpacing)
        {
            obstacles.Clear();
            obstacleSizes.Clear();

            float x = 400 + startOffset; // začneme dále vpravo (delší level)
            for (int i = 0; i < count; i++)
            {
                float sizeW = rand.Next(30, 70);
                float sizeH = rand.Next(30, 120); // někde vysoké, někde nízké
                float y = rand.Next(0, _graphics.PreferredBackBufferHeight - (int)sizeH);

                obstacles.Add(new Vector2(x, y));
                obstacleSizes.Add(new Vector2(sizeW, sizeH));

                float spacing = rand.Next(minSpacing, maxSpacing);
                x += spacing;
            }

            // portal bude za poslední překážkou + offset
            portalPosition = new Vector2(x + 250, _graphics.PreferredBackBufferHeight / 2 - portalSize / 2);

            // ulož celkovou délku (pro případ potřeby)
            levelLength = x + 500;
        }

        protected override void Update(GameTime gameTime)
        {
            var kstate = Keyboard.GetState();
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Ukončit hru ESC
            if (kstate.IsKeyDown(Keys.Escape))
                Exit();

            // Pohyb hlavy nahoru/dolů (X zůstává konstantní)
            if (kstate.IsKeyDown(Keys.Up)) snakeHeadPos.Y -= 220f * dt;
            if (kstate.IsKeyDown(Keys.Down)) snakeHeadPos.Y += 220f * dt;
            snakeHeadPos.Y = MathHelper.Clamp(snakeHeadPos.Y, 0, _graphics.PreferredBackBufferHeight - 20);

            // --- Posun světa doleva (obstacles a portal)
            for (int i = 0; i < obstacles.Count; i++)
                obstacles[i] -= new Vector2(scrollSpeed, 0);

            portalPosition.X -= scrollSpeed;

            // posun hvězd (jen jednoduchý efekt)
            for (int i = 0; i < stars.Count; i++)
            {
                stars[i] -= new Vector2(scrollSpeed * 0.6f, 0);
                if (stars[i].X < 0)
                    stars[i] = new Vector2(800 + rand.Next(0, 200), rand.Next(0, 600));
            }

            // Odstranění překážek, které už jsou mimo obrazovku
            for (int i = obstacles.Count - 1; i >= 0; i--)
            {
                if (obstacles[i].X + obstacleSizes[i].X < 0)
                {
                    obstacles.RemoveAt(i);
                    obstacleSizes.RemoveAt(i);
                }
            }

            // --- Segmenty hada: méně „plovoucí“, hladké sledování
            // Hlavu nastavíme do seznamu
            snakeSegments[0] = snakeHeadPos;

            // Pro každý následující segment spočítáme cílovou pozici těsně za předchozím
            for (int i = 1; i < snakeSegments.Count; i++)
            {
                Vector2 prev = snakeSegments[i - 1];
                Vector2 cur = snakeSegments[i];
                Vector2 dir = cur - prev;
                float dist = dir.Length();

                if (dist == 0) dir = new Vector2(1, 0);
                else dir /= dist;

                Vector2 desired = prev + dir * segmentSpacing * -1f; // pozice za předchozím
                // Lerp pro plynulost: menší hodnota = méně "plovoucí" (rychlejší dorovnání)
                snakeSegments[i] = Vector2.Lerp(cur, desired, 0.5f);
            }

            // --- Kolize s překážkami (hlava vs obstacles)
            Rectangle headRect = new Rectangle((int)snakeSegments[0].X, (int)snakeSegments[0].Y, 20, 20);
            for (int i = 0; i < obstacles.Count; i++)
            {
                Rectangle obsRect = new Rectangle((int)obstacles[i].X, (int)obstacles[i].Y,
                                                  (int)obstacleSizes[i].X, (int)obstacleSizes[i].Y);
                if (headRect.Intersects(obsRect))
                {
                    // restart levelu (můžeš změnit chování)
                    ResetLevel();
                    return;
                }
            }

            // --- Kolize s portálem (vyhráno)
            Rectangle portalRect = new Rectangle((int)portalPosition.X, (int)portalPosition.Y, portalSize, portalSize);
            if (headRect.Intersects(portalRect))
            {
                // tady může být animace nebo další level; prozatím restart a zachovej 3 segmenty
                ResetLevel();
                return;
            }

            base.Update(gameTime);
        }

        private void ResetLevel()
        {
            // reset pozice hada
            snakeHeadPos = new Vector2(100, 300);
            snakeSegments.Clear();
            for (int i = 0; i < segmentCount; i++)
                snakeSegments.Add(snakeHeadPos - new Vector2(i * segmentSpacing, 0));

            // znovu vygenerovat delší sadu překážek a portal
            GenerateObstacles(25, 300, 150, 300);

            // reset hvězd
            stars.Clear();
            for (int i = 0; i < starCount; i++)
                stars.Add(new Vector2(rand.Next(0, 800), rand.Next(0, 600)));
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin();

            // vykreslení hvězd
            foreach (var s in stars)
                _spriteBatch.Draw(starTexture, new Rectangle((int)s.X, (int)s.Y, 2, 2), Color.White);

            // překážky
            for (int i = 0; i < obstacles.Count; i++)
            {
                _spriteBatch.Draw(obstacleTexture,
                    new Rectangle((int)obstacles[i].X, (int)obstacles[i].Y, (int)obstacleSizes[i].X, (int)obstacleSizes[i].Y),
                    Color.OrangeRed);
            }

            // portál (modrý) - vykreslí se až když dojde do zorného pole
            _spriteBatch.Draw(portalTexture, new Rectangle((int)portalPosition.X, (int)portalPosition.Y, portalSize, portalSize), Color.CornflowerBlue);

            // had (3 segmenty)
            for (int i = 0; i < snakeSegments.Count; i++)
            {
                // menší tlumení pro zadní segmenty, aby vizuálně vypadaly lépe
                int size = 20;
                if (i == 0) size = 20;        // hlava
                else if (i == 1) size = 18;   // tělo
                else size = 16;              // ocas
                _spriteBatch.Draw(snakeSegmentTexture, new Rectangle((int)snakeSegments[i].X, (int)snakeSegments[i].Y, size, size), Color.LimeGreen);
            }

            _spriteBatch.End();
            base.Draw(gameTime);
        }
    }
}
