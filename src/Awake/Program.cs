using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Awake;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var single = new Mutex(true, @"Local\ZmasterC39.Awake", out bool first);
        if (!first) return;
        ApplicationConfiguration.Initialize();
        using var context = new AwakeContext();
        Application.Run(context);
    }
}

internal sealed class AwakeContext : ApplicationContext
{
    private const uint Continuous = 0x80000000, SystemRequired = 1, DisplayRequired = 2;
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint SetThreadExecutionState(uint flags);

    private readonly NotifyIcon tray;
    private readonly Form panel = new();
    private readonly Label status = new() { AutoSize = true };
    private readonly Label battery = new() { AutoSize = true };
    private readonly CheckBox screen = new() { Text = "Mantener también la pantalla encendida", AutoSize = true };
    private readonly CheckBox startup = new() { Text = "Abrir al iniciar sesión", AutoSize = true };
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 1000 };
    private readonly ToolStripMenuItem trayStatus = new("Normal");
    private readonly Icon activeIcon = MakeIcon(true), idleIcon = MakeIcon(false);
    private DateTimeOffset? deadline;
    private bool active, closing, warnedClaude;
    private int ticks;

    public AwakeContext()
    {
        var menu = new ContextMenuStrip();
        trayStatus.Enabled = false;
        menu.Items.Add(trayStatus);
        menu.Items.Add("Abrir Awake", null, (_, _) => ShowPanel());
        foreach (var minutes in new[] { 15, 30, 60, 120 })
        {
            int duration = minutes;
            menu.Items.Add($"Activar {duration} min", null, (_, _) => Start(duration));
        }
        menu.Items.Add("Activar sin límite", null, (_, _) => Start(null));
        menu.Items.Add("Volver a normal", null, (_, _) => Stop());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Salir", null, (_, _) => ExitThread());
        tray = new NotifyIcon { Icon = idleIcon, Text = "Awake · Normal", Visible = true, ContextMenuStrip = menu };
        tray.DoubleClick += (_, _) => ShowPanel();
        BuildPanel();
        timer.Tick += (_, _) => Tick();
        timer.Start();
        Tick();
        if (!Environment.GetCommandLineArgs().Contains("--background")) ShowPanel();
    }

    private void BuildPanel()
    {
        panel.Text = "Awake · Pausa café";
        panel.ClientSize = new Size(440, 530);
        panel.FormBorderStyle = FormBorderStyle.FixedDialog;
        panel.MaximizeBox = false;
        panel.StartPosition = FormStartPosition.CenterScreen;
        panel.BackColor = Color.FromArgb(246, 243, 236);
        panel.Font = new Font("Segoe UI", 10);
        panel.Icon = idleIcon;
        var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
            WrapContents = false, Padding = new Padding(22), AutoScroll = true };
        layout.Controls.Add(new Label { Text = "☕  Tu equipo sigue trabajando.", Font = new Font("Segoe UI", 17, FontStyle.Bold), AutoSize = true, Margin = new Padding(0, 0, 0, 18) });
        layout.Controls.Add(status);
        layout.Controls.Add(battery);
        layout.Controls.Add(new Label { Text = "Elige cuánto dura tu pausa:", AutoSize = true, Margin = new Padding(0, 18, 0, 6) });
        var choices = new FlowLayoutPanel { Width = 390, Height = 80 };
        foreach (var minutes in new[] { 15, 30, 60, 120 })
        {
            int duration = minutes;
            var button = new Button { Text = minutes < 60 ? $"{minutes} min" : $"{minutes / 60} h", Width = 87, Height = 34 };
            button.Click += (_, _) => Start(duration);
            choices.Controls.Add(button);
        }
        var unlimited = new Button { Text = "Sin límite", Width = 177, Height = 32 };
        unlimited.Click += (_, _) => Start(null);
        choices.Controls.Add(unlimited);
        var stop = new Button { Text = "Volver a normal", Width = 177, Height = 32 };
        stop.Click += (_, _) => Stop();
        choices.Controls.Add(stop);
        layout.Controls.Add(choices);
        screen.CheckedChanged += (_, _) => { if (active && !ApplyPower()) StopWithNotice("Windows no pudo mantener el equipo activo."); };
        layout.Controls.Add(screen);
        using (var key = Registry.CurrentUser.OpenSubKey(RunKey)) startup.Checked = key?.GetValue("Awake") != null;
        startup.CheckedChanged += (_, _) =>
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(RunKey);
                if (startup.Checked) key.SetValue("Awake", $"\"{Application.ExecutablePath}\" --background");
                else key.DeleteValue("Awake", false);
            }
            catch (Exception ex) { MessageBox.Show(panel, ex.Message, "No se pudo guardar el inicio automático"); }
        };
        layout.Controls.Add(startup);
        var mobile = new Button { Text = "Abrir Claude en el móvil / Remote Control", Width = 380, Height = 34, Margin = new Padding(0, 15, 0, 4) };
        mobile.Click += (_, _) =>
        {
            Clipboard.SetText("/remote-control");
            Process.Start(new ProcessStartInfo("https://claude.ai/code") { UseShellExecute = true });
            MessageBox.Show(panel, "Copiamos /remote-control. Pégalo en tu sesión de Claude Code y abre el enlace que genere desde tu móvil. Awake no activa Remote Control por sí solo.", "Continuar en el móvil");
        };
        layout.Controls.Add(mobile);
        var lid = new Button { Text = "Configurar cierre de tapa en Windows", Width = 380, Height = 32 };
        lid.Click += (_, _) => Process.Start(new ProcessStartInfo("control.exe", "/name Microsoft.PowerOptions /page pageGlobalSettings") { UseShellExecute = true });
        layout.Controls.Add(lid);
        layout.Controls.Add(new Label { Text = "Para trabajar con la tapa cerrada, elige «No hacer nada» en Windows. Ese ajuste es permanente: el temporizador no lo restaura. Awake evita la suspensión por inactividad mientras está activo. No guardes el portátil activo en una mochila.", MaximumSize = new Size(380, 0), AutoSize = true, ForeColor = Color.FromArgb(90, 65, 42), Margin = new Padding(0, 12, 0, 0) });
        panel.Controls.Add(layout);
        panel.FormClosing += (_, e) => { if (!closing) { e.Cancel = true; panel.Hide(); } };
    }

    private void ShowPanel() { panel.Show(); panel.WindowState = FormWindowState.Normal; panel.Activate(); }
    private bool ApplyPower() => SetThreadExecutionState(Continuous | SystemRequired | (screen.Checked ? DisplayRequired : 0)) != 0;
    private void Start(int? minutes)
    {
        var power = SystemInformation.PowerStatus;
        if (power.PowerLineStatus == PowerLineStatus.Offline && power.BatteryLifePercent >= 0 && power.BatteryLifePercent < .10f)
        {
            MessageBox.Show(panel, "Conecta el cargador: la batería está por debajo del 10 %.", "Batería baja");
            return;
        }
        if (!ApplyPower()) { MessageBox.Show(panel, "Windows rechazó la solicitud de energía.", "Awake"); return; }
        active = true;
        deadline = minutes.HasValue ? DateTimeOffset.UtcNow.AddMinutes(minutes.Value) : null;
        RefreshStatus();
    }
    private void Stop()
    {
        SetThreadExecutionState(Continuous);
        active = false;
        deadline = null;
        RefreshStatus();
    }
    private void StopWithNotice(string message) { Stop(); tray.ShowBalloonTip(5000, "Awake", message, ToolTipIcon.Info); }
    private void Tick()
    {
        var power = SystemInformation.PowerStatus;
        bool onBattery = power.PowerLineStatus == PowerLineStatus.Offline;
        if (active && SessionPolicy.ShouldStop(DateTimeOffset.UtcNow, deadline, onBattery, power.BatteryLifePercent))
            StopWithNotice(onBattery && power.BatteryLifePercent >= 0 && power.BatteryLifePercent < .10f ? "Batería por debajo del 10 %. Volvemos a normal." : "Tu pausa terminó. Windows vuelve a gestionar la suspensión.");
        battery.Text = power.BatteryLifePercent < 0 || power.BatteryChargeStatus.HasFlag(BatteryChargeStatus.NoSystemBattery)
            ? "Sin batería disponible" : $"Batería: {power.BatteryLifePercent:P0} · {(onBattery ? "Sin cargador" : "Con alimentación / estado desconocido")}";
        RefreshStatus();
        if (++ticks % 30 == 0 && !active && !warnedClaude)
        {
            foreach (var process in Process.GetProcessesByName("claude"))
            {
                process.Dispose();
                if (warnedClaude) continue;
                warnedClaude = true;
                tray.ShowBalloonTip(5000, "Claude Code está abierto", "Activa Awake si necesitas evitar la suspensión por inactividad.", ToolTipIcon.Info);
            }
        }
    }
    private void RefreshStatus()
    {
        string text = active ? $"Despierto · {SessionPolicy.Remaining(DateTimeOffset.UtcNow, deadline)}" : "Normal · Windows gestiona la suspensión";
        status.Text = text;
        trayStatus.Text = text;
        tray.Text = "Awake · " + (active ? SessionPolicy.Remaining(DateTimeOffset.UtcNow, deadline) : "Normal");
        tray.Icon = active ? activeIcon : idleIcon;
    }
    protected override void ExitThreadCore()
    {
        closing = true;
        timer.Stop();
        Stop();
        tray.Visible = false;
        panel.Close();
        base.ExitThreadCore();
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { timer.Dispose(); tray.Dispose(); panel.Dispose(); activeIcon.Dispose(); idleIcon.Dispose(); }
        base.Dispose(disposing);
    }
    private static Icon MakeIcon(bool enabled)
    {
        using var bitmap = new Bitmap(32, 32);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var brush = new SolidBrush(enabled ? Color.FromArgb(177, 103, 47) : Color.Gray);
        using var pen = new Pen(brush, 3);
        graphics.FillRectangle(brush, 5, 12, 17, 14);
        graphics.DrawEllipse(pen, 19, 13, 8, 10);
        graphics.DrawLine(pen, 4, 29, 25, 29);
        if (enabled) { graphics.DrawArc(pen, 8, 1, 5, 10, -70, 160); graphics.DrawArc(pen, 16, 1, 5, 10, -70, 160); }
        var handle = bitmap.GetHicon();
        try { using var borrowed = Icon.FromHandle(handle); return (Icon)borrowed.Clone(); }
        finally { DestroyIcon(handle); }
    }
    [DllImport("user32.dll")] private static extern bool DestroyIcon(IntPtr handle);
}
