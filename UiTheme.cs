using System.Drawing.Drawing2D;

namespace logingui;

internal static class UiTheme
{
    public static readonly Color Background = Color.FromArgb(241, 245, 249);
    public static readonly Color Ink = Color.FromArgb(30, 41, 59);
    public static readonly Color Muted = Color.FromArgb(100, 116, 139);
    public static readonly Color Accent = Color.FromArgb(37, 99, 235);
    public static readonly Color Navy = Color.FromArgb(15, 23, 42);

    public static void Apply(Form form)
    {
        form.Font = new Font("Segoe UI", 10f);
        form.BackColor = Background;
        form.ForeColor = Ink;
        form.StartPosition = FormStartPosition.CenterScreen;
        StyleChildren(form);
    }

    private static void StyleChildren(Control parent)
    {
        foreach (Control control in parent.Controls)
        {
            if (control is Button button) StyleButton(button);
            else if (control is TextBox text)
            {
                text.BorderStyle = BorderStyle.FixedSingle;
                text.BackColor = Color.White;
                text.ForeColor = Ink;
            }
            else if (control is Label label) label.ForeColor = Muted;
            StyleChildren(control);
        }
    }

    public static void StyleButton(Button button, bool primary = false, bool danger = false)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = primary ? 0 : 1;
        button.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
        button.BackColor = primary ? Accent : Color.White;
        button.ForeColor = primary ? Color.White : danger ? Color.FromArgb(220, 38, 38) : Ink;
        button.FlatAppearance.MouseOverBackColor = primary ? Color.FromArgb(29, 78, 216) : Color.FromArgb(226, 232, 240);
        button.FlatAppearance.MouseDownBackColor = primary ? Color.FromArgb(30, 64, 175) : Color.FromArgb(203, 213, 225);
        button.Cursor = Cursors.Hand;
        button.Font = new Font("Segoe UI", 10f, primary ? FontStyle.Bold : FontStyle.Regular);
        button.Height = Math.Max(button.Height, 38);
    }

    public static void StyleList(ListView list)
    {
        list.BackColor = Color.White;
        list.ForeColor = Ink;
        list.BorderStyle = BorderStyle.None;
        list.Font = new Font("Segoe UI", 10f);
        list.OwnerDraw = true;
        list.DrawColumnHeader += (_, e) =>
        {
            using var background = new SolidBrush(Background);
            e.Graphics.FillRectangle(background, e.Bounds);
            TextRenderer.DrawText(e.Graphics, e.Header?.Text ?? "", list.Font,
                Rectangle.Inflate(e.Bounds, -12, 0), Muted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        };
        list.DrawItem += (_, _) => { };
        list.DrawSubItem += (_, e) =>
        {
            var selected = e.Item?.Selected == true;
            using var background = new SolidBrush(selected ? Color.FromArgb(219, 234, 254) :
                e.ItemIndex % 2 == 0 ? Color.White : Color.FromArgb(248, 250, 252));
            e.Graphics.FillRectangle(background, e.Bounds);
            var color = selected ? Color.FromArgb(30, 64, 175) : Ink;
            if (e.ColumnIndex == 2)
            {
                color = e.SubItem?.Text?.StartsWith("有", StringComparison.Ordinal) == true || e.SubItem?.Text == "Yes" ? Color.FromArgb(5, 150, 105) : Muted;
            }
            TextRenderer.DrawText(e.Graphics, e.SubItem?.Text ?? "", list.Font,
                Rectangle.Inflate(e.Bounds, -12, 0), color,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        };
    }
}
