using Godot;
using System;

namespace NoExit
{
    /// <summary>
    /// NO EXIT - Light Manager
    /// سیستم نورپردازی و افکت‌های ترسناک
    /// </summary>
    public partial class LightManager : Node
    {
        #region Singleton
        private static LightManager _instance;
        public static LightManager Instance => _instance;
        #endregion

        #region Exports
        [Export] private float _baseAmbientLight = 0.3f;
        [Export] private float _flickerInterval = 0.1f;
        [Export] private float _flickerIntensity = 0.1f;
        [Export] private bool _enableFlicker = true;
        [Export] private bool _enableHorrorEvents = true;
        #endregion

        #region Private Fields
        private float _currentLightLevel;
        private float _targetLightLevel;
        private bool _isFlickering = false;
        private bool _isHorrorEventActive = false;
        private Timer _flickerTimer;
        private Timer _horrorEventTimer;
        private RandomNumberGenerator _random = new();
        #endregion

        #region Properties
        public float CurrentLightLevel => _currentLightLevel;
        public bool IsFlickering => _isFlickering;
        public bool IsHorrorEventActive => _isHorrorEventActive;
        #endregion

        #region Lifecycle
        public override void _EnterTree()
        {
            base._EnterTree();

            if (_instance == null)
            {
                _instance = this;
            }
            else
            {
                QueueFree();
                return;
            }
        }

