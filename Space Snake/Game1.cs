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

        private Vector2 snakePosition;
        private List<Vector2> snakeSegments = new List<Vector2>();
        private int segmentCount = 3; // 3 segmenty
        private float segmentSpacing = 20f;

        private List<Vector2> obstacles = new List<Vector2>();
        private List<Vector2> obstacleSizes = new List<Vector2>();
        private float minObstacleDistance = 80f; // menší mezera = více překážek

        private Vector2 portalPosition;
        private float portalWidth = 50f;
        private float portalHeight = 50f;
        private bool portalActive = false;

        private float scrollSpeed = 3f;
        private float snakeSpeed = 300f; // rychlost hada
        private float levelProgress = 0f;

        private int level = 1;
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

            ResetLevel();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            snakeSegmentTexture = new Texture2D(GraphicsDevice, 1, 1);
            snakeSegmentTexture.SetData(new[] { Color.LimeGreen });

            obstacleTexture = new Texture2D(GraphicsDevice, 1, 1);
            obstacleTexture.SetData(new[] { Color.Red });

            portalTexture = new Texture2D(GraphicsDevice, 1, 1);
            portalTexture.SetData(new[] { Color.Cyan });

            starTexture = new Texture2D(GraphicsDevice, 1, 1);
            starTexture.SetData(new[] { Color.White });
        }

        private void GenerateObstacles()
        {
            obstacles.Clear();
            obstacleSizes.Clear();

            float x = 400;
            int obstacleCount = 15 + level * 3; // hodně překážek
            for (int i = 0; i < obstacleCount; i++)
            {
                float y = rand.Next(50, 550);
                float size = rand.Next(25, 50);
                obstacles.Add(new Vector2(x, y));
                obstacleSizes.Add(new Vector2(size, size));
                x += minObstacleDistance + rand.Next(20, 60);
            }

            portalPosition = new Vector2(x + 200, rand.Next(100, 500));
            portalActive = false;
        }

        protected override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            var kstate = Keyboard.GetState();

            // pohyb nahoru/dolů
            Vector2 velocity = Vector2.Zero;
            if (kstate.IsKeyDown(Keys.Up)) velocity.Y = -snakeSpeed;
            if (kstate.IsKeyDown(Keys.Down)) velocity.Y = snakeSpeed;

            snakePosition += velocity * dt;
            snakePosition.Y = MathHelper.Clamp(snakePosition.Y, 0, _graphics.PreferredBackBufferHeight - 20);

            // posun segmentů hada
            for (int i = 0; i < snakeSegments.Count; i++)
            {
                if (i == 0) snakeSegments[i] = snakePosition;
                else
                {
                    Vector2 dir = snakeSegments[i - 1] - snakeSegments[i];
                    if (dir.Length() > segmentSpacing)
                        snakeSegments[i] += 0.3f * dir;
                }
            }

            // posun překážek a portálu
            for (int i = 0; i < obstacles.Count; i++)
                obstacles[i] -= new Vector2(scrollSpeed, 0);

            portalPosition.X -= scrollSpeed;

            levelProgress += scrollSpeed;

            // kolize s překážkami
            Rectangle headRect = new Rectangle((int)snakeSegments[0].X, (int)snakeSegments[0].Y, 20, 20);
            for (int i = 0; i < obstacles.Count; i++)
            {
                Rectangle obsRect = new Rectangle((int)obstacles[i].X, (int)obstacles[i].Y,
                    (int)obstacleSizes[i].X, (int)obstacleSizes[i].Y);
                if (headRect.Intersects(obsRect))
                {
                    ResetLevel();
                    return;
                }
            }

            // aktivace portálu
            if (!portalActive && portalPosition.X < _graphics.PreferredBackBufferWidth - 100)
                portalActive = true;

            // kolize s portálem → nový level
            if (portalActive)
            {
                Rectangle portalRect = new Rectangle((int)portalPosition.X, (int)portalPosition.Y, (int)portalWidth, (int)portalHeight);
                if (headRect.Intersects(portalRect))
                {
                    level++;
                    scrollSpeed += 0.5f;
                    snakeSpeed += 20f;
                    ResetLevel();
                    return;
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

            levelProgress = 0f;
            GenerateObstacles();
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin();

            // hvězdy
            for (int i = 0; i < 200; i++)
                _spriteBatch.Draw(starTexture, new Rectangle(rand.Next(0, 800), rand.Next(0, 600), 2, 2), Color.White);

            // překážky
            for (int i = 0; i < obstacles.Count; i++)
                _spriteBatch.Draw(obstacleTexture, new Rectangle((int)obstacles[i].X, (int)obstacles[i].Y,
                    (int)obstacleSizes[i].X, (int)obstacleSizes[i].Y), Color.Red);

            // portál
            if (portalActive)
                _spriteBatch.Draw(portalTexture, new Rectangle((int)portalPosition.X, (int)portalPosition.Y,
                    (int)portalWidth, (int)portalHeight), Color.Cyan);

            // had
            foreach (var seg in snakeSegments)
                _spriteBatch.Draw(snakeSegmentTexture, new Rectangle((int)seg.X, (int)seg.Y, 20, 20), Color.LimeGreen);

            _spriteBatch.End();
            base.Draw(gameTime);
        }
    }
}
