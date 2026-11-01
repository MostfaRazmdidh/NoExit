using Godot;
using System.Collections.Generic;

namespace NoExit
{
    /// <summary>
    /// NO EXIT - Game Manager
    /// مدیریت کلی بازی خروج ممنوع
    /// Singleton pattern برای دسترسی سراسری
    /// </summary>
    public partial class GameManager : Node
    {
        // Singleton instance
        private static GameManager _instance;
        public static GameManager Instance => _instance;

        // Game State
        public enum GameState { Playing, Paused, Dialogue, Inventory, Menu }
        public GameState CurrentState { get; private set; } = GameState.Playing;

        // Player Data
        public int CurrentCycle { get; set; } = 1;
        public Vector2 PlayerSpawnPosition { get; set; } = new Vector2(576, 588);

        // Puzzle States - ذخیره وضعیت معماها
        private Dictionary<string, bool> _puzzleStates = new();
        public Dictionary<string, bool> PuzzleStates => _puzzleStates;

        // Room/Level
        public string CurrentRoom { get; set; } = "res://scenes/main.tscn";

        // Collected Items
        private List<string> _inventory = new();
        public List<string> Inventory => _inventory;

        // Interaction
        public Node2D LastInteractedObject { get; set; }

        // References
        public PlayerController Player { get; set; }
        public CanvasLayer UI { get; set; }

        public override void _EnterTree()
        {
            base._EnterTree();

            // Singleton setup
            if (_instance == null)
            {
                _instance = this;
            }
            else
            {
                QueueFree(); // Only one instance allowed
                return;
            }
        }

        public override void _ExitTree()
        {
            base._ExitTree();
            if (_instance == this)
            {
                _instance = null;
            }
        }

        public override void _Ready()
        {
            // Game starts in playing state
            SetGameState(GameState.Playing);
        }

        /// <summary>
        /// تنظیم وضعیت بازی
        /// </summary>
        public void SetGameState(GameState newState)
        {
            CurrentState = newState;

            // Update player movement based on state
            if (Player != null)
            {
                Player.SetCanMove(CurrentState == GameState.Playing);
            }

            // Emit signal for UI updates
            EmitSignal(SignalName.GameStateChanged, (int)CurrentState);
        }

        /// <summary>
        /// بررسی وضعیت معما
        /// </summary>
        public bool IsPuzzleSolved(string puzzleId)
        {
            if (_puzzleStates.TryGetValue(puzzleId, out bool solved))
                return solved;
            return false;
        }

        /// <summary>
        /// علامت‌گذاری معما به عنوان حل شده
        /// </summary>
        public void SetPuzzleSolved(string puzzleId, bool solved = true)
        {
            _puzzleStates[puzzleId] = solved;
            GD.Print($"[GameManager] Puzzle '{puzzleId}' solved: {solved}");
        }

        /// <summary>
        /// اضافه کردن آیتم به موجودی
        /// </summary>
        public void AddToInventory(string itemId)
        {
            if (!_inventory.Contains(itemId))
            {
                _inventory.Add(itemId);
                GD.Print($"[GameManager] Added to inventory: {itemId}");
            }
        }

        /// <summary>
        /// بررسی وجود آیتم در موجودی
        /// </summary>
        public bool HasItem(string itemId)
        {
            return _inventory.Contains(itemId);
        }

        /// <summary>
        /// نمایش پیام داستانی
        /// </summary>
        public void ShowStoryText(string text, float duration = 3.0f)
        {
            SetGameState(GameState.Dialogue);
            // TODO: Implement with UI
            GD.Print($"[Story] {text}");

            // Auto resume after duration
            var timer = new Timer();
            timer.WaitTime = duration;
            timer.OneShot = true;
            timer.Timeout += () => SetGameState(GameState.Playing);
            AddChild(timer);
            timer.Start();
        }

        /// <summary>
        /// شروع مجدد چرخه (برای پایان بازی)
        /// </summary>
        public void StartNewCycle()
        {
            CurrentCycle++;
            GD.Print($"[GameManager] Starting Cycle #{CurrentCycle}");

            // TODO: Reset room and player position
            // For now, just show the cycle message
            ShowStoryText($"چرخه شماره {CurrentCycle}", 4.0f);
        }

        /// <summary>
        /// ذخیره بازی
        /// </summary>
        public void SaveGame()
        {
            // TODO: Implement full save system
            GD.Print("[GameManager] Save requested");
        }

        /// <summary>
        /// بارگذاری بازی
        /// </summary>
        public void LoadGame()
        {
            // TODO: Implement full save system
            GD.Print("[GameManager] Load requested");
        }

        [Signal]
        public delegate void GameStateChangedEventHandler(int newState);
    }
}
