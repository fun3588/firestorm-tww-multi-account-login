namespace logingui;

partial class Form1
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1120, 760);
        MinimumSize = new Size(940, 640);
        Text = "Firestorm 多账号登录器 v1.0";
    }
}