        public override void _Ready()
        {
            base._Ready();

            _currentLightLevel = _baseAmbientLight;
            _targetLightLevel = _baseAmbientLight;

            // Setup flicker timer
            _flickerTimer = new Timer();
            _flickerTimer.Name = "FlickerTimer";
            _flickerTimer.WaitTime = _flickerInterval;
            _flickerTimer.OneShot = false;
            _flickerTimer.Timeout += OnFlickerTimeout;
            AddChild(_flickerTimer);

            // Setup horror event timer
            _horrorEventTimer = new Timer();
            _horrorEventTimer.Name = "HorrorEventTimer";
            _horrorEventTimer.OneShot = true;
            _horrorEventTimer.Timeout += OnHorrorEventEnd;
            AddChild(_horrorEventTimer);

            if (_enableFlicker)
            {
                _flickerTimer.Start();
            }

            // Random horror events
            if (_enableHorrorEvents)
            {
                ScheduleRandomHorrorEvent();
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

        public override void _Process(double delta)
        {
            // Smooth light level transition
            if (Mathf.Abs(_currentLightLevel - _targetLightLevel) > 0.01f)
            {
                _currentLightLevel = Mathf.Lerp(_currentLightLevel, _targetLightLevel, (float)delta * 5f);
                UpdateSceneLighting();
            }
        }
        #endregion

        #region Flicker Effects
        private void OnFlickerTimeout()
        {
            if (_isHorrorEventActive) return;

            // Random flicker
            if (_random.Randf() < 0.3f) // 30% chance each interval
            {
                float flickerAmount = _random.RandfRange(-_flickerIntensity, _flickerIntensity);
                _targetLightLevel = Mathf.Clamp(_baseAmbientLight + flickerAmount, 0.1f, 0.5f);

                // Quick return to normal
                var returnTimer = new Timer();
                returnTimer.WaitTime = 0.05f;
                returnTimer.OneShot = true;
                returnTimer.Timeout += () => _targetLightLevel = _baseAmbientLight;
                AddChild(returnTimer);
                returnTimer.Start();
            }
        }

        /// <summary>
        /// شروع چشمک زدن شدید
        /// </summary>
        public void StartIntenseFlicker(float duration = 2.0f)
        {
            _isFlickering = true;
            _flickerTimer.Stop();

            var flickerSequence = CreateTween();
            flickerSequence.SetLoops((int)(duration / 0.1f));

            for (int i = 0; i < (int)(duration / 0.1f); i++)
            {
                float randomLight = _random.RandfRange(0.05f, 0.4f);
                flickerSequence.TweenProperty(this, "_currentLightLevel", randomLight, 0.05f);
            }

            flickerSequence.Finished += () =>
            {
                _isFlickering = false;
                _flickerTimer.Start();
                _targetLightLevel = _baseAmbientLight;
            };
        }

        /// <summary>
        /// خاموش شدن کامل چراغ
        /// </summary>
        public void Blackout(float duration = 1.0f)
        {
            GD.Print("[LightManager] BLACKOUT!");
            _targetLightLevel = 0f;

            var timer = new Timer();
            timer.WaitTime = duration;
            timer.OneShot = true;
            timer.Timeout += () =>
            {
                _targetLightLevel = _baseAmbientLight;
                UpdateSceneLighting();
            };
            AddChild(timer);
            timer.Start();
        }
        #endregion

        #region Horror Events
        private void ScheduleRandomHorrorEvent()
        {
            if (!IsInstanceValid(this)) return;

            float delay = _random.RandfRange(10f, 30f); // Random 10-30 seconds
            var timer = new Timer();
            timer.WaitTime = delay;
            timer.OneShot = true;
            timer.Timeout += () =>
            {
                if (IsInstanceValid(this) && _enableHorrorEvents)
                {
                    TriggerRandomHorrorEvent();
                    ScheduleRandomHorrorEvent();
                }
            };
            AddChild(timer);
            timer.Start();
        }

        private void TriggerRandomHorrorEvent()
        {
            if (_isHorrorEventActive) return;

            _isHorrorEventActive = true;
            int eventType = (int)_random.RandfRange(0, 4);

            switch (eventType)
            {
                case 0:
                    // Light flicker
                    StartIntenseFlicker(1.5f);
                    break;

                case 1:
                    // Brief blackout
                    Blackout(0.5f);
                    break;

                case 2:
                    // Lights dim slightly
                    _targetLightLevel = 0.15f;
                    _horrorEventTimer.WaitTime = 3f;
                    _horrorEventTimer.Start();
                    break;

                case 3:
                    // Strange light color shift (simulated)
                    _targetLightLevel = 0.5f;
                    _horrorEventTimer.WaitTime = 2f;
                    _horrorEventTimer.Start();
                    break;
            }

            GD.Print($"[LightManager] Horror Event triggered: {eventType}");
        }

        private void OnHorrorEventEnd()
        {
            _isHorrorEventActive = false;
            _targetLightLevel = _baseAmbientLight;
        }
        #endregion

        #region Light Control
        /// <summary>
        /// تنظیم سطح نور کلی
        /// </summary>
        public void SetLightLevel(float level)
        {
            _targetLightLevel = Mathf.Clamp(level, 0f, 1f);
        }

        /// <summary>
        /// چرای تدریجی نور
        /// </summary>
        public void FadeLight(float targetLevel, float duration = 1.0f)
        {
            var tween = CreateTween();
            tween.TweenProperty(this, "_targetLightLevel", targetLevel, duration);
        }

        private void UpdateSceneLighting()
        {
            // This would affect all lights in the scene
            // For now, emit signal for other systems to use
            EmitSignal(SignalName.LightLevelChanged, _currentLightLevel);
        }

        /// <summary>
        /// دریافت نور فعلی محیط (برای استفاده در سایر اسکریپت‌ها)
        /// </summary>
        public float GetAmbientLight()
        {
            return _currentLightLevel;
        }

        /// <summary>
        /// آیا محیط تاریک است
        /// </summary>
        public bool IsDark()
        {
            return _currentLightLevel < 0.2f;
        }
        #endregion

        #region Emergency Lights
        /// <summary>
        /// روشن کردن چراغ اضطراری قرمز
        /// </summary>
        public void EnableEmergencyLights()
        {
            GD.Print("[LightManager] Emergency lights ON");
            SetLightLevel(0.6f);
        }

        /// <summary>
        /// خاموش کردن چراغ اضطراری
        /// </summary>
        public void DisableEmergencyLights()
        {
            GD.Print("[LightManager] Emergency lights OFF");
            SetLightLevel(_baseAmbientLight);
        }
        #endregion

        [Signal]
        public delegate void LightLevelChangedEventHandler(float newLevel);
    }
}
