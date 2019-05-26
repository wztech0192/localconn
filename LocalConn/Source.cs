using System;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Reflection;
using WebSocketSharp.Server;
using WebSocketSharp;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace Control
{
    class Program
    {
        private Program()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                string resourceName = new AssemblyName(args.Name).Name + ".dll";
                string resource = Array.Find(GetType().Assembly.GetManifestResourceNames(), element => element.EndsWith(resourceName));

                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource))
                {
                    byte[] assemblyData = new byte[stream.Length];
                    stream.Read(assemblyData, 0, assemblyData.Length);
                    return Assembly.Load(assemblyData);
                }
            };
            Init();
        }

        private void Init()
        {
            IntPtr ConsoleWindow = Process.GetCurrentProcess().MainWindowHandle;
            //  ShowWindow(h, display);
            var wssv = new WebSocketServer(1998);
            wssv.AddWebSocketService("/", ()=>new Receiver(ConsoleWindow));
            wssv.Start();
            do
            {
                Console.WriteLine("Try the following IP Addres in your browser dashboard!");
                PrintIP();
                Console.WriteLine("Type \"Quit\" to exit the app");
            } while (Console.ReadLine().ToLower() != "quit");
         
            wssv.Stop();
        }

        static void PrintIP()
        {
            Console.WriteLine("***************************");
            string hostName = Dns.GetHostName(); // Retrive the Name of HOST  
            // Get the IP  
            var host = Dns.GetHostEntry(hostName);
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    Console.WriteLine(ip);
                }
            }
            Console.WriteLine("***************************");
        }

        static void Main(string[] args)
        {

            new Program();
        }
    }
}


public class Receiver : WebSocketBehavior
{

    [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
    public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    //Mouse actions
    private const int MOUSEEVENTF_LEFTDOWN = 0x02;
    private const int MOUSEEVENTF_LEFTUP = 0x04;
    private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
    private const int MOUSEEVENTF_RIGHTUP = 0x10;
    private int display = 1;
    private bool Lock = false;
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
                double radians = (Math.PI / 180) * int.Parse(action);
                int speed = int.Parse(split[2]);
                int x = (int)(Cursor.Position.X + (Math.Cos(radians) * speed));
                int y = (int)(Cursor.Position.Y - (Math.Sin(radians) * speed));
                Cursor.Position = new Point(x, y);
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