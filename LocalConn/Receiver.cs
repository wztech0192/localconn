using System;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Diagnostics;
using WebSocketSharp.Server;
using WebSocketSharp;
using System.Threading;

namespace LocalConn
{
    public class Receiver : WebSocketBehavior
    {

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        DateTime lastTime = DateTime.Now;
        //Mouse actions
        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;
        private int display = 1;
        private bool Lock = false;
        private bool MouseRun = false;
        private double radian, speed;
        private readonly IntPtr ConsoleWindow;

        public Receiver(IntPtr ConsoleWindow)
        {
            Console.WriteLine("Connected!!");
            this.ConsoleWindow = ConsoleWindow;
            ShowWindow(ConsoleWindow, display);
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            Handler(e.Data);
        }

        private void LockPC()
        {
            new Thread(() =>
            {
                while (Lock)
                {
                    try
                    {
                        Cursor.Position = new Point(0, 0);
                        Thread.Sleep(50);
                    }
                    catch { }
                }
            }).Start();
        }

        private void MouseMovement()
        {
            new Thread(MouseWorker).Start();
        }

        private void MouseWorker()
        {
            while (MouseRun)
            {
                try
                {
                    int x = (int)(Cursor.Position.X + (Math.Cos(radian) * speed));
                    int y = (int)(Cursor.Position.Y - (Math.Sin(radian) * speed));
                    Cursor.Position = new Point(x, y);
                    Thread.Sleep(16);
                }
                catch { }
            }
        }

        private void Handler(string data)
        {
            string[] split = data.Split('&');
            string type = split[0];
            string action = split[1];
            switch (type)
            {
                case "cmd":
                    if (action == "EXIT")
                    {
                        Environment.Exit(1);
                    }
                    else if (action == "TOGGLE")
                    {
                        display = display == 1 ? 0 : 1;
                        ShowWindow(ConsoleWindow, display);
                    }
                    else if (action == "LOCK")
                    {
                        if (!Lock)
                        {
                            Lock = true;
                            LockPC();
                        }
                        else
                        {
                            Lock = false;
                        }
                    }
                    else
                    {
                        action = action.Replace("\n", "&");
                        Process process = new Process();
                        ProcessStartInfo startInfo = new ProcessStartInfo();
                        startInfo.UseShellExecute = false;
                        startInfo.RedirectStandardOutput = true;
                        startInfo.FileName = "CMD.exe";
                        startInfo.Arguments = "/C " + action;
                        process.StartInfo = startInfo;
                        process.Start();
                        string output = process.StandardOutput.ReadToEnd();
                        Console.WriteLine(output);
                        Send("b&" + output);
                        process.WaitForExit();
                    }
                    break;

                case "mm":
                    speed = int.Parse(split[2]);
                    radian = (Math.PI / 180) * int.Parse(action); 
                    if (!MouseRun)
                    {
                        MouseRun = true;
                        MouseMovement();
                    }
                  //  int fps = (int)((DateTime.Now - lastTime).TotalMilliseconds);
                  //  lastTime = DateTime.Now;
                  //  Console.WriteLine(fps);
                    break;
                case "ms":
                    MouseRun = false;
                    break;

                case "mc":
                    uint click = (uint)(action == "left" ? MOUSEEVENTF_LEFTDOWN : MOUSEEVENTF_RIGHTDOWN);
                    mouse_event(click, (uint)Cursor.Position.X, (uint)Cursor.Position.Y, 0, 0);
                    break;
                case "me":
                    uint unclick = (uint)(action == "left" ? MOUSEEVENTF_LEFTUP : MOUSEEVENTF_RIGHTUP);
                    mouse_event(unclick, (uint)Cursor.Position.X, (uint)Cursor.Position.Y, 0, 0);
                    break;
                case "ky":
                    string key = action;
                    SendKeys.SendWait(action);
                    break;
            }
        }

    }
}
