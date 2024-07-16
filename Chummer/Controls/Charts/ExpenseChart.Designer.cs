namespace Chummer.Controls.Charts
{
    partial class ExpenseChart
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            chart = new LiveChartsCore.SkiaSharpView.WinForms.CartesianChart();
            SuspendLayout();
            // 
            // chart
            // 
            chart.Dock = DockStyle.Fill;
            chart.Location = new Point(0, 0);
            chart.Name = "chart";
            chart.Size = new Size(400, 400);
            chart.TabIndex = 0;
            // 
            // ExpenseChart2
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(chart);
            Name = "ExpenseChart2";
            Size = new Size(400, 400);
            Load += ExpenseChart_Load;
            ResumeLayout(false);
        }

        #endregion

        private LiveChartsCore.SkiaSharpView.WinForms.CartesianChart chart;
    }
}
