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
        private int segmentCount = 5;
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
        private float levelLength = 2000f; // délka levelu
        private float levelProgress = 0f;  // jak daleko hráč je ve levelu

        private bool levelCompleted = false;
        private bool menuActive = false;

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

            if (!menuActive)
            {
                // Pohyb hada/loď volný
                snakeVelocity = Vector2.Zero;
                if (kstate.IsKeyDown(Keys.Up)) snakeVelocity.Y = -200f;
                if (kstate.IsKeyDown(Keys.Down)) snakeVelocity.Y = 200f;
                if (kstate.IsKeyDown(Keys.Left)) snakeVelocity.X = -200f;
                if (kstate.IsKeyDown(Keys.Right)) snakeVelocity.X = 200f;

                snakePosition += snakeVelocity * dt;

                // Omezení obrazovky
                snakePosition.X = MathHelper.Clamp(snakePosition.X, 0, _graphics.PreferredBackBufferWidth - segmentSpacing);
                snakePosition.Y = MathHelper.Clamp(snakePosition.Y, 0, _graphics.PreferredBackBufferHeight - segmentSpacing);

                // Posouvání segmentů
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

                // Posun levelu (scroll)
                float horizontalMovement = scrollSpeed * dt * 60; // jednoduchý scroll efekt
                levelProgress += horizontalMovement;

                // Generování překážek jen před portálem
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

                // Posun překážek
                for (int i = 0; i < obstacles.Count; i++)
                    obstacles[i] -= new Vector2(scrollSpeed, 0);

                // Odstranit překážky mimo obrazovku
                for (int i = obstacles.Count - 1; i >= 0; i--)
                {
                    if (obstacles[i].X + obstacleSizes[i].X < 0)
                    {
                        obstacles.RemoveAt(i);
                        obstacleSizes.RemoveAt(i);
                    }
                }

                // Kolize s překážkami
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

                // Kolize s portálem
                Rectangle portalRect = new Rectangle((int)portalPosition.X - (int)levelProgress + 0, (int)portalPosition.Y, (int)portalWidth, (int)portalHeight);
                if (headRect.Intersects(portalRect))
                {
                    menuActive = true; // level dokončen, menu aktivní
                }
            }
            else
            {
                // Menu logika
                if (kstate.IsKeyDown(Keys.Enter))
                {
                    // Pokračovat do dalšího levelu
                    ResetLevel();
                    menuActive = false;
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

            levelProgress = 0f;
            portalPosition = new Vector2(levelLength, rand.Next(100, 500));
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

            // portál na konci
            if (!menuActive)
            {
                int portalDrawX = (int)portalPosition.X - (int)levelProgress;
                _spriteBatch.Draw(portalTexture, new Rectangle(portalDrawX, (int)portalPosition.Y, (int)portalWidth, (int)portalHeight), Color.Cyan);
            }

            // had
            foreach (var seg in snakeSegments)
                _spriteBatch.Draw(snakeSegmentTexture, new Rectangle((int)seg.X, (int)seg.Y, 20, 20), Color.LimeGreen);

            // Menu placeholder
            if (menuActive)
            {
                // Text můžeme doplnit později
            }

            _spriteBatch.End();
            base.Draw(gameTime);
        }
    }
}
