using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace killTelemetry
{
    public partial class KillTelForm : Form
    {
        private bool _awaitHide = false, _didHide = false;
        private string _defTxt;
        public KillTelForm()
        {
            InitializeComponent();
            StartPosition = FormStartPosition.CenterScreen;
            _defTxt = this.Text;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _awaitHide = true;
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Visible = true;
            WindowState = FormWindowState.Normal;
            Focus();
        }

        private void KillTelForm_Resize(object sender, EventArgs e)
        {
            Visible = WindowState != FormWindowState.Minimized;
        }

        private bool _busy;
        private int _killedCount;

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void label1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right) return;
            WindowState = FormWindowState.Minimized;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (_awaitHide && !_didHide)
            {
                _didHide = true;
                WindowState = FormWindowState.Minimized;
                Visible = false;
                _awaitHide = false;
            }

            if (_busy) return;
            _busy = true;
            ThreadPool.QueueUserWorkItem(o =>
            {
                try {Thread.CurrentThread.Priority = ThreadPriority.BelowNormal; }
                catch{}
                try
                {
                    string[] killProcNames = new[]
                    {
                        "CompatTelRunner",
                        "software_reporter_tool",
                        "sedlauncher",
                        "smartscreen"
                    };
                    string[] killOtherNames = new[]
                    {
                        "Microsoft Compatibility Telemetry",
                        "Software Reporter Tool"
                    };
                    var allProcesses = Process.GetProcesses();
                    foreach (var p in allProcesses)
                    {
                        bool killed = false;
                        Thread.Sleep(1);
                        try
                        {
                            var fn = Path.GetFileNameWithoutExtension(p.MainModule.FileName);
                            foreach (var pName in killProcNames)
                                if (fn.Equals(pName, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    p.Kill();
                                    _killedCount++;
                                    killed = true;
                                    break;
                                }
                        }
                        catch
                        {
                            try
                            {
                                if (!killed)
                                    foreach (var pName in killProcNames)
                                        if (p.ProcessName.Equals(pName, StringComparison.InvariantCultureIgnoreCase))
                                        {
                                            p.Kill();
                                            _killedCount++;
                                            killed = true;
                                            break;
                                        }
                                if (!killed)
                                    foreach (var otherName in killOtherNames)
                                        if (p.ProcessName.Equals(otherName, StringComparison.InvariantCultureIgnoreCase))
                                        {
                                            p.Kill();
                                            _killedCount++;
                                            killed = true;
                                            break;
                                        }
                            }
                            catch { }
                        }
                    }
                }
                catch { }

                if (_killedCount > 0)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        this.Text = notifyIcon1.Text = _defTxt + " kills=" + _killedCount;
                    }));

                }
                _busy = false;
            });
        }
    }
}
