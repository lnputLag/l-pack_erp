using System;
using System.Windows;
using Client.Interfaces.Main;

namespace Client.Interfaces.Production.Corrugator._CorrugatorMachineOperator.Elements
{
    public partial class IdleSplit : ControlBase
    {
        private bool _suppressSliderUpdate;
        private bool _suppressSplitUpdate;
        
        private readonly  TimeSpan MinSegmentDuration = TimeSpan.FromMinutes(1);

        public IdleSplit()
        {
            InitializeComponent();
            
            OnLoad = () =>
            {
                if (FromTime == default || ToTime == default || FromTime >= ToTime)
                {
                    var now = DateTime.Now;
                    FromTime = now.AddMinutes(-30);
                    ToTime = now;
                }

                if (SplitTime <= FromTime || SplitTime >= ToTime)
                {
                    SplitTime = FromTime + TimeSpan.FromTicks((ToTime - FromTime).Ticks / 2);
                }

                SyncSliderWithSplit();
                UpdateDurationsAndValidation();
            };

            OnUnload = () => { };
        }
        
        public event EventHandler<SplitConfirmedEventArgs> SplitConfirmed;
        public event EventHandler Cancelled;

        public class SplitConfirmedEventArgs : EventArgs
        {
            public DateTime FromTime { get; set; }
            public DateTime ToTime { get; set; }
            public DateTime SplitTime { get; set; }
            public TimeSpan LeftDuration { get; set; }
            public TimeSpan RightDuration { get; set; }
        }

        public static readonly DependencyProperty FromTimeProperty =
            DependencyProperty.Register(nameof(FromTime), typeof(DateTime), typeof(IdleSplit),
                new PropertyMetadata(DateTime.Now, OnFromOrToChanged));

        public static readonly DependencyProperty ToTimeProperty =
            DependencyProperty.Register(nameof(ToTime), typeof(DateTime), typeof(IdleSplit),
                new PropertyMetadata(DateTime.Now.AddMinutes(30), OnFromOrToChanged));

        public static readonly DependencyProperty SplitTimeProperty =
            DependencyProperty.Register(nameof(SplitTime), typeof(DateTime), typeof(IdleSplit),
                new PropertyMetadata(DateTime.Now, OnSplitTimeChanged));

        public static readonly DependencyProperty SliderValueProperty =
            DependencyProperty.Register(nameof(SliderValue), typeof(double), typeof(IdleSplit),
                new PropertyMetadata(0.5, OnSliderValueChanged));

        public static readonly DependencyProperty LeftDurationProperty =
            DependencyProperty.Register(nameof(LeftDuration), typeof(TimeSpan), typeof(IdleSplit),
                new PropertyMetadata(TimeSpan.Zero));

        public static readonly DependencyProperty RightDurationProperty =
            DependencyProperty.Register(nameof(RightDuration), typeof(TimeSpan), typeof(IdleSplit),
                new PropertyMetadata(TimeSpan.Zero));

        public static readonly DependencyProperty IsValidProperty =
            DependencyProperty.Register(nameof(IsValid), typeof(bool), typeof(IdleSplit),
                new PropertyMetadata(false));

        public static readonly DependencyProperty ValidationMessageProperty =
            DependencyProperty.Register(nameof(ValidationMessage), typeof(string), typeof(IdleSplit),
                new PropertyMetadata(string.Empty));

        public DateTime FromTime
        {
            get => (DateTime)GetValue(FromTimeProperty);
            set => SetValue(FromTimeProperty, value);
        }

        public DateTime ToTime
        {
            get => (DateTime)GetValue(ToTimeProperty);
            set => SetValue(ToTimeProperty, value);
        }

        public DateTime SplitTime
        {
            get => (DateTime)GetValue(SplitTimeProperty);
            set => SetValue(SplitTimeProperty, value);
        }

        // 0..1
        public double SliderValue
        {
            get => (double)GetValue(SliderValueProperty);
            set => SetValue(SliderValueProperty, value);
        }

        public TimeSpan LeftDuration
        {
            get => (TimeSpan)GetValue(LeftDurationProperty);
            set => SetValue(LeftDurationProperty, value);
        }

