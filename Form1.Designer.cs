namespace GoblinFarmer
{
    partial class frmMain
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));
            btnMakeNewGame = new Button();
            lblCombatStatus = new Label();
            btnExitGame = new Button();
            tmrStatus = new System.Windows.Forms.Timer(components);
            lblDiabloStatus = new Label();
            btnSouthernHighlands = new Button();
            grpFlows = new GroupBox();
            grpRoutes = new GroupBox();
            btnPandemoniumFortressLevel2 = new Button();
            btnPandemoniumFortressLevel1 = new Button();
            lblAct5 = new Label();
            btnRakkisCrossing = new Button();
            lblAct3 = new Label();
            btnBattlefields = new Button();
            btnStingingWinds = new Button();
            btnAncientWaterway = new Button();
            btnCityOfCaldeum = new Button();
            btnHiddenCamp = new Button();
            lblAct2 = new Label();
            btnRoyalCrypts = new Button();
            btnCathedral = new Button();
            btnTheFesteringWoods = new Button();
            btnTheWeepingHollow = new Button();
            btnNewTristram = new Button();
            btnNorthernHighlands = new Button();
            lblAct1 = new Label();
            grpStatus = new GroupBox();
            lblAppStatus = new Label();
            grpCharacter = new GroupBox();
            radWD = new RadioButton();
            radDH = new RadioButton();
            radMonk = new RadioButton();
            grpHotkeys = new GroupBox();
            chkTeleportNextHotkey = new CheckBox();
            chkExitGameHotkey = new CheckBox();
            chkKeepDebugScreenshots = new CheckBox();
            chkLoot = new CheckBox();
            chkKadala = new CheckBox();
            chkCombat = new CheckBox();
            grpGoblinTracker = new GroupBox();
            lblGoblinEvidenceTime = new Label();
            lblGoblinEvidenceConfidence = new Label();
            lblGoblinEvidenceType = new Label();
            lblGoblinEvidenceLast = new Label();
            lblGoblinObservation = new Label();
            btnResetGoblinTrackerStats = new Button();
            lblGoblinActiveTime = new Label();
            lblGoblinGph = new Label();
            lblGoblinCount = new Label();
            grpSessionStats = new GroupBox();
            lblSessionRuntime = new Label();
            lblSessionFailures = new Label();
            lblSessionBlocked = new Label();
            lblSessionTeleports = new Label();
            lblSessionGames = new Label();
            lblEscape = new Label();
            grpFlows.SuspendLayout();
            grpRoutes.SuspendLayout();
            grpStatus.SuspendLayout();
            grpCharacter.SuspendLayout();
            grpHotkeys.SuspendLayout();
            grpGoblinTracker.SuspendLayout();
            grpSessionStats.SuspendLayout();
            SuspendLayout();
            // 
            // btnMakeNewGame
            // 
            btnMakeNewGame.Location = new Point(15, 22);
            btnMakeNewGame.Name = "btnMakeNewGame";
            btnMakeNewGame.Size = new Size(126, 23);
            btnMakeNewGame.TabIndex = 0;
            btnMakeNewGame.Text = "Make New Game";
            btnMakeNewGame.UseVisualStyleBackColor = true;
            // 
            // lblCombatStatus
            // 
            lblCombatStatus.AutoEllipsis = true;
            lblCombatStatus.Location = new Point(14, 61);
            lblCombatStatus.Name = "lblCombatStatus";
            lblCombatStatus.Size = new Size(280, 18);
            lblCombatStatus.TabIndex = 1;
            lblCombatStatus.Text = "Combat Status: Idle";
            // 
            // btnExitGame
            // 
            btnExitGame.Location = new Point(161, 22);
            btnExitGame.Name = "btnExitGame";
            btnExitGame.Size = new Size(126, 23);
            btnExitGame.TabIndex = 2;
            btnExitGame.Text = "Exit Game";
            btnExitGame.UseVisualStyleBackColor = true;
            // 
            // tmrStatus
            // 
            tmrStatus.Interval = 1000;
            tmrStatus.Tick += tmrStatus_Tick;
            // 
            // lblDiabloStatus
            // 
            lblDiabloStatus.AutoEllipsis = true;
            lblDiabloStatus.Location = new Point(14, 32);
            lblDiabloStatus.Name = "lblDiabloStatus";
            lblDiabloStatus.Size = new Size(280, 18);
            lblDiabloStatus.TabIndex = 3;
            lblDiabloStatus.Text = "Diablo Status: Unknown";
            // 
            // btnSouthernHighlands
            // 
            btnSouthernHighlands.Location = new Point(12, 99);
            btnSouthernHighlands.Name = "btnSouthernHighlands";
            btnSouthernHighlands.Size = new Size(140, 29);
            btnSouthernHighlands.TabIndex = 5;
            btnSouthernHighlands.Text = "Southern Highlands";
            btnSouthernHighlands.UseVisualStyleBackColor = true;
            // 
            // grpFlows
            // 
            grpFlows.Controls.Add(btnMakeNewGame);
            grpFlows.Controls.Add(btnExitGame);
            grpFlows.Location = new Point(12, 142);
            grpFlows.Name = "grpFlows";
            grpFlows.Size = new Size(311, 63);
            grpFlows.TabIndex = 6;
            grpFlows.TabStop = false;
            grpFlows.Text = "Game Flows";
            // 
            // grpRoutes
            // 
            grpRoutes.Controls.Add(btnPandemoniumFortressLevel2);
            grpRoutes.Controls.Add(btnPandemoniumFortressLevel1);
            grpRoutes.Controls.Add(lblAct5);
            grpRoutes.Controls.Add(btnRakkisCrossing);
            grpRoutes.Controls.Add(lblAct3);
            grpRoutes.Controls.Add(btnBattlefields);
            grpRoutes.Controls.Add(btnStingingWinds);
            grpRoutes.Controls.Add(btnAncientWaterway);
            grpRoutes.Controls.Add(btnCityOfCaldeum);
            grpRoutes.Controls.Add(btnHiddenCamp);
            grpRoutes.Controls.Add(lblAct2);
            grpRoutes.Controls.Add(btnRoyalCrypts);
            grpRoutes.Controls.Add(btnCathedral);
            grpRoutes.Controls.Add(btnTheFesteringWoods);
            grpRoutes.Controls.Add(btnTheWeepingHollow);
            grpRoutes.Controls.Add(btnNewTristram);
            grpRoutes.Controls.Add(btnNorthernHighlands);
            grpRoutes.Controls.Add(lblAct1);
            grpRoutes.Controls.Add(btnSouthernHighlands);
            grpRoutes.Location = new Point(349, 63);
            grpRoutes.Name = "grpRoutes";
            grpRoutes.Size = new Size(544, 387);
            grpRoutes.TabIndex = 7;
            grpRoutes.TabStop = false;
            grpRoutes.Text = "Locations";
            // 
            // btnPandemoniumFortressLevel2
            // 
            btnPandemoniumFortressLevel2.Location = new Point(313, 300);
            btnPandemoniumFortressLevel2.Name = "btnPandemoniumFortressLevel2";
            btnPandemoniumFortressLevel2.Size = new Size(194, 27);
            btnPandemoniumFortressLevel2.TabIndex = 23;
            btnPandemoniumFortressLevel2.Text = "Pandemonium Fortress Level 2";
            btnPandemoniumFortressLevel2.UseVisualStyleBackColor = true;
            // 
            // btnPandemoniumFortressLevel1
            // 
            btnPandemoniumFortressLevel1.Location = new Point(313, 263);
            btnPandemoniumFortressLevel1.Name = "btnPandemoniumFortressLevel1";
            btnPandemoniumFortressLevel1.Size = new Size(194, 27);
            btnPandemoniumFortressLevel1.TabIndex = 22;
            btnPandemoniumFortressLevel1.Text = "Pandemonium Fortress Level 1";
            btnPandemoniumFortressLevel1.UseVisualStyleBackColor = true;
            // 
            // lblAct5
            // 
            lblAct5.AutoSize = true;
            lblAct5.Location = new Point(388, 227);
            lblAct5.Name = "lblAct5";
            lblAct5.Size = new Size(34, 15);
            lblAct5.TabIndex = 21;
            lblAct5.Text = "Act 5";
            // 
            // btnRakkisCrossing
            // 
            btnRakkisCrossing.Location = new Point(341, 98);
            btnRakkisCrossing.Name = "btnRakkisCrossing";
            btnRakkisCrossing.Size = new Size(140, 27);
            btnRakkisCrossing.TabIndex = 20;
            btnRakkisCrossing.Text = "Rakkis Crossing";
            btnRakkisCrossing.UseVisualStyleBackColor = true;
            // 
            // lblAct3
            // 
            lblAct3.AutoSize = true;
            lblAct3.Location = new Point(401, 28);
            lblAct3.Name = "lblAct3";
            lblAct3.Size = new Size(34, 15);
            lblAct3.TabIndex = 18;
            lblAct3.Text = "Act 3";
            // 
            // btnBattlefields
            // 
            btnBattlefields.Location = new Point(342, 63);
            btnBattlefields.Name = "btnBattlefields";
            btnBattlefields.Size = new Size(140, 27);
            btnBattlefields.TabIndex = 17;
            btnBattlefields.Text = "Battlefields";
            btnBattlefields.UseVisualStyleBackColor = true;
            // 
            // btnStingingWinds
            // 
            btnStingingWinds.Location = new Point(85, 337);
            btnStingingWinds.Name = "btnStingingWinds";
            btnStingingWinds.Size = new Size(140, 27);
            btnStingingWinds.TabIndex = 16;
            btnStingingWinds.Text = "Stinging Winds";
            btnStingingWinds.UseVisualStyleBackColor = true;
            // 
            // btnAncientWaterway
            // 
            btnAncientWaterway.Location = new Point(158, 300);
            btnAncientWaterway.Name = "btnAncientWaterway";
            btnAncientWaterway.Size = new Size(140, 27);
            btnAncientWaterway.TabIndex = 15;
            btnAncientWaterway.Text = "Ancient Waterway";
            btnAncientWaterway.UseVisualStyleBackColor = true;
            // 
            // btnCityOfCaldeum
            // 
            btnCityOfCaldeum.Location = new Point(12, 300);
            btnCityOfCaldeum.Name = "btnCityOfCaldeum";
            btnCityOfCaldeum.Size = new Size(140, 27);
            btnCityOfCaldeum.TabIndex = 14;
            btnCityOfCaldeum.Text = "City of Caldeum";
            btnCityOfCaldeum.UseVisualStyleBackColor = true;
            // 
            // btnHiddenCamp
            // 
            btnHiddenCamp.Location = new Point(85, 263);
            btnHiddenCamp.Name = "btnHiddenCamp";
            btnHiddenCamp.Size = new Size(140, 27);
            btnHiddenCamp.TabIndex = 13;
            btnHiddenCamp.Text = "Hidden Camp";
            btnHiddenCamp.UseVisualStyleBackColor = true;
            // 
            // lblAct2
            // 
            lblAct2.AutoSize = true;
            lblAct2.Location = new Point(134, 227);
            lblAct2.Name = "lblAct2";
            lblAct2.Size = new Size(34, 15);
            lblAct2.TabIndex = 12;
            lblAct2.Text = "Act 2";
            // 
            // btnRoyalCrypts
            // 
            btnRoyalCrypts.Location = new Point(158, 166);
            btnRoyalCrypts.Name = "btnRoyalCrypts";
            btnRoyalCrypts.Size = new Size(140, 27);
            btnRoyalCrypts.TabIndex = 11;
            btnRoyalCrypts.Text = "Royal Crypts";
            btnRoyalCrypts.UseVisualStyleBackColor = true;
            // 
            // btnCathedral
            // 
            btnCathedral.Location = new Point(12, 166);
            btnCathedral.Name = "btnCathedral";
            btnCathedral.Size = new Size(140, 27);
            btnCathedral.TabIndex = 10;
            btnCathedral.Text = "Cathedral";
            btnCathedral.UseVisualStyleBackColor = true;
            // 
            // btnTheFesteringWoods
            // 
            btnTheFesteringWoods.Location = new Point(158, 133);
            btnTheFesteringWoods.Name = "btnTheFesteringWoods";
            btnTheFesteringWoods.Size = new Size(140, 27);
            btnTheFesteringWoods.TabIndex = 9;
            btnTheFesteringWoods.Text = "The Festering Woods";
            btnTheFesteringWoods.UseVisualStyleBackColor = true;
            // 
            // btnTheWeepingHollow
            // 
            btnTheWeepingHollow.Location = new Point(12, 133);
            btnTheWeepingHollow.Name = "btnTheWeepingHollow";
            btnTheWeepingHollow.Size = new Size(140, 27);
            btnTheWeepingHollow.TabIndex = 8;
            btnTheWeepingHollow.Text = "The Weeping Hollow";
            btnTheWeepingHollow.UseVisualStyleBackColor = true;
            // 
            // btnNewTristram
            // 
            btnNewTristram.Location = new Point(85, 63);
            btnNewTristram.Name = "btnNewTristram";
            btnNewTristram.Size = new Size(140, 27);
            btnNewTristram.TabIndex = 7;
            btnNewTristram.Text = "New Tristram";
            btnNewTristram.UseVisualStyleBackColor = true;
            // 
            // btnNorthernHighlands
            // 
            btnNorthernHighlands.Location = new Point(158, 98);
            btnNorthernHighlands.Name = "btnNorthernHighlands";
            btnNorthernHighlands.Size = new Size(140, 30);
            btnNorthernHighlands.TabIndex = 6;
            btnNorthernHighlands.Text = "Northern Highlands";
            btnNorthernHighlands.UseVisualStyleBackColor = true;
            // 
            // lblAct1
            // 
            lblAct1.AutoSize = true;
            lblAct1.Location = new Point(134, 28);
            lblAct1.Name = "lblAct1";
            lblAct1.Size = new Size(34, 15);
            lblAct1.TabIndex = 0;
            lblAct1.Text = "Act 1";
            // 
            // grpStatus
            // 
            grpStatus.Controls.Add(lblAppStatus);
            grpStatus.Controls.Add(lblDiabloStatus);
            grpStatus.Controls.Add(lblCombatStatus);
            grpStatus.Location = new Point(12, 12);
            grpStatus.Name = "grpStatus";
            grpStatus.Size = new Size(311, 118);
            grpStatus.TabIndex = 8;
            grpStatus.TabStop = false;
            grpStatus.Text = "Status";
            // 
            // lblAppStatus
            // 
            lblAppStatus.AutoEllipsis = true;
            lblAppStatus.Location = new Point(14, 90);
            lblAppStatus.Name = "lblAppStatus";
            lblAppStatus.Size = new Size(280, 18);
            lblAppStatus.TabIndex = 4;
            lblAppStatus.Text = "App Status: Idle";
            // 
            // grpCharacter
            // 
            grpCharacter.Controls.Add(radWD);
            grpCharacter.Controls.Add(radDH);
            grpCharacter.Controls.Add(radMonk);
            grpCharacter.Location = new Point(12, 222);
            grpCharacter.Name = "grpCharacter";
            grpCharacter.Size = new Size(144, 114);
            grpCharacter.TabIndex = 9;
            grpCharacter.TabStop = false;
            grpCharacter.Text = "Characters";
            // 
            // radWD
            // 
            radWD.AutoSize = true;
            radWD.Location = new Point(18, 81);
            radWD.Name = "radWD";
            radWD.Size = new Size(95, 19);
            radWD.TabIndex = 2;
            radWD.TabStop = true;
            radWD.Text = "Witch Doctor";
            radWD.UseVisualStyleBackColor = true;
            // 
            // radDH
            // 
            radDH.AutoSize = true;
            radDH.Location = new Point(18, 56);
            radDH.Name = "radDH";
            radDH.Size = new Size(104, 19);
            radDH.TabIndex = 1;
            radDH.TabStop = true;
            radDH.Text = "Demon Hunter";
            radDH.UseVisualStyleBackColor = true;
            // 
            // radMonk
            // 
            radMonk.AutoSize = true;
            radMonk.Location = new Point(18, 31);
            radMonk.Name = "radMonk";
            radMonk.Size = new Size(56, 19);
            radMonk.TabIndex = 0;
            radMonk.TabStop = true;
            radMonk.Text = "Monk";
            radMonk.UseVisualStyleBackColor = true;
            // 
            // grpHotkeys
            // 
            grpHotkeys.Controls.Add(chkTeleportNextHotkey);
            grpHotkeys.Controls.Add(chkExitGameHotkey);
            grpHotkeys.Controls.Add(chkLoot);
            grpHotkeys.Controls.Add(chkKadala);
            grpHotkeys.Controls.Add(chkCombat);
            grpHotkeys.Location = new Point(12, 348);
            grpHotkeys.Name = "grpHotkeys";
            grpHotkeys.Size = new Size(270, 157);
            grpHotkeys.TabIndex = 10;
            grpHotkeys.TabStop = false;
            grpHotkeys.Text = "Hotkeys";
            // 
            // chkTeleportNextHotkey
            // 
            chkTeleportNextHotkey.AutoSize = true;
            chkTeleportNextHotkey.Location = new Point(15, 49);
            chkTeleportNextHotkey.Name = "chkTeleportNextHotkey";
            chkTeleportNextHotkey.Size = new Size(162, 19);
            chkTeleportNextHotkey.TabIndex = 1;
            chkTeleportNextHotkey.Text = "1 - Teleport Next Location";
            chkTeleportNextHotkey.UseVisualStyleBackColor = true;
            // 
            // chkExitGameHotkey
            // 
            chkExitGameHotkey.AutoSize = true;
            chkExitGameHotkey.Location = new Point(15, 74);
            chkExitGameHotkey.Name = "chkExitGameHotkey";
            chkExitGameHotkey.Size = new Size(95, 19);
            chkExitGameHotkey.TabIndex = 2;
            chkExitGameHotkey.Text = "2 - Exit Game";
            chkExitGameHotkey.UseVisualStyleBackColor = true;
            // 
            // chkKeepDebugScreenshots
            //
            chkKeepDebugScreenshots.AutoSize = true;
            chkKeepDebugScreenshots.Location = new Point(15, 149);
            chkKeepDebugScreenshots.Name = "chkKeepDebugScreenshots";
            chkKeepDebugScreenshots.Size = new Size(156, 19);
            chkKeepDebugScreenshots.TabIndex = 5;
            chkKeepDebugScreenshots.Text = "Keep Debug Screenshots";
            chkKeepDebugScreenshots.UseVisualStyleBackColor = true;
            // 
            // chkLoot
            // 
            chkLoot.AutoSize = true;
            chkLoot.Location = new Point(15, 124);
            chkLoot.Name = "chkLoot";
            chkLoot.Size = new Size(99, 19);
            chkLoot.TabIndex = 4;
            chkLoot.Text = "Alt + ` to Loot";
            chkLoot.UseVisualStyleBackColor = true;
            // 
            // chkKadala
            // 
            chkKadala.AutoSize = true;
            chkKadala.Location = new Point(15, 99);
            chkKadala.Name = "chkKadala";
            chkKadala.Size = new Size(132, 19);
            chkKadala.TabIndex = 3;
            chkKadala.Text = "Up Arrow for Kadala";
            chkKadala.UseVisualStyleBackColor = true;
            // 
            // chkCombat
            // 
            chkCombat.AutoSize = true;
            chkCombat.Location = new Point(15, 24);
            chkCombat.Name = "chkCombat";
            chkCombat.Size = new Size(231, 19);
            chkCombat.TabIndex = 0;
            chkCombat.Text = "Enable Combat Hotkey (tilde/backtick)";
            chkCombat.UseVisualStyleBackColor = true;
            //
            // grpGoblinTracker
            //
            grpGoblinTracker.Controls.Add(lblGoblinEvidenceTime);
            grpGoblinTracker.Controls.Add(lblGoblinEvidenceConfidence);
            grpGoblinTracker.Controls.Add(lblGoblinEvidenceType);
            grpGoblinTracker.Controls.Add(lblGoblinEvidenceLast);
            grpGoblinTracker.Controls.Add(lblGoblinObservation);
            grpGoblinTracker.Controls.Add(btnResetGoblinTrackerStats);
            grpGoblinTracker.Controls.Add(lblGoblinActiveTime);
            grpGoblinTracker.Controls.Add(lblGoblinGph);
            grpGoblinTracker.Controls.Add(lblGoblinCount);
            grpGoblinTracker.Location = new Point(12, 517);
            grpGoblinTracker.Name = "grpGoblinTracker";
            grpGoblinTracker.Size = new Size(311, 214);
            grpGoblinTracker.TabIndex = 13;
            grpGoblinTracker.TabStop = false;
            grpGoblinTracker.Text = "Goblin Tracker";
            //
            // lblGoblinEvidenceTime
            //
            lblGoblinEvidenceTime.AutoSize = true;
            lblGoblinEvidenceTime.Location = new Point(12, 122);
            lblGoblinEvidenceTime.Name = "lblGoblinEvidenceTime";
            lblGoblinEvidenceTime.Size = new Size(96, 15);
            lblGoblinEvidenceTime.TabIndex = 7;
            lblGoblinEvidenceTime.Text = "Evidence Time: --";
            //
            // lblGoblinEvidenceConfidence
            //
            lblGoblinEvidenceConfidence.AutoSize = true;
            lblGoblinEvidenceConfidence.Location = new Point(12, 104);
            lblGoblinEvidenceConfidence.Name = "lblGoblinEvidenceConfidence";
            lblGoblinEvidenceConfidence.Size = new Size(139, 15);
            lblGoblinEvidenceConfidence.TabIndex = 6;
            lblGoblinEvidenceConfidence.Text = "Evidence Confidence: 0.00";
            //
            // lblGoblinEvidenceType
            //
            lblGoblinEvidenceType.AutoSize = true;
            lblGoblinEvidenceType.Location = new Point(12, 86);
            lblGoblinEvidenceType.Name = "lblGoblinEvidenceType";
            lblGoblinEvidenceType.Size = new Size(113, 15);
            lblGoblinEvidenceType.TabIndex = 5;
            lblGoblinEvidenceType.Text = "Evidence Type: None";
            //
            // lblGoblinEvidenceLast
            //
            lblGoblinEvidenceLast.AutoSize = true;
            lblGoblinEvidenceLast.Location = new Point(12, 68);
            lblGoblinEvidenceLast.Name = "lblGoblinEvidenceLast";
            lblGoblinEvidenceLast.Size = new Size(113, 15);
            lblGoblinEvidenceLast.TabIndex = 4;
            lblGoblinEvidenceLast.Text = "Last Evidence: None";
            //
            // lblGoblinObservation
            //
            lblGoblinObservation.Location = new Point(12, 144);
            lblGoblinObservation.Name = "lblGoblinObservation";
            lblGoblinObservation.Size = new Size(287, 63);
            lblGoblinObservation.TabIndex = 8;
            lblGoblinObservation.Text = "Last Observation:\r\n--\r\n--\r\n--\r\n--";
            //
            // btnResetGoblinTrackerStats
            //
            btnResetGoblinTrackerStats.Location = new Point(183, 22);
            btnResetGoblinTrackerStats.Name = "btnResetGoblinTrackerStats";
            btnResetGoblinTrackerStats.Size = new Size(104, 23);
            btnResetGoblinTrackerStats.TabIndex = 3;
            btnResetGoblinTrackerStats.Text = "Reset Stats";
            btnResetGoblinTrackerStats.UseVisualStyleBackColor = true;
            //
            // lblGoblinActiveTime
            //
            lblGoblinActiveTime.AutoSize = true;
            lblGoblinActiveTime.Location = new Point(12, 58);
            lblGoblinActiveTime.Name = "lblGoblinActiveTime";
            lblGoblinActiveTime.Size = new Size(124, 15);
            lblGoblinActiveTime.TabIndex = 2;
            lblGoblinActiveTime.Text = "Active Time: 00:00:00";
            //
            // lblGoblinGph
            //
            lblGoblinGph.AutoSize = true;
            lblGoblinGph.Location = new Point(12, 40);
            lblGoblinGph.Name = "lblGoblinGph";
            lblGoblinGph.Size = new Size(57, 15);
            lblGoblinGph.TabIndex = 1;
            lblGoblinGph.Text = "GPH: 0.00";
            //
            // lblGoblinCount
            //
            lblGoblinCount.AutoSize = true;
            lblGoblinCount.Location = new Point(12, 22);
            lblGoblinCount.Name = "lblGoblinCount";
            lblGoblinCount.Size = new Size(61, 15);
            lblGoblinCount.TabIndex = 0;
            lblGoblinCount.Text = "Goblins: 0";
            // 
            // grpSessionStats
            // 
            grpSessionStats.Controls.Add(lblSessionRuntime);
            grpSessionStats.Controls.Add(lblSessionFailures);
            grpSessionStats.Controls.Add(lblSessionBlocked);
            grpSessionStats.Controls.Add(lblSessionTeleports);
            grpSessionStats.Controls.Add(lblSessionGames);
            grpSessionStats.Location = new Point(175, 222);
            grpSessionStats.Name = "grpSessionStats";
            grpSessionStats.Size = new Size(148, 114);
            grpSessionStats.TabIndex = 12;
            grpSessionStats.TabStop = false;
            grpSessionStats.Text = "Session Stats";
            // 
            // lblSessionRuntime
            // 
            lblSessionRuntime.AutoSize = true;
            lblSessionRuntime.Location = new Point(12, 88);
            lblSessionRuntime.Name = "lblSessionRuntime";
            lblSessionRuntime.Size = new Size(100, 15);
            lblSessionRuntime.TabIndex = 4;
            lblSessionRuntime.Text = "Runtime: 00:00:00";
            // 
            // lblSessionFailures
            // 
            lblSessionFailures.AutoSize = true;
            lblSessionFailures.Location = new Point(12, 70);
            lblSessionFailures.Name = "lblSessionFailures";
            lblSessionFailures.Size = new Size(59, 15);
            lblSessionFailures.TabIndex = 3;
            lblSessionFailures.Text = "Failures: 0";
            // 
            // lblSessionBlocked
            // 
            lblSessionBlocked.AutoSize = true;
            lblSessionBlocked.Location = new Point(12, 52);
            lblSessionBlocked.Name = "lblSessionBlocked";
            lblSessionBlocked.Size = new Size(61, 15);
            lblSessionBlocked.TabIndex = 2;
            lblSessionBlocked.Text = "Blocked: 0";
            // 
            // lblSessionTeleports
            // 
            lblSessionTeleports.AutoSize = true;
            lblSessionTeleports.Location = new Point(12, 34);
            lblSessionTeleports.Name = "lblSessionTeleports";
            lblSessionTeleports.Size = new Size(67, 15);
            lblSessionTeleports.TabIndex = 1;
            lblSessionTeleports.Text = "Teleports: 0";
            // 
            // lblSessionGames
            // 
            lblSessionGames.AutoSize = true;
            lblSessionGames.Location = new Point(12, 16);
            lblSessionGames.Name = "lblSessionGames";
            lblSessionGames.Size = new Size(55, 15);
            lblSessionGames.TabIndex = 0;
            lblSessionGames.Text = "Games: 0";
            // 
            // lblEscape
            // 
            lblEscape.Location = new Point(349, 35);
            lblEscape.Name = "lblEscape";
            lblEscape.Size = new Size(544, 20);
            lblEscape.TabIndex = 11;
            lblEscape.Text = "Press Esc to stop";
            lblEscape.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // frmMain
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(918, 740);
            Controls.Add(lblEscape);
            Controls.Add(grpGoblinTracker);
            Controls.Add(grpSessionStats);
            Controls.Add(grpHotkeys);
            Controls.Add(grpCharacter);
            Controls.Add(grpStatus);
            Controls.Add(grpRoutes);
            Controls.Add(grpFlows);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MinimumSize = new Size(934, 779);
            Name = "frmMain";
            Text = "GoblinFarmer";
            Load += frmMain_Load;
            grpFlows.ResumeLayout(false);
            grpRoutes.ResumeLayout(false);
            grpRoutes.PerformLayout();
            grpStatus.ResumeLayout(false);
            grpCharacter.ResumeLayout(false);
            grpCharacter.PerformLayout();
            grpHotkeys.ResumeLayout(false);
            grpHotkeys.PerformLayout();
            grpGoblinTracker.ResumeLayout(false);
            grpGoblinTracker.PerformLayout();
            grpSessionStats.ResumeLayout(false);
            grpSessionStats.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Button btnMakeNewGame;
        private Label lblCombatStatus;
        private Button btnExitGame;
        private System.Windows.Forms.Timer tmrStatus;
        private Label lblDiabloStatus;
        private Button btnSouthernHighlands;
        private GroupBox grpFlows;
        private GroupBox grpRoutes;
        private Label lblAct1;
        private Button btnNewTristram;
        private Button btnNorthernHighlands;
        private Label lblAct2;
        private Button btnRoyalCrypts;
        private Button btnCathedral;
        private Button btnTheFesteringWoods;
        private Button btnTheWeepingHollow;
        private Button btnBattlefields;
        private Button btnStingingWinds;
        private Button btnAncientWaterway;
        private Button btnCityOfCaldeum;
        private Button btnHiddenCamp;
        private Button btnRakkisCrossing;
        private Label lblAct3;
        private Button btnPandemoniumFortressLevel2;
        private Button btnPandemoniumFortressLevel1;
        private Label lblAct5;
        private GroupBox grpStatus;
        private GroupBox grpCharacter;
        private RadioButton radWD;
        private RadioButton radDH;
        private RadioButton radMonk;
        private GroupBox grpHotkeys;
        private CheckBox chkKadala;
        private CheckBox chkCombat;
        private Label lblAppStatus;
        private CheckBox chkLoot;
        private CheckBox chkKeepDebugScreenshots;
        private CheckBox chkTeleportNextHotkey;
        private CheckBox chkExitGameHotkey;
        private GroupBox grpGoblinTracker;
        private Label lblGoblinEvidenceTime;
        private Label lblGoblinEvidenceConfidence;
        private Label lblGoblinEvidenceType;
        private Label lblGoblinEvidenceLast;
        private Label lblGoblinObservation;
        private Button btnResetGoblinTrackerStats;
        private Label lblGoblinActiveTime;
        private Label lblGoblinGph;
        private Label lblGoblinCount;
        private GroupBox grpSessionStats;
        private Label lblSessionRuntime;
        private Label lblSessionFailures;
        private Label lblSessionBlocked;
        private Label lblSessionTeleports;
        private Label lblSessionGames;
        private Label lblEscape;
    }
}
