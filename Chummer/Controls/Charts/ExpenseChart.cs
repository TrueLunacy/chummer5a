using LiveChartsCore.Defaults;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.VisualElements;
using LiveChartsCore.Drawing;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Padding = LiveChartsCore.Drawing.Padding;

namespace Chummer.Controls.Charts
{
    public partial class ExpenseChart : UserControl
    {
        private readonly LineSeries<DateTimePoint> series;
        private readonly LabelVisual title;
        // also used as default karma format
        private const string DefaultFormat = "#,0.##";
        private string nuyenFormat = DefaultFormat;
        private Axis YAxis;
        private DateTimeAxis XAxis;

        public List<DateTimePoint> ExpenseValues { get; } = new List<DateTimePoint>();

        public ExpenseChart()
        {
            InitializeComponent();
            series = new LineSeries<DateTimePoint>
            {
                AnimationsSpeed = TimeSpan.FromMilliseconds(200),
                LineSmoothness = 0,
                Values = ExpenseValues,
                GeometrySize = 10,
                GeometryFill = new SolidColorPaint(SKColors.White),
                DataPadding = new LvcPoint(0, 0),
            };
            chart.Series = [series];
            XAxis = new DateTimeAxis(TimeSpan.FromSeconds(1), dt =>
            {
                return dt.ToString(GlobalSettings.CustomDateTimeFormats
                    ? GlobalSettings.CustomDateFormat
                      + ' ' + GlobalSettings.CustomTimeFormat
                    : GlobalSettings.CultureInfo.DateTimeFormat.ShortDatePattern
                      + ' ' + GlobalSettings.CultureInfo.DateTimeFormat.ShortTimePattern, GlobalSettings.CultureInfo);
            })
            {
                TextSize = 11,
            };
            chart.XAxes = [XAxis];
            YAxis = new Axis()
            {
                TextSize = 11,
                NameTextSize = 11,
                NamePadding = new Padding(10),
            };
            chart.YAxes = [YAxis];
            SetKarmaMode();
        }

        private bool _NuyenMode;

        private void ExpenseChart_Load(object _, EventArgs __)
        {
            if (ParentForm is CharacterShared charform
                && charform.CharacterObject?.Settings.NuyenFormat is not null)
            {
                nuyenFormat = charform.CharacterObject.Settings.NuyenFormat;
            }
            else
            {
                nuyenFormat = DefaultFormat;
            }
        }

        public void NormalizeAxisValues()
        {
            if (ExpenseValues.Count == 0)
                return;
            var max = ExpenseValues.Select(v => v.Value).Max().Value;
            var min = ExpenseValues.Select(v => v.Value).Min().Value;
            var interval = NuyenMode ? 5000 : 5;
            max = Math.Max(
                Math.Ceiling(max / interval),
                Math.Floor(min / interval) + 1
            ) * interval;
            min = Math.Floor(min / interval) * interval;
            YAxis.MaxLimit = max;
            YAxis.MinLimit = min;
        }

        private void SetNuyenMode()
        {
            series.GeometryStroke = new SolidColorPaint(SKColors.Red, 3);
            series.Stroke = new SolidColorPaint(SKColors.Red, 3);
            series.Fill = new SolidColorPaint(new SKColor(0xFF, 0x00, 0x00, 0x7F));
            YAxis.Labeler = v => v.ToString(nuyenFormat) + LanguageManager.GetString("String_NuyenSymbol");
            YAxis.Name = LanguageManager.GetString("Label_SummaryNuyen");
        }

        private void SetKarmaMode()
        {
            series.GeometryStroke = new SolidColorPaint(SKColors.Blue, 3);
            series.Stroke = new SolidColorPaint(SKColors.Blue, 3);
            series.Fill = new SolidColorPaint(new SKColor(00, 0x00, 0xFF, 0x7F));
            YAxis.Labeler = v => v.ToString(DefaultFormat);
            YAxis.Name = LanguageManager.GetString("String_Karma");
        }

        public bool NuyenMode
        {
            get => _NuyenMode;
            set
            {
                if (value == _NuyenMode)
                    return;
                _NuyenMode = value;
                chart.SuspendLayout();
                if (_NuyenMode)
                    SetNuyenMode();
                else
                    SetKarmaMode();
                chart.ResumeLayout();
            }
        }
    }
}