        public TimeSpan RightDuration
        {
            get => (TimeSpan)GetValue(RightDurationProperty);
            set => SetValue(RightDurationProperty, value);
        }

        public bool IsValid
        {
            get => (bool)GetValue(IsValidProperty);
            set => SetValue(IsValidProperty, value);
        }

        public string ValidationMessage
        {
            get => (string)GetValue(ValidationMessageProperty);
            set => SetValue(ValidationMessageProperty, value);
        }

        private static void OnFromOrToChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (IdleSplit)d;
            c.CoerceSplitIntoRange();
            c.SyncSliderWithSplit();
            c.UpdateDurationsAndValidation();
        }

        private static void OnSplitTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (IdleSplit)d;
            if (c._suppressSplitUpdate) return;
            
            if (c.FromTime < c.ToTime)
            {
                if (c.SplitTime <= c.FromTime) c.SplitTime = c.FromTime.AddTicks(1);
                if (c.SplitTime >= c.ToTime) c.SplitTime = c.ToTime.AddTicks(-1);
            }

            c.SyncSliderWithSplit();
            c.UpdateDurationsAndValidation();
        }

        private static void OnSliderValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (IdleSplit)d;
            if (c._suppressSliderUpdate) return;
            c.SyncSplitWithSlider();
            c.UpdateDurationsAndValidation();
        }

        private void CoerceSplitIntoRange()
        {
            if (FromTime >= ToTime)
            {
                IsValid = false;
                ValidationMessage = "Неверный интервал: время начала должно быть меньше времени окончания.";
                return;
            }

            if (SplitTime <= FromTime || SplitTime >= ToTime)
            {
                SplitTime = FromTime + TimeSpan.FromTicks((ToTime - FromTime).Ticks / 2);
            }
        }

        private void SyncSliderWithSplit()
        {
            if (FromTime >= ToTime) return;
            var total = (ToTime - FromTime).TotalMilliseconds;
            var pos = (SplitTime - FromTime).TotalMilliseconds;
            var v = total > 0 ? pos / total : 0.5;

            _suppressSliderUpdate = true;
            try
            {
                SliderValue = Math.Max(0.0, Math.Min(1.0, v));
            }
            finally
            {
                _suppressSliderUpdate = false;
            }
        }

        private void SyncSplitWithSlider()
        {
            if (FromTime >= ToTime) return;
            var totalTicks = (ToTime - FromTime).Ticks;
            var newTicks = FromTime.Ticks + (long)(totalTicks * Math.Max(0.0, Math.Min(1.0, SliderValue)));

            var proposed = new DateTime(newTicks);
            // Don't allow boundaries
            if (proposed <= FromTime) proposed = FromTime.AddTicks(1);
            if (proposed >= ToTime) proposed = ToTime.AddTicks(-1);

            _suppressSplitUpdate = true;
            try
            {
                SplitTime = proposed;
            }
            finally
            {
                _suppressSplitUpdate = false;
            }
        }

        private void UpdateDurationsAndValidation()
        {
            if (FromTime >= ToTime)
            {
                LeftDuration = TimeSpan.Zero;
                RightDuration = TimeSpan.Zero;
                IsValid = false;
                ValidationMessage = "Неверный интервал: время начала должно быть меньше времени окончания.";
                return;
            }

            LeftDuration = SplitTime - FromTime;
            RightDuration = ToTime - SplitTime;

            if (LeftDuration <= TimeSpan.Zero || RightDuration <= TimeSpan.Zero)
            {
                IsValid = false;
                ValidationMessage = "Время разбиения должно быть строго между началом и концом простоя.";
                return;
            }

            if (LeftDuration < MinSegmentDuration || RightDuration < MinSegmentDuration)
            {
                IsValid = false;
                ValidationMessage = "Простой не может быть короче 1 минуты";
                return;
            }
            
            IsValid = true;
            ValidationMessage = string.Empty;
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            if (!IsValid) return;

            SplitConfirmed?.Invoke(this, new SplitConfirmedEventArgs
            {
                FromTime = this.FromTime,
                ToTime = this.ToTime,
                SplitTime = this.SplitTime,
                LeftDuration = this.LeftDuration,
                RightDuration = this.RightDuration
            });
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Cancelled?.Invoke(this, EventArgs.Empty);
        }
    }
}