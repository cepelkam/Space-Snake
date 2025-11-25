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
        private float minObstacleDistance = 150f; // minimální vzdálenost mezi překážkami

        private Vector2 portalPosition;
        private float portalWidth = 80f;
        private float portalHeight = 80f;
        private bool portalActive = false;
        private float timeSinceStart = 0f;
        private float portalAppearTime = 25f; // po 25 sekundách se objeví portál

        private float scrollSpeed = 1.5f; // pomalejší scroll
        private float levelProgress = 0f;

        private bool menuActive = false;
        private bool gameFrozen = true; // začátek hry zastavený
        private bool startCountdown = true;

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

            // spawn překážek hned na začátku
            SpawnInitialObstacles();

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

        private void SpawnInitialObstacles()
        {
            float x = 400; // start od středu
            while (x < 1600) // generujeme překážky dopředu
            {
                float y = rand.Next(50, 550);
                float size = rand.Next(30, 50);
                obstacles.Add(new Vector2(x, y));
                obstacleSizes.Add(new Vector2(size, size));
                x += minObstacleDistance + rand.Next(50, 100); // rozdíl mezi překážkami
            }
        }

        protected override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            var kstate = Keyboard.GetState();

            if (startCountdown)
            {
                // čekáme, až hráč stiskne Enter
                if (kstate.IsKeyDown(Keys.Enter))
                {
                    startCountdown = false;
                    gameFrozen = false;
                    timeSinceStart = 0f;
                }
                return; // nic jiného se zatím nepohybuje
            }

            if (!gameFrozen)
            {
                timeSinceStart += dt;

                // pohyb jen nahoru/dolů
                snakeVelocity = Vector2.Zero;
                if (kstate.IsKeyDown(Keys.Up)) snakeVelocity.Y = -200f;
                if (kstate.IsKeyDown(Keys.Down)) snakeVelocity.Y = 200f;

                snakePosition += snakeVelocity * dt;
                snakePosition.X = MathHelper.Clamp(snakePosition.X, 0, _graphics.PreferredBackBufferWidth - segmentSpacing);
                snakePosition.Y = MathHelper.Clamp(snakePosition.Y, 0, _graphics.PreferredBackBufferHeight - segmentSpacing);

                // posun segmentů hada
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

                // posun levelu
                if (!portalActive)
                    levelProgress += scrollSpeed;

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

                // Aktivace portálu po určité době
                if (!portalActive && timeSinceStart >= portalAppearTime)
                {
                    portalActive = true;
                    portalPosition = new Vector2(_graphics.PreferredBackBufferWidth / 2 - portalWidth / 2,
                                                 rand.Next(100, 500));
                }

                // kolize s portálem
                if (portalActive)
                {
                    Rectangle portalRect = new Rectangle((int)portalPosition.X, (int)portalPosition.Y, (int)portalWidth, (int)portalHeight);
                    if (headRect.Intersects(portalRect))
                    {
                        gameFrozen = true;
                        menuActive = true;
                    }
                }

                // dynamické hvězdy
                UpdateStars();
            }
            else if (menuActive)
            {
                if (kstate.IsKeyDown(Keys.Enter))
                {
                    ResetLevel();
                    menuActive = false;
                    startCountdown = true; // nový level čeká na start
                }
                else if (kstate.IsKeyDown(Keys.Escape))
                {
                    Exit();
                }
            }

            base.Update(gameTime);
        }

        private void UpdateStars()
        {
            for (int i = 0; i < stars.Count; i++)
            {
                stars[i] -= new Vector2(scrollSpeed, 0);

                if (stars[i].X < 0)
                    stars[i] = new Vector2(800 + rand.Next(0, 200), rand.Next(0, 600));
            }
        }

        private void ResetLevel()
        {
            snakePosition = new Vector2(100, 300);
            snakeSegments.Clear();
            for (int i = 0; i < segmentCount; i++)
                snakeSegments.Add(snakePosition - new Vector2(i * segmentSpacing, 0));

            obstacles.Clear();
            obstacleSizes.Clear();
            SpawnInitialObstacles();

            portalActive = false;
            timeSinceStart = 0f;
            levelProgress = 0f;
            gameFrozen = true;
            startCountdown = true;
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin();

            // hvězdy celou dobu
            foreach (var star in stars)
                _spriteBatch.Draw(starTexture, new Rectangle((int)star.X, (int)star.Y, 2, 2), Color.White);

            // překážky
            for (int i = 0; i < obstacles.Count; i++)
                _spriteBatch.Draw(obstacleTexture,
                    new Rectangle((int)obstacles[i].X - (int)levelProgress, (int)obstacles[i].Y,
                                  (int)obstacleSizes[i].X, (int)obstacleSizes[i].Y), Color.OrangeRed);

            // portál
            if (portalActive)
                _spriteBatch.Draw(portalTexture,
                    new Rectangle((int)portalPosition.X, (int)portalPosition.Y, (int)portalWidth, (int)portalHeight), Color.Cyan);

            // had (2 segmenty)
            foreach (var seg in snakeSegments)
                _spriteBatch.Draw(snakeSegmentTexture, new Rectangle((int)seg.X, (int)seg.Y, 20, 20), Color.LimeGreen);

            // start countdown
            if (startCountdown)
            {
                SpriteFont font = Content.Load<SpriteFont>("MenuFont");
                _spriteBatch.DrawString(font, "Stiskni ENTER pro start", new Vector2(200, 250), Color.White);
            }

            // menu po portálu
            if (menuActive)
            {
                SpriteFont font = Content.Load<SpriteFont>("MenuFont");
                _spriteBatch.DrawString(font, "Level dokončen!", new Vector2(200, 200), Color.White);
                _spriteBatch.DrawString(font, "Enter = pokračovat, Esc = konec", new Vector2(200, 250), Color.White);
            }

            _spriteBatch.End();
            base.Draw(gameTime);
        }
    }
}
