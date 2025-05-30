using System;
using System.Windows.Forms;
using System.Drawing;
using System.Threading.Tasks;

namespace RPG_Game
{
    public partial class Form1 : Form
    {
        // Animation state
        private int player1Pose = 0;
        private int player2Pose = 0;
        private int player1Offset = 0;
        private int player2Offset = 0;
        private int player1Dir = 1;
        private int player2Dir = -1;
        private Random moveRng = new Random();
        private bool animateAttack = false;
        private bool player1Attacking = true;
        private Random blockRng = new Random();
        private string[] trashtalkPhrases = new string[]
        {
            "Bumawas ka naman",
            "Parang kagat lang ng lamok",
            "Anong Gentle-gentle!",
            "Mahina nilalang!",
            "No balls!",
            "Yun na 'yon?!",
            "Ang dumi-dumi mo!",
            "Bading!",
            "Mama mo blue",
            "Di ka mahal ng mama mo",
            "Katapusan mo na"
        };
        // Dialogue state for drawing above heads
        private string player1Dialogue = "";
        private string player2Dialogue = "";
        private DateTime player1DialogueEndTime;
        private DateTime player2DialogueEndTime;
        private readonly TimeSpan dialogueDuration = TimeSpan.FromSeconds(2);
        // Store last health for drawing health bars
        private int lastPlayer1Health = 100, lastPlayer1MaxHealth = 100;
        private int lastPlayer2Health = 100, lastPlayer2MaxHealth = 100;
        // Blood effect state
        private bool player1HitEffect = false;
        private DateTime player1HitEffectEndTime;
        private bool player2HitEffect = false;
        private DateTime player2HitEffectEndTime;
        private readonly TimeSpan hitEffectDuration = TimeSpan.FromMilliseconds(500);
        private Random effectRng = new Random();
        // Dead state
        private bool player1Dead = false;
        private bool player2Dead = false;
        // Blocking state
        private bool player1Blocking = false;
        private bool player2Blocking = false;
        private readonly TimeSpan blockDuration = TimeSpan.FromMilliseconds(700);
        private DateTime player1BlockEndTime;
        private DateTime player2BlockEndTime;
        // Skill UI state
        private bool player1SkillReady = true;
        private bool player2SkillReady = true;
        private string player1SkillText = "Skill Ready";
        private string player2SkillText = "Skill Ready";
        // Store last mana for drawing mana bars
        private int lastPlayer1Mana = 100, lastPlayer1MaxMana = 100;
        private int lastPlayer2Mana = 100, lastPlayer2MaxMana = 100;
        // Critical hit flash effect
        private bool player1CriticalFlash = false;
        private bool player2CriticalFlash = false;
        private DateTime player1CriticalEndTime;
        private DateTime player2CriticalEndTime;
        private readonly TimeSpan criticalFlashDuration = TimeSpan.FromMilliseconds(300);
        private int criticalFlashAlpha = 255;
        private int criticalFlashSize = 0;
        // Sword wave effect state
        private bool player1SwordWaveActive = false;
        private bool player2SwordWaveActive = false;
        private DateTime player1SwordWaveEndTime;
        private DateTime player2SwordWaveEndTime;
        private readonly TimeSpan swordWaveDuration = TimeSpan.FromMilliseconds(2500);
        private float player1SwordWaveProgress = 0;
        private float player2SwordWaveProgress = 0;
        // Store positions at the start of skill animation
        private Point player1SkillStartPos;
        private Point player2SkillStartPos;
        private Point player1SkillTargetPos;
        private Point player2SkillTargetPos;
        // Timer for animation updates
        private System.Windows.Forms.Timer animationTimer;

        public Form1()
        {
            InitializeComponent();
            
            // Enable double buffering for the form
            this.DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.UserPaint, true);
            
            // Enable double buffering for the panel
            typeof(Panel).InvokeMember("DoubleBuffered", 
                System.Reflection.BindingFlags.SetProperty | 
                System.Reflection.BindingFlags.Instance | 
                System.Reflection.BindingFlags.NonPublic,
                null, pnlArena, new object[] { true });

            // Set panel styles for smoother rendering
            pnlArena.GetType().GetMethod("SetStyle", 
                System.Reflection.BindingFlags.Instance | 
                System.Reflection.BindingFlags.NonPublic)
                .Invoke(pnlArena, new object[] { 
                    ControlStyles.UserPaint | 
                    ControlStyles.AllPaintingInWmPaint | 
                    ControlStyles.OptimizedDoubleBuffer, true });
            
            // Populate character selection ComboBoxes
            cmbPlayer1Class.Items.AddRange(new string[] { "Bida-bida", "Pabibo" });
            cmbPlayer2Class.Items.AddRange(new string[] { "Bida-bida", "Pabibo" });
            cmbPlayer1Class.SelectedIndex = 0;
            cmbPlayer2Class.SelectedIndex = 1;
            btnStartBattle.Click += BtnStartBattle_Click;
            pnlArena.Paint += PnlArena_Paint;
            // Initialize and start the animation timer
            animationTimer = new System.Windows.Forms.Timer();
            animationTimer.Interval = 16; // ~60 FPS (1000ms / 60 = 16.67ms)
            animationTimer.Tick += AnimationTimer_Tick;
            animationTimer.Start();
        }

        // Timer tick event to invalidate the panel for animation
        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            // Suspend layout while updating
            pnlArena.SuspendLayout();

