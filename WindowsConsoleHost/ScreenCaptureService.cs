﻿using Common.EventArgs.Network;
using Common.Thread;
using Driver.Windows.Desktop;
using Driver.Windows.Screen;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsConsoleHost
{
    public class ScreenCaptureService
    {

        public HostThread Manager { get { return Common.Instance.Host.Instance.Thread; } }
        private DesktopInfo _DesktopInfo;
        private ScreenCapture _ScreenCapture;

        private StreamWriter f = new StreamWriter(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + "\\service.txt", true);

        protected Common.Config.Manager ConfigManager;

        public Driver.Interfaces.Mouse Mouse { get { return Driver.Manager.Instance.Mouse; } }
        public Driver.Interfaces.Keyboard Keyboard { get { return Driver.Manager.Instance.Keyboard; } }
        public Driver.Interfaces.Display Display { get { return Driver.Manager.Instance.Display; } }
        public Driver.Interfaces.FileManager FileManager { get { return Driver.Manager.Instance.FileManager; } }

        protected System.Timers.Timer connectionStatus;

        public void OnStart()
        {
            f.AutoFlush = true;
            f.WriteLine("Start");
            _ScreenCapture = new ScreenCapture(80);

            f.WriteLine("Screen");


            connectionStatus = new System.Timers.Timer(1000);
            connectionStatus.Elapsed += Connection_Elapsed;

            ConfigManager = new Common.Config.Manager();

            f.WriteLine("Server" + ConfigManager.HostConfig.ServerName);
            f.WriteLine("Configpath" + ConfigManager.ConfigPath);

            Manager.HostListener.OnConnected += new EventHandler<ConnectedEventArgs>(Network_OnConnected);

            Manager.HostListener.OnClientInitalizeConnected += (object sender, Common.EventArgs.Network.Host.ClientInitalizeConnectedEventArgs e) =>
            {

                var pair = Manager.Manager.CreateNewKeyPairKey(e.ClientSystemId);

                Network.Messages.Connection.Response.InitalizeHostConnectionMessage rs = new Network.Messages.Connection.Response.InitalizeHostConnectionMessage();
                rs.HostSystemId = Manager.Manager.SystemId;
                rs.ClientSystemId = e.ClientSystemId;
                rs.HostPublicKey = pair.PublicKey;

                Manager.Manager.sendMessage(rs);
            };

            Manager.HostListener.OnClientConnected += (object sender, ClientConnectedEventArgs e) =>
            {
                if (e.PasswordOk)
                {
                    f.WriteLine("Passwort Ok Verbunden mit: " + e.SystemId);
                }
                else
                {
                    f.WriteLine("Passwort Falsch Verbindung abgebrochen von: " + e.SystemId);
                }
            };
            Manager.HostListener.OnMouseMove += (object sender, MouseMoveEventArgs e) =>
            {
                Mouse.Move((int)e.X, (int)e.Y);
            };
            Manager.HostListener.OnKey += (object sender, Common.EventArgs.Network.KeyEventArgs e) =>
            {
                if (e.Mode == Network.Messages.Connection.OneWay.KeyMessage.KeyMode.Down)
                {
                    Keyboard.Down(e.Key);
                }
                else
                {
                    Keyboard.Up(e.Key);
                }

            };
            Manager.HostListener.OnMouseClick += (object sender, MouseClickEventArgs e) =>
            {
                if (e.DoubleClick)
                {
                    switch (e.Button)
                    {
                        case MouseClickEventArgs.ButtonType.Left:
                            Mouse.DoubleClickLeft((int)e.X, (int)e.Y);
                            break;
                        case MouseClickEventArgs.ButtonType.Middle:
                            Mouse.DoubleClickMiddle((int)e.X, (int)e.Y);
                            break;
                        case MouseClickEventArgs.ButtonType.Right:
                            Mouse.DoubleClickRight((int)e.X, (int)e.Y);
                            break;
                    }

                    return;
                }
                if (e.Down)
                {
                    switch (e.Button)
                    {
                        case MouseClickEventArgs.ButtonType.Left:
                            Mouse.ClickDownLeft((int)e.X, (int)e.Y);
                            break;
                        case MouseClickEventArgs.ButtonType.Middle:
                            Mouse.ClickDownMiddle((int)e.X, (int)e.Y);
                            break;
                        case MouseClickEventArgs.ButtonType.Right:
                            Mouse.ClickDownRight((int)e.X, (int)e.Y);
                            break;
                    }

                    return;
                }
                if (e.Up)
                {
                    switch (e.Button)
                    {
                        case MouseClickEventArgs.ButtonType.Left:
                            Mouse.ClickUpLeft((int)e.X, (int)e.Y);
                            break;
                        case MouseClickEventArgs.ButtonType.Middle:
                            Mouse.ClickUpMiddle((int)e.X, (int)e.Y);
                            break;
                        case MouseClickEventArgs.ButtonType.Right:
                            Mouse.ClickUpRight((int)e.X, (int)e.Y);
                            break;
                    }

                    return;
                }


                switch (e.Button)
                {
                    case MouseClickEventArgs.ButtonType.Left:
                        Mouse.ClickLeft((int)e.X, (int)e.Y);
                        break;
                    case MouseClickEventArgs.ButtonType.Middle:
                        Mouse.ClickMiddle((int)e.X, (int)e.Y);
                        break;
                    case MouseClickEventArgs.ButtonType.Right:
                        Mouse.ClickRight((int)e.X, (int)e.Y);
                        break;
                }

            };
            Manager.HostListener.OnScreenshotRequest += HostListener_OnScreenshotRequest;
            Manager.HostListener.OnClientClose += HostListener_OnClientClose;
            Display.SetManager(Manager.Manager);
            Manager.HostListener.onPeerConnected += HostListener_onPeerConnected;
            Manager.HostListener.onPeerDisconnected += HostListener_onPeerDisconnected;
            Manager.HostListener.onNetworkError += HostListener_onNetworkError;

            Manager.Start();
        }

        public void OnStop()
        {
            connectionStatus.Stop();
            Manager.Stop();
        }

        public bool IsRunning()
        {
            return Manager.IsRunning();
        }

        private void HostListener_OnScreenshotRequest(object sender, ScreenshotRequestEventArgs e)
        {
            Display.RequestScreenshot(e, e.Fullscreen);
        }

        private void HostListener_OnClientClose(object sender, Common.EventArgs.Network.Host.ClientCloseEventArgs e)
        {
            Display.RemoveClient(e.ClientSystemId);
            Network.Messages.Connection.Response.CloseHostConnectionMessage rs = new Network.Messages.Connection.Response.CloseHostConnectionMessage();
            rs.HostSystemId = Manager.Manager.SystemId;
            rs.ClientSystemId = e.ClientSystemId;

            Manager.Manager.sendMessage(rs);
        }

        private void Connection_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Display.RemoveAllClients();
            Manager.Reconnect();
        }

        private void HostListener_onNetworkError(object sender, EventArgs e)
        {
            f.WriteLine("Network Error");
            connectionStatus.Start();
        }

        private void HostListener_onPeerDisconnected(object sender, EventArgs e)
        {
            f.WriteLine("Introducer Disconnected");
            connectionStatus.Start();
        }

        private void HostListener_onPeerConnected(object sender, EventArgs e)
        {
            f.WriteLine("Introducer Connected");
            connectionStatus.Stop();
        }

        void Network_OnConnected(object sender, ConnectedEventArgs e)
        {
            f.WriteLine("SystemId: " + e.SystemId + " Password:" + Manager.Manager.Password);
        }
    }
}
