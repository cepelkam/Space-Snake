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

        private Texture2D snakeSegmentTexture;
        private Texture2D obstacleTexture;
        private Texture2D portalTexture;
        private Texture2D starTexture;

        private List<Vector2> snakeSegments = new List<Vector2>();
        private int segmentCount = 2; // jen 2 kostky
        private float segmentSpacing = 20f;

        private Vector2 snakePosition;
        private Vector2 snakeVelocity;

        private List<Vector2> obstacles = new List<Vector2>();
        private List<Vector2> obstacleSizes = new List<Vector2>();
        private float obstacleSpawnTimer = 0f;
        private float obstacleSpawnDelay = 1f;

        private Vector2 portalPosition;
        private float portalWidth = 80f;
        private float portalHeight = 80f;

        private float scrollSpeed = 2f;
        private float levelLength = 2000f;
        private float levelProgress = 0f;

        private bool menuActive = false;
        private bool gameFrozen = false; // zastavení po portálu

        private List<Vector2> stars = new List<Vector2>();
        private int starCount = 200;
        private Random rand = new Random();

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

            snakePosition = new Vector2(100, 300);
            for (int i = 0; i < segmentCount; i++)
                snakeSegments.Add(snakePosition - new Vector2(i * segmentSpacing, 0));

            // hvězdy
            for (int i = 0; i < starCount; i++)
                stars.Add(new Vector2(rand.Next(0, 800), rand.Next(0, 600)));

            // portál na konci levelu
            portalPosition = new Vector2(levelLength, rand.Next(100, 500));

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
            portalTexture.SetData(new[] { Color.Cyan });

            starTexture = new Texture2D(GraphicsDevice, 1, 1);
            starTexture.SetData(new[] { Color.White });
        }

        protected override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            var kstate = Keyboard.GetState();

            if (!gameFrozen)
            {
                // pohyb jen nahoru/dolů
                snakeVelocity = Vector2.Zero;
                if (kstate.IsKeyDown(Keys.Up)) snakeVelocity.Y = -200f;
                if (kstate.IsKeyDown(Keys.Down)) snakeVelocity.Y = 200f;

                snakePosition += snakeVelocity * dt;

                snakePosition.X = MathHelper.Clamp(snakePosition.X, 0, _graphics.PreferredBackBufferWidth - segmentSpacing);
                snakePosition.Y = MathHelper.Clamp(snakePosition.Y, 0, _graphics.PreferredBackBufferHeight - segmentSpacing);

                // pohyb segmentů hada
                for (int i = 0; i < snakeSegments.Count; i++)
                {
                    if (i == 0) snakeSegments[i] = snakePosition;
                    else
                    {
                        Vector2 dir = snakeSegments[i - 1] - snakeSegments[i];
                        if (dir.Length() > segmentSpacing)
                            snakeSegments[i] += dir * 0.2f;
                    }
                }

                // scroll levelu
                float horizontalMovement = scrollSpeed * dt * 60;
                levelProgress += horizontalMovement;

                // generování překážek jen před portálem
                if (levelProgress < levelLength - portalWidth)
                {
                    obstacleSpawnTimer += dt;
                    if (obstacleSpawnTimer >= obstacleSpawnDelay)
                    {
                        obstacleSpawnTimer = 0f;
                        float y = rand.Next(50, 550);
                        float size = rand.Next(20, 50);
                        obstacles.Add(new Vector2(800, y));
                        obstacleSizes.Add(new Vector2(size, size));
                    }
                }

                // posun překážek
                for (int i = 0; i < obstacles.Count; i++)
                    obstacles[i] -= new Vector2(scrollSpeed, 0);

                // odstranění překážek mimo obrazovku
                for (int i = obstacles.Count - 1; i >= 0; i--)
                {
                    if (obstacles[i].X + obstacleSizes[i].X < 0)
                    {
                        obstacles.RemoveAt(i);
                        obstacleSizes.RemoveAt(i);
                    }
                }

                // kolize s překážkami
                Rectangle headRect = new Rectangle((int)snakeSegments[0].X, (int)snakeSegments[0].Y, 20, 20);
                for (int i = 0; i < obstacles.Count; i++)
                {
                    Rectangle obsRect = new Rectangle((int)obstacles[i].X, (int)obstacles[i].Y, (int)obstacleSizes[i].X, (int)obstacleSizes[i].Y);
                    if (headRect.Intersects(obsRect))
                    {
                        ResetLevel();
                        return;
                    }
                }

                // kolize s portálem
                Rectangle portalRect = new Rectangle((int)portalPosition.X - (int)levelProgress, (int)portalPosition.Y, (int)portalWidth, (int)portalHeight);
                if (headRect.Intersects(portalRect))
                {
                    gameFrozen = true;  // ZASTAVIT HRU
                    menuActive = true;
                }
            }
            else if (menuActive)
            {
                // menu logika
                if (kstate.IsKeyDown(Keys.Enter))
                {
                    ResetLevel();
                    menuActive = false;
                    gameFrozen = false;
                }
                else if (kstate.IsKeyDown(Keys.Escape))
                {
                    Exit();
                }
            }

            base.Update(gameTime);
        }

        private void ResetLevel()
        {
            snakePosition = new Vector2(100, 300);
            snakeSegments.Clear();
            for (int i = 0; i < segmentCount; i++)
                snakeSegments.Add(snakePosition - new Vector2(i * segmentSpacing, 0));

            obstacles.Clear();
            obstacleSizes.Clear();
            portalPosition = new Vector2(levelLength, rand.Next(100, 500));
            levelProgress = 0f;
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin();

            // hvězdy
            foreach (var star in stars)
                _spriteBatch.Draw(starTexture, new Rectangle((int)star.X, (int)star.Y, 2, 2), Color.White);

            // překážky
            for (int i = 0; i < obstacles.Count; i++)
                _spriteBatch.Draw(obstacleTexture, new Rectangle((int)obstacles[i].X, (int)obstacles[i].Y, (int)obstacleSizes[i].X, (int)obstacleSizes[i].Y), Color.OrangeRed);

            // portál
            int portalDrawX = (int)portalPosition.X - (int)levelProgress;
            _spriteBatch.Draw(portalTexture, new Rectangle(portalDrawX, (int)portalPosition.Y, (int)portalWidth, (int)portalHeight), Color.Cyan);

            // had (jen 2 segmenty)
            foreach (var seg in snakeSegments)
                _spriteBatch.Draw(snakeSegmentTexture, new Rectangle((int)seg.X, (int)seg.Y, 20, 20), Color.LimeGreen);

            // menu
            if (menuActive)
            {
                SpriteFont font = Content.Load<SpriteFont>("MenuFont"); // musíš mít SpriteFont
                _spriteBatch.DrawString(font, "Level dokončen!", new Vector2(200, 200), Color.White);
                _spriteBatch.DrawString(font, "Enter = pokračovat, Esc = konec", new Vector2(200, 250), Color.White);
            }

            _spriteBatch.End();
            base.Draw(gameTime);
        }
    }
}