            try
            {
                // Update animation states
                if (player1SwordWaveActive && DateTime.Now >= player1SwordWaveEndTime)
                {
                    player1SwordWaveActive = false;
                }
                if (player2SwordWaveActive && DateTime.Now >= player2SwordWaveEndTime)
                {
                    player2SwordWaveActive = false;
                }
                if (player1Blocking && DateTime.Now >= player1BlockEndTime)
                {
                    player1Blocking = false;
                }
                if (player2Blocking && DateTime.Now >= player2BlockEndTime)
                {
                    player2Blocking = false;
                }

                // Force immediate redraw
                pnlArena.Refresh();
            }
            finally
            {
                // Resume layout
                pnlArena.ResumeLayout();
            }
        }

        // Draw stickmen and swords
        private void PnlArena_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            // Player 1 (left, full body, with offset, farther apart)
            int p1x = 130 + player1Offset, p1y = 160;
            int p2x = 570 + player2Offset, p2y = 160;
            DrawCoolStickman(g, p1x, p1y, player1Pose, true, player1Dead, player1Blocking);
            DrawCoolStickman(g, p2x, p2y, player2Pose, false, player2Dead, player2Blocking);
            // Draw health bars above heads
            if (!player1Dead) DrawHealthBar(g, p1x, p1y - 100, lastPlayer1Health, lastPlayer1MaxHealth, true);
            if (!player2Dead) DrawHealthBar(g, p2x, p2y - 100, lastPlayer2Health, lastPlayer2MaxHealth, false);
            // Draw mana bars below health bars
            if (!player1Dead) DrawManaBar(g, p1x, p1y - 85, lastPlayer1Mana, lastPlayer1MaxMana, true);
            if (!player2Dead) DrawManaBar(g, p2x, p2y - 85, lastPlayer2Mana, lastPlayer2MaxMana, false);
            // Draw dialogue above heads if active
            if (DateTime.Now < player1DialogueEndTime)
            {
                DrawDialogue(g, p1x, p1y - 140, player1Dialogue);
            }
            if (DateTime.Now < player2DialogueEndTime)
            {
                DrawDialogue(g, p2x, p2y - 140, player2Dialogue);
            }
            // Draw blood effect if active
            if (DateTime.Now < player1HitEffectEndTime)
            {
                DrawBloodEffect(g, p1x, p1y + 20); // Draw around body/legs
            }
            if (DateTime.Now < player2HitEffectEndTime)
            {
                DrawBloodEffect(g, p2x, p2y + 20); // Draw around body/legs
            }
            // Draw skill status text
            if (!player1Dead) DrawSkillStatus(g, p1x, p1y + 140, player1SkillText);
            if (!player2Dead) DrawSkillStatus(g, p2x, p2y + 140, player2SkillText);
            // Reset blocking state after duration
            if (player1Blocking && DateTime.Now >= player1BlockEndTime)
            {
                player1Blocking = false;
                pnlArena.Invalidate();
            }
            if (player2Blocking && DateTime.Now >= player2BlockEndTime)
            {
                player2Blocking = false;
                pnlArena.Invalidate();
            }
            // Draw critical hit flash if active
            if (DateTime.Now < player1CriticalEndTime)
            {
                DrawCriticalFlash(g, p1x, p1y);
            }
            if (DateTime.Now < player2CriticalEndTime)
            {
                DrawCriticalFlash(g, p2x, p2y);
            }
            // Draw sword wave if active
            if (player1SwordWaveActive && DateTime.Now < player1SwordWaveEndTime)
            {
                // Calculate elapsed time from the animation start
                TimeSpan elapsed = DateTime.Now - (player1SwordWaveEndTime - swordWaveDuration);
                player1SwordWaveProgress = (float)elapsed.TotalMilliseconds / (float)swordWaveDuration.TotalMilliseconds;
                // Ensure progress is between 0 and 1
                player1SwordWaveProgress = Math.Max(0, Math.Min(1, player1SwordWaveProgress));

                // Draw using stored start and target positions
                DrawSwordWave(g, player1SkillStartPos.X, player1SkillStartPos.Y, player1SkillTargetPos.X, player1SkillTargetPos.Y, true, player1SwordWaveProgress);
            } else if (player1SwordWaveActive && DateTime.Now >= player1SwordWaveEndTime)
            {
                player1SwordWaveActive = false;
            }

            if (player2SwordWaveActive && DateTime.Now < player2SwordWaveEndTime)
            {
                // Calculate elapsed time from the animation start
                TimeSpan elapsed = DateTime.Now - (player2SwordWaveEndTime - swordWaveDuration);
                player2SwordWaveProgress = (float)elapsed.TotalMilliseconds / (float)swordWaveDuration.TotalMilliseconds;
                 // Ensure progress is between 0 and 1
                player2SwordWaveProgress = Math.Max(0, Math.Min(1, player2SwordWaveProgress));

                // Draw using stored start and target positions
                DrawSwordWave(g, player2SkillStartPos.X, player2SkillStartPos.Y, player2SkillTargetPos.X, player2SkillTargetPos.Y, false, player2SwordWaveProgress);
            } else if (player2SwordWaveActive && DateTime.Now >= player2SwordWaveEndTime)
            {
                player2SwordWaveActive = false;
            }
        }

        // Draw a cooler stickman with headband, sword glow, handle, dead pose, and blocking pose
        private void DrawCoolStickman(Graphics g, int x, int y, int pose, bool left, bool dead, bool blocking)
        {
            if (dead)
            {
                // Draw stickman lying down
                Pen bodyPenDead = new Pen(Color.Gray, 5);
                Pen limbPenDead = new Pen(Color.DarkGray, 7);
                // Body (lying down)
                g.DrawLine(bodyPenDead, x - 40, y + 60, x + 40, y + 60);
                // Head (on the ground)
                g.FillEllipse(Brushes.DarkGray, x - 50, y + 45, 30, 30);
                g.DrawEllipse(bodyPenDead, x - 50, y + 45, 30, 30);
                // Limbs (collapsed)
                g.DrawLine(limbPenDead, x - 10, y + 60, x - 30, y + 80);
                g.DrawLine(limbPenDead, x + 10, y + 60, x + 30, y + 80);
                // Legs (collapsed, with feet)
                Point legL1Dead = new Point(x - 40, y + 60), legL2Dead = new Point(x - 60, y + 90);
                Point legR1Dead = new Point(x + 40, y + 60), legR2Dead = new Point(x + 60, y + 90);
                g.DrawLine(limbPenDead, legL1Dead, legL2Dead);
                g.DrawLine(limbPenDead, legR1Dead, legR2Dead);
                // Feet (collapsed)
                g.FillRectangle(Brushes.DarkGray, legL2Dead.X - 10, legL2Dead.Y - 5, 15, 10);
                g.FillRectangle(Brushes.DarkGray, legR2Dead.X - 5, legR2Dead.Y - 5, 15, 10);
                // No weapons or effects when dead
                return;
            }

            // Drawing for living stickman
            Pen bodyPenLiving = new Pen(Color.WhiteSmoke, 5);
            Pen limbPenLiving = new Pen(Color.LightGray, 7);
            Pen swordPen = new Pen(Color.Lime, 7); // Green blade
            Pen swordGlow = new Pen(Color.FromArgb(120, 50, 255, 50), 22); // Bright green glow
            Pen handlePen = new Pen(Color.Gray, 14); // Grey handle
            Pen handleOutline = new Pen(Color.Black, 18); // Black outline for handle
            Pen shieldPen = new Pen(Color.Silver, 6);
            Brush shieldBrush = new SolidBrush(Color.FromArgb(180, 180, 180));
            Pen axeHandlePen = new Pen(Color.SaddleBrown, 12);
            Pen axeBladePen = new Pen(Color.Gray, 14);
            // Head
            g.FillEllipse(Brushes.White, x - 18, y - 63, 36, 36);
            g.DrawEllipse(bodyPenLiving, x - 18, y - 63, 36, 36);
            // Headband
            Rectangle headbandRect = new Rectangle(x - 18, y - 55, 36, 10);
            g.FillRectangle(left ? Brushes.Blue : Brushes.Red, headbandRect);
            // Body
            g.DrawLine(bodyPenLiving, x, y - 30, x, y + 60);
            // Arms
            Point armL1 = new Point(x, y - 10), armR1 = new Point(x, y - 10);
            Point armL2, armR2;

            if (blocking)
            {
                // Blocking pose: both arms forward, shield prominent, weapon hidden/lowered
                if (left) // Player 1 blocking
                {
                    armL2 = new Point(x - 10, y + 10); // Shield arm forward
                    armR2 = new Point(x + 10, y + 30); // Sword arm lowered
                }
                else // Player 2 blocking
                {
                    armL2 = new Point(x - 10, y + 30); // Sword arm lowered
                    armR2 = new Point(x + 10, y + 10); // Shield arm forward
                }
                 // Draw arms for blocking pose
                g.DrawLine(limbPenLiving, armL1, armL2);
                g.DrawLine(limbPenLiving, armR1, armR2);

                // Draw shield in front
                if (left) // Player 1 blocking (shield in left hand)
                {
                    int shieldX = armL2.X + 30, shieldY = armL2.Y - 20; // Position in front of body
                    g.FillEllipse(shieldBrush, shieldX - 25, shieldY - 25, 50, 50);
                    g.DrawEllipse(shieldPen, shieldX - 25, shieldY - 25, 50, 50);
                }
                else // Player 2 blocking (shield in right hand)
                {
                    int shieldX = armR2.X - 30, shieldY = armR2.Y - 20; // Position in front of body
                    g.FillEllipse(shieldBrush, shieldX - 25, shieldY - 25, 50, 50);
                    g.DrawEllipse(shieldPen, shieldX - 25, shieldY - 25, 50, 50);
                }
                // Weapons are not drawn in the blocking pose
            }
            else
            {
                // Normal pose: based on weapon/shield and attack animation
                 // Default arm positions
                Point armL1_normal = new Point(x, y - 10), armR1_normal = new Point(x, y - 10);
                Point armL2_normal, armR2_normal;

                if (left) // Player 1: sword (right), shield (left)
                {
                    armL2_normal = new Point(x - 38, y + 35); // shield arm
                    armR2_normal = new Point(x + 30, y + 20); // sword arm
                }
                else // Player 2: sword (left), shield (right)
                {
                    armL2_normal = new Point(x - 30, y + 20); // sword arm
                    armR2_normal = new Point(x + 38, y + 35); // shield arm
                }
                 // Draw arms for normal pose
                g.DrawLine(limbPenLiving, armL1_normal, armL2_normal);
                g.DrawLine(limbPenLiving, armR1_normal, armR2_normal);

                // Draw shield (normal position)
                if (left) // Player 1: shield in left hand
                {
                    int shieldX = armL2_normal.X, shieldY = armL2_normal.Y;
                    g.FillEllipse(shieldBrush, shieldX - 18, shieldY - 18, 36, 36);
                    g.DrawEllipse(shieldPen, shieldX - 18, shieldY - 18, 36, 36);
                }
                else // Player 2: shield in right hand
                {
                    int shieldX = armR2_normal.X, shieldY = armR2_normal.Y;
                    g.FillEllipse(shieldBrush, shieldX - 18, shieldY - 18, 36, 36);
                    g.DrawEllipse(shieldPen, shieldX - 18, shieldY - 18, 36, 36);
                }
                 // Draw weapon (normal position based on attack pose)
                if (left) // Player 1: sword in right hand
                {
                    // Sword arm (right)
                    int swordX1 = armR2_normal.X;
                    int swordY1 = armR2_normal.Y;
                    int swordX2, swordY2;
                    if (pose == 0) { swordX2 = x + 130; swordY2 = y - 20; }
                    else if (pose == 1) { swordX2 = x + 120; swordY2 = y - 60; }
                    else { swordX2 = x + 110; swordY2 = y + 110; }
                    // Calculate handle
                    double dx = swordX2 - swordX1, dy = swordY2 - swordY1;
                    double len = Math.Sqrt(dx * dx + dy * dy);
                    double ux = dx / len, uy = dy / len;
                    int handleLength = 28;
                    int handleX2 = (int)(swordX1 + ux * handleLength);
                    int handleY2 = (int)(swordY1 + uy * handleLength);
                    g.DrawLine(handleOutline, swordX1, swordY1, handleX2, handleY2);
                    g.DrawLine(handlePen, swordX1, swordY1, handleX2, handleY2);
                    g.DrawLine(swordGlow, handleX2, handleY2, swordX2, swordY2);
                    g.DrawLine(swordPen, handleX2, handleY2, swordX2, swordY2);
                }
                else // Player 2: sword in left hand (purple blade)
                {
                    // Sword arm (left)
                    int swordX1 = armL2_normal.X;
                    int swordY1 = armL2_normal.Y;
                    int swordX2, swordY2;
                    if (pose == 0) { swordX2 = x - 130; swordY2 = y - 20; }
                    else if (pose == 1) { swordX2 = x - 120; swordY2 = y - 60; }
                    else { swordX2 = x - 110; swordY2 = y + 110; }
                    // Calculate handle
                    double dx = swordX2 - swordX1, dy = swordY2 - swordY1;
                    double len = Math.Sqrt(dx * dx + dy * dy);
                    double ux = dx / len, uy = dy / len;
                    int handleLength = 28;
                    int handleX2 = (int)(swordX1 + ux * handleLength);
                    int handleY2 = (int)(swordY1 + uy * handleLength);
                    Pen purpleSwordPen = new Pen(Color.MediumPurple, 7);
                    Pen purpleSwordGlow = new Pen(Color.FromArgb(120, 128, 0, 255), 22);
                    g.DrawLine(handleOutline, swordX1, swordY1, handleX2, handleY2);
                    g.DrawLine(handlePen, swordX1, swordY1, handleX2, handleY2);
                    g.DrawLine(purpleSwordGlow, handleX2, handleY2, swordX2, swordY2);
                    g.DrawLine(purpleSwordPen, handleX2, handleY2, swordX2, swordY2);
                }
            }
             // Legs (with feet) - Moved outside blocking check
            Point legL1Living = new Point(x, y + 60), legL2Living = new Point(x - 22, y + 110);
            Point legR1Living = new Point(x, y + 60), legR2Living = new Point(x + 22, y + 110);
            g.DrawLine(limbPenLiving, legL1Living, legL2Living);
            g.DrawLine(limbPenLiving, legR1Living, legR2Living);
            // Feet (living) - Moved outside blocking check
            g.FillRectangle(Brushes.LightGray, legL2Living.X - 10, legL2Living.Y - 5, 15, 10);
            g.FillRectangle(Brushes.LightGray, legR2Living.X - 5, legR2Living.Y - 5, 15, 10);
        }

        // Draw a health bar above the stickman's head
        private void DrawHealthBar(Graphics g, int x, int y, int health, int maxHealth, bool left)
        {
            int barWidth = 70, barHeight = 12;
            int barX = x - barWidth / 2, barY = y;
            float percent = Math.Max(0, Math.Min(1, (float)health / maxHealth));

            // Draw the background (empty portion) of the health bar in grey
            using (Brush bg = new SolidBrush(Color.FromArgb(60, 60, 60)))
            {
                g.FillRectangle(bg, barX, barY, barWidth, barHeight);
            }

            // Draw the filled portion of the health bar with a Green to Yellow gradient
            RectangleF fillRect = new RectangleF(barX, barY, barWidth * percent, barHeight);
            using (System.Drawing.Drawing2D.LinearGradientBrush fillBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
                new PointF(barX, barY),
                new PointF(barX + barWidth, barY), // Gradient across the full width
                Color.LimeGreen, // Start color (Green)
                Color.Yellow)) // End color (Yellow)
            {
                 // Draw the gradient fill, but only up to the current health percentage
                 g.FillRectangle(fillBrush, fillRect);
            }

            using (Pen border = new Pen(Color.White, 2))
                g.DrawRectangle(border, barX, barY, barWidth, barHeight);
            // Draw health text
            string text = $"{health}/{maxHealth}";
            using (Font font = new Font("Segoe UI", 9, FontStyle.Bold))
            using (Brush textBrush = new SolidBrush(Color.White))
            {
                SizeF sz = g.MeasureString(text, font);
                g.DrawString(text, font, textBrush, x - sz.Width / 2, barY - sz.Height - 2);
            }
        }

        // Draw a mana bar below the health bar
        private void DrawManaBar(Graphics g, int x, int y, int mana, int maxMana, bool left)
        {
            int barWidth = 70, barHeight = 8;
            int barX = x - barWidth / 2, barY = y;
            float percent = Math.Max(0, Math.Min(1, (float)mana / maxMana));
            Color fillColor = Color.FromArgb(0, 100, 255); // Blue mana color
            using (Brush bg = new SolidBrush(Color.FromArgb(60, 60, 60)))
                g.FillRectangle(bg, barX, barY, barWidth, barHeight);
            using (Brush fill = new SolidBrush(fillColor))
                g.FillRectangle(fill, barX, barY, (int)(barWidth * percent), barHeight);
            using (Pen border = new Pen(Color.White, 1))
                g.DrawRectangle(border, barX, barY, barWidth, barHeight);
        }

        // Draw a dialogue bubble above the stickman's head
        private void DrawDialogue(Graphics g, int x, int y, string text)
        {
            using (Font font = new Font("Segoe UI", 9, FontStyle.Bold))
            using (Brush textBrush = new SolidBrush(Color.White))
            using (StringFormat sf = new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                SizeF sz = g.MeasureString(text, font);
                int padding = 8;
                RectangleF rect = new RectangleF(x - sz.Width / 2 - padding, y - sz.Height / 2 - padding, sz.Width + padding * 2, sz.Height + padding * 2);
                // Draw bubble background
                using (Brush bubbleBrush = new SolidBrush(Color.FromArgb(180, 0, 0, 0))) // Semi-transparent black
                using (Pen bubblePen = new Pen(Color.White, 2))
                {
                    g.FillRectangle(bubbleBrush, rect);
                    g.DrawRectangle(bubblePen, rect.X, rect.Y, rect.Width, rect.Height);
                }
                // Draw text
                g.DrawString(text, font, textBrush, x, y, sf);
            }
        }

        // Draw a blood effect around the hit location
        private void DrawBloodEffect(Graphics g, int x, int y)
        {
            using (Brush bloodBrush = new SolidBrush(Color.Red))
            {
                // Draw several small random circles/splatters
                for (int i = 0; i < 8; i++)
                {
                    int offsetX = effectRng.Next(-20, 21);
                    int offsetY = effectRng.Next(-20, 21);
                    int size = effectRng.Next(4, 10);
                    g.FillEllipse(bloodBrush, x + offsetX, y + offsetY, size, size);
                }
            }
        }

        // Draw skill status text below the stickman
        private void DrawSkillStatus(Graphics g, int x, int y, string text)
        {
            using (Font font = new Font("Segoe UI", 8, FontStyle.Bold))
            using (Brush textBrush = new SolidBrush(Color.White))
            using (StringFormat sf = new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                 SizeF sz = g.MeasureString(text, font);
                 // Draw a subtle background for the text
                 RectangleF rect = new RectangleF(x - sz.Width / 2 - 5, y - sz.Height / 2 - 3, sz.Width + 10, sz.Height + 6);
                 using (Brush bg = new SolidBrush(Color.FromArgb(120, 0, 0, 0)))
                 {
                     g.FillRectangle(bg, rect);
                 }
                 g.DrawString(text, font, textBrush, x, y, sf);
            }
        }

        /// <summary>
        /// Handles the Start Battle button click event.
        /// Now animates stickman sword fighting with move-in, dialogue, hit effects, and skills.
        /// </summary>
        private async void BtnStartBattle_Click(object sender, EventArgs e)
        {
            try
            {
                // Validate input
                string name1 = txtPlayer1Name.Text.Trim();
                string name2 = txtPlayer2Name.Text.Trim();
                if (string.IsNullOrEmpty(name1) || string.IsNullOrEmpty(name2))
                    throw new ArgumentException("Both player names must be entered.");
                if (cmbPlayer1Class.SelectedIndex == -1 || cmbPlayer2Class.SelectedIndex == -1)
                    throw new ArgumentException("Both players must select a character class.");
                if (name1 == name2)
                    throw new ArgumentException("Player names must be different.");

                // Create characters with updated constructor
                ClassFighter fighter1 = CreateFighter(cmbPlayer1Class.SelectedItem.ToString(), name1);
                ClassFighter fighter2 = CreateFighter(cmbPlayer2Class.SelectedItem.ToString(), name2);

                // Reset UI and state
                lstBattleLog.Items.Clear();
                lblWinner.Text = "";
                UpdateHealthLabels(fighter1, fighter2); // Updates mana too
                player1Pose = 0; player2Pose = 0;
                player1Offset = 0; player2Offset = 0;
                player1Dir = 1; player2Dir = -1;
                player1Dialogue = ""; player2Dialogue = "";
                player1HitEffect = false; player2HitEffect = false;
                player1Dead = false; player2Dead = false;
                player1Blocking = false; player2Blocking = false;
                player1SkillReady = true; player2SkillReady = true;
                player1SkillText = "Skill Ready"; player2SkillText = "Skill Ready";
                pnlArena.Invalidate();
                await Task.Delay(400);

                // Turn-based battle loop with animation, movement, dialogue, hit effects, and skills
                bool player1Turn = true;
                int moveFrame = 0;
                while (fighter1.Health > 0 && fighter2.Health > 0)
                {
                    // Update skill status (cooldown)
                    fighter1.UpdateSkillStatus();
                    fighter2.UpdateSkillStatus();
                    UpdateSkillUI(fighter1, fighter2);

                    // Move both stickmen back and forth for a few frames before each attack
                    for (int i = 0; i < 5; i++)
                    {
                        player1Offset += player1Dir * moveRng.Next(2, 5);
                        player2Offset += player2Dir * moveRng.Next(2, 5);
                        // Reverse direction occasionally for a 'fighting style' effect
                        if (moveFrame % 3 == 0) player1Dir *= -1;
                        if (moveFrame % 4 == 0) player2Dir *= -1;
                        pnlArena.Invalidate();
                        await Task.Delay(60);
                        moveFrame++;
                    }
                    // Clamp offsets to keep stickmen in the arena
                    player1Offset = Math.Max(-30, Math.Min(30, player1Offset));
                    player2Offset = Math.Max(-30, Math.Min(30, player2Offset));

                    // Decide whether to use skill or normal attack (simple AI: use skill if ready and enough mana)
                    bool useSkill = false;
                    double skillChance = 0.25; // 25% chance to attempt skill
                    if (player1Turn)
                    {
                        if (fighter1.IsSkillReady && fighter1.Mana > 0 && blockRng.NextDouble() < skillChance) // Check for mana > 0 and random chance
                        {
                            useSkill = true;
                            // Store start and target positions for the skill animation
                            player1SkillStartPos = new Point(130 + player1Offset, 160); // Player 1 current position
                            player1SkillTargetPos = new Point(570 + player2Offset, 160); // Player 2 current position

                            await AnimateSwordSwing(true); // Use attack animation for skill
                            int skillVal = fighter1.UseSkill();
                            fighter2.TakeDamage(skillVal); // Apply skill damage/effect
                            lstBattleLog.Items.Add($"{fighter1.Name} uses skill, consuming all mana for {skillVal} damage!");
                            player2Dialogue = "Skill hit!"; // Skill hit dialogue
                            player2DialogueEndTime = DateTime.Now + dialogueDuration;
                            // Activate hit effect
                            player2HitEffect = true;
                            player2HitEffectEndTime = DateTime.Now + hitEffectDuration;
                            // Activate sword wave effect
                            player1SwordWaveActive = true;
                            player1SwordWaveEndTime = DateTime.Now + swordWaveDuration;

                            // The timer will handle invalidation and animation updates
                            await Task.Delay(swordWaveDuration); // Wait for the animation to finish before continuing battle logic
                            player1SwordWaveActive = false; // Ensure flag is reset after animation
                        }
                        else
                        {
                            await AnimateSwordSwing(true);
                            int dmg = fighter1.Attack();
                            if (blockRng.NextDouble() < 0.25)
                            {
                                lstBattleLog.Items.Add($"{fighter2.Name} blocks the attack!");
                                player2Dialogue = "Blocked!";
                                player2DialogueEndTime = DateTime.Now + dialogueDuration;
                                player2Blocking = true;
                                player2BlockEndTime = DateTime.Now + blockDuration;
                            }
                            else
                            {
                                fighter2.TakeDamage(dmg);
                                lstBattleLog.Items.Add($"{fighter1.Name} attacks {fighter2.Name} for {dmg} damage!");
                                player2Dialogue = GetRandomTrashtalk();
                                player2DialogueEndTime = DateTime.Now + dialogueDuration;
                                player2HitEffect = true;
                                player2HitEffectEndTime = DateTime.Now + hitEffectDuration;
                                fighter1.RecoverMana(5); // Recover mana on hit
                            }
                        }
                    }
                    else // Player 2's turn
                    {
                         if (fighter2.IsSkillReady && fighter2.Mana > 0 && blockRng.NextDouble() < skillChance) // Check for mana > 0 and random chance
                        {
                            useSkill = true;
                            // Store start and target positions for the skill animation
                            player2SkillStartPos = new Point(570 + player2Offset, 160); // Player 2 current position
                            player2SkillTargetPos = new Point(130 + player1Offset, 160); // Player 1 current position

                             await AnimateSwordSwing(false); // Use attack animation for skill
                            int skillVal = fighter2.UseSkill();
                             fighter1.TakeDamage(skillVal); // Apply skill damage/effect
                             lstBattleLog.Items.Add($"{fighter2.Name} uses skill, consuming all mana for {skillVal} damage!");
                             player1Dialogue = "Skill hit!"; // Skill hit dialogue
                             player1DialogueEndTime = DateTime.Now + dialogueDuration;
                             // Activate hit effect
                             player1HitEffect = true;
                             player1HitEffectEndTime = DateTime.Now + hitEffectDuration;
                            // Activate sword wave effect
                            player2SwordWaveActive = true;
                            player2SwordWaveEndTime = DateTime.Now + swordWaveDuration;

                            // The timer will handle invalidation and animation updates
                            await Task.Delay(swordWaveDuration); // Wait for the animation to finish before continuing battle logic
                            player2SwordWaveActive = false; // Ensure flag is reset after animation
                        }
                        else
                        {
                            await AnimateSwordSwing(false);
                            int dmg = fighter2.Attack();
                            if (blockRng.NextDouble() < 0.25)
                            {
                                lstBattleLog.Items.Add($"{fighter1.Name} blocks the attack!");
                                player1Dialogue = "Blocked!";
                                player1DialogueEndTime = DateTime.Now + dialogueDuration;
                                player1Blocking = true;
                                player1BlockEndTime = DateTime.Now + blockDuration;
                            }
                            else
                            {
                                fighter1.TakeDamage(dmg);
                                lstBattleLog.Items.Add($"{fighter2.Name} attacks {fighter1.Name} for {dmg} damage!");
                                player1Dialogue = GetRandomTrashtalk();
                                player1DialogueEndTime = DateTime.Now + dialogueDuration;
                                player1HitEffect = true;
                                player1HitEffectEndTime = DateTime.Now + hitEffectDuration;
                                 fighter2.RecoverMana(5); // Recover mana on hit
                            }
                        }
                    }
                    UpdateHealthLabels(fighter1, fighter2); // Updates mana too
                    player1Turn = !player1Turn;
                    await Task.Delay(400);
                    pnlArena.Invalidate(); // Redraw to show/hide dialogue and effects
                }

                // Determine winner and show winner dialogue
                string winnerName = fighter1.Health > 0 ? fighter1.Name : fighter2.Name;
                lblWinner.Text = $"Winner: {winnerName}!";
                lstBattleLog.Items.Add($"{winnerName} wins the battle!");

                // Display winner dialogue above head
                if (fighter1.Health > 0)
                {
                    player1Dialogue = "Ez, walang kaba!";
                    player1DialogueEndTime = DateTime.Now + dialogueDuration + TimeSpan.FromSeconds(1); // Show longer
                    player2Dialogue = ""; // Clear loser's dialogue
                }
                else
                {
                    player2Dialogue = "Ez, night night!";
                    player2DialogueEndTime = DateTime.Now + dialogueDuration + TimeSpan.FromSeconds(1); // Show longer
                    player1Dialogue = ""; // Clear loser's dialogue
                }
                pnlArena.Invalidate(); // Redraw to show winner dialogue

                // Set dead flag and loser dialogue for the loser
                if (fighter1.Health <= 0)
                {
                    player1Dead = true;
                    player1Dialogue = "Arayyyy koooh!"; // Loser dialogue
                    player1DialogueEndTime = DateTime.Now + dialogueDuration; // Show for normal duration
                }
                if (fighter2.Health <= 0)
                {
                    player2Dead = true;
                    player2Dialogue = "Babawi ako, kupal!"; // Loser dialogue
                    player2DialogueEndTime = DateTime.Now + dialogueDuration; // Show for normal duration
                }
                player1Pose = 0; player2Pose = 0;
                player1Offset = 0; player2Offset = 0;
                pnlArena.Invalidate(); // Redraw with dead stickman
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // Animate sword swing for the attacking stickman, now with move-in effect
        private async Task AnimateSwordSwing(bool player1)
        {
            int moveIn = 120; // how much closer they move
            if (player1)
            {
                // Move both closer
                player1Offset += moveIn;
                player2Offset -= moveIn;
                pnlArena.Invalidate(); await Task.Delay(80);
                player1Pose = 1; pnlArena.Invalidate(); await Task.Delay(120);
                player1Pose = 2; pnlArena.Invalidate(); await Task.Delay(120);
                player1Pose = 0; pnlArena.Invalidate(); await Task.Delay(80);
                // Move back
                player1Offset -= moveIn;
                player2Offset += moveIn;
                pnlArena.Invalidate();
            }
            else
            {
                // Move both closer
                player1Offset += moveIn;
                player2Offset -= moveIn;
                pnlArena.Invalidate(); await Task.Delay(80);
                player2Pose = 1; pnlArena.Invalidate(); await Task.Delay(120);
                player2Pose = 2; pnlArena.Invalidate(); await Task.Delay(120);
                player2Pose = 0; pnlArena.Invalidate(); await Task.Delay(80);
                // Move back
                player1Offset -= moveIn;
                player2Offset += moveIn;
                pnlArena.Invalidate();
            }
        }

        /// <summary>
        /// Factory method to create a ClassFighter based on selection.
        /// Demonstrates Polymorphism.
        /// </summary>
        private ClassFighter CreateFighter(string className, string name)
        {
            switch (className)
            {
                case "Bida-bida":
                    return new Pabida(name);
                case "Pabibo":
                    return new Pabibo(name);
                default:
                    throw new ArgumentException("Unknown character class selected.");
            }
        }

        /// <summary>
        /// Updates the health labels for both players.
        /// Now also updates mana labels and triggers a panel redraw.
        /// </summary>
        private void UpdateHealthLabels(ClassFighter f1, ClassFighter f2)
        {
            lastPlayer1Health = f1.Health;
            lastPlayer1MaxHealth = f1.MaxHealth;
            lastPlayer2Health = f2.Health;
            lastPlayer2MaxHealth = f2.MaxHealth;
            lastPlayer1Mana = f1.Mana;
            lastPlayer1MaxMana = f1.MaxMana;
            lastPlayer2Mana = f2.Mana;
            lastPlayer2MaxMana = f2.MaxMana;
            lblPlayer1Health.Text = $"Health: {f1.Health}/{f1.MaxHealth} Mana: {f1.Mana}/{f1.MaxMana}";
            lblPlayer2Health.Text = $"Health: {f2.Health}/{f2.MaxHealth} Mana: {f2.Mana}/{f2.MaxMana}";
            pnlArena.Invalidate();
        }

        // Get skill mana cost based on fighter type
        private int GetSkillManaCost(ClassFighter fighter)
        {
             if (fighter is Pabida) return 20;
             if (fighter is Pabibo) return 30;
             return 0; // Default
        }

        // Update skill status text based on fighter state
        private void UpdateSkillUI(ClassFighter f1, ClassFighter f2)
        {
            player1SkillReady = f1.IsSkillReady;
            player2SkillReady = f2.IsSkillReady;
            if (f1.IsSkillReady)
            {
                player1SkillText = "Skill Ready";
            }
            else
            {
                TimeSpan remaining = f1.SkillCooldownEnd - DateTime.Now;
                player1SkillText = $"CD: {remaining.TotalSeconds:F1}s";
            }
            if (f2.IsSkillReady)
            {
                player2SkillText = "Skill Ready";
            }
            else
            {
                TimeSpan remaining = f2.SkillCooldownEnd - DateTime.Now;
                player2SkillText = $"CD: {remaining.TotalSeconds:F1}s";
            }
        }

        private void pnlArena_Paint_1(object sender, PaintEventArgs e)
        {

        }

        private void lstBattleLog_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        // Get a random trashtalk phrase
        private string GetRandomTrashtalk()
        {
            return trashtalkPhrases[blockRng.Next(trashtalkPhrases.Length)];
        }

        private void lblWinner_Click(object sender, EventArgs e)
        {

        }

        // Draw a sword wave effect (now a piercing energy bolt)
        private void DrawSwordWave(Graphics g, int attackerX, int attackerY, int targetX, int targetY, bool startsLeft, float progress)
        {
            if (startsLeft) // Player 1's fire wave effect
            {
                // progress goes from 0 (start) to 1 (end)
                int alpha = (int)(255 * (1 - progress)); // Fade out
                if (alpha <= 0) return;

                // Calculate the current position of the bolt head
                int currentX = (int)(attackerX + (targetX - attackerX) * progress);
                int currentY = (int)(attackerY + (targetY - attackerY) * progress);

                float sizeProgress = progress < 0.5f ? progress * 2 : (1 - progress) * 2;
                int headSize = (int)(30 + sizeProgress * 40);

                if (headSize <= 0) headSize = 1;

                // Draw the main bolt head
                using (Brush headBrush = new SolidBrush(Color.FromArgb(alpha, 255, 100, 0)))
                using (Pen headPen = new Pen(Color.FromArgb(alpha, 255, 255, 0), 4))
                {
                    // Draw a diamond/arrow shape for the bolt head pointing right
                    Point[] diamond = new Point[]
                    {
                        new Point(currentX, currentY),
                        new Point(currentX - headSize / 2, currentY - headSize / 2),
                        new Point(currentX - headSize, currentY),
                        new Point(currentX - headSize / 2, currentY + headSize / 2)
                    };
                    g.FillPolygon(headBrush, diamond);
                    g.DrawPolygon(headPen, diamond);
                }

                // Draw fiery trailing effect
                using (Pen trailPen = new Pen(Color.FromArgb(alpha, 255, 60, 0), 6))
                {
                    trailPen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                    trailPen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                    int trailLength = (int)(250 * (1 - progress));
                    int numTrailSegments = 6;
                    for (int i = 0; i < numTrailSegments; i++)
                    {
                        float segmentProgress = (float)i / (numTrailSegments - 1);
                        int trailSegmentX = (int)(currentX - segmentProgress * trailLength);
                        int trailYOffset = (int)(Math.Sin(progress * Math.PI * 10 + i) * 12 * (1 - progress));
                        g.DrawLine(trailPen, currentX, currentY + trailYOffset, trailSegmentX, currentY + trailYOffset);
                    }
                }

                // Draw bright core glow
                using (Brush glowBrush = new SolidBrush(Color.FromArgb(alpha, 255, 255, 50)))
                {
                    int glowSize = headSize + 15;
                    g.FillEllipse(glowBrush, currentX - glowSize / 2, currentY - glowSize / 2, glowSize, glowSize);
                }

                // Add fire particles
                if (alpha > 50)
                {
                    using (Brush particleBrush = new SolidBrush(Color.FromArgb(alpha, 255, 100, 0)))
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            int pOffsetX = effectRng.Next(-(headSize + 10), (headSize + 10) + 1);
                            int pOffsetY = effectRng.Next(-(headSize + 10), (headSize + 10) + 1);
                            int pSize = effectRng.Next(3, 7);
                            g.FillEllipse(particleBrush, currentX + pOffsetX, currentY + pOffsetY, pSize, pSize);
                        }
                    }
                }
            }
            else // Player 2's vortex effect
            {
                int alpha = (int)(255 * (1 - progress));
                if (alpha <= 0) return;

                int currentX = (int)(attackerX + (targetX - attackerX) * progress);
                int currentY = (int)(attackerY + (targetY - attackerY) * progress);

                // Create a path for the spiral with bounds checking
                using (System.Drawing.Drawing2D.GraphicsPath spiralPath = new System.Drawing.Drawing2D.GraphicsPath())
                {
                    float angle = 0;
                    float radius = Math.Max(1, 40 * (1 - progress)); // Ensure radius is never 0
                    float centerX = currentX;
                    float centerY = currentY;

                    PointF lastPoint = new PointF(centerX, centerY);
                    spiralPath.StartFigure();

                    // Create spiral effect with safety checks
                    for (float i = 0; i < 20; i += 0.1f)
                    {
                        angle = i * (float)Math.PI * 2;
                        radius = Math.Max(1, (20 - i) * (1 - progress) * 3); // Ensure radius is never 0
                        float x = centerX + (float)(Math.Cos(angle + progress * 10) * radius);
                        float y = centerY + (float)(Math.Sin(angle + progress * 10) * radius);

                        // Ensure points are valid
                        if (!float.IsInfinity(x) && !float.IsNaN(x) && 
                            !float.IsInfinity(y) && !float.IsNaN(y))
                        {
                            spiralPath.AddLine(lastPoint, new PointF(x, y));
                            lastPoint = new PointF(x, y);
                        }
                    }

                    // Draw the spiral with a gradient effect
                    if (spiralPath.PointCount > 0)
                    {
                        using (Pen spiralPen = new Pen(Color.FromArgb(alpha, 128, 0, 255), 4))
                        {
                            spiralPen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                            spiralPen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                            g.DrawPath(spiralPen, spiralPath);
                        }
                    }
                }

                // Add energy orb in the center with bounds checking
                int orbSize = Math.Max(1, (int)(50 * (1 - progress * 0.5f))); // Ensure size is never 0
                Rectangle orbBounds = new Rectangle(
                    currentX - orbSize/2,
                    currentY - orbSize/2,
                    Math.Max(1, orbSize),
                    Math.Max(1, orbSize)
                );

                using (System.Drawing.Drawing2D.GraphicsPath orbPath = new System.Drawing.Drawing2D.GraphicsPath())
                {
                    orbPath.AddEllipse(orbBounds);
                    
                    // Create a gradient for the orb
                    using (System.Drawing.Drawing2D.PathGradientBrush orbBrush = 
                           new System.Drawing.Drawing2D.PathGradientBrush(orbPath))
                    {
                        orbBrush.CenterColor = Color.FromArgb(alpha, 255, 255, 255);
                        orbBrush.SurroundColors = new Color[] { Color.FromArgb(alpha, 128, 0, 255) };
                        g.FillPath(orbBrush, orbPath);
                    }
                }

                // Add energy particles with bounds checking
                if (alpha > 50)
                {
                    float particleRadius = Math.Max(1, 30 * (1 - progress)); // Ensure radius is never 0
                    for (int i = 0; i < 12; i++)
                    {
                        double particleAngle = i * Math.PI * 2 / 12 + progress * 10;
                        int px = (int)(currentX + Math.Cos(particleAngle) * particleRadius);
                        int py = (int)(currentY + Math.Sin(particleAngle) * particleRadius);
                        
                        // Ensure particle coordinates are valid
                        if (!double.IsInfinity(px) && !double.IsNaN(px) &&
                            !double.IsInfinity(py) && !double.IsNaN(py))
                        {
                            using (Brush particleBrush = new SolidBrush(Color.FromArgb(alpha, 180, 100, 255)))
                            {
                                int particleSize = Math.Max(1, effectRng.Next(2, 6)); // Ensure size is never 0
                                g.FillEllipse(particleBrush, 
                                    px - particleSize/2, 
                                    py - particleSize/2, 
                                    particleSize, 
                                    particleSize);
                            }
                        }
                    }
                }

                // Add outer glow ring with bounds checking
                float glowRadius = Math.Max(1, 60 * (1 - progress * 0.7f)); // Ensure radius is never 0
                Rectangle glowBounds = new Rectangle(
                    (int)(currentX - glowRadius),
                    (int)(currentY - glowRadius),
                    (int)(glowRadius * 2),
                    (int)(glowRadius * 2)
                );

                if (glowBounds.Width > 0 && glowBounds.Height > 0)
                {
                    using (Pen glowPen = new Pen(Color.FromArgb(alpha / 2, 180, 100, 255), 2))
                    {
                        g.DrawEllipse(glowPen, glowBounds);
                    }
                }
            }
        }

        // Draw critical hit flash effect
        private void DrawCriticalFlash(Graphics g, int x, int y)
        {
            // This method is now unused, but keeping it for reference or potential future use.
            // The sword wave effect replaces this.
        }
    }
}
